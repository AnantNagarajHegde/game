"""Voice checks with a fake Sarvam: cache-first (offline), per-speaker voice and pace, Indian English, warm-up list."""
import base64
import json
import tempfile
import unittest
from unittest import mock

import server

WAV = b"RIFF" + (36).to_bytes(4, "little") + b"WAVEfmt " + bytes(24) + b"data" + bytes(4)


class FakeResponse:
    def __init__(self, payload):
        self.payload = payload

    def read(self, *_):
        return json.dumps({"audios": [base64.b64encode(WAV).decode()]}).encode()

    def __enter__(self):
        return self

    def __exit__(self, *_):
        return False


class SpeechCache(unittest.TestCase):
    def setUp(self):
        self.cache = tempfile.mkdtemp()
        self.sent = []

    def fake(self, request, timeout=None):
        self.sent.append(json.loads(request.data))
        return FakeResponse(self.sent[-1])

    def speak(self, speaker, text="ನಿಮಗೆ ಏನು ಬೇಕು?", language="kn-IN", key="k"):
        return server.sarvam_speech({"speaker": speaker, "language": language, "text": text}, key, self.cache)

    def test_cache_first_then_offline(self):
        with mock.patch.object(server.urllib.request, "urlopen", self.fake):
            self.assertEqual(self.speak("Manjunatha"), WAV)
            self.assertEqual(self.speak("Manjunatha"), WAV)
        self.assertEqual(len(self.sent), 1, "second request must come from the cache")
        self.assertEqual(self.speak("Manjunatha", key=""), WAV, "cached line must play without a key")
        with self.assertRaises(OSError):
            self.speak("Manjunatha", text="ಹೊಸ ಸಾಲು", key="")

    def test_voices_and_pace(self):
        with mock.patch.object(server.urllib.request, "urlopen", self.fake):
            self.speak("Manjunatha")
            self.speak("Somanna")
            self.speak("notebook_slow", text="ಚಹಾ")
        manjunatha, somanna, slow = self.sent
        self.assertNotIn("pace", manjunatha)
        self.assertEqual(somanna["pace"], server.sarvam_voice("somanna")[1])
        self.assertLess(slow["pace"], 1.0)
        self.assertNotEqual(server.sarvam_voice("notebook")[1], server.sarvam_voice("notebook_slow")[1])

    def test_indian_english_and_bad_language(self):
        with mock.patch.object(server.urllib.request, "urlopen", self.fake):
            self.speak("Manjunatha", text="Hello", language="en-IN")
        self.assertEqual(self.sent[-1]["language_code"], "en-IN")
        with self.assertRaises(ValueError):
            self.speak("Manjunatha", language="hi-IN")


class WarmUpList(unittest.TestCase):
    def test_collects_game_lines_and_skips_silent(self):
        import curriculum_engine as ce
        import warm_tts_cache as warm
        items = warm.collect(ce.CurriculumEngine(live=False))
        texts = {(speaker, text) for speaker, _, text in items}
        self.assertIn(("Manjunatha", "ನಿಮಗೆ ಏನು ಬೇಕು?"), texts)
        self.assertIn(("Shanthakka", "ಇಲ್ಲ, ಐವತ್ತು!"), texts)
        self.assertIn(("notebook_slow", "ಚಹಾ"), texts)
        self.assertFalse(any("·" in text for _, text in texts), "silent word-card lines must not be recorded")
        self.assertEqual(len(items), len({(s.casefold(), l, t) for s, l, t in items}))


if __name__ == "__main__":
    unittest.main()
