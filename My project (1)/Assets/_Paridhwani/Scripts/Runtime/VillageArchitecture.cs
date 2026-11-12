using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Paridhwani
{
    // Deterministic construction detail, applied to both the benchmark and additive
    // worlds after their roof infill. Geometry is visual only and batched per facade.
    public sealed class VillageArchitecture : MonoBehaviour
    {
        const string RootName="Village architectural finish";
        readonly List<Object> owned=new List<Object>();
        readonly Dictionary<Material,Material> surfaces=new Dictionary<Material,Material>();
        public int RoofCount {get;private set;}
        public int JoineryCount {get;private set;}
        public int WallCount {get;private set;}
        public int DetailVertices {get;private set;}
        public static void Apply(Scene scene)
        {
            var renderers=new List<MeshRenderer>();
            foreach(var root in scene.GetRootGameObjects())
            {
                if(root.name==RootName)return;
                renderers.AddRange(root.GetComponentsInChildren<MeshRenderer>(true));
            }
            var owner=new GameObject(RootName);SceneManager.MoveGameObjectToScene(owner,scene);
            owner.AddComponent<VillageArchitecture>().Build(renderers);
        }
        Material Surface(Material source)
        {
            if(surfaces.TryGetValue(source,out var found))return found;
            string key=source.name;var material=new Material(source){name=key+" | architectural finish"};
            float relief=key=="Plaster"?.045f:key=="Damp"?.025f:key.StartsWith("Clay")?.065f:key.StartsWith("Wood")?.035f:.028f;
            material.SetFloat("_Relief",relief);
            if(key=="Plaster") {material.SetFloat("_Weathering",.75f);material.SetFloat("_Roughness",.94f);material.SetFloat("_TextureStrength",.6f);}
            if(key.StartsWith("Wood"))material.SetFloat("_Roughness",.82f);
            surfaces.Add(source,material);owned.Add(material);return material;
        }
        Material Detail(Material source,string name,Color tint)
        {
            var material=new Material(Surface(source)){name=name};
            material.SetColor("_BaseColor",tint);material.SetFloat("_VertexSurface",1);
            owned.Add(material);return material;
        }
        void Build(List<MeshRenderer> renderers)
        {
            Material wood=null,stone=null,clay=null;
            foreach(var r in renderers)
            {
                if(r.GetComponentInParent<VillageResidence>())continue;
                var m=r.sharedMaterial;if(!m || !m.shader || m.shader.name!="Paridhwani/Village Surface")continue;
                string key=m.name;
                if(key=="Wood")wood=m;if(key=="Stone")stone=m;if(key=="Clay")clay=m;
                if(key=="Plaster" || key=="Damp" || key=="Wood" || key=="WoodLight" || key=="Clay" || key=="ClayLight" || key=="Stone" || key=="StoneDark")r.sharedMaterial=Surface(m);
            }
            if(!wood || !stone || !clay)return;
            var timber=Detail(wood,"Aged structural timber",new Color(.30f,.265f,.205f));
            var masonry=Detail(stone,"Dressed foundation stone",new Color(.40f,.415f,.38f));
            var tile=Detail(clay,"Weathered ridge caps",new Color(.43f,.245f,.17f));
            foreach(var r in renderers)
            {
                if(r.GetComponentInParent<VillageResidence>())continue;
                if(!r.enabled || !r.sharedMaterial)continue;
                var b=r.bounds;string key=r.sharedMaterial.name;
                if(key.StartsWith("Clay |") && b.size.x>2 && b.size.z>2 && b.size.y<2.5f)
                {Roof(r,b,timber,tile);RoofCount++;}
                if(key.StartsWith("Plaster |") && b.size.y>2 && !r.name.StartsWith("Roof-fitted"))
                {Wall(r,b,masonry);WallCount++;}
                if(key.StartsWith("Wood |") && b.size.z<.3f && b.size.x>.7f && b.size.y>.65f &&
                   (r.name.EndsWith("door") || r.name.EndsWith("window")))
                {Joinery(r,b,timber,masonry);JoineryCount++;}
            }
        }
        void Roof(MeshRenderer r,Bounds b,Material timber,Material tile)
        {
            var mesh=new Geometry();float peak=b.max.y-.055f,mid=b.center.z;
            foreach(float z in new[]{b.min.z+.03f,b.max.z-.03f})
            {
                float y=peak-Mathf.Abs(z-mid)*.31f-.085f;
                mesh.Box(new Vector3(b.center.x,y,z),new Vector3(b.size.x,.14f,.12f));
                foreach(float x in new[]{b.min.x+.06f,b.max.x-.06f})
                    mesh.Beam(new Vector3(x,y,z),new Vector3(x,peak-.075f,mid),.10f,.12f);
                for(float x=b.min.x+.3f;x<b.max.x-.15f;x+=.8f)
                    mesh.Beam(new Vector3(x,y-.055f,z),new Vector3(x,peak-.13f,mid),.065f,.085f);
            }
            Emit(r.transform,"Timber fascia and rafters",mesh,timber);
            var caps=new Geometry();
            for(float x=b.min.x+.1f;x<b.max.x-.08f;x+=.43f)
                caps.Ridge(new Vector3(x,peak+.045f,mid),Mathf.Min(.46f,b.max.x-x),.115f);
            Emit(r.transform,"Overlapping ridge tiles",caps,tile);
        }
        void Wall(MeshRenderer r,Bounds b,Material material)
        {
            var mesh=new Geometry();
            // Two low stone courses and corner quoins articulate scale without
            // projecting into doors or changing the walkable collision footprint.
            if(b.size.x>.6f)
            {
                Course(mesh,b.min.x,b.max.x,b.min.z-.018f,b.min.y,true);
                if(b.size.z>.6f)Course(mesh,b.min.x,b.max.x,b.max.z+.018f,b.min.y,true);
            }
            if(b.size.z>.6f)
            {
                Course(mesh,b.min.z,b.max.z,b.min.x-.018f,b.min.y,false);
                if(b.size.x>.6f)Course(mesh,b.min.z,b.max.z,b.max.x+.018f,b.min.y,false);
            }
            Emit(r.transform,"Bonded stone foundation",mesh,material);
        }
        static void Course(Geometry mesh,float start,float end,float plane,float bottom,bool alongX)
        {
            for(int row=0;row<2;row++)
            {
                float cursor=start;bool first=true;
                while(cursor<end-.01f)
                {
                    float width=Mathf.Min(end-cursor,first && row==1?.32f:.64f),mid=cursor+width*.5f;
                    var center=alongX?new Vector3(mid,bottom+.085f+row*.16f,plane):new Vector3(plane,bottom+.085f+row*.16f,mid);
                    mesh.Box(center,alongX?new Vector3(width-.012f,.148f,.048f):new Vector3(.048f,.148f,width-.012f),.012f);
                    cursor+=width;first=false;
                }
            }
        }
        void Joinery(MeshRenderer r,Bounds b,Material timber,Material stone)
        {
            var frame=new Geometry();float z=b.min.z-.035f;
            foreach(float x in new[]{b.min.x-.025f,b.max.x+.025f})frame.Box(new Vector3(x,b.center.y,z),new Vector3(.095f,b.size.y+.13f,.10f),.012f);
            frame.Box(new Vector3(b.center.x,b.max.y+.04f,z),new Vector3(b.size.x+.16f,.12f,.12f),.012f);
            if(r.name.EndsWith("door"))
            {
                foreach(float y in new[]{b.min.y+.28f,b.max.y-.28f})frame.Box(new Vector3(b.center.x,y,z-.045f),new Vector3(b.size.x-.09f,.055f,.028f),.007f);
                for(float x=b.min.x+.17f;x<b.max.x-.08f;x+=.18f)frame.Box(new Vector3(x,b.center.y,z+.017f),new Vector3(.009f,b.size.y-.06f,.016f),.003f);
                frame.Box(new Vector3(b.max.x-.12f,b.center.y,z-.065f),new Vector3(.032f,.16f,.045f),.006f);
            }
            else
            {
                frame.Box(new Vector3(b.center.x,b.center.y,z-.06f),new Vector3(.045f,b.size.y,.05f),.006f);
                frame.Box(new Vector3(b.center.x,b.center.y,z-.06f),new Vector3(b.size.x,.045f,.05f),.006f);
                var sill=new Geometry();sill.Box(new Vector3(b.center.x,b.min.y-.065f,z-.035f),new Vector3(b.size.x+.23f,.10f,.24f),.018f);
                Emit(r.transform,"Dressed window sill",sill,stone);
            }
            Emit(r.transform,"Rebated frame and joinery",frame,timber);
        }
        void Emit(Transform parent,string name,Geometry geometry,Material material)
        {
            if(geometry.vertices.Count==0)return;
            var item=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));item.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};owned.Add(mesh);
            for(int i=0;i<geometry.vertices.Count;i++)geometry.vertices[i]=item.transform.InverseTransformPoint(geometry.vertices[i]);
            mesh.SetVertices(geometry.vertices);mesh.SetColors(geometry.colors);mesh.SetTriangles(geometry.triangles,0);
            mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.UploadMeshData(false);
            item.GetComponent<MeshFilter>().sharedMesh=mesh;item.GetComponent<MeshRenderer>().sharedMaterial=material;
            DetailVertices+=geometry.vertices.Count;
        }
        void OnDestroy(){foreach(var value in owned)if(value)Destroy(value);owned.Clear();}

        sealed class Geometry
        {
            public readonly List<Vector3> vertices=new List<Vector3>();
            public readonly List<Color> colors=new List<Color>();
            public readonly List<int> triangles=new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {
                int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                colors.AddRange(new[]{color,color,color,color});triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            void Triangle(Vector3 a,Vector3 b,Vector3 c,Color color)
            {int n=vertices.Count;vertices.AddRange(new[]{a,b,c});colors.AddRange(new[]{color,color,color});triangles.AddRange(new[]{n,n+1,n+2});}
            public void Beam(Vector3 a,Vector3 b,float width,float depth)
            {Box((a+b)*.5f,new Vector3(width,(b-a).magnitude,depth),.006f,Quaternion.FromToRotation(Vector3.up,(b-a).normalized));}
            public void Box(Vector3 center,Vector3 size,float bevel=.008f,Quaternion rotation=default)
            {
                if(rotation==default)rotation=Quaternion.identity;
                float x=size.x*.5f,y=size.y*.5f,z=size.z*.5f;
                float e=Mathf.Min(bevel,Mathf.Min(x,Mathf.Min(y,z))*.5f);
                var outline=new[]{new Vector2(-x+e,-z),new Vector2(x-e,-z),new Vector2(x,-z+e),new Vector2(x,z-e),new Vector2(x-e,z),new Vector2(-x+e,z),new Vector2(-x,z-e),new Vector2(-x,-z+e)};
                float tint=.91f+Mathf.PerlinNoise(center.x*1.7f,center.z*2.3f+center.y)*.17f;
                var color=new Color(tint,tint,tint,.8f);var lower=new Vector3[8];var upper=new Vector3[8];var cap=new Vector3[8];
                for(int i=0;i<8;i++)
                {var p=outline[i];lower[i]=center+rotation*new Vector3(p.x,-y,p.y);upper[i]=center+rotation*new Vector3(p.x,y-e,p.y);cap[i]=center+rotation*new Vector3(p.x*(x-e)/x,y,p.y*(z-e)/z);}
                for(int i=0;i<8;i++)
                {
                    int j=(i+1)%8;Quad(lower[i],upper[i],upper[j],lower[j],color);Quad(upper[i],cap[i],cap[j],upper[j],color);
                    Triangle(cap[i],center+rotation*Vector3.up*y,cap[j],color);
                    Triangle(lower[j],center-rotation*Vector3.up*y,lower[i],color);
                }
            }
            public void Ridge(Vector3 start,float length,float radius)
            {
                var color=new Color(.96f,.96f,.94f,.6f);
                for(int i=0;i<8;i++)
                {
                    float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;
                    var va=new Vector3(0,Mathf.Sin(a)*radius,Mathf.Cos(a)*radius);
                    var vb=new Vector3(0,Mathf.Sin(b)*radius,Mathf.Cos(b)*radius);
                    Quad(start+va,start+Vector3.right*length+va,start+Vector3.right*length+vb,start+vb,color);
                }
            }
        }
    }
}
