"""Curriculum turn engine for chapters 1 to 3 (levels 1 to 3).

The matcher decides whether an answer is right. This module only adds what the
game needs around that decision: which authored choice an accepted answer maps
to, the NPC reaction, the tip, the help level, the player's name, and a short
live reply (or an authored fallback line) for input nobody anticipated.
The LLM never judges answers and never selects a choice.
"""
import json
import os
import re
import sys
import time
import unicodedata
import urllib.request
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROUTE = "/v2/curriculum/turn"
MAX_TEXT = 512
LIVE_BUDGET_SECONDS = float(os.environ.get("PARIDHWANI_LIVE_BUDGET", "4.5"))
SARVAM_CHAT_URL = os.environ.get("SARVAM_CHAT_URL", "https://api.sarvam.ai/v1/chat/completions")
SARVAM_CHAT_MODEL = os.environ.get("SARVAM_CHAT_MODEL", "sarvam-105b-conversations")
KANNADA = re.compile(r"[\u0C80-\u0CFF]")
EN_ITEM = {"w_chaha": "tea", "w_kaapi": "coffee", "w_neeru": "water", "w_haalu": "milk", "w_thindi": "snacks",
           "w_oota": "meals", "w_anna": "rice", "w_idli": "idli", "w_dose": "dosa", "w_chapati": "chapati",
           "w_rotti": "roti", "w_saaru": "rasam", "w_sambar": "sambar", "w_palya": "palya", "w_mosaru": "curd",
           "w_balehannu": "bananas"}


def default_language_engine():
    env = os.environ.get("PARIDHWANI_LANGUAGE_ENGINE")
    if env:
        return Path(env).resolve()
    # Master layout: <master>/game/village_backend and <master>/language-engine
    return (HERE.parent.parent / "language-engine").resolve()


class CurriculumError(ValueError):
    pass


