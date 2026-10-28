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
                    foreach(string line in File.ReadAllLines(active.playerLog))
                        if(line.StartsWith("VILLAGE_SMOKE_OK ",StringComparison.Ordinal)) active.smokeSuccessLine=line;
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
