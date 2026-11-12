"""Author Kannada narration once; the shipped movie plays entirely offline.

Install edge-tts in a local authoring environment. Only authored narration text is
sent to Microsoft's speech service. No game save, account or player data is used.
"""
import argparse
import asyncio
import hashlib
import json
import math
from pathlib import Path
import subprocess
import sys
import wave

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "verification/Temp/ArrivalMovie"
sys.path.insert(0, str(ROOT / "verification/Temp/ArrivalIntro/pythonlibs"))
import edge_tts


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", required=True)
    args = parser.parse_args()
    story = json.loads((ROOT / "verification/ArrivalMovie/storyboard.json").read_text(encoding="utf-8-sig"))
    CACHE.mkdir(parents=True, exist_ok=True)
    assets = ROOT / "My project (1)/Assets/_Paridhwani/Resources/ArrivalIntro"
    assets.mkdir(parents=True, exist_ok=True)
    start = 0.0
    cues = []
    for beat in story["beats"]:
        mp3 = CACHE / (beat["id"] + "-kn.mp3")
        await edge_tts.Communicate(beat["kannada"], story["voice"], rate=story["rate"]).save(str(mp3))
        wav = CACHE / (beat["id"] + "-kn.wav")
        subprocess.run([args.ffmpeg, "-hide_banner", "-loglevel", "error", "-y", "-i", str(mp3), "-ar", "24000", "-ac", "1", "-af", "afade=t=in:d=0.02", str(wav)], check=True)
        with wave.open(str(wav), "rb") as audio:
            length = audio.getnframes() / audio.getframerate()
        duration = math.ceil(max(beat["minimumSeconds"], length + 1.4) * story["fps"]) / story["fps"]
        cues.append(dict(id=beat["id"], start=start, end=start+duration, caption=beat["caption"], kannada=beat["kannada"], voiceSeconds=round(length, 3), sha256=hashlib.sha256(wav.read_bytes()).hexdigest()))
        print(f"{beat['id']}: {length:.2f}s Kannada / {duration:.2f}s scene", flush=True)
        start += duration
    timeline = dict(version=1, language="kn-IN", voice=story["voice"], rate=story["rate"], fps=story["fps"], width=story["width"], height=story["height"], duration=start+3, cues=cues)
    text = json.dumps(timeline, ensure_ascii=False, indent=2)
    (assets / "MovieTimeline.json").write_text(text, encoding="utf-8")
    (ROOT / "docs/ARRIVAL_NARRATION.json").write_text(text, encoding="utf-8")
    print(f"KANNADA_NARRATION_OK: {start:.2f}s total", flush=True)


if __name__ == "__main__":
    asyncio.run(main())
