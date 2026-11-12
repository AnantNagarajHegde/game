#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Paridhwani.MovieAuthoring
{
    public static class ArrivalMovieValidate
    {
        const string Key="Paridhwani.MovieValidate";
        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
            EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        }
        public static void Start()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/verification/Temp/ArrivalMovie/UnityProject/"))throw new InvalidOperationException("Use the isolated movie project.");
            SessionState.SetBool("Paridhwani.MovieRender",false);
            EditorSceneManager.OpenScene(Paridhwani.Editor.TeaStallSceneBuilder.ScenePath);
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            session.generatedRecipeSignature=WorldRecipeSignature.Compute(Path.Combine(Application.dataPath,"_Paridhwani/Scripts/Editor"));
            SessionState.SetString(Key+"Started",DateTime.UtcNow.ToString("O"));
            SessionState.SetBool(TeaStallSession.IsolatedTestKey,true);SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Attached",false);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || SessionState.GetBool(Key+"Attached",false))return;
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if(!session || session.Progress==null || !session.ui || session.ui.Mode!="main-menu")return;
            SessionState.SetBool(Key+"Attached",true);session.gameObject.AddComponent<VillageArrivalIntroReview>().Begin(session);
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification/Temp/ArrivalIntro",Screen.width+"x"+Screen.height));
            string result=Path.Combine(folder,"result.txt");
            bool passed=File.Exists(result) && File.GetLastWriteTimeUtc(result)>DateTime.Parse(SessionState.GetString(Key+"Started",""),null,System.Globalization.DateTimeStyles.RoundtripKind);
            Debug.Log(passed?"ARRIVAL_MOVIE_VALIDATION_OK":"ARRIVAL_MOVIE_VALIDATION_FAILED");EditorApplication.Exit(passed?0:1);
        }
    }
}
#endif
