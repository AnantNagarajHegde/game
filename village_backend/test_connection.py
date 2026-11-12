"""Exercise the real HTTP handlers without sockets or provider credentials."""
import io
import json
import os
import socket
import unittest
import urllib.error
from email.message import Message
from types import SimpleNamespace
from unittest.mock import MagicMock, patch

import server


def invoke(handler_type, path, method="POST", obj=None, authorization=None):
    handler = object.__new__(handler_type)
    handler.path = path
    handler.connection = SimpleNamespace(settimeout=lambda _: None)
    handler.headers = Message()
    raw = json.dumps(obj or {}).encode()
    handler.headers.add_header("Content-Length", str(len(raw)))
    for value in authorization or []:
        handler.headers.add_header("Authorization", value)
    handler.rfile, handler.wfile = io.BytesIO(raw), io.BytesIO()
    status, headers = [], {}
    handler.send_response = status.append
    handler.send_header = lambda name, value: headers.update({name: value})
    handler.end_headers = lambda: None
    getattr(handler, "do_" + method)()
    return status[0], headers, handler.wfile.getvalue()


class ConnectionTests(unittest.TestCase):
    def setUp(self):
        registry = patch.object(server, "winreg", None)
        registry.start()
        self.addCleanup(registry.stop)

    def test_edge_block_is_not_reported_as_an_invalid_key(self):
        from test_server import request
        for body in (b"error code: 1010", b"<h1>Error 1010</h1> private-provider-detail"):
            with self.subTest(body=body), \
                    patch.object(server, "groq_generate", side_effect=urllib.error.HTTPError(
                        "https://api.groq.com/", 403, "Forbidden", {}, io.BytesIO(body))), \
                    patch("builtins.print") as log:
                status, _, response = invoke(server.make_handler("model", "groq"), server.ROUTE, obj=request())
                self.assertEqual((status, json.loads(response)["error"]), (503, "model_request_blocked"))
                self.assertNotIn("private-provider-detail", str(log.call_args_list) + response.decode())

    def test_plain_access_denial_remains_distinct_from_edge_block(self):
        for body in (None, io.BytesIO(b'{"error":{"code":"model_permission_blocked_project"}}')):
            error = urllib.error.HTTPError("https://api.groq.com/", 403, "Forbidden", {}, body)
            self.assertEqual(server.read_http_failure(error, "model"), "model_access_denied")

    def test_groq_health_checks_configuration_without_exposing_key(self):
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-private-key"}, clear=True), \
                patch.object(server, "groq_generate", side_effect=AssertionError("provider called")):
            status, _, body = invoke(server.make_handler("qwen/qwen3.8-27b", "groq"), server.HEALTH_ROUTE, "GET")
        self.assertEqual(status, 200)
        self.assertTrue(json.loads(body)["model_configured"])
        self.assertNotIn(b"synthetic-private-key", body)

    def test_groq_handler_routes_text_to_groq(self):
        from test_server import request
        def generate(model, key, system, scene, schema, timeout=server.MODEL_CALL_TIMEOUT):
            self.assertEqual(model, "qwen/qwen3.8-27b")
            self.assertEqual(key, "synthetic-key")
            if schema is server.MANJUNATHA_ASSESS_SCHEMA:
                return {"action_id": "none", "evidence": "", "confidence": 0.2, "input_language": "kannada_or_mixed"}
            if schema is server.REPLY_SCHEMA:
                return {"kannada": "", "roman": "Namaskara", "english": "Hello"}
            self.fail("unexpected provider request")
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-key"}, clear=True), \
                patch.object(server, "groq_generate", side_effect=generate):
            status, _, body = invoke(server.make_handler("qwen/qwen3.8-27b", "groq"), server.ROUTE, obj=request("chaha beku"))
        self.assertEqual(status, 200)
        self.assertEqual(json.loads(body)["action_id"], "none")
        self.assertEqual(json.loads(body)["english"], "Hello")

    def test_provider_failures_are_classified_without_leaking_sensitive_content(self):
        from test_server import request
        expected = {400: "model_request_rejected", 401: "model_authentication_failed",
                    403: "model_access_denied", 404: "model_resource_not_found",
                    429: "model_rate_limited", 500: "model_unavailable"}
        for code, failure in expected.items():
            with self.subTest(code=code), \
                    patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-private-key"}, clear=True), \
                    patch.object(server, "groq_generate", side_effect=urllib.error.HTTPError(
                        "https://example.invalid/secret", code, "sensitive-provider-detail", {}, io.BytesIO(b"sensitive-response"))), \
                    patch("builtins.print") as log:
                status, _, body = invoke(server.make_handler("qwen/qwen3.8-27b", "groq"), server.ROUTE,
                                          obj=request("private-player-message"))
                self.assertEqual(status, 503)
                self.assertEqual(json.loads(body), {"error": failure})
                output = str(log.call_args_list) + body.decode()
                for secret in ("synthetic-private-key", "sensitive-provider-detail", "sensitive-response", "private-player-message"):
                    self.assertNotIn(secret, output)
                self.assertIn(f"HTTP {code}", output)

    def test_unreachable_provider_and_speech_failure_are_distinct(self):
        from test_server import request
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-key"}, clear=True), \
                patch.object(server, "groq_generate", side_effect=urllib.error.URLError("private-network-details")), \
                patch("builtins.print") as log:
            status, _, body = invoke(server.make_handler("qwen/qwen3.8-27b", "groq"), server.ROUTE, obj=request())
            self.assertEqual((status, json.loads(body)["error"]), (503, "model_unavailable"))
            self.assertNotIn("private-network-details", str(log.call_args_list))
        with patch.object(server, "azure_speech", side_effect=urllib.error.HTTPError(
                "https://example.invalid", 401, "private", {}, None)), patch("builtins.print"):
            status, _, body = invoke(server.make_handler("fake"), server.SPEECH_ROUTE,
                                     obj={"speaker": "Manjunatha", "language": "kn-IN", "text": "Hello"})
            self.assertEqual((status, json.loads(body)["error"]), (503, "speech_authentication_failed"))

    def test_model_budget_is_shared_between_calls_and_late_reply_is_rejected(self):
        from test_server import request
        clock = [0.0]
        observed = []
        def generate(model, key, system, scene, schema, timeout):
            observed.append(timeout)
            clock[0] += 19.0
            if schema is server.MANJUNATHA_ASSESS_SCHEMA:
                return {"action_id": "a0", "evidence": "hesaru Amogh", "confidence": .99, "input_language": "kannada_or_mixed"}
            if schema is server.MANJUNATHA_VERIFY_SCHEMA:
                return {"matches": True, "understands_action": True}
            return {"kannada": "", "roman": "Namaskara", "english": "Hello"}
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-key"}, clear=True), \
                patch.object(server.time, "monotonic", side_effect=lambda: clock[0]), \
                patch.object(server, "groq_generate", side_effect=generate), patch("builtins.print"):
            status, _, body = invoke(server.make_handler("qwen/qwen3.8-27b", "groq"), server.ROUTE, obj=request())
        self.assertEqual(observed, [20, 20, 7])
        self.assertEqual((status, json.loads(body)["error"]), (503, "model_timed_out"))

    def test_invalid_request_is_distinct_from_invalid_model_reply(self):
        from test_server import request
        with patch("builtins.print"), patch.object(server, "evaluate", side_effect=ValueError("private details")):
            status, _, body = invoke(server.make_handler("fake"), server.ROUTE, obj=request())
            self.assertEqual((status, json.loads(body)["error"]), (502, "invalid_model_response"))
            status, _, body = invoke(server.make_handler("fake"), server.ROUTE, obj={"wrong": "shape"})
            self.assertEqual((status, json.loads(body)["error"]), (400, "invalid_request"))

    def test_wrapped_provider_timeout_has_a_specific_safe_code(self):
        from test_server import request
        with patch.object(server, "ollama_generate", side_effect=urllib.error.URLError(TimeoutError("private detail"))), \
                patch("builtins.print") as log:
            status, _, body = invoke(server.make_handler("fake"), server.ROUTE, obj=request())
        self.assertEqual((status, json.loads(body)["error"]), (503, "model_timed_out"))
        self.assertNotIn("private detail", str(log.call_args_list))

    def test_health_never_invokes_providers_or_returns_keys(self):
        with patch.dict(os.environ, {"OPENROUTER_API_KEY": "private-model-key",
                                     "AZURE_SPEECH_KEY": "private-speech-key",
                                     "AZURE_SPEECH_REGION": "centralindia"}, clear=True), \
                patch.object(server, "openrouter_generate", side_effect=AssertionError("provider called")), \
                patch.object(server, "azure_speech", side_effect=AssertionError("speech called")):
            status, headers, data = invoke(server.make_handler("example/model", "openrouter"), server.HEALTH_ROUTE, "GET")
        self.assertEqual(status, 200)
        self.assertEqual(json.loads(data), {"version": 2, "provider": "openrouter", "model_configured": True,
                                           "speech_configured": True, "authentication_required": False})
        self.assertNotIn(b"private-", data)
        self.assertEqual(headers["Cache-Control"], "no-store")

    def test_missing_provider_setup_is_visible_without_inference(self):
        with patch.dict(os.environ, {}, clear=True):
            status, _, data = invoke(server.make_handler("", "openrouter"), server.HEALTH_ROUTE, "GET")
        self.assertEqual(status, 200)
        self.assertFalse(json.loads(data)["model_configured"])
        self.assertFalse(json.loads(data)["speech_configured"])

    def test_unauthorized_chat_and_speech_never_reach_provider(self):
        token = "private_demo_token_" + "a" * 32
        with patch.dict(os.environ, {"PARIDHWANI_SERVICE_TOKEN": token}, clear=True), \
                patch.object(server, "evaluate", side_effect=AssertionError("model called")), \
                patch.object(server, "azure_speech", side_effect=AssertionError("speech called")):
            handler = server.make_handler("example/model", "openrouter")
            for route in (server.ROUTE, server.SPEECH_ROUTE):
                for auth in ([], ["Bearer wrong"], ["Bearer " + token, "Bearer " + token]):
                    with self.subTest(route=route, auth_count=len(auth)):
                        status, _, _ = invoke(handler, route, authorization=auth)
                        self.assertEqual(status, 401)

    def test_authorized_request_reaches_existing_turn_handler(self):
        token = "a" * 48
        with patch.dict(os.environ, {"PARIDHWANI_SERVICE_TOKEN": token}, clear=True), \
                patch.object(server, "evaluate", return_value={"action_id": "none"}) as evaluate:
            from test_server import request
            status, _, data = invoke(server.make_handler("fake"), server.ROUTE,
                                     obj=request(), authorization=["Bearer " + token])
        self.assertEqual(status, 200)
        self.assertEqual(json.loads(data), {"action_id": "none"})
        self.assertEqual(evaluate.call_count, 1)

    def test_voice_failure_does_not_break_next_text_request(self):
        with patch.dict(os.environ, {}, clear=True), \
                patch.object(server, "azure_speech", side_effect=OSError("unavailable")), \
                patch.object(server, "evaluate", return_value={"action_id": "none"}):
            handler = server.make_handler("fake")
            self.assertEqual(invoke(handler, server.SPEECH_ROUTE)[0], 503)
            from test_server import request
            self.assertEqual(invoke(handler, server.ROUTE, obj=request())[0], 200)


