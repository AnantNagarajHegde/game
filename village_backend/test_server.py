import json
import unittest

from server import evaluate


def request():
    return {"version": 1, "request_id": "a" * 32, "turn_id": "hello", "speaker": "Manjunatha", "role": "Tea vendor",
            "learner_text": "Namaskara nanna hesaru Amogh", "objective": "Greet and introduce yourself",
            "npc_roman": "Namaskara", "npc_english": "Hello", "history": [],
            "choices": [{"roman": "Namaskara", "english": "Hello"}, {"roman": "", "english": "Help"}]}


class VillageBackendTests(unittest.TestCase):
    def test_reply_is_correlated_and_scene_is_grounded(self):
        seen = {}
        def model(system, scene):
            seen.update(json.loads(scene))
            return {"roman": "Namaskara", "english": "Welcome, Amogh.", "choice": 0, "feedback": "You introduced yourself."}
        response = evaluate(request(), model)
        self.assertEqual(response["request_id"], "a" * 32)
        self.assertEqual(response["turn_id"], "hello")
        self.assertEqual(response["choice"], 0)
        self.assertIn("tea stall", seen["character_description"])
        self.assertEqual(seen["player_message"], "Namaskara nanna hesaru Amogh")

    def test_off_task_reply_does_not_need_a_choice(self):
        response = evaluate(request(), lambda *_: {"roman": "", "english": "I don't know.", "choice": -1, "feedback": "Try an introduction."})
        self.assertEqual(response["choice"], -1)

    def test_invalid_or_extra_model_actions_are_rejected(self):
        for value in (True, 2, -2, "0"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                evaluate(request(), lambda *_, value=value: {"roman": "", "english": "Hello", "choice": value, "feedback": ""})
        with self.assertRaises(ValueError):
            evaluate(request(), lambda *_: {"roman": "", "english": "Hello", "choice": 0, "feedback": "", "effect": "unlock"})

    def test_invalid_request_never_reaches_model(self):
        data = request()
        data["learner_text"] = "x" * 513
        with self.assertRaises(ValueError):
            evaluate(data, lambda *_: self.fail("model called"))

    def test_history_is_bounded(self):
        data = request()
        data["history"] = [{"player": "hi", "npc": "hello"}] * 7
        with self.assertRaises(ValueError):
            evaluate(data, lambda *_: self.fail("model called"))


if __name__ == "__main__":
    unittest.main()
