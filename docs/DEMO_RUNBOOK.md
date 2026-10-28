# Presentation demo rehearsal

Target: a presentable prototype demonstration within 24 hours of the 28 October 2026 planning discussion. The full story is authored; final character/environment art and a connected NLP service remain outside this demo's verified scope.

## Prepare before the presentation

1. Open `My project (1)` with Unity 6000.6.7f1 through Unity Hub. Use Assets > Refresh and let compilation finish.
2. Open `Assets/_Paridhwani/Scenes/TeaStall_Benchmark.unity`. These presentation changes do not require regeneration. If Unity reports an outdated world recipe, regenerate before rehearsal.
3. Press Play, open Settings, and adjust Display brightness for the actual screen/projector. Its default is +0.7 exposure stops; its range is -0.5 to +1.5. UI text is unaffected by this scene exposure adjustment.
4. From the title choose **Presentation demo / choose a scene**. The main visit is saved first. Demo scenes use separate temporary saves and supply the earlier clues required for their activities. They contain spoilers and do not unlock the main story.
5. Each demo arrival shows its four-step route and current objective. Choose Enter the demo scene; Pause can reopen Review this demo scene's instructions.
6. Rehearse every scene below. On a demo visit, Pause offers **Choose another demo scene** near the top. **Return to the main visit** restores the original main save. Ordinary chapter practice still requires reached chapters.

## Suggested 8–10 minute demonstration

| Segment | What to show | What to say |
|---|---|---|
| Arrival and tea, about 2 minutes | Walk to Manjunatha; use E to talk; greet, order tea/water and ask for the library; briefly open the notebook | Kannada, Roman letters and English can support the same exchange. Local authored choices own story progression. |
| River investigation, about 3 minutes | Choose the River demo scene, talk to Giri, then inspect the bicycle's frame, stand and carrier pocket | Observations, an advance booking and eyewitness testimony establish different kinds of evidence. Incorrect answers offer another attempt. |
| Trunk and original deed, about 2 minutes | Choose the Deed scene; inspect the family trunk; use its twelve-month/seven-day reminder (1207), then review the deed clauses | Language tasks connect to an in-world document, rather than an unrelated quiz. The original still needs interpretation. |
| Library court council, about 2 minutes | Choose Council, speak to the president, and present the original's educational purpose, commercial-use restriction and community beneficiary | Presenting and reviewing evidence changes the story outcome. Scene entry alone does not grant a council decision. |

Keep the explanation of the real NLP engine accurate: this build uses authored dialogue and includes an optional service boundary. A real remote engine is not configured.

## Checks that still require desktop Unity

- Confirm the new title layout and brightness on the actual display at 960x540 and the intended presentation resolution.
- Check the world signs at the tea stall, library, river and council route: complete text, sensible proportions and readable contrast. The automated sign check logs VILLAGE_SIGN_LAYOUT_CHECKED; it does not replace a human readability check.
- Launch all four demo scenes, reopen their instructions, switch between them through Pause, and return to the main visit. Check that its position, clues and chapter unlocks are retained.
- Rehearse movement, interaction, notebook and the complete short route with the input device you will use. Controller work is being developed separately and should be accepted separately.
- Run Paridhwani > Run Automated Editor Playtest. Require a fresh VILLAGE_SMOKE_OK; the older 6,800-frame result predates this batch.
- Run **Paridhwani > Run Presentation Demo Playtest** with the benchmark scene open and other tests finished. It uses an isolated main save and checks all four prepared scenes, guide layout/focus at 1.3 text scale, Pause navigation and main-save restoration. It walks the tea/river routes through CharacterController collisions and invokes authored conversation/evidence actions programmatically, including wrong-answer retries, the trunk/deed and the council decision. It captures the title, guides, arrival views and key activity outcomes. Reports and PNGs are written to `verification/Temp/PresentationPlaytests/run-<timestamp>-<id>`. Require `state: passed` and `PARIDHWANI_PRESENTATION_PLAYTEST_OK`; an interrupted or timed-out run is a failure. The report records actual Game-view dimensions and a SHA-256 fingerprint of the Paridhwani C#, styles, scenes, materials and asset files; it fails if those change during the run. Review the PNGs yourself. This is scripted route regression; human input/usability acceptance, physical controllers and Player builds remain separate checks.
- Run Paridhwani > Build and Run Standalone Playtest from the open licensed Editor. Keep the new successful player and its matching report. Do not present an older build as if it includes these changes.

## Priority for the remaining time

1. First: native compile, the short demo route, readable lighting/UI, and a fresh standalone build.
2. Next: visible art/composition defects encountered along that exact route and any blocking input/collision issue.
3. Freeze features before the final rehearsal; repeat the route on the actual machine and keep a known working build.

Source checks alone do not establish rendered quality or a working Player build. This runbook is a rehearsal procedure, not a claim that the new native checks have passed.

## Latest focused rehearsal evidence (28 October 2026)

The latest focused Editor run `verification/Temp/PresentationPlaytests/run-20261028-092152133-3d51775b/report.json` passed 93 checks at 960x540 with 1.3 text scale and produced 15 PNGs. It covered the four prepared routes and restoration after each. The Arrival presentation snapshot now starts in the open lane facing Manjunatha's tea stall, away from the unrelated timetable prompt; ordinary visits and chapter practice keep their existing spawns. The updated Arrival/world and tea-conversation captures were inspected alongside the earlier guide and activity captures. This confirms those scripted flows and captured layouts, while final art, human input and fluent Kannada acceptance remain open. The existing standalone build predates this small demo-framing change; build again before presenting it as included.

If the command is absent, first finish any current test/build, then use Assets > Refresh. During the recorded recovery, the script had been added during a standalone build: the Player compiler response contained it while the Editor response and assembly stayed stale. A normal edit to the existing presenter-guide source followed by Refresh rebuilt the Editor assembly and exposed the command. The precise cache-invalidation cause is unproven. Do not restart, delete caches or regenerate the world solely for this tooling change.
