using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void ExpandVillage(TeaStallSession session)
        {
            var tea=session.vendor.gameObject.AddComponent<VillageNpc>();
            tea.displayName="Manjunatha"; tea.roleLabel="TEA STALL";
            tea.role=VillageNpcRole.TeaVendor; tea.eyes=session.vendor.GetComponentInChildren<VillageCharacter>().head;
            var somanna=Character("Somanna",V(-4.6f,.02f,11.3f),false);
            somanna.transform.rotation=Quaternion.Euler(0,180,0);
            var librarian=somanna.AddComponent<VillageNpc>(); librarian.displayName="Somanna";
            librarian.roleLabel="LIBRARY VERANDA"; librarian.role=VillageNpcRole.Librarian;
            librarian.eyes=somanna.GetComponentInChildren<VillageCharacter>().head;
            somanna.GetComponentInChildren<VillageCharacter>().lookTarget=session.player.transform;
            Bench(V(-6.1f,.02f,11.7f));
            Box("Library public veranda",V(-4.8f,.02f,11.55f),V(6.9f,.09f,2.5f),"Stone");
            Box("Reading room closed notice",V(-2.4f,1.6f,13.29f),V(.7f,.8f,.05f),"Sign",false);
            Text("ಗ್ರಂಥಾಲಯ",V(-4.7f,2.9f,13.25f),.12f,true,0);
            Text("COMMUNITY LIBRARY",V(-4.7f,2.65f,13.24f),.045f,false,0);
            foreach(float x in new[]{-8.05f,-1.5f}) Box("Library veranda post",V(x,1.55f,11.1f),V(.17f,3,.17f),"Wood");
            Roof("Library veranda eave",-4.8f,12.1f,3.5f,7.1f,3.5f);
            Box("Library eave weather collider",V(-4.8f,3.08f,11.8f),V(7.1f,.1f,2.7f),"Wood").GetComponent<Renderer>().enabled=false;
            // The banyan garden is bounded scenery, with a clear walking route to the door.
            Cylinder("Heritage banyan trunk",V(-9,2.1f,9.4f),.38f,4.2f,"Trunk",true);
            for(int i=0;i<7;i++)
            {
                float a=i*Mathf.PI*2/7;
                Vector3 end=V(-9+Mathf.Cos(a)*1.8f,4.2f,9.4f+Mathf.Sin(a)*1.8f);
                Beam("Banyan branch",V(-9,2.5f,9.4f),end,.12f,"Trunk");
                Ellipsoid("Banyan canopy",end+Vector3.up*.4f,V(3.4f,1.7f,3.1f),i%2==0?"Leaf":"LeafLight");
            }
            foreach(float x in new[]{-9.5f,-8.1f})
                Beam("Banyan aerial root",V(x,3.7f,9.6f),V(x,0,9.6f),.035f,"Trunk");

            // A welcoming mess veranda across the lane, with a gap in its compound wall.
            Box("Mess stone threshold",V(7.4f,.09f,.85f),V(4.8f,.18f,3.9f),"Stone");
            Box("Mess serving counter",V(7.5f,.55f,1.65f),V(2.8f,1,.6f),"Wood");
            Box("Mess counter stone",V(7.5f,1.08f,1.65f),V(2.95f,.12f,.75f),"StoneDark");
            foreach(float x in new[]{5.2f,9.7f}) Box("Mess veranda post",V(x,1.5f,-.8f),V(.16f,2.9f,.16f),"Wood");
            Roof("Mess veranda eave",7.5f,.6f,3.45f,5.3f,4.1f);
            Box("Mess roof weather collider",V(7.5f,2.93f,.6f),V(5.3f,.1f,4),"Wood").GetComponent<Renderer>().enabled=false;
            Box("Mess nameboard",V(7.5f,2.55f,-.85f),V(3.4f,.55f,.07f),"Sign",false);
            Text("ಶಾಂತಕ್ಕನ ಊಟದ ಮನೆ",V(7.5f,2.62f,-.90f),.11f,true,0);
            Text("SHANTHAKKA'S MESS",V(7.5f,2.37f,-.91f),.047f,false,0);
            Box("Mess drain crossing",V(4.5f,.12f,-.2f),V(1.5f,.12f,1.5f),"Stone");
            Bench(V(6,.18f,.2f)); Bench(V(9,.18f,.2f));
            var shanthakka=Character("Shanthakka",V(7.4f,.18f,2.5f),false,true);
            shanthakka.transform.rotation=Quaternion.Euler(0,180,0);
            var cook=shanthakka.AddComponent<VillageNpc>(); cook.displayName="Shanthakka";
            cook.roleLabel="MESS VERANDA"; cook.role=VillageNpcRole.MessOwner;
            cook.eyes=shanthakka.GetComponentInChildren<VillageCharacter>().head;
            shanthakka.GetComponentInChildren<VillageCharacter>().lookTarget=session.player.transform;
            Cylinder("Serving vessel",V(8.3f,1.35f,1.65f),.23f,.42f,"Steel");
            Cylinder("Serving lid",V(8.3f,1.57f,1.65f),.25f,.035f,"Steel");
            Cylinder("Meal plate",V(7.1f,1.16f,1.5f),.23f,.025f,"Steel");
            for(int i=0;i<3;i++) Cylinder("Mess tumbler",V(6.5f+i*.17f,1.25f,1.7f),.055f,.18f,"Steel");
            var meal=new GameObject("Prepared meal"); meal.transform.position=V(7.1f,1.18f,1.5f);
            Ellipsoid("Rice",V(0,.04f,0),V(.23f,.085f,.23f),"Cream",meal.transform);
            Ellipsoid("Vegetables",V(.13f,.02f,.08f),V(.12f,.045f,.1f),"LeafLight",meal.transform);
            session.servedMeal=meal; meal.SetActive(false);
            Box("Ananya shopping slip",V(7.8f,1.16f,1.4f),V(.18f,.008f,.24f),"Cream",false);
            session.villagers=new[]{tea,librarian,cook};
            session.libraryLane.position=V(-2.2f,.1f,11.5f);
        }

        static void PersistAtmosphere(VolumeProfile profile)
        {
            string path=Root+"/Generated/VillageAtmosphere.asset";
            var asset=Save(profile,path);
            foreach(var component in asset.components)
                if(component && !AssetDatabase.Contains(component)) AssetDatabase.AddObjectToAsset(component,asset);
            EditorUtility.SetDirty(asset);
            Object.FindAnyObjectByType<Volume>().sharedProfile=asset;
        }

        static void ConfigureBenchmarkRendering()
        {
            PlayerSettings.productName="Paridhwani";
            PlayerSettings.defaultScreenWidth=1440; PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            // A private copy keeps this benchmark's choices out of the user's template renderer.
            var original=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
            var pipeline=Object.Instantiate(original);
            pipeline.name="Village_PC";
            pipeline.msaaSampleCount=4;
            pipeline.renderScale=1;
            pipeline.supportsHDR=true;
            pipeline=Save(pipeline,Root+"/Generated/Village_PC.asset");
            GraphicsSettings.defaultRenderPipeline=pipeline;
            QualitySettings.renderPipeline=pipeline;
            QualitySettings.vSyncCount=1;
            // Shader.Find-only runtime references must survive player shader stripping.
            string[] keep={"Universal Render Pipeline/Unlit","Hidden/Universal Render Pipeline/GaussianDepthOfField",
                "Hidden/Universal Render Pipeline/BokehDepthOfField","Hidden/Universal Render Pipeline/PaniniProjection"};
            var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
            foreach(string shaderName in keep)
            {
                var shader=Shader.Find(shaderName); if(!shader) continue;
                bool exists=false;
                for(int i=0;i<shaders.arraySize;i++) if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader) exists=true;
                if(!exists) { shaders.InsertArrayElementAtIndex(shaders.arraySize); shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=shader; }
            }
            graphics.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
