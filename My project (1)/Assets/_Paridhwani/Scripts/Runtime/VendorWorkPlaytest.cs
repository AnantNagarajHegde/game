#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Paridhwani
{
    // Opt-in native regression in an isolated visit. Never opens the player's save.
    public sealed class VendorWorkPlaytest : MonoBehaviour
    {
        const string Key="Paridhwani.VendorWorkPlaytest";
        [Serializable] sealed class Report
        {
            public string state="running",failure="",startedUtc,completedUtc;
            public int checks,width,height;
            public List<string> captures=new List<string>();
        }
        TeaStallSession session;Report report;string output;bool finished,background;
        [MenuItem("Paridhwani/Run Vendor Work Playtest",true)]
        static bool Available()=>!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !BuildPipeline.isBuildingPlayer;
        [MenuItem("Paridhwani/Run Vendor Work Playtest")]
        static void Run()
        {
            if(!Available())return;
            SessionState.SetBool(Key,true);SessionState.SetBool(TeaStallSession.AutomatedTestKey,false);
            try {if(!EditorApplication.ExecuteMenuItem("Paridhwani/Play Isolated Test Visit") || !EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Isolated visit did not start");}
            catch {SessionState.SetBool(Key,false);throw;}
        }
        [InitializeOnLoadMethod] static void Register()
        {EditorApplication.playModeStateChanged-=PlayState;EditorApplication.playModeStateChanged+=PlayState;}
        static void PlayState(PlayModeStateChange change)
        {
            if(change==PlayModeStateChange.EnteredEditMode)SessionState.SetBool(Key,false);
            if(change!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key,false))return;
            SessionState.SetBool(Key,false);new GameObject("Vendor work verification").AddComponent<VendorWorkPlaytest>().Begin();
        }
        void Begin()
        {
            background=Application.runInBackground;Application.runInBackground=true;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","verification","Temp","VendorNative",DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff")));
            Directory.CreateDirectory(output);report=new Report {startedUtc=DateTime.UtcNow.ToString("O"),width=Screen.width,height=Screen.height};
            Write();StartCoroutine(Guarded());
        }
        void Write()=>File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(report,true));
        void Check(bool ok,string why) {if(!ok)throw new Exception(why);report.checks++;}
        void Finish(string failure)
        {
            if(finished)return;finished=true;report.state=failure==null?"passed":"failed";report.failure=failure??"";report.completedUtc=DateTime.UtcNow.ToString("O");Write();
            if(failure==null)Debug.Log("VENDOR_NATIVE_OK "+output);else Debug.LogError("VENDOR_NATIVE_FAILED "+output+"\n"+failure);
            EditorApplication.isPlaying=false;
        }
        void OnDestroy(){Application.runInBackground=background;if(!finished && report!=null)Finish("Play stopped or sources reloaded");}
        IEnumerator Guarded()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Checks());float until=Time.realtimeSinceStartup+240;
            while(stack.Count>0)
            {
                bool next;object current=null;
                try {if(Time.realtimeSinceStartup>until)throw new Exception("Native vendor test timed out");next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception e){Finish(e.ToString());yield break;}
                if(!next){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
            Finish(null);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();string path=Path.Combine(output,name+".png");ScreenCapture.CaptureScreenshot(path);
            float until=Time.realtimeSinceStartup+5;while((!File.Exists(path)||new FileInfo(path).Length<5000)&&Time.realtimeSinceStartup<until)yield return null;
            Check(File.Exists(path)&&new FileInfo(path).Length>=5000,"Capture missing: "+name);report.captures.Add(name+".png");Write();
        }
        void Place(Vector3 position,float yaw,float pitch=17)
        {session.player.Restore(position,yaw);session.player.SetLookAngles(yaw,pitch);session.player.blocked=true;}
        IEnumerator Observe(VillageNpc npc,Vector3 view,float heading,string prefix)
        {
            Place(view,heading);yield return new WaitForSecondsRealtime(1);
            var visual=npc.GetComponentInChildren<VillageCharacter>();var routine=visual.workRoutine;
            Check(routine!=null,"Vendor has no routine: "+prefix);
            var origin=npc.transform.position;float traveled=0;var seen=new HashSet<VendorWorkTask>();
            int workEntries=0;var previous=VendorWorkTask.Rest;float until=Time.realtimeSinceStartup+65;
            var body=npc.GetComponent<CapsuleCollider>();
            while(Time.realtimeSinceStartup<until)
            {
                var before=npc.transform.position;yield return null;traveled+=Vector3.Distance(before,npc.transform.position);seen.Add(routine.Task);
                if(routine.Task!=previous && routine.Task!=VendorWorkTask.Rest && routine.Task!=VendorWorkTask.Walk)
                {
                    workEntries++;yield return new WaitForSecondsRealtime(1.2f);yield return Capture(prefix+"-"+routine.Task+"-"+workEntries);
                }
                previous=routine.Task;
                Check((npc.transform.position-origin).sqrMagnitude<2.5f,"Vendor left local work patch");
                // Skip the initial escape from the pre-existing tea counter overlap.
                if(routine.Z>.349f || npc.role==VillageNpcRole.MarketVendor)
                {
                    foreach(var other in Physics.OverlapCapsule(npc.transform.position+Vector3.up*.3f,npc.transform.position+Vector3.up*1.5f,.265f,~0,QueryTriggerInteraction.Ignore))
                        if(other && !other.transform.IsChildOf(npc.transform) && Physics.ComputePenetration(body,npc.transform.position,npc.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth))
                            Check(depth<.02f,"Vendor penetrates "+other.name+" by "+depth);
                }
                if(workEntries>=4)break;
            }
            Check(workEntries>=4&&traveled>1.5f,"Vendor did not repeat two occupational cycles; distance="+traveled+", entries="+workEntries+", task="+routine.Task+", offset="+routine.X+","+routine.Z);
            Check(seen.Contains(VendorWorkTask.Walk)&&seen.Contains(VendorWorkTask.Rest),"Missing walk/rest phases");
            // Freeze during a real conversation, including position and work clock.
            session.StartConversation(npc);yield return null;var held=npc.transform.position;float clock=routine.ActionTime;
            yield return new WaitForSecondsRealtime(1.5f);Check(Vector3.Distance(held,npc.transform.position)<.001f&&routine.ActionTime==clock&&visual.talking,"Conversation did not suspend work");
            yield return Capture(prefix+"-conversation");session.OpenNotebook();yield return new WaitForSecondsRealtime(.5f);
            Check(Vector3.Distance(held,npc.transform.position)<.001f,"Notebook displaced conversing actor");session.Close();session.Close();
            yield return new WaitForSecondsRealtime(2);Check(routine.ActionTime!=clock||Vector3.Distance(held,npc.transform.position)>.01f,"Work failed to resume after conversation");
            session.ui.ShowPause();held=npc.transform.position;clock=routine.ActionTime;yield return new WaitForSecondsRealtime(.5f);
            Check(Vector3.Distance(held,npc.transform.position)<.001f&&routine.ActionTime==clock,"Pause did not freeze work");session.Close();
            // Let the normal interaction scan and camera return run after leaving the modal.
            session.player.SetLookAngles(heading,17);session.player.blocked=true;
            yield return new WaitForSecondsRealtime(.8f);
        }
        IEnumerator Checks()
        {
            yield return null;session=FindAnyObjectByType<TeaStallSession>();Check(session&&session.IsIsolatedTestVisit,"Requires isolated visit");
            session.Progress.brightness=.9f;session.Progress.textScale=1.3f;session.SettingsChanged();
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Arrival);session.Close();
            var tea=Array.Find(session.villagers,n=>n&&n.role==VillageNpcRole.TeaVendor);var market=Array.Find(session.villagers,n=>n&&n.role==VillageNpcRole.MarketVendor);
            yield return Observe(tea,new Vector3(-4.2f,.2f,-1.65f),0,"Tea");
            Check(session.Nearby=="Talk to Manjunatha","Established approach lost tea interaction");
            // Actual walkable apron/drain view, without a detached/editor camera.
            Place(new Vector3(-.5f,.2f,-2.2f),-65,46);yield return new WaitForSecondsRealtime(.6f);yield return Capture("Tea-ground-drain-closeup");
            yield return Observe(market,new Vector3(8.15f,.2f,8.85f),0,"Market");
            session.Save();session.Load();yield return null;Check(session.LastSaveSucceeded,"Isolated save/load failed");
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Council);session.Close();yield return null;
            var staged=tea.transform.position;yield return new WaitForSecondsRealtime(2);
            Check(Vector3.Distance(staged,tea.transform.position)<.001f&&!tea.GetComponentInChildren<VillageCharacter>().workRoutine.PresentationEnabled,"Routine displaced finale staging");
            session.ReturnToCompletedStory();yield return null;Check(!session.IsPracticeVisit,"Demo exit failed to restore isolated main visit");
        }
    }
}
#endif
