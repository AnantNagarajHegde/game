using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Paridhwani
{
    // Opt-in diagnostic player. Fixed cameras, isolated progress, no graphics-setting overrides.
    public sealed class VillageLocationProfile : MonoBehaviour
    {
        public static bool Requested => Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(), "-village-location-profile") >= 0;
        const int Samples = 600, Passes = 2;
        [Serializable] public sealed class Metric
        {
            public int samples;
            public double mean, p50, p95, p99, max;
            public static Metric From(List<double> values)
            {
                var result = new Metric { samples = values.Count };
                if (values.Count == 0) return result;
                values.Sort(); double sum = 0; foreach (double value in values) sum += value;
                result.mean = sum / values.Count; result.p50 = values[(int)Math.Ceiling(values.Count * .5) - 1];
                result.p95 = values[(int)Math.Ceiling(values.Count * .95) - 1]; result.p99 = values[(int)Math.Ceiling(values.Count * .99) - 1];
                result.max = values[values.Count - 1]; return result;
            }
        }
        [Serializable] sealed class Memory
        {
            public long unityAllocated, unityReserved, managedUsed, processWorkingSet;
            public static Memory Read()
            {
                using (var process = System.Diagnostics.Process.GetCurrentProcess())
                    return new Memory { unityAllocated = Profiler.GetTotalAllocatedMemoryLong(), unityReserved = Profiler.GetTotalReservedMemoryLong(),
                        managedUsed = Profiler.GetMonoUsedSizeLong(), processWorkingSet = process.WorkingSet64 };
            }
        }
        [Serializable] sealed class Location
        {
            public string name, quality, csv, capture;
            public int pass, width, height, vSyncCount, targetFrameRate, focusedFrames, visibleRenderers;
            public float renderScale;
            public Vector3 position; public float heading, pitch;
            public Memory before, after;
            public Metric frameMs, cpuTotalMs, mainThreadMs, renderThreadMs, presentWaitMs, gpuMs;
        }
        [Serializable] sealed class Report
        {
            public string state = "running", startedUtc, completedUtc, currentLocation, failure, unityVersion, operatingSystem, processor, graphicsDevice, graphicsApi, graphicsDriver;
            public int processorCount, systemMemoryMiB, graphicsMemoryMiB, requestedWidth = 1920, requestedHeight = 1080;
            public string method = "Two fixed-view passes; 3 seconds warmup then 600 frames per location. Balanced quality, existing VSync/target FPS preserved. Frame intervals include waits. FrameTimingManager data is delayed; non-positive/unavailable timings have zero samples, not measured zero cost. Memory is a whole-process endpoint snapshot, not location-owned memory. Screenshots and file writes occur outside sampling. Development player; no performance threshold or minimum-spec certification.";
            public List<Location> locations = new List<Location>();
        }
        struct Scenario
        {
            public string name; public ReplayChapter chapter; public Vector3 position; public float yaw, pitch;
            public Scenario(string name, ReplayChapter chapter, Vector3 position, float yaw, float pitch = 17)
            { this.name = name; this.chapter = chapter; this.position = position; this.yaw = yaw; this.pitch = pitch; }
        }
        struct Sample { public double frame, cpu, main, render, wait, gpu; public int frameNumber; }
        TeaStallSession session;
        Report report;
        string output;
        bool finished, originalBackground;
        float deadline;
        public void Begin(TeaStallSession value)
        {
            if (!Requested || !value.IsIsolatedTestVisit || !value.IsAutomatedTest) { enabled = false; return; }
            session = value; originalBackground = Application.runInBackground; Application.runInBackground = true;
            output = Path.Combine(Path.GetTempPath(), "ParidhwaniPerformance", "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            report = new Report { startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion,
                operatingSystem = SystemInfo.operatingSystem, processor = SystemInfo.processorType, processorCount = SystemInfo.processorCount,
                systemMemoryMiB = SystemInfo.systemMemorySize, graphicsDevice = SystemInfo.graphicsDeviceName,
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), graphicsDriver = SystemInfo.graphicsDeviceVersion, graphicsMemoryMiB = SystemInfo.graphicsMemorySize };
            deadline = Time.realtimeSinceStartup + 300;
            try { Directory.CreateDirectory(output); WriteReport(); StartCoroutine(Guarded()); }
            catch (Exception error) { Complete(error.ToString()); }
        }
        void WriteReport() => File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
        void Complete(string error)
        {
            if (finished) return; finished = true;
            report.failure = error ?? ""; report.state = error == null ? "completed" : "failed"; report.completedUtc = DateTime.UtcNow.ToString("O");
            WriteReport(); Debug.Log("PARIDHWANI_LOCATION_PROFILE_" + (error == null ? "OK " : "FAILED ") + output + (error == null ? "" : "\n" + error));
            Application.Quit(error == null ? 0 : 2);
        }
        void Update() { if (!finished && report != null && Time.realtimeSinceStartup > deadline) Complete("Profiling exceeded 300 seconds."); }
        void OnApplicationQuit()
        {
            if (!finished && report != null) { report.state = "interrupted"; report.failure = "Player closed before all locations completed."; WriteReport(); }
        }
        void OnDestroy() { Application.runInBackground = originalBackground; }
        IEnumerator Guarded()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            while (stack.Count > 0)
            {
                bool next; object current = null;
                try { next = stack.Peek().MoveNext(); if (next) current = stack.Peek().Current; }
                catch (Exception error) { Complete(error.ToString()); yield break; }
                if (!next) { stack.Pop(); continue; }
                if (current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
            Complete(null);
        }
        IEnumerator Run()
        {
            var scenarios = new[] {
                new Scenario("Tea", ReplayChapter.Arrival, new Vector3(0, .2f, -4.8f), -32),
                new Scenario("Market", ReplayChapter.Market, new Vector3(3.2f, .2f, 8.5f), 60),
                new Scenario("River", ReplayChapter.River, new Vector3(-21, .2f, 6.5f), -105),
                new Scenario("RainLane", ReplayChapter.Arrival, new Vector3(1.4f, .2f, 6.5f), 0, -10),
                new Scenario("Council", ReplayChapter.Council, new Vector3(-2.2f, .2f, 7.3f), 0)
            };
            for (int pass = 1; pass <= Passes; pass++) foreach (var scenario in scenarios)
            {
                report.currentLocation = scenario.name + " / pass " + pass; WriteReport();
                session.PrepareLocationProfile(scenario.chapter, scenario.position, scenario.yaw, scenario.pitch);
                var timings = new FrameTiming[1];
                float warmUntil = Time.realtimeSinceStartup + 3;
                while (Time.realtimeSinceStartup < warmUntil) { FrameTimingManager.CaptureFrameTimings(); yield return null; }
                var row = new Location { name = scenario.name, pass = pass, quality = session.Progress.quality.ToString(), width = Screen.width, height = Screen.height,
                    vSyncCount = QualitySettings.vSyncCount, targetFrameRate = Application.targetFrameRate, position = session.player.transform.position, heading = scenario.yaw, pitch = scenario.pitch,
                    renderScale = (QualitySettings.renderPipeline as UniversalRenderPipelineAsset)?.renderScale ?? 1, before = Memory.Read() };
                foreach (var renderer in FindObjectsByType<Renderer>()) if (renderer.enabled && renderer.isVisible) row.visibleRenderers++;
                var samples = new Sample[Samples]; ulong lastTiming = 0;
                // Drain metadata-query/allocation frames and FrameTimingManager's delayed results.
                for (int settle = 0; settle < 8; settle++) { FrameTimingManager.CaptureFrameTimings(); yield return null; }
                for (int index = 0; index < Samples; index++)
                {
                    yield return null;
                    if (session.ui.IsModal) throw new InvalidOperationException("A modal interrupted fixed-view sampling.");
                    if (Screen.width != row.width || Screen.height != row.height) throw new InvalidOperationException("Resolution changed during sampling.");
                    if (Application.isFocused) row.focusedFrames++;
                    var sample = new Sample { frame = Time.unscaledDeltaTime * 1000.0, frameNumber = Time.frameCount };
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].frameStartTimestamp != lastTiming)
                    {
                        var timing = timings[0]; lastTiming = timing.frameStartTimestamp;
                        sample.cpu = timing.cpuFrameTime; sample.main = timing.cpuMainThreadFrameTime; sample.render = timing.cpuRenderThreadFrameTime;
                        sample.wait = timing.cpuMainThreadPresentWaitTime; sample.gpu = timing.gpuFrameTime;
                    }
                    samples[index] = sample;
                }
                row.after = Memory.Read();
                var frame = new List<double>(Samples); var cpu = new List<double>(Samples); var main = new List<double>(Samples);
                var render = new List<double>(Samples); var wait = new List<double>(Samples); var gpu = new List<double>(Samples);
                var csv = new StringBuilder("sample,frameNumber,frameMs,cpuTotalMs,mainThreadMs,renderThreadMs,presentWaitMs,gpuMs\n");
                for (int index = 0; index < samples.Length; index++)
                {
                    var sample = samples[index]; frame.Add(sample.frame); Add(cpu, sample.cpu); Add(main, sample.main); Add(render, sample.render); Add(wait, sample.wait); Add(gpu, sample.gpu);
                    csv.Append(index).Append(',').Append(sample.frameNumber).Append(',').Append(Value(sample.frame)).Append(',').Append(Value(sample.cpu)).Append(',')
                        .Append(Value(sample.main)).Append(',').Append(Value(sample.render)).Append(',').Append(Value(sample.wait)).Append(',').Append(Value(sample.gpu)).Append('\n');
                }
                row.frameMs = Metric.From(frame); row.cpuTotalMs = Metric.From(cpu); row.mainThreadMs = Metric.From(main); row.renderThreadMs = Metric.From(render); row.presentWaitMs = Metric.From(wait); row.gpuMs = Metric.From(gpu);
                row.csv = scenario.name + "-" + pass + ".csv"; File.WriteAllText(Path.Combine(output, row.csv), csv.ToString());
                row.capture = scenario.name + "-" + pass + ".png";
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, row.capture));
                yield return null;
                report.locations.Add(row); WriteReport();
                Debug.Log("PARIDHWANI_LOCATION_SAMPLE " + row.name + " pass=" + pass + " frames=" + row.frameMs.samples + " meanMs=" + row.frameMs.mean.ToString("F3", CultureInfo.InvariantCulture));
            }
        }
        static void Add(List<double> values, double value) { if (double.IsFinite(value) && value > 0) values.Add(value); }
        static string Value(double value) => double.IsFinite(value) && value > 0 ? value.ToString("F6", CultureInfo.InvariantCulture) : "";
    }

    public sealed partial class TeaStallSession
    {
        internal void PrepareLocationProfile(ReplayChapter chapter, Vector3 position, float heading, float pitch)
        {
            if (!VillageLocationProfile.Requested || !IsIsolatedTestVisit || !IsAutomatedTest) throw new InvalidOperationException("Location profiling requires an isolated diagnostic visit.");
            EndForJourney(); ResetActorsToOrigins(); Progress = ChapterReplay.Seed(chapter, Progress); Progress.quality = VillageQuality.Balanced;
            player.Restore(position, heading); player.SetLookAngles(heading, pitch); returnTrail.Reset(position); ApplyState();
            ui.HideModal(); SetCursor(false); player.blocked = true;
        }
    }
}
