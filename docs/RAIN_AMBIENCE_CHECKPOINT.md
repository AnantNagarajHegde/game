# Rain / shelter transitions — 29 October 2026

Source-tested improvement on baseline `b998a0b`. Native physics/audio acceptance is pending: this execution environment lacks the `node_repl`/Computer Use tool required to inspect or operate Unity. No alternative UI route was attempted.

## Behaviour

`TeaAmbience` still uses the existing synthesized rainfall and eaves clips, the existing upward roof probe, ambience preference and player-layer exclusion. No sound asset, scene, emitter, saved preference or story content is added.

- The roof ray explicitly ignores trigger colliders, so an overhead trigger volume does not count as a roof.
- `RainShelterMix` confirms continuous cover for .12 seconds and continuous open sky for .30 seconds. Brief obstructions and gaps along tiled eaves do not repeatedly switch the shelter state.
- Cover crossfades over .6 seconds when entering and .9 seconds when leaving. Settled authored levels stay unchanged: outdoor rain/eaves .34/.03; sheltered .13/.16, scaled by the existing ambience slider.
- Mute, focus loss and zero volume silence immediately. Focus return, re-enable and nonzero slider changes ramp audibility over up to .5 seconds; shelter state continues updating while muted, avoiding an outdoor burst when resuming under a roof.
- Component disable or missing listener clears both source gains. Rain particle tracking, footsteps and the river emitter are otherwise unchanged.

The timing logic is independent of Unity physics/audio and uses unscaled elapsed time. Negative/non-finite time does not advance it; a single long frame advances at most .1 seconds to avoid a sudden mix jump. Gain is finite and clamped.

## Evidence and limits

`verification/Temp/RainAmbience-20261029/source-check.log` records Runtime/Editor/Player-source-branch compilation and **405 passing assertions**, including **18 focused checks** exercising the actual mixing class: continuous entry/exit, edge flicker, settled targets, volume scaling, immediate silence, muted shelter tracking, resume/disable behaviour, 30/120 Hz agreement and invalid/long-frame inputs. Source compilation does not run `AudioSource`, raycasts or playback.

The current main save SHA-256 stayed `87E29421458FF41E87774019B4B9EF27CA6C946A1D73C328BFFE47DC4E39F8E0`. Cast, controller, NLP and generated scene/assets are not changed by this batch. No push, security change, external service or new software was used.

The earlier intermittent `recovery writes fresh primary` file-store failure remains unresolved historical evidence; this passing aggregate run does not diagnose it. The final denser vegetation's visual acceptance and Somanna's close conversation framing repair also remain pending.

## Native acceptance when supported UI returns

Use an isolated visit and preserve the current main save. Walk repeatedly across the tea, library, mess and checkpost eaves at different speeds; listen for a smooth change without edge pumping or loop clicks. Check that real roofs still muffle rain and an overhead trigger does not. Verify both ambience extremes, mute/unmute, focus loss/return, Pause and component disable/re-enable; ensure no stale sound or outdoor burst. Listen to the unchanged river/footstep balance nearby. Confirm roof probe range and fade timing feel suitable on the actual audio device before signing off. No native listening, physics, human-input or standalone acceptance is claimed here.
