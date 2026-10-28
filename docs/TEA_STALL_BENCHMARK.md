# Paridhwani village prototype - main story source

The source now contains the main story from arrival through the later council review and epilogue, with generated village/river, checkpost/ridge and farmhouse environments. It includes third-person exploration, first-person conversations, evidence tasks, notebook, settings, saved progress and a separate practice visit. The latest source has not yet been regenerated, built or visually verified in Unity. Characters and environments remain procedural prototypes; final art, animation, fluent Kannada review, external NLP integration and release QA are pending.

## Open and play

Open game/My project (1) with Unity 6000.6.7f1. Open Assets/_Paridhwani/Scenes/TeaStall_Benchmark.unity and press Play. The same scene now contains all five locations. Reopen the scene after updating assets if it was already loaded in the editor. If generated assets are absent, choose Paridhwani > Create Tea Stall Benchmark.

The generation menu rebuilds the generated benchmark assets and scene. Preserve manual environment edits in a separate scene copy. SampleScene is retained; the benchmark is first in Build Settings. Separate additive location scenes remain a later production step.

A Windows development player is generated at My project (1)/Builds/TeaStall/Paridhwani.exe. The executable needs its accompanying data folder and Unity libraries; do not move the EXE by itself.

## Controls

| Input | Action |
|---|---|
| WASD / Shift | Walk / jog |
| Mouse | Orbit the third-person camera, including looking up |
| E | Interact with a nearby, visible NPC or sign |
| 1–3 / pointer | Choose a conversation response, evidence answer, or bicycle part |
| Tab | Open/close notebook; return to the same conversation if opened during dialogue |
| Escape | Leave conversation, close notebook, or pause |
| F5 / F9 | Save / restore this visit |

Approach the tea stall on the left and look roughly towards Manjunatha. A prompt appears within speaking distance when no wall blocks the view. Greet him, order tea or water, and ask for the library. Both drinks are valid; declining tea gets a polite clarification.

Walk up the lane and left to the library veranda. Somanna stands outside. Introduce yourself and ask politely about the door or Ananya. He explains the master/duplicate keys and directs you to Shanthakka across the lane. The reading-room interior remains closed.

Return along the lane to the mess veranda on the opposite side. Request a meal or water, practise “a little” and “enough”, and ask about Ananya. Receive her shopping note, then ask who can help interpret it. This establishes Basavaraju at the market as the next lead. Continue to Basavaraju at the market, farther up the opposite side of the lane.

The objective and a short distance/location hint update as you progress. A first-visit introduction explains the controls and purpose. English and Roman assistance remain available throughout.

## Conversation camera and UI

Every VillageNpc supplies an eye anchor to the same camera system. Entering conversation eases from the current third-person orbit to the player's actual eye position over roughly 0.65 seconds. It frames the speaker above the dialogue panel; it does not teleport the player towards the NPC. The player's head/body stop drawing near the camera while their shadows remain.

Leaving eases back to the previous exploration heading and pitch. Locomotion waits for that transition to settle. Collision checks cover the interpolated path. Refocusing the game or locking the pointer briefly discards mouse movement so the camera does not jump.

Opening the notebook preserves the NPC and turn. Closing it resumes that conversation and first-person framing. Assistance toggles redraw the text without restarting the conversation/camera. Exploration objectives/control hints are hidden while a modal is open.

## Roof and sky correction

- Clay materials are matte with very limited wet highlights.
- The sky is an authored overcast gradient with no bright sun disc.
- Tone mapping and colour effects are persisted as real profile subassets, rather than empty references in the built player.
- Bloom is disabled for this benchmark.
- A stable ambient probe lights unbaked shaded spaces.
- Palm fronds use connected curved ribbons instead of disconnected triangles.
- A generated renderer copy keeps benchmark rendering choices separate from the original PC renderer.

## Saves and compatibility

Progress, assistance/settings, position and heading save periodically, on important exchanges, focus loss/pause/quit, and manually. The file is paridhwani-tea-v1.json beneath Application.persistentDataPath, with a previous-save backup.

The product is now named Paridhwani. On first launch, if the new location has no save, a valid visit from the old “My project (1)” product directory is copied forward; the old file is kept. New chapter fields default to false in old saves. Corrupt primary saves can recover their backup. Unsupported schemas fail safely.

Tests use unique temporary saves and do not modify the player's visit.

## Regenerate, validate and build

- Paridhwani > Create Tea Stall Benchmark: generate editable scene, meshes, materials, lighting and NPCs.
- Paridhwani > Run Benchmark Checks: validate help/decline/revisits, all three NPC progression paths, unheard clue gates, saves/recovery, references, atmosphere subassets, and shaders.
- Paridhwani > Validate and Build Benchmark: regenerate, validate, and build the Windows development player.

From the game directory, using the installed Unity CLI:

    unity run "My project (1)" -- -executeMethod Paridhwani.Editor.TeaStallSceneBuilder.GenerateAndCheck
    unity run "My project (1)" -- -executeMethod Paridhwani.Editor.TeaStallSceneBuilder.ValidateAndBuild

Close this project's editor before launching another editor instance for a batch build.

A development player also accepts -village-smoke. Run it in a visible window for real rendering checks. It uses an isolated save, captures the game-rendered roof/sky, conversations and notebook to a unique temporary folder, checks intermediate and final camera positions, face framing, body visibility, notebook resume, all three NPCs and saved progress, then exits. It rejects missing/blank render captures. This opt-in check is compiled only for editor/development builds.

## Code and content ownership

- VillageWalker: third-person movement and shared first-person transition.
- VillageNpc: NPC identity, role, eye anchor and provider selection.
- TeaStallSession: interaction focus, UI lifecycle, save data and consequences.
- LocalTeaConversation / VillageChapterConversation: authored local content behind ITeaConversationProvider; no NLP or external APIs.
- TeaStallUI / VillageSign: advanced text shaping for UI and physical signs.
- VillageLighting / TeaAmbience: stable overcast fill, generated rain/roof audio and footsteps.
- TeaStallSceneBuilder / VillageNextPhaseBuilder: editable environment generation and build.
- TeaStallChecks / VillageSmokeCheck: state, recovery, rendering and camera regression checks.

