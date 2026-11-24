"""CPU inference for the jointly trained word and acoustic predictor. No training corpus at runtime."""
import json
from pathlib import Path
import unicodedata

import numpy as np

START, END = "<start>", "<end>"
WEIGHTS = Path(__file__).resolve().parent / "weights"
PRIOR, MASK = "/Add_output_0", "/Cast_3_output_0"


def features(prompt):
    chars = unicodedata.normalize("NFC", prompt).casefold()
    words = "".join(c if unicodedata.category(c)[0] in "LMN" else " " for c in chars).split()[:64]
    return words + ["~"+a+"_"+b for a,b in zip(words,words[1:])]


def character_ids(word, chars):
    ids = [0]
    for char in unicodedata.normalize("NFC", word):
        if char in chars:
            ids.extend((chars[char],0))
    if len(ids)>1 and not word.endswith((".","!","?")):
        ids.extend((chars[" "],0))
    return np.array(ids, dtype=np.int64)


def sigmoid(x):
    return 1 / (1 + np.exp(-np.clip(x,-40,40)))


class JointModel:
    def __init__(self, renderer, weights=WEIGHTS):
        import onnxruntime as ort
        cfg = json.loads((weights / "vocabulary.json").read_text("utf-8"))
        self.words, self.chars = cfg["output"], cfg["chars"]
        self.inputs = {w:i for i,w in enumerate(cfg["input"])}
        self.outputs = {w:i for i,w in enumerate(self.words)}
        with np.load(weights / "joint.npz", allow_pickle=False) as weights_file:
            self.p = {k:weights_file[k] for k in weights_file.files}
        opts = ort.SessionOptions()
        opts.intra_op_num_threads = 2
        opts.inter_op_num_threads = 1
        opts.enable_cpu_mem_arena = False
        self.renderer = ort.InferenceSession(str(renderer),opts,providers=["CPUExecutionProvider"])
        if {x.name for x in self.renderer.get_inputs()} != {PRIOR,MASK}:
            raise ValueError("The renderer must take acoustic latents and a frame mask")

    def linear(self,x,name):
        return x @ self.p[name+".weight"].T + self.p[name+".bias"]

    def context(self,prompt):
        pieces=features(prompt)
        words=[w for w in pieces if not w.startswith("~")]
        known=sum(w in self.inputs for w in words)
        ids=[self.inputs[w] for w in pieces if w in self.inputs]
        # A lone familiar drink in an otherwise unfamiliar request is insufficient evidence for an order.
        if not ids or known*2<len(words): ids=[self.inputs["unknown"]]
        pooled = self.p["input_emb.weight"][ids].sum(0)/np.float32(len(ids)**.5)
        return np.tanh(self.linear(pooled,"context"))

    def step(self, context, h, previous):
        x = np.concatenate((self.p["word.weight"][previous],context))
        xp = x @ self.p["decoder.weight_ih_l0"].T + self.p["decoder.bias_ih_l0"]
        hp = h @ self.p["decoder.weight_hh_l0"].T + self.p["decoder.bias_hh_l0"]
        xr,xz,xn = np.split(xp,3)
        hr,hz,hn = np.split(hp,3)
        z = sigmoid(xz+hz)
        new = (1-z)*np.tanh(xn+sigmoid(xr+hr)*hn)+z*h
        return new,int(np.argmax(self.linear(new,"text")))

    def acoustic(self,h,word,phase,rng):
        # The learned acoustic head sees the emitted word, local characters, its position
        # in the utterance, and the same recurrent state used by the language head.
        p = self.p
        ids = character_ids(word,self.chars)
        if len(ids)==1: raise ValueError("Word has no Kannada characters")
        n=len(ids)
        prev=np.r_[0,ids[:-1]]
        following=np.r_[ids[1:],0]
        lexical=p["word.weight"][self.outputs.get(word,3)]
        item=np.concatenate((lexical,p["phases.weight"][phase],h[:12]*.08))
        x=np.concatenate((p["letters.weight"][prev],p["letters.weight"][ids],p["letters.weight"][following],
                          p["positions.weight"][np.minimum(np.arange(n),63)],
                          np.broadcast_to(item,(n,len(item)))),axis=1)
        z=np.tanh(self.linear(x,"ac1"))
        z=np.tanh(self.linear(z,"ac2"))
        result=self.linear(z,"ac3")*p["target_std"]+p["target_mean"]
        repeats=np.clip(np.rint(np.expm1(np.clip(result[:,-1],0,3.4))),1,30).astype(int)
        if not word.endswith((".","!","?")): repeats[-1]=0  # The next word begins at the same blank.
        mean=np.repeat(result[:,:192],repeats,axis=0)
        logs=np.repeat(result[:,192:384],repeats,axis=0)
        return (mean+.5*np.exp(np.clip(logs,-6,2))*rng.standard_normal(mean.shape,dtype=np.float32)).T

    def groups(self,prompt,forced=None):
        ctx=self.context(prompt)
        h=ctx.copy()
        previous=self.outputs[START]
        words,latents=[],[]
        rng=np.random.default_rng(73)
        force=iter(forced.split()) if forced else None
        for index in range(32):
            h,chosen=self.step(ctx,h,previous)
            if force is not None:
                word=next(force,None)
                if word is None: break
                chosen=self.outputs.get(word,3)
            else:
                if chosen==self.outputs[END]: break
                if chosen<4: raise ValueError("Invalid generated token")
                word=self.words[chosen]
            phase=0 if index==0 else 2 if word.endswith((".","!","?")) else 1
            latents.append(self.acoustic(h,word,phase,rng))
            words.append(word)
            previous=chosen
            # A short lookahead joins words in latent space before decoding the waveform.
            if len(words)>=3 or word.endswith((".","!","?")):
                yield " ".join(words), self.render(latents)
                words,latents=[],[]
        else:
            raise ValueError("Decoder exceeded the turn length bound")
        if words: yield " ".join(words), self.render(latents)

    def render(self,latents):
        prior=np.ascontiguousarray(np.concatenate(latents,axis=1)[None],dtype=np.float32)
        wave,=self.renderer.run(None,{PRIOR:prior,MASK:np.ones((1,1,prior.shape[-1]),np.float32)})
        return (np.clip(wave.ravel(),-1,1)*32767).astype("<i2")
