# Sarvam Kannada TTS: Team Handoff

## Purpose

This note records the current Paridhwani game branch work to add a higher-quality
Kannada voice option, explains what has been checked without Unity, and lists the
remaining integration checks. It contains no API credentials.

## What changed

- `village_backend/server.py` now supports Sarvam as a selectable speech provider
  on the existing `POST /v2/village/speech` route. Unity's request contract did
  not need to change.
- Set `PARIDHWANI_TTS_PROVIDER=sarvam` to select Sarvam Bulbul v4 Flash. The
  default model is `bulbul:v4-flash`; the backend-only `SARVAM_API_KEY` is never
  sent to Unity.
- Speech requests use `kn-IN`, request 24 kHz mono WAV, validate the returned WAV,
  and return it with `Content-Type: audio/wav` for the existing Unity audio path.
- A local disk cache stores generated WAV by model, voice, language and exact
  dialogue text under `village_backend/.tts-cache` by default. It is ignored by
  Git. Identical requests can be served again without another Sarvam synthesis
  request.
- Existing Azure Speech remains selectable with
  `PARIDHWANI_TTS_PROVIDER=azure` and its existing Azure environment settings.
- NPC mapping currently uses Sarvam's Chetan Kannada conversation voice for male
  NPCs and Chaitra Kannada conversation voice for female NPCs. This is a simple
  gender grouping, not individualized casting. The mapping is in `SARVAM_SPEAKERS`
  in `village_backend/server.py`.
- `village_backend/preview_sarvam_tts.py` generates standalone Chetan/Chaitra
  sample WAV files without Unity. Its output directory is ignored by Git.
- `Start-VillageConversation.ps1` recognizes the Sarvam environment settings.
  More provider setup and deployment notes are in
  `docs/HOSTED_CONVERSATION_SETUP.md`.

## What has been verified

1. The standalone preview generated a 24 kHz mono Chetan WAV, about 4.88 seconds
   long.
2. The initial preview mapped both sample labels to the male voice. That mapping
   bug was fixed; regenerating the Chaitra sample produced a female voice.
3. The local Python HTTP service started successfully on `127.0.0.1:8767`.
4. A request to `/v2/village/speech` initially failed because Windows PowerShell
   sent the Kannada JSON body with the wrong encoding. Sending the JSON as UTF-8
   bytes succeeded and saved `sarvam-samples/game-route-test.wav`.
5. All 48 existing backend unit tests passed after the service change. Python
   syntax checks passed for the preview and server.

These checks validate the provider call, local HTTP route, voice selection and
WAV response. They do **not** verify Unity decoding, audio playback, subtitles,
timing, NPC flow, or behavior when the provider is unavailable.

## Reproducing the standalone voice preview

From a PowerShell window opened in the `game` directory:

```powershell
$env:SARVAM_API_KEY = '<your Sarvam API subscription key>'
python village_backend\preview_sarvam_tts.py --voice both
```

The script writes `sarvam-samples\chetan.wav` and `sarvam-samples\chaitra.wav`.
Open them with Windows Media Player. Use `--text "..."` to preview a Kannada
line. Do not commit the key or generated audio.

## Reproducing the local HTTP speech test without Unity

### Terminal 1: start the speech service

Open PowerShell in the `game` directory. Set the environment variables in this
same terminal before launching the service:

```powershell
$env:PARIDHWANI_TTS_PROVIDER = 'sarvam'
$env:SARVAM_API_KEY = '<your Sarvam API subscription key>'
python village_backend\server.py --provider ollama --model preview --port 8767
```

`ollama` is only the unused chat-provider selection here; the speech route calls
Sarvam directly. Do not use this command as a complete conversation-server test.
Keep the terminal open. The provider and key are read when the server starts.

### Terminal 2: post a UTF-8 request

Open a second PowerShell window in the `game` directory:

