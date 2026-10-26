# Playable village slice — tea, library, mess, market and river

This Unity 3D prototype now covers five story locations: Manjunatha's tea stall, Somanna's library veranda, Shanthakka's mess, Basavaraju's market stall, and Giri's river repair shed. It includes third-person exploration, smooth first-person conversations, Kannada/Roman/English presentation, inspected signs, a resumable notebook, settings and saved progress. It is a compact playable prototype, not the final 25–35 minute polished slice. Characters and vegetation remain original procedural stand-ins; dialogue still needs fluent-speaker review.

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
