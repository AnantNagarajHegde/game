"""Held-out semantic composition and actual decoding checks; writes a listenable Kannada example."""
import argparse
import json
from pathlib import Path
import time
import wave

import numpy as np
from aids import reading_aids
from corpus import examples
from model import JointModel, WEIGHTS

ROOT=Path(__file__).resolve().parents[2]
CACHE=ROOT/"verification/Temp/JointTea"

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache",type=Path,default=CACHE)
    parser.add_argument("--weights",type=Path,default=WEIGHTS)
    args=parser.parse_args()
    train,held=examples()
    seen={answer for _,answer in train}
    if any(not all(reading_aids(text)) for _,text in train+held):
        raise ValueError("A supported reply is missing its optional reading aids")
    model=JointModel(args.cache/"renderer.onnx",args.weights)
    metrics=[]
    for index,(prompt,expected) in enumerate(held):
        start=time.perf_counter()
        chunks=[];audio=[];first=None
        for text,pcm in model.groups(prompt):
            if first is None:first=time.perf_counter()-start
            chunks.append(text);audio.append(pcm)
        actual=" ".join(chunks)
        assert expected not in seen
        pcm=np.concatenate(audio)
        if not len(pcm) or np.sqrt(np.square(pcm.astype(float)).mean())<25:
            raise ValueError("Invalid generated audio")
        elapsed=time.perf_counter()-start
        path=args.cache/f"held-{index}.wav"
        with wave.open(str(path),"wb") as f:
            f.setparams((1,2,16000,0,"NONE","not compressed"));f.writeframes(pcm.tobytes())
        metrics.append(dict(prompt=prompt,expected=expected,actual=actual,exact=actual==expected,
                            new_reply=actual not in seen,packets=len(chunks),seconds=round(len(pcm)/16000,3),
                            first_packet_ms=round(1000*first),realtime_factor=round(elapsed/(len(pcm)/16000),3)))
    (args.cache/"held-out.json").write_text(json.dumps(metrics,ensure_ascii=False,indent=2),encoding="utf-8")
    for item in metrics: print(item["exact"],item["actual"],"RTF",item["realtime_factor"],flush=True)
    if not all(i["exact"] and i["new_reply"] for i in metrics):raise SystemExit(1)

if __name__=="__main__":main()