Font sources/licence: THIRD_PARTY_NOTICES.md and Assets/_Paridhwani/Art/Fonts/OFL.txt. No third-party character, environment or audio packs were added.

## Current limits

The five locations share one benchmark scene. Temple/northern-stop testimony, the library interior and the remaining mystery are not implemented. This prototype does not claim production character quality, complete village authenticity, voice/lip-sync, Kannada fluency, a final curriculum, release-ready save migrations, or measured minimum-spec performance. The language engine remains separate and API design remains deferred.

## Earlier tea/library/mess verification

- Unity 6000.6.7f1 compiled the sources, regenerated the scene, and produced the Windows development player successfully.
- Editor regression checks passed for tea/help/decline/revisits, library and mess progression, unheard clues, save round-trips/recovery, NPC references, persisted atmosphere effects and shader compilation.
- The visible built-player smoke check passed across 1,226 advancing frames, using an isolated temporary save. It verified intermediate and settled TPP/FPP camera states, framing above the dialogue panel, body visibility, notebook resume, all three NPC roles, the shopping-note sequence and save restoration.
- Actual game-rendered village, upward roof/sky, three conversation and notebook captures were inspected. The tested village/sky views each had approximately 0.019% fully clipped white pixels, with no large roof/sky glare visible. This is a check of those views, not a hardware performance benchmark.
- Unity still reports unused depth-of-field/Panini shader warnings in the player log; this scene does not enable those effects.

## Market chapter

Show Ananya’s shopping note to Basavaraju. Choose the reply to read his ledger excerpt. Interpret Monday evening at 18:00, then return to the same conversation and ask about the journey. The record of a purchase is kept separate from the witness’s account of intended travel.

Return to the timetable beside the tea stall. It is readable on an early visit, but it grants no journey clue until you have Basavaraju’s account. Match Route 14, Tuesday morning at 06:45; wrong answers show a contextual hint and allow retry. Read Giri’s pinned notice afterward to complete the market chapter. That report is labelled unverified; neither the timetable nor the notice proves boarding, an accident or Ananya’s whereabouts.

The ledger and timetable support Kannada, Roman and English layers. The notebook preserves source-linked facts and lets you revisit the ledger excerpt and an interpreted timetable. Closing an evidence panel during a conversation resumes the relevant NPC turn and first-person framing. Old version-1 saves remain readable; new market flags default to false.

MarketInvestigation owns the authored task rules; MarketInvestigationUI presents the documents; MarketSceneBuilder creates the market and public timetable. No external NLP/API was added.

## How the graphics will change

The current primitive characters, canopy vegetation and much of the architectural geometry are prototype assets. The intended final finish is grounded Karnataka village realism with practical simplification. Dedicated art passes will replace character models/rigs and vegetation, add a consistent vernacular building kit, improve layered earth/plaster/tile materials, and refine lighting, animation and sound. The first complete art pass should finish the tea stall and adjacent lane as the visual standard before applying that quality everywhere.

NPC identity, eye anchors, interaction rules and saved evidence are separated from visual meshes so better art can replace these models while preserving gameplay. Final asset selection, licensing, production time and performance still need to be established; the present look is not the final art benchmark.

## Market iteration verification

- Unity compiled and rebuilt the Windows development player successfully.
- Editor checks passed for four NPCs, earned evidence, public/early timetable visits, incorrect answers, repeat reads, source gates, saved market progress, scene references and non-cancelling fabric/foliage normals.
- A visible standalone playthrough passed across 1,709 advancing frames with an isolated save. It reached Basavaraju through the normal interaction prompt, read the ledger with an incorrect and correct answer, resumed his first-person conversation, visited the world timetable, retried a wrong service, read Giri’s notice and restored the resulting progress.
- Thirteen game-rendered captures were produced; market, receipt, timetable and notice views were inspected. The receipt hint and return action stay visible, and the canopy no longer has the cancelled-normal shading artifact.
- Tested village, upward sky and market views each had about 0.019% fully clipped white pixels. This is a visual regression check, not a minimum-hardware benchmark.

## River chapter

After reading Giri’s pinned notice, follow the RIVER PATH sign west from the central lane. The fenced path leads to a sheltered repair shed on the safe river bank. Town return signs point back through the same gap. The animated weir sits below the bank, beyond a visible railing.

Talk to Giri, introduce the notice, then ask to examine the bicycle. The camera eases from NPC first person to a view of the actual 3D bicycle without moving the player. Inspect the frame, deployed stand and pocket below the rear carrier. Use the spatial word for inside to retrieve the dry slip. Wrong answers give a hint and allow retry.

The slip is an advance booking issued Monday for Tuesday’s 06:45 Route 14 trip, not a used boarding ticket. Read it, return to the bicycle, then return to Giri and compare what the observations prove. The parked, undamaged-looking bicycle and advance booking weaken the accident rumour; they do not prove boarding or Ananya’s location/safety. Giri then suggests Gurumurthy and Rathnamma by the temple/northern stop as potential witnesses. Their chapter is future work.

Tab during inspection or document reading returns to that exact evidence activity when closed. Nested booking pages return to the bicycle first, then to Giri. Notes preserve sources and distinguish observation, intent and inference. Repeated inspections do not duplicate the booking; the physical paper disappears from its pocket once collected.

River positions and flags save using the existing version-1 format. Earlier saves remain readable with new fields defaulting to false. The supported position bounds now include the river bank.

The shed uses restrained bounced daylight and the tiled roofs have separate underside geometry so looking upward from shelter shows a solid roof.