class CurriculumEngine:
    def __init__(self, language_engine=None, flow_path=None, npc_path=None, live=None):
        root = Path(language_engine) if language_engine else default_language_engine()
        self.root = root
        src = root / "Paridhwani-NLP" / "src"
        if not (src / "matcher.py").exists():
            raise FileNotFoundError(f"Paridhwani-NLP not found under {root}. Set PARIDHWANI_LANGUAGE_ENGINE.")
        if str(src) not in sys.path:
            sys.path.insert(0, str(src))
        from matcher import AnswerMatcher  # noqa: E402  (path set above)
        content = root / "learning-design" / "content"
        self.matcher = AnswerMatcher(root / "Paridhwani-NLP" / "data", content)
        self.norm = self.matcher.policy.norm
        self.flow = json.loads(Path(flow_path or HERE / "curriculum" / "flow.json").read_text(encoding="utf-8"))
        self.npc = json.loads(Path(npc_path or HERE / "curriculum" / "npc.json").read_text(encoding="utf-8"))
        self.nodes = {n["id"]: n for n in self.flow["nodes"]}
        self.quests = {}
        for path in sorted((content / "quests").glob("*.json")):
            quest = json.loads(path.read_text(encoding="utf-8-sig"))
            self.quests[quest["quest_id"]] = quest
        self.steps = {s["step_id"]: (q, s) for q in self.quests.values() for s in q["steps"]}
        feedback = json.loads((content / "feedback.json").read_text(encoding="utf-8-sig"))
        self.reactions = {r["reaction_id"]: r for r in feedback["reactions"]}
        self.tips = {c["correction_type"]: c["tip"] for c in feedback["correction_types"]}
        self.words = {w["word_id"]: w for w in json.loads((content / "vocabulary.json").read_text(encoding="utf-8-sig"))["words"]}
        progression = json.loads((content / "progression.json").read_text(encoding="utf-8-sig"))
        self.help_rules = progression["help"]
        self.level_of = {q: lv["level"] for lv in progression["levels"] for q in lv["quests"]}
        self.item_forms = {}
        for wid in feedback["answer_rules"]["item_words"]:
            w = self.words[wid]
            for form in (w["kanglish"], w["kannada"], w["english"], EN_ITEM.get(wid, "")):
                if form:
                    self.item_forms[self.norm(form)] = wid
        self.item_forms.update({self.norm(f): "w_chaha" for f in ("cha", "chai", "tea")})
        self.live = live if live is not None else sarvam_live_reply
        # Kannada-script input: map whole words to Kanglish with the team's verified pairs
        # (translit_pairs.csv, 304 rows) plus the vocabulary, then let the matcher decide.
        self.kn_words = {}
        import csv
        with open(root / "Paridhwani-NLP" / "data" / "translit_pairs.csv", encoding="utf-8-sig") as handle:
            for row in csv.DictReader(handle):
                if row.get("type") == "word" and row.get("verified") == "yes":
                    self.kn_words.setdefault(row["kannada"].strip(), row["kanglish"].strip())
        for w in self.words.values():
            self.kn_words.setdefault(w["kannada"].strip(), w["kanglish"].strip())

    # ------------------------------------------------------------------ helpers
    def line_for(self, node):
        """Resolve a node's NPC line into {kn, ro, en}."""
        line = node.get("line")
        if isinstance(line, dict):
            return {k: line.get(k, "") for k in ("kn", "ro", "en")}
        quest, step = self.steps[node["step"]]
        if line == "json":
            src = step["npc_line"]
            return {"kn": src["kannada"], "ro": src["kanglish"], "en": "" if node.get("english_hidden") else src["english"]}
        if isinstance(line, str) and line.startswith("json_reply:"):
            src = self.steps[line.split(":", 1)[1]][1]["npc_reply"]
            return {"kn": src["kannada"], "ro": src["kanglish"], "en": src["english"]}
        if line == "json_text":
            src = step["text"]
            return {"kn": src["kannada"], "ro": "", "en": node.get("line_en", "")}
        raise CurriculumError(f"unknown line source for {node['id']}")

    def choice_text(self, node, choice):
        if "text" in choice:
            return {k: choice["text"].get(k, "") for k in ("kn", "ro", "en")}
        quest, step = self.steps[node["step"]]
        for answer in step.get("accepted", []):
            if answer["answer_id"] == choice["answer"]:
                return {"kn": answer["kannada"], "ro": answer["kanglish"], "en": answer["english"]}
        raise CurriculumError(f"answer {choice.get('answer')} missing in {node['step']}")

    def task_for(self, node):
        if node.get("kind") != "respond":
            return ""
        quest, step = self.steps[node["step"]]
        return step.get("task", "")

    def romanize(self, text):
        """Return Kanglish for Kannada-script text when every word is known, else ''."""
        if not KANNADA.search(text):
            return ""
        words = re.sub(r"[.!?,;:\"'()\u2026]+", " ", text).split()
        out = []
        for word in words:
            if KANNADA.search(word):
                if word not in self.kn_words:
                    return ""
                out.append(self.kn_words[word])
            else:
                out.append(word)
        return " ".join(out)

    def find_item(self, text):
        tokens = self.norm(text).split()
        for token in tokens:
            if token in self.item_forms:
                return self.item_forms[token]
        return None

    def extract_name(self, text):
        raw = unicodedata.normalize("NFC", text.strip())
        raw = re.sub(r"[.!?,;:\"'()]+", " ", raw).strip()
        patterns = [r"(?i)\b(?:hesaru|hesru|hesaaru)\s+(.+)$", r"ಹೆಸರು\s+(.+)$",
                    r"(?i)^(?:namaskara\s+)?(?:naanu|nanu|naan)\s+(.+)$", r"^(?:ನಮಸ್ಕಾರ\s+)?ನಾನು\s+(.+)$"]
        name = raw
        for pattern in patterns:
            match = re.search(pattern, raw)
            if match:
                name = match.group(1)
                break
        name = " ".join(name.split())
        if not name or len(name) > 40 or any(ch.isdigit() for ch in name):
            return ""
        if not all(unicodedata.category(c)[0] in "LMZ" for c in name):
            return ""
        if self.norm(name) in {"hosa archivist", "archivist"}:
            return ""
        return " ".join(part[:1].upper() + part[1:] for part in name.split()) if name.isascii() else name

    def reaction_line(self, reaction_id, speaker):
        reaction = self.reactions.get(reaction_id) or self.reactions["r_puzzled"]
        return {"kn": reaction.get("kannada", ""), "ro": reaction.get("kanglish", ""),
                "en": reaction.get("english", ""), "action": reaction.get("action", ""), "speaker": speaker}

    def fallback_line(self, speaker, wrong_count):
        lines = self.npc["fallbacks"][speaker]
        line = lines[wrong_count % len(lines)]
        return {"kn": line["kn"], "ro": line["ro"], "en": line["en"], "action": "", "speaker": speaker}

    # ------------------------------------------------------------------ main entry
    def turn(self, request):
        if not isinstance(request, dict) or request.get("version") != 1:
            raise CurriculumError("version must be 1")
        node = self.nodes.get(request.get("node"))
        if not node or node.get("kind") != "respond":
            raise CurriculumError("unknown or non-typing node")
        text = request.get("text")
        if not isinstance(text, str) or not text.strip() or len(text) > MAX_TEXT:
            raise CurriculumError("text must be 1 to 512 characters")
        player_name = request.get("player_name") or ""
        wrong = request.get("wrong_count", 0)
        if not isinstance(player_name, str) or len(player_name) > 80 or not isinstance(wrong, int) or not 0 <= wrong <= 99:
            raise CurriculumError("invalid player_name or wrong_count")
        history = request.get("history") or []
        quest, step = self.steps[node["step"]]
        speaker = step.get("speaker") or node["npc"]
        matched = self.matcher.evaluate(node["quest"], node["step"], text, {"name": player_name})
        assessed_text = text
        if matched["outcome"] == "not_accepted":
            romanized = self.romanize(text)
            if romanized:
                second = self.matcher.evaluate(node["quest"], node["step"], romanized, {"name": player_name})
                if second["outcome"] != "not_accepted" or second["error_category"] != "unclear":
                    matched, assessed_text = {**second, "detected_form": matched.get("detected_form", "kannada")}, romanized
        result = {"version": 1, "node": node["id"], "outcome": matched["outcome"], "answer_id": matched["answer_id"],
                  "error_category": matched["error_category"], "correction_type": matched["correction_type"],
                  "detected_form": matched.get("detected_form", ""), "choice_key": "", "player_name": "",
                  "reaction": None, "tip": "", "help": "none", "quick_hint": quest.get("quick_hint", ""),
                  "full_example": step.get("full_example"), "source": "matcher", "counts_as_wrong": False}
        if matched["outcome"] in ("accepted", "accepted_with_correction"):
            resolve = node.get("resolve", {})
            if resolve.get("type") == "item":
                item = self.find_item(assessed_text)
                if item is None:
                    key = resolve.get("default", "")
                elif item in resolve["map"]:
                    key = resolve["map"][item]
                else:
                    word = self.words[item]
                    template = resolve["unavailable"]
                    line = {k: template[k].replace("{item_kn}", word["kannada"]).replace("{item_ro}", word["kanglish"].capitalize())
                                                .replace("{item_en}", EN_ITEM.get(item, word["english"].lower())) for k in ("kn", "ro", "en")}
                    result.update(choice_key="", reaction={**line, "action": "", "speaker": speaker}, outcome="unavailable")
                    return result
            else:
                key = resolve.get("map", {}).get(matched["answer_id"], "")
            if not key:
                raise CurriculumError(f"no choice for {matched['answer_id']} at {node['id']}")
            result["choice_key"] = key
            if matched["correction_type"]:
                result["tip"] = self.tips.get(matched["correction_type"], "")
            if matched.get("reaction_id"):
                result["reaction"] = self.reaction_line(matched["reaction_id"], speaker)
            if node.get("captures_name"):
                result["player_name"] = self.extract_name(text) if not player_name or matched["answer_id"] == "a_name" else ""
            return result
        # Not accepted: count the try, escalate help, react.
        result["counts_as_wrong"] = True
        tries = wrong + 1
        if tries >= self.help_rules["full_example_offered_after_wrong_answers"]:
            result["help"] = "full_example"
        elif tries >= self.help_rules["quick_hint_appears_after_wrong_answers"]:
            result["help"] = "quick_hint"
        if result["help"] != "none":
            result["tip"] = quest.get("quick_hint", "")
        if matched["answer_id"] == "" and matched["error_category"] == "unclear" and matched.get("reaction_id", "") in ("", "r_puzzled"):
            reply = None
            if self.live:
                messages = self.live_prompt(node, speaker, text, history)
                started = time.monotonic()
                for attempt in (1, 2):
                    remaining = LIVE_BUDGET_SECONDS - (time.monotonic() - started)
                    if remaining < 1.5:
                        break
                    reason = ""
                    try:
                        reply = validate_live(self.live(messages, remaining))
                        if reply is None:
                            reason = "reply missing Kannada or not JSON"
                    except Exception as error:  # live replies are optional; any failure uses the fallback line
                        reply, reason = None, type(error).__name__ + (f" {error.code}" if hasattr(error, "code") else "")
                    if reply:
                        break
                    print(f"Live reply attempt {attempt} failed: {reason}", flush=True)
            if reply:
                result.update(reaction={**reply, "action": "", "speaker": speaker}, source="live")
            else:
                result.update(reaction=self.fallback_line(speaker, wrong), source="fallback")
            return result
        result["reaction"] = self.reaction_line(matched.get("reaction_id") or "r_puzzled", speaker)
        return result

    def live_prompt(self, node, speaker, text, history):
        card = self.npc["cards"][speaker]
        quest, step = self.steps[node["step"]]
        level = self.level_of.get(node["quest"], 1)
        vocab = sorted({w["kanglish"] for w in self.words.values()
                        if self.level_of.get(w.get("first_quest", ""), 99) <= level and w.get("role") != "name"})
        current = self.line_for(node)
        system = (
            f"You are {card['who']} Speaking style: {card['style']} You know: {card['knows']} {card['never']}\n"
            "The player is a beginner learning Kannada. You asked them: "
            f"\"{current['kn']}\" ({current['en'] or 'task: ' + step.get('task', '')}).\n"
            "Their answer was not one you expected. Reply in character in ONE or TWO very short, simple sentences, "
            "using mostly these beginner words: " + ", ".join(vocab[:80]) + ". "
            "Always address the player politely as ನೀವು / ನಿಮಗೆ / ನಿಮ್ಮ (never ನಾವು / ನಮಗೆ). "
            "At most two short sentences. End by gently asking them again for what this moment needs. "
            "Never give the full answer.\n"
            "Return ONLY a JSON object with keys kannada (Kannada script), roman (Romanised Kannada), english (translation)."
        )
        messages = [{"role": "system", "content": system}]
        for exchange in history[-2:]:
            if isinstance(exchange, dict):
                messages.append({"role": "user", "content": str(exchange.get("player", ""))[:200]})
                messages.append({"role": "assistant", "content": str(exchange.get("npc", ""))[:200]})
        messages.append({"role": "user", "content": text[:MAX_TEXT]})
        return messages


