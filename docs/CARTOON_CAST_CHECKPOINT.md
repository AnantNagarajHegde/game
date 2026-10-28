# Approved cartoon cast rollout — 28 October 2026

The corrected player/Manjunatha sample was verified and committed as `5efeac2`. The user then approved continuing the same style. `CartoonCastLooks.cs` supplies original procedural variations for the remaining cast; `CartoonCharacterSample.cs` retains the approved proportions, shader and transform animation rig. The primitive character fallback is removed. This is a prototype art milestone, not a completed character-production pipeline.

| Identity | Visible treatment |
|---|---|
| Somanna | Grey hair/moustache, glasses, light work shirt, muted trousers, restrained facial lines |
| Shanthakka | Earth-toned sari/blouse, tied bun, border/drape and bindi |
| Basavaraju | Sage work shirt, tan apron, dark trousers and moustache |
| Giri | Desaturated green shirt, clean-shaven face and dark trousers |
| Gurumurthy | Greying hair/moustache, cream clothing and shoulder towel |
| Rathnamma | Grey bun, mauve sari/blouse and elder face details |
| Shresthi | Pale blue-grey office shirt, glasses, moustache and shirt pocket |
| Raghavendra | Khaki/olive work clothes and moustache; both driver instances share his look |
| Kencha | Khaki uniform palette, belt and small brass badge |
| Ananya | Mustard kurta, green trousers/bag, scarf, tied hair and younger adult face |
| Kamalamma | Silver bun, muted earth sari, elder face and distinct palette from Rathnamma |
| Council president | Greying hair/moustache, spectacles and pale clothing |

Including Manjunatha, these are 13 NPC identities across all 14 role instances. Root names, role enums, quest IDs, display names, eye bindings and the 1.66 m head pivot are retained. NPC root capsules remain 1.8 m high with radius 0.28 m; new mesh parts have no colliders. Runtime input, save and NLP implementations are unchanged. Both legs and arms remain bound to the existing transform animation system. Saris and the kurta are simple procedural garments; no fabric simulation, facial/lip-sync rig or production cloth-animation acceptance is claimed.

## Native verification and review

Source compilation and the 387 existing assertions passed in `verification/Temp/cartoon-cast-source-rerun.log` and after the mesh refresh change in `verification/Temp/cartoon-cast-mesh-refresh-source.log`. An earlier invocation compiled successfully but failed the existing isolated file-store assertion `recovery writes fresh primary`; an immediate unchanged rerun passed. The cause is unestablished. Preserve `cartoon-cast-final-source.log` alongside the passing rerun; this was not a failure in the character mesh checks and no save implementation was changed.

The first cast renders exposed unintended pale footwear on grey-haired characters and a visible kurta waist seam. Footwear now has its own dark material; Ananya's kurta is one continuous torso mesh. Initial long rear camera offsets entered the farm/mess walls; the revised rear hair views use the available space, and side portraits frame the head closer. These are staged poses of the real actors with the actual game camera, not concept art. Normal gameplay/conversation views remain separate references.

A further real-render check found the serialized kurta bounds had changed while the loaded renderer still showed its earlier outline. Character mesh replacement now uses explicit vertices/normals/triangles/bounds setters and `UploadMeshData(false)` rather than only `CopySerialized`. The next actual capture showed the intended continuous garment. This is scoped to character meshes; the terrain/general asset save helper is unchanged. Unity documents the immediate graphics upload in [Mesh.UploadMeshData](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Mesh.UploadMeshData.html). All 67 approved player/Manjunatha mesh files remained byte-identical after regeneration.

Final native run `verification/Temp/PresentationPlaytests/run-20261028-173817275-2397e932/` passed 100,888 checks (primarily per-vertex/triangle validations, not gameplay scenarios) with 51 PNG captures at 960x540. All 28 remaining-cast portraits, ordinary arrival/conversation frames and the four world-route frames were inspected. Usable local preview copies are in `verification/Temp/CartoonCastPreview-20261028/`. Shanthakka's rear hair image is tightly framed because the actual actor stands near the mess wall. Front views behind counters intentionally show actual occlusion; they do not establish hidden garment acceptance.

The existing two-vendor native run after the model rollout passed 4,256 sampled checks in `verification/Temp/VendorNative/20261028-172434040/`: two occupational cycles per vendor, physics queries, conversation/notebook/Pause/resumption, isolated save/load and finale staging. Market work and conversation captures were inspected. The subsequent kurta-only geometry correction does not change those two vendors. The main save SHA-256 remained `FE156A5D14BE07CA28C8E15F725151B7A8AECF911FB8A9F4CE6A41FABBE40DFA`. Exact evidence is also in `verification/cartoon-cast-check.txt`.

Generated scene/material/mesh outputs remain local with pre-existing dirty work. The commit contains the deterministic source and evidence, not a wholesale scene/index capture. Regenerate the benchmark to reproduce the cast. No push, new standalone build, broad physical-controller acceptance or fluent Kannada review was performed. Screenshot Library delivery remains blocked as described in `CHARACTER_STYLE_SAMPLE.md`; do not invent IDs or cloud URLs.

## Environment consistency review started

The cast portraits also expose the remaining environment contrast. Tea-stall ground has shaped mud/plinth/drain surfaces; library/mess/market and more distant locations retain flat rectangular floors, sharp slab edges and repetitive material coverage. Repeated spherical canopies and very thin palm trunks remain conspicuous across the village. These are visible art gaps, not performance findings.

Next bounded graphics batch: carry the tea-stall stone-edge/threshold language to the library and mess verandas and the market approach, using WetEarth/WeatheredStone and deterministic non-colliding surface geometry. Preserve the existing walking surface, drain crossings, vendor reach and safe route widths. Capture matched arrival/conversation views for each frontage. After that, review temple/river, checkpost/ridge/farm and finale/epilogue groups as listed in `CHARACTER_STYLE_SAMPLE.md`; the village-wide pass is not signed off by this cast milestone.