RiverSceneBuilder creates the route, barriers, weir, shed and bicycle meshes; RiverInvestigation owns clue prerequisites and bounded deductions; RiverInvestigationUI presents the inspection/booking/tasks; RiverAmbience supplies a spatial current sound that respects ambience mute. The shared camera now interpolates from its actual current pose for exploration, conversation and inspection transitions.

Interactive regeneration preserves a dirty scene copy under Assets/_Paridhwani/Scenes/ReviewBackups before replacing generated scene content. No external NLP/API, art pack, or speech dependency was added.

## Temple / northern-stop iteration

Source implementation adds a northern lane, public tiled temple forecourt, low steps, flower table, stop board, and Gurumurthy and Rathnamma. Regenerate using **Paridhwani > Create Tea Stall Benchmark** before playing this iteration. The previously generated scene and executable do not yet contain this extension.

After Giri supplies the temple lead, speak to each witness in either order. Gurumurthy saw Ananya come alone towards the stop; Rathnamma reports seeing her board Route 14. Compare the two accounts; a wrong inference offers a vocabulary hint and retry. Ask Rathnamma about the family name to earn the Gudda Mane lead. Notebook entries retain each source and distinguish boarding from arrival and present safety. Return visits repeat testimony. The existing NPC camera and notebook suspension apply to both witnesses.

New save fields default to false in earlier version-1 visits; supported saved positions include the northern forecourt. Editor checks now cover early visits, separate sources, wrong-answer retries, the unheard family gate, and saved temple facts/position. These new checks have been authored but have not been executed in this session.

Verification limitation: the Unity CLI refused batch execution because it detected an open project. No accessible running Editor/Pipeline instance was found; direct batch launch exited without creating a log. Compilation, scene regeneration, Windows rebuild, walking clearance, and rendered temple/camera review therefore remain unverified. Existing build success reports above apply to earlier iterations only. Kannada text needs fluent-speaker review; temple characters and environment remain procedural prototypes. The library archive continuation is not implemented.

## Archive revisit iteration

After earning the Gudda Mane family lead from Rathnamma, return to Somanna. Ask him to open the reading room: the master-key door is removed from the walking route and this access is saved. The library silhouette is replaced by an interior under its original roof, with a catalogue desk, two labelled cupboards, and restrained bounced light.

Read the catalogue with Somanna or approach the desk inside and press E. Find the second cupboard using `eradane` / `olage`, then distinguish the original deed from a catalogue reference. Wrong answers give hints and permit retries. The reference says the 1952 original is held by Shankare Gowda's family at Gudda Mane; it establishes a research target, not Ananya's arrival or a final legal outcome. Reopen it through the notebook after access is granted. Tab returns to the exact reading activity; closing evidence resumes Somanna's relevant turn.

Somanna offers rest after the reference is understood. Declining keeps Wednesday active. Accepting saves Thursday and shows a guest-room interlude with an explicit Continue action. This is a text interlude, not a new bedroom level or a completed time-of-day lighting system. Thursday points to the panchayat notice, whose office/chapter is not yet implemented. New archive save fields default to false for older version-1 saves.

### Verification on this iteration

- Retried real Unity batch generation/checks. Package Manager connected, but licensing timed out after 60 seconds and Unity crashed before compilation. The diagnostic log is `game/temple-verification.log`; no new scene or Windows player was produced.
- Compiled all Paridhwani runtime and editor-generator sources using Unity 6000.6.7f1's bundled Roslyn compiler and the project's cached compiler references. Compilation passed with existing deprecation warnings. This is a separate source check, not an Editor import/build success.
- Executed 36 logic assertions against the actual compiled runtime classes: both witness orders, help/early visits, independent sources, wrong inference retries, library access, shelf/reference prerequisites, explicit rest acceptance/decline, and revisit availability. Passed.
- Added Editor checks for archive prerequisites and rest, and extended the isolated-save built-player smoke sequence through both temple witnesses, the archive doorway/desk, notebook return, FPP conversation, rest interlude, and save restoration. These engine-dependent checks are authored but could not execute in this session. Save serialization, room/path clearance, camera framing, Kannada rendering and final art remain unverified for this iteration.

To reproduce the available source/logic checks from the repository root:

    ./game/verification/Verify-Sources.ps1

It uses the installed editor's compiler and existing Library/Bee response files, writes temporary outputs under the Unity project's Temp folder, and never launches Unity or touches the player's save. The runner is `game/verification/ChapterChecks.cs`. It does not replace the real Editor checks.

Before playing this updated chapter, regenerate with **Paridhwani > Create Tea Stall Benchmark**, then run **Paridhwani > Run Benchmark Checks** or **Validate and Build Benchmark** in a working licensed Editor. The shipped/generated scene and executable remain from the earlier iteration until regeneration succeeds.

## Panchayat notice iteration

After accepting rest and entering Thursday, follow the northern lane to the panchayat public counter east of the temple. Read the public board with E or talk to Shresthi. Early visitors can read the displayed notice, but investigation facts require the archive reference and Thursday progression.

Interpret the named community library, Friday at 09:00, and the required original ownership document. Each wrong answer supplies a contextual hint and allows retry. Compare the library catalogue to the checklist: the family's original deed at Gudda Mane is needed for council review. The notice is a posted clearance claim, not a final ownership ruling, and submitting a document does not guarantee protection. The public route advisory supplies the forest checkpost lead without requiring optional questioning of the clerk. There is no real-time countdown while reading or exploring.

The notebook saves the notice source, deadline, required deed and safe-route lead and supports reopening the notice. Notebook suspension and return to Shresthi use the shared modal and TPP/FPP camera system. The public counter contains files, a stamp, a proposed warehouse drawing, notice board, tiled shelter and damp masonry. Its geometry is generated alongside the other benchmark locations. The checkpost journey is not implemented yet.

