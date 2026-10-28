# Vendor liveliness prototype - source implementation

28 October 2026. **Implemented in source; native import, rendering and playtest acceptance pending.** This is a small implementation of the short routines/work presentation in UNITY_GAME_PLAN sections 8 and 10, not a percentage of a completed living-world system.

## Behaviors

- Manjunatha steps back into the tea-stall work lane, visits the boiler and cup area with different arm/head gestures, and returns to a resting/service position.
- Basavaraju alternates checking the ledger, tending the scales and resting at the market counter. These are procedural gestures; props are not picked up or physically manipulated.
- Each loop has its own seeded timing, different work/rest durations, slow short steps and smooth arm transitions. Idle gaze varies rather than continuously tracking a distant player while working.
- The two actors move their root, collider and eye anchor together. Work offsets remain within 1.12 m of their authored origins. No additional NPCs, crowds, dialogue, schedules or NLP behavior are introduced.

## Integration and safeguards

`VendorWorkRoutine.cs` is a transient, engine-independent work loop. `VillageVendorWork.cs` binds it to the current actors through the existing `VillageCharacter.cs` session presentation hook. No scene/generator, input/controller, quest, save schema, terrain/material or NLP files change in this batch. An ordinary source import is required before Unity can run it; there is no new scene-generation requirement introduced by these routines.

Conversations, evidence/menu screens, camera transitions, lost application focus and close player approach hold movement. Conversation gestures take over, and work resumes from the interrupted step afterward. Menus hold non-speaking NPC presentation. Friday finale staging disables these work loops; epilogue/practice/load repositioning takes precedence, with transient offsets reset when an actor is restored. An actor moved beyond its local work patch is left to story staging.

Movement checks floor height/slope, player clearance and nearby colliders, and stops when blocked. Small steps and a reused overlap buffer bound movement/query cost. Source inspection found the original tea anchor already slightly overlapping the counter; movement may reduce that existing overlap to step back, but must not deepen it or introduce a new overlap. This collision behavior requires native verification.

## Checks completed

- `verification/Verify-VendorWork.ps1`: **167 focused assertions passed**. Tests execute the actual work-loop source in .NET: interruption/resumption during work and travel, blocked/released movement, invalid/long frame times, restoration, different role timing and varied rests. Six long runs cover **72,000 movement samples** and selected authored counter/boiler/bench/crate horizontal footprints. These are mathematical path checks, not Unity collision or animation acceptance.
- `verification/Verify-Sources.ps1`: runtime, Editor and Player-source-branch compilation against cached Unity references; **387 existing assertions passed** in the combined workspace. This includes concurrent source; it is not a new Player build.
- Separate Runtime compilation against the committed baseline plus only this batch passed without concurrent controller/NLP source. This establishes source compatibility, not native import or execution.
- Raw local check outputs: `verification/Temp/VendorWorkVerification/` and `verification/Temp/vendor-work-combined-source.log`. Summary: `verification/vendor-work-source-check.txt`.

## Pending native gate

Native rendering and interaction are paused while the Windows Security prompt awaits the user. No attempt was made to interact with it, change permissions, restart/close Unity or bypass the earlier denied window action. No new screenshots or visual acceptance are claimed.

After the user clears the prompt, safely import the source and check:

1. Observe at least two loops per vendor: no foot sliding, body/prop intersections, inappropriate gestures or repeated spinning. Review actual work pose alignment and adjust within the existing art direction if necessary.
2. Interrupt travel and each task via keyboard and controller conversation; verify a stable eye/camera target, readable replies, exit and smooth resumption. Open evidence/notebook/Pause while work is active.
3. Stand near/in the work lane; check that neither vendor pushes the player or walks through props. Confirm both remain reachable from the normal approach throughout the loop.
4. Save/load, enter/leave chapter practice/demo, stage Friday, and return to the epilogue. Confirm no routine pulls a staged actor back to the stall or alters story state.

Then capture actual in-game clips/stills of the routines alongside the separately pending tea-stall art preview. Do not treat these source checks as playtesting. No further source feature is needed before this gate.
