"""Extract an acoustic renderer and numeric training targets from the prepared Kannada teacher."""
import argparse
import json
from pathlib import Path
import re
import unicodedata

import numpy as np
import onnx
import onnxruntime as ort
from onnx import TensorProto, helper

from corpus import examples

ROOT = Path(__file__).resolve().parents[2]
CACHE = ROOT / "verification/Temp/JointTea"
SOURCE = ROOT / "My project (1)/Assets/_Paridhwani/Scripts/Runtime/TeaConversation.cs"
PRIOR = "/Add_output_0"
MASK = "/Cast_3_output_0"
MEAN = "/text_encoder/Split_output_0"
LOGS = "/text_encoder/Split_output_1"
DURATION = "/Ceil_output_0"


def value(name, shape):
    return helper.make_tensor_value_info(name, TensorProto.FLOAT, shape)


def subgraph(model, inputs, outputs, name):
    supplied = {x.name for x in inputs}
    needed = {x.name for x in outputs}
    nodes = []
    for n in reversed(model.graph.node):
        if any(x in needed and x not in supplied for x in n.output):
            nodes.append(n)
            needed.update(x for x in n.input if x)
    initializers = [i for i in model.graph.initializer if i.name in needed]
    remaining = needed - supplied - {i.name for i in initializers} - {v for n in nodes for v in n.output}
    if remaining:
        raise ValueError(f"Unresolved ONNX inputs {remaining}")
    graph = helper.make_graph(list(reversed(nodes)), name, inputs, outputs, initializers)
    out = helper.make_model(graph, opset_imports=model.opset_import, ir_version=model.ir_version)
    onnx.checker.check_model(out)
    return out


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--teacher", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=CACHE)
    parser.add_argument("--renderer-only",action="store_true",help="Prepare inference without training annotations")
    args = parser.parse_args()
    teacher, out = args.teacher, args.output
    out.mkdir(parents=True, exist_ok=True)
    model = onnx.load(teacher / "kannada.onnx")
    vocab = json.loads((teacher / "vocab.json").read_text(encoding="utf-8"))
    renderer = subgraph(model, [value(PRIOR, [1, 192, "frames"]), value(MASK, [1, 1, "frames"])],
                        [model.graph.output[0]], "Kannada latent waveform decoder")
    onnx.save(renderer, out / "renderer.onnx")
    del renderer
    if args.renderer_only:
        return
    labels = subgraph(model, list(model.graph.input),
                      [value(MEAN, [1,"tokens",192]), value(LOGS, [1,"tokens",192]),
                       value(DURATION, [1,1,"tokens"])], "Offline teacher annotations")
    del model
    for n in labels.graph.node:
        if n.op_type == "RandomNormalLike":
            n.attribute.append(helper.make_attribute("seed", 19.0))
    opts = ort.SessionOptions()
    opts.intra_op_num_threads = 2
    session = ort.InferenceSession(labels.SerializeToString(), opts, providers=["CPUExecutionProvider"])
    sentences = {response for _,response in examples()[0]}
    sentences.update(re.findall(r'"([^"\n]*[\u0c80-\u0cff][^"\n]*)"', SOURCE.read_text(encoding="utf-8-sig")))
    words = sorted({word for text in sentences for word in text.split()})
    where = {word:index for index,word in enumerate(words)}
    recorded = {}
    def align(text):
        ids = [vocab[c] for c in unicodedata.normalize("NFC",text) if c in vocab]
        tokens = np.zeros((1,2*len(ids)+1), np.int64)
        tokens[0,1::2] = ids
        mean,logs,dur = session.run(None, {"input_ids":tokens, "attention_mask":np.ones_like(tokens)})
        return np.concatenate((mean[0],logs[0],np.log1p(dur[0,0,:,None])),axis=1)
    # Keep the acoustic word boundary, including the encoded space. Use neighboring words
    # from real training sentences to teach coarticulation. Test sentences never enter here.
    for text in sorted(sentences):
        parts=text.split()
        clean=["".join(c for c in unicodedata.normalize("NFC",s) if c in vocab) for s in parts]
        data=align(" ".join(parts)+" ")
        offset=0
        for i,(word,chars) in enumerate(zip(parts,clean)):
            if chars:
                size=len(chars)+int(not word.endswith((".","!","?")))
                phase=0 if i==0 else 2 if word.endswith((".","!","?")) else 1
                key=f"{where[word]}_{phase}"
                recorded.setdefault(key,[]).append(data[2*offset:2*(offset+size)+1].copy())
            offset+=len(chars)+1
    annotations={}
    for index,word in enumerate(words):
        if not any(c in vocab for c in word):continue
        word_size=sum(c in vocab for c in word)+int(not word.endswith((".","!","?")))
        for phase,left in enumerate(("","ಸರಿ ","ಸರಿ ")):
            key=f"{index}_{phase}"
            if key in recorded:
                annotations[key]=np.mean(recorded[key],axis=0)
            else:
                start=2*sum(c in vocab for c in left)
                data=align(left+word+" ಕೊಡುತ್ತೇನೆ")
                annotations[key]=data[start:start+2*word_size+1]
    (out / "acoustic_vocab.json").write_text(json.dumps(dict(words=words,chars=vocab),ensure_ascii=False),encoding="utf-8")
    np.savez_compressed(out / "teacher-targets.npz",**annotations)
    print(f"{len(words)} words, {len(annotations)} aligned acoustic targets. Renderer has only latent and mask inputs.",flush=True)

if __name__ == "__main__":
    main()
