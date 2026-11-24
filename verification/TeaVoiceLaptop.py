"""Run the isolated Windows tea-stall player with local CPU inference."""
import argparse
import json
import os
from pathlib import Path
import socket
import statistics
import struct
import subprocess
import sys
import time
import urllib.request
from datetime import datetime
import psutil

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT/"verification/Temp/TeaVoice"

def main():
    parser=argparse.ArgumentParser()
    parser.add_argument("--demo",action="store_true",help="Open the interactive tea stall")
    parser.add_argument("--editor",type=Path,default=Path("C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe"))
    parser.add_argument("--engine",type=Path,help="Path to Paridhwani-NLP/src/assessment_host.py")
    parser.add_argument("--width",type=int,default=960)
    parser.add_argument("--height",type=int,default=540)
    parser.add_argument("--scale",choices=["1","1.3"],default="1")
    parser.add_argument("--settle-seconds",type=int,choices=range(0,91),default=30,metavar="0..90")
    args=parser.parse_args()
    if not args.demo and not args.engine: parser.error("--engine is required for benchmarking")
    run=CACHE/datetime.now().strftime("run-%Y%m%d-%H%M%S-%f")
    run.mkdir(parents=True)
    print(run,flush=True)
    owned={}
    logs=[]
    env=dict(os.environ,PARIDHWANI_TEA_VOICE="1",PARIDHWANI_CONVERSATION_URL="http://127.0.0.1:18767/",
             PARIDHWANI_SERVICE_TOKEN="",PARIDHWANI_TEA_START=str(run/"go"),PARIDHWANI_TEA_CAPTURE=str(run/"caption.png"),
             PARIDHWANI_TEA_SCALE=args.scale)
    def start(name,cmd,visible=False):
        out=(run/(name+".out")).open("wb")
        logs.append(out)
        si=subprocess.STARTUPINFO()
        si.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        si.wShowWindow=1 if visible else 0
        owned[name]=subprocess.Popen(list(map(str,cmd)),cwd=ROOT,env=env,stdout=out,stderr=subprocess.STDOUT,
                                     startupinfo=si,creationflags=subprocess.CREATE_NO_WINDOW)
    def log(name):
        f=run/(name+".log")
        return f.read_text("utf-8",errors="replace") if f.exists() else ""
    def check():
        for name,p in owned.items():
            if p.poll() is not None:
                raise RuntimeError(f"{name} exited ({p.returncode}), see {run}")
    def wait(test,seconds,sample=None):
        end=time.monotonic()+seconds
        while time.monotonic()<end:
            if sample: sample()
            if test(): return
            check()
            time.sleep(.1)
        raise TimeoutError(f"Timed out waiting for component, see {run}")
    def ready(port,http=True):
        try:
            if http:
                with urllib.request.urlopen(f"http://127.0.0.1:{port}/health",timeout=.15) as r: return r.status==200
            with socket.create_connection(("127.0.0.1",port),timeout=.15): return True
        except OSError: return False
    def models():
        start("text",[CACHE/"llama/llama-server.exe","-m",CACHE/"text.gguf","--host","127.0.0.1","--port","18768",
                      "-c","512","-b","64","-ub","32","-np","1","-t","2","-ngl","0"])
        start("speech",[sys.executable,"-B","-u",ROOT/"village_backend/tea_voice.py","--models",CACHE])
    def player():
        start("player",[CACHE/"Player/TeaVoice.exe","-tea-voice-demo" if args.demo else "-tea-voice-benchmark",
              *([] if args.demo else ["-popupwindow"]),"-screen-fullscreen","0","-screen-width",args.width,"-screen-height",args.height,"-logFile",run/"player.log"],True)
    def memory():
        data={}
        for name,p in owned.items():
            try:
                proc=psutil.Process(p.pid)
                family=[proc]+proc.children(recursive=True)
            except psutil.NoSuchProcess: continue
            rss=private=peak=0
            for child in family:
                try: m=child.memory_info()
                except (psutil.NoSuchProcess,psutil.AccessDenied): continue
                rss+=m.rss
                private+=m.private
                peak+=m.peak_wset
            data[name]=(rss,private,peak)
        return data
    try:
        for port in ([18767,18768] if args.demo else [18766,18767,18768]):
            if ready(port,False): raise RuntimeError(f"Prototype port {port} already occupied")
        if args.demo:
            models()
            wait(lambda:ready(18768) and ready(18767),90)
            player()
            if owned["player"].wait(): raise RuntimeError("Player failed")
            return
        start("editor",[args.editor,"-projectPath",ROOT/"My project (1)","-logFile",run/"editor.log"])
        wait(lambda:"Application.AssetDatabase Initial Refresh End" in log("editor"),180)
        start("engine",[sys.executable,"-B","-u",args.engine,"--serve","--port","18766"])
        wait(lambda:ready(18766,False),30)
        player()
        wait(lambda:"TEA_BASELINE_READY" in log("player"),90)
        # An Editor can keep indexing after Initial Refresh End. Keep the first
        # cold-Editor result separately and use a fixed settling interval on repeats.
        for _ in range(args.settle_seconds):
            check()
            time.sleep(1)
        base_samples=[]
        for _ in range(24):
            check()
            base_samples.append(memory())
            time.sleep(.125)
        baseline={key:int(statistics.median(s[key][0] for s in base_samples)) for key in owned}
        peaks={}
        model_high={}
        max_delta=0
        samples=0
        def sample():
            nonlocal max_delta,samples
            m=memory()
            delta=0
            for key,(rss,private,high) in m.items():
                peaks[key]=max(peaks.get(key,0),rss)
                delta+=max(0,rss-baseline.get(key,0))
                if key in ("text","speech"):model_high[key]=max(model_high.get(key,0),high)
            max_delta=max(max_delta,delta)
            samples+=1
        models()
        wait(lambda:ready(18768) and ready(18767),90,sample)
        (run/"go").touch()
        wait(lambda:"TEA_BENCH_OK" in log("player"),160,sample)
        sample()
        result=dict(requested_resolution=[args.width,args.height],
                    capture_resolution=struct.unpack(">II",(run/"caption.png").read_bytes()[16:24]),
                    editor_settle_seconds=args.settle_seconds,text_scale=args.scale,
                    baseline_rss_bytes=baseline,peak_sampled_rss_bytes=peaks,
                    peak_extra_rss_bytes=max_delta,model_process_peak_working_sets=model_high,
                    samples=samples,limit_bytes=1_000_000_000,
                    conservative_extra_bytes=sum(max(0,rss-baseline[key]) for key,rss in peaks.items() if key in baseline)+sum(model_high.values()),
                    captions=[s for s in log("player").splitlines() if s.startswith(("TEA_CAPTION","TEA_BENCH_OK","TEA_FIRST_AUDIO_MS","TEA_FRAME_P95_MS"))],
                    inference=[s for s in (run/"speech.out").read_text(errors="replace").splitlines() if s.startswith("TEXT_INFERENCE")])
        result["under_budget"]=result["peak_extra_rss_bytes"]<result["limit_bytes"]
        if not result["inference"]:raise RuntimeError("Text inference missing")
        (run/"result.json").write_text(json.dumps(result,indent=2),"utf-8")
        print(json.dumps(result),flush=True)
        owned["player"].wait(timeout=20)
    finally:
        for name,p in reversed(list(owned.items())):
            if p.poll() is None:
                if name=="editor":
                    # Ask the isolated Editor to close normally on Windows.
                    subprocess.run(["taskkill","/PID",str(p.pid)],capture_output=True)
                    try:p.wait(timeout=10)
                    except subprocess.TimeoutExpired:pass
                if p.poll() is None:p.terminate()
                p.wait(timeout=15)
        for out in logs:out.close()

if __name__=="__main__":
    main()
