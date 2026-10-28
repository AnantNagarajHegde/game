# Current production status

The main story is authored from arrival to epilogue. The revision-3 benchmark world has now been regenerated in Unity 6000.6.7f1, including all story locations. The game remains a procedural prototype and is not release-ready. Historical reports in TEA_STALL_BENCHMARK.md describe older revisions.

## Verified implementation

- Seven 512px surface PNGs import as Texture2D. The import blocker was caused by Cubemap texture shape as well as obsolete metadata; explicit Texture2D preprocessing and native-upgraded metadata preserve existing GUIDs. Generation checks every import before replacing a scene.
- Native benchmark generation and Run Benchmark Checks passed on 27 October, and native checks passed again on 28 October. These exercise story gates, Unity JSON save/recovery, fourteen NPC instances, scene references and shader checks.
- Runtime/editor source compilation, runtime branches without Editor/diagnostic defines (using cached references), and 313 standalone assertions pass. These standalone checks are not a native Player build.
- Direct Editor playtesting confirmed title, arrival handoff, rain/world rendering and the isolated test HUD. The corrected automated native sequence passed on 28 October: arrival through reunion, original deed, return, council decision, epilogue, credits, completed-story restore, isolated practice and new-visit settings. It advanced 6,800 frames and produced 51 captures. The library approach now walks around Somanna and the bench through the open east side of the veranda.
- World signs have a UI theme and literal text. Generated asset names match their filenames. Dedicated manual and automated Editor test menus use temporary saves and do not migrate a player's visit.

## Latest native regression evidence (28 October 2026)

- Run directory: C:/Users/amogh/AppData/Local/Temp/ParidhwaniSmoke/run-20261028-052700.
- result.txt records VILLAGE_SMOKE_OK, Frames: 6800; Editor.log records PARIDHWANI_EDITOR_SMOKE_EXIT 0.
- Fixed Game view selected through the Editor UI: 960x540. VILLAGE_DIALOGUE_LAYOUT recorded 960x540 at text scale 1.3. The final capture was visually reviewed: long literal text scrolls beneath the fixed header/footer; Leave, notebook and Original line remain visible. The text assertion targets actual speech labels and matches the original reply, rather than incorrectly including Unity's unrelated TextField label.
- The route failure was reproduced at player (-5.15, 0.08, 11.20), beside Somanna and the veranda bench. Correcting the scripted approach preserved physical collision and verified archive entry/exit, without teleporting through the obstacle.
- This is a scripted Editor regression, not a new standalone build, complete human playthrough, performance benchmark or final Kannada acceptance. It predates the subsequent typed-NLP, cancellation, menu and exit-flow changes; their added native assertions still need execution. The small-window layout and story checks passed within the recorded run's scope.

## Engine test workflow

Open the project through Unity Hub. After external source edits, run **Assets > Refresh** and wait for script compilation before testing. Use **Paridhwani > Create Tea Stall Benchmark** after changing generator recipes, then **Run Benchmark Checks**. **Play Isolated Test Visit** opens the normal title/arrival flow on a temporary save. **Run Automated Editor Playtest** runs the scripted native regression in the Game view and returns to Edit mode on completion/failure. The HUD labels test visits. The automated sequence uses real CharacterController movement for authored walking routes, while conversation/activity actions are invoked programmatically; this does not replace keyboard/controller acceptance testing.

Automated test captures and result.txt or failure.txt are written below the OS temporary directory at ParidhwaniSmoke/run-<timestamp>. Success requires VILLAGE_SMOKE_OK; reaching an intermediate screenshot is not a pass. Editor output also records PARIDHWANI_EDITOR_SMOKE_EXIT (0 success, 2 failure).

On 28 October, a shell-launched Editor again refused the licensing IPC connection. Launching the same project through the desktop Unity Hub succeeded. This demonstrates a launch-context limitation, not an invalid Unity license; its exact underlying cause is unproven. Direct desktop testing worked in the preceding pass. This pass has shell/file access, but native desktop-control tools are not exposed.

From the master workspace root:

    ./game/verification/Verify-Sources.ps1

Standalone compiler outputs live under game/verification/Temp, outside Unity's project Temp directory so Editor startup cannot remove them mid-check.

With this project's Editor closed, a normal desktop shell can run the fresh-build workflow:

    ./game/verification/Verify-Unity.ps1 -Layout Legacy -Build -Smoke
    ./game/verification/Verify-Unity.ps1 -Layout Additive -Build -Smoke

The new **Paridhwani > Build and Run Standalone Playtest** command runs from the already-open desktop Editor. It regenerates/checks the benchmark, completes a development build, then launches only that successful build with an isolated save at 960x540. The Editor monitors the returned player process without blocking its UI. A zero exit code and VILLAGE_SMOKE_OK are both required. Failure, timeout, Editor shutdown or script reload cannot be reported as success; cancellation stops only the player launched by that test.

Each invocation writes report.json and player.log below game/verification/Temp/StandalonePlaytests/run-<timestamp>-<id>. The report records building/running/passed/failed state, build completion time, player exit code, completion marker and failure details. The command is source-compiled but its fresh standalone execution remains pending desktop Unity acceptance.

The batch workflow also keeps its reports outside Unity's cleared Temp directory, always writes a failure report, and stops early when licensing initialization fails. The actual retry on 28 October recorded editorPassed=false and smokePassed=false with a licensing initialization failure in verification/Temp/UnityVerification/Legacy-20261028-112727976.json; no stale player was launched. Its failed-build gate was exercised with a deliberately invalid non-Unity child: the report marked failure and no old player was launched. That negative check is not a Unity build.

