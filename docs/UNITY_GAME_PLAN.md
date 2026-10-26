# Paridhwani — Unity 3D Game Plan

Planning baseline: 26 October 2026. Status: proposed implementation plan. This review creates documentation only; new gameplay and scenes have not been implemented.

## 1. Game direction and scope

Paridhwani is a third-person, story-led Kannada learning adventure in a fictional Karnataka settlement during the monsoon. The player arrives for an archiving job, investigates Ananya's disappearance, discovers the threat to the community library, and helps retrieve the deed that can protect it. Kannada gives the player useful actions: ordering, asking directions, understanding witnesses, reading notices, and explaining evidence.

Build a believable, lived-in place with restrained realism: convincing proportions, materials, lighting, movement, and human behaviour without requiring photorealistic characters. The mystery supplies momentum; daily life supplies learning.

User requirements supersede the source brief: Unity 3D replaces Godot/tilemaps/pixel art; a serious, earthy village finish defines the art; the NLP engine is separate and integrates later; this phase plans the complete game.

### Proposed defaults

These are working assumptions, not previously confirmed requirements.

| Decision | Default | Reason/review point |
|---|---|---|
| First target | Windows desktop, keyboard/mouse | Keeps initial 3D and text-entry scope manageable; browser/mobile need a separate decision. |
| Camera | Third-person free-look, restrained conversation framing | Shows the character and world while supporting inspection; validate comfort in blockout. |
| Regional reference | Fictional Malnad foothill settlement with a small town centre and village outskirts | Fits rain, areca gardens, tiled roofs, forest road and farmhouse. A chosen reference, not a statement about every Karnataka village. |
| World | Compact connected town and bounded hill route | Revisits and familiarity without a huge environment. |
| Progression | Ten chapters, milestone-controlled story time | Retains urgency while giving learners time to think. |
| Initial scope | 25–35 minute vertical slice | Full-game duration must be estimated after testing. |
| Communication | Authored choices/text first; speech later | Every scene remains playable without speech recognition. |

## 2. Existing project audit

Inspected Unity project: game/My project (1).

| Item | Observed state |
|---|---|
| Editor | Unity 6000.6.7f1 from ProjectSettings/ProjectVersion.txt. |
| Renderer | URP 17.6.0 declared; PC and Mobile pipeline assets exist. |
| Input | Input System 1.20.1 and an actions asset exist. |
| UI | Unity UI 2.6.0 declared; Kannada rendering has not been proved. |
| Scene | Only Assets/Scenes/SampleScene.unity: template camera, directional light, global volume. |
| Gameplay | No authored player/NPC/quest/dialogue scripts under Assets; tutorial scripts only. |
| Art | No village, character, or story assets found under Assets. |
| Git | game is an independently versioned repository/submodule. |

Pre-existing changes in ProjectAuditorSettings.asset and QualitySettings.asset must remain intact. Audit was by file inspection, not Play Mode/build execution. Keep current Unity/URP baseline unless a reproducible problem warrants changing it. Do not rename the project directory during initial work.

## 3. Proposed narrative continuity corrections

These resolve conflicts in the brief and must become one agreed canon before final dialogue production. They are proposed revisions, not decisions already confirmed by the user.

| Conflict | Proposed consistent version |
|---|---|
| Missing 36 hours, but left Tuesday and player arrives Wednesday morning | Wednesday 09:00 opening, about 26 hours after departure; Friday 09:00 is 48 story hours away. |
| Monday events called yesterday | Use named days or correct relative dates: Monday is two days before Wednesday. |
| Meals prepared Monday/Tuesday in different sections | Supplies purchased Monday 18:00; two meals packed Tuesday 06:00; shopping slip dropped Monday. |
| Bicycle submerged but upright/intact with dry evidence | Parked under Giri's shed eave near weir; distant sighting becomes an accident rumour. Never submerged. |
| Used bus ticket left on bicycle before boarding | Monday-issued advance booking slip for Tuesday Route 14 at 06:45. It proves intention; later witnesses establish boarding. |
| Collapsed bridge but unexplained player/return route | Road culvert fails after Ananya crosses. Higher ridge is blocked by fallen branches until Kencha's team clears/checks it Thursday. Both player and return use that route. |
| Auto bypasses dangerous closure | Raghavendra drives only to the open checkpost approach. Cleared ridge is walked. |
| Kamalamma is 68 and also an octogenarian | Age 78 provisionally; confirm once in character sheet. Distinct from Rathnamma. |
| Copper plate, copper-bound paper and deed overlap | One decisive registered paper deed from 1952, cloth-wrapped in tin trunk. Any copper plate is background heritage. |
| Lock is a three-part phrase in one passage and 1207 elsewhere | One four-digit lock: twelve months/seven days gives 1207; rehearse those numbers earlier. |
| A witness completely disproves every rumour | Record limited observations. Ananya's reunion/account finally resolves the disappearance. |
| Disappearance deliberately staged as a puzzle | Ordinary receipts and archive references form the trail; one note to Somanna was misplaced. Ananya expected to return that day. |
| Nobody takes a missing person seriously | Family/local authorities are already informed. Player contributes archival knowledge and follows leads; outpost can be a background facade. |
| Clerk alone orders demolition; deed instantly creates permanent protection | Clerk improperly advances clearance. Council president suspends work pending review; later epilogue confirms protection. Fictional story logic, not legal simulation. |

