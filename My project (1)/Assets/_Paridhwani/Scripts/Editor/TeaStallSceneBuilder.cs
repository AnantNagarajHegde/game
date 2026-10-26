using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        public const string Root="Assets/_Paridhwani";
        public const string ScenePath=Root+"/Scenes/TeaStall_Benchmark.unity";
        static Transform environment;
        static readonly Dictionary<string,Material> mats=new Dictionary<string,Material>();
        static System.Random random;
        static float R(float min,float max) => Mathf.Lerp(min,max,(float)random.NextDouble());
        static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        static Color Hex(string c) { ColorUtility.TryParseHtmlString("#"+c,out var value); return value; }

        [MenuItem("Paridhwani/Create Tea Stall Benchmark")]
        public static void Create()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Generate();
            Debug.Log("Tea-stall benchmark created. Open "+ScenePath+" and press Play.");
        }

        public static void Generate()
        {
            if(!Application.isBatchMode)
            {
                for(int index=0;index<SceneManager.sceneCount;index++) {
                    var active=SceneManager.GetSceneAt(index);if(!active.IsValid() || !active.isDirty)continue;
                    string backup=Root+"/Scenes/ReviewBackups";Directory.CreateDirectory(backup);AssetDatabase.Refresh();
                    string name="BeforeRegeneration_"+index+"_"+DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff")+".unity";
                    if(!EditorSceneManager.SaveScene(active,backup+"/"+name,true))throw new IOException("Could not preserve an edited scene before regeneration.");
                }
            }
            foreach(string path in new[]{"Scenes","Generated","Generated/Materials","Generated/Meshes","Generated/UI"})
                Directory.CreateDirectory(Root+"/"+path);
            AssetDatabase.Refresh();
            random=new System.Random(1976); mats.Clear();
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            scene.name="TeaStall_Benchmark";
            environment=new GameObject("Environment").transform;
            Materials();
            Ground();
            Stall();
            Surroundings();
            var vendor=Character("Manjunatha",V(-4.3f,.28f,1.3f),false);
            vendor.transform.rotation=Quaternion.Euler(0,165,0);
            var player=Character("Player",V(2.1f,.2f,-5.7f),true);
            var controller=player.AddComponent<CharacterController>();
            controller.height=1.76f; controller.center=V(0,.9f,0); controller.radius=.28f;
            controller.stepOffset=.32f; controller.slopeLimit=48; controller.skinWidth=.04f;
            var walker=player.AddComponent<VillageWalker>();
            walker.visual=player.transform.GetChild(0);
            walker.actions=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            player.GetComponentInChildren<VillageCharacter>().walker=walker;
            vendor.GetComponentInChildren<VillageCharacter>().lookTarget=player.transform;
            var camera=new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag="MainCamera"; camera.fieldOfView=53; camera.nearClipPlane=.08f; camera.farClipPlane=220;
            camera.backgroundColor=Hex("89948D"); camera.clearFlags=CameraClearFlags.Skybox;
            camera.AdditionalCameraData().renderPostProcessing=true;
            camera.gameObject.AddComponent<AudioListener>(); walker.view=camera;
            camera.transform.position=V(5,3,-9); camera.transform.LookAt(V(-2,1.5f,2));

            var systems=new GameObject("Tea Stall Session");
            var session=systems.AddComponent<TeaStallSession>();
            session.player=walker; session.vendor=vendor.transform;
            session.sign=new GameObject("Sign inspection anchor").transform; session.sign.position=V(-2.1f,.1f,-.8f);
            session.libraryLane=new GameObject("Library lane anchor").transform; session.libraryLane.position=V(1.5f,.1f,14);
            session.offeredTea=Drink("Your tea",V(-3.8f,1.14f,.11f),true);
            session.offeredWater=Drink("Your water",V(-3.8f,1.14f,.11f),false);
            session.offeredTea.SetActive(false); session.offeredWater.SetActive(false);
            var ambient=systems.AddComponent<TeaAmbience>(); ambient.listener=player.transform;
            ambient.rain=Rain(); session.ambience=ambient;
            var doc=systems.AddComponent<UIDocument>();
            var panel=ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode=PanelScaleMode.ScaleWithScreenSize; panel.referenceResolution=new Vector2Int(1440,900);
            panel.screenMatchMode=PanelScreenMatchMode.MatchWidthOrHeight; panel.match=.5f;
            // Default runtime controls plus our own complete visual stylesheet.
            File.WriteAllText(Root+"/Generated/UI/VillageTheme.tss","@import url(\"unity-theme://default\");");
            AssetDatabase.ImportAsset(Root+"/Generated/UI/VillageTheme.tss");
            panel.themeStyleSheet=AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(Root+"/Generated/UI/VillageTheme.tss");
            doc.panelSettings=Save(panel,Root+"/Generated/UI/TeaPanel.asset");
            var ui=systems.AddComponent<TeaStallUI>();
            ui.stylesheet=AssetDatabase.LoadAssetAtPath<StyleSheet>(Root+"/UI/TeaStall.uss");
            ui.latinFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/NotoSans-Regular.ttf");
            ui.kannadaFont=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/NotoSansKannada-Regular.ttf");
            session.ui=ui;
            var events=new GameObject("UI Input",typeof(EventSystem),typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            ExpandVillage(session);
            AddMarketChapter(session);
            AddRiverChapter(session);
            AddTempleChapter(session);
            AddArchiveChapter(session);
            AddPanchayatChapter(session);
            AddCheckpostChapter(session);
            AddRidgeChapter(session);
            AddDeedChapter(session);
            AddReturnChapter(session);
            AddFinaleChapter(session);
            AddEpilogueChapter(session);
            ConfigureBenchmarkRendering();
            Lighting();
            EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceSynchronousImport);
            // Resolve the imported scene GUID before registering it in Build Settings.
            var benchmarkEntry=new EditorBuildSettingsScene(ScenePath,true);
            benchmarkEntry.guid=new GUID(AssetDatabase.AssetPathToGUID(ScenePath));
            var buildScenes=new List<EditorBuildSettingsScene>{benchmarkEntry};
            foreach(var entry in EditorBuildSettings.scenes) if(entry.path!=ScenePath) buildScenes.Add(entry);
            EditorBuildSettings.scenes=buildScenes.ToArray();
            foreach(var material in mats.Values) EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }

        static void Materials()
        {
            Mat("Earth","65503E",.94f,.7f,30); Mat("Lane","74624B",.95f,.5f,60);
            Mat("Clay","834B37",.97f,.06f,32); Mat("ClayLight","9B6146",.96f,.07f,35);
            Mat("Plaster","BAB398",.92f,.2f,40); Mat("Damp","686C54",.96f,.45f,25);
            Mat("Wood","43372B",.92f,.2f,55); Mat("WoodLight","76654B",.9f,.2f,35);
            Mat("Stone","777B69",.92f,.5f,45); Mat("StoneDark","4F594E",.8f,.7f,50);
            Mat("Leaf","354832",.9f,.15f,20).SetFloat("_Cull",0); Mat("LeafLight","596348",.9f,.1f,25).SetFloat("_Cull",0);
            Mat("Trunk","69634C",.97f,.3f,18); Mat("Cream","E1D7B6",.85f,0,30);
            Mat("Shirt","B9AB8A",.95f,0,60); Mat("Trousers","343F37",.98f,0,40);
            Mat("Skin","986D4B",.92f,0,50); Mat("Hair","272A23",.98f,0,40);
            Mat("SkinLight","B08059",.95f,0,45); Mat("Green","4D5F49",.93f,0,35);
            Mat("Sign","253E37",.85f,.15f,20); Mat("Tea","75482D",.4f,0,25);
            Mat("Tyre","282D29",.98f,.3f,35); Mat("Bus","8A4438",.85f,.1f,25);
            var steel=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            steel.SetColor("_BaseColor",Hex("9BACA3")); steel.SetFloat("_Metallic",.78f); steel.SetFloat("_Smoothness",.7f);
            mats["Steel"]=Save(steel,Root+"/Generated/Materials/Steel.mat");
            var water=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            water.SetColor("_BaseColor",Hex("697568")); water.SetFloat("_Smoothness",.96f); water.SetFloat("_Metallic",.35f);
            mats["Water"]=Save(water,Root+"/Generated/Materials/Water.mat");
            var brass=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            brass.SetColor("_BaseColor",Hex("AC8A4A")); brass.SetFloat("_Metallic",.76f); brass.SetFloat("_Smoothness",.55f);
            mats["Brass"]=Save(brass,Root+"/Generated/Materials/Brass.mat");
            var black=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            black.color=Hex("172B27"); mats["Glass"]=Save(black,Root+"/Generated/Materials/Glass.mat");
            var lamp=new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lamp.color=Hex("E0C18B"); lamp.EnableKeyword("_EMISSION"); lamp.SetColor("_EmissionColor",Hex("F2BF70")*2.2f);
            mats["Lamp"]=Save(lamp,Root+"/Generated/Materials/Lamp.mat");
        }
        static Material Mat(string name,string hex,float rough,float wet,float grain)
        {
            var m=new Material(Shader.Find("Paridhwani/Village Surface"));
            m.SetColor("_BaseColor",Hex(hex)); m.SetFloat("_Roughness",rough); m.SetFloat("_Wet",wet); m.SetFloat("_Grain",grain);
            ConfigureSurfaceTexture(m,name);
            mats[name]=Save(m,Root+"/Generated/Materials/"+name+".mat"); return mats[name];
        }
        static T Save<T>(T asset,string path) where T:UnityEngine.Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing) { EditorUtility.CopySerialized(asset,existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(asset); return existing; }
            AssetDatabase.CreateAsset(asset,path); return asset;
        }
        static GameObject Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,string material,bool collision=true,Transform parent=null)
        {
            var o=GameObject.CreatePrimitive(type); o.name=name; o.transform.SetParent(parent?parent:environment,false);
            o.transform.localPosition=position; o.transform.localScale=scale; o.GetComponent<Renderer>().sharedMaterial=mats[material];
            if(!collision) UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            return o;
        }
        static GameObject Box(string n,Vector3 p,Vector3 s,string m,bool collision=true,Transform parent=null) =>
            Shape(n,PrimitiveType.Cube,p,s,m,collision,parent);
        static GameObject Ellipsoid(string n,Vector3 p,Vector3 s,string m,Transform parent=null) =>
            Shape(n,PrimitiveType.Sphere,p,s,m,false,parent);
        static GameObject Cylinder(string n,Vector3 p,float radius,float height,string m,bool collision=false,Transform parent=null) =>
            Shape(n,PrimitiveType.Cylinder,p,V(radius*2,height*.5f,radius*2),m,collision,parent);
        static GameObject Beam(string n,Vector3 a,Vector3 b,float radius,string material,Transform parent=null)
        {
            var o=Cylinder(n,(a+b)*.5f,radius,Vector3.Distance(a,b),material,false,parent);
            o.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a); return o;
        }

        static void Ground()
        {
            Box("Red earth clearing",V(0,-.2f,4),V(46,.4f,55),"Earth");
            Box("Compacted village lane",V(1.6f,.012f,3),V(5.8f,.03f,36),"Lane");
            for(int i=0;i<100;i++)
            {
                float x=R(-10,13),z=R(-12,19);
                if(x<-.5f && x>-8 && z>-1 && z<5) continue;
                Ellipsoid("Laterite gravel",V(x,.025f,z),V(R(.04f,.15f),R(.025f,.065f),R(.05f,.2f)),"StoneDark");
            }
            for(int i=0;i<18;i++)
            {
                float x=R(-.5f,3.8f),z=R(-10,17);
                Ellipsoid("Shallow rain pool",V(x,.019f,z),V(R(.3f,1.1f),.012f,R(.5f,1.8f)),"Water");
            }
            // Open roadside drain beside the stall; stone crossings remain walkable.
            Box("Drain channel",V(-1.18f,.025f,3),V(.4f,.07f,31),"StoneDark");
            Box("Drain water",V(-1.18f,.065f,3),V(.24f,.015f,31),"Water",false);
            for(int i=0;i<36;i++)
            {
                float z=-12+i*.86f;
                Box("Drain stone",V(-1.45f,.08f,z),V(.15f,.18f,.78f),"Stone");
                Box("Drain stone",V(-.92f,.08f,z),V(.15f,.18f,.78f),"Stone");
            }
            Box("Stall drain crossing",V(-1.2f,.17f,-.8f),V(.88f,.12f,1.3f),"Stone");
            // Invisible outer boundaries stop wandering beyond the authored benchmark.
            foreach(var v in new[]{V(13,2,3)})
            { var b=Box("Benchmark boundary",v,V(.2f,5,32),"Earth"); b.GetComponent<Renderer>().enabled=false; }
            foreach(float z in new[]{-12.5f,33f})
            { var b=Box("Benchmark boundary",V(0,2,z),V(27,5,.2f),"Earth"); b.GetComponent<Renderer>().enabled=false; }
        }

        static void Stall()
        {
            Box("Raised stone plinth",V(-4.6f,.12f,2),V(6.8f,.26f,5.1f),"Stone");
            for(int x=0;x<10;x++) for(int z=0;z<7;z++)
                Box("Sheltered floor slab",V(-7.6f+x*.66f,.26f,-.2f+z*.66f),V(.645f,.035f,.645f),"StoneDark");
            Box("Old limewashed back wall",V(-4.6f,1.6f,4.45f),V(6.8f,2.9f,.22f),"Plaster");
            Box("Wall damp base",V(-4.6f,.49f,4.30f),V(6.8f,.43f,.06f),"Damp",false);
            Box("Side wall",V(-7.9f,1.5f,2),V(.2f,2.65f,5),"Plaster");
            Box("Side damp base",V(-7.77f,.5f,2),V(.04f,.42f,5),"Damp",false);
            Box("Kitchen door",V(-6.6f,1.31f,4.29f),V(1.05f,2.1f,.1f),"Wood");
            for(int i=0;i<6;i++) Box("Door plank",V(-7.04f+i*.18f,1.31f,4.22f),V(.015f,2.05f,.025f),"WoodLight",false);
            Ellipsoid("Door latch",V(-6.22f,1.35f,4.13f),V(.07f,.07f,.04f),"Brass");
            Box("Back shelf",V(-4.1f,1.69f,4.05f),V(2.6f,.09f,.56f),"Wood");
            Box("Shelf bracket",V(-5.1f,1.45f,4.13f),V(.07f,.45f,.15f),"Wood");
            for(int i=0;i<6;i++) Cylinder("Kitchen tin",V(-5.1f+i*.4f,1.9f,4.0f),.12f,.33f,i%2==0?"Steel":"Cream");
            foreach(float x in new[]{-7.6f,-1.65f})
            {
                Box("Timber veranda post",V(x,1.62f,-.38f),V(.17f,2.95f,.17f),"Wood");
                Box("Post stone shoe",V(x,.37f,-.38f),V(.27f,.2f,.27f),"Stone");
                Beam("Diagonal roof brace",V(x,2.25f,-.38f),V(x+(x<-5?.7f:-.7f),2.96f,-.38f),.065f,"Wood");
            }
            Box("Front eave beam",V(-4.6f,2.96f,-.39f),V(6.4f,.18f,.2f),"Wood");
            Box("Roof ridge beam",V(-4.6f,3.83f,2),V(7.1f,.17f,.2f),"Wood");
            Roof("Stall clay tiled roof",-4.6f,2,3.92f,7.4f,6.1f);
            // A thin solid roof catches rain particles and camera casts.
            Box("Roof rain barrier",V(-4.6f,3.07f,2),V(7.4f,.08f,6.1f),"Wood",true).GetComponent<Renderer>().enabled=false;
            Beam("Eave gutter",V(-8.2f,2.85f,-1.05f),V(-.95f,2.85f,-1.05f),.055f,"Steel");
            Beam("Rain downpipe",V(-1.56f,2.85f,-1.05f),V(-1.56f,.2f,-1.05f),.045f,"Steel");
            Box("Serving counter base",V(-4.2f,.65f,.95f),V(3.8f,.75f,.66f),"Wood");
            Box("Stone counter",V(-4.2f,1.08f,.94f),V(4,.12f,.86f),"StoneDark");
            for(int i=0;i<7;i++) Box("Counter panel",V(-5.9f+i*.53f,.68f,.59f),V(.017f,.65f,.03f),"WoodLight",false);
            // Boiler with handles, lid, tap and a small warming light.
            Cylinder("Tea boiler",V(-5.5f,1.58f,.94f),.29f,.85f,"Steel",true);
            Cylinder("Boiler lid",V(-5.5f,2.03f,.94f),.33f,.055f,"Steel");
            Ellipsoid("Boiler lid grip",V(-5.5f,2.10f,.94f),V(.13f,.08f,.1f),"Wood");
            Beam("Boiler tap",V(-5.5f,1.27f,.7f),V(-5.5f,1.27f,.39f),.035f,"Brass");
            Box("Tap lever",V(-5.5f,1.36f,.46f),V(.12f,.025f,.04f),"Wood",false);
            for(int i=0;i<5;i++) Cylinder("Steel tumbler",V(-3.5f+i*.18f,1.23f,.9f),.06f,.19f,"Steel");
            Cylinder("Water vessel",V(-2.65f,1.4f,1.05f),.19f,.52f,"Steel");
            Cylinder("Water vessel lid",V(-2.65f,1.67f,1.05f),.205f,.04f,"Steel");
            Box("Cup tray",V(-4.1f,1.17f,.65f),V(.75f,.025f,.3f),"Steel",false);
            Bench(V(-6.7f,.3f,2.6f));
            Box("Tea stall nameboard",V(-4.6f,2.65f,-.48f),V(4.25f,.5f,.07f),"Sign",false);
            Text("ಮಂಜುನಾಥ ಚಹಾ ಅಂಗಡಿ",V(-4.6f,2.65f,-.53f),.105f,true,180);
            Text("MANJUNATHA  ·  TEA & WATER",V(-4.6f,2.43f,-.54f),.036f,false,180);
            Box("Menu board",V(-1.85f,1.25f,-.48f),V(.65f,1.03f,.06f),"Sign",false);
            Text("ಚಹಾ",V(-1.85f,1.55f,-.53f),.08f,true,180);
            Text("TEA",V(-1.85f,1.33f,-.53f),.045f,false,180);
            Text("ನೀರು",V(-1.85f,1.12f,-.53f),.08f,true,180);
            Text("WATER",V(-1.85f,.91f,-.53f),.04f,false,180);
            Beam("Lamp cable",V(-3.9f,3.06f,.4f),V(-3.9f,2.52f,.4f),.01f,"Tyre");
            Cylinder("Warm shelter lamp",V(-3.9f,2.5f,.4f),.15f,.08f,"Lamp");
            var bulb=new GameObject("Warm pool of light").AddComponent<Light>(); bulb.type=LightType.Point;
            bulb.color=Hex("FFD6A0"); bulb.intensity=.7f; bulb.range=4; bulb.transform.position=V(-4,2.4f,1.2f);
            // Wire and clock make the stall a place of work rather than a set.
            Cylinder("Wall clock",V(-3.1f,2.35f,4.23f),.26f,.055f,"Wood").transform.rotation=Quaternion.Euler(90,0,0);
            Text("09 : 00",V(-3.1f,2.35f,4.18f),.055f,false,180);
        }
        static void Bench(Vector3 p)
        {
            Box("Veranda bench",p+V(0,.44f,0),V(1.65f,.12f,.48f),"Wood");
            foreach(float x in new[]{-.64f,.64f}) Box("Bench leg",p+V(x,.19f,0),V(.12f,.42f,.36f),"Wood");
        }
        static GameObject Drink(string name,Vector3 p,bool tea)
        {
            var g=new GameObject(name); g.transform.position=p;
            Cylinder("Steel cup",V(0,.09f,0),.065f,.18f,"Steel",false,g.transform);
            Cylinder("Drink surface",V(0,.181f,0),.057f,.005f,tea?"Tea":"Water",false,g.transform);
            return g;
        }
        static void Text(string value,Vector3 p,float size,bool kannada,float yaw)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Quad); o.name="Shaped sign: "+value;
            o.transform.SetParent(environment); o.transform.position=p;
            o.transform.rotation=Quaternion.identity;
            float width=Mathf.Clamp(value.Length*size*1.5f,.4f,4.1f);
            o.transform.localScale=new Vector3(width,size*5f,1);
            UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>());
            o.GetComponent<Renderer>().sharedMaterial=mats["Sign"];
            var sign=o.AddComponent<VillageSign>(); sign.text=value; sign.kannada=kannada;
            sign.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Art/Fonts/"+(kannada?"NotoSansKannada-Regular.ttf":"NotoSans-Regular.ttf"));
        }

        static void Roof(string name,float cx,float cz,float peak,float width,float depth)
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            int cols=Mathf.CeilToInt(width/.23f),rows=Mathf.CeilToInt(depth/.5f);
            for(int side=0;side<2;side++) for(int row=0;row<rows/2;row++) for(int col=0;col<cols;col++)
            {
                float x=cx-width*.5f+col*width/cols;
                float z=cz+(side==0?-1:1)*row*.5f;
                int start=vertices.Count;
                for(int edge=0;edge<2;edge++) for(int arc=0;arc<=6;arc++)
                {
                    float a=arc/6f*Mathf.PI;
                    float zz=z+(side==0?-1:1)*edge*.56f;
                    float y=peak-Mathf.Abs(zz-cz)*.31f+Mathf.Sin(a)*.055f+R(-.006f,.006f);
                    vertices.Add(V(x+arc/6f*.245f,y,zz));
                }
                for(int arc=0;arc<6;arc++)
                {
                    int a=start+arc,b=a+1,c=a+7,d=a+8;
                    if(side==0) triangles.AddRange(new[]{a,b,c,b,d,c});
                    else triangles.AddRange(new[]{a,c,b,b,c,d});
                }
            }
            // Separate underside vertices preserve proper normals and a solid view from shelter.
            int upperCount=vertices.Count,upperIndices=triangles.Count;
            for(int i=0;i<upperCount;i++) vertices.Add(vertices[i]-Vector3.up*.025f);
            for(int i=0;i<upperIndices;i+=3)
                triangles.AddRange(new[]{triangles[i]+upperCount,triangles[i+2]+upperCount,triangles[i+1]+upperCount});
            var mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(environment);
            go.GetComponent<MeshFilter>().sharedMesh=Save(mesh,Root+"/Generated/Meshes/"+name.Replace(" ","_")+".asset");
            go.GetComponent<Renderer>().sharedMaterial=mats["Clay"];
            Beam("Ridge cap",V(cx-width*.5f,peak+.03f,cz),V(cx+width*.5f,peak+.03f,cz),.1f,"ClayLight");
        }

        static void Surroundings()
        {
            House("Opposite house",8,5.3f,5,5,4.8f);
            House("Distant workshop",8,16.2f,4.7f,4.5f,4.6f);
            House("Library silhouette",-4.8f,16,7,5.3f,5);
            Text("GRANTHALAYA  /  LIBRARY",V(-4.7f,2.5f,13.2f),.075f,false,0);
            for(int i=0;i<20;i++)
            {
                float z=-7+i*1.3f;
                if((z>-.8f && z<5) || (z>6f && z<12.2f)) continue;
                Box("Compound wall",V(5.4f,.63f,z),V(.26f,1.23f,1.28f),"Plaster");
                Box("Wall cap",V(5.4f,1.3f,z),V(.38f,.13f,1.28f),"Stone");
                Box("Damp wall foot",V(5.23f,.25f,z),V(.035f,.36f,1.28f),"Damp",false);
            }
            for(int i=0;i<20;i++) Palm(V(i%2==0?R(-14,-10):R(11,17),0,R(-9,28)),R(7,12),i);
            for(int i=0;i<12;i++) Ellipsoid("Distant canopy",V(R(-32,32),R(4,7),R(23,42)),V(R(7,15),R(4,9),R(6,13)),i%2==0?"Leaf":"LeafLight");
            for(int i=0;i<5;i++) Ellipsoid("Misty foothill",V(-65+i*31,0,60+i*7),V(55,R(12,24),40),"Leaf");
            for(int i=0;i<45;i++)
            {
                float x=i%2==0?R(-10,-8):R(6,10),z=R(-9,17);
                Ellipsoid("Roadside planting",V(x,.15f,z),V(R(.15f,.45f),R(.2f,.6f),R(.2f,.55f)),i%3==0?"LeafLight":"Leaf");
            }
            foreach(float z in new[]{-8,11})
            {
                Cylinder("Utility pole",V(4.7f,4.2f,z),.12f,8.4f,"Wood",true);
                Box("Utility crossarm",V(4.7f,7.6f,z),V(1.6f,.12f,.14f),"Wood");
            }
            for(int wire=0;wire<3;wire++) for(int segment=0;segment<16;segment++)
            {
                float a=segment/16f,b=(segment+1)/16f;
                Beam("Overhead wire",V(4.15f+wire*.55f,7.65f-Mathf.Sin(a*Mathf.PI)*.8f,-8+19*a),
                    V(4.15f+wire*.55f,7.65f-Mathf.Sin(b*Mathf.PI)*.8f,-8+19*b),.014f,"Tyre");
            }
            Bench(V(3.8f,.1f,-5.6f));
            Box("Bus stop notice",V(4.8f,1.7f,-7),V(.12f,1.5f,1.5f),"Sign");
            // Partial parked rural bus, a background landmark rather than a drivable vehicle.
            var bus=new GameObject("Parked rural bus").transform; bus.SetParent(environment);
            bus.position=V(7.5f,.1f,-8.9f); bus.rotation=Quaternion.Euler(0,90,0);
            Box("Bus body",V(0,1.6f,0),V(2.4f,2.7f,7.2f),"Bus",true,bus);
            Box("Bus upper cream strip",V(0,2.5f,0),V(2.42f,.23f,7.22f),"Cream",false,bus);
            for(int side=-1;side<=1;side+=2) for(int i=0;i<6;i++)
            {
                Box("Bus window",V(side*1.21f,2, -2.85f+i*1.1f),V(.035f,.65f,.94f),"Glass",false,bus);
                Box("Window rail",V(side*1.24f,1.9f,-2.85f+i*1.1f),V(.035f,.035f,.97f),"Steel",false,bus);
            }
            foreach(float side in new[]{-1.15f,1.15f}) foreach(float z in new[]{-2.2f,2.2f})
            {
                var tyre=Cylinder("Bus tyre",V(side,.5f,z),.48f,.23f,"Tyre",false,bus); tyre.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            Box("Library lane marker",V(2.6f,1.1f,12.2f),V(1.1f,.45f,.09f),"Sign",false);
            Text("LIBRARY  ←",V(2.6f,1.1f,12.14f),.047f,false,0);
            Cylinder("Lane marker post",V(2.6f,.48f,12.2f),.045f,1,"Wood");
        }
        static void House(string name,float x,float z,float width,float depth,float peak)
        {
            Box(name+" stone base",V(x,.17f,z),V(width+.3f,.35f,depth+.3f),"Stone");
            Box(name+" plaster walls",V(x,1.7f,z),V(width,3.1f,depth),"Plaster");
            Box(name+" damp lower wall",V(x,.48f,z-depth*.5f-.02f),V(width,.5f,.05f),"Damp",false);
            Box(name+" door",V(x-.7f,1.28f,z-depth*.5f-.055f),V(1.05f,2.25f,.08f),"Wood");
            Box(name+" window",V(x+1.1f,1.9f,z-depth*.5f-.06f),V(1,.85f,.08f),"Wood");
            for(int i=0;i<6;i++) Box("Window slat",V(x+.68f+i*.17f,1.9f,z-depth*.5f-.115f),V(.07f,.78f,.035f),"WoodLight",false);
            Roof(name+" roof",x,z,peak,width+.7f,depth+1);
        }
        static void Palm(Vector3 p,float height,int id)
        {
            Beam("Areca palm trunk",p,p+V(.12f,height,0),.085f,"Trunk");
            var vertices=new List<Vector3>(); var tris=new List<int>();
            for(int frond=0;frond<9;frond++)
            {
                float a=frond*Mathf.PI*2/9+R(-.15f,.15f); Vector3 dir=V(Mathf.Cos(a),0,Mathf.Sin(a));
                Vector3 side=Vector3.Cross(dir,Vector3.up);
                Vector3 tip=p+V(.12f,height,0);
                int start=vertices.Count;
                for(int segment=0;segment<=12;segment++)
                {
                    float t=segment/12f;
                    Vector3 center=tip+dir*t*3+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.6f-t*t*.95f);
                    float w=Mathf.Sin(t*Mathf.PI)*.3f+.005f;
                    vertices.Add(center-side*w); vertices.Add(center+side*w);
                }
                for(int segment=0;segment<12;segment++)
                {
                    int v0=start+segment*2,v1=v0+1,v2=v0+2,v3=v0+3;
                    tris.AddRange(new[]{v0,v2,v1,v1,v2,v3});
                }
            }
            var mesh=new Mesh{name="Palm fronds"}; mesh.SetVertices(vertices); mesh.SetTriangles(tris,0); mesh.RecalculateNormals();
            var go=new GameObject("Areca crown",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(environment);
            go.GetComponent<MeshFilter>().sharedMesh=Save(mesh,Root+"/Generated/Meshes/Palm_"+id+".asset");
            go.GetComponent<Renderer>().sharedMaterial=mats[id%2==0?"Leaf":"LeafLight"];
        }
        static GameObject Character(string name,Vector3 position,bool player,bool female=false)
        {
            var root=new GameObject(name); root.transform.position=position;
            var model=new GameObject("Character visual").transform; model.SetParent(root.transform,false);
            var rig=model.gameObject.AddComponent<VillageCharacter>();
            var hips=Ellipsoid("Trousers waist",V(0,.86f,0),V(.4f,.3f,.26f),"Trousers",model);
            Ellipsoid("Work shirt",V(0,1.2f,0),V(.5f,.65f,.31f),player?"Green":female?"ClayLight":"Shirt",model);
            Cylinder("Neck",V(0,1.53f,0),.065f,.13f,"Skin",false,model);
            var head=new GameObject("Head").transform; head.SetParent(model,false); head.localPosition=V(0,1.66f,0);
            rig.head=head;
            Ellipsoid("Face",V(0,0,0),V(.235f,.3f,.245f),"Skin",head);
            Ellipsoid("Hair",V(0,.095f,-.026f),V(.246f,.16f,.23f),"Hair",head);
            Ellipsoid("Nose",V(0,-.012f,.122f),V(.048f,.066f,.052f),"SkinLight",head);
            foreach(float side in new[]{-1,1})
            {
                Ellipsoid("Ear",V(side*.119f,-.012f,0),V(.043f,.078f,.045f),"Skin",head);
                Ellipsoid("Eye",V(side*.047f,.026f,.109f),V(.025f,.013f,.016f),"Hair",head);
                Box("Brow",V(side*.046f,.046f,.11f),V(.04f,.009f,.012f),"Hair",false,head);
            }
            Box("Mouth",V(0,-.078f,.107f),V(.053f,.009f,.012f),"Wood",false,head);
            if(!player && !female) Ellipsoid("Moustache",V(0,-.052f,.121f),V(.085f,.026f,.027f),"Hair",head);
            for(int side=-1;side<=1;side+=2)
            {
                var arm=new GameObject("Arm pivot").transform; arm.SetParent(model,false); arm.localPosition=V(side*.24f,1.43f,0);
                var sleeve=Shape("Shirt sleeve",PrimitiveType.Capsule,V(0,-.10f,0),V(.16f,.145f,.17f),player?"Green":"Shirt",false,arm);
                Shape("Forearm",PrimitiveType.Capsule,V(0,-.36f,.025f),V(.11f,.29f,.12f),"Skin",false,arm);
                Ellipsoid("Hand",V(0,-.53f,.04f),V(.11f,.13f,.08f),"Skin",arm);
                var leg=new GameObject("Leg pivot").transform; leg.SetParent(model,false); leg.localPosition=V(side*.115f,.83f,0);
                Shape("Trouser leg",PrimitiveType.Capsule,V(0,-.33f,0),V(.17f,.66f,.19f),"Trousers",false,leg);
                Ellipsoid("Shoe",V(0,-.75f,.055f),V(.17f,.115f,.3f),"Hair",leg);
                if(side<0) {rig.leftArm=arm;rig.leftLeg=leg;} else {rig.rightArm=arm;rig.rightLeg=leg;}
            }
            if(female) Ellipsoid("Hair bun",V(0,.03f,-.14f),V(.18f,.17f,.15f),"Hair",head);
            if(!player) Box("Shoulder towel",V(-.14f,1.3f,.17f),V(.13f,.55f,.025f),"Cream",false,model);
            else
            {
                Box("Canvas shoulder bag",V(-.28f,1.0f,-.015f),V(.18f,.33f,.28f),"WoodLight",false,model);
                Beam("Bag strap",V(-.28f,1.03f,.16f),V(.17f,1.47f,.17f),.019f,"Wood",model);
            }
            if(!player) { var body=root.AddComponent<CapsuleCollider>(); body.height=1.8f; body.center=V(0,.9f,0); body.radius=.28f; }
            SetLayer(root,player?2:0);
            return root;
        }
        static void SetLayer(GameObject root,int layer)
        { root.layer=layer; foreach(Transform child in root.transform) SetLayer(child.gameObject,layer); }

        static ParticleSystem Rain()
        {
            var go=new GameObject("Lane rainfall"); go.transform.position=V(-8.5f,7,3);
            var ps=go.AddComponent<ParticleSystem>();
            var main=ps.main; main.startLifetime=1.1f; main.startSpeed=0; main.startSize=.022f;
            main.startColor=new Color(.66f,.74f,.7f,.25f); main.maxParticles=2400;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=ps.emission; emission.rateOverTime=1800;
            var shape=ps.shape; shape.shapeType=ParticleSystemShapeType.Box; shape.scale=V(46,.1f,32);
            var velocity=ps.velocityOverLifetime; velocity.enabled=true; velocity.space=ParticleSystemSimulationSpace.World;
            velocity.y=-10; velocity.x=-.9f; velocity.z=.35f;
            var collision=ps.collision; collision.enabled=true; collision.type=ParticleSystemCollisionType.World;
            collision.mode=ParticleSystemCollisionMode.Collision3D; collision.collidesWith=~(1<<2);
            collision.lifetimeLoss=1; collision.quality=ParticleSystemCollisionQuality.Low;
            var renderer=ps.GetComponent<ParticleSystemRenderer>(); renderer.renderMode=ParticleSystemRenderMode.Stretch;
            renderer.lengthScale=4; renderer.velocityScale=.018f;
            var m=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetColor("_BaseColor",new Color(.6f,.71f,.67f,.35f)); m.SetFloat("_Surface",1);
            m.SetFloat("_Blend",0); m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite",0); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue=3000;
            renderer.sharedMaterial=Save(m,Root+"/Generated/Materials/Rain.mat");
            return ps;
        }
        static void Lighting()
        {
            new GameObject("Overcast ambient fill").AddComponent<VillageLighting>();
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=Hex("A5B4AD")*.6f;
            RenderSettings.ambientEquatorColor=Hex("8D9985")*.55f;
            RenderSettings.ambientGroundColor=Hex("575E4A")*.45f;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=Hex("89948D"); RenderSettings.fogDensity=.013f;
            var sun=new GameObject("Overcast sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.transform.rotation=Quaternion.Euler(46,-32,0); sun.color=Hex("D9DECB"); sun.intensity=.65f;
            sun.shadows=LightShadows.Soft; sun.shadowStrength=.35f; sun.shadowBias=.04f;
            RenderSettings.sun=sun;
            var sky=new Material(Shader.Find("Paridhwani/Overcast Sky"));
            sky.SetColor("_Horizon",Hex("A0AAA5")); sky.SetColor("_Zenith",Hex("667C80"));
            RenderSettings.skybox=Save(sky,Root+"/Generated/Materials/OvercastSky.mat");
            RenderSettings.reflectionIntensity=.25f;
            var volume=new GameObject("Village atmosphere").AddComponent<Volume>(); volume.isGlobal=true;
            var profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var tone=profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var color=profile.Add<ColorAdjustments>(true); color.postExposure.Override(0f); color.contrast.Override(5); color.saturation.Override(-8);
            var vignette=profile.Add<Vignette>(true); vignette.intensity.Override(.16f); vignette.smoothness.Override(.5f);
            var bloom=profile.Add<Bloom>(true); bloom.intensity.Override(0); bloom.threshold.Override(2);
            PersistAtmosphere(profile);
        }

        public static void GenerateAndCheck() { Generate(); TeaStallChecks.Run(); }

        [MenuItem("Paridhwani/Validate and Build Benchmark")]
        public static void ValidateAndBuild()
        {
            Generate(); TeaStallChecks.Run();
            string output="Builds/TeaStall/Paridhwani.exe";
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{ScenePath}, locationPathName=output,
                target=BuildTarget.StandaloneWindows64, options=BuildOptions.Development
            });
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Tea-stall player build failed: "+report.summary.result);
            Debug.Log("PARIDHWANI_BUILD_OK "+Path.GetFullPath(output));
        }
    }
    static class CameraExtensions
    {
        internal static UniversalAdditionalCameraData AdditionalCameraData(this Camera c) =>
            c.GetUniversalAdditionalCameraData();
    }
}
