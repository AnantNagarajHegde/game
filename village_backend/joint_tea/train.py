"""Train the tea-stall model from scratch on supervised dialogues and aligned teacher acoustics."""
import argparse
from collections import Counter
import json
from pathlib import Path

import numpy as np
import torch
from torch import nn
from torch.nn import functional as F

from corpus import examples
from model import features, character_ids, START, END, WEIGHTS

CACHE=Path(__file__).resolve().parents[2]/"verification/Temp/JointTea"
H=96

class Network(nn.Module):
    def __init__(self, inputs, words, chars):
        super().__init__()
        self.input_emb=nn.Embedding(inputs,96,padding_idx=0)
        self.context=nn.Linear(96,H)
        self.word=nn.Embedding(words,64)
        self.decoder=nn.GRU(64+H,H,batch_first=True)
        self.text=nn.Linear(H,words)
        self.letters=nn.Embedding(chars,32)
        self.positions=nn.Embedding(64,24)
        self.phases=nn.Embedding(3,8)
        self.ac1=nn.Linear(3*32+24+64+8+12,256)
        self.ac2=nn.Linear(256,256)
        self.ac3=nn.Linear(256,385)

    def language(self, inputs, prev):
        vec=self.input_emb(inputs).sum(1)/inputs.ne(0).sum(1).clamp(min=1).sqrt()[:,None]
        ctx=torch.tanh(self.context(vec))
        x=torch.cat((self.word(prev),ctx[:,None].expand(-1,prev.size(1),-1)),dim=-1)
        hidden,_=self.decoder(x,ctx.unsqueeze(0))
        return self.text(hidden),hidden

    def acoustic(self,x,h):
        prev,current,nxt,pos,word,phase=x.T
        x=torch.cat((self.letters(prev),self.letters(current),self.letters(nxt),self.positions(pos),
                     self.word(word),self.phases(phase),h[:,:12]*.08),dim=-1)
        return self.ac3(torch.tanh(self.ac2(torch.tanh(self.ac1(x)))))