Chronology: Monday archive discovery and supplies → Tuesday food, bicycle, 06:45 bus, arrival at farmhouse, washout → Wednesday player investigates town → Thursday archive/panchayat, cleared trail, reunion and deed → Friday return and public presentation.

Do not promise fluency after two fictional days. Teach limited beginner tasks through repetition; complex passages use supported chunks.

## 4. Visual and audio direction

“Muddy” comes from believable wet earth and drainage, not a brown filter. Depressions hold water, eaves shelter dry patches, thresholds/lower walls collect mud, traffic creates ruts, wet stone darkens. Keep food surfaces, faces, signs and documents clear.

| Element | Direction |
|---|---|
| Ground | Red-brown earth, compacted tracks, gravel, puddles, damp stone steps, moss where moisture persists. |
| Architecture | Tiled roofs/deep eaves, plaster, stone plinths, timber verandas, plus modest concrete shops and practical repairs. |
| Streets | Narrow lanes, drains/crossings, low walls, uneven thresholds, wires, shelter, bicycles/auto, restrained modern details such as payment stickers. |
| Planting | Areca garden references, broad leaves, layered undergrowth, distinctive heritage banyan. |
| Palette | Umber #665043, laterite #8E5944, tile #874B39, wet leaf #455544, plaster #C3B79B, sky #A7B1B1; art swatches, not universal material values. |
| Characters | Believable proportions, restrained expressions, useful work gestures. Ananya's mustard kurta, green bag, brass bell and green bicycle remain identifiers. |
| Lighting | Soft overcast day, shaded verandas, warm practical interiors, controlled rainy dusk; readable faces/text. |
| Camera | Human-scale views, brief skippable arrival, gentle conversation framing, prompt return of control. |
| UI | Quiet notebook surfaces, ink colours, minimal ornament, readable type; no constant quiz celebration. |
| Sound | Roof drips, mud/stone/wood footsteps, rain muffling under shelter, boiler, utensils, bus, distant voices, river/insects/bell. |
| Music | Sparse original/licensed regional instrumentation reviewed for fit; ambient sound carries most locations. |

Gather actual regional photo references and Kannada speaker review before final art; this plan has not conducted a photographic location survey. Avoid unrelated regional motifs assembled merely to signal India. Temple is a community place. NPCs have individual reasons for warmth, concern or obstruction; avoid caricatures. Track asset licences and maintain consistent style.

First art benchmark: finish a 15–20 metre tea-stall section with muddy verge, stone standing area, drain, tiled eave, wet plaster, boiler/glasses, one NPC, readable Kannada sign, rain and shelter audio. Judge it at the real gameplay camera. Its material, scale, light, animation and audio standards apply everywhere.

## 5. Spatial layout and Unity scene manifest

A chapter is narrative state; a location is a place; a Unity scene is an authoring/loading unit. Reuse locations for weather, chapters and endings.

| Place | Connections/landmarks |
|---|---|
| Bus stand | South arrival, tea shelter, market lane ahead, library roof visible. |
| Market | Links bus, mess frontage, library court, panchayat door; side lane to river. |
| Library court | Central landmark/banyan, reading-room entrance, north lane to temple; finale venue. |
| Mess | Compact serving veranda/interior off market. |
| River | Safe overlook and covered bicycle shed at lower town edge. |
| Temple | Public forecourt/water steps, view of northern stop, road toward checkpost. |
| Panchayat | Public counter/office off market; no separate town hall. |
| Checkpost | Brief explained travel transition from town; washout overlook and ridge entrance. |
| Ridge | Bounded walking path to farm, overlooking blocked road. |
| Gudda Mane | Garden, veranda, archive room; return uses same ridge. |

Blockout targets: most town destinations 30–90 seconds apart on foot; ridge 2–4 minutes with interactions. Measure/tune these. Compress the brief's four kilometres through travel transition, not empty terrain.

### Proposed assets

These scenes do not exist yet; keep SampleScene until the replacement works.

