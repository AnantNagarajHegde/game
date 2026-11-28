"""Curriculum checks: run from game/village_backend with
    PARIDHWANI_LANGUAGE_ENGINE=<path to language-engine> python3 -m unittest test_curriculum -v
Every authored Phrase must pass the real matcher and map to its own choice.
"""
import json
import os
import sys
import threading
import unittest
import urllib.request
from http.server import ThreadingHTTPServer

import curriculum_engine as ce


def engine(live=False):
    return ce.CurriculumEngine(live=live)


class FlowStructure(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.e = engine()

    def test_ids_links_and_reachability(self):
        nodes = self.e.nodes
        self.assertEqual(len(nodes), len(self.e.flow["nodes"]), "duplicate node id")
        for node in nodes.values():
            self.assertIn(node["kind"], ("respond", "word", "story"))
            self.assertTrue(node["choices"], node["id"])
            keys = [c["key"] for c in node["choices"]]
            self.assertEqual(len(keys), len(set(keys)), node["id"])
            for choice in node["choices"]:
                self.assertTrue(choice["next"] == "close" or choice["next"] in nodes, (node["id"], choice["next"]))
                self.assertTrue(self.e.choice_text(node, choice), node["id"])
            line = self.e.line_for(node)
            self.assertTrue(line["kn"] or line["en"], node["id"])
        for npc in self.e.flow["npcs"].values():
            seen, todo = set(), [npc["start"]]
            while todo:
                current = todo.pop()
                if current in seen or current == "close":
                    continue
                seen.add(current)
                todo += [c["next"] for c in nodes[current]["choices"]]
            self.assertIn("close", {c["next"] for n in seen for c in nodes[n]["choices"]})
        reached = set()
        for npc in self.e.flow["npcs"].values():
            todo = [npc["start"]]
            while todo:
                current = todo.pop()
                if current == "close" or current in reached:
                    continue
                reached.add(current)
                todo += [c["next"] for c in nodes[current]["choices"]]
        self.assertEqual(reached, set(nodes), "unreachable nodes")

    def test_steps_exist_and_every_kept_quest_completes(self):
        removed = set(self.e.flow["removed_quests"])
        completes = {q for n in self.e.nodes.values() for c in n["choices"] for q in c.get("completes", [])}
        for node in self.e.nodes.values():
            if node["kind"] in ("respond", "word"):
                self.assertIn(node["step"], self.e.steps, node["id"])
                self.assertEqual(self.e.steps[node["step"]][0]["quest_id"], node["quest"])
        wanted = {q for q, lv in self.e.level_of.items() if lv <= 3} - removed
        self.assertEqual(completes, wanted)
        covered = {n["step"] for n in self.e.nodes.values() if n.get("step")}
        for quest_id in wanted:
            for step in self.e.quests[quest_id]["steps"]:
                self.assertIn(step["step_id"], covered, step["step_id"])

    def test_json_lines_are_used_word_for_word(self):
        for node in self.e.nodes.values():
            if node.get("line") == "json":
                step = self.e.steps[node["step"]][1]
                self.assertEqual(self.e.line_for(node)["kn"], step["npc_line"]["kannada"])


class PhrasesPassTheMatcher(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.e = engine()

    def ask(self, node, text, name="Arun", wrong=0):
        return self.e.turn({"version": 1, "node": node, "text": text, "player_name": name, "wrong_count": wrong})

    def test_every_phrase_maps_to_its_own_choice(self):
        for node in self.e.nodes.values():
            if node["kind"] != "respond":
                continue
            for choice in node["choices"]:
                text = self.e.choice_text(node, choice)
                for form in (text["ro"], text["kn"]):
                    result = self.ask(node["id"], form)
                    if choice.get("wrong"):
                        self.assertEqual(result["outcome"], "not_accepted", (node["id"], form))
                    else:
                        self.assertIn(result["outcome"], ("accepted", "accepted_with_correction"), (node["id"], form, result))
                        self.assertEqual(result["choice_key"], choice["key"], (node["id"], form))

    def test_every_authored_form_resolves(self):
        for node in self.e.nodes.values():
            if node["kind"] != "respond":
                continue
            step = self.e.steps[node["step"]][1]
            for answer in step.get("accepted", []):
                forms = [answer["kanglish"]] + [f for f in answer.get("also_accepted", []) if "{" not in f]
                forms += [c["kanglish"] for c in answer.get("accepted_with_correction", []) if "{" not in c["kanglish"]]
                for form in forms:
                    if "{player_name}" in form:
                        form = form.replace("{player_name}", "Arun")
                    result = self.ask(node["id"], form)
                    self.assertIn(result["outcome"], ("accepted", "accepted_with_correction"), (node["id"], form, result))
                    self.assertTrue(result["choice_key"], (node["id"], form))
            for error in step.get("anticipated_errors", []):
                result = self.ask(node["id"], error["kanglish"].replace("{player_name}", "Arun"))
                self.assertEqual(result["outcome"], "not_accepted", (node["id"], error["kanglish"]))
                self.assertTrue(result["counts_as_wrong"])
                self.assertTrue(result["reaction"]["kn"] or result["reaction"]["action"], error["kanglish"])
                self.assertNotEqual(result["source"], "live")

    def test_items_names_and_kannada_script(self):
        self.assertEqual(self.ask("m_order", "ನನಗೆ ನೀರು ಬೇಕು")["choice_key"], "water")
        coffee = self.ask("m_order", "Nanage kaapi beku")
        self.assertEqual((coffee["outcome"], coffee["counts_as_wrong"]), ("unavailable", False))
        self.assertIn("ಕಾಫಿ", coffee["reaction"]["kn"])
        self.assertEqual(self.ask("h_meal", "Haudu, neeru kodi")["choice_key"], "water")
        self.assertEqual(self.ask("h_meal", "idli kodi")["choice_key"], "meal")
        self.assertEqual(self.ask("m_name", "Nanna hesaru meera rao", name="")["player_name"], "Meera Rao")
        self.assertEqual(self.ask("m_name", "Naanu Kiran", name="")["player_name"], "Kiran")
        self.assertEqual(self.ask("s_who", "Namaskara, naanu hosa archivist", name="")["player_name"], "")

    def test_help_escalates_with_wrong_tries(self):
        first, second, third = (self.ask("m_where", "Library", wrong=w) for w in (0, 1, 2))
        self.assertEqual([r["help"] for r in (first, second, third)], ["none", "quick_hint", "full_example"])
        self.assertEqual(first["tip"], "")
        self.assertEqual(second["tip"], "Say the place first, then ellide?")


class LiveAndFallback(unittest.TestCase):
    def ask(self, e, text="asdf qwerty", wrong=0):
        return e.turn({"version": 1, "node": "m_order", "text": text, "player_name": "Arun", "wrong_count": wrong})

    def test_fallback_without_live(self):
        result = self.ask(engine(live=False))
        self.assertEqual(result["source"], "fallback")
        self.assertTrue(result["reaction"]["kn"])
        self.assertEqual(result["choice_key"], "")

    def test_live_reply_used_and_never_advances(self):
        seen = {}
        def fake(messages, budget):
            seen["messages"], seen["budget"] = messages, budget
            return {"kannada": "ಏನು? ಚಹಾ ಬೇಕಾ?", "roman": "Enu? Chaha beka?", "english": "What? Do you want tea?"}
        result = self.ask(engine(live=fake))
        self.assertEqual((result["source"], result["choice_key"]), ("live", ""))
        self.assertLessEqual(seen["budget"], 5)
        self.assertIn("Manjunatha", seen["messages"][0]["content"])

    def test_bad_or_failing_live_falls_back(self):
        for fake in (lambda m, b: {"kannada": "no kannada here"}, lambda m, b: None):
            self.assertEqual(self.ask(engine(live=fake))["source"], "fallback")
        def boom(messages, budget):
            raise TimeoutError("slow")
        self.assertEqual(self.ask(engine(live=boom))["source"], "fallback")

    def test_retries_once_then_uses_live(self):
        calls = []
        def flaky(messages, budget):
            calls.append(budget)
            return None if len(calls) == 1 else {"Kannada": "ಏನು? ಚಹಾ ಬೇಕಾ?", "Roman": "Enu? Chaha beka?", "English": "What? Tea?"}
        result = self.ask(engine(live=flaky))
        self.assertEqual((result["source"], len(calls)), ("live", 2))
        self.assertIn("ನಿಮಗೆ", engine().live_prompt(engine().nodes["m_order"], "manjunatha", "x", [])[0]["content"])

    def test_known_mistakes_never_call_live(self):
        def fail(messages, budget):
            raise AssertionError("live must not be called")
        e = engine(live=fail)
        for text in ("Nanage chaha beda", "I want tea", "Nimage chaha beku"):
            self.assertEqual(self.ask(e, text)["source"], "matcher")

    def test_parse_reply_formats_and_polite_fix(self):
        lines = ce.parse_reply("Kannada: ಸರಿ, ನಮಗೇನು ಬೇಕು?\nRoman: Sari, namagenu beku?\nEnglish: OK, what do we need?")
        fixed = ce.validate_live(lines)
        self.assertEqual(fixed["kn"], "ಸರಿ, ನಿಮಗೇನು ಬೇಕು?")
        self.assertEqual(fixed["ro"], "Sari, nimagenu beku?")
        self.assertEqual(fixed["en"], "OK, what do you need?")
        only = ce.validate_live(ce.parse_reply("Here you go: ನಿಮಗೆ ಚಹಾ ಬೇಕಾ?"))
        self.assertEqual(only["kn"], "ನಿಮಗೆ ಚಹಾ ಬೇಕಾ?")
        self.assertIsNone(ce.parse_reply("no kannada at all"))

    def test_parse_json_object(self):
        self.assertEqual(ce.parse_json_object('```json\n{"kannada":"ಸರಿ"}\n```'), {"kannada": "ಸರಿ"})
        self.assertIsNone(ce.parse_json_object("no json"))


class GeneratedUnityData(unittest.TestCase):
    def test_unity_data_is_up_to_date(self):
        sys.path.insert(0, os.path.join(os.path.dirname(__file__), "tools"))
        import gen_curriculum_cs as gen
        current = gen.TARGET.read_text(encoding="utf-8") if gen.TARGET.exists() else ""
        self.assertEqual(current, gen.generate(engine()), "Run: python tools/gen_curriculum_cs.py")


class Validation(unittest.TestCase):
    def test_rejects_bad_requests(self):
        e = engine()
        for bad in ({}, {"version": 2}, {"version": 1, "node": "m_directions", "text": "x"},
                    {"version": 1, "node": "m_order", "text": ""}, {"version": 1, "node": "m_order", "text": "x" * 600},
                    {"version": 1, "node": "m_order", "text": "x", "wrong_count": -1}):
            with self.assertRaises(ValueError):
                e.turn(bad)


class HttpRoute(unittest.TestCase):
    def test_route_through_server(self):
        import server
        ce._ENGINE = engine(live=False)
        httpd = ThreadingHTTPServer(("127.0.0.1", 0), server.make_handler("test", "ollama"))
        thread = threading.Thread(target=httpd.serve_forever, daemon=True)
        thread.start()
        try:
            url = f"http://127.0.0.1:{httpd.server_address[1]}{ce.ROUTE}"
            body = json.dumps({"version": 1, "node": "m_order", "text": "Nanage chaha beku", "player_name": "", "wrong_count": 0}).encode()
            req = urllib.request.Request(url, data=body, headers={"Content-Type": "application/json"}, method="POST")
            with urllib.request.urlopen(req, timeout=10) as response:
                result = json.loads(response.read())
            self.assertEqual((result["outcome"], result["choice_key"]), ("accepted", "tea"))
            bad = urllib.request.Request(url, data=b'{"version":1}', headers={"Content-Type": "application/json"}, method="POST")
            with self.assertRaises(urllib.error.HTTPError) as caught:
                urllib.request.urlopen(bad, timeout=10)
            self.assertEqual(caught.exception.code, 400)
        finally:
            httpd.shutdown()
            ce._ENGINE = None


if __name__ == "__main__":
    unittest.main()
