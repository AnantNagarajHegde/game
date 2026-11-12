using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Paridhwani.Editor
{
    public static class ArrivalIntroReviewRunner
    {
        const string Key="Paridhwani.ArrivalIntroReview.";
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification/Temp/ArrivalIntro"));
        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        }
        [MenuItem("Paridhwani/Review arrival introduction")]
        public static void Start()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play mode before the intro review.");
            if(SceneManager.GetActiveScene().path!=TeaStallSceneBuilder.ScenePath) {
                if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                EditorSceneManager.OpenScene(TeaStallSceneBuilder.ScenePath);
            }
            Directory.CreateDirectory(Output);SessionState.SetBool(Key+"Active",true);SessionState.SetInt(Key+"Step",0);
            SessionState.SetBool(Key+"Started",false);SessionState.SetString(Key+"Since",DateTime.UtcNow.ToString("O"));
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key+"Active",false) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try {
                int step=SessionState.GetInt(Key+"Step",0);
                if(!SessionState.GetBool(Key+"Started",false)) {
                    int width=step==1?960:1920,height=width*9/16;
                    typeof(VillageExpansionReviewRunner).GetMethod("SetGameSize",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{width,height});
                    TeaStallSceneBuilder.PlayIsolatedTestVisit();SessionState.SetBool(Key+"Started",true);SessionState.SetBool(Key+"Attached",false);return;
                }
                if(!EditorApplication.isPlaying || SessionState.GetBool(Key+"Attached",false))return;
                var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
                if(!session || session.Progress==null || !session.IsIsolatedTestVisit || !session.ui || session.ui.Mode!="main-menu")return;
                EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
                SessionState.SetBool(Key+"Attached",true);
                if(step==0)session.gameObject.AddComponent<VillageArrivalIntroReview>().Begin(session);
                else session.gameObject.AddComponent<VillageVisualReview>().Begin(session);
            } catch(Exception error){Fail(error);}
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key+"Active",false))return;
            int step=SessionState.GetInt(Key+"Step",0);
            string root=step==0?Output:Path.GetFullPath(Path.Combine(Output,"../UIReview"));
            var since=DateTime.Parse(SessionState.GetString(Key+"Since",""),null,System.Globalization.DateTimeStyles.RoundtripKind);
            var report=Directory.Exists(root)?Directory.GetFiles(root,"result.txt",SearchOption.AllDirectories).Where(p=>File.GetLastWriteTimeUtc(p)>=since).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault():null;
            if(report==null){Fail(new InvalidOperationException("Review stage "+step+" failed; inspect failure.txt and Editor log."));return;}
            File.AppendAllText(Path.Combine(Output,"suite-report.txt"),"Stage "+step+": "+report+"\n"+File.ReadAllText(report)+"\n");
            SessionState.SetString(Key+"Since",DateTime.UtcNow.ToString("O"));SessionState.SetInt(Key+"Step",++step);SessionState.SetBool(Key+"Started",false);
            if(step==3) {
                SessionState.SetBool(Key+"Active",false);Debug.Log("ARRIVAL_INTRO_SUITE_OK");
                if(Environment.GetCommandLineArgs().Contains("-arrival-review-exit"))EditorApplication.Exit(0);
            }
        }
        static void Fail(Exception error)
        {
            SessionState.SetBool(Key+"Active",false);Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"suite-failure.txt"),error.ToString());Debug.LogException(error);
            if(Environment.GetCommandLineArgs().Contains("-arrival-review-exit"))EditorApplication.Exit(1);
        }
    }
}