| ID | Proposed scene path | Responsibility | First needed |
|---|---|---|---|
| S00 | Scenes/System/Bootstrap.unity | Persistent session, save/loading, audio coordination, UI lifecycle, conversation access | Foundation |
| S01 | Scenes/System/FrontEnd.unity | Title, new/continue, settings, accessibility, credits | Slice |
| S02 | Scenes/World/World_Core.unity | Shared backdrop/sky and global weather/light ownership | Slice |
| S03 | Scenes/World/BusStand.unity | Arrival, tea, timetable, transit revisits | Slice |
| S04 | Scenes/World/Library_Courtyard.unity | Door, veranda, banyan, garden, finale | Slice |
| S05 | Scenes/Interiors/Library_ReadingRoom.unity | Desk, archive corner, guest-room doorway | Full |
| S06 | Scenes/World/MarketLane.unity | Connecting street, vendor, mess/office frontages | Slice |
| S07 | Scenes/Interiors/Shanthakka_Mess.unity | Food task, note, feast state | Slice |
| S08 | Scenes/World/RiverWeir.unity | Safe bank, shed, bicycle inspection | Full |
| S09 | Scenes/World/Temple_Steps.unity | Witnesses and northern-stop sightline | Full |
| S10 | Scenes/Interiors/Panchayat_Office.unity | Counter, notice, clerk | Full |
| S11 | Scenes/World/Forest_Checkpost.unity | Guard, auto, washout, trail permission | Full |
| S12 | Scenes/World/Ridge_Trail.unity | Hike, direction practice, return | Full |
| S13 | Scenes/World/GuddaMane.unity | Farm approach/veranda reunion | Full |
| S14 | Scenes/Interiors/GuddaMane_Archive.unity | Trunk, deed, family records | Full |

Fifteen scene assets: two system, one shared, twelve location. Title background, finale, feast, farewell and variants reuse them. In the slice, S07 may be just the compact serving/seating veranda.

### Ownership/loading

- Simple persistent bootstrap plus location loading first. Keep compact slice exteriors resident if profiling permits; fade door/travel transitions for interiors/hills. Add streaming only for a demonstrated need.
- Locations own local walkable ground, colliders, props, NPC anchors, audio and lights. World_Core owns shared backdrop/global atmosphere, never duplicate walkable terrain.
- One world origin, documented exterior boundary coordinates, stable doorway/spawn IDs and safe facing directions.
- Exactly one player, gameplay camera, active listener, UI/input owner and global weather/sun controller.
- Session restores clue/NPC state on scene load; unloading never resets story.
- Prototype active-scene lighting, baked lighting/probes, roof rain exclusion and navigation boundaries. Use single-location loading if additive lighting proves unreliable.

## 6. Complete playable sequence plan

State labels below are conceptual identifiers, not implemented code. Each card defines narrative, language, staging, assets, output and completion gate.

### Sequence 0 — Title/setup

**Scenes:** S00/S01 → S02/S03. **Purpose:** tone and assistance setup.

Use a sheltered tea-stall title view once available; static background during foundation is sufficient. Offer new/continue, name/simple protagonist preset, subtitle size, language layers, assistance, audio, camera sensitivity/motion settings and credits. No elaborate character creator. All assistance remains changeable later.

Start save, brief skippable bus arrival, give control with notebook and first objective. Skip must still record arrival facts.

**Output/gate:** valid session; new/continue restores correct spawn/settings; menu input never traps gameplay.

### Chapter 1 — Tea stall arrival

**Scene:** S03. **NPCs:** Manjunatha, minimal conductor. **Mood:** uncertainty and welcome. **Language:** greeting, name, want/do not want, water/tea, where.

Frame arrival toward shelter/boiler and first NPC; bus leaves on authored path. Teach move/look/interact. Model greeting, introduce name, order tea or water with guided phrases. Show simple handover and notebook entry. Wrong phrase produces clarification and retry, not ridicule or financial punishment.

Ask for library. Gesture and landmarks indicate stall → market corner → library wall; notebook sketches route. Learn Ananya has missed her routine since Tuesday; speculation remains labelled rumour.

**Assets:** bus/shelter, boiler/glasses, bench, timetable, gutter/puddles, Kannada sign.

**Output:** arrival_complete, ordered_drink, library_route_known, ananya_routine_disrupted.

**Next/gate:** S04 through S06; beginner finds library using directions/landmarks without dependence on a floating arrow.

### Chapter 2 — Locked library

**Scene:** S04; S05 opens on later revisit. **NPC:** Somanna. **Mood:** concern. **Language:** respectful request, who/where, key/door/book.

Padlocked public door, visible windows, shaded veranda, banyan court establish the place worth saving. Somanna is seated and hard of hearing; use patient repetition, clear subtitles and orientation. His hearing is not a joke or microphone challenge.