Source validation passed using `verification/Verify-Sources.ps1`: runtime/editor compilation and 48 behavior assertions. The development-player smoke sequence now includes the independent public-board approach, retries, notebook return, Shresthi's FPP interaction and evidence save/restore. Those engine-dependent tests remain unexecuted because this session cannot initialize Unity licensing. Regenerate the benchmark in a working Editor before playing these source changes; no new Windows build or updated generated scene is claimed.

## Forest-checkpost iteration

After completing the panchayat notice, meet Raghavendra at the auto pickup near the southern bus stand. Review the trip, then confirm it. Cancelling returns to the same conversation and grants no arrival. The confirmed journey skips uneventful driving to a bounded checkpost location, stops before the damaged road and saves your position. An arrival card explains the new location. A second driver instance offers return transport at the checkpost; evidence survives return visits.

The checkpost environment includes an auto, clearing, damp tiled shelter, field desk, hanging raincoat prop, closure board, fenced washout view, stone marker, and a separately gated ridge approach. These are procedural prototype assets. Both locations remain in the generated benchmark scene; additive scene loading, animated driving and a continuous connecting road are not implemented. Rain follows the player and clears old distant particles after travel.

Speak to Kencha, explain your purpose with a supported phrase, and hear the road-closure account. He relays Kamalamma's report of a young woman in yellow matching Ananya's description at Gudda Mane. The notebook explicitly treats this as relayed testimony rather than a reunion or confirmation of Ananya's present condition.

Read the closure and practise the safe directions: left at the stone marker, then follow the fence uphill. Wrong choices give a hint and retry. Permission requires the earned journey, actual checkpost arrival, Kencha's account, understood directions, and a scene route marked as checked. The generated prototype's short ridge approach is authored as cleared. Setting the session's `ridgeRouteChecked` to false keeps/reinstates the gate even when directions have been answered correctly. Old saved permission is recalculated from current scene availability. The road closure remains fixed independently of the ridge gate.

The prototype stops at the marked ridge approach and end fence; the long ridge hike, farmhouse, reunion and trunk are still future work. The raincoat is a visual shelter prop, not an implemented equipment system. Travel does not secretly advance the deadline or consume currency. Roman and English assistance remain available and notebook suspension returns to the same directions task.

Save validation now accepts two distinct playable regions (village/river and checkpost), rejecting the empty gap between them. New chapter fields default to false in old version-1 saves. The Editor checks include a checkpost-position save round-trip, and the built-player smoke sequence now covers confirming/cancelling travel, the public board, Kencha's FPP conversation, retries, notebook return, the ridge gate, persistent road closure, save restoration and returning to the village. These engine-dependent checks remain authored but unexecuted until Unity licensing works in the execution session.

Source verification: runtime and editor-generator compilation pass; 65 executable logic assertions pass, including travel readiness, early visits, route closure after learning, re-opening, and supported save regions. This does not establish rendered quality, physics clearance, save serialization or successful Unity generation/build. Regenerate the benchmark in Unity before trying this new chapter.

## Production status and visual development

Current work is a functional prototype with supporting rough environments, not empty scene creation and not final art. Dialogue, tasks, saved evidence and shared camera/UI behavior are being implemented alongside procedural buildings, props, materials and lighting. The latest source additions must still pass in-engine review. Completing the remaining story does not automatically finish environment production.

The planned visual production sequence is: establish the intended finish in the tea-stall/lane benchmark; build a consistent Karnataka vernacular architecture kit and earth/plaster/tile materials; replace placeholder characters and vegetation; improve terrain, animation, sound and environmental storytelling; then measure and tune performance across locations. Asset selection and Kannada/regional review remain necessary. Avoid postponing all visual validation until the whole game is implemented: a finished representative area should set the standard before full-world art production.

## Ridge and Gudda Mane reunion iteration

After Kencha's route permission, continue through the ridge entrance instead of stopping at the old prototype fence. At the stone marker, press E and recall the left turn and fence instruction. A wrong answer leaves the farm approach gated; the task requires being near the physical marker. Tab returns to that same activity. Follow the bounded path around the left bend, shelter and second bend, then continue through the farm gate to Gudda Mane.

The ridge is currently a short, mostly level procedural route, not the final 2-4 minute terrain hike. Fences and bends establish its navigation. No jumping, stamina, timer or dangerous-water traversal is required. The shelter, cleared branches, wet rocks, planting, farm garden, tiled farmhouse, veranda, drink table, bag/bell and closed trunk are prototype environment assets. Final terrain, planting, characters, animation and texture work remain separate production tasks.

Arriving at the farm records the player's own traversal, not an automatic reunion. Meet Ananya on the veranda to establish direct contact, then hear and acknowledge her explanation: Tuesday's bus journey, the washed-out culvert, blocked ridge, missing phone signal, and note that did not reach Somanna. The notebook keeps this direct account distinct from Kencha's earlier relayed report. Help, repeats and early exits do not grant unheard explanation flags. Kashaya, water or politely declining are equally valid hospitality choices.

Kamalamma does not give the trunk clue before Ananya's explanation. Ask her respectfully about the original deed to learn that the closed tin trunk is in the farm archive room, with the reminder twelve months / seven days. The trunk is present visually but its lock and deed interaction are not yet implemented. Finding Ananya does not resolve the library clearance or retrieve the deed.

The route is walkable in both directions by design, and the driver return journey remains available at the checkpost. This is not yet the final escorted return with Ananya. New version-1 flags default to false in old visits; the supported forest region extends to cover the farmhouse. Editor checks include farm-position/evidence serialization. The development-player smoke sequence now includes marker retries, notebook continuity, the complete authored route both ways, both farm FPP conversations, hospitality and farm save/restore. Those engine-dependent checks are authored but remain unexecuted while Unity licensing is unavailable in the execution session.

Runtime/editor source compilation and 87 executable logic assertions pass. These checks verify progression rules, not actual Unity scene generation, physics clearance, Kannada rendering or visual quality. Regenerate the benchmark in a working Editor before playing this cycle.