```powershell
$body = @{
  speaker = 'ananya'
  language = 'kn-IN'
  text = 'ನಮಸ್ಕಾರ, ನಮ್ಮ ಊರಿಗೆ ಸ್ವಾಗತ.'
} | ConvertTo-Json -Compress

$utf8Body = [System.Text.Encoding]::UTF8.GetBytes($body)
New-Item -ItemType Directory -Force '.\sarvam-samples' | Out-Null
Invoke-WebRequest `
  -Uri 'http://127.0.0.1:8767/v2/village/speech' `
  -Method Post `
  -ContentType 'application/json; charset=utf-8' `
  -Body $utf8Body `
  -OutFile '.\sarvam-samples\game-route-test.wav'
```

Use `speaker = 'manjunatha'` to request the male mapping. Open the saved WAV to
listen. Repeating an identical request should hit the backend cache. If the
server reports HTTP 400, ensure the request body is sent as UTF-8 bytes as shown;
the server currently masks Sarvam's detailed error body in its client response.

## Unity integration: next steps

On a teammate's machine with the Unity project and a supported Unity version:

1. Fetch/switch to this feature branch after the branch and its commits are
   pushed/shared. Confirm `server.py`, `preview_sarvam_tts.py`, and this handoff
   file are present.
2. Start the local service with `PARIDHWANI_TTS_PROVIDER=sarvam` and a valid
   `SARVAM_API_KEY` in its environment. Never put the Sarvam key in Unity files,
   assets, logs, or source control.
3. Start the Unity project with its service URL set to
   `http://127.0.0.1:8767/` (or leave the default). If service and Unity run on
   the same computer, localhost is correct.
4. Enter a scene with an NPC that speaks Kannada and trigger a new dialogue line.
   Check that the correct voice plays, audio stops on new dialogue/focus loss,
   and subtitles/text remain usable if speech fails.
5. Check a male NPC and a female NPC. Current mappings are group-based, so adjust
   the mapping if narrative casting requires a different assignment.
6. Inspect the service output for HTTP errors and compare first-request versus
   repeat-request behavior. A repeat should be served from the WAV cache.
7. Check at least one authored line and one generated NPC reply, then verify
   spoken Kannada agrees with the displayed Kannada text.
8. Confirm what build/runtime setup teammate used and record any Unity version,
   audio decoding, latency, or platform-specific issue in the branch notes.

### Sharing the service across two computers

The service currently binds to localhost by default. Unity on another computer
cannot call the developer's `127.0.0.1`. For a teammate to use a service running
on your machine, the backend must be explicitly configured to listen on the LAN,
Windows Firewall must allow the port, and Unity must use your LAN address. This
exposes a paid provider-backed endpoint to the LAN. Coordinate before enabling
it; do not bind publicly or expose it to the internet. The simpler test is for
the Unity teammate to run the service locally with their own authorized API key.

## Known constraints / open decisions

- Only Kannada (`kn-IN`) is supported by the Sarvam branch of the speech route;
  English lines should use Azure or remain text-only.
- Bulbul voice assignments need a Kannada-speaking reviewer to judge pronunciation,
  naturalness, speaker distinction, pace and suitability for each character.
- The current HTTP/Unity test uses a short phrase. Check the game's longest NPC
  lines against provider limits and Unity's 4 MiB response cap.
- Live API verification depends on a valid Sarvam key, account access and credits.
- This work adds the TTS provider path; it does not add Sarvam 105B NPC response
  generation. Conversation generation and story/action logic remain on their
  existing providers and Unity flow.
- This handoff is intended to be committed on `amg/manjunatha-joint`; check the
  branch head if you are reading a separately downloaded copy.

## References

- Sarvam TTS request format: <https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/rest-api>
- Sarvam Kannada voice catalog: <https://docs.sarvam.ai/api/api-guides-tutorials/text-to-speech/voices>
- Existing setup and hosting notes: `docs/HOSTED_CONVERSATION_SETUP.md`
