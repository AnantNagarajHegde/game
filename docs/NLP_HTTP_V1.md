# Unity / NLP HTTP v1 development boundary

Status: implemented local development contract, 28 October 2026. This is a concrete mock-compatible game-side API, not an assertion that the separate language engine already implements it. No backend, model, credentials or production endpoint is included. The authored game remains the default and works offline.

## Contract and ownership

`POST /v1/dialogue/respond`, UTF-8 JSON, `Content-Type: application/json; charset=utf-8`, `Accept: application/json`. Return HTTP 200 with one JSON object. The [machine-readable request and response schema](contracts/nlp-v1.schema.json) defines the envelope. Every listed field is required; additional or duplicate properties, null text, wrong types, trailing JSON, malformed UTF-8 and unsupported versions are rejected. No streaming or persistent socket is needed for a single optional turn response. Any backend/framework may implement the contract.

Request:

```json
{
  "version": 1,
  "request_id": "f30b6c852e104d28b8a3052effda6922",
  "turn_id": "hello",
  "speaker": "Manjunatha",
  "learner_text": "How do I greet you?",
  "authored_line": {"kn": "", "roman": "Namaskara.", "en": "Hello."},
  "available_replies": [{"kn": "", "roman": "Namaskara.", "en": "Hello."}]
}
```

Response:

```json
{
  "version": 1,
  "request_id": "f30b6c852e104d28b8a3052effda6922",
  "turn_id": "hello",
  "line": {"kn": "", "roman": "Namaskara.", "en": "You can greet me with Namaskara."}
}
```

These are protocol fixtures, not reviewed learning content. Echo the exact version, request ID and turn ID. The adapter creates a new 32-character lowercase hexadecimal request ID for each explicit submission. IDs correlate replies; they provide neither authentication nor server deduplication. Turn IDs and speaker display names are current content labels, not globally stable identities for server conversation memory.

Payloads contain only copied text visible at the current turn, available authored reply text, current speaker/turn labels and the learner's bounded submission. There is no full save, history, undiscovered fact list, quest effect, reply-selection command, intent application or scene mutation. Unity retains all quest gates, story consequences and saves. Generated text is presented literally; the supported authored choices still advance the story. The transport never receives a session or progress object. Additional response fields such as `effect` fail validation. A reviewed allowed-fact/spoiler policy is still required before a real language model is used.

Limits enforced by the game:

| Item | Limit |
| --- | --- |
| Request and response bodies | 65,536 UTF-8 bytes each, before sending/parsing |
| JSON nesting | 8 levels on responses |
| Learner submission | 512 UTF-16 code units; existing prompt truncates at text-element boundaries |
| Speaker / turn ID | 128 UTF-16 code units each; non-whitespace |
| Available replies | 0–32, each with text only |
| Each `kn`, `roman`, `en` field | 4,096 UTF-16 code units, valid Unicode |
| Response visible content | At least `kn` or `en` contains non-whitespace text |

Protocol code never silently truncates returned text or authored context; over-limit context falls back locally. JSON Schema counts Unicode code points, so its `maxLength` is supplemented by `x-maxUtf16CodeUnits` and the normative limits above. A body can exceed the byte limit even if all individual text fields fit (JSON escaping counts toward bytes).

The [backend handoff and portable conformance corpus](contracts/nlp-v1-fixtures/README.md) supplies synthetic request/response files, expected acceptance results and a socket-free saved-response check. It also covers duplicate nested fields, unpaired surrogates and supplementary characters at the UTF-16 limit. No model implementation or backend framework is required to use the corpus.

## Game-side implementation and lifecycle

`LoopbackDialogueTextService` implements the existing `IDialogueTextService`; `DialogueHttpContract` maps the immutable in-process types. It uses `HttpClient` and explicit `System.Text.Json` readers/writers already supplied by Unity 6000.6's cached .NET references. This permits worker-thread cancellation and standalone HTTP tests without Unity API calls or a second Editor process. No package/backend framework is added. Windows Editor/desktop is the initial target. WebGL, mobile and IL2CPP acceptance are not established.

Network construction requires `enableMockNetworking: true`. Only `http://127.0.0.1:<port>/v1/dialogue/respond` is accepted. Hostnames, wildcard addresses, IPv6, remote hosts, HTTPS endpoints, alternate routes, credentials, queries and fragments are rejected before a request. Proxy use, redirects, cookies, decompression and default credentials are disabled. No endpoint or opt-in is persisted in a scene or asset; no startup hook attaches a service. These tools do not change firewall settings or request permissions. The Editor menu remains idle until explicitly invoked.

```csharp
// On the Unity thread; owner retains this instance until detaching.
var service = new LoopbackDialogueTextService(
    true, new System.Uri("http://127.0.0.1:8765/v1/dialogue/respond"));
session.SetOptionalDialogueService(service);
// When done, on the Unity thread:
session.SetOptionalDialogueService(null);
service.Dispose();
```