## Story-loop progress estimate

The source now covers approximately 11 of 15 major story beats (about 73% by equal milestone count): arrival/tea, library introduction, mess, market/timetable, river, temple witnesses, archive/rest, panchayat, checkpost, ridge, and reunion. This is not an estimate of total development effort or release readiness; beats differ in complexity. Trunk/deed, final return, public finale and epilogue remain. Current generated scene/build verification covers less than the latest source, and final environment art, characters, animation, external NLP integration and release QA are not included in this percentage.

## Trunk and deed cycle

At the farm archive trunk, use E for a stable inspection view. Enter 1207 from Kamalamma's twelve-month/seven-day reminder; the optional reminder fills the code without granting success until Try the lock is pressed beside the physical trunk. Attempts are unlimited. Opening changes the hinged lid and reveals the document but does not collect it.

Read the original story deed through three supported clause tasks: education purpose, restricted commercial use, community beneficiary. Wrong answers give feedback and retry; Tab preserves the current page and lock attempt. The register/signatories are linked to the library catalogue. After all clauses, collect the original into its protective folder while beside the trunk. Council review remains necessary. Notebook rereading is supported. These are authored fictional clauses, not a real legal instrument.

Runtime/editor compilation and 98 logic assertions pass. Unity scene generation, save serialization, physical trunk/camera review and rendering remain unverified in this session. The scene generator includes the hinged lid, paper and inspection anchors; regenerate before playing.

## Return and Friday preparation cycle

After protecting the reviewed deed, speak to Ananya. Confirm either walking back together or skipping the already explored return; neither option changes evidence or the ending. Walking uses the player's recent path points for the placeholder companion, with catch-up so she cannot block progression. Her walking collider is disabled; this is not final navigation, locomotion animation or an escort challenge. On save restoration she can catch up to the player. Return via the same ridge and Raghavendra's auto. The skip reaches the same village arrival and story flags.

Safe return records Ananya and the original back in the village but does not automatically advance the day. Speak to Ananya at the village pickup and explicitly confirm rest/Friday preparations. Friday begins at the library court before 09:00, with the original still protected and work not yet suspended. The transition saves immediately. Reading and exploration do not consume the deadline.

Source compilation and 106 logic assertions pass, including unavailable-route gates, beginning versus completing a return, walk/skip state equivalence, and explicit Friday progression. Companion motion, collisions, travel framing, serialization and in-engine scene/build checks remain pending Unity verification.

## Library court finale cycle

After Friday preparation, meet the council president at the library court. Friday staging activates the president, familiar community members, an idle works vehicle and posted works status. Inactive NPCs no longer offer interaction prompts.

Present the protected original while near the president, then restate education purpose, commercial restriction and community beneficiary through three short supported choices. A wrong answer offers retry. Ananya's explanation and Roman/English assistance remain available and do not affect the decision. Notebook rereading away from the court is allowed, but presenting evidence or requesting a decision requires being near the president.

After all evidence, explicitly request the council's decision. Works are suspended pending review; the original does not instantly grant permanent protection. The works-status sign changes while the vehicle remains parked. Later protection is a separate epilogue transition.

Source compilation and 116 executable logic assertions pass. Friday staging, council framing, physics, Kannada text, native save serialization and the final built-player sequence still require Unity verification.

## Later review, endings and isolated practice cycle

After the interim suspension, explicitly advance to the later council review outcome. Some weeks later the council confirms protection after examining the original and records; this is separate from Friday's temporary suspension. The Friday works staging is replaced by reopened-library props and later community dialogue.

Speak to Somanna or Ananya to choose staying with the archive or departing with an invitation to return. Both preserve the same protected-library outcome, regardless of assistance use. The choice is saved separately from explicitly finishing the story. Credits and a phrase recap remain available through Pause; completed endings cannot be overwritten by revisiting a choice. The recap records encounters and does not claim independent Kannada proficiency.

Replay starts a full, separate practice visit at the tea stall. It saves the main completed story first, switches to a uniquely named temporary save and restores initial NPC/prop staging. Pause offers Return to the completed story; quitting/reopening also returns to the persistent main story. This is full-visit replay, not per-chapter selection. Tests for native serialization and practice isolation are authored for Unity and remain pending execution; no player save is used by the smoke sequence.

The trunk now supports large +/- digit controls as well as typed four-digit input and the reminder option. The HUD and notebook headings distinguish Wednesday, Thursday, Friday and the later epilogue. Council history distinguishes the interim suspension from the later protection result.

Runtime and editor sources compile, and 132 executable logic assertions pass for progression gates, retries, the lock, return equivalence, council review, valid endings and assistance-independent outcomes. The isolated-save built-player smoke sequence has been extended through the trunk, return confirmation, Friday, council tasks, later outcome, credits and practice-return restoration. Editor checks cover final-save fields and distinct ending/practice stores. These engine-dependent checks are authored but unexecuted: Unity's licensing initialization remains blocked in this session. The checked-in generated scene and existing Windows build are older and must be regenerated before this source is playable.

## Current completion boundary

All 15 major story beats now have authored source implementations. This means the planned main story has an end-to-end code path, not that the game is release-ready or fully verified. Remaining work includes in-engine compilation/import and generation, full route/camera/Kannada/save testing, resolving any resulting defects, the finished tea-stall visual benchmark, production assets/materials/animation/audio, external NLP integration, accessibility/performance work and release QA. Separate additive scenes and per-chapter replay are also still future work.

## Recovery and accessibility hardening

Save storage now recovers a valid backup even when the primary is missing, preserves a good backup during corruption recovery, and retains unreadable/unsupported save files before a fresh primary is written. Real file-I/O tests run through an injected minimal JSON codec; these do not claim Unity serializer coverage. Reduced-motion camera behavior, vertical-look inversion, volume and Low/Balanced/High runtime rendering profiles are persisted. Runtime quality uses an owned pipeline clone and restores original engine settings on teardown. Pause/settings now scroll.