Introduce appointment, request access. Ananya has duplicate; Somanna retains master. Inspect appointment/Monday archive reference; follow lead to Shanthakka. Slice keeps reading room closed while access is arranged; full game opens it at archive follow-up.

**Assets:** door/lock, veranda/bench, noticeboard, appointment note, wall/banyan.

**Output:** met_somanna, polite_request_practised, mess_lead_known, library_access_pending.

**Next/gate:** mess; understand investigation lead and everyday importance of library.

### Chapter 3 — Shanthakka's mess

**Scenes:** S06/S07. **NPC:** Shanthakka; introduce Basavaraju by name before modelling him. **Mood:** warmth. **Language:** meal/water, little/enough, price/payment.

Wet threshold leads to drier serving space with compact kitchen backdrop. Offer seat/food; if food declined, equivalent water/serving exchange remains. Request water, indicate enough, pay displayed amount. Replace timed overflowing-rasam failure with pauseable serving.

Learn two meals were packed Tuesday morning. Receive Monday's dropped shopping slip, link to Basavaraju. Reuse greeting/request patterns from previous chapters.

**Assets:** counter, vessels/tumblers, meal setting, benches, menu, shopping slip, wet threshold.

**Output:** food_exchange_complete, two_meals_clue, shopping_note_collected, market_lead_known.

**Next/gate:** slice ends with recap/hook; full continues to market. Complete three-chapter loop works locally with retries and save/reload.

### Chapter 4 — Market receipt and timetable

**Scenes:** S06, return to S03. **NPCs:** Basavaraju; conductor/Raghavendra for supporting account. **Mood:** everyday bustle. **Language:** numbers/days, morning/evening, asking when.

Dress one hero stall and several simpler facades. Match note/ledger, identify supplies, establish Monday 18:00 purchase. Clock/timetable inspection uses large controls. Purchase and departure are distinct events.

Identify Tuesday Route 14 at 06:45; record planned travel, not proven boarding. Hear unverified bicycle report.

**Assets:** stall/scales/crates, ledger/receipt, awning, clock/timetable.

**Output:** provisions_time_known, planned_departure_known, river_report_heard.

**Next/gate:** river; distinguish date/time and intention from observation, beyond matching numbers.

### Chapter 5 — Bicycle at the weir

**Scene:** S08. **NPC:** Giri. **Mood:** tension reassessed through evidence. **Language:** on/under/inside, here/there, intact/damaged, look.

Safe playable bank overlooks dramatic current. Bicycle remains sheltered. Hear Giri, inspect actual 3D bicycle in controlled close-up, use supported spatial phrases to locate carrier pocket/booking slip.

Intact frame, upright stand and shelter weaken accident theory without proving safety. Advance booking establishes intent, not boarding.

**Assets:** green bicycle/cane basket/pocket, slip, shed, weir/water/reeds, safe boundary.

**Output:** bicycle_inspected, booking_slip_collected, accident_rumour_weakened, north_stop_lead_known.

**Next/gate:** temple/northern stop; clue collected once, persists on revisit, no falling into current.

### Chapter 6 — Temple witnesses

**Scene:** S09; optional timetable return. **NPCs:** Gurumurthy, Rathnamma. **Mood:** reflection. **Language:** came/went/spoke, alone/with, yesterday morning, respectful address.

Public forecourt, flower preparation, water steps and northern-stop sightline. Review local water structure reference rather than assuming an elaborate stepwell.

Interview separately; select phrase describing company, record testimony with sources. Witnesses report only what their sightline allows; conductor corroborates boarding if necessary. Rathnamma connects archive reference to Shankare Gowda's family/Gudda Mane.

**Assets:** shelter/bell/steps, flower basket/work pose, family reference, stop background.

**Output:** witness_accounts_compared, boarding_corroborated, gudda_family_lead_known, updated rumour confidence.

**Next/gate:** library archive; distinguish observation/inference and support muted-audio play.

### Interlude — Archive revisit and overnight rest

**Scenes:** S04/S05. **NPC:** Somanna. **Mood:** urgency. **Language:** reused location words, dates/documents.

Master key opens modest room: desk, shelf bays, window light, archive corner. Follow index reference through known shelf/inside vocabulary to land-record/planning reference. Explain why original deed is at Gudda Mane and why Ananya went.

Guest-room doorway and fade handle rest; no bedroom level needed. Confirm readiness before Wednesday evening → Thursday morning.

**Assets:** catalogue, labels, ledger/files, desk/lamp.

**Output:** library_access_granted, deed_reference_found, thursday_started.

**Next/gate:** panchayat; revisit reuses learning and clarifies stakes.

