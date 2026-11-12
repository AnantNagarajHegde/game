# Paridhwani UI and graphics template

Approved visual direction, November 2026. Use this as the baseline for future game UI and visual changes; extend it when a new feature needs a component rather than creating a competing style.

## Identity and palette

The game is a story-led Kannada adventure set in Kalyanapura. Keep the interface calm, legible, and grounded in the village. Use charcoal surfaces, warm ivory text and restrained brass accents. The current source of truth for UI values is `Assets/_Paridhwani/UI/TeaStall.uss`:

| Role | Colour |
|---|---|
| Ink | `#131819` |
| Panel | `#191F20` |
| Raised control | `#252C2C` |
| Border | `#434B4A` |
| Primary text | `#F2EDDF` |
| Secondary text | `#B5BEBA` |
| Brass accent | `#D5B579` |
| Player chat blue | `#2470E1` |

Use brass sparingly for primary actions, focus and small labels. Keep readable contrast, visible hover, focus and disabled states, and literal text without rich-text interpretation.

## Layout and components

- Frame the title in a left column over a live village view; keep Settings, chapter practice and Exit reachable in the footer. Continue or Begin is the primary action.
- Keep the exploration HUD compact and leave the village in view. Use the objective card, location badge and contextual interaction prompt consistently.
- Use the shared dark panels, spacing, typography, buttons, scrollbars and reading surfaces for menus, notebooks and evidence. Keep fixed actions accessible while long content scrolls.
- Pause uses a fixed sidebar for Your visit, Display, Audio, Controls and Accessibility. Keep Resume prominent in the header and Back outside the content scroll. Escape / controller Back returns from a category to Your visit, then to the world; title settings return to the title. Keep developer display options collapsed by default.
- Use `VillageSwitch` for binary settings and conversation aids. Keep the setting label, visible On/Off state and circular thumb together; brass indicates On. Retain native Toggle focus, keyboard and controller behaviour. Use the compact variant inside conversation/evidence aids; leave Foldout disclosure controls alone.
- Use `VillageSlider` for continuous settings: label and live percentage above the track, clear endpoints below, a brass filled portion and a circular thumb centred on the track. Retain native input and visible focus. Text size reports its actual scale percentage; other sliders report their position in the available range. Game audio uses a positive On/Off label and disables volume controls while off without discarding their levels. Keep its explanation in the existing description so changing audio does not shift the controls.
- Keep the notebook's current objective above separate Story notes and Words & phrases tabs. A notebook opened from Pause returns to Pause. Preserve all collected evidence and vocabulary when switching tabs.
- Use softly rounded surfaces, consistent spacing and restrained borders. Exploration shows the current objective and essential action hints; movement help lives in Controls. Avoid permanently displaying successful save messages over the world, but keep save errors visible.
- Evidence answer lists use explicit up/down controller navigation and scroll the selected row into view. Preserve stable control names and focus across retries; do not rely on spatial navigation to find clipped rows.
- Use the bundled Noto Sans and Noto Sans Kannada fonts with correct Kannada shaping. Preserve English and Roman-letter aids, text scaling, reduced motion and controller navigation.
- Retain the solid black right-side chat. Player messages use blue with white text; NPC messages use white with dark text. Frame the speaker in the uncovered world. Keep the message composer and language controls accessible.
- Put Send beside the message field, Notes and Leave in the conversation header, and the English / Romanized controls below the composer. Keep secondary actions quiet and preserve their controller focus.
- Show delivery state inside an outgoing bubble. Retrying the same failed message reuses that bubble; repeating a successfully delivered message creates a new one. Preserve the draft on failure and explain the safe failure category without copying raw server errors or credentials into the interface.
- Keep Phrases beside the composer actions. Render authored phrase choices in the chat's scroll area using the shared choice style, Kannada shaping and language aids. Use explicit up/down navigation and scroll focused choices into view; retain focus on aids across refreshes. Opening phrases cancels a pending reply and keeps the draft. Do not automatically open the controller keyboard over phrase choices.
- Vertical phrase buttons use their content height with an automatic flex basis; override the horizontal choice row's zero flex basis. Kannada, Roman and English labels must fit inside each button at every supported text scale.
- Use the QWERTY on-screen keyboard. When it opens, compress only the chat panel; do not move or resize the world viewport. Preserve typed text, focus, and controller shoulder actions.
- Design for multiple window sizes and the supported 0.85–1.3 text-scale range. The runtime panel uses a 1280×720 reference; do not overwrite the authored panel asset.

## World presentation

