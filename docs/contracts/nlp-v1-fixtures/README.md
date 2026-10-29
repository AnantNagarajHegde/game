# Backend handoff: dialogue HTTP v1

This directory is a portable body-conformance corpus for the [Unity API contract](../../NLP_HTTP_V1.md). All input is synthetic. It contains no model, backend framework, credential, real learner text or network client. The request is checked against the game's encoder in the default test run.

`request.json` is the input envelope. Each `manifest.json` case names a response body and its expected result: `accepted` or `InvalidContract`. The accepted Unicode case deliberately includes escaped Kannada, an emoji and literal markup. The rejection cases cover nested quest commands, duplicate text fields, unpaired UTF-16 surrogates, a second JSON value, wrong correlation and numeric text. Some rejection files are deliberately invalid JSON; preserve their bytes and duplicate keys when testing them. A generic JSON parser that silently overwrites duplicate properties cannot check the full contract.

## Implement the engine boundary

1. Accept one bounded UTF-8 JSON request at `POST /v1/dialogue/respond`. Validate against `../nlp-v1.schema.json#/$defs/request`, including the extra UTF-16 limits and duplicate-property rejection described in the contract. Do not infer persistent identity from the speaker display name or turn label.
2. Build a language response using only the supplied current-turn context and an agreed allowed-fact policy. This integration supplies no permission to select a reply or alter a quest, save or world state. Backend language/model behavior and fluent Kannada review remain separate work.
3. Return HTTP 200, `Content-Type: application/json; charset=utf-8`, with version 1, the unchanged `request_id` and `turn_id`, and exactly three string fields in `line`: `kn`, `roman`, `en`. At least Kannada or English must contain visible text. Do not add effect commands, intent selections or model metadata to v1.
4. Keep success bodies at or below 65,536 UTF-8 bytes and each text field at or below 4,096 UTF-16 code units. Preserve Unicode. Run the corpus through a duplicate-aware decoder, not just a schema validator; also enforce exact correlation against each request.
5. Return a non-200 status when the service cannot provide a valid reply. Unity uses its authored fallback and ignores the error body. Do not assume automatic retries, server deduplication or remote inference cancellation. Client correlation IDs implement none of those server behaviors.

The existing Unity test transport accepts literal IPv4 loopback only, disables proxies and redirects, and has no production authentication path. Hosting and remote credentials need their own agreement before a remote implementation is supplied. No framework is imposed by the body schema.

## Check a saved response without opening a socket

From the game repository root, run the default verification once to compile the console runner and execute the corpus:

```powershell
.\verification\Verify-Nlp.ps1
```

Then a developer with .NET 8 or a compatible runtime can check any saved synthetic response body against explicit expected correlation:

```powershell
dotnet .\verification\Temp\NlpVerification\DialogueHttpChecks.dll --check-response 0123456789abcdef0123456789abcdef fixture-greeting .\docs\contracts\nlp-v1-fixtures\accepted-minimal.json
```

Exit code 0 and `NLP_RESPONSE_CONFORMS` mean the saved body passed the game's decoder and correlation checks. Exit code 1 with a fixed transport failure code means rejection. The tool bounds the file before parsing, makes no network calls, and does not print the body. Supply the original game's expected request/turn IDs when checking a different exchange; copying IDs from the response would defeat the correlation check. The example response is a protocol fixture, not reviewed learning content.

This checks JSON bodies only. HTTP headers/status, redirects, connection behavior and native Unity attachment still require their separate acceptance checks when access is available. The previously denied loopback test remains parked; this corpus does not claim socket acceptance or route around that denial.
