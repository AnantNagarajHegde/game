using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug=UnityEngine.Debug;

namespace Paridhwani.Editor
{
    // Runs a freshly built player from the already licensed Editor, without a second Editor process.
    public static class StandalonePlayerPlaytest
    {
        const string Menu="Paridhwani/Build and Run Standalone Playtest";
        const double TimeoutSeconds=600;
        static Process player;
        static Stopwatch timer;
        static RunReport active;
        static string reportPath;
        static double teaPreviewDeadline;
        static double visualReviewDeadline;
        static double polishReviewDeadline;
        [MenuItem("Paridhwani/Review movement and architecture",true)]
        static bool CanReviewPolish()=>Available();
        [MenuItem("Paridhwani/Review movement and architecture")]
        public static void ReviewPolish()
        {
            if(!Available())return;
            TeaStallSceneBuilder.PlayIsolatedTestVisit();polishReviewDeadline=EditorApplication.timeSinceStartup+45;
            EditorApplication.update-=BeginPolishReview;EditorApplication.update+=BeginPolishReview;
        }
        static void BeginPolishReview()
        {
            if(EditorApplication.timeSinceStartup>polishReviewDeadline){EditorApplication.update-=BeginPolishReview;Debug.LogError("Polish review could not enter an isolated visit.");return;}
            if(!EditorApplication.isPlaying)return;
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if(!session || !session.IsIsolatedTestVisit || session.Progress==null || !session.ui || session.ui.Mode!="main-menu")return;
            EditorApplication.update-=BeginPolishReview;session.gameObject.AddComponent<VillagePolishReview>().Begin(session);
        }
        [MenuItem("Paridhwani/Review UI and graphics",true)]
        static bool CanReviewVisuals()=>Available();
        [MenuItem("Paridhwani/Review UI and graphics")]
        public static void ReviewVisuals()
        {
            if(!Available())return;
            TeaStallSceneBuilder.PlayIsolatedTestVisit();
            visualReviewDeadline=EditorApplication.timeSinceStartup+45;
            EditorApplication.update-=BeginVisualReview;
            EditorApplication.update+=BeginVisualReview;
        }
        static void BeginVisualReview()
        {
            if(EditorApplication.timeSinceStartup>visualReviewDeadline){EditorApplication.update-=BeginVisualReview;Debug.LogError("Visual review could not enter an isolated visit.");return;}
            if(!EditorApplication.isPlaying)return;
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if(!session || !session.IsIsolatedTestVisit || session.Progress==null || !session.ui || session.ui.Mode!="main-menu")return;
            EditorApplication.update-=BeginVisualReview;
            session.gameObject.AddComponent<VillageVisualReview>().Begin(session);
        }
        [MenuItem("Paridhwani/Preview isolated tea practice",true)]
        static bool CanPreviewTea()=>Available();
        [MenuItem("Paridhwani/Preview isolated tea practice")]
        public static void PreviewIsolatedTeaPractice()
        {
            if(!Available())return;
            TeaStallSceneBuilder.PlayIsolatedTestVisit();
            teaPreviewDeadline=EditorApplication.timeSinceStartup+30;
            EditorApplication.update-=PrepareTeaPreview;
            EditorApplication.update+=PrepareTeaPreview;
        }
        static void PrepareTeaPreview()
        {
            if(EditorApplication.timeSinceStartup>teaPreviewDeadline){EditorApplication.update-=PrepareTeaPreview;Debug.LogError("Isolated tea preview did not become ready.");return;}
            if(!EditorApplication.isPlaying)return;
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            var ui=UnityEngine.Object.FindAnyObjectByType<TeaStallUI>();
            if(!session||!session.IsIsolatedTestVisit||session.Progress==null||!ui||ui.Mode!="main-menu")return;
            VillageNpc vendor=null;
            foreach(var npc in UnityEngine.Object.FindObjectsByType<VillageNpc>())
                if(npc.displayName=="Manjunatha"){if(vendor){EditorApplication.update-=PrepareTeaPreview;Debug.LogError("Tea preview needs a unique Manjunatha.");return;}vendor=npc;}
            if(!vendor)return;
            EditorApplication.update-=PrepareTeaPreview;
            session.StartConversation(vendor);session.Choose(0);
            Debug.Log("TEA_PRACTICE_PREVIEW_READY isolated save; no service request sent.");
        }

        // Build the already-authored scene without invoking world/art generation.
        [MenuItem("Paridhwani/Build current tea assessment snapshot",true)]
        static bool CanBuildSnapshot()=>Available();
        [MenuItem("Paridhwani/Build current tea assessment snapshot")]
        public static void BuildCurrentTeaAssessmentSnapshot()
        {
            if(!Available())return;
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if(scene.path!=TeaStallSceneBuilder.ScenePath || scene.isDirty || !session ||
               session.generatedWorldRevision!=TeaStallSession.RequiredWorldRevision ||
               session.generatedRecipeSignature!=WorldRecipeSignature.Compute(Path.Combine(Application.dataPath,"_Paridhwani","Scripts","Editor")))
                throw new InvalidOperationException("Open the saved current benchmark scene before building; no scene or art is regenerated by this command.");
            TeaStallChecks.Run();
            string output="Builds/TeaAssessment-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"/Paridhwani.exe";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scene.path},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Assessment snapshot build failed: "+report.summary.result);
            Debug.Log("TEA_ASSESSMENT_SNAPSHOT_BUILT "+Path.GetFullPath(output));
        }

