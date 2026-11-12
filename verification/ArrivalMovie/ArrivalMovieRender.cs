#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Paridhwani.MovieAuthoring
{
    // Copied into an isolated authoring project by Render-ArrivalMovie.ps1.
    // Never installed in the playable project or included in a Player build.
    public static class ArrivalMovieRender
    {
        const string Key="Paridhwani.MovieRender";
        [InitializeOnLoadMethod]
        static void Register(){EditorApplication.update-=Tick;EditorApplication.update+=Tick;}
        public static void Start()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/verification/Temp/ArrivalMovie/UnityProject/"))
                throw new InvalidOperationException("Movie rendering is restricted to the isolated authoring copy.");
            EditorSceneManager.OpenScene(Paridhwani.Editor.TeaStallSceneBuilder.ScenePath);
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            // The extra authoring script is part of this copy only. The playable scene is never saved.
            session.generatedRecipeSignature=WorldRecipeSignature.Compute(Path.Combine(Application.dataPath,"_Paridhwani/Scripts/Editor"));
            SessionState.SetBool(TeaStallSession.IsolatedTestKey,true);
            SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Attached",false);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false) || !EditorApplication.isPlaying || SessionState.GetBool(Key+"Attached",false))return;
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if(!session || session.Progress==null || !session.ui || session.ui.Mode!="main-menu")return;
            SessionState.SetBool(Key+"Attached",true);session.gameObject.AddComponent<ArrivalMovieCapture>().Begin(session);
        }
    }

    [DefaultExecutionOrder(400)]
    public sealed class ArrivalMovieCapture : MonoBehaviour
    {
        [Serializable] public sealed class Cue {public string id,caption;public float start,end;}
        [Serializable] public sealed class Timeline {public int fps,width,height;public float duration;public Cue[] cues;}
        Timeline story;
        TeaStallSession session;
        Camera view;
        Transform bus;
        Vector3 busPark,heroLast;
        GameObject notebook;
        RenderTexture target;
        Texture2D pixels;
        int frame,warmup=40,previousBeat=-1,proofIndex;
        bool proofs;
        string output;
        float travelled;
        List<float> proofTimes;
        System.Collections.IDictionary work;
        readonly Dictionary<string,PropertyInfo> walkerProperties=new Dictionary<string,PropertyInfo>();
        static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;

        public void Begin(TeaStallSession owner)
        {
            session=owner;
            if(!session.IsIsolatedTestVisit)throw new InvalidOperationException("Movie render cannot use the real save.");
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Frames"));Directory.CreateDirectory(output);
            story=JsonUtility.FromJson<Timeline>(File.ReadAllText(Path.Combine(Application.dataPath,"_Paridhwani/Resources/ArrivalIntro/MovieTimeline.json")));
            proofs=Environment.GetCommandLineArgs().Contains("-movie-proofs");
            proofTimes=new List<float>();foreach(var cue in story.cues){proofTimes.Add(cue.start+2);proofTimes.Add(cue.start+(cue.end-cue.start)*.72f);}
            session.ui.HideModal();session.ui.GetComponent<UIDocument>().enabled=false;
            foreach(var title in FindObjectsByType<VillageTitleBackdrop>())title.enabled=false;
            session.Progress.quality=VillageQuality.High;session.Progress.brightness=.7f;session.Progress.masterVolume=0;
            session.SettingsChanged();session.enabled=false;session.player.enabled=false;session.player.blocked=false;
            work=(System.Collections.IDictionary)typeof(TeaStallSession).GetField("vendorWork",Private).GetValue(session);
            foreach(var actor in FindObjectsByType<VillageCharacter>())actor.presentationPaused=false;
            foreach(var cull in FindObjectsByType<VillageDistanceCulling>())cull.enabled=false;
            view=session.player.view;view.enabled=false;view.aspect=(float)story.width/story.height;
            var data=view.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
            foreach(var t in FindObjectsByType<Transform>())if(t.name=="Parked rural bus"){bus=t;break;}
            if(!bus)throw new InvalidOperationException("The village bus is missing.");
            busPark=bus.position;foreach(var collider in bus.GetComponentsInChildren<Collider>())collider.enabled=false;
            target=new RenderTexture(story.width,story.height,24,RenderTextureFormat.ARGB32){antiAliasing=1};target.Create();
            pixels=new Texture2D(story.width,story.height,TextureFormat.RGB24,false);
            Time.captureFramerate=story.fps;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;Application.runInBackground=true;
            foreach(string property in new[]{"Speed","Velocity","TravelDistance"})walkerProperties[property]=typeof(VillageWalker).GetProperty(property);
            heroLast=session.player.transform.position;
            BuildNotebook();
            Debug.Log("ARRIVAL_MOVIE_CAPTURE_STARTED "+output+" duration="+story.duration);
        }
        Material Surface(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/_Paridhwani/Generated/Materials/"+name+".mat");
        GameObject Box(string name,Vector3 size,Vector3 position,Material material,Transform parent)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localScale=size;go.transform.localPosition=position;
            go.GetComponent<Renderer>().sharedMaterial=material;Destroy(go.GetComponent<Collider>());return go;
        }
        void BuildNotebook()
        {
            notebook=new GameObject("Movie-only field notebook");notebook.transform.position=new Vector3(-3.1f,1.02f,-.25f);
            Box("Writing surface",new Vector3(1.4f,.08f,.85f),new Vector3(0,-.07f,0),Surface("Wood"),notebook.transform);
            Box("Notebook cover",new Vector3(.78f,.045f,.5f),Vector3.zero,Surface("WoodLight"),notebook.transform);
            Box("Left page",new Vector3(.36f,.012f,.46f),new Vector3(-.19f,.031f,0),Surface("Cream"),notebook.transform);
            Box("Right page",new Vector3(.36f,.012f,.46f),new Vector3(.19f,.031f,0),Surface("Cream"),notebook.transform);
            for(int i=0;i<6;i++)foreach(float side in new[]{-.19f,.19f})Box("Pencil note",new Vector3(i==5?.18f:.26f,.002f,.005f),new Vector3(side,.039f,-.14f+i*.053f),Surface("StoneDark"),notebook.transform);
            var pencil=Box("Pencil",new Vector3(.013f,.013f,.36f),new Vector3(.48f,.01f,-.03f),Surface("WoodLight"),notebook.transform);pencil.transform.localRotation=Quaternion.Euler(0,-18,0);
            notebook.SetActive(false);
        }
        Vector3 Npc(VillageNpcRole role)
        {foreach(var npc in session.villagers)if(npc && npc.role==role)return npc.transform.position;return Vector3.zero;}
        void Hero(Vector3 at,float heading,bool teleport)
        {
            var walker=session.player;var controller=walker.GetComponent<CharacterController>();
            if(teleport){controller.enabled=false;walker.transform.position=at;controller.enabled=true;heroLast=at;}
            else {var delta=at-walker.transform.position;delta.y=-.08f;controller.Move(delta);}
            var travel=walker.transform.position-heroLast;travel.y=0;heroLast=walker.transform.position;
            float speed=teleport?0:Mathf.Min(2.15f,travel.magnitude*story.fps);travelled+=teleport?0:travel.magnitude;
            walkerProperties["Speed"].SetValue(walker,speed);walkerProperties["Velocity"].SetValue(walker,travel*story.fps);walkerProperties["TravelDistance"].SetValue(walker,travelled);
            walker.visual.rotation=Quaternion.Euler(0,heading,0);
        }
        void Stage(float time)
        {
            int beat=story.cues.Length-1;for(int i=0;i<story.cues.Length;i++)if(time<story.cues[i].end){beat=i;break;}
            var cue=story.cues[beat];float seconds=time-cue.start,t=Mathf.Clamp01(seconds/(cue.end-cue.start));bool cut=beat!=previousBeat;
            Vector3 position,targetPoint;float fov=50;
            var tea=Npc(VillageNpcRole.TeaVendor);var library=Npc(VillageNpcRole.Librarian);
            notebook.SetActive(beat==4);bus.gameObject.SetActive(beat<2);
            session.player.visual.gameObject.SetActive(beat!=0 || t>.55f);
            switch(beat)
            {
                case 0:
                    bus.rotation=Quaternion.Euler(0,270,0);bus.position=busPark+Vector3.right*Mathf.Lerp(15,0,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/.72f)));
                    Hero(Vector3.Lerp(new Vector3(5.3f,.2f,-6.7f),new Vector3(0,.2f,-4.8f),Mathf.Clamp01((t-.55f)/.45f)),-70,cut || t<=.55f);
                    position=new Vector3(-.5f,2.65f,-14.4f);targetPoint=busPark+new Vector3(-1,1.45f,1.3f);fov=55;break;
                case 1:
                    bus.rotation=Quaternion.Euler(0,270,0);bus.position=busPark+Vector3.left*Mathf.Lerp(0,26,Mathf.SmoothStep(0,1,t));
                    Hero(new Vector3(0,.2f,-4.8f),Mathf.Lerp(-25,45,Mathf.SmoothStep(0,1,t)),cut);
                    if(t<.54f){position=new Vector3(1.8f,1.8f,-1.4f);targetPoint=new Vector3(0,1.32f,-4.8f);fov=42;}
                    else {position=new Vector3(-2.8f,1.75f,-4.5f);targetPoint=new Vector3(0,1.25f,-4.8f);fov=49;}break;
                case 2:
                    Hero(Vector3.Lerp(new Vector3(.7f,.2f,4.5f),new Vector3(.7f,.2f,8),t),0,cut);
                    position=t<.55f?library+new Vector3(5.8f,2.7f,-5.5f):library+new Vector3(2,1.85f,-3);
                    targetPoint=library+new Vector3(0,t<.55f?1.7f:1.1f,.25f);fov=t<.55f?56:43;break;
                case 3:
                    Hero(Vector3.Lerp(new Vector3(1,.2f,9.2f),new Vector3(1,.2f,14.8f),t),0,cut);
                    position=t<.55f?new Vector3(5,2.1f,14):new Vector3(0,2.4f,18.5f);
                    targetPoint=session.player.transform.position+Vector3.up*1.1f;fov=50;break;
                case 4:
                    session.player.visual.gameObject.SetActive(false);
                    Hero(new Vector3(-2,.2f,-1),-45,cut);
                    position=notebook.transform.position+new Vector3(.15f,1.3f,-1.05f);
                    targetPoint=notebook.transform.position;fov=44;break;
                default:
                    Hero(Vector3.Lerp(new Vector3(-1.8f,.2f,-3.9f),new Vector3(-3.9f,.2f,-1.6f),Mathf.Clamp01(t/.66f)),-37,cut);
                    if(t<.48f){position=tea+new Vector3(1.45f,1.65f,-2.3f);targetPoint=tea+new Vector3(0,1.2f,0);fov=44;}
                    else{position=new Vector3(-.8f,2.1f,-4.5f);targetPoint=tea+new Vector3(-.1f,1.0f,-.7f);fov=48;}
                    if(t>.7f){var actor=session.player.GetComponentInChildren<VillageCharacter>();actor.rightArm.localRotation=Quaternion.Euler(-70,0,-18);}
                    break;
            }
            foreach(System.Collections.DictionaryEntry pair in work)pair.Value.GetType().GetMethod("Update").Invoke(pair.Value,new object[]{1f/story.fps,session.player.transform,false,false});
            foreach(var actor in FindObjectsByType<VillageCharacter>())actor.presentationPaused=false;
            view.fieldOfView=fov;view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(targetPoint-position));
            if(session.ambience && session.ambience.rain)session.ambience.rain.transform.position=position+Vector3.up*7;
            previousBeat=beat;
        }
        void LateUpdate()
        {
            if(session==null)return;
            try {
                if(warmup-->0){Stage(0);return;}
                float time=proofs?proofTimes[proofIndex]:(float)frame/story.fps;
                Stage(time);
                RenderPipeline.SubmitRenderRequest(view,new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                var previous=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=previous;
                string name=proofs?"proof-"+proofIndex.ToString("00")+".jpg":"frame-"+frame.ToString("00000")+".jpg";
                File.WriteAllBytes(Path.Combine(output,name),pixels.EncodeToJPG(94));
                if(proofs)proofIndex++;else frame++;
                if(frame%120==0)Debug.Log("ARRIVAL_MOVIE_FRAME "+frame+" t="+time);
                if(proofs?proofIndex==proofTimes.Count:frame>=Mathf.CeilToInt(story.duration*story.fps)) {
                    File.WriteAllText(Path.Combine(output,proofs?"proofs-ready.txt":"render-ready.txt"),"Native Unity scene; "+story.width+"x"+story.height+"; "+story.fps+" fps; frames="+frame+"\n");
                    Debug.Log("ARRIVAL_MOVIE_RENDER_OK");EditorApplication.Exit(0);
                }
            }catch(Exception error){File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());Debug.LogException(error);EditorApplication.Exit(1);}
        }
        void OnDestroy(){Time.captureFramerate=0;if(target){target.Release();Destroy(target);}if(pixels)Destroy(pixels);}
    }
}
#endif