Verification-only runner changes are excluded from the world recipe signature. The checked-in generated world still matches; changed or added actual generator files remain covered by regression tests.

No fresh standalone Player build or additive-package run has been verified in this pass. Both additive worlds currently load at startup; distance-based scene streaming is not implemented.

## Remaining acceptance and production work

| Area | Implemented | Still required |
|---|---|---|
| Story and replay | All 15 beats, evidence gates, return, endings, reached-chapter practice | Broader fresh/resumed human playthroughs and all replay spawn checks |
| Persistence and session | Atomic recovery/archive, transactional restore, main/practice isolation, title/settings/exit | Runtime failure cases and full menu/input acceptance |
| Movement and audio | Input focus policy, normalized movement, tagged ground footsteps and independent mixes | Broader route coverage, hardware input and listening review |
| Conversation and accessibility | Scrollable literal replies, original-line restore, text scale, reduced motion, language support | Kannada shaping, additional viewport/input and comfort review |
| World and rendering | Regenerated prototype locations, seven surface maps, quality profiles and culling | Production characters/rigs, terrain, vegetation, textures, animation, composition and authored audio |
| Packaging and performance | Optional additive exporter/loader, profiling tools | Execute additive export/build, frame-time/memory measurements and release packaging |
| NLP | Optional cancellable text-only adapter, immutable versioned service messages and response correlation | Connect the separately built real service once its contract is supplied; see NLP_UNITY_BOUNDARY.md |
| Language and play quality | Authored Kannada-learning tasks and source gates | Fluent Kannada/regional review and player usability testing |

The implemented story source does not establish final art quality, complete engine verification or a finished game percentage.


## NLP boundary implementation pass (28 October 2026)

Added IDialogueTextService and DialogueServiceAdapter to bind an engine-specific transport without changing the local story provider. Immutable request context and version/request/turn matching reject unrelated replies; cancellation reaches the service. Legacy provider responses are copied after validation. All runtime/editor sources compile and 275 standalone assertions pass. This pass does not connect a real engine, implement HTTP/authentication or claim a new native playthrough. See NLP_SERVICE_CONTRACT_PROPOSAL.md for the recommended transport and remaining contract decisions.


## NLP cancellation and service lifecycle pass (28 October 2026)

Reproduced and fixed an escaping transport-cancellation callback exception. Request cancellation now contains/counts callback failures, wakes ignored-cancellation requests, preserves reentrant replacements and keeps deadline cleanup separate from a completed transport. The session rejects stale generations immediately before presentation, including completed replies queued before a service change. Provider replacement/detachment cancels pending work, clears optional text and refreshes authored conversation controls. SetOptionalDialogueService provides explicit Unity-thread attachment/detachment; the service owner retains connection disposal.

All runtime/editor sources compile and 287 standalone assertions pass, including 12 lifecycle regression checks. New in-engine smoke assertions cover delayed retired-provider replies, replacement and detach without quest effects, but this pass does not claim their native execution. The actual engine/HTTP/authentication contract remains pending. The generated world recipe is unchanged.


## Chapter-menu navigation and settings accessibility pass (28 October 2026)

Chapter practice now remembers whether it was opened from the title, Pause or credits. Its Back button and Escape close path return to the same screen and restore focus to the launching control. Rebuilding the selector retains its origin, while opening it from another screen replaces the old context. Closing title practice cannot release the player into an unacknowledged arrival.

Pause/settings and chapter selection keep a fixed Back control outside their scrollable content. Headings and controls scroll within the remaining space. Changing text size live recalculates both card width and height limits, preserving the slider and its focus instead of rebuilding the screen. Menu scroll areas can shrink in short windows while their contents remain scrollable.

Runtime/editor compilation and 292 standalone assertions pass. Added native assertions cover title/Pause/credits return and focus, arrival protection, live .85-to-1.3 text scaling, settings scrolling and visible Back controls; standalone runs request a 640x480 viewport for those layout checks. The Editor's actual Game-view dimensions may differ. These new native assertions and visual layout have not been executed during this pass because desktop-control tools are unavailable; no new player or rendered acceptance is claimed. No generator recipes were changed.


## Arrival and failed-exit recovery pass (28 October 2026)

Escape at the first welcome screen now returns to the title with the player blocked; it does not silently enter the world with an unacknowledged arrival. The explicit welcome action records arrival and releases the player. An arrival callback from any other screen is ignored. Welcome includes a Back to title action and initially focuses its entry button.

If saving fails while quitting, the game keeps the current visit open and offers Retry save and exit, a context-aware return, and Leave without saving. Repeated failures preserve the original title, title-settings or gameplay-Pause context. Escape follows the same return path; the safe return control receives focus and remains above scrollable failure details/actions. Cancelling a failed title quit cannot bypass the first welcome.

Runtime/editor compilation and 313 standalone assertions pass. The 21 added assertions cover exit navigation, arrival callback ownership and an actual filesystem write failure followed by successful recovery of the same snapshot. New native smoke checks exercise welcome/title/entry, a deliberately failing isolated save codec, repeated failed exits, safe focus and title/settings/Pause recovery, with a new quit-failure capture. Those native checks remain unexecuted because this session exposes no desktop-control tools. No new native Player build, rendered acceptance or world regeneration is claimed; generator recipes are unchanged.