### Chapter 7 — Panchayat notice

**Scenes:** S06/S10. **NPC:** Shresthi; optional minimal counter staff. **Mood:** obstruction. **Language:** notice/date/signature, must/must not, document purpose.

Public counter, accessible notice, desk/files beyond, damp window light, partly visible warehouse plan. Ask and inspect from public area; no stealth/break-in/combat.

Identify library, clearance and Friday date/time; unfamiliar administrative wording is chunked with support. Present archive reference. Evasiveness raises suspicion; notice itself is evidence. Deed must reach council before works.

**Assets:** notice, desk/files, clock, stamp, plan roll/counter.

**Output:** notice_deciphered, deadline_known, deed_required, checkpost_destination_known.

**Next/gate:** auto transition; essential information remains available if optional questions are refused.

### Chapter 8 — Checkpost and washout

**Scene:** S11. **NPCs:** Kencha/Raghavendra. **Mood:** concern and hope. **Language:** rain/bridge, closed/open, because/if, need/help/walk.

Shelter/barrier, safe culvert overlook, separate marked ridge entrance. Auto stops at open approach. Read closure, ask what happened, explain purpose using supported chunks. Understandable intent matters more than perfect grammar.

Kencha reports girl in yellow reached Kamalamma and explains ridge was blocked, now checked. Directions/raincoat given. Permission needs both story readiness and physically open route; a correct sentence does not make danger disappear.

**Assets:** shelter/barrier/sign, route sketch, washout background, auto/raincoat.

**Output:** ananya_location_reported, ridge_route_open, trail_instructions_understood, trail_permission.

**Next/gate:** ridge; slow readers retry without countdown or unsafe crossing.

### Journey — Ridge path

**Scene:** S12. **NPC:** none outward; Ananya on return. **Mood:** quiet effort. **Language:** reused directions, sequence, weather/warnings.

Short authored route: stone marker, sheltered bend, farm gate. One supported direction check recalls instructions. Slopes/planting/fence/rocks form readable boundaries. No precision jumps, hunger, stamina or drowning systems.

View damaged road to explain detour. Shelter changes rain sound and provides notebook pause.

**Assets:** wet rock/earth kit, marker, cleared branches, shelter, distant roof.

**Output:** ridge_crossed, gudda_mane_reached.

**Next/gate:** farm; clear path, comfortable slope camera, no physics blockage.

### Chapter 9A — Reunion

**Scene:** S13. **NPCs:** Ananya/Kamalamma. **Mood:** relief. **Language:** how/why, came, help/protect/gratitude; recombination of known patterns.

Reveal mustard kurta under veranda before dialogue; bag/bell link person to clues. Explain journey. Ananya clarifies work notes, connectivity, blocked return and goal.

Kashaya exchange repeats food/request pattern with less default assistance. Deed remains in trunk; Ananya found correct records and waited for Kamalamma's reminder, rather than already displaying an opened deed.

**Assets:** farmhouse/veranda, cups/seating, drying cloth, photo, garden, bag/bell.

**Output:** ananya_found, disappearance_explained, trunk_location_known.

**Next/gate:** archive; no early conversation may reveal this resolution.

### Chapter 9B — Trunk/deed

**Scene:** S14. **NPCs:** Kamalamma/Ananya. **Mood:** discovery. **Language:** practised numbers, inside/open, month/day/year, three deed concepts.

Small archive with trunk. Stable close-up and large dial controls. Hear/read twelve months/seven days reminder, enter 1207; contextual hints/assisted completion remain. Ananya helps interpret.

Inspect paper deed; understand education purpose, commercial restriction and beneficiary through supported phrases. Link signatures/reference to library record. Place in protective folder; save before return.

**Assets:** trunk/lock, cloth folder, deed facsimile, archive props.

**Output:** deed_retrieved, deed_concepts_reviewed, finale_ready.

**Next/gate:** return; authored interaction/state determines success, not generated document text.

### Return — Back to town

**Scenes:** S12/S11 → S04. **NPCs:** Ananya, briefly Kencha. **Mood:** shared purpose.

Reuse path with changed dialogue. Short escorted walk or skip after outbound traversal. Companion catch-up anchors prevent navigation from blocking story.

Explicit safe-return and Friday-morning transition. Conversation never secretly consumes deadline.

**Output/gate:** returned_with_ananya, friday_finale_started; walk/skip create equivalent quest/save state.

### Chapter 10 — Voice in the library court

**Scene:** S04 finale state; optionally S05 afterward. **NPCs:** Ananya, Shresthi, Somanna, council president, small familiar crowd. **Mood:** public tension.

