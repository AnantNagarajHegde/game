using System;
using UnityEditor;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        // Visual skins only. Existing floor/crossing/stepping-stone colliders,
        // NPCs and all route/interaction anchors are retained verbatim.
        static void FrontageMesh(string name,SurfaceGeometry geometry,Material material,Transform parent)
        {
            string path=Root+"/Generated/Meshes/"+name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);bool saved=mesh;
            if(!mesh)mesh=new Mesh{name=name};else mesh.Clear();
            mesh.SetVertices(geometry.vertices);mesh.SetColors(geometry.colors);mesh.SetTriangles(geometry.triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(false);
            if(saved)EditorUtility.SetDirty(mesh);else mesh=Save(mesh,path);
            var item=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(parent,false);
            item.GetComponent<MeshFilter>().sharedMesh=mesh;item.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static Bounds FrontageBounds(BoxCollider body)
        {
            // Newly created Edit-mode colliders may still report their unit-cube
            // physics bounds. These authored slabs are axis aligned: derive the
            // visual footprint directly, without altering or depending on physics.
            if(Quaternion.Angle(body.transform.rotation,Quaternion.identity)>.001f)throw new InvalidOperationException("Frontage slab must be axis aligned: "+body.name);
            return new Bounds(body.transform.TransformPoint(body.center),Vector3.Scale(body.size,body.transform.lossyScale));
        }
        static void FrontageFloor(string original,string id,float cell,Material material,Transform parent,System.Random rng)
        {
            var source=environment.Find(original);if(!source)throw new InvalidOperationException("Missing frontage floor: "+original);
            var body=source.GetComponent<BoxCollider>();var bounds=FrontageBounds(body);source.GetComponent<MeshRenderer>().enabled=false;
            float top=bounds.max.y+.002f,x0=bounds.min.x,x1=bounds.max.x,z0=bounds.min.z,z1=bounds.max.z;
            var stone=new SurfaceGeometry();var mortar=new SurfaceGeometry();
            // Recessed continuous grout under the joints; no holes down to the soil.
            int a=mortar.Point(V(x0,top-.012f,z0),new Color(.6f,.6f,.58f,.3f));
            mortar.Point(V(x1,top-.012f,z0),new Color(.6f,.6f,.58f,.3f));mortar.Point(V(x0,top-.012f,z1),new Color(.6f,.6f,.58f,.3f));mortar.Point(V(x1,top-.012f,z1),new Color(.6f,.6f,.58f,.3f));
            mortar.Triangle(a,a+2,a+1);mortar.Triangle(a+1,a+2,a+3);
            int row=0;
            for(float z=z0;z<z1-.001f;z+=cell,row++)
            {
                float depth=Mathf.Min(cell,z1-z),x=x0;
                while(x<x1-.001f)
                {
                    float width=Mathf.Min(x1-x,x==x0 && row%2==1?cell*.5f:cell);
                    float wet=Mathf.Lerp(.5f,.08f,Mathf.Clamp01((z-z0)/.9f));
                    stone.Stone(V(x+width*.5f,top-.020f,z+depth*.5f),V(width-.012f,.040f,depth-.012f),rng,wet,1.04f);
                    x+=width;
                }
            }
            // Bonded front/side facing follows the existing slab footprint. Corners
            // stop before the front strip, so their coplanar surfaces never overlap.
            float height=Mathf.Max(.03f,bounds.max.y-Mathf.Max(0,bounds.min.y));
            for(float x=x0;x<x1-.001f;x+=.72f)
            {
                float width=Mathf.Min(.72f,x1-x);
                stone.Stone(V(x+width*.5f,bounds.max.y-height*.5f-.004f,z0+.035f),V(width-.012f,height,.07f),rng,.65f,.86f);
            }
            for(int side=0;side<2;side++)for(float z=z0+.072f;z<z1-.001f;z+=.72f)
            {
                float depth=Mathf.Min(.72f,z1-z);
                stone.Stone(V(side==0?x0+.035f:x1-.035f,bounds.max.y-height*.5f-.004f,z+depth*.5f),V(.07f,height,depth-.012f),rng,.5f,.88f);
            }
            FrontageMesh(id+"Mortar",mortar,material,parent);FrontageMesh(id+"Stone",stone,material,parent);
        }
        static void FrontageApron(string id,float x0,float x1,float edgeZ,Material material,Transform parent)
        {
            var ground=new SurfaceGeometry();const int across=24,deep=6;
            var original=mats["Earth"];Color originalColor=original.GetColor("_BaseColor").linear,baseColor=material.GetColor("_BaseColor").linear;
            var ratio=new Color(originalColor.r/baseColor.r,originalColor.g/baseColor.g,originalColor.b/baseColor.b,1);
            for(int row=0;row<=deep;row++)for(int col=0;col<=across;col++)
            {
                float x=Mathf.Lerp(x0,x1,col/(float)across),z=Mathf.Lerp(edgeZ-.55f,edgeZ,row/(float)deep);
                float blend=Mathf.SmoothStep(0,1,row/(float)deep)*Mathf.SmoothStep(0,1,Mathf.Min(x-x0,x1-x)/.25f);
                float texture=.87f+Mathf.PerlinNoise(x*1.4f,z*1.4f)*.16f;
                var edge=new Color(ratio.r,ratio.g,ratio.b,original.GetFloat("_Wet")/material.GetFloat("_Wet"));
                ground.Point(V(x,.0015f+blend*.004f,z),Color.Lerp(edge,new Color(texture,texture*.98f,texture*.94f,.8f),blend));
            }
            for(int row=0;row<deep;row++)for(int col=0;col<across;col++)
            {int a=row*(across+1)+col,b=a+1,c=a+across+1,d=c+1;ground.Triangle(a,c,b);ground.Triangle(b,c,d);}
            FrontageMesh(id+"DampApron",ground,material,parent);
        }
        static void VillageFrontageArt()
        {
            var parent=new GameObject("Village Frontage Surface Art").transform;parent.SetParent(environment,false);
            var rng=new System.Random(20261028);
            var stone=TeaSurfaceMaterial("VillageFrontageStone","StoneDark","777669",.94f,.6f);
            var earth=TeaSurfaceMaterial("VillageFrontageEarth","Earth","705540",.97f,.65f);
            FrontageFloor("Library public veranda","LibraryFrontage",.69f,stone,parent,rng);
            FrontageFloor("Mess stone threshold","MessFrontage",.64f,stone,parent,rng);
            FrontageFloor("Market raised stone floor","MarketFrontage",.72f,stone,parent,rng);
            FrontageFloor("Mess drain crossing","MessCrossing",.50f,stone,parent,rng);
            FrontageApron("LibraryFrontage",-8.25f,-1.65f,10.30f,earth,parent);
            FrontageApron("MessFrontage",5.05f,9.75f,-1.10f,earth,parent);
            FrontageApron("MarketFrontage",5.60f,10.50f,8.30f,earth,parent);
            var steps=new SurfaceGeometry();
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name!="Market approach stepping stone")continue;
                var bounds=FrontageBounds(renderer.GetComponent<BoxCollider>());renderer.enabled=false;
                // The remaining original steps lie below the raised market floor.
                if(bounds.center.x<5.55f)steps.Stone(bounds.center+Vector3.up*.001f,bounds.size,rng,.6f,.90f);
            }
            FrontageMesh("MarketApproachWeatheredSteps",steps,stone,parent);
        }
    }
}
