#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Editor-only, opt-in presentation regression. Never opens the player's main save.
    public sealed class PresentationDemoPlaytest : MonoBehaviour
    {
        const string Menu = "Paridhwani/Run Presentation Demo Playtest";
        const string PendingKey = "Paridhwani.PresentationDemoPlaytest";
        [Serializable]
        sealed class Report
        {
            public string state = "running", startedUtc, completedUtc, unityVersion, sourceSha256, failure;
            public int checks, width, height;
            public List<string> captures = new List<string>();
        }
        Report report;
        TeaStallSession session;
        string output;
        float deadline;
        bool finished, originalBackground;
        int originalWidth, originalHeight;
        FullScreenMode originalMode;

        [MenuItem(Menu, true)]
        static bool Available() => !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;

        [MenuItem(Menu)]
        public static void RunFromMenu()
        {
            if (!Available()) { Debug.LogWarning("Wait for the current playtest, build or compilation to finish."); return; }
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(TeaStallSession.AutomatedTestKey, false);
            try
            {
                if (!EditorApplication.ExecuteMenuItem("Paridhwani/Play Isolated Test Visit") || !EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("The isolated test visit did not start. Open the current benchmark scene and run its checks first.");
            }
            catch { SessionState.SetBool(PendingKey, false); throw; }
        }

        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.playModeStateChanged -= PlayState;
            EditorApplication.playModeStateChanged += PlayState;
        }
        static void PlayState(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(PendingKey, false);
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            new GameObject("Presentation demo verification").AddComponent<PresentationDemoPlaytest>().Begin();
        }

        void Begin()
        {
            originalBackground = Application.runInBackground;
            originalWidth = Screen.width; originalHeight = Screen.height; originalMode = Screen.fullScreenMode;
            Application.runInBackground = true;
            deadline = Time.realtimeSinceStartup + 120;
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "verification", "Temp", "PresentationPlaytests",
                "run-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
            Directory.CreateDirectory(output);
            report = new Report { startedUtc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            try { report.sourceSha256 = SourceDigest(); WriteReport(); StartCoroutine(Guarded()); }
            catch (Exception error) { Complete(error.ToString()); }
        }
        // Includes content and paths, so changed sources cannot be mistaken for a tested revision.
        static string SourceDigest()
        {
            string root = Path.Combine(Application.dataPath, "_Paridhwani");
            var paths = new List<string>();
            foreach (var path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase) ||
                    path.EndsWith(".mat", StringComparison.OrdinalIgnoreCase)) paths.Add(path);
            paths.Sort(StringComparer.Ordinal);
            using (var sha = SHA256.Create())
            {
                foreach (var path in paths)
                {
                    var bytes = Encoding.UTF8.GetBytes(path.Substring(root.Length).Replace('\\', '/') + "\n" + File.ReadAllText(path).Replace("\r\n", "\n") + "\n");
                    sha.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
                sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
            }
        }
        void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException("Presentation demo playtest: " + message);
            report.checks++;
        }
        void WriteReport() => File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
        void Complete(string failure)
        {
            if (finished) return;
            finished = true;
            if (report != null)
            {
                report.failure = failure ?? ""; report.state = failure == null ? "passed" : "failed";
                report.completedUtc = DateTime.UtcNow.ToString("O"); WriteReport();
                if (failure == null) Debug.Log("PARIDHWANI_PRESENTATION_PLAYTEST_OK " + output);
                else Debug.LogError("PARIDHWANI_PRESENTATION_PLAYTEST_FAILED " + output + "\n" + failure);
            }
            EditorApplication.isPlaying = false;
        }
        void Update() { if (!finished && report != null && Time.realtimeSinceStartup > deadline) Complete("Presentation checks exceeded 120 seconds."); }
        void OnDestroy()
        {
            Application.runInBackground = originalBackground;
            Screen.SetResolution(originalWidth, originalHeight, originalMode);
            if (!finished && report != null) Complete("Play mode stopped or scripts reloaded before presentation verification completed.");
        }
        IEnumerator Guarded()
        {
            var stack = new Stack<IEnumerator>(); stack.Push(Checks());
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
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(output, name + ".png"); ScreenCapture.CaptureScreenshot(path);
            float until = Time.realtimeSinceStartup + 5;
            do { yield return null; } while ((!File.Exists(path) || new FileInfo(path).Length < 5000) && Time.realtimeSinceStartup < until);
            Check(File.Exists(path) && new FileInfo(path).Length >= 5000, "missing rendered capture: " + name);
            report.captures.Add(name + ".png"); WriteReport();
        }
        VisualElement Root => session.ui.GetComponent<UIDocument>().rootVisualElement;
        void Visible(VisualElement element, string name)
        {
            Check(element != null && float.IsFinite(element.worldBound.width) && element.worldBound.width > 0 && element.worldBound.height > 0 &&
                element.worldBound.xMin >= Root.worldBound.xMin - 2 && element.worldBound.xMax <= Root.worldBound.xMax + 2 &&
                element.worldBound.yMin >= Root.worldBound.yMin - 2 && element.worldBound.yMax <= Root.worldBound.yMax + 2, name + " stays inside the viewport");
        }
        IEnumerator Checks()
        {
            yield return null;
            session = FindAnyObjectByType<TeaStallSession>();
            Check(session && session.IsIsolatedTestVisit && !session.IsAutomatedTest && session.Progress != null,
                "requires an initialized isolated visit with no full-story smoke running");
            Check(session.ui.Mode == "main-menu", "current benchmark opens at the title");
            yield return new WaitForSecondsRealtime(.5f);
            yield return VillagePresentationSmoke.CheckSigns();
            session.Progress.brightness = .9f; session.Progress.signRead = true; session.Progress.textScale = 1.3f;
            session.SettingsChanged(); Check(session.LastSaveSucceeded, "isolated main snapshot saves");
            Vector3 mainPosition = session.Progress.position; float mainHeading = session.Progress.heading;
            // The Editor controls Game-view dimensions; record actual dimensions rather than claiming a requested resolution.
            report.width = Screen.width; report.height = Screen.height;
            yield return Capture("00-title");
            foreach (var chapter in PresentationDemo.Chapters)
            {
                session.ui.ShowPresentationDemo(); session.StartPresentationDemo(chapter);
                yield return new WaitForSecondsRealtime(.3f);
                Check(session.IsPresentationDemo && session.PresentationChapter == chapter && session.LastSaveSucceeded, "prepared separate demo: " + chapter);
                Check(!session.Progress.storyCompleted && !session.Progress.libraryProtected, "demo entry does not grant an ending");
                Check(Root.Query<Label>().ToList().Exists(label => label.text == PresentationDemo.Route(chapter)), "literal scene instructions: " + chapter);
                Visible(Root.Q<Button>("demo-enter"), "demo Enter");
                Visible(Root.Q<ScrollView>("demo-arrival-scroll"), "demo instructions scroll area");
                Check((Root.focusController.focusedElement as VisualElement)?.name == "demo-enter", "demo Enter initially focused");
                yield return Capture(chapter + "-guide");
                session.Close(); yield return new WaitForSecondsRealtime(.3f);
                Check(!session.ui.IsModal && !session.player.blocked, "demo entry releases movement");
                yield return Capture(chapter + "-world");
                session.ui.ShowPause();
                Check(Root.Q<Button>("settings-demo-guide") != null && Root.Q<Button>("settings-demo-scenes") != null, "Pause offers guide and scene switching");
                session.ui.ShowPresentationArrival(); yield return new WaitForSecondsRealtime(.2f);
                Check(session.ui.Mode == "practice-arrival", "guide reopens from Pause");
                session.Close(); session.ui.ShowPause(); session.ui.ShowPresentationDemo(); session.Close();
                Check(session.ui.Mode == "pause", "closing selector returns to Pause");
                session.ReturnToCompletedStory(); yield return null;
                Check(!session.IsPracticeVisit && !session.IsPresentationDemo && session.Progress.signRead && !session.Progress.greeted &&
                    !session.Progress.riverReportKnown && !session.Progress.deedRetrieved && !session.Progress.fridayFinaleStarted,
                    "main evidence and chapter gates restored: " + chapter);
                Check(Vector3.Distance(session.Progress.position, mainPosition) < .1f && Mathf.Abs(Mathf.DeltaAngle(session.Progress.heading, mainHeading)) < .1f,
                    "main saved position and heading restored");
                Check(session.Progress.textScale == 1.3f && session.Progress.brightness == .9f, "main presentation preferences restored");
                session.OpenTitle();
            }
            Check(SourceDigest() == report.sourceSha256, "source and scene assets remained unchanged throughout the run");
        }
    }
}
#endif
