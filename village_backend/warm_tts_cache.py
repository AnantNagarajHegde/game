"""Record every voiced line for levels 1 to 3 once, so the demo plays them from disk (no waiting, no internet).

    python warm_tts_cache.py --dry-run     # list what would be recorded, characters and cost; no key needed
    python warm_tts_cache.py               # record (needs SARVAM_API_KEY); already cached lines are skipped

It uses the same speaker names, text and voices as the game, so the cache keys match exactly.
Rerun it after changing flow.json, npc.json, voices.json or the curriculum JSON.
"""
import argparse
import os
import re
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import curriculum_engine as ce  # noqa: E402
import server  # noqa: E402

UI_FILE = HERE.parent / "My project (1)" / "Assets" / "_Paridhwani" / "Scripts" / "Runtime" / "TeaStallUI.cs"
RUPEES_PER_CHAR = 20 / 10000  # Bulbul v4 Flash: Rs 20 per 10,000 characters
KANNADA = re.compile(r"[\u0C80-\u0CFF]")


def cache_dir():
    return os.environ.get("PARIDHWANI_TTS_CACHE", str(HERE / ".tts-cache"))


def collect(engine):
    """Return [(speaker, language, text)] exactly as Unity will request them."""
    items = []

    def add(speaker, text):
        text = (text or "").strip()
        if text and KANNADA.search(text):
            items.append((speaker, "kn-IN", text))

    displays = {k: v["display"] for k, v in engine.flow["npcs"].items()}
    for node in engine.nodes.values():
        speaker = displays[node["npc"]]
        if not node.get("silent"):
            add(speaker, engine.line_for(node)["kn"])
        for choice in node["choices"]:
            if choice.get("wrong") and node["kind"] == "respond":
                text = engine.choice_text(node, choice)
                result = engine.turn({"version": 1, "node": node["id"], "text": text["ro"] or text["kn"],
                                      "player_name": "Arun", "wrong_count": 0})
                add(speaker, result["reaction"]["kn"])
        if node["kind"] != "respond":
            continue
        quest, step = engine.steps[node["step"]]
        reaction_ids = {e.get("reaction_id", "r_puzzled") for e in step.get("anticipated_errors", [])}
        reaction_ids |= {"r_puzzled"}
        for answer in step.get("accepted", []):
            for correction in answer.get("accepted_with_correction", []):
                if correction.get("reaction_id"):
                    reaction_ids.add(correction["reaction_id"])
        for rid in reaction_ids:
            add(speaker, engine.reactions.get(rid, {}).get("kannada", ""))
        resolve = node.get("resolve", {})
        if resolve.get("type") == "item":
            for wid in engine.item_forms.values():
                if wid not in resolve["map"]:
                    word = engine.words[wid]
                    add(speaker, resolve["unavailable"]["kn"].replace("{item_kn}", word["kannada"]))
    for npc, lines in engine.npc["fallbacks"].items():
        for line in lines:
            add(displays[npc], line["kn"])
    words = set()
    sys.path.insert(0, str(HERE / "tools"))
    import gen_curriculum_cs as gen
    for word in gen.notebook_words(engine):
        words.add(word["kannada"])
    if UI_FILE.exists():
        words |= set(re.findall(r'Word\(scroll,"([^"]+)"', UI_FILE.read_text(encoding="utf-8")))
    for word in sorted(words):
        add("notebook", word)
        add("notebook_slow", word)
    unique, seen = [], set()
    for item in items:
        key = (item[0].casefold(), item[1], item[2])
        if key not in seen:
            seen.add(key)
            unique.append(item)
    return unique


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    items = collect(ce.CurriculumEngine(live=False))
    chars = sum(len(text) for _, _, text in items)
    print(f"{len(items)} lines, {chars} characters, about Rs {chars * RUPEES_PER_CHAR:.2f} if none are cached yet")
    if args.dry_run:
        for speaker, _, text in items:
            print(f"  {speaker:14} {text}")
        return
    key = os.environ.get("SARVAM_API_KEY", "").strip()
    if not key:
        sys.exit("Set SARVAM_API_KEY first (export SARVAM_API_KEY=...).")
    model = os.environ.get("SARVAM_TTS_MODEL", "bulbul:v4-flash")
    done = failed = 0
    for speaker, language, text in items:
        try:
            server.sarvam_speech({"speaker": speaker, "language": language, "text": text}, key, cache_dir(), model)
            done += 1
            print(f"ok   {speaker:14} {text}")
        except Exception as error:  # keep going; report at the end
            failed += 1
            detail = error.read(300).decode("utf-8", "replace") if hasattr(error, "read") else str(error)
            print(f"FAIL {speaker:14} {text}  ->  {type(error).__name__}: {detail}")
    print(f"\n{done} recorded or already cached, {failed} failed. Cache folder: {cache_dir()}")
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
