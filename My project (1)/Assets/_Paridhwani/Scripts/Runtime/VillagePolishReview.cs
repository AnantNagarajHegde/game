#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Paridhwani
{
    [DefaultExecutionOrder(200)]
    public sealed class VillagePolishReview : MonoBehaviour
    {
        TeaStallSession session;Gamepad pad;string output;int checks,steps;bool background;
        readonly List<InputDevice> devices=new List<InputDevice>();
        readonly List<string> observations=new List<string>();
        VillageWalker Player=>session.player;
        bool sideView;
        void LateUpdate()
        {
            if(!sideView || !session)return;
            Player.view.transform.position=Player.transform.position+new Vector3(2.8f,1.4f,1.4f);
            Player.view.transform.LookAt(Player.transform.position+Vector3.up*.95f);
        }
        public void Begin(TeaStallSession value)
        {
            if(!value.IsIsolatedTestVisit)throw new InvalidOperationException("Polish review needs an isolated save.");
            session=value;background=Application.runInBackground;Application.runInBackground=true;
            foreach(var device in InputSystem.devices)if(device.enabled && (device is Keyboard || device is Mouse || device is Gamepad))devices.Add(device);
            foreach(var device in devices)InputSystem.DisableDevice(device);
            pad=InputSystem.AddDevice<Gamepad>();
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification/Temp/PolishReview",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            Directory.CreateDirectory(output);Player.GetComponent<VillageFootsteps>().Stepped+=Step;
            StartCoroutine(Guarded());
        }
        void Step(bool jog){steps++;}
        void OnDestroy()
        {
            if(session && Player)Player.GetComponent<VillageFootsteps>().Stepped-=Step;
            if(pad!=null && pad.added)InputSystem.RemoveDevice(pad);
            foreach(var device in devices)if(device.added && !device.enabled)InputSystem.EnableDevice(device);
            Application.runInBackground=background;Time.timeScale=1;
        }
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
        IEnumerator Guarded()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more;object current=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);UnityEditor.EditorApplication.isPlaying=false;yield break;}
                if(!more){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        IEnumerator Drive(Vector2 stick,float duration,bool jog=false)
        {
            float start=Time.time;
            while(Time.time-start<duration)
            {InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=stick,rightTrigger=jog?1:0});yield return null;}
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try{File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());}finally{Destroy(texture);}
        }
        IEnumerator Run()
        {
            session.BeginNewMainVisit();session.AcknowledgeArrival();
            Player.Restore(new Vector3(1,.2f,-5),0);yield return Drive(Vector2.zero,.4f);
            Check(Player.Grounded,"Player did not settle on the lane");
            // Equal elapsed time at different frame rates must produce the same motor response.
            foreach(int rate in new[]{20,30,60,120})
            {
                Vector3 v=Vector3.zero;for(int i=0;i<rate;i++)v=VillageMotion.Step(v,Vector3.forward*2.15f,1f/rate);
                Check(Mathf.Abs(v.magnitude-2.15f)<.001f,"Frame-dependent walking speed at "+rate);
                for(int i=0;i<rate/2;i++)v=VillageMotion.Step(v,Vector3.zero,1f/rate);
                Check(v.sqrMagnitude<.00001f,"Motor fails to stop at "+rate);
                v=Vector3.forward*2.15f;for(int i=0;i<rate/2;i++)v=VillageMotion.Step(v,Vector3.back*2.15f,1f/rate);
                Check(Vector3.Distance(v,Vector3.back*2.15f)<.001f,"Reversal fails at "+rate);
            }
            Vector3 before=Player.transform.position;
            yield return Drive(Vector2.up,.10f);Check(Player.Speed>.1f && Player.Speed<1.8f,"Walking start must accelerate progressively: "+Player.Speed);
            yield return Drive(Vector2.up,.85f);Check(Player.Speed>2.05f && Player.Speed<2.2f,"Walking never reaches its target speed: "+Player.Speed);
            var rig=Player.visual.GetComponent<VillageCharacter>();Check(rig.Gait!=null,"Articulated gait missing");
            Check(Player.visual.GetComponentsInChildren<SkinnedMeshRenderer>().Length==2,"Expected two weighted leg meshes");
            Check(rig.Gait.Blend>.9f,"Walking animation failed to blend in");
            Check(steps>=1,"Distance-based gait did not trigger footsteps");
            yield return Shot("01-walking");
            Vector3 release=Player.transform.position;yield return Drive(Vector2.zero,.28f);
            float stopping=Vector3.Distance(release,Player.transform.position);
            Check(Player.Speed<.02f && stopping<.31f,"Braking drift is too long: "+stopping);
            observations.Add("Walk braking distance: "+stopping.ToString("F3")+" m (includes one queued input frame).");
            yield return Drive(Vector2.up,.75f,true);Check(Player.Speed>3.45f,"Jog target speed not reached");
            yield return Shot("02-jogging");
            sideView=true;yield return Drive(Vector2.up,.14f,true);yield return Shot("02b-jog-side");sideView=false;
            yield return Drive(Vector2.down,.8f);Check(Player.Velocity.z< -2,"Reversal stuck or failed to face travel");
            Check(Quaternion.Angle(Quaternion.LookRotation(Player.Velocity),Quaternion.Euler(0,Player.visual.eulerAngles.y,0))<8,"Body did not turn with travel: "+Player.visual.eulerAngles.y);
            session.OpenNotebook();yield return Drive(Vector2.up,.15f);
            var blocked=Player.transform.position;yield return Drive(Vector2.up,.3f);
            Check(Vector3.Distance(blocked,Player.transform.position)<.005f && Player.Speed==0,"Modal allowed movement");session.Close();
            yield return Drive(Vector2.zero,.2f);
            Player.Restore(new Vector3(-7,.2f,2),90);yield return Drive(Vector2.zero,.3f);
            // A wall approach must stop both actual travel and gait; don't bank velocity.
            Player.SetLookAngles(-90,17);yield return Drive(Vector2.up,1.2f);
            Check(Player.Speed<.08f,"Walking into a wall keeps the gait moving: "+Player.Speed);
            yield return Drive(Vector2.down,.3f);Check(Player.Speed>1,"Could not move away from collision");
            yield return Drive(Vector2.zero,.25f);
            var architecture=FindAnyObjectByType<VillageArchitecture>();
            Check(architecture && architecture.RoofCount>=10 && architecture.WallCount>=12 && architecture.JoineryCount>=5,"Architectural coverage incomplete");
            Check(architecture.DetailVertices<120000,"Architectural detail exceeded the geometry budget");
            observations.Add("Architecture: "+architecture.RoofCount+" roofs, "+architecture.WallCount+" walls, "+architecture.JoineryCount+" doors/windows, "+architecture.DetailVertices+" detail vertices.");
            foreach(var filter in FindObjectsByType<MeshFilter>())
            {
                if(filter.name!="Timber fascia and rafters" && filter.name!="Bonded stone foundation" && filter.name!="Rebated frame and joinery" && filter.name!="Overlapping ridge tiles" && filter.name!="Dressed window sill")continue;
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
                for(int i=0;i<triangles.Length;i+=3)
                    Check(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).sqrMagnitude>1e-15f,"Degenerate detail triangle: "+filter.name);
                Check(!filter.GetComponent<Collider>(),"Visual detail unexpectedly blocks a route");
                Check(filter.GetComponent<Renderer>().sharedMaterial.shader.isSupported,"Unsupported building shader");
            }
            Player.Restore(new Vector3(0,.2f,-4.8f),-32);yield return Drive(Vector2.zero,.5f);yield return Shot("03-tea-stall");
            Player.Restore(new Vector3(-4,.3f,8.9f),0);yield return Drive(Vector2.zero,.5f);yield return Shot("04-library");
            Player.Restore(new Vector3(2,.2f,3),58);yield return Drive(Vector2.zero,.5f);yield return Shot("05-house");
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Deed);session.Close();
            session.Progress.trunkOpened=true;session.OpenActivity(VillageActivity.DeedReview);
            yield return Drive(Vector2.zero,.3f);
            var root=session.ui.GetComponent<UIDocument>().rootVisualElement;
            var answers=root.Query<Button>(className:"evidence-answer").ToList();
            Check(answers.Count==3,"Document answer controls missing");answers[0].Focus();yield return Drive(Vector2.zero,.2f);
            InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadDown));yield return new WaitForSecondsRealtime(.12f);
            yield return Drive(Vector2.zero,.12f);
            Check(ReferenceEquals(root.focusController.focusedElement,answers[1]),"Document D-pad focus index="+answers.FindIndex(b=>ReferenceEquals(b,root.focusController.focusedElement)));
            session.Close();session.ReturnToCompletedStory();
            Player.Restore(new Vector3(0,.2f,-4.8f),-32);session.ui.ShowMainMenu();
            File.WriteAllText(Path.Combine(output,"report.txt"),"POLISH_REVIEW_OK: "+checks+" checks\n"+string.Join("\n",observations)+"\nReal Unity CharacterController / Gamepad input / skinning / shader and geometry checks.\n");
            Debug.Log("POLISH_REVIEW_OK: "+checks+" checks; "+output);UnityEditor.EditorApplication.isPlaying=false;
        }
    }
}
#endif
