import math
import json
import io
import unittest
from email.message import Message
from types import SimpleNamespace
from unittest.mock import patch

from server import ASSESS_SCHEMA, MANJUNATHA_ASSESS_SCHEMA, MANJUNATHA_VERIFY_SCHEMA, REPLY_SCHEMA, ROUTE, SPEECH_ROUTE, HEALTH_ROUTE, VERIFY_SCHEMA, azure_speech, evaluate, make_handler, openrouter_generate, groq_generate


def request(message="Namaskara nanna hesaru Amogh"):
    return {"version": 2, "request_id": "a" * 32, "event": "message", "turn_id": "hello", "speaker": "Manjunatha",
            "role": "Tea vendor", "learner_text": message, "objective": "Greet and introduce yourself by name",
            "scene_fact": "Ask the visitor their name.", "history": [], "actions": [
                {"id": "a0", "description": "Greet and introduce yourself by name.", "next_fact": "Ask the visitor whether they want tea or water."},
                {"id": "a1", "description": "Explicitly request language help.", "next_fact": "Speak slowly."}]}


def model(assessment, verify=True, scenes=None, language="kannada_or_mixed", understands=True):
    def generate(system, scene, schema):
        if scenes is not None:
            scenes.append((schema, scene))
        if schema is MANJUNATHA_ASSESS_SCHEMA:
            return {**assessment, "input_language": language}
        if schema is ASSESS_SCHEMA:
            return assessment
        if schema is VERIFY_SCHEMA:
            return {"matches": verify}
        if schema is MANJUNATHA_VERIFY_SCHEMA:
            return {"matches": verify, "understands_action": understands}
        return {"kannada": "ನಮಸ್ಕಾರ", "roman": "Namaskara", "english": "Welcome to the tea stall."}
    return generate


