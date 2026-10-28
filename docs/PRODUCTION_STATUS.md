# Current production status

The main story is authored from arrival to epilogue. The revision-3 benchmark world has now been regenerated in Unity 6000.6.7f1, including all story locations. The game remains a procedural prototype and is not release-ready. Historical reports in TEA_STALL_BENCHMARK.md describe older revisions.

## Verified implementation

- Seven 512px surface PNGs import as Texture2D. The import blocker was caused by Cubemap texture shape as well as obsolete metadata; explicit Texture2D preprocessing and native-upgraded metadata preserve existing GUIDs. Generation checks every import before replacing a scene.
- Native benchmark generation and Run Benchmark Checks passed on 27 October, and native checks passed again on 28 October. These exercise story gates, Unity JSON save/recovery, fourteen NPC instances, scene references and shader checks.
- Runtime/editor source compilation, runtime branches without Editor/diagnostic defines (using cached references), and 259 standalone assertions pass. These standalone checks are not a native Player build.
- Direct Editor playtesting confirmed title, arrival handoff, rain/world rendering and the isolated test HUD. Automated testing exercises TPP/FPP transitions, evidence tasks, save/restore, river walking and temple witnesses. The previous run stopped at the library veranda approach; a complete latest-revision run is still pending.
- World signs have a UI theme and literal text. Generated asset names match their filenames. Dedicated manual and automated Editor test menus use temporary saves and do not migrate a player's visit.

## Engine test workflow

Open the project through Unity Hub. Use **Paridhwani > Create Tea Stall Benchmark** after changing generator recipes, then **Run Benchmark Checks**. **Play Isolated Test Visit** opens the normal title/arrival flow on a temporary save. **Run Automated Editor Playtest** runs the scripted native regression in the Game view and returns to Edit mode on completion/failure. The HUD labels test visits. The automated sequence uses real CharacterController movement for authored walking routes, while conversation/activity actions are invoked programmatically; this does not replace keyboard/controller acceptance testing.

Automated test captures and result.txt or failure.txt are written below the OS temporary directory at ParidhwaniSmoke/run-<timestamp>. Success requires VILLAGE_SMOKE_OK; reaching an intermediate screenshot is not a pass. Editor output also records PARIDHWANI_EDITOR_SMOKE_EXIT (0 success, 2 failure).

On 28 October, a shell-launched Editor again refused the licensing IPC connection. Launching the same project through the desktop Unity Hub succeeded. This demonstrates a launch-context limitation, not an invalid Unity license; its exact underlying cause is unproven. Desktop computer access is working in the current session.

From the master workspace root:

    ./game/verification/Verify-Sources.ps1

With this project's Editor closed, a normal desktop shell can run the fresh-build workflow:

    ./game/verification/Verify-Unity.ps1 -Layout Legacy -Build -Smoke
    ./game/verification/Verify-Unity.ps1 -Layout Additive -Build -Smoke

No fresh standalone Player build or additive-package run has been verified in this pass. Both additive worlds currently load at startup; distance-based scene streaming is not implemented.

## Remaining acceptance and production work

| Area | Implemented | Still required |
|---|---|---|
| Story and replay | All 15 beats, evidence gates, return, endings, reached-chapter practice | Complete fresh/resumed native regression and replay spawn checks |
| Persistence and session | Atomic recovery/archive, transactional restore, main/practice isolation, title/settings/exit | Runtime failure cases and full menu/input acceptance |
| Movement and audio | Input focus policy, normalized movement, tagged ground footsteps and independent mixes | Resolve walking failures, hardware input and listening review |
| Conversation and accessibility | Scrollable literal replies, original-line restore, text scale, reduced motion, language support | Short-window native layout, Kannada shaping and comfort review |
| World and rendering | Regenerated prototype locations, seven surface maps, quality profiles and culling | Production characters/rigs, terrain, vegetation, textures, animation, composition and authored audio |
| Packaging and performance | Optional additive exporter/loader, profiling tools | Execute additive export/build, frame-time/memory measurements and release packaging |
| NLP | Optional cancellable text-only adapter | Connect the separately built real service once its contract is supplied; see NLP_UNITY_BOUNDARY.md |
| Language and play quality | Authored Kannada-learning tasks and source gates | Fluent Kannada/regional review and player usability testing |

The implemented story source does not establish final art quality, complete engine verification or a finished game percentage.
