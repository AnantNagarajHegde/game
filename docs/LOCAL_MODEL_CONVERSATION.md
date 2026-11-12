# Conversations and phrase help

For the current launch commands, configuration check and private hosted-backend
setup, see [Hosted conversation setup](HOSTED_CONVERSATION_SETUP.md).

Every NPC chat opens immediately with its authored line. **Phrases**, beside Send,
offers the current authored Kannada replies with the player's Roman-letter and
English aids. Choosing a phrase applies that specific story choice locally; it
does not call a model or certify language proficiency. Evidence activities retain
their existing prerequisites and wrong-answer checks. This assisted route works
without a conversation service.

Typed messages still go through the bridge to the configured model. A reply has
a 60-second deadline, including a short health check of the game's configured
service address. The backend shares a 45-second budget across its model calls,
with at most 20 seconds allocated to one call. A failed or slow request preserves the typed message for
retry and opens the phrase choices. An unmatched reply also opens phrases and
explains that the conversation can continue without having completed the current
task. Retrying the same failed message reuses its chat bubble and updates its
delivery status; deliberately repeating a successfully delivered message creates
a new bubble. Phrases can be opened while waiting; doing so cancels the request. A late
reply cannot advance the turn reached using a phrase.

The old tea-only evaluator, authored topic router, optional
mock dialogue transport and their Editor attachment menus have been removed from
the game. The separate `language-engine` repository has not been modified.

The story's authored state machine is still essential: it owns available actions,
quest prerequisites, story facts, activities and saved progress. Unity sends
the current character, objective and relevant story facts to the service. It also
previews eligible outcomes on a detached copy of the save, so only the accepted
action's resulting fact is supplied to the reply generator. No preview mutates
the player's visit. Authored lines provide immediate dialogue and story facts;
successful typed messages add generated replies. A short in-memory history is
kept per NPC and cleared when loading or switching main, practice or demo visits.

For a message the service first assesses intent, then independently checks a
proposed action, and finally generates the NPC reply in Kannada, Romanised Kannada
and English. When there is no matching action it still generates a reply. Unity
checks the returned action against the current turn and applies it through the
story state machine; stale, invalid or cancelled replies cannot change progress.
This uses up to three sequential model calls, so stronger local models may be
slow. The model can still misclassify a response or invent dialogue: the second
assessment and confidence threshold reduce risk, but neither is proof of meaning.
The model is prompted with story context; it has not been trained on this story.

A reply needs at least one readable Kannada, Romanised or English field; a missing
English translation alone does not discard it. Unknown or missing action metadata
is downgraded to no story action while readable dialogue remains visible. Request,
turn and protocol correlation remain mandatory; unrelated responses are rejected.

Manjunatha is prompted to be gently confused by English-only messages. The
model classifies the player's language along with intent; an English-only or
uncertain classification cannot complete his current action, while Kannada or
genuine Kannada-English mixing can. The classification is model-generated, so
alternate spellings and ambiguous or short messages need real-model and fluent
Kannada review. Other characters retain their existing language policy.

## Try Groq's hosted free tier

The bridge uses Groq by default. Its current model
candidate is `qwen/qwen3.8-27b`, which supports strict structured output. This is
a candidate to benchmark, not a validated Kannada or Kanglish model. Provider
availability, free-tier quotas and model IDs may change; check the Groq dashboard.

Save `GROQ_API_KEY` in Windows user environment variables. From `game`, launch
the bridge with the saved key:

```powershell
.\Start-VillageConversation.ps1 -Provider groq -CheckConfiguration
.\Start-VillageConversation.ps1 -Provider groq
```

The launcher, Python server and benchmark read the saved Windows user value
before an inherited terminal value. If a saved value is unavailable, they use
`GROQ_API_KEY` from the process environment, including on Linux/container hosts.
Restart the bridge after changing the saved key. The root `groq.ps1` shortcut
uses the same launcher and saved key.

To use another Groq model with strict structured-output support, supply
`-Model '<model-id>'`. The configuration check does not call Groq or verify the
key, model availability, quotas, response quality or latency. Before using this
for a demo, compare the real model on the synthetic initial-turn examples:

```powershell
python village_backend/benchmark.py --provider groq
```

Those checks make actual provider calls and use the account's quota. They only
check action IDs, not translation quality. The game currently makes up to three
model calls per typed turn; multiple players share the provider's account limits.
Speech is a separate Azure service and is not covered by a Groq chat quota.

## Use an OpenRouter-hosted model

The launcher defaults to a model hosted by OpenRouter, avoiding local LLM inference
while Unity is running. An OpenRouter account, API key and model supporting
structured output are required; availability and charges depend on the chosen
model and account. Set the key in the terminal, not in source control, then use
the chosen provider/model ID:

```powershell
$env:OPENROUTER_API_KEY = '<your key>'
.\Start-VillageConversation.ps1 -Provider openrouter -Model '<provider/model>'
```

The bridge still runs on the player's machine and sends requests to OpenRouter;
it is not a deployed shared backend. The key remains in the bridge environment,
not Unity. Each turn may make up to three model calls, so consider latency and
cost. Do not distribute a build with provider credentials.

To compare an actual hosted model against the initial task examples, run:

```powershell
python village_backend/benchmark.py --provider openrouter --model '<provider/model>'
```

This makes real paid-provider calls when the account/model is billable. Its
action checks do not establish that the generated Kannada is correct.

## Spoken NPC replies

Optionally configure an Azure Speech resource to synthesize each generated reply.
The game speaks the Kannada-script line; if the model did not provide one, it
speaks the displayed English line instead. The Kannada voice is used for Kannada,
with different voice defaults for male and female named characters. Text remains
available if speech is unavailable. Audio generation is separate from story
progress and stops when the conversation is closed, a new message is sent, or
the game loses focus. Existing master volume controls the voice.

