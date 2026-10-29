using System;
using UnityEditor;
using UnityEngine;
namespace Paridhwani.Editor {
 [InitializeOnLoad] public static class TeaAssessmentMenu {
 static LoopbackTeaAssessmentService service;static TeaStallSession session;
 static TeaAssessmentMenu(){AssemblyReloadEvents.beforeAssemblyReload+=Detach;EditorApplication.quitting+=Detach;EditorApplication.playModeStateChanged+=s=>{if(s==PlayModeStateChange.ExitingPlayMode)Detach();};}
 [MenuItem("Paridhwani/Tea assessment/Attach local assessment (port 8766)",true)]static bool Available()=>EditorApplication.isPlaying&&!EditorApplication.isCompiling&&UnityEngine.Object.FindAnyObjectByType<TeaStallSession>()!=null;
 [MenuItem("Paridhwani/Tea assessment/Attach local assessment (port 8766)")]public static void Attach(){if(!Available())return;Detach();session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();service=new LoopbackTeaAssessmentService(true,new Uri("http://127.0.0.1:8766"+TeaAssessmentContract.Route));session.SetTeaAssessmentService(service);}
 [MenuItem("Paridhwani/Tea assessment/Detach assessment")]public static void Detach(){if(session)session.SetTeaAssessmentService(null);service?.Dispose();service=null;session=null;}
 }
}