Maintain the existing stylized village identity, monsoon atmosphere and story-driven weather. Use soft cool sky fill, subtle warm bounce, restrained ACES colour grading and vignette. Avoid glare, excessive bloom, heavy grain or depth blur that obscures faces, signs or learning content. Keep materials responsive to lighting and tune quality settings with performance in mind. Preserve the player's display brightness and accessibility preferences.

Implementation anchors: `Scripts/Runtime/VillageArtDirection.cs`, `VillageLighting.cs`, `VillagePresentationSettings.cs`, `VillageTitleBackdrop.cs`, `VillageWalker.cs`, and `TeaStallUI.cs`, under `Assets/_Paridhwani`.

Residential streets use the six shared prefabs in `Generated/Residences`, regular setbacks, connected public lanes and clear veranda approaches. Keep the 14 story NPC bindings separate from the ambient residents. Background houses continue beyond visible railings and closed road exits; keep the whole playable perimeter physically sealed, including the river terrace. Distant houses use their simpler shared meshes and remain visible through the haze at every quality setting.

Use **Paridhwani > Update residential village and river** to apply the residential layer to an existing benchmark while preserving its story geometry and bindings. This command backs up the scene under `verification/Temp/VillageExpansion` before applying the update. Full benchmark generation includes the same layer. Authoring anchors: `Scripts/Editor/VillageExpansionBuilder.cs` and `VillageRiverLandscape.cs`; runtime anchors: `VillageAmbientResident.cs` and `VillageResidence.cs`. River flow, foam and rain rings share the story weather and reduced-motion settings.

## Movement and construction standards

- Preserve responsive camera-relative controls. Use gradual starts, short braking and finite turning speed; base gait on actual travel after collision, so pushing into a wall does not keep the player striding.
- Articulate knees, ankles and elbows. Coordinate stance, leg clearance, modest pelvis motion and footstep audio from the same travelled-distance phase. Keep the exploration camera free of artificial head bob or sprint FOV pumping. Preserve reduced motion.
- Stop movement immediately when focus is lost or a modal/conversation takes control. Restore, inspection and dialogue transitions must clear residual movement. Sweep the camera's final damped position against the world.
- Give buildings plausible construction: layered clay ridge caps, timber fascia and rafters, recessed joinery, stone sills and bonded foundations. Use subtle light-reactive surface relief and rain wear rather than arbitrary darkening or visual clutter.
- Generate deterministic visual detail with shared materials and batches per facade/roof. Keep doors attached to their original moving/visibility hierarchy. Do not add collision to trim or change story entrances, route clearances or inspection anchors.

Implementation anchors: `VillageMotion.cs`, `VillageWalker.cs`, `VillageCharacterGait.cs`, `VillageFootsteps.cs`, `VillageArchitecture.cs` and `VillageRoofWallTemplate.cs` in `Scripts/Runtime`, plus `Art/Shaders/VillageSurface.shader`. Runtime mesh/material ownership must be cleaned up when the scene unloads; authored source assets remain reusable.

## Review and verification

Review actual Unity captures at 960×540 and 1920×1080, including 1.0 and 1.3 text scale, title, exploration, notebook, settings, conversation and the keyboard. Confirm important actions remain visible and that keyboard opening does not move the camera. Use **Paridhwani > Review UI and graphics** for the isolated native check and `verification/Verify-Sources.ps1` for compilation and source checks. For changes touching story flows or shared evidence panels, run the presentation demo playtest as appropriate. Check physical input, performance and a standalone build separately when relevant; Editor checks alone do not establish those results.

Reference captures: `docs/visual-review/`. The initial verification record is `verification/ui-graphics-review.txt`.

For residential layout or river changes, use **Paridhwani > Review expanded village and graphics**. This also runs the movement/architecture and UI reviews, samples public and resident routes, checks house spacing and riverbank normals, and pushes the native player controller against every perimeter segment. Results are under `verification/Temp/VillageExpansion`; selected captures are in `docs/village-review`. The market shortcut retains the Friday works-vehicle closure, with the southern lane and panchayat link available as alternate routes.

For movement or construction changes, also run **Paridhwani > Review movement and architecture**. This checks the native CharacterController, gamepad input, acceleration, braking, reversing, modal stops, wall contact, articulated skinning, footfalls and construction geometry. It writes captures and measurements to `verification/Temp/PolishReview/`. Check the side-view gait and building close-ups as well as the normal camera. The durable review record and selected captures are in `verification/movement-architecture-review.txt` and `docs/polish-review/`.
