# Standalone location baseline — 28 October 2026

This is a measured baseline on the current laptop, not performance approval or a minimum-spec claim. No performance threshold has been agreed. No optimization, scene streaming, graphics option, power setting or security setting was changed.

## Build and machine

- Unity 6000.6.7f1 Windows development player, Direct3D12, active renderer **NVIDIA GeForce RTX 4050 Laptop GPU**.
- Intel Core 5 210H, 8 physical/12 logical cores, approximately 15.64 GiB usable RAM. Windows 11 Home Single Language, build 26300.
- Native display: 1920x1200 at 144 Hz; Windows Balanced power plan. AC/battery state, temperatures and clocks were not captured. The idle Editor and ordinary desktop applications remained open.
- Measured output: **1920x1080**, windowed, Balanced game quality, URP render scale **0.9**. VSync count **1**, application targetFrameRate **60**; both retained from the game. No deep profiler was attached.
- Source base: **217bed26173f6a25fa673ea7093737c0c0576eac plus the recorded dirty controller work and this profiling instrumentation**. The diagnostic source list/hashes were captured before building, and no source changed during the build.
- Diagnostic executable: `My project (1)/Builds/Profiling-20261028-0947/Paridhwani.exe`.
- Managed game assembly SHA-256: `08e84aa0357648b468519976355aae15e3ec57a55f6c1192ce3357e91e7c2ee2`.
- The previous verified runnable ZIP remains untouched. The normal build output now contains diagnostic-capable sources; it is not a newly full-story-accepted replacement for that frozen ZIP.

## Method and validity

The opt-in `-village-location-profile` flag creates a unique temporary save and never reads/migrates the player's main save. It stages the existing authored chapter state at five fixed viewpoints. It does not add a language engine or alter story outcomes in ordinary play.

Each location receives three seconds of warmup, metadata queries, eight drain frames, then 600 sampled frames. Two passes give **1,200 frames per location / 6,000 total**. All ten windows recorded exactly 600 focused frames at the intended resolution. Locations run in the same order in both passes. Cameras are stationary; this does not measure travel/loading, dialogue rebuilds, maximum camera coverage, a long session or final production assets.

Frame intervals use `Time.unscaledDeltaTime`. CPU total, main/render thread, present-wait and GPU times use FrameTimingManager. Its results are delayed, and unavailable/duplicate/non-positive timings are excluded with sample counts retained. GPU coverage was 589–599 of 600 frames per window. Percentiles use nearest rank. Raw CSV-derived frame means and p95 values were independently recomputed and matched the JSON report. Capture/write work happens after sampling; the next location warms up before measuring.

Unity allocated/reserved and managed memory are endpoint snapshots for the **whole loaded process**, not memory owned by each location. The reported .NET process-working-set field returned zero in this player and is **unavailable**, not zero RAM usage. The player had already exited when an external counter cross-check started, so it produced no samples; the run was not repeated solely to fill that optional field. Profiling allocations and normal caching can contribute to the small endpoint growth.

## Results

The table combines both passes. GPU means use available positive timing samples; memory ranges show the two after-sampling endpoints.

| View | Mean interval, ms | p95, ms | Maximum interval, ms | GPU mean, ms | Unity allocated, MiB |
|---|---:|---:|---:|---:|---:|
| Tea stall | 6.978 | 6.950 | 34.723 | 1.706 | 223.46–224.23 |
| Market | 6.949 | 6.950 | 13.891 | 1.669 | 223.34–224.13 |
| River / Giri shed | 6.972 | 6.950 | 27.773 | 1.403 | 223.76–224.09 |
| Open rain lane | 6.967 | 6.950 | 34.717 | 1.432 | 223.76–224.15 |
| Council court | 6.972 | 6.950 | 34.724 | 1.705 | 224.40–224.73 |

Unity reserved memory was **804.53 MiB** at all ten endpoints. Managed used memory ranged **14.13–16.28 MiB**. Main-thread means ranged **3.48–4.05 ms**, with present-wait means **2.90–3.49 ms**. These categories should not be summed with GPU time, which overlaps CPU work.

The run completed 09:48:49–09:50:02 UTC and exited normally with code 0 and `PARIDHWANI_LOCATION_PROFILE_OK`. All ten context screenshots were produced; the five second-pass images were inspected to confirm the intended viewpoints. No exception/error patterns were found in the player log. Source compilation and 387 combined assertions passed; the profiling changes also compiled independently of the controller work and passed the existing 367 assertions. A full story smoke was not repeated for this diagnostic build.

## Interpretation and decisions before changes

The roughly 6.95 ms cadence is consistent with the 144 Hz display. On desktop, VSync count 1 follows the display refresh and takes precedence over targetFrameRate; the configured 60 value therefore does not establish a 60 FPS cap. This interpretation follows [Unity's VSync documentation](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/QualitySettings-vSyncCount.html).

GPU means below 1.8 ms and nontrivial present waits do not show sustained GPU saturation in these fixed views. This is an inference from the sampled workload, not proof about low-end hardware or moving/final-art scenes. Isolated 13.9–34.7 ms frame spikes remain. Some CPU timing windows also contain large outliers, while GPU times stay short; the cause cannot be assigned to GC, scripts, streaming, drivers or desktop scheduling without a CPU timeline/counter trace.

The similar memory totals are expected with the monolithic world remaining loaded; they neither establish a leak nor quantify possible streaming savings. Renderer `isVisible` counts in the raw report are diagnostic counts, not draw calls or a geometry budget.

Before optimization or streaming changes, agree **target hardware tier, output resolution/quality and frame-pacing policy**: native-refresh VSync versus an intentional 60 FPS policy. Recommendation: preserve current settings, investigate the intermittent CPU-side stalls with a timeline trace if they matter to that target, then profile representative traversal and lower-end hardware. This short run provides no basis for a streaming rewrite or a minimum-spec sign-off. Physical-controller, fluent-Kannada and human/display acceptance remain separate.

## Evidence and reproduction

- [Raw JSON](../verification/performance/2026-10-28/report.json), [combined CSV](../verification/performance/2026-10-28/summary.csv), per-frame CSVs in the same directory, hardware/build hashes and process exit record.
- Complete local evidence: `verification/Temp/PerformanceBaseline-20261028-094607/`, including source hashes/patch/copies, player log, screenshots and the untouched raw measurements.
- Reproduce from a normal desktop shell with the diagnostic build: `Paridhwani.exe -village-location-profile -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -logFile <local-log-path>`. Keep its window focused. It ends after two passes and writes beneath the OS temporary directory's `ParidhwaniPerformance` folder.
- API interpretation: [FrameTimingManager](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FrameTimingManager.html), [FrameTiming fields](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FrameTiming.html). Timings include defined waits and are not interchangeable with total physical process memory or uncapped throughput.
