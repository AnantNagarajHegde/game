# Hosted conversation and spoken replies

The default launcher uses Groq's `qwen/qwen3.8-27b` model. A small Python HTTP
bridge runs beside Unity; model inference and selected speech synthesis run remotely.
The existing story and saved-progress rules remain in Unity.

## Set up the local bridge

Save `GROQ_API_KEY` in Windows user environment variables, then run from `game`:

```powershell
.\Start-VillageConversation.ps1 -CheckConfiguration
.\Start-VillageConversation.ps1 -CheckProvider
.\Start-VillageConversation.ps1
```

The launcher and Python backend read the saved Windows user value before an
inherited `GROQ_API_KEY`. This also applies to direct Python startup and the
benchmark. If the saved value is unavailable, the process environment supplies
the key, including on Linux/container hosts. Restart the bridge after changing
the saved key. From the repository root, `groq.ps1` starts the same service.

For OpenRouter, set `OPENROUTER_API_KEY` and launch with `-Provider openrouter`
and `-Model '<provider/model-id>'`. Sarvam Kannada speech is selected with
`$env:PARIDHWANI_TTS_PROVIDER = 'sarvam'` and `SARVAM_API_KEY` before starting
the bridge. It uses Bulbul v4 Flash by default; set `SARVAM_TTS_MODEL` only when
choosing another supported Sarvam TTS model. The key stays in the backend
environment and is never sent to Unity. A local disk cache avoids repeated API
calls for the same text, voice and model (`village_backend/.tts-cache`; override
with `PARIDHWANI_TTS_CACHE`). Azure remains available with
`PARIDHWANI_TTS_PROVIDER=azure`, `AZURE_SPEECH_KEY` and `AZURE_SPEECH_REGION`.

The configuration check makes no provider calls and prints only configuration
booleans and the provider name. It does not prove that credentials work, that the
model is available, or that its Kannada is correct. Missing speech configuration
keeps text chat usable. Keys belong in the backend's environment, never in Unity
assets, commits, or a distributed game. Provider calls can incur charges.
Sarvam currently accepts Kannada in this integration; English lines should use
Azure or remain text-only. The configured Sarvam voice map uses Chetan for male
NPCs and Chaitra for female NPCs. These are voice assignments by character
group, not verified age or personality matches.

Speech follows each newly added NPC chat line, including an initial greeting or
an authored scene line. Refreshing the chat or changing a translation aid does
not replay the same line. A new line stops the previous audio.

The service exposes `GET /health` with the same configuration information, without
making paid inference or audio calls. A completed chat makes up to three sequential
model calls sharing a 45-second turn budget; each call receives at most 20 seconds
or the remaining budget. The game allows 60 seconds including a 4-second health
check and transport margin. These are bounded client waits and cooperative backend
deadlines, not a provider-side cancellation mechanism. Speech has its
own 30-second timeout and cannot apply story actions.

## Host the bridge as well

The backend can run on an existing container host behind its HTTPS reverse proxy.
Build from `game/village_backend`; the Dockerfile copies only `server.py`, and
requires no Python packages. The container build has not been verified here.
Supply `GROQ_API_KEY`, `PARIDHWANI_MODEL`, and the selected speech provider's
secrets (`SARVAM_API_KEY` with `PARIDHWANI_TTS_PROVIDER=sarvam`, or
`AZURE_SPEECH_KEY` plus `AZURE_SPEECH_REGION`) as host runtime secrets. Set `PORT` to the host's service
port, or expose its default port 8767. Configure `/health` as the health check.

Set an additional `PARIDHWANI_SERVICE_TOKEN` on the host: 32 to 256 random URL-safe
characters. A network-facing server refuses to start without it. Both chat and
speech require that token before calling a provider. At most four paid operations
run concurrently; additional requests return HTTP 429. The health check remains
public and exposes no keys or conversation text.

On the player's computer, set only these values **before launching Unity Hub,
the Unity Editor, or the built game**, so that the game inherits them:

```powershell
$env:PARIDHWANI_CONVERSATION_URL = 'https://<your-private-backend-host>/'
$env:PARIDHWANI_SERVICE_TOKEN = '<the private demo service access token>'
```

