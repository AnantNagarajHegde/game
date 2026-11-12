using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Paridhwani.Editor
{
    // Runs the existing approved reviews in isolated visits, plus new street/river acceptance.
    public static class VillageExpansionReviewRunner
    {
        const string Key = "Paridhwani.ExpansionReview.";
        static readonly BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        static string Output => Path.GetFullPath(Path.Combine(Application.dataPath, "../../verification/Temp/VillageExpansion"));
        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        [MenuItem("Paridhwani/Review expanded village and graphics")]
        public static void StartSuite()
        {
            try {
            TeaStallSceneBuilder.UpgradeVillage();
            TeaStallChecks.Run();
            Directory.CreateDirectory(Output);
            SessionState.SetBool(Key + "Active", true); SessionState.SetInt(Key + "Step", 0);
            SessionState.SetBool(Key + "Started", false); SessionState.SetBool(Key + "Attached", false);
            SessionState.SetString(Key + "Since", DateTime.UtcNow.ToString("O"));
            } catch(Exception error) { Fail(error); }
        }
        static void SetGameSize(int width, int height)
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup", Flags).Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var groupClass = group.GetType(); var sizeType = assembly.GetType("UnityEditor.GameViewSize");
            int count = (int)groupClass.GetMethod("GetTotalCount", Flags).Invoke(group, null), index = -1;
            for (int i = 0; i < count; i++)
            {
                var size = groupClass.GetMethod("GetGameViewSize", Flags).Invoke(group, new object[] { i });
                if ((int)sizeType.GetProperty("width", Flags).GetValue(size) == width && (int)sizeType.GetProperty("height", Flags).GetValue(size) == height) { index = i; break; }
            }
            if (index < 0)
            {
                var kind = assembly.GetType("UnityEditor.GameViewSizeType");
                var size = Activator.CreateInstance(sizeType, Flags, null, new object[] { Enum.Parse(kind, "FixedResolution"), width, height, "Village review " + width }, null);
                groupClass.GetMethod("AddCustomSize", Flags).Invoke(group, new[] { size }); index = count;
            }
            var view = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            view.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(view, index);
            view.Show(); view.Focus();
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key + "Active", false) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            try
            {
                int step = SessionState.GetInt(Key + "Step", 0);
                if (!SessionState.GetBool(Key + "Started", false))
                {
                    SetGameSize(step == 3 ? 960 : 1920, step == 3 ? 540 : 1080);
                    TeaStallSceneBuilder.PlayIsolatedTestVisit();
                    SessionState.SetBool(Key + "Started", true); SessionState.SetBool(Key + "Attached", false);
                    return;
                }
                if (!EditorApplication.isPlaying || SessionState.GetBool(Key + "Attached", false)) return;
                var session = UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
                if (!session || !session.IsIsolatedTestVisit || session.Progress == null || !session.ui || session.ui.Mode != "main-menu") return;
                EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                SessionState.SetBool(Key + "Attached", true);
                if (step == 0) session.gameObject.AddComponent<VillageExpansionReview>().Begin(session);
                else if (step == 1) session.gameObject.AddComponent<VillagePolishReview>().Begin(session);
                else session.gameObject.AddComponent<VillageVisualReview>().Begin(session);
            }
            catch (Exception error) { Fail(error); }
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key + "Active", false)) return;
            int step = SessionState.GetInt(Key + "Step", 0);
            string root = step == 0 ? Path.Combine(Output, "Native") : Path.GetFullPath(Path.Combine(Output, step == 1 ? "../PolishReview" : "../UIReview"));
            string file = step <= 1 ? "report.txt" : "result.txt";
            var since = DateTime.Parse(SessionState.GetString(Key + "Since", ""), null, System.Globalization.DateTimeStyles.RoundtripKind);
            var found = Directory.Exists(root) ? Directory.GetFiles(root, file, SearchOption.AllDirectories).Where(p => File.GetLastWriteTimeUtc(p) >= since).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault() : null;
            if (found == null) { Fail(new InvalidOperationException("Review stage " + step + " did not produce a success report. Inspect its failure.txt and Editor log.")); return; }
            File.AppendAllText(Path.Combine(Output, "suite-report.txt"), "Stage " + step + ": " + found + "\n" + File.ReadAllText(found) + "\n");
            SessionState.SetString(Key + "Since", DateTime.UtcNow.ToString("O"));
            SessionState.SetInt(Key + "Step", ++step); SessionState.SetBool(Key + "Started", false);
            if (step == 4)
            {
                SessionState.SetBool(Key + "Active", false); Debug.Log("VILLAGE_REVIEW_SUITE_OK");
                if (Environment.GetCommandLineArgs().Contains("-village-review-exit")) EditorApplication.Exit(0);
            }
        }
        static void Fail(Exception error)
        {
            SessionState.SetBool(Key + "Active", false); Debug.LogException(error);
            Directory.CreateDirectory(Output); File.WriteAllText(Path.Combine(Output, "suite-failure.txt"), error.ToString());
            if (Environment.GetCommandLineArgs().Contains("-village-review-exit")) EditorApplication.Exit(1);
        }
        public static void BuildPlayer()
        {
            TeaStallSceneBuilder.UpgradeVillage(); // Batch mode can start with an empty scene.
            TeaStallChecks.Run(); TeaStallSceneBuilder.ValidateResidentialVillage();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { TeaStallSceneBuilder.ScenePath },
                locationPathName = "Builds/VillageExpansion/Paridhwani.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new InvalidOperationException("Village build failed: " + report.summary.result);
            Debug.Log("VILLAGE_EXPANSION_BUILD_OK");
        }
    }
}
