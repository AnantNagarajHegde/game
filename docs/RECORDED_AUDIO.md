# Recorded village audio

The game now uses imported audio in `Assets/_Paridhwani/Resources/VillageAudio`, loaded locally through `VillageRecordedAudio`. No audio is downloaded during gameplay. See `AUDIO_ASSETS.json` and `../THIRD_PARTY_NOTICES.md` for sources, licenses, transformations and file hashes.

## Playback

- Outdoor rain (50 seconds), gutter/roof runoff (48 seconds), and river water (60 seconds) replace runtime noise synthesis. Two sources overlap using DSP scheduling, up to 1.5 seconds per transition. No periodic sine modulation is added.
- Existing shelter hysteresis and story-driven weather clearing remain. Sheltered outdoor rain is low-pass filtered; roof runoff becomes more prominent. Rain and river duck to 65% during dialogue.
- Five imported footsteps per earth/stone/timber category replace synthesized transients. Selection never immediately repeats the preceding variant for that surface. Grounding, movement eligibility, travelled-distance cadence, independent volume and controller step events are preserved.
- Localized boiling-water loops and occasional utensil sounds mark the tea stall and mess. A sparse bell marks the temple; birds/insects mark forest/farm areas and frogs the river. These stop or quiet during dialogue; wildlife is not claimed as regionally authenticated.
- Pour/cup, meal serving, library door, trunk latch/creak, deed paper, evidence handling and notebook open/close sounds follow local presentation/state changes. Restoring a save/practice chapter seeds state without replaying earlier cues.
- A bus idle is limited to arrival, and an auto loop to the travel activity. Emitters use linear distance attenuation, with Doppler disabled. The auto recording is from Mumbai; no language/crowd track is added to simulate Kannada conversation.
- Environment and interaction cues follow the existing ambience volume/mute control; footsteps retain their own slider. Master volume remains global. Sources silence on focus loss. Imported clips are shared assets and are not destroyed at teardown.

Bindings are installed by `TeaAmbience.Start`, so the current generated scene and additive world use them without a scene regeneration. No scene, recipe or save schema is changed by the audio implementation.

## Preparation and verification

`verification/Prepare-RecordedAudio.py` prepares the files from ignored downloads under `verification/Temp/AudioSources`, and writes the provenance/measurement manifest. It requires NumPy, SciPy, SoundFile and imageio-ffmpeg; dependencies are local tools, not game packages. Preparation filters DC/sub-bass, trims, matches levels, adds tiny edge ramps, encodes OGG, then checks decoded files for finite samples and sample clipping. This is not a listening test, LUFS/true-peak certification or proof of Unity's final output mix.

Long ambience uses compressed-in-memory import settings and short foley uses decompression on load. All assets retain GUIDs when preparation reruns. Sources are lossy HQ derivatives where stated; upsampling/encoding is not a source-quality improvement.

The source checks cover movement/cadence, non-repeating footstep selection, shelter mixing, persistence and game regressions. Unity import evidence and current test results are recorded in `verification/recorded-audio-check.txt`.

Remaining specialist work: reviewed Kannada pronunciation/voices, original or licensed regional music, regionally verified wildlife/crowd recordings and dedicated wet-mud/puddle foley. These are deliberately not filled with unrelated speech/music or further procedural noise. Headphone/speaker listening acceptance of the final Unity mix remains necessary.
