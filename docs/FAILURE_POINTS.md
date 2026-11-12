# Paridhwani failure register

Reviewed: 5 November 2026. Scope: the current working source, the reported tea-stall
chat failure, and additional issues noticed while tracing the game. This is an
initial engineering register, not an exhaustive playtest or release certification.
Existing uncommitted work has been preserved. General issues below are recorded
for a later pass, as requested.

Paths are relative to `game`. `Runtime/` means
`My project (1)/Assets/_Paridhwani/Scripts/Runtime/`; `Editor/` means the sibling
`Scripts/Editor/` directory. Priority P1 means a progress/reliability concern, P2 a
usability or scaling concern, and P3 maintenance or acceptance work. A source
hazard is distinguished from a failure reproduced in the running game.

## The current reported incident

The player sees an authored greeting, sends `Hello` or `namaskara neeru beku`, and
gets no generated reply. The original game reported every failure as "Chat is
unavailable". The supplied browser health response establishes that a v2 Groq
bridge is running with a nonempty configured key. It does not establish that the
key works, that the provider accepts the selected model/request, or that the
running Unity process uses that same address. `speech_configured: false` explains
missing voice, independently of text chat.

The precise live cause remains unverified. The new Unity diagnostics and specific
in-game errors are intended to identify it without sending API keys into chat or
logs. A successful synthetic reply is a connectivity result, not evidence of
fluent or semantically correct Kannada.

## Conversation fixes implemented in this pass

| ID | Priority | Failure and resulting change | Evidence / validation |
|---|---|---|---|
| C01 | P1 | HTTP failures lost their status and backend reason. The client now retains a numeric status and maps only known error codes to safe categories. | `Runtime/VillageConversationClient.cs`, `DecodeFailure` and `Exchange`; real HTTP-client methods exercised through an in-memory handler. |
| C02 | P1 | Exceptions became an unexplained unavailable result. Request receipts now carry a safe failure category, the UI explains it, and the Unity Console records category/status/request ID and bounded exception type names without raw exception messages. | `Runtime/DialogueTextRequests.cs`, `Runtime/VillageConversationFlow.cs`, `Runtime/VillageModelConversation.cs`; credential-redaction regression cases. |
| C03 | P1 | The game never checked its own configured bridge. A bounded health check now precedes a model turn and rejects incompatible/unconfigured services before inference. Editor menus test the actual process configuration and one synthetic reply. | `Runtime/VillageServiceConnection.cs`, `Runtime/VillageConversationClient.cs`, `Editor/VillageConversationDiagnostics.cs`; mocked health-to-POST checks. Native/live result remains pending. |
| C04 | P1 | Unity allowed 15 seconds while the backend could spend 45 seconds on each of up to three calls. Unity now allows 60 seconds; the backend shares a 45-second turn budget, allocating at most 20 seconds per call. | `Runtime/VillageConversationFlow.cs`, `village_backend/server.py`; simulated decreasing-budget and late-response rejection tests. The backend budget is cooperative, not a way to kill provider work. |
| C05 | P1 | Readable Kannada/Roman replies were discarded if English was empty. At least one nonempty text field now suffices, and display fallback prevents blank bubbles when an enabled aid's translation is absent. Unknown/missing action metadata becomes no action; readable dialogue remains. Correlation, text bounds and verified story gates remain enforced. | Client normalization, flow, `TeaLine.DisplayFallback` and backend reply validation; translation-absent, aid-fallback, unknown-action and correlation tests. |
| C06 | P2 | Failed attempts kept the draft but appended another identical bubble on retry. Failed delivery identities are now reused and bubbles show delivery state. Successfully delivered repeated messages remain distinct. | `Runtime/TeaStallUI.cs`, `Runtime/VillageConversationFlow.cs`; identity regression tests and a native retry assertion added. |
| C07 | P2 | Vertical phrase buttons inherited a zero flex basis from horizontal choices, visibly compressing their text. Phrase buttons now use automatic content height, a minimum height and nonshrinking labels. | `My project (1)/Assets/_Paridhwani/UI/TeaStall.uss`; source fix and native containment checks added. Actual viewport captures remain pending. |
| C08 | P1 | Exceptions while preparing the detached request could escape the async UI handler. Preparation now fails with a safe message and preserves the draft. | `Runtime/VillageModelConversation.cs`; preparation guard and source compilation. |
| C09 | P2 | A busy port produced misleading advice even when an old/different provider bridge occupied it. The launcher now checks the existing service's protocol/provider and explains the required restart. | `Start-VillageConversation.ps1`; syntax verification. It does not stop another process automatically. |
| C10 | P1 | Adding the diagnostic menu would otherwise invalidate the generated-world recipe hash. The tool is excluded from geometry signatures, with a regression assertion. | `Runtime/WorldRecipeSignature.cs`, `verification/ChapterChecks.cs`; current scene signature checked against current generator sources. |

