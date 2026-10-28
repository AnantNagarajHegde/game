# Library, mess and market surface consistency — 28 October 2026

This bounded pass extends the inspected tea-stall stone language to three nearby public frontages. It uses the existing WeatheredStone/WetEarth resources and Village Surface shader; no new external art, software, purchase, global lighting or material change is involved.

| Location | Visual change |
|---|---|
| Library veranda | Staggered .69 m stone paving, recessed joints, front/side facing and a damp soil strip; bench, posts, door and drain remain in place |
| Mess | .64 m paving and plinth facing; matching stone finish on the existing drain crossing, with the serving space and approach intact |
| Market | .72 m paving and floor-edge facing; weathered exposed approach stones, retaining the stall's crates, scales and ledger |

The new `VillageFrontageArt.cs` runs immediately after the market builder. Its separate random seed leaves all other placement unchanged. Existing floor/step/crossing renderers are hidden and their colliders and footstep tags retained. New visual meshes have no colliders. Slab tops differ from the physical walking surface by only .002 m. The surface materials are private clones; character meshes, identity data, runtime controller/NLP code and save schema are untouched.

## Actual before/after evidence

- Before: `verification/Temp/PresentationPlaytests/run-20261028-175201706-d583b346/`, passed 22 focused checks.
- Final after: `verification/Temp/PresentationPlaytests/run-20261028-180510144-0c64d149/`, passed 6,042 checks, primarily finite mesh-vertex checks plus the targeted behaviours below.
- Both produced 10 PNGs at **960x540**, brightness .9, text scale 1.3, using identical nominal player targets and game-camera angles. Rain and NPC motion are not pixel-identical. All nine frontage views in each run were inspected.
- Unmodified preview copies: `verification/Temp/FrontagePreview-20261028/Before/` and `After/`. Compare `Frontage-Library-approach.png`, `Frontage-Library-threshold.png`, `Frontage-Mess-threshold.png` and `Frontage-Market-threshold.png`; each location also has a conversation image.

The focused menu **Paridhwani > Run Frontage Art Playtest** uses an isolated visit. It walks each actual CharacterController across its local threshold, verifies the expected nearby NPC and opens the real conversation. It checks all four visual floor/crossing bounds against the live physical floor, original collider retention, absence of new colliders, finite visual vertices, isolated save/load, return to the isolated main visit, and stable source/asset digest. It does not claim physical-controller playtesting or a full-story rerun.

## Corrections and verification limits

The first generated after-run (`run-20261028-175922340-ae71a8bc`) technically passed its then-existing route checks, but its images failed inspection: newly created Edit-mode colliders reported stale unit-cube bounds, so replacement stone geometry appeared at the origin and the intended floors were hidden. This was not accepted as an art pass. The generator now derives bounds from the authored axis-aligned transforms and collider sizes; new native checks verify footprint, height and centering. The final images confirm paving at the intended frontages. This illustrates why assertions are not visual acceptance.

Source compilation and **387 existing assertions passed** in `verification/Temp/FrontageArt-20261028/bounds-fixed-source.log`. An initial Color/Color operator compilation error was corrected before native generation. The earlier intermittent file-store assertion `recovery writes fresh primary` from the cast batch remains unresolved evidence; its failure and passing unchanged rerun remain in `cartoon-cast-final-source.log` and `cartoon-cast-source-rerun.log`. Passing runs in this art batch do not establish a cause or erase it.

All **516 `Cartoon*.asset` mesh hashes** matched the pre-pass manifest. The user's main save hash remained `FE156A5D14BE07CA28C8E15F725151B7A8AECF911FB8A9F4CE6A41FABBE40DFA`. The pre-pass scene copy and cast manifest are in `verification/Temp/FrontageArt-20261028/`.

The close library conversation pose crops Somanna's face in both before and after images. That pre-existing camera-distance issue was recorded, not changed in this surface batch. The mess/market conversations remain readable. No fresh standalone, exhaustive human input/controller pass, full-story/vendor-cycle rerun, performance acceptance or fluent Kannada review was performed. Generated scene/material/mesh outputs remain local alongside shared dirty work; only scoped source and evidence are committed. Regenerate the benchmark to reproduce the pass.

## Remaining visible mismatches / next batch

1. Repetitive spherical banyan/distant canopies and uniformly thin palm trunks remain conspicuous next to the improved stonework. Next bounded graphics candidate: refine one hero banyan crown/branch silhouette and the nearby palm line with original procedural meshes, keeping trunk colliders, paths, landmarks and camera clearance.
2. Distant ground/foothill masses remain coarse ellipsoids with sparse transition detail; inspect river/checkpost sightlines before a terrain/fog/vegetation pass. Do not treat these current prototype vistas as finished village art.
3. Temple/river, office/checkpost, ridge/farm and finale/epilogue frontage consistency is still pending. These three areas are a local extension, not village-wide acceptance.
4. The close library conversation framing deserves a separate bounded camera fix rather than being mistaken for a character or paving defect.

Preview Library delivery remains blocked by the previously established unavailable `prepare_uploads` capability. No upload retry or alternate route was attempted; no Library IDs exist. Local PNGs are ready for supported transfer when capability changes. No push or security change occurred.