class VillageBackendTests(unittest.TestCase):
    def test_introduction_uses_verified_action_and_reveals_only_selected_fact(self):
        seen = []
        response = evaluate(request(), model({"action_id": "a0", "evidence": "hesaru Amogh", "confidence": .98}, scenes=seen))
        self.assertEqual(response["action_id"], "a0")
        self.assertEqual(response["request_id"], "a" * 32)
        self.assertEqual(response["turn_id"], "hello")
        self.assertEqual(len(seen), 3)
        self.assertNotIn("next_fact", str(seen[0][1]))
        self.assertEqual(seen[-1][1]["newly_available_fact"], "Ask the visitor whether they want tea or water.")

    def test_tea_question_can_reply_without_advancing_introduction(self):
        seen = []
        response = evaluate(request("chaha beku"), model({"action_id": "none", "evidence": "", "confidence": .4}, scenes=seen))
        self.assertEqual(response["action_id"], "none")
        self.assertTrue(response["english"])
        self.assertEqual(seen[-1][1]["newly_available_fact"], "")

    def test_english_only_does_not_advance_and_reaches_reply_as_ununderstood(self):
        seen = []
        response = evaluate(request("Hello, my name is Amogh"),
                            model({"action_id": "a0", "evidence": "my name is Amogh", "confidence": .99},
                                  scenes=seen, language="english_only"))
        self.assertEqual(response["action_id"], "none")
        self.assertEqual(seen[-1][1]["input_language"], "english_only")
        self.assertEqual(seen[-1][1]["newly_available_fact"], "")
        self.assertTrue(response["english"])

    def test_mixed_speech_can_complete_a_task(self):
        response = evaluate(request("Hello, nanna hesaru Amogh"),
                            model({"action_id": "a0", "evidence": "nanna hesaru Amogh", "confidence": .99}))
        self.assertEqual(response["action_id"], "a0")

    def test_uncertain_language_does_not_advance_manjunatha(self):
        response = evaluate(request(), model({"action_id": "a0", "evidence": "hesaru Amogh", "confidence": .99},
                                             language="uncertain"))
        self.assertEqual(response["action_id"], "none")

    def test_greeting_does_not_bypass_english_comprehension(self):
        seen = []
        response = evaluate(request("Namaskara, my name is Amogh"),
                            model({"action_id": "a0", "evidence": "my name is Amogh", "confidence": .99},
                                  scenes=seen, understands=False))
        self.assertEqual(response["action_id"], "none")
        self.assertEqual(seen[-1][1]["newly_available_fact"], "")
        self.assertEqual(seen[-1][1]["accepted_action"], "none")

    def test_manjunath_alias_keeps_language_policy(self):
        data = request("Hello, my name is Amogh")
        data["speaker"] = " Manjunath "
        response = evaluate(data, model({"action_id": "a0", "evidence": "Amogh", "confidence": .99},
                                        language="english_only"))
        self.assertEqual(response["action_id"], "none")

    def test_malformed_comprehension_verifier_cannot_advance(self):
        for understands in (None, "true", 1):
            with self.subTest(understands=understands):
                response = evaluate(request(), model({"action_id": "a0", "evidence": "hesaru Amogh", "confidence": .99},
                                                     understands=understands))
                self.assertEqual(response["action_id"], "none")

    def test_other_npcs_keep_existing_language_policy(self):
        data = request("Hello, my name is Amogh")
        data["speaker"] = "Somanna"
        response = evaluate(data, model({"action_id": "a0", "evidence": "my name is Amogh", "confidence": .99}))
        self.assertEqual(response["action_id"], "a0")

    def test_verifier_rejects_wrong_help_action(self):
        response = evaluate(request("chaha beku"), model({"action_id": "a1", "evidence": "chaha beku", "confidence": .99}, verify=False))
        self.assertEqual(response["action_id"], "none")

    def test_fabricated_evidence_or_low_confidence_fails_closed(self):
        for evidence, confidence in (("not in input", .99), ("hesaru Amogh", .3), ("", .99)):
            with self.subTest(evidence=evidence, confidence=confidence):
                response = evaluate(request(), model({"action_id": "a0", "evidence": evidence, "confidence": confidence}))
                self.assertEqual(response["action_id"], "none")

    def test_invalid_or_unknown_actions_are_rejected(self):
        for candidate, confidence in (("a2", .99), ("a0", True), ("a0", math.nan), ("a0", 1.1)):
            with self.subTest(candidate=candidate, confidence=confidence):
                response = evaluate(request(), model({"action_id": candidate, "evidence": "Amogh", "confidence": confidence}))
                self.assertEqual(response["action_id"], "none")
                self.assertTrue(response["english"])

    def test_opening_never_assesses_or_transitions(self):
        data = request("")
        data["event"] = "open"
        seen = []
        response = evaluate(data, model(None, scenes=seen))
        self.assertEqual(response["action_id"], "none")
        self.assertEqual(len(seen), 1)
        self.assertIs(seen[0][0], REPLY_SCHEMA)

    def test_readable_reply_does_not_require_english_translation(self):
        data = request("")
        data["event"] = "open"
        for language in ("kannada", "roman"):
            with self.subTest(language=language):
                line = {"kannada": "", "roman": "", "english": ""}
                line[language] = "Namaskara"
                reply = evaluate(data, lambda *_: line)
                self.assertEqual(reply[language], "Namaskara")
                self.assertEqual(reply["action_id"], "none")
        with self.assertRaises(ValueError):
            evaluate(data, lambda *_: {"kannada": "", "roman": " ", "english": ""})

    def test_rejects_invalid_request_before_model(self):
        data = request("x" * 513)
        with self.assertRaises(ValueError):
            evaluate(data, lambda *_: self.fail("model called"))
        data = request()
        data["history"] = [{"player": "hi", "npc": "hello"}] * 7
        with self.assertRaises(ValueError):
            evaluate(data, lambda *_: self.fail("model called"))

    def test_malformed_verifier_and_extra_model_fields_rejected(self):
        def invalid_verifier(system, scene, schema):
            if schema is MANJUNATHA_ASSESS_SCHEMA:
                return {"action_id": "a0", "evidence": "Amogh", "confidence": 1.0, "input_language": "kannada_or_mixed"}
            if schema is MANJUNATHA_VERIFY_SCHEMA:
                return {"matches": "yes", "understands_action": True}
            return {"kannada": "", "roman": "", "english": "Hello"}
        self.assertEqual(evaluate(request(), invalid_verifier)["action_id"], "none")
        def extra_field(system, scene, schema):
            return {"kannada": "", "roman": "", "english": "Hi", "effect": "unlock"}
        data = request("")
        data["event"] = "open"
        with self.assertRaises(ValueError):
            evaluate(data, extra_field)

    def test_remote_model_request_uses_structured_output(self):
        class Response:
            def __enter__(self): return self
            def __exit__(self, *_): pass
            def read(self, _):
                return json.dumps({"choices": [{"message": {"content": '{"matches":true}'}}]}).encode()
        captured = []
        def urlopen(req, timeout):
            captured.append((req, timeout))
            return Response()
        with patch("server.urllib.request.urlopen", urlopen):
            result = openrouter_generate("example/model", "test-key", "system", {"message": "hello"}, VERIFY_SCHEMA)
        payload = json.loads(captured[0][0].data)
        self.assertEqual(result, {"matches": True})
        self.assertEqual(payload["model"], "example/model")
        self.assertEqual(captured[0][0].get_header("User-agent"), "Paridhwani/2.0")
        self.assertEqual(payload["response_format"]["type"], "json_schema")
        self.assertTrue(payload["provider"]["require_parameters"])

    def test_groq_request_uses_strict_schema_and_provider_endpoint(self):
        class Response:
            def __enter__(self): return self
            def __exit__(self, *_): pass
            def read(self, _):
                return json.dumps({"choices": [{"message": {"content": '{"matches":true}'}}]}).encode()
        captured = []
        def urlopen(req, timeout):
            captured.append((req, timeout))
            return Response()
        with patch("server.urllib.request.urlopen", urlopen):
            result = groq_generate("qwen/qwen3.8-27b", "synthetic-key", "system", {"message": "hello"}, VERIFY_SCHEMA)
        request_object, timeout = captured[0]
        payload = json.loads(request_object.data)
        self.assertEqual(result, {"matches": True})
        self.assertEqual(request_object.full_url, "https://api.groq.com/openai/v1/chat/completions")
        self.assertEqual(request_object.get_header("Authorization"), "Bearer synthetic-key")
        self.assertEqual(request_object.get_header("User-agent"), "Paridhwani/2.0")
        self.assertEqual(payload["model"], "qwen/qwen3.8-27b")
        self.assertTrue(payload["response_format"]["json_schema"]["strict"])
        self.assertEqual(payload["response_format"]["json_schema"]["schema"], VERIFY_SCHEMA)
        self.assertNotIn("provider", payload)
        self.assertEqual(timeout, 20)

    def test_speech_escapes_text_and_uses_kannada_voice(self):
        class Response:
            def __enter__(self): return self
            def __exit__(self, *_): pass
            def read(self, _): return b"RIFF" + b"1234" + b"WAVE" + bytes(32)
        captured = []
        def urlopen(req, timeout):
            captured.append(req)
            return Response()
        with patch("server.urllib.request.urlopen", urlopen):
            audio = azure_speech({"speaker": "Manjunatha", "language": "kn-IN", "text": "a & b < c"}, "key", "centralindia")
        self.assertEqual(audio[:4], b"RIFF")
        self.assertIn("kn-IN-GaganNeural", captured[0].data.decode())
        self.assertIn("a &amp; b &lt; c", captured[0].data.decode())
        with self.assertRaises(OSError):
            azure_speech({"speaker": "Manjunatha", "language": "kn-IN", "text": "hi"}, "", "")

    def test_chat_and_speech_http_handlers_without_sockets(self):
        def invoke(path, obj):
            data = json.dumps(obj).encode()
            handler = object.__new__(make_handler("fake"))
            handler.path = path
            handler.connection = SimpleNamespace(settimeout=lambda _: None)
            handler.headers = Message()
            handler.headers.add_header("Content-Length", str(len(data)))
            handler.rfile, handler.wfile = io.BytesIO(data), io.BytesIO()
            status, headers = [], {}
            handler.send_response = status.append
            handler.send_header = lambda name, value: headers.update({name: value})
            handler.end_headers = lambda: None
            handler.do_POST()
            return status[0], headers, handler.wfile.getvalue()

        def generate(_model, _system, _scene, schema, timeout=20):
            if schema is MANJUNATHA_ASSESS_SCHEMA:
                return {"action_id": "a0", "evidence": "my name is Amogh", "confidence": .99, "input_language": "english_only"}
            if schema is REPLY_SCHEMA:
                return {"kannada": "ನಮಸ್ಕಾರ", "roman": "Namaskara", "english": "Sorry, I do not understand."}
            self.fail("English-only input must not reach verification")

        audio = b"RIFF1234WAVE" + bytes(32)
        with patch("server.ollama_generate", generate), patch("server.azure_speech", return_value=audio):
            status, headers, body = invoke(ROUTE, request("Hello, my name is Amogh"))
            self.assertEqual(status, 200)
            self.assertEqual(json.loads(body)["action_id"], "none")
            status, headers, body = invoke(SPEECH_ROUTE, {"speaker": "Manjunatha", "language": "kn-IN", "text": "ನಮಸ್ಕಾರ"})
            self.assertEqual((status, headers["Content-Type"], body), (200, "audio/wav", audio))


if __name__ == "__main__":
    unittest.main()
