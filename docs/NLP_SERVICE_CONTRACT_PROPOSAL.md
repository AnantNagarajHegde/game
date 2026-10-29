# Proposed Unity / NLP service contract

Status: integration proposal, 28 October 2026. The mapping below now has an executable, loopback-only development implementation: see [HTTP v1 contract, mock and checks](NLP_HTTP_V1.md). That document defines the concrete local route, limits and validation; remote deployment guidance here remains a proposal. This is not an existing engine endpoint. The language-engine and integration-pipeline working directories currently contain no readable implementation/contract. Keep the authored game playable until the real contract and service are supplied.

## Recommended transport and ownership

Start with asynchronous HTTPS request/response carrying UTF-8 JSON. This suits the current turn-based text interaction. A Python, .NET or other engine can implement the same HTTP contract without being embedded in Unity. UnityWebRequest is the suggested Unity transport. Streaming/voice can be added later if required; a persistent socket is not necessary for the current exchange.

Unity owns current turns, permitted choices, evidence gates, quest consequences, world state and saves. The service owns language understanding, phrasing and pedagogical feedback within supplied/allowed context. This version returns optional text only. Intent recognition that selects an authored reply must be agreed separately with stable reply IDs, an explicit per-turn allowlist, calibrated uncertainty handling and Unity-side gate validation. Never accept arbitrary effect names, C# snippets or commands from generated text.

Only supply the current NPC/turn, player input, authored lines/replies and explicitly permitted facts. Do not send the full save or undiscovered story. The service needs a reviewed allowed-fact policy to prevent spoilers; limiting the payload alone cannot guarantee a model will obey that policy.

## Proposed JSON mapping

One POST to an agreed versioned route, for example /v1/dialogue/respond. The route is illustrative and is not configured anywhere in the project.

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

```json
{
  "version": 1,
  "request_id": "f30b6c852e104d28b8a3052effda6922",
  "turn_id": "hello",
  "line": {"kn": "", "roman": "Namaskara.", "en": "You can greet me with Namaskara."}
}
```

These examples illustrate the text-only envelope; they are not reviewed Kannada lesson content. Map snake_case JSON fields to the corresponding DialogueServiceRequest/Reply C# members in the transport. Speaker is currently a display name, not a stable actor identifier. Agree a stable NPC/content identity before adding server conversation history or intent selection. A request ID correlates responses; it is not an authentication credential.

Current Unity limits: learner input is at most 512 UTF-16 code units, cut at text-element boundaries; each response language field is at most 4096 UTF-16 code units. At least Kannada or English must contain non-whitespace text. Preserve Unicode, and never truncate halfway through a text element. Set an additional agreed byte limit on HTTP bodies before parsing (64 KiB is a proposed starting limit). Responses must match the supported version, exact request ID and exact turn ID. Display generated text literally.

## Request lifecycle

- Send on explicit submission, not every frame or every keystroke. Do not block Unity's main thread. Keep Unity API/UI work on the Unity thread.
- Retain the current 8-second overall UI deadline initially and tune it from real latency measurements. Set a transport timeout too: UnityWebRequest defaults to no timeout. Forward cancellation and marshal Abort through the Unity thread when leaving the conversation, changing turn or cancelling. Cancelling a client task does not prove remote inference stopped; server cancellation needs its own support.
- Treat timeouts, malformed JSON, wrong schema/IDs, oversized replies, 401/403, 429 and server errors as defined failure states. Preserve the authored reply path and let the player continue. Avoid showing stack traces/raw server errors to the learner.
- Do not automatically retry a POST until the backend supports deduplication/idempotency. A correlation ID by itself does not implement deduplication. If retries are agreed, reuse that logical request ID and use bounded backoff/jitter within the same overall deadline; respect rate-limit guidance.
- Use HTTPS certificate validation for remote deployment. Keep provider/model keys on the backend; use an agreed short-lived client authentication scheme. Never put tokens in URLs or commit secrets to Unity assets.
- Log request ID, status, duration and contract/model version. Avoid recording learner utterances or authentication tokens by default. Agree retention/privacy rules before production telemetry.

## Still needed from the engine team

1. Actual endpoint and a sample real request/response or OpenAPI document.
2. Hosting mode: local development service versus deployed backend, and supported target platforms.
3. Authentication and credential provisioning/refresh.
4. Supported Kannada/script/transliteration fields and reviewed content/allowed-fact policy.
5. Whether the service supplies text only or intent candidates; stable per-turn intent IDs if the latter.
6. Expected latency, rate limits, errors, cancellation and deduplication behavior.

The [loopback HTTP v1 transport](NLP_HTTP_V1.md) now maps this text-only proposal behind IDialogueTextService and supplies a mock and test runner. Once the engine decisions above are supplied, agree any contract differences and provide a reviewed production transport, bind it at session setup with SetOptionalDialogueService(service) on the Unity thread (pass null to detach), and run real-service tests for success, wrong IDs/schema, delayed/out-of-order responses, cancellation, errors, limits and offline fallback. Passing in-memory HTTP-handler tests do not establish socket or deployed-engine integration.

## Reference documentation

- [UnityWebRequest](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest.html)
- [UnityWebRequest timeout](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest-timeout.html)
- [UnityWebRequest Abort](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Networking.UnityWebRequest.Abort.html)
- [Microsoft HTTP resilience and retry behavior](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [OWASP REST security guidance](https://cheatsheetseries.owasp.org/cheatsheets/REST_Security_Cheat_Sheet.html)

The Unity coordinator contains transport cancellation callback exceptions and rejects retired-provider results. The transport should still keep cancellation callbacks short and nonblocking, and marshal UnityWebRequest operations to the Unity thread. The session cancels requests but does not dispose a shared service connection.