Source compilation and 146 assertions pass. Read docs/PRODUCTION_STATUS.md for current production gates rather than interpreting older iteration sections as current limitations.

## Chapter-select practice

Pause now offers earlier reached chapters, and credits offer all reached story beats. Practice starts from one of 15 authored checkpoints with its preceding clues, a supported spawn and copied accessibility/settings preferences. It does not pre-complete that chapter's puzzle or ending. Unreached chapters remain locked. The main visit must save successfully before switching to a distinct temporary practice store. Pause returns to that main visit; nested practice visits are blocked. This supersedes the earlier full-visit-only replay limitation.

Source compilation and 197 assertions pass. These include all checkpoint seeds, several provider/task entry gates, preference copying and actual storage recovery tests. In-engine checkpoint placement/staging still requires the Unity smoke run.

## Reusable surfaces and character presentation

Seven deterministic, periodic 512px source maps now cover wet earth, weathered stone, lime plaster, clay, timber, cotton and bark. The material shader uses world-space triplanar mapping with restrained tint/roughness; matte roof limits are retained. The editor importer configures repeat sampling, mipmaps and compression. Foliage has subtle wind, disabled by reduced motion. The generator is verification/Generate-Surfaces.py. Size/variance and opposite-edge checks pass for all seven PNGs; actual shader import and rendered appearance still require Unity.

Ananya, Kamalamma and Rathnamma now use the female model variant rather than the generic moustached model. Placeholder rigs have subtle idle/work/talk motion, turn towards a conversation partner, and derive walking motion from observed NPC movement so the companion animates. This is improved procedural presentation, not final production character art or voice lip-sync.

C# compilation and 197 assertions pass. One intermittent restricted-runtime file preservation failure returned an explicit save error; later runs passed. Save writes now expose an internal operation diagnostic for troubleshooting without displaying paths in the player-facing error.

## Additive world package and diagnostics

An optional editor workflow now exports Bootstrap.unity (one persistent player/camera/session/UI), Village_World.unity, and Forest_Farm_World.unity. Use Paridhwani > Create Additive World Package or Build Additive World Package. The legacy TeaStall_Benchmark source is retained. The exporter first generates/checks the legacy benchmark, then moves environment roots into regional scenes, records local world-reference bindings, clears cross-scene NPC/audio pointers and validates ownership/reference uniqueness. At runtime a loading panel waits for both worlds and binds references before enabling the session/player. Retry and quit are provided on load failure.

Both regional scenes load at startup in this first modular package; this is additive authoring/loading, not distance-based scene streaming. Fourteen NPC instances are registered once, including inactive story actors. Runtime binding restores eye targets/audio preferences. The exporter/build and world-reference validation compile but have not executed in Unity here, so no generated modular files or modular build success is claimed.

Development builds accept -village-profile to write an opt-in local report after warm-up: 600 frame intervals, mean/p95/max interval and allocated/reserved memory. The report records resolution and quality and makes no minimum-spec certification. No telemetry is transmitted. Actual performance results remain pending.

Runtime/editor source compilation and 200 assertions pass, including binding-field ownership/type policy. Engine-dependent additive loading, exports, shader imports and performance capture still require a licensed Unity run.

## Optional dialogue adapter and reproducible native checks

The local story remains the default. A transport-independent optional text adapter supports bounded prompts/responses, cancellation, timeout, stale-reply rejection and authored fallback. It is disabled until an integration explicitly assigns it. External text cannot grant quest effects; no backend endpoint/model/secret or service protocol is invented. See NLP_UNITY_BOUNDARY.md. Source compilation and 208 assertions pass.

verification/Verify-Unity.ps1 runs real Editor generation/checks, optionally builds, and optionally launches a visible isolated-save smoke player. It checks both process exit codes and explicit completion markers, writes a local result report, and does not call an old executable a new verification. Examples from the master workspace root:

    ./game/verification/Verify-Unity.ps1 -Layout Legacy -Build -Smoke
    ./game/verification/Verify-Unity.ps1 -Layout Additive -Build -Smoke

Close this project's Editor first. Run from a normal desktop shell with the project's licensed Unity installation. The smoke flag opts into a visible game window. This session retried Legacy generation on the latest source, but Unity timed out waiting for licensing after 60 seconds and crashed before compilation. No new scene/build/rendering success is claimed; the desktop helper also failed to launch its app server.

## Title, new-story and exit flow

The normal launch now opens a scrollable title menu with Continue, new story, reached-chapter practice, settings and exit. The isolated smoke path skips the title so its existing progression tests remain deterministic. Settings opened from the title return to the title; the first introduction is not bypassed by Escape or restore shortcuts.

Starting a new main story explicitly confirms the reset and writes the previous visit into ArchivedVisits before saving the new one. If saving/archiving fails, the new story does not start. Accessibility/display preferences are retained. Pause offers return to title and save-and-exit. A failed exit save offers returning to the visit or explicitly leaving without the latest changes. The rendered smoke sequence now includes title/settings return, chapter practice and a new main visit using its isolated save.

Runtime/editor compilation and 209 assertions pass, including real archive-file creation with the injected storage codec. Native menu layout, interaction, archives with Unity serialization and the whole newly generated game still require the engine run.

## Final source hardening

Rendering quality now also controls cached renderer-distance culling (45/80/120 metres) while retaining all collision and quest objects. This reduces distant draw work; it is not LOD mesh production or proven FPS improvement. The opt-in frame profiler waits for initial gameplay movement before its warm-up so it does not profile an unattended title screen as the representative scene.

Regeneration preserves copies of every dirty loaded scene, including untitled scenes, before replacing generated content. Deprecated object-finding APIs were updated against the installed Unity references. The smoke sequence includes reduced camera motion, audio/presentation save restoration, title return, chapter practice and a fake optional text adapter that cannot grant quest effects. These engine tests remain authored but unexecuted. Source compilation and 209 assertions pass.