Idle construction vehicle, survey marks/barrier, assembled townspeople. No physical demolition simulation. Present deed and request work stop; three short evidence exchanges reuse reviewed clauses, not spontaneous legal fluency.

Notebook, replay and Ananya's help remain. President checks evidence and suspends work pending review. Suppression is exposed by evidence; community acts together.

**Assets added:** vehicle, works props, president, document handover, restrained crowd reactions.

**Output:** deed_presented, work_suspended, ending_selected.

**Gate:** aided/unaided players can both save library; optional gossip never blocks resolution.

### Epilogue and variants

**Scenes:** S07 meal, S04/S05 reopened library, optionally S03 departure. **Mood:** belonging.

Later scene confirms protection after review. Active library, fuller notebook, familiar conversations. Proposed main variations: stay archiving or depart with invitation to return; relationship choices/optional help alter reactions.

Retain source's late-arrival regret only as optional later narrative mode driven by explicit choices, with checkpoint replay. Never cause it through slow reading, translation use or pronunciation errors. Optional fewer-hints recognition must not determine community welcome.

Credits/phrase recap/chapter replay distinguish encountered words from independently demonstrated skills.

**Gate:** recoverable final save, clean credits exit, replay isolated from main progress.

## 7. Learning presentation and accessibility

The engine team owns syllabus, transliteration, evaluation and adaptive policy; Unity displays supplied content and safe authored defaults.

| Stage | Chapters | Default support | Task |
|---|---|---|---|
| Arrival | 1–2 | Kannada/Roman/English; model audio if available | Use modelled phrase. |
| Daily tasks | 3–4 | Content-supplied code mixing or Roman support alongside Kannada; meanings accessible | Reuse requests, prices, times/directions. |
| Investigation | 5–7 | Kannada-forward, optional Roman/selected meanings | Extract facts, compare accounts, read notice. |
| Synthesis | 8–10 | Kannada-forward, all assistance available | Explain reason/present evidence with practised chunks. |

Kanglish in the brief conflates English–Kannada code mixing with Romanised Kannada. Keep Kannada script, Roman transliteration, English meaning and optional code-mixed phrasing separate. The engine team defines their progression.

Do not replace a fixed percentage per chapter. Fade support per concept after reuse, allow restoration, keep settings understandable. Core loop: encounter → supported attempt → visible consequence → notebook → reuse elsewhere → recall. Slice starting cap: 8–12 key words and 3–5 phrase patterns, tuned with reviewers/learners.

Notebook: people, objective, landmark map, observed phrases, source-linked evidence, timeline. Separate facts/rumours/deductions; allow replay/reinspection without conversation restart.

Signs stay legible authentic Kannada; progress adds annotation/translation, not blur/question marks. Relationship growth comes from help/familiarity, separate from language competence. English assistance, accent and sincere mistakes are not moral failures.

### Kannada text gate

Prove glyph shaping, vowel positioning, conjuncts, mixed scripts, wrapping, punctuation, fallback, word selection and input in an actual built-player specimen before mass authoring.

Evaluate UI Toolkit Advanced Text Generator; documentation describes complex shaping, but exact font/input/tooltip behaviour still needs validation in this editor/build. Noto Sans Kannada is a candidate, subject to licence/rendering checks. A font existing does not prove shaping.

Never split vowel signs/conjuncts into arbitrary character buttons. Start with full-line reveal or later cluster-aware animation. Static signs may use reviewed artwork and equivalent inspectable text; dynamic dialogue still needs a real text solution.

Require keyboard/click meanings, scalable subtitles, replay, contrast, motion settings, volume categories and full text alternative to speech. Assistance remains through finale; story clock pauses for reading, notebook/settings and network waits.

## 8. Shared Unity systems

| System | Responsibility | Needed |
|---|---|---|
| Player/camera | Walk/jog, slopes/collision, obstruction, focus, conversation control | Blockout |
| Interaction | Consistent prompt/action for people/signs/doors/props | Ch1 |
| Conversation UI | Speaker/line, layers, choices/text, replay/feedback, safe exit | Ch1 |
| Quest/state | IDs, prerequisites, transitions, one-time rewards | Ch1 onward |
| Notebook/evidence | Map, people, phrases, source-linked clues/deductions | Slice |
| Inspection/items | Handover, receipt/bicycle/notice/deed close-ups | Slice onward |
| World/time | Story day/block, weather, NPC anchors, finale/variants | Slice defaults onward |
| Loading | Entries/fades, session ownership, failure recovery | Slice |
| Save/load | Location, phase, clues/notebook, relationships, settings, content/save version | Slice |
| NPC presentation | Idle/work/talk/look-at, short schedules; companion later | Slice onward |
| Audio | Surface/shelter sounds, emitters, dialogue ducking | Benchmark |
| Debug authoring | Chapter jump, prerequisites, reset test state, mock outcomes | Foundation |

