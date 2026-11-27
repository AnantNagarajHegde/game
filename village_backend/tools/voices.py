"""List and audition Bulbul v4 Flash voices for curriculum/voices.json.

    python tools/voices.py --list                 # every voice ID Sarvam accepts (Kannada ones first)
    python tools/voices.py --try shubh_kn_xxx     # writes sarvam-samples/<voice>.wav with a Kannada line
Needs SARVAM_API_KEY. --list costs nothing: it sends an invalid speaker and reads the error that lists valid ones.
"""
import argparse
import base64
import json
import os
import re
import sys
import urllib.error
import urllib.request

URL = "https://api.sarvam.ai/text-to-speech"
SAMPLE = "ನಮಸ್ಕಾರ! ನಿಮಗೆ ಏನು ಬೇಕು? ಚಹಾ ಬೇಕಾ, ನೀರು ಬೇಕಾ?"


def post(key, speaker, text, pace=1.0):
    body = {"text": text, "language_code": "kn-IN", "model": "bulbul:v4-flash", "speaker": speaker,
            "speech_sample_rate": 24000, "output_audio_codec": "wav"}
    if pace != 1.0:
        body["pace"] = pace
    req = urllib.request.Request(URL, data=json.dumps(body).encode(), method="POST",
                                 headers={"api-subscription-key": key, "Content-Type": "application/json"})
    return urllib.request.urlopen(req, timeout=30)


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--list", action="store_true")
    parser.add_argument("--try", dest="voice")
    parser.add_argument("--pace", type=float, default=1.0)
    parser.add_argument("--text", default=SAMPLE)
    args = parser.parse_args()
    key = os.environ.get("SARVAM_API_KEY", "").strip()
    if not key:
        sys.exit("Set SARVAM_API_KEY first.")
    if args.list:
        try:
            post(key, "zz_not_a_voice", "ಸರಿ")
            print("Sarvam accepted the test speaker; open the Voices page in the Sarvam docs instead.")
        except urllib.error.HTTPError as error:
            message = error.read(20000).decode("utf-8", "replace")
            ids = sorted(set(re.findall(r"\b[a-z]+_[a-z]+_[a-z]+\b", message)))
            kannada = [v for v in ids if "_kn_" in v]
            print("Kannada voices:", ", ".join(kannada) or "(none tagged kn; any voice can speak kn-IN)")
            print("All voices:", ", ".join(ids) if ids else message[:2000])
        return
    if args.voice:
        with post(key, args.voice, args.text, args.pace) as response:
            audio = base64.b64decode(json.loads(response.read())["audios"][0])
        os.makedirs("sarvam-samples", exist_ok=True)
        path = os.path.join("sarvam-samples", f"{args.voice}-{args.pace}.wav")
        with open(path, "wb") as handle:
            handle.write(audio)
        print("wrote", path, "(open it in Finder to listen)")
        return
    parser.print_help()


if __name__ == "__main__":
    main()
