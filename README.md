# Product & Game Experience

Owns the learner-facing product experience: product concept, world and location design, story, characters, quests, game progression, and UI/UX coordination.

This is the independent Unity game repository in the Paridhwani workspace. The authored source now covers the main story from village arrival through the ridge/farmhouse reunion, original deed, return with Ananya, public council review and epilogue. It includes a separate practice visit that preserves the main completed save. The checked-in revision-3 benchmark has been regenerated in Unity; the existing standalone executable is older. Native benchmark checks and the scripted full-story Editor regression pass. The current art is procedural prototype work.


## Unity 3D implementation plan

See [Unity game plan](docs/UNITY_GAME_PLAN.md) for the project audit, village art direction, proposed scene manifest, complete story sequences, learning flow, separate NLP boundary, and implementation milestones.

## Playable village slice

See [play and build instructions](docs/TEA_STALL_BENCHMARK.md). Open Assets/_Paridhwani/Scenes/TeaStall_Benchmark.unity in the Unity project and press Play.

## Current readiness and verification

Read [current production status](docs/PRODUCTION_STATUS.md) for executed checks and blocked acceptance gates. Runtime/editor source compilation and 259 standalone assertions pass. Desktop Unity generation, benchmark checks and the complete scripted Editor regression pass (6,800 frames, 51 captures), including the corrected library route and long dialogue at 960x540 with maximum text scale. A fresh standalone build and broader human acceptance are still pending.

The project now includes reached-chapter practice, recovery/archival safeguards, title/settings/exit flow, quality and motion preferences, reusable source textures, additive world packaging and an optional cancellable text adapter. Final production art, the real separate NLP service, language review and full in-engine release testing remain outstanding.

From the master workspace root, run `./game/verification/Verify-Sources.ps1` for source/logic checks. In a normal licensed Unity desktop session, use `./game/verification/Verify-Unity.ps1 -Layout Legacy -Build -Smoke` (or `-Layout Additive`) for a fresh build and isolated rendered playthrough.

Use **Paridhwani > Play Isolated Test Visit** for manual testing on a temporary save, or **Run Automated Editor Playtest** for the scripted native regression. Open the project through Unity Hub if a shell launch fails to reach its licensing client.