## Deferred service and conversation issues

| ID | Priority / status | Failure point, trigger and impact | Evidence / next work |
|---|---|---|---|
| S01 | P1 / live evidence missing | The user's actual failed turn has not been reproduced through the running Editor and real provider. Unit success cannot identify a bad key, model rejection, process-specific URL, firewall issue or runtime response problem. | Run both new Unity connection menus and then a real chat. Preserve the sanitized failure category/status. This is the first acceptance step. |
| S02 | P1 / confirmed operational dependency | The bridge is still a separate manually started process. Closing its terminal stops generated chat. Existing processes retain old code and credentials until restarted. | `Start-VillageConversation.ps1`, `village_backend/server.py`; plan a managed local launcher or a deployed backend with a clear lifecycle. |
| S03 | P1 / confirmed limit | Cancelling a Unity request does not cancel provider computation already started by Python. A retry can use quota while the abandoned operation finishes. UI deduplication does not make provider calls idempotent. | Backend synchronous provider calls and thread handling; add cancellation/idempotency policy if required. Socket timeouts and the cooperative turn budget reduce, but do not eliminate, residual work. |
| S04 | P1 / language acceptance pending | Kanglish spelling variation, English/Kannada mixing, negation and task intent can be misclassified. The verifier uses the same model family; confidence is not calibrated proof. | `assess_action`, `VERIFY_SYSTEM`, language rules in `server.py`; build a fluent-speaker-reviewed corpus covering every chapter, not only introduction examples. |
| S05 | P1 / capability limit | The model is prompted with supplied scene facts; it has not been trained on the village and has no full authoritative knowledge retrieval system. Unsupported questions may produce invented details. | `PROFILES`, `REPLY_SYSTEM`, `evaluate`; review grounding tests and decide the permitted knowledge scope. |
| S06 | P2 / confirmed architecture limit | Four backend operations share the service, and speech also uses those slots. Provider account limits can be reached independently of that local concurrency cap. | `BoundedSemaphore(4)`; measure concurrent use and define account-level pacing. No automatic billable retry was added. |
| S07 | P2 / confirmed current configuration | The supplied health output reports speech disabled. Spoken replies require a separate configured Azure resource. Speech exceptions are deliberately suppressed and failed speech can be attempted again for later lines. | `PlayVillageSpeech`, `azure_speech`; improve voice availability/status and retry policy in a later audio pass. Text success must remain independent. |
| S08 | P2 / confirmed behavior | Successful unrelated conversation automatically opens phrase help. This can distract from free conversation and occupy the chat area even though delivery succeeded. | `VillageConversationFlow.Send`; usability test whether this should be an unobtrusive hint or manual help. |
| S09 | P2 / confirmed limit | Conversation memory retains only six exchanges per NPC, in memory. Reloading/resetting a visit clears it; longer discussions lose older context. | `VillageConversationFlow.History/Remember/ResetVisit`; decide whether longer or persistent conversational continuity is needed. |
| S10 | P1 / public-release gap | The optional network-facing backend uses a shared demonstration token rather than individual player identity, per-user quotas or a public spending policy. | Service-token checks and `docs/HOSTED_CONVERSATION_SETUP.md`; suitable deployment design needs a separate pass before public distribution. |
| S11 | P2 / confirmed response-handling gap | Hosted requests have a 768-token completion cap, but provider finish reasons/refusals are not classified separately. A truncated or refused structured reply can become a generic invalid-model-response failure. | `groq_generate`, `openrouter_generate`; test completion-limit/refusal envelopes and report their safe reason before considering token-budget changes. |