Unity sends both chat and speech to that address. HTTP is accepted only on
localhost; remote connections require HTTPS. The local bridge does not need to
run for this mode. If the host has a path prefix, include it with a trailing slash.
Remove these two environment variables to return to the default local bridge.
No provider keys are sent from Unity.

This shared token is for a private college demonstration. It can be extracted
from a player's process and is not a public-release identity system. A public
release needs individual sign-in, server-side rate limits and a spending policy.
No hosting account has been created, no service has been deployed, and no paid
provider has been activated by these changes.

## Diagnose a rejected API key or provider request

For Groq, run from `game` in the same terminal that starts the bridge:

```powershell
.\Start-VillageConversation.ps1 -Provider groq -CheckProvider
```

For OpenRouter, use `-Provider openrouter` with your chosen `-Model`. This check
makes one authenticated GET, starts no server, and generates no text. Groq uses
its model-list endpoint; OpenRouter uses its authenticated current-key endpoint,
because the public OpenRouter model list does not validate a key. Only safe
booleans and fixed error categories are printed, never keys or account details.
Exit code 0 means the check passed; failures exit with code 1.

`credentials_accepted: true` confirms the key worked for that GET.
`model_listed: true` confirms the chosen Groq model appears in the response.
Neither proves model-specific inference permissions; the output explicitly keeps
`model_access_verified: false`. Use **Paridhwani > Test one conversation reply**
in Unity to test actual generation with provider quota.

| Result | Meaning and next step |
|---|---|
| `model_authentication_failed` / HTTP 401 | The provider rejected the key. Set a valid key in this terminal, then restart the bridge. |
| `model_access_denied` / HTTP 403 | The provider denied access. Check the account/project's model permissions. A new key alone may not resolve this. |
| `model_request_blocked` / HTTP 403 | The response contains the known Cloudflare 1010 client block. Check the backend's network/provider access. Hosted requests now identify themselves as `Paridhwani/2.0` rather than the generic Python client. |
| `model_resource_not_found` | The selected Groq model was not listed. Check the model ID. |
| `provider_dns_failed` | The API hostname could not be resolved from that terminal. Check DNS/network access. |
| `model_timed_out` / `model_unavailable` | The check timed out or could not reach the provider. |

The game distinguishes rejected keys, access denials and recognized request
blocks instead of presenting every 403 as a bad credential. Unknown 403 responses
remain access denials; arbitrary provider text is never copied into chat or logs.
Restart an already running bridge after changing its code or key: its environment
and credential values were captured when that process started.

## Acceptance playthrough

Start a fresh or arrival practice visit and talk to Manjunatha:

1. An English-only introduction should receive a confused response and leave
   the introduction incomplete.
2. A Kannada greeting followed by an entirely English name introduction must
   not bypass his comprehension check.
3. `Hello, nanna hesaru Amogh` should be eligible to complete the introduction.
4. An unrelated question should still receive a reply without completing the task.
5. Check that the NPC's Kannada chat line is spoken, and that leaving chat,
   losing focus, or sending a new message stops the previous audio.
6. Temporarily disable speech at the backend and confirm that text and story
   progression remain usable.

These are real-model acceptance checks, not claims of measured language quality.
A Kannada speaker should review both script and Romanised output before a demo.

Provider references: [OpenRouter structured outputs](https://openrouter.ai/docs/guides/features/structured-outputs),
[Sarvam TTS REST API](https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/rest-api),
[Sarvam voices](https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/voices),
and [Azure text-to-speech REST API](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/rest-text-to-speech).
Diagnostic references: [Groq error codes](https://console.groq.com/docs/errors),
[Groq model permissions](https://console.groq.com/docs/model-permissions),
[Groq model listing](https://console.groq.com/docs/models),
[OpenRouter current-key endpoint](https://openrouter.ai/docs/api/api-reference/api-keys/get-current-key),
and [Cloudflare 1010](https://developers.cloudflare.com/support/troubleshooting/http-status-codes/cloudflare-1xxx-errors/error-1010/).
