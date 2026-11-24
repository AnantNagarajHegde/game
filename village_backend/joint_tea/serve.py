"""Local HTTP adapter for one trained Manjunatha model. Each streamed record contains both outputs."""
import argparse
import base64
from io import BytesIO
import json
from http.server import BaseHTTPRequestHandler, HTTPServer
import os
from pathlib import Path
import time
import wave

# One character/word at a time. Spawning a BLAS pool adds latency and memory.
for key in ("OPENBLAS_NUM_THREADS","MKL_NUM_THREADS","OMP_NUM_THREADS"):
    os.environ[key]="1"
import numpy as np
from model import JointModel, WEIGHTS
from aids import reading_aids


def wav(samples):
    buf=BytesIO()
    with wave.open(buf,"wb") as f:
        f.setparams((1,2,16000,0,"NONE","not compressed"))
        f.writeframes(samples.tobytes())
    return buf.getvalue()


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--renderer",type=Path,required=True)
    parser.add_argument("--weights",type=Path,default=WEIGHTS)
    parser.add_argument("--port",type=int,default=18877)
    args=parser.parse_args()
    model=JointModel(args.renderer,args.weights)

    class Handler(BaseHTTPRequestHandler):
        def log_message(self,*args): pass
        def body(self,data,kind):
            self.send_response(200);self.send_header("Content-Type",kind)
            self.send_header("Content-Length",str(len(data)));self.end_headers();self.wfile.write(data)
        def do_GET(self):
            if self.path!="/health":return self.send_error(404)
            self.body(b'{"version":2,"provider":"manjunatha-joint","model_configured":true,"speech_configured":true,"authentication_required":false,"joint_model":true}',"application/json")
        def record(self,obj):
            self.wfile.write(json.dumps(obj,ensure_ascii=False,separators=(",",":")).encode()+b"\n")
            self.wfile.flush()
        def do_POST(self):
            started=False
            try:
                size=int(self.headers.get("Content-Length",0))
                if not 0<size<=8192:return self.send_error(413)
                obj=json.loads(self.rfile.read(size))
                if not isinstance(obj,dict):return self.send_error(400)
                if self.path=="/v2/village/speech":
                    text=obj.get("text")
                    if obj.get("language")!="kn-IN" or not isinstance(text,str) or not 0<len(text)<=256:
                        return self.send_error(400)
                    clips=[pcm for _,pcm in model.groups("namaskara",forced=text)]
                    if not clips:return self.send_error(400)
                    self.body(wav(np.concatenate(clips)),"audio/wav")
                    return
                if self.path!="/joint/turn":return self.send_error(404)
                prompt,req=obj.get("prompt"),obj.get("id")
                if not isinstance(prompt,str) or not 0<len(prompt.strip())<=512 or not isinstance(req,str) or len(req)!=32:
                    return self.send_error(400)
                self.send_response(200);self.send_header("Content-Type","application/x-ndjson")
                self.send_header("Connection","close");self.end_headers();started=True
                groups=0
                samples=0
                spoken=[]
                start=time.perf_counter()
                for text,pcm in model.groups(prompt):
                    samples+=len(pcm)
                    spoken.append(text)
                    self.record(dict(id=req,seq=groups,text=text,wav=base64.b64encode(wav(pcm)).decode("ascii")))
                    groups+=1
                roman,english=reading_aids(" ".join(spoken))
                self.record(dict(id=req,seq=groups,done=True,roman=roman,english=english))
                elapsed=time.perf_counter()-start
                print(f"JOINT_INFERENCE chunks={groups} id={req} generation_ms={round(elapsed*1000)} audio_ms={round(samples/16)}",flush=True)
            except (BrokenPipeError,ConnectionResetError,ConnectionAbortedError):
                self.close_connection=True
            except (ValueError,TypeError,KeyError) as exc:
                if not started:self.send_error(400)
                self.close_connection=True
                print("JOINT_ERROR",type(exc).__name__,flush=True)
    with HTTPServer(("127.0.0.1",args.port),Handler) as server:
        print("JOINT_READY",flush=True)
        server.serve_forever()

if __name__=="__main__":main()