## Deferred game lifecycle, world and persistence issues

| ID | Priority / status | Failure point, trigger and impact | Evidence / next work |
|---|---|---|---|
| G01 | P1 / source hazard; native reproduction needed | Disabling `TeaStallSession` during an outstanding reply does not currently cancel that reply in `OnDisable`. The completion guard checks object/turn/provider/UI identity, but not whether the component is enabled. A response could therefore apply while the session is disabled. | `Runtime/VillageControllerInput.cs:OnDisable`, `Runtime/VillageModelConversation.cs:AskVillageModel`; reproduce with an injected delayed reply and define disable/pause cancellation policy. |
| G02 | P1 / confirmed recovery gap | Additive loading waits indefinitely for `AsyncOperation.isDone`. There is no loading timeout or cancellation path if an operation stalls. | `Runtime/VillageSceneBootstrap.cs:LoadWorlds`; add bounded loading and an explicit recovery route. |
| G03 | P1 / source hazard | Failed additive loading/binding retains partially loaded scenes and possibly partial bindings. Retry skips already loaded scenes, so it is not a transactional reload. | `VillageSceneBootstrap` and `VillageWorldBindings`; inject load/binding failures and verify rollback/retry. |
| G04 | P2 / confirmed maintenance coupling | Generated-world readiness hashes almost every Editor script and requires exactly fourteen story NPCs. An unrelated new Editor tool or a content expansion can block startup unless the exclusion/revision/bindings are updated. | `Runtime/WorldRecipeSignature.cs`; diagnostic-tool exclusion fixed here, wider recipe ownership remains a later refactor. |
| G05 | P1 / confirmed validation gap | Save loading validates version, numeric preferences and position bounds, but does not enforce consistency among all story flags. A syntactically valid contradictory save can be accepted. | `Runtime/TeaProgress.cs:TeaSaveStore.TryRead`; add explicit migrations/invariant validation and malformed-state fixtures. No ordinary-play corruption has been reproduced. |
| G06 | P1 / source hazard | Two standalone instances share the same primary, backup and fixed `.tmp` save paths without a process lease. Concurrent saves can race or overwrite one another. | `TeaSaveStore.Write`; reproduce concurrent instances, then choose a single-instance or locked-save policy. Atomic replacement alone does not serialize writers. |
| G07 | P2 / confirmed bound missing | Save files are read with unbounded `File.ReadAllText`. An oversized/corrupt local file can consume excessive memory before decode. | `TeaSaveStore.TryRead`; set a generous save-size limit and retain existing recovery behavior. |
| G08 | P2 / confirmed growth | Archived visits and preserved unreadable copies have no retention/cleanup policy. Repeated fresh starts or corruption recovery grows disk use. | `TeaSaveStore.Archive/Write`; offer deliberate archive management rather than deleting player history silently. |
| G09 | P2 / confirmed main-thread work; impact unmeasured | Settings changes call `ApplyState` and synchronous save immediately. Dragging a slider can repeatedly serialize and write files during interaction. | `TeaStallSession.SettingsChanged`, slider callbacks in `TeaStallUI`; profile and debounce/coalesce settings persistence. |
| G10 | P2 / confirmed validation limit | Saved positions are validated against broad coordinate regions, not current walkable geometry. A valid coordinate may still be inside changed architecture or an obstruction. | `TeaSaveStore.SupportedPosition`, `VillageWalker.Restore`; test restored/spawned positions after world changes and add a safe-placement policy. |
| G11 | P2 / content hazard | The conversation header takes the first character of `displayName` without a runtime empty-name fallback. Missing NPC display data can throw while building the panel. | `TeaStallUI.ShowConversation`; test malformed NPC data and improve content validation/fallbacks. Current Manjunatha data is populated. |

## Deferred UI, input, performance and delivery work