def validate_live(reply):
    if not isinstance(reply, dict):
        return None
    lowered = {str(k).strip().casefold(): v for k, v in reply.items()}
    def pick(*names):
        for name in names:
            if isinstance(lowered.get(name), str) and lowered[name].strip():
                return lowered[name].strip()
        return ""
    kn = pick("kannada", "kn", "kannada_script", "reply")
    ro = pick("roman", "ro", "romanised", "romanized", "kanglish", "transliteration")
    en = pick("english", "en", "translation")
    if not kn or not KANNADA.search(kn) or len(kn) > 240 or len(ro) > 300 or len(en) > 300:
        return None
    return {"kn": kn, "ro": ro, "en": en}


def parse_json_object(text):
    text = text.strip()
    text = re.sub(r"^```(?:json)?\s*|\s*```$", "", text)
    start, end = text.find("{"), text.rfind("}")
    if start < 0 or end <= start:
        return None
    try:
        value = json.loads(text[start:end + 1])
    except ValueError:
        return None
    return value if isinstance(value, dict) else None


def sarvam_live_reply(messages, budget_seconds):
    key = os.environ.get("SARVAM_API_KEY", "").strip()
    if not key:
        return None
    payload = {"model": SARVAM_CHAT_MODEL, "messages": messages, "temperature": 0.3, "max_tokens": 400,
               "reasoning_effort": None}
    request = urllib.request.Request(SARVAM_CHAT_URL, data=json.dumps(payload, ensure_ascii=False).encode("utf-8"),
                                     headers={"api-subscription-key": key, "Authorization": "Bearer " + key,
                                              "Content-Type": "application/json", "User-Agent": "Paridhwani/2.0"},
                                     method="POST")
    with urllib.request.urlopen(request, timeout=budget_seconds) as response:
        envelope = json.loads(response.read(65536).decode("utf-8"))
    content = envelope["choices"][0]["message"].get("content") or ""
    return parse_json_object(content)


_ENGINE = None


def engine():
    global _ENGINE
    if _ENGINE is None:
        _ENGINE = CurriculumEngine()
    return _ENGINE


if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="Curriculum engine checks")
    parser.add_argument("--check-live", action="store_true", help="Send one unexpected answer to Sarvam and time it")
    parser.add_argument("--node", default="m_order")
    parser.add_argument("--text", default="Is it going to rain today?")
    args = parser.parse_args()
    started = time.monotonic()
    result = engine().turn({"version": 1, "node": args.node, "text": args.text, "player_name": "Arun", "wrong_count": 0})
    print(json.dumps(result, ensure_ascii=False, indent=1))
    print(f"elapsed: {time.monotonic() - started:.2f} s, source: {result['source']}")