class ProviderAccessTests(unittest.TestCase):
    def setUp(self):
        registry = patch.object(server, "winreg", None)
        registry.start()
        self.addCleanup(registry.stop)

    def response(self, payload):
        response = io.BytesIO(json.dumps(payload).encode())
        return patch.object(server.urllib.request, "urlopen", return_value=response)

    def test_groq_check_authenticates_without_generation_or_disclosing_account_data(self):
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-private-key"}, clear=True), \
                self.response({"data": [{"id": "model", "account_detail": "private-detail"}]}) as urlopen:
            result = server.check_provider_access("model", "groq")
        req = urlopen.call_args.args[0]
        self.assertEqual(req.get_method(), "GET")
        self.assertIsNone(req.data)
        self.assertEqual(req.full_url, "https://api.groq.com/openai/v1/models")
        self.assertEqual(req.get_header("Authorization"), "Bearer synthetic-private-key")
        self.assertEqual(req.get_header("User-agent"), "Paridhwani/2.0")
        self.assertEqual(urlopen.call_args.kwargs["timeout"], 10)
        self.assertTrue(result["ok"])
        self.assertTrue(result["credentials_accepted"])
        self.assertTrue(result["model_listed"])
        self.assertFalse(result["model_access_verified"])
        self.assertNotIn("private", json.dumps(result))

    def test_openrouter_uses_authenticated_key_endpoint_instead_of_public_model_list(self):
        with patch.dict(os.environ, {"OPENROUTER_API_KEY": "synthetic-key"}, clear=True), \
                self.response({"data": {"label": "private-label", "usage": 123}}) as urlopen:
            result = server.check_provider_access("example/model", "openrouter")
        self.assertEqual(urlopen.call_args.args[0].full_url, "https://openrouter.ai/api/v1/key")
        self.assertTrue(result["ok"])
        self.assertFalse(result["model_access_verified"])
        self.assertNotIn("private-label", json.dumps(result))
        self.assertNotIn("usage", result)

    def test_accepted_key_with_missing_groq_model_does_not_pass(self):
        with patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-key"}, clear=True), \
                self.response({"data": [{"id": "different-model"}]}):
            result = server.check_provider_access("model", "groq")
        self.assertTrue(result["credentials_accepted"])
        self.assertFalse(result["ok"])
        self.assertFalse(result["model_listed"])
        self.assertEqual(result["error"], "model_resource_not_found")

    def test_bad_keys_permissions_and_edge_blocks_have_different_results(self):
        cases = [(401, b"private-body", "model_authentication_failed"),
                 (403, b"private-body", "model_access_denied"),
                 (403, b"error code: 1010 private-body", "model_request_blocked"),
                 (429, b"private-body", "model_rate_limited")]
        for status, body, failure in cases:
            with self.subTest(status=status, failure=failure), \
                    patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-private-key"}, clear=True), \
                    patch.object(server.urllib.request, "urlopen", side_effect=urllib.error.HTTPError(
                        "https://example.invalid/private-url", status, "private-message", {}, io.BytesIO(body))):
                result = server.check_provider_access("model", "groq")
            self.assertEqual(result, {"provider": "groq", "ok": False, "error": failure, "http_status": status})
            self.assertNotIn("private", json.dumps(result))

    def test_dns_timeouts_and_other_network_failures_are_sanitized(self):
        for error, failure in ((socket.gaierror(11001, "private-dns-detail"), "provider_dns_failed"),
                               (TimeoutError("private-timeout"), "model_timed_out"),
                               (OSError("private-network-detail"), "model_unavailable")):
            for wrapped in (False, True):
                with self.subTest(failure=failure, wrapped=wrapped), \
                        patch.dict(os.environ, {"GROQ_API_KEY": "synthetic-key"}, clear=True), \
                        patch.object(server.urllib.request, "urlopen", side_effect=urllib.error.URLError(error) if wrapped else error):
                    result = server.check_provider_access("model", "groq")
                self.assertEqual(result["error"], failure)
                self.assertNotIn("private", json.dumps(result))

    def test_missing_configuration_does_not_contact_provider(self):
        for values, model in (({}, "model"), ({"GROQ_API_KEY": " "}, "model"), ({"GROQ_API_KEY": "key"}, "")):
            with self.subTest(values=values, model=model), patch.dict(os.environ, values, clear=True), \
                    patch.object(server.urllib.request, "urlopen", side_effect=AssertionError("provider called")):
                self.assertEqual(server.check_provider_access(model, "groq")["error"], "model_not_configured")

    def test_invalid_or_oversized_provider_data_is_not_accepted_or_echoed(self):
        for raw in (b"private-not-json", b'{"data":[null]}', b'{"data":"private-invalid-list"}', b"x" * (server.MAX_BODY + 1)):
            with self.subTest(size=len(raw)), patch.dict(os.environ, {"GROQ_API_KEY": "key"}, clear=True), \
                    patch.object(server.urllib.request, "urlopen", return_value=io.BytesIO(raw)):
                result = server.check_provider_access("model", "groq")
            self.assertEqual(result["error"], "invalid_model_response")
            self.assertNotIn("private", json.dumps(result))

    def test_cli_keeps_configuration_check_offline_and_sets_provider_check_exit_status(self):
        with patch.dict(os.environ, {}, clear=True), \
                patch("sys.argv", ["server.py", "--provider", "groq", "--check"]), \
                patch.object(server, "check_provider_access", side_effect=AssertionError("provider called")), \
                patch("builtins.print"):
            server.main()
        for ok, exit_code in ((True, 0), (False, 1)):
            with self.subTest(ok=ok), patch.dict(os.environ, {}, clear=True), \
                    patch("sys.argv", ["server.py", "--provider", "groq", "--check-provider"]), \
                    patch.object(server, "check_provider_access", return_value={"ok": ok}) as check, \
                    patch("builtins.print"), self.assertRaises(SystemExit) as exit_result:
                server.main()
            self.assertEqual(exit_result.exception.code, exit_code)
            self.assertEqual(check.call_args.args, ("qwen/qwen3.8-27b", "groq"))


