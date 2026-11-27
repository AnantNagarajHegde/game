"""Measure the demo's response times against the targets. Start the backend first (./start-demo.sh).

    python time_demo.py            # matcher path and cached voice (no credit used)
    python time_demo.py --live     # also 3 live replies and 1 uncached voice line (uses a little credit)
"""
import argparse
import json
import os
import statistics
import time
import urllib.request

BASE = "http://127.0.0.1:8767"
INPUTS = [("m_order", "Nanage chaha beku"), ("m_order", "Nanage kaapi beku"), ("m_order", "Nanage chaha beda"),
          ("m_water", "Haudu, beku"), ("m_name", "Nanna hesaru Meera"), ("m_where", "Library ellide?"),
          ("m_where", "Library"), ("s_who", "Namaskara, naanu hosa archivist"), ("s_open", "Svalpa bagilu tegeyiri"),
          ("s_open", "Bagilu tegi"), ("s_key", "Ananya"), ("s_where", "Keeli ellide?"), ("h_meal", "Haudu, oota kodi"),
          ("h_more", "Saaku"), ("h_price", "Eshtu rupayi?"), ("h_pay", "Hattu"), ("h_pay", "Aivattu"),
          ("h_where", "Ananya elli?"), ("m_order", "ನನಗೆ ನೀರು ಬೇಕು"), ("s_open", "open the door")]


def post(path, body, timeout=15):
    started = time.monotonic()
    req = urllib.request.Request(BASE + path, data=json.dumps(body).encode("utf-8"), method="POST",
                                 headers={"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as response:
        data = response.read()
    return time.monotonic() - started, data


def report(name, seconds, target):
    seconds = sorted(seconds)
    median = statistics.median(seconds)
    worst = seconds[-1]
    verdict = "OK" if worst <= target else "SLOW"
    print(f"{name:34} median {median:6.3f} s   slowest {worst:6.3f} s   target {target:.1f} s   {verdict}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--live", action="store_true")
    args = parser.parse_args()
    turns = []
    for node, text in INPUTS:
        seconds, data = post("/v2/curriculum/turn", {"version": 1, "node": node, "text": text, "player_name": "Meera", "wrong_count": 0})
        turns.append(seconds)
        result = json.loads(data)
        if result["source"] == "live":
            print("note: a sample input reached the live path:", text)
    report("Matcher path (typed answer)", turns, 0.5)
    speech = []
    for _ in range(5):
        seconds, _ = post("/v2/village/speech", {"speaker": "Manjunatha", "language": "kn-IN", "text": "ನಿಮಗೆ ಏನು ಬೇಕು?"}, timeout=40)
        speech.append(seconds)
    report("Voice line from cache (after 1st)", speech[1:], 0.5)
    if args.live:
        live = []
        for text in ("Is it going to rain today?", "Do you like cricket?", "What is your favourite film?"):
            seconds, data = post("/v2/curriculum/turn", {"version": 1, "node": "m_order", "text": text, "player_name": "Meera", "wrong_count": 0})
            live.append(seconds)
            result = json.loads(data)
            print(f"  live '{text}' -> {result['source']}: {result['reaction']['kn']}")
        report("Live reply text (unclear input)", live, 5.0)
        seconds, _ = post("/v2/village/speech", {"speaker": "Manjunatha", "language": "kn-IN",
                                                 "text": f"ಸರಿ, ಇನ್ನೊಮ್ಮೆ ಹೇಳಿ. {int(time.time()) % 1000}"}, timeout=40)
        report("New voice line (not cached)", [seconds], 3.0)


if __name__ == "__main__":
    main()
