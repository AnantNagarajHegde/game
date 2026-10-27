# Current production status

The main story is authored from arrival to epilogue. This is the current status; older iteration reports in TEA_STALL_BENCHMARK.md are historical. The game is not release-ready and the latest generated world/player has not been produced in this session.

## Implementation delivered

| Area | Implemented | Acceptance still required |
|---|---|---|
| Story | All 15 major beats, evidence gates, return, both endings | Fresh/resumed full Unity playthroughs |
| Session | Title, continue, archived new story, save-aware exit | Native menu/layout and archive review |
| Persistence | Atomic writes, backup recovery, unreadable-file preservation, transactional explicit restore, main/practice isolation, chapter actor reset | Unity JSON serializer and runtime failure tests |
| Replay | 15 reached-chapter practice checkpoints | In-engine spawn/collision and staging checks |
| Accessibility | Roman/English support, text scale, reduced camera motion, inverted look, volume | Kannada shaping, keyboard/UI and comfort review |
| Input and movement | Text-entry shortcut protection, modal Tab traversal, one command per frame, normalized input and actual movement speed | Native focus/input/collision regression checks |
| Audio | Ground-tagged earth/stone/timber steps, grounded distance cadence, separate environment/footstep mix | Listening review, original production audio and in-engine contact checks |
| Rendering | Runtime Low/Balanced/High profiles, distance culling, seven periodic material maps, restrained wind | Shader compilation, representative visual/performance capture |
| Characters | Corrected female variants; procedural idle/work/talk/follow motion | Production models/rigs, animation and visual review |
| Scene packaging | Optional persistent Bootstrap + village + forest/farm additive exporter/loader | Execute export, bind/load validation and modular smoke player |
| Dialogue boundary | Optional text-only delegate, cancellation, timeout, stale rejection, authored quest authority | Actual separate NLP service/contract and language behavior |
| Verification | Source, storage, texture, editor/built-player checks and profiling scripts | Successful licensed Unity build and rendered execution |

The surface maps, synthesized footsteps and procedural rigs improve the prototype. They do not establish the intended final Karnataka village art standard. Production models, terrain/vegetation refinement, authored audio and final scene composition remain unfinished.

## Executed checks

- All runtime/editor C# sources compile against the installed Unity 6000.6.7f1 references. Existing Unity build-variant deprecation warnings remain.
- 259 executable assertions pass, covering story gates, retries, endings, storage recovery/archive/restore I/O, chapter seeds, input routing, movement input, footstep synthesis/cadence, companion paths, preferences, binding policy and optional-dialogue failure/cancellation behavior.
- Storage I/O uses an injected minimal JSON codec in standalone tests. This does not claim Unity JsonUtility coverage; those tests are authored for the Editor/player.
- Seven 512px RGB texture sources pass size, variation and opposite-edge checks. GPU shader import/rendering has not been executed.
- The native verification PowerShell script parses successfully and was actually retried on the latest source. It correctly reported failure rather than success.
- Local commits are created after passing source checks. No GitHub push was attempted in this pass.

## Blocked engine gate

The latest real Editor retry connected to Package Manager, timed out after 60 seconds waiting for licensing, then crashed before compilation. The Windows computer-use helper also failed to launch its app server. This session therefore cannot claim scene regeneration, a new Windows player, native serialization/collision/camera checks, rendered Kannada acceptance or measured performance.

The checked-in scene/build predates the latest source. The revision-3 schema/recipe guard stops a stale world before reading or changing a visit. Regenerate before playing.

From a normal desktop shell at the master repository root, with this project's Editor closed:

    ./game/verification/Verify-Unity.ps1 -Layout Legacy -Build -Smoke
    ./game/verification/Verify-Unity.ps1 -Layout Additive -Build -Smoke

`-Smoke` explicitly opens a visible isolated-save test player after a successful fresh build. Logs and JSON result reports are written under the Unity project's Temp/UnityVerification directory. Only newly built players are accepted by this workflow.

Source checks can run here or locally:

    ./game/verification/Verify-Sources.ps1

An opt-in development player `-village-profile` records frame intervals and memory after gameplay starts. It has not produced an accepted performance result here. Both additive worlds currently load at startup; distance-based scene streaming is not implemented or claimed.

## Outstanding work that is not complete

1. Run the regenerated game in Unity, fix any discovered scene/physics/UI/shader/save defects, and complete regression testing.
2. Finish and approve the representative tea-stall art benchmark, then bring all locations/characters/terrain/vegetation/animation/audio to that standard. The current procedural art is not final production art.
3. Integrate the real separately built NLP service when its contract, implementation and allowed-fact policy are supplied. The optional adapter is not that service. See NLP_UNITY_BOUNDARY.md.
4. Obtain fluent Kannada and regional review; validate shaping, teaching usefulness, pacing, accessibility and camera comfort with players.
5. Measure CPU/GPU/frame-time/memory behavior on agreed hardware, tune accordingly, then perform release QA and packaging acceptance.

These gates prevent an honest claim that every pending implementation/production cycle is complete. The remaining work is not hidden behind a story-source completion percentage.


## Latest local implementation pass (27 October 2026)

Three cycles address text-entry safety and real movement speed; grounded surface audio and per-channel volume; and safe restore, NPC staging and companion-path lifecycle. The story content is unchanged. The native smoke runner now acknowledges arrival, verifies restored actor origins, and checks the new volume channels through Unity serialization. These native assertions are authored, not executed here. Source compilation and 259 standalone assertions passed after the final changes. No percentage increase is claimed without completing production/engine gates.