## Migration and explicit quit safeguards

A valid backup now also blocks the old-product migration path, so a missing current primary cannot let an older legacy visit replace newer recoverable progress. Choosing to leave without saving suppresses subsequent quit/focus autosaves. The in-Editor exit action stops Play mode; standalone exits the player. Source compilation and 210 assertions pass.

## Stale generated-world safeguard

Generated worlds now carry a schema revision and a fingerprint of the editor world recipes. In Editor play, changed recipes require regeneration. Standalone validates the authored world revision and essential late-story bindings. If the checked-in older benchmark is opened against newer scripts, play stops before reading or writing the visit and explains how to regenerate, rather than allowing progress into missing locations. Generator fingerprints ignore checkout line endings and detect changed recipe content.

Source compilation and 212 assertions pass. The current checked-in scene is intentionally treated as stale until a licensed Unity generation succeeds.


## Input and movement follow-up (27 October 2026)

- Optional dialogue text and lock-entry focus suppress story number shortcuts and save/restore hotkeys. Modal Tab navigation stays with UI controls; conversation now offers a notebook button. World/notebook Tab remains available.
- Keyboard handling executes one command per frame so closing/restoring cannot also select the newly displayed reply. Protected title/confirmation modes reject F5/F9. Losing focus from exploration saves and opens pause before returning control.
- Walking normalizes diagonal input while retaining analogue magnitude. Animation/footstep speed now uses actual planar controller displacement, so holding movement against a wall does not keep reporting walking speed.
- Runtime/editor sources compile and 229 executable assertions pass. Actual UI Toolkit focus dispatch, collision movement and focus-loss behavior still require the licensed Unity gate.


## Ground contact audio follow-up (27 October 2026)

- Replaced the map-x footstep rule with authored collider surface metadata for earth/lane, stone and timber. The generated world exports these local tags with each additive scene.
- Nine original synthesized transient variants use travelled distance and grounded contact. Blocked, stationary, airborne, unfocused and muted footsteps reset cadence; restores reset reported speed. These remain audio placeholders pending listening/production review.
- Added persistent rain/river and footstep volume channels, copied into practice/new-story preferences with backwards-compatible full-volume defaults for older saves. The existing ambience toggle is now labelled rain/river; footsteps have their own slider. Master volume controls both.
- Rain and river start at zero gain, mute immediately, and silence on focus loss. Shelter transitions still blend. No recorded/third-party audio was added.
- Runtime/editor compilation and 247 executable assertions pass. Generated-world checks cover all three collider tags; native smoke checks cover mix application and Unity JSON round-trip. Those native checks remain authored but unexecuted because of the existing Unity startup gate.


## Restore and companion lifecycle follow-up (27 October 2026)

- Manual restore distinguishes a valid primary/backup from missing or unreadable files. A failed restore keeps the live visit, cancels optional text, and explains recovery rather than replacing progress with startup defaults. Returning to a pre-arrival save keeps the arrival screen.
- Successful restore resets NPC staging to authored origins before reapplying the saved chapter. This fixes loading an earlier chapter while NPCs remain at the later council or return positions.
- Companion breadcrumbs now have a bounded 128-point history, reset on visit replacement and transport, and reject cross-world discontinuities. Old walking history cannot survive a load into another chapter. Returning from practice switches save ownership only after reading a valid main visit; failure keeps practice isolated.
- World revision is now 3 because regenerated collider audio metadata is required. The old checked-in scene remains deliberately guarded; no fresh scene/player is claimed.
- Added storage-status and companion lifecycle checks plus a native actor-displacement restore regression. Runtime/editor compilation and 259 executable assertions pass; scene assertions remain pending the licensed Unity gate.


## Conversation readability follow-up (27 October 2026)

- Conversation cards use a bounded, scrollable reading/reply area. Leave and notebook/language controls stay outside the scroll area. Text scaling also limits the card's width; narrower cards stack replies. The card remains below the speaker's face region by design, pending rendered acceptance.
- Same-turn language toggles and optional-reply refreshes retain reading position and available control focus. New turns clear stale learner drafts. The Original line action cancels pending optional text and restores the authored line without quest effects.
- Dialogue labels render literal text, including any markup characters returned by the optional service. No actual NLP service is added.
- Native smoke assertions now exercise a long response at 960x540 and 1.3 text scale, pinned action bounds, literal labels, original-line restoration and draft clearing. These are authored but cannot be executed through the current restricted Editor connection.
- Runtime/editor source compilation and all 259 standalone assertions pass. Actual layout and Kannada shaping remain in-engine review gates.


## Unity availability clarification (27 October 2026)

Earlier successful builds/rendered tests remain valid for their historical revisions. The old licensing timeout was a real automated-launch failure. A fresh desktop log now shows successful licensing; the exact project is open and has imported assets. The CLI batch attempt refuses the open project, while live discovery returns STATUS_NO_INSTANCES plus a running-process-inspection warning. No Pipeline package is installed in the project; no license, account, sandbox or package setting was changed. The shell's LocalApplicationData path is sandbox-specific. The current limitation is control of the running Editor, with regeneration/native acceptance still pending, rather than a proven ongoing license failure on the desktop. See PRODUCTION_STATUS.md for the current manual/closed-Editor verification routes.


## Surface import blocker and diagnostic warning repair (27 October 2026)

The reported WetEarth exception was a real generator defect. All seven PNGs exist and decode correctly. The desktop Editor log reports their previous minimal metadata as TextureImporter version 1, below Unity 6's supported minimum of 10; the files were originally checked in without full texture importer metadata. Unity has now upgraded them to version 13 without changing GUIDs. Those native-generated metadata files are included in this repair.

Generation now validates all source files, synchronously forces texture imports and checks every Texture2D before creating a replacement scene. A failed import is reported separately from a missing file. The Repair Surface Texture Imports menu can run that preflight independently. No source files or Library caches are deleted, and no material falls back silently to an unrelated texture.