The existing coordinator owns supersession, the eight-second UI deadline, turn/provider replacement and stale-result rejection. The transport adds a seven-second default total deadline covering headers and streamed body reads (configurable from 1 to 7,000 ms). It forwards cancellation to the HTTP operation and body reads, with independent state per request. There are no application retries. Cancelling the game request does not prove remote computation stopped.

The response is read incrementally with a hard byte bound; declared excessive lengths fail before body reads, and undeclared/streamed bodies cannot bypass the cap. Only HTTP 200 and uncompressed JSON with no charset or UTF-8 charset are accepted. Errors, redirects, 401/403, 429, server failures, disconnects, invalid envelopes and body limits become the existing `Unavailable` authored fallback. The coordinator's own deadline returns `TimedOut`; a shorter transport deadline returns a sanitized `Timeout` failure through `Unavailable`. Explicit cancel/new turns/providers invalidate the request and existing generation checks reject late responses before display. Shared services may handle parallel callers with separate correlation IDs.

`DialogueTransportException.Failure` is a fixed code for optional diagnostics. No payload, learner text, raw response, token, server error or inner exception is logged. The transport logs nothing by default. The Editor logs only attachment metadata. Before adding production telemetry, agree retention and privacy rules; avoid storing learner utterances. Disposing the owner aborts active calls; the session itself only cancels its requests.

## Run the mock and checks

From the game repository root in PowerShell, with the configured Unity installation and one prior project import:

```powershell
# Compile runtime/editor/player source branches and run in-memory HTTP + existing logic tests.
.\verification\Verify-Nlp.ps1
# Compile only, without opening any listener or making any requests.
.\verification\Verify-Nlp.ps1 -CompileOnly
# Explicitly opt into real loopback socket tests when local socket access is available.
.\verification\Verify-Nlp.ps1 -NetworkChecks
# Run the synthetic fixture on literal IPv4 loopback, default port 8765. Ctrl+C stops it.
.\verification\Verify-Nlp.ps1 -ServeMock
```

The runner uses its own ignored `verification/Temp/NlpVerification` outputs. It does not open Unity, regenerate scenes, launch a Player, change firewall settings or contact an external service. The default run uses the actual client/codec/adapter/coordinator with an in-memory HTTP handler and opens no sockets. `-NetworkChecks` opts into an ephemeral loopback listener and the actual `HttpClientHandler`; `-ServeMock` opens the fixed development port. Both modes use the same synthetic response fixtures and no learner/private data. The mock is a bounded, development-only minimal HTTP/1.1 fixture implemented with .NET TCP primitives; no Python, HTTP.sys registration or web framework is needed. It does not log request bodies or access requests. Its fixed Kannada greeting is a protocol fixture only.

When desktop Unity playtesting is available, start the mock, enter Play mode and use **Paridhwani > NLP mock > Attach loopback service (port 8765)**. Speak to an NPC and explicitly submit optional text. The reply should appear without advancing the quest; supported replies continue normally. Use **Detach and use authored dialogue** to restore the offline path. Leaving Play mode, assembly reload and Editor shutdown detach/dispose. Manual playtests send only to the explicitly attached local fixture; use synthetic input. No automatic mock startup or connection is shipped.

For interactive fault tests, submit `mock:delay`, `mock:slow-body`, `mock:wrong-id`, `mock:wrong-turn`, `mock:wrong-version`, `mock:malformed`, `mock:duplicate`, `mock:text-limit`, `mock:oversize`, `mock:stream-oversize`, `mock:redirect`, `mock:unauthorized`, `mock:forbidden`, `mock:limited`, `mock:error`, `mock:content-type`, `mock:encoding`, or `mock:command`. Delays are 600 ms to keep automated tests fast; cancel or submit another turn during the delay to exercise supersession. Automated tests also set shorter service/coordinator deadlines. Restart/detach the local server to test connectivity fallback. These fault commands are fixture behavior, not production protocol fields.

## Acceptance and next boundary

See [recorded verification](../verification/nlp-http-v1-check.txt). The source runner compiles against cached Unity references and executes the .NET transport with an injected in-memory HTTP handler plus the existing adapter/coordinator. It does not establish socket/native Unity/Player behavior. The real loopback attempt failed with `Connectivity`; a minimal loopback TCP probe confirmed `SocketException: AccessDenied` in the execution environment. No permission escalation or bypass was attempted. Real socket tests and a fresh in-engine attach/submit/cancel/detach acceptance pass remain pending. The graphics task also owns the Editor and the user's firewall prompt remains pending. Do not dismiss that security prompt or request network permissions on the user's behalf.

Next, the language-engine team can implement the request and response schema and run the same client against its loopback process. Agree real hosting, authentication, stable content identities, allowed facts, language review, latency and deduplication separately before a remote transport is introduced. Remote networking is deliberately unavailable in this implementation; replacing the service behind `IDialogueTextService` preserves the authored story boundary.

References: [HttpClient asynchronous send](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync), [explicit JSON document parsing](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsondocument.parse), and the existing [Unity ownership boundary](NLP_UNITY_BOUNDARY.md).