Proposed Assets/_Paridhwani folders: Scenes, Scripts, Data, Prefabs, Art, Materials, Audio, UI, Tests. Use data assets/stable IDs for quests/NPCs/clues rather than burying state in scene scripts. Reuse/remap current input actions. Cinemachine, navigation and narrative tooling are choices to evaluate only when needed, not confirmed dependencies.

Location hierarchy convention: Environment, Collision, Interactables, NPCAnchors, EntryPoints, LocalAudio, LocalLighting, StoryVariants.

## 9. Separate NLP engine boundary

Create a replaceable conversation provider. First provider uses reviewed local content and simulates success, misunderstanding, delay and failure. It is a fixture, not another NLP engine.

Unity owns world, permitted facts, NPC identity, quests, animation, UI, saves and consequences. Engine owns understanding, generation where appropriate, feedback and adaptation. Curriculum is coordinated with its team, not independently reinvented.

Conceptual exchange, not API specification:

- Unity supplies NPC/location, phase, allowed knowledge, learner context, goal and input.
- Engine supplies response and optional structured feedback/recognised intent.
- Unity validates proposed consequences against authored prerequisites before granting clues/unlocking paths. Displayed text alone never changes quests.

Stable IDs/request identity let Unity ignore late replies after exit. Provide progress, cancel, timeout/retry and authored fallback. Waiting never advances time or traps input. Backend secrets stay out of shipped builds.

Defer endpoints, transport, hosting, speech services, models and schemas as requested. Every scene works locally; future integration respects knowledge gates.

## 10. Production assets

Reusable kit:

- Architecture: roof/eaves, plaster variants, posts, doors/windows, plinths, low walls, steps, shutters/awnings.
- Ground: earth/stone/gravel, drains/crossings, puddles/leaves/moss, edges/rocks.
- Street: shelter/benches/noticeboard, wires/poles, crates/scales/vessels, clock, bicycle/auto/bus.
- Interior: shelves/files, counters/tables/chairs, lamps, curtains, cups/plates.
- Rural: areca planting, gate/fence, barrier/marker, shed/trunk.

Hero assets when required: bicycle/bag/bell, booking slip, archive reference, notice, lock/deed, construction vehicle. Share modules while giving library/farmhouse distinct silhouettes; concentrate bespoke detail on recognised/inspected objects. Track source/licence/credits for every sourced asset.

Slice cast: protagonist, Manjunatha, Somanna, Shanthakka; conductor may remain background. Full cast adds Basavaraju, Giri, Gurumurthy, Rathnamma, Shresthi, Kencha, Raghavendra, Kamalamma, Ananya, minimal council president. Reuse compatible rigs with distinct age/clothing/posture/voice.

Shared animations: idle, walk, turn, seated idle, talk, point, give/take, hold/inspect. Hero actions: pour/serve, key/door, trunk, deed handover. Prioritise work loops and clean prop alignment; defer full lip-sync until audio scope warrants it.

## 11. Milestones and build order

No reliable calendar estimate exists until team capacity, art sourcing and reference hardware are known. Use gates; estimate after benchmark/slice.

| Milestone | Deliverable | Exit gate |
|---|---|---|
| M0 Baseline/canon | Preserve changes, settle continuity/map/IDs and camera/input defaults | No impossible date/ticket/key/bridge logic in slice. |
| M1 Technical proof | Player blockout, NPC/sign, Kannada specimen, local provider, save/restore, one transition | Correct text in built player, input returns, no duplicate session objects. |
| M2 Visual benchmark | Finished tea corner/character, materials/light, shelter/rain audio | User can judge serious muddy village finish at gameplay camera. |
| M3 Greybox slice | Bus → tea → library → mess → note → recap, connected lane | Complete local loop with retries/saves. |
| M4 Polished slice | 25–35 minute target demo, coherent art/sound, settings/animation/text | Beginner and fluent reviewer test; record navigation confusion, recall and defects. |
| M5 Full greybox | Remaining places, interlude, return, finale/epilogue | Whole mystery completes with placeholders/authored dialogue. |
| M6 Location production | Apply kit to library/market/river, then hills/finale | Every location meets checklist/performance profile. |
| M7 NLP integration | Replace provider when separate engine is ready; keep fallback | Valid/invalid/late/unavailable handling; quest authority preserved. May start after M4 without blocking M5/M6. |
| M8 Release candidate | Reviewed language, integrated game, tuned pacing, credits/licences/build | Fresh and resumed full playthroughs pass on reference hardware. |

If four contributors are available, suggested workstreams are world/art/light; gameplay/state; UI/text/interaction; narrative/Kannada validation/audio. Separate engine remains its own workstream. These are suggestions, not assumed assignments or instructions to delegate now.