Removed all obsolete DEVELOPMENT_BUILD directives. Diagnostic types compile independently of managed variants, and Debug.isDebugBuild gates profiling/smoke activation at runtime (including the isolated-save smoke flag). Release players do not activate these opt-in tools. Source verification additionally compiles runtime branches without Editor/diagnostic symbols using the cached references, and checks PNG signatures/dimensions, metadata schema and stable unique GUIDs. This remains source/import-input validation, not a native player build or completed Editor import repair execution.


## Native surface repair and isolated Editor testing (28 October 2026)

The desktop Inspector identified WetEarth as a Cubemap. Explicit Texture2D preprocessing, in addition to supported metadata, fixed all seven imports. Repair Surface Texture Imports emitted PARIDHWANI_SURFACE_IMPORTS_OK, Create Tea Stall Benchmark regenerated the revision-3 full story world, and native benchmark checks passed. Generated materials now retain filename-matching names and world-sign panels receive their theme.

Added Play Isolated Test Visit and Run Automated Editor Playtest. Both isolate saves from the normal visit; automated tests return to Edit mode and report failures to disk. Manual title/arrival/world rendering was observed. The automated run reached river and temple tasks but stopped approaching the library veranda at (-5.25, 0, 12.30). A complete run is not claimed. The walking timeout now reports actual player position and nearby colliders instead of the misleading generic river-route message.

Source compilation and 259 standalone assertions pass. On 28 October, direct shell launching again failed licensing IPC, but launching the project through Unity Hub worked. Current status is recorded in PRODUCTION_STATUS.md.


## Native regression completion (28 October 2026)

Reproduced the library blockage with actual position/collider diagnostics: the scripted straight approach squeezed between Somanna and the veranda bench. The test now takes the open east side before entering the existing doorway. Physical CharacterController walking passed archive entry/exit and continued through panchayat, checkpost, ridge, reunion, trunk/deed, return, council, epilogue, credits, practice and completed-save restoration.

An overbroad literal-text assertion was corrected to inspect the speech container, match the original optional reply exactly and require rich text disabled on its labels. The old assertion incorrectly included Unity's built-in TextField label. The fixed Game view was set through the native UI to 960x540. At 1.3 text scale, scroll overflow and fixed action bounds passed; the final capture was visually reviewed and preserves literal markup characters. The actual resolution is now logged.

The final run, ParidhwaniSmoke/run-20261028-052700 under the OS temporary directory, emitted VILLAGE_SMOKE_OK and PARIDHWANI_EDITOR_SMOKE_EXIT 0, advanced 6,800 frames and wrote 51 PNG captures. All runtime/editor source checks and 259 standalone assertions also pass. This is native scripted Editor coverage; it does not claim a new standalone build, final language/art approval or human input acceptance.

Standalone verification compiler outputs moved to game/verification/Temp, outside the Unity project Temp folder which Editor startup clears. This avoids the compilation-output deletion observed when starting Unity alongside a source check. After external C# edits, use Assets > Refresh and wait for compilation before starting an Editor playtest.


## Standalone test workflow implementation (28 October 2026)

Added Build and Run Standalone Playtest to the Paridhwani menu. It uses the already-open Editor to regenerate/check/build before launching a visible isolated-save development player at 960x540. It never falls back to an older executable after a build failure. The asynchronous monitor requires a clean exit and VILLAGE_SMOKE_OK, writes JSON and a player log, and handles timeout/script-reload/Editor-close failure with cleanup limited to its own child process.

The batch verifier now writes reports even on failure, stops early at an explicit licensing initialization failure, and stores logs/reports outside the Unity project Temp directory. A deliberate non-Unity child failure confirmed that failure reporting works and does not launch an existing player; this fixture is not a native build.

The standalone runner is excluded from world recipe hashing because it creates no geometry/bindings. The checked-in world retains its recipe match. Regression checks cover ignored tooling edits and still detect changed/new world recipe files. Runtime/editor compilation and 262 standalone assertions pass. Fresh standalone acceptance is still pending; the preceding 6,800-frame Editor story pass remains the latest successful native run.

The updated batch workflow was actually retried. It stopped at licensing initialization failure, wrote Legacy-20261028-112727976.json under verification/Temp/UnityVerification with both pass flags false, and did not launch an old player. This confirms failure reporting, not a successful standalone build.


## Typed NLP boundary (28 October 2026)

Added a transport-independent IDialogueTextService with immutable request snapshots, unique request IDs, version/turn/request correlation and cancellation forwarding. DialogueServiceAdapter binds it to the existing optional text delegate. Empty, oversized or unrelated responses fall back without story effects; legacy responses are copied after validation. The real engine remains external and unconfigured. The proposed HTTP/JSON mapping and ownership/lifecycle recommendations are documented separately, without claiming they are the engine's existing contract. Source compilation and 275 standalone assertions pass, including 13 new service-boundary checks; no network or new native playthrough is claimed.


## NLP request lifecycle regression repair (28 October 2026)

A standalone regression first reproduced a transport callback escaping Cancel into gameplay. Cancellation is now fault-contained, runs callbacks outside the state lock and wakes uncooperative requests. Deadline cleanup no longer cancels already-completed transport work; setup failures still cancel partially started work. Generation checks prevent already-completed queued responses from appearing after provider replacement, turn change or detach. Disposed coordinators reject new requests, and reentrant replacement retains its busy state.

Added Unity-thread SetOptionalDialogueService attachment/detachment and made the existing delegate setter invalidate pending/visible optional text. Native smoke assertions now simulate a throwing cancellation callback, retired-provider late reply, working replacement and detach without quest effects. These are authored/compiled, not newly executed here. Runtime/editor compilation and 287 standalone assertions pass, including 12 new lifecycle checks. Real engine integration is unchanged and remains pending its supplied contract.