class UserEnvironmentTests(unittest.TestCase):
    def test_saved_user_key_overrides_stale_terminal_in_provider_check(self):
        registry = MagicMock()
        registry.QueryValueEx.return_value = ("  saved-user-key  ", 1)
        response = io.BytesIO(b'{"data":[{"id":"model"}]}')
        with patch.object(server, "winreg", registry), \
                patch.dict(os.environ, {"GROQ_API_KEY": "stale-terminal-key"}, clear=True), \
                patch.object(server.urllib.request, "urlopen", return_value=response) as urlopen:
            result = server.check_provider_access("model", "groq")
        self.assertTrue(result["ok"])
        self.assertEqual(urlopen.call_args.args[0].get_header("Authorization"), "Bearer saved-user-key")
        registry.OpenKey.assert_called_with(registry.HKEY_CURRENT_USER, "Environment")
        registry.QueryValueEx.assert_called_with(registry.OpenKey.return_value.__enter__.return_value, "GROQ_API_KEY")
        self.assertNotIn("saved-user-key", json.dumps(result))

    def test_missing_empty_or_unreadable_user_value_falls_back_to_process(self):
        for value in ("", "   ", None, 123):
            registry = MagicMock()
            registry.QueryValueEx.return_value = (value, 1)
            with self.subTest(value=value), patch.object(server, "winreg", registry), \
                    patch.dict(os.environ, {"GROQ_API_KEY": "process-key"}, clear=True):
                self.assertEqual(server.provider_api_key("groq"), "process-key")
        for error in (FileNotFoundError(), PermissionError()):
            registry = MagicMock()
            registry.OpenKey.side_effect = error
            with self.subTest(error=type(error).__name__), patch.object(server, "winreg", registry), \
                    patch.dict(os.environ, {"GROQ_API_KEY": "process-key"}, clear=True):
                self.assertEqual(server.provider_api_key("groq"), "process-key")

    def test_without_windows_registry_process_environment_still_works(self):
        with patch.object(server, "winreg", None), \
                patch.dict(os.environ, {"GROQ_API_KEY": "groq-process-key", "OPENROUTER_API_KEY": "router-key"}, clear=True):
            self.assertEqual(server.provider_api_key("groq"), "groq-process-key")
            self.assertEqual(server.provider_api_key("openrouter"), "router-key")
            self.assertTrue(server.configuration_status("model", "groq")["model_configured"])
        with patch.object(server, "winreg", None), patch.dict(os.environ, {}, clear=True):
            self.assertFalse(server.configuration_status("model", "groq")["model_configured"])

    def test_other_providers_do_not_read_saved_groq_key(self):
        registry = MagicMock()
        registry.OpenKey.side_effect = AssertionError("Groq registry accessed")
        with patch.object(server, "winreg", registry), \
                patch.dict(os.environ, {"OPENROUTER_API_KEY": "router-key"}, clear=True):
            self.assertEqual(server.provider_api_key("openrouter"), "router-key")
            self.assertEqual(server.provider_api_key("ollama"), "")
            self.assertTrue(server.configuration_status("local-model", "ollama")["model_configured"])

    def test_default_startup_accepts_saved_user_key_without_process_key(self):
        registry = MagicMock()
        registry.QueryValueEx.return_value = ("saved-user-key", 1)
        with patch.object(server, "winreg", registry), patch.dict(os.environ, {}, clear=True), \
                patch("sys.argv", ["server.py"]), patch("builtins.print") as log, \
                patch.object(server, "ThreadingHTTPServer") as http_server:
            server.main()
        http_server.return_value.__enter__.return_value.serve_forever.assert_called_once()
        self.assertIn("provider=groq", str(log.call_args_list))
        self.assertNotIn("saved-user-key", str(log.call_args_list))

    def test_health_and_chat_use_the_key_captured_when_bridge_started(self):
        from test_server import request
        registry = MagicMock()
        registry.QueryValueEx.return_value = ("startup-user-key", 1)
        with patch.object(server, "winreg", registry), patch.dict(os.environ, {}, clear=True), \
                patch.object(server, "groq_generate", return_value={"matches": True}) as generate, \
                patch.object(server, "evaluate", side_effect=lambda obj, generate: generate("system", {}, server.VERIFY_SCHEMA)):
            handler = server.make_handler("model", "groq")
            registry.QueryValueEx.return_value = ("", 1)
            status, _, health = invoke(handler, server.HEALTH_ROUTE, "GET")
            self.assertEqual(status, 200)
            self.assertTrue(json.loads(health)["model_configured"])
            status, _, _ = invoke(handler, server.ROUTE, obj=request())
        self.assertEqual(status, 200)
        self.assertEqual(generate.call_args.args[1], "startup-user-key")
        self.assertNotIn("startup-user-key", health.decode())


if __name__ == "__main__":
    unittest.main()
