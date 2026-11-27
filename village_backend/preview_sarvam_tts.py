"""Generate standalone Kannada voice samples from Sarvam Bulbul without Unity."""
import argparse
import json
import os
import sys
import urllib.error
import wave

from server import sarvam_speech


DEFAULT_TEXT = "ನಮಸ್ಕಾರ, ನೀವು ಹೇಗಿದ್ದೀರಿ? ನಮ್ಮ ಊರಿನ ಕಥೆಯನ್ನು ಕೇಳಲು ಸಿದ್ಧರಿದ್ದೀರಾ?"
SAMPLES = {
    "chetan": "chetan_kn_conversation",
    "chaitra": "chaitra_kn_conversation",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--text", default=DEFAULT_TEXT, help="Kannada script dialogue, up to 2500 characters")
    parser.add_argument("--voice", choices=("chetan", "chaitra", "both"), default="both")
    parser.add_argument("--output-dir", default="sarvam-samples")
    args = parser.parse_args()
    key = os.environ.get("SARVAM_API_KEY", "").strip()
    if not key:
        parser.error("Set SARVAM_API_KEY in this terminal before running the preview.")
    os.makedirs(args.output_dir, exist_ok=True)
    selected = SAMPLES if args.voice == "both" else {args.voice: SAMPLES[args.voice]}
    for name, voice in selected.items():
        audio = sarvam_speech({"speaker": name, "language": "kn-IN", "text": args.text}, key,
                              os.path.join(args.output_dir, ".cache"))
        path = os.path.abspath(os.path.join(args.output_dir, name + ".wav"))
        with open(path, "wb") as sample:
            sample.write(audio)
        with wave.open(path, "rb") as wav:
            duration = wav.getnframes() / wav.getframerate()
            print(f"{name}: {path} | {wav.getframerate()} Hz | {wav.getnchannels()} channel(s) | {duration:.2f} seconds")
    print("Listen to the WAV files in File Explorer with Windows Media Player or another audio player.")


if __name__ == "__main__":
    try:
        main()
    except urllib.error.HTTPError as exc:
        raw = exc.read(4096)
        try:
            detail = json.loads(raw.decode("utf-8"))
            error = detail.get("error", {}) if isinstance(detail, dict) else {}
            code = error.get("code", "") if isinstance(error, dict) else ""
            message = error.get("message", "") if isinstance(error, dict) else ""
        except (UnicodeError, ValueError):
            code, message = "", ""
        print(f"Sarvam returned HTTP {exc.code}.", file=sys.stderr)
        if code:
            print(f"Provider error code: {code}", file=sys.stderr)
        if message:
            print(f"Provider message: {message[:500]}", file=sys.stderr)
        if exc.code == 403 and code == "invalid_api_key_error":
            print("Check that SARVAM_API_KEY is the API subscription key generated in the Sarvam dashboard; do not use an account password or session token.", file=sys.stderr)
        elif exc.code == 403:
            print("The key was recognized but this request may not be enabled for the account/workspace. Check the account's TTS access and workspace.", file=sys.stderr)
        elif exc.code == 402:
            print("The account has no available API credits. Check Sarvam dashboard billing/credits.", file=sys.stderr)
        raise SystemExit(1)
    except Exception as exc:
        print(f"Sarvam preview failed: {type(exc).__name__}: {exc}", file=sys.stderr)
        raise SystemExit(1)
