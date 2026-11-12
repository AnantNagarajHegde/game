# Arrival movie

The opening is a rendered 1920×1080, 24 fps film of the game's own village and characters. It replaces the live camera tour. The bus arrives and leaves, the archive worker looks for Ananya, and edited scenes introduce the library, field notes, language aids and Manjunatha's tea stall. The film reveals no later investigation answers.

- Watchable preview with English subtitles: [Paridhwani introduction](intro-movie/Paridhwani-introduction.mp4).
- In-game movie: `Assets/StreamingAssets/ArrivalMovie/arrival.mp4`.
- Scalable English subtitle timings: `Assets/_Paridhwani/Resources/ArrivalIntro/MovieTimeline.json`.
- Authored Kannada and English script: [storyboard](../verification/ArrivalMovie/storyboard.json).

New main visits play the movie before the welcome and controls. Skip and normal completion both preserve that welcome, record that the movie was seen, and grant no quest clues. Continue resumes an existing visit without replaying it. Welcome offers replay; practice and presentation scenes bypass the movie. The video is local and needs no speech service during play.

Space or controller Menu pauses/resumes; the fixed buttons support pointer and controller focus. Escape or controller Back returns to title without marking an unfinished movie complete. Focus loss pauses playback. The movie uses fixed camera compositions and cuts, without camera shake or automatic fly-throughs. Subtitles follow the video decoder's time and the player's text scale. The soundtrack follows mute and master volume; world audio is suspended and restored around playback.

The Kannada narration is synthetic, using Microsoft's `kn-IN-GaganNeural` voice at -5% rate through the authoring-only `edge-tts` tool. The user explicitly approved sending the six narration passages to Microsoft. The script, durations and audio hashes are recorded in [ARRIVAL_NARRATION.json](ARRIVAL_NARRATION.json). Rain, bus and boiler recordings reuse the sources already credited in [third-party notices](../THIRD_PARTY_NOTICES.md). No downloaded models, image-generated artwork, player data or external runtime requests are used.

## Reproduction

1. `verification/Prepare-ArrivalNarration.py --ffmpeg PATH` generates the approved Kannada lines and timing manifest. It requires the authoring `edge-tts` dependency and network access. The game's runtime has neither dependency.
2. Copy the Unity project's `Assets`, `Packages`, `ProjectSettings` and cached `Library/PackageCache` into `verification/Temp/ArrivalMovie/UnityProject`. Do not share writable Unity cache directories with the open project.
3. `verification/Render-ArrivalMovie.ps1 -Proofs` renders proof frames. Inspect all shots. The same command without `-Proofs` renders the full film.
4. `verification/Encode-ArrivalMovie.py --ffmpeg PATH` mixes the Kannada audio and credited ambience, adds titles and exports the shipping MP4 and English-subtitle preview.
5. `verification/Verify-Sources.ps1` checks compilation and logic. `verification/Verify-ArrivalMovie.ps1` copies the current assets into the isolated project and exercises actual Unity video decoding, playback and UI layout. `Paridhwani > Review arrival introduction` runs the movie and shared UI review from a desktop Editor.

The isolated authoring scripts stage actors and the bus only in memory. They never regenerate or save the playable source scene. Raw frames and authoring dependencies stay under ignored `verification/Temp`; only the movie, preview, timing/script and reproducible source are deliverables.

Verification: [native playback and source check record](../verification/arrival-movie-check.txt), including the final asset hashes and the limits of the checks performed.
