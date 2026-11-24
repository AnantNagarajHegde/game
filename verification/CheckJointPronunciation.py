"""Compare locally generated clips and matched MMS controls using an independent Kannada recognizer."""
import argparse
import json
from pathlib import Path
import sys
import unicodedata

import onnx_asr
import onnxruntime as ort


def words(text):
    numbers = {"1":"ಒಂದು","2":"ಎರಡು","3":"ಮೂರು","4":"ನಾಲ್ಕು","೧":"ಒಂದು","೨":"ಎರಡು","೩":"ಮೂರು","೪":"ನಾಲ್ಕು"}
    numbers.update({"1ಕ್ಕೆ":"ಒಂದಕ್ಕೆ","೧ಕ್ಕೆ":"ಒಂದಕ್ಕೆ"})
    clean = "".join(c if unicodedata.category(c)[0] in "LMN" else " " for c in unicodedata.normalize("NFC",text))
    return [numbers.get(w,w) for w in clean.split()]


def edit_distance(a,b):
    row = list(range(len(b)+1))
    for i,x in enumerate(a,1):
        nxt = [i]
        for j,y in enumerate(b,1):
            nxt.append(min(row[j]+1,nxt[-1]+1,row[j-1]+(x!=y)))
        row = nxt
    return row[-1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cache",type=Path,required=True)
    parser.add_argument("--asr",type=Path,required=True)
    parser.add_argument("--teacher",type=Path,help="Prepared MMS checkpoint for synthesizing matched control clips")
    args = parser.parse_args()
    cases=json.loads((args.cache/"held-out.json").read_text("utf-8"))
    if args.teacher:
        sys.path.insert(0,str(Path(__file__).resolve().parents[1]/"village_backend"))
        from tea_voice import Voice
        teacher=Voice(args.teacher)
        for i,case in enumerate(cases):
            (args.cache/f"control-{i}.wav").write_bytes(teacher.wav(case["expected"]))
        del teacher
    opts = ort.SessionOptions()
    opts.intra_op_num_threads = 3
    model = onnx_asr.load_model("nemo-conformer-tdt",str(args.asr),quantization="int8",sess_options=opts,
                               providers=["CPUExecutionProvider"])
    totals={k:[0,0,0,0] for k in ("held","control")}
    out=[]
    for i,case in enumerate(cases):
        entry=dict(expected=case["expected"])
        ref=words(entry["expected"])
        chars="".join(ref)
        for kind in totals:
            transcript=model.recognize(str(args.cache/f"{kind}-{i}.wav"))
            seq=words(transcript)
            entry[kind]=transcript
            counts=totals[kind]
            counts[0]+=edit_distance(ref,seq);counts[1]+=len(ref)
            counts[2]+=edit_distance(chars,"".join(seq));counts[3]+=len(chars)
        print(i,entry,flush=True)
        out.append(entry)
    result=dict(verifier="adalat-ai/koyal-kn-120m-1.0-onnx/int8",cases=out,
                scores={k:dict(word_errors=v[0],reference_words=v[1],character_errors=v[2],reference_characters=v[3])
                        for k,v in totals.items()})
    (args.cache/"pronunciation.json").write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding="utf-8")

if __name__=="__main__":
    main()