        [Serializable]
        sealed class RunReport
        {
            public string startedUtc,completedUtc,unityVersion,state,buildCompletedUtc,buildResult,playerPath,playerLog,smokeSuccessLine,failure;
            public int playerExitCode=-1;
            public bool smokePassed;
        }

        [MenuItem(Menu,true)]
        static bool Available() => active==null && !EditorApplication.isPlayingOrWillChangePlaymode &&
            !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;

        [MenuItem(Menu)]
        public static void BuildAndRun()
        {
            if(!Available()) {Debug.LogWarning("Wait for the current build/playtest or script compilation to finish first.");return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string project=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            string output=Path.GetFullPath(Path.Combine(project,"..","verification","Temp","StandalonePlaytests",
                "run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")+"-"+Guid.NewGuid().ToString("N").Substring(0,8)));
            Directory.CreateDirectory(output);
            reportPath=Path.Combine(output,"report.json");
            active=new RunReport {
                startedUtc=DateTime.UtcNow.ToString("O"),unityVersion=Application.unityVersion,state="building",
                playerPath=Path.Combine(project,"Builds","TeaStall","Paridhwani.exe"),playerLog=Path.Combine(output,"player.log")
            };
            try
            {
                SaveReport();
                // Failure throws before launch: an old executable can never be used as a fallback.
                TeaStallSceneBuilder.ValidateAndBuild();
                active.buildResult="Succeeded";active.buildCompletedUtc=DateTime.UtcNow.ToString("O");
                if(!File.Exists(active.playerPath)) throw new FileNotFoundException("The successful build did not produce its player.",active.playerPath);
                player=Process.Start(new ProcessStartInfo {
                    FileName=active.playerPath,WorkingDirectory=Path.GetDirectoryName(active.playerPath),
                    Arguments="-village-smoke -screen-fullscreen 0 -screen-width 960 -screen-height 540 -logFile \""+active.playerLog+"\"",
                    UseShellExecute=false,WindowStyle=ProcessWindowStyle.Normal
                });
                if(player==null) throw new InvalidOperationException("Could not start the freshly built player.");
                timer=Stopwatch.StartNew();active.state="running";SaveReport();
                EditorApplication.update+=Poll;
                AssemblyReloadEvents.beforeAssemblyReload+=Interrupted;
                EditorApplication.quitting+=Interrupted;
                Debug.Log("PARIDHWANI_PLAYER_TEST_STARTED "+reportPath);
            }
            catch(Exception error) {Finish(error.ToString());}
        }

        static void Poll()
        {
            try
            {
                if(player.HasExited) {Finish(null);return;}
                if(timer.Elapsed.TotalSeconds>=TimeoutSeconds) Finish("The started player exceeded its 600-second test timeout.");
            }
            catch(Exception error) {Finish(error.ToString());}
        }

        static void Interrupted() {Finish("The Editor closed or reloaded scripts before standalone verification finished.");}
        static void SaveReport() {File.WriteAllText(reportPath,JsonUtility.ToJson(active,true));}
        static void Finish(string failure)
        {
            if(active==null) return;
            EditorApplication.update-=Poll;
            AssemblyReloadEvents.beforeAssemblyReload-=Interrupted;
            EditorApplication.quitting-=Interrupted;
            try
            {
                if(player!=null)
                {
                    // Only this test's returned process handle is owned; never discover/stop other players.
                    if(!player.HasExited) player.Kill();
                    else active.playerExitCode=player.ExitCode;
                }
                if(failure==null && File.Exists(active.playerLog))
                    // Unity's logging handle may still be closing after the process signals exit.
                    using(var stream=new FileStream(active.playerLog,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))
                    using(var reader=new StreamReader(stream))
                    {
                        string line;while((line=reader.ReadLine())!=null)
                            if(line.StartsWith("VILLAGE_SMOKE_OK ",StringComparison.Ordinal)) active.smokeSuccessLine=line;
                    }
                active.smokePassed=failure==null && active.buildResult=="Succeeded" && active.playerExitCode==0 && !string.IsNullOrEmpty(active.smokeSuccessLine);
                active.failure=failure??(active.smokePassed?"":"Player verification failed; inspect player.log for the failure or missing completion marker.");
                active.state=active.smokePassed?"passed":"failed";active.completedUtc=DateTime.UtcNow.ToString("O");
                SaveReport();
                if(active.smokePassed) Debug.Log("PARIDHWANI_STANDALONE_PLAYTEST_OK "+reportPath);
                else Debug.LogError("PARIDHWANI_STANDALONE_PLAYTEST_FAILED "+reportPath+"\n"+active.failure);
            }
            catch(Exception error) {Debug.LogError("Could not finalize standalone test report at "+reportPath+"\n"+error);}
            finally {player?.Dispose();player=null;timer=null;active=null;}
        }
    }
}
