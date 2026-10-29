using System;
using UnityEditor;
using UnityEngine;

namespace Paridhwani.Editor
{
    // Explicit temporary attachment; never saves a component, endpoint or setting into a scene.
    [InitializeOnLoad]
    public static class LocalDialogueMockMenu
    {
        const string Root="Paridhwani/NLP mock/";
        static LoopbackDialogueTextService service;
        static TeaStallSession session;
        static Func<DialoguePrompt,System.Threading.CancellationToken,System.Threading.Tasks.Task<TeaLine>> installed;
        static LocalDialogueMockMenu()
        {
            AssemblyReloadEvents.beforeAssemblyReload+=Detach;
            EditorApplication.quitting+=Detach;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingPlayMode)Detach();};
        }
        [MenuItem(Root+"Attach loopback service (port 8765)",true)]
        static bool CanAttach()=>EditorApplication.isPlaying && !EditorApplication.isCompiling &&
            UnityEngine.Object.FindAnyObjectByType<TeaStallSession>()!=null;
        [MenuItem(Root+"Attach loopback service (port 8765)")]
        public static void Attach()
        {
            if(!CanAttach())return;
            Detach();session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            service=new LoopbackDialogueTextService(true,new Uri("http://127.0.0.1:8765"+DialogueHttpContract.Route));
            session.SetOptionalDialogueService(service);
            installed=session.OptionalDialogueText;
            Debug.Log("NLP_MOCK_ATTACHED v1 loopback port 8765; authored replies still control story progression.");
        }
        [MenuItem(Root+"Detach and use authored dialogue")]
        public static void Detach()
        {
            if(session && ReferenceEquals(session.OptionalDialogueText,installed))session.SetOptionalDialogueService(null);
            service?.Dispose();service=null;session=null;installed=null;
        }
    }
}