| ID | Priority / status | Failure point, trigger and impact | Evidence / next work |
|---|---|---|---|
| U01 | P2 / confirmed scaling risk | The visible chat message list has no bound and every refresh recreates its bubbles. A very long conversation increases memory/layout work. | `TeaStallUI.chatMessages/ShowConversation`; measure long chats, then bound or virtualize display history. |
| U02 | P1 / acceptance pending | Phrase containment, delivery labels, focus, keyboard compression and controls need actual 960x540/1920x1080 checks at text scales 1.0/1.3. Source assertions cannot validate glyph shaping or physical hit targets. | `VillageVisualReview`, approved UI template; new native containment/retry assertions are authored but not executed in this tool session. |
| U03 | P2 / hardware acceptance pending | Gamepad drift, simultaneous mouse/controller input, disconnect/reconnect and shoulder actions need physical-device testing. Virtual input checks do not prove driver behavior. | `VillageControllerInput`, `VillageControllerUI`, controller smoke checks; preserve the recent controller-only menu-navigation work and test it on hardware. |
| U04 | P2 / confirmed architecture limit | Additive worlds load together. Renderer distance culling does not unload their scene assets, and its renderer list is captured at startup. | `VillageSceneBootstrap`, `VillageDistanceCulling`; profile memory and dynamic-renderer handling before choosing streaming work. |
| U05 | P2 / performance evidence missing | Recent world, material, resident and UI changes have no fresh target-laptop frame-time/memory acceptance result. Render-scale overrides can substantially increase GPU work. | `VillagePresentationSettings`, performance/production records; capture representative locations on the target hardware. |
| U06 | P1 / configuration inconsistency; build effect unproven | Build Settings contains the benchmark scene path with an all-zero GUID, while its `.meta` has a real GUID; SampleScene is also enabled. | `My project (1)/ProjectSettings/EditorBuildSettings.asset`; verify/normalize scene selection in a fresh build. This is not proof that the path-based build currently fails. |
| U07 | P1 / release evidence missing | Historical successful executables and ZIPs predate this source. They do not validate the current conversation transport, layout or scene changes. | `docs/PRODUCTION_STATUS.md`, build provenance records; create and playtest a new standalone package after native checks. |
| U08 | P3 / documentation drift | Historical setup/status sections still describe retired NLP boundaries or older deadlines. Readers can mistake them for current launch instructions. | README/current setup updated in this pass; retain historical evidence but label its scope and consolidate active documentation later. |

## Verification and next acceptance steps

Implemented checks cover runtime/editor compilation, non-Editor source branches,
story assertions, request lifecycle, in-memory HTTP, sanitized failure mapping,
response correlation, translation-absent replies, rejected action metadata, retry
identity and backend budgets. They make no real provider calls.

The final source run passed **764 assertions**. The backend passed **32 tests**
without sockets or credentials. Launcher syntax and scoped source whitespace
checks also passed. Pre-existing generated scene formatting was not rewritten.

Native execution and live inference remain separate: the user's Editor holds the
project lock, this session cannot drive that desktop Editor, and network isolation
prevents this execution environment from reaching the user's running localhost
bridge. No live Groq/Kannada success, new game capture or new Player build is
claimed by the source checks.

1. Stop Play, refresh assets and wait for compilation. These conversation changes
   do not require regenerating the already matching world recipe.
2. Restart the existing bridge in its terminal to load backend changes and current
   credentials. Keep that terminal open.
3. In Unity, run **Paridhwani > Check conversation connection**. It makes no
   provider call. Confirm the Console's effective service origin and provider.
4. Run **Paridhwani > Test one conversation reply**. This uses provider quota but
   changes no saved visit. Record the safe success/failure line.
5. Test English-only input, a real mixed-language introduction, an unrelated water
   request, a failed retry and a valid task completion. Confirm reply delivery and
   story advancement independently.
6. Run the approved native UI review at both viewports/scales, the presentation
   demo and full-story playtests, then a fresh standalone build.

Source verification totals and the exact remaining native gates are recorded in
the latest conversation section of `PRODUCTION_STATUS.md`.