def padded(rows):
    a=torch.zeros(len(rows),max(map(len,rows)),dtype=torch.long)
    for i,row in enumerate(rows): a[i,:len(row)]=torch.tensor(row)
    return a


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--cache",type=Path,default=CACHE)
    ap.add_argument("--weights",type=Path,default=WEIGHTS)
    ap.add_argument("--steps",type=int,default=1200)
    ap.add_argument("--acoustic-steps",type=int,default=3200)
    ap.add_argument("--resume-heads",type=Path,help="Run only joint fine-tuning from matching pretrained heads")
    args=ap.parse_args()
    torch.manual_seed(417);torch.set_num_threads(3)
    train,held=examples()
    if {v for _,v in train}.intersection(v for _,v in held):
        raise ValueError("Held-out reply leaked into training")
    count=Counter(f for x,_ in train for f in features(x))
    inv=["<pad>"]+sorted(i for i,n in count.items() if not i.startswith("~") or n>=5)
    words=["<pad>",START,END,"<unk>"]+sorted({w for _,y in train for w in y.split()})
    im={w:i for i,w in enumerate(inv)}; wm={w:i for i,w in enumerate(words)}
    inputs=padded([[im[t] for t in features(q) if t in im] for q,_ in train])
    target=padded([[wm[w] for w in y.split()]+[wm[END]] for _,y in train])
    prev=torch.cat((torch.full((len(train),1),wm[START]),target[:,:-1]),1)
    spec=json.loads((args.cache/"acoustic_vocab.json").read_text("utf-8"))
    chars=spec["chars"]
    model=Network(len(inv),len(words),max(chars.values())+1)
    if args.resume_heads:
        original=json.loads((args.resume_heads/"vocabulary.json").read_text("utf-8"))
        if original["input"]!=inv or original["output"]!=words or original["chars"]!=chars:raise ValueError("Checkpoint vocabulary differs")
        with np.load(args.resume_heads/"joint.npz",allow_pickle=False) as archive:
            model.load_state_dict({k:torch.from_numpy(archive[k]) for k in model.state_dict()})
    opt=torch.optim.AdamW(model.parameters(),lr=.003,weight_decay=.0001)
    for step in range(0 if args.resume_heads else args.steps):
        ids=torch.randint(len(train),(64,))
        logits,_=model.language(inputs[ids],prev[ids])
        loss=F.cross_entropy(logits.flatten(0,1),target[ids].flatten(),ignore_index=0)
        opt.zero_grad();loss.backward();nn.utils.clip_grad_norm_(model.parameters(),2);opt.step()
        if step%300==0:print("language",step,round(loss.item(),4),flush=True)
    # Real generated-word contexts from teacher-forced training replies; no test prompts enter training.
    pools={i:[] for i in range(len(words))}
    with torch.no_grad():
        for rows in torch.arange(len(train)).split(96):
            _,h=model.language(inputs[rows],prev[rows])
            tok=target[rows]
            for i in torch.unique(tok).tolist():
                pools[i].append(h[tok==i])
    states=torch.zeros(len(words),128,H)
    for i,items in pools.items():
        if items:
            a=torch.cat(items);states[i]=a[torch.randint(len(a),(128,))]
    xa=[];ya=[]
    with np.load(args.cache/"teacher-targets.npz",allow_pickle=False) as labels:
        for key in labels.files:
            index,phase=map(int,key.split("_"));word=spec["words"][index];seq=character_ids(word,chars)
            y=labels[key]
            if len(y)!=len(seq):raise ValueError((word,len(y),len(seq)))
            xa.extend((int(seq[max(0,i-1)]) if i else 0,int(c),int(seq[i+1]) if i+1<len(seq) else 0,
                       min(63,i),wm.get(word,3),phase) for i,c in enumerate(seq))
            ya.append(y)
    x=torch.tensor(xa);y=torch.from_numpy(np.concatenate(ya)).float()
    mean=y.mean(0);std=y.std(0).clamp(min=.07);y=(y-mean)/std
    # Preserve learned word semantics while fitting acoustic features to the same shared embeddings.
    opt=torch.optim.AdamW([*model.letters.parameters(),*model.positions.parameters(),*model.phases.parameters(),
                            *model.ac1.parameters(),*model.ac2.parameters(),*model.ac3.parameters()],lr=.002)
    for step in range(0 if args.resume_heads else args.acoustic_steps):
        ids=torch.randint(len(x),(384,));batch=x[ids]
        prediction=model.acoustic(batch,states[batch[:,4],torch.randint(128,(len(ids),))])
        diff=(prediction-y[ids]).square()
        loss=diff[:,:192].mean()+.45*diff[:,192:384].mean()+.20*diff[:,-1].mean()
        opt.zero_grad();loss.backward();opt.step()
        if step==args.acoustic_steps*2//3:
            for group in opt.param_groups:group["lr"]=.0008
        if step%450==0:print("acoustic",step,round(loss.item(),4),flush=True)
    # Fine-tune both outputs on the same decoder state. The acoustic loss backpropagates
    # through the shared lexical embedding and recurrent backbone during this final stage.
    torch.manual_seed(119)
    by_token=torch.zeros(len(words),3,128,dtype=torch.long)
    for token in range(4,len(words)):
        for phase in range(3):
            choices=torch.nonzero((x[:,4]==token)&(x[:,5]==phase)).flatten()
            if len(choices):
                by_token[token,phase]=choices[torch.randint(len(choices),(128,))]
    endings=torch.tensor([w.endswith((".","!","?")) for w in words])
    opt=torch.optim.AdamW(model.parameters(),lr=.00015,weight_decay=.0001)
    for _ in range(180):
        rows=torch.randint(len(train),(40,))
        logits,hidden=model.language(inputs[rows],prev[rows])
        current=target[rows]
        text_loss=F.cross_entropy(logits.flatten(0,1),current.flatten(),ignore_index=0)
        valid=torch.nonzero(current>=4)
        selected=valid[torch.randint(len(valid),(384,))]
        row,pos=selected.T
        token=current[row,pos]
        phase=torch.where(pos==0,0,torch.where(endings[token],2,1))
        sample=by_token[token,phase,torch.randint(128,(len(token),))]
        diff=(model.acoustic(x[sample],hidden[row,pos])-y[sample]).square()
        acoustic_loss=diff[:,:192].mean()+.45*diff[:,192:384].mean()+.2*diff[:,-1].mean()
        opt.zero_grad();(text_loss+.4*acoustic_loss).backward()
        nn.utils.clip_grad_norm_(model.parameters(),2)
        opt.step()
    args.weights.mkdir(parents=True,exist_ok=True)
    np.savez_compressed(args.weights/"joint.npz",**{k:v.detach().numpy() for k,v in model.state_dict().items()},
                        target_mean=mean.numpy(),target_std=std.numpy())
    meta=dict(input=inv,output=words,chars=chars,parameters=sum(p.numel() for p in model.parameters()),
              training_dialogues=len(train),training_distinct_replies=len(set(y for _,y in train)),
              acoustic_character_examples=len(x),seed=417)
    (args.weights/"vocabulary.json").write_text(json.dumps(meta,ensure_ascii=False,indent=1),encoding="utf-8")
    print({k:v for k,v in meta.items() if k not in ("input","output","chars")},flush=True)

if __name__=="__main__":main()