Set these environment variables in the same terminal before starting the bridge:

```powershell
$env:AZURE_SPEECH_KEY = '<your key>'
$env:AZURE_SPEECH_REGION = '<your Azure Speech resource region>'
```

Azure Speech is optional and may incur charges. Without both values the game
remains text-only. It does not currently listen to or transcribe player speech.
Voice quality, pronunciation, latency and audio playback need a real-provider
test and an in-Unity listening review.

## Start the local service

1. Open PowerShell in the `game` folder and run:

   ```powershell
   .\Start-VillageConversation.ps1 -Provider ollama
   ```

   The launcher starts Ollama if needed, checks the selected model and runs the
   bridge. Keep the terminal open while playing. Stop an old bridge first after
   updating: the new game requires the `/v2/village/turn` protocol.
2. Press Play in Unity and talk to an NPC. Type to use generated chat, or select
   **Phrases** to use authored replies immediately. If the service is unavailable,
   the message stays in the composer for retry and phrase choices open. Escape
   leaves the conversation. The service is only required for generated chat.

The default model is `gemma3:4b`, selected because it was already installed on
the development machine. It is an alternative to the previous `qwen2.5:3b`, not
a claim that its Kannada is better. Test it before a demonstration.

To try a larger local candidate, install it yourself and choose it explicitly:

```powershell
ollama pull qwen3:8b
.\Start-VillageConversation.ps1 -Provider ollama -Model qwen3:8b
```

The 8B model is much larger and may compete with Unity for memory and GPU time;
measure both game FPS and response latency on the actual machine. No model is
bundled or automatically downloaded. For a small real-model comparison, with
Ollama running, execute from `game`:

```powershell
python village_backend/benchmark.py --model gemma3:4b
python village_backend/benchmark.py --model qwen3:8b
```

The benchmark checks initial-turn action examples, including the reported
`chaha beku` case. It is not a Kannada quality evaluation and does not review the
generated text. A fluent speaker must review real Kannada-script and Romanised
responses, alternate spellings, negations and later story scenes.

The bridge listens locally on `127.0.0.1:8767`; if Ollama is selected it is
expected at `127.0.0.1:11434`. The bridge does not log messages or persist chat
history. OpenRouter and Azure Speech receive the relevant text when configured;
check the providers' data handling and charges. Close the bridge window or press
Ctrl+C to stop it. No shared cloud deployment is included.

If typed chat is unavailable, open `http://127.0.0.1:8767/health` on the game
computer. For the Groq setup, the result should report `provider: groq` and
`model_configured: true`. This only checks local configuration: it does not
validate the API key or contact Groq. Restart an existing bridge after backend
updates, retry the message, and check its PowerShell window for a sanitized
provider error. For example, `model_authentication_failed` indicates an upstream
authentication failure and `model_rate_limited` indicates an upstream rate limit.
These diagnostics do not print provider response bodies, API keys or player text.

For a check from the actual Unity process, stop Play, use **Assets > Refresh**,
wait for compilation, and open **Paridhwani > Check conversation connection**.
It uses the same service address/access token as the game and makes no model call.
The Console records a sanitized service origin and provider, or an error category.
Changing `PARIDHWANI_CONVERSATION_URL` or `PARIDHWANI_SERVICE_TOKEN` requires the
Editor to inherit the new environment; changing a Groq/OpenRouter key requires
restarting the bridge, since Unity does not read provider keys.

Then use **Paridhwani > Test one conversation reply** to exercise a synthetic
turn through the game's real HTTP client. This uses provider quota (up to three
model calls) without touching a saved visit. The Console reports success or
`PARIDHWANI_CHAT_DIAGNOSTIC_FAILURE`, with a safe category and HTTP status. In-game
failures also emit `PARIDHWANI_CHAT_FAILURE` with a request ID and safe exception
type names for unexpected failures. These logs contain
no API keys, access tokens, player messages or provider response bodies. The
checks distinguish configuration readiness from successful model inference; they
do not certify Kannada quality. See [the failure register](FAILURE_POINTS.md).

## Verification

Run `verification/Verify-TeaConversation.ps1` for source compilation, existing
story checks and socket-free backend tests. `village_backend/benchmark.py` makes
real local model calls only when explicitly run. A Unity playthrough, controller
interaction, local HTTP exchange, model quality, timing and standalone Player build
remain separate acceptance checks. Earlier NLP/tea assessment documentation and
recorded test artifacts describe retired integrations and are historical.

`VillageConversationFlow` owns request lifecycle, drafts, feedback and bounded
history. `VillageConversationClient` owns HTTP and protocol handling. Unity's
session adapter previews eligible actions and applies accepted choices through
the authored provider. `VillageStory` defines chapter entries, preparation and
ordered objectives; existing saved fields and story gates remain compatible.

The source runner includes failure, timeout, unmatched/invalid reply, late reply,
draft retention, protocol correlation, offline opening-chapter and all-chapter
objective/unlock checks. Native layout and input checks have been extended but
have not been run for this change; the user is doing those in Unity later.

For the native review, use **Paridhwani > Review UI and graphics** at 960×540 and
1920×1080, with 1.0 and 1.3 text scale. It now captures phrases, injects an
unavailable service, exercises the assisted introduction and rejects a stale
phrase callback. Also check controller navigation through the phrases and the
QWERTY keyboard, and run **Run Presentation Demo Playtest** and the full Editor
playtest. Rehearse the introduction without a bridge: greet, choose tea or water,
ask for the library, and check the notebook. Fluent Kannada and human usability
review, real-provider latency and a fresh standalone build remain separate work.