Slice requirements: movement/camera, three chapters, lane, notebook, correct Kannada, persistence, local dialogue, settings, benchmark quality.

Deferred from slice: full archive, river/temple/office/hills/farm, finale/crowd, endings, free speech, full schedules, economy, hunger/stamina, driving, procedural world/story, combat, large open world. Most are not required by the full narrative either.

## 12. Performance and completion standards

Provisional target: stable 60 FPS at 1080p on an agreed Windows laptop. Confirm hardware/quality/minimum at M1. This is a target, not a measured result.

Profile tea stall, market, rain, river and crowd in standalone builds; record CPU/GPU time, memory and stalls before fixing geometry/texture budgets. Use LOD/culling, modular assets/shared materials, limited shadow lights, restrained transparent foliage/rain and verified scene lighting/probes. Authored water/wetness replaces physical flood simulation. Wet surfaces need not all be mirrors. Bound NPC navigation/physics to gameplay needs.

When code exists, focused tests cover quest prerequisites, save compatibility, state restoration, one-time clues, scene ownership and interrupted/late conversations. Human review remains necessary for Kannada, learning usefulness, authenticity, camera comfort and visual tone.

Definition of done for every location:

1. Entry/exit, required/optional actions and revisits are playable.
2. Collision, doors, NPC approach and camera paths work at final scale.
3. A meaningful language action changes something or retrieves information.
4. Required clues have IDs, sources, prerequisites and fallback.
5. Kannada/mixed text renders and assistance permits understanding.
6. Mistakes, repeats, interruptions and unavailable NLP do not break progress.
7. Saves/world changes preserve state without duplicate rewards.
8. Audio/light/materials/props/animation meet benchmark; faces/text remain readable.
9. Transitions retain one player/camera/UI owner and safe spawn.
10. Performance is measured; licences/credits recorded.

## 13. Decisions to revisit and first implementation task

Review after proofs: Windows-first target, camera, Malnad reference, learning pace, protagonist presets, milestone clock, relationship/ending revisions, text renderer, art sourcing, team capacity and full duration. None blocks a first blockout. API details stay deferred.

Next implementation: M1 plus M2. Player approaches Manjunatha in a convincing rainy 3D tea-stall space, reads correct Kannada, orders a drink through authored content, records it in notebook, and saves/restores. This gives every subsequent scene a tested standard.

## Sources and technical references

- Creative source: [Pasted text(20261026-113235).txt](https://chatgpt.com/library?file_id=libfile_7bffc61ec09081919af38ded67e0026b&account_id=personal), read in full. Current instructions supersede its 2D recommendations. Continuity, assistance and ending changes here are proposed revisions.
- Local evidence: ProjectVersion.txt, Packages/manifest.json, EditorBuildSettings.asset, GraphicsSettings.asset, QualitySettings.asset, SampleScene.unity and Assets/Settings under game/My project (1), inspected 26 October 2026.
- [Unity 6000.6 SceneManager](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.html): runtime scene management; proposed architecture is our design choice.
- [Unity LoadSceneMode](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityengine/scenemanagement/loadscenemode): single/additive loading.
- [Advanced Text Generator](https://docs.unity.com/en-us/engine/6000.3/manual/uitoolkits/uielements/uie-work-with-text/uie-advanced-text-generator) and [enable/use guide](https://docs.unity.com/en-us/engine/6000.3/manual/uitoolkits/uielements/uie-work-with-text/uie-advanced-text-generator/enable-and-use-atg): shaping support reference, not proof of the particular Kannada font/input implementation in this editor/build.


## Implementation and art status — river iteration

The compact Unity prototype now covers arrival/tea, the library veranda, mess, market and a connected river bank, with shared TPP-to-FPP NPC conversations. Market tasks interpret a purchase entry and published bus service before recording Giri’s unverified river report. At the river, the player hears Giri’s account, inspects a 3D bicycle, retrieves and interprets an advance booking, and makes a qualified deduction before receiving the temple-witness lead. The temple and remaining chapters are future work. The same benchmark scene is retained for compatibility; scene splitting remains a later step.

The current visual assets are procedural stand-ins. The final-art benchmark remains open: rounded primitive faces, sphere canopies, simple walls and coarse ground must be replaced or refined in explicit art passes. Priorities are rigged adult characters/clothing, credible regional architecture, authored foliage, layered surface materials/drainage detail, restrained lighting and natural work animations. Finish one tea-stall/lane area to the intended standard before scaling that art quality across the village. Replacing visuals must retain each NPC’s identity/eye anchor, collisions, player scale, interaction references and saved quest IDs.
