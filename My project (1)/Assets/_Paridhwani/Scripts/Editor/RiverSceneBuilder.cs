using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void AddRiverChapter(TeaStallSession session)
        {
            Mat("BicycleGreen","345D48",.74f,.12f,35);
            Mat("BasketCane","AC9366",.94f,.05f,42);
            Mat("GiriShirt","6A8076",.97f,0,40);
            // Open the west-town boundary only at the marked, fenced river path.
            Box("West boundary south",V(-12,2,-4),V(.2f,5,17),"Earth").GetComponent<Renderer>().enabled=false;
            Box("West boundary north",V(-12,2,13.4f),V(.2f,5,9.8f),"Earth").GetComponent<Renderer>().enabled=false;
            Box("River path",V(-12,.02f,6.5f),V(23,.04f,2.8f),"Lane");
            Box("River bank ground",V(-25,-.18f,5.3f),V(10.2f,.4f,14.4f),"Earth");
            for(int i=0;i<22;i++) Box("Worn path stone",V(-3-i*1.12f,.065f,6.45f+R(-.25f,.25f)),V(.6f,.06f,.8f),"StoneDark");
            Rail(V(-16,0,4.8f),V(8.2f,1,0.12f));
            Rail(V(-16,0,8.3f),V(8.2f,1,0.12f));
            Rail(V(-20,0,1.6f),V(.12f,1,6.2f));
            Rail(V(-20,0,10.4f),V(.12f,1,4.2f));
            Rail(V(-25,0,-1.8f),V(10.4f,1,.12f));
            Rail(V(-25,0,12.5f),V(10.4f,1,.12f));
            Box("Weir stone retaining wall",V(-30.25f,-.65f,5.3f),V(.7f,1.8f,14.6f),"Stone");
            Rail(V(-29.85f,0,5.3f),V(.14f,1,14.4f));
            for(int i=0;i<9;i++) Box("Weir coping stone",V(-30.2f,.27f,-1.1f+i*1.65f),V(.9f,.16f,1.5f),"StoneDark");
            var riverMat=new Material(Shader.Find("Paridhwani/River Water"));
            riverMat.SetColor("_BaseColor",Hex("354F48")); riverMat.SetColor("_FoamColor",Hex("9AA695"));
            mats["River"]=Save(riverMat,Root+"/Generated/Materials/RiverWater.mat");
            Box("River upstream",V(-35.2f,-.95f,9.2f),V(9.7f,.045f,14.1f),"River",false);
            Box("River downstream",V(-35.2f,-1.5f,-5.4f),V(9.7f,.045f,14.2f),"River",false);
            Box("Low stone check dam",V(-35.2f,-1.25f,2.35f),V(9.7f,1.15f,.8f),"Stone");
            Box("Water across crest",V(-35.2f,-.66f,2.35f),V(9.75f,.035f,.9f),"River",false);
            Box("Weir falling water",V(-35.2f,-1.07f,1.92f),V(9.7f,.82f,.035f),"River",false);
            Box("Far bank",V(-42,-.15f,4),V(4,.8f,32),"Earth",false);
            for(int i=0;i<18;i++)
            {
                Ellipsoid("Riverbank rock",V(-31.2f+R(-.4f,.1f),-.9f,-10+i*1.6f),V(R(.4f,.9f),R(.4f,.8f),R(.5f,1.1f)),"StoneDark");
                Ellipsoid("Far bank vegetation",V(-40.8f,.6f,-10+i*1.7f),V(1.9f,2,2.4f),i%2==0?"Leaf":"LeafLight");
            }
            var current=new GameObject("Weir current ambience"); current.transform.position=V(-32,-.7f,2);
            current.AddComponent<RiverAmbience>().preferences=session.ambience;

            Box("Repair shed stone floor",V(-25.4f,.08f,4.6f),V(6.8f,.16f,4.7f),"StoneDark");
            Box("Repair shed back wall",V(-25.4f,1.5f,6.85f),V(6.8f,2.8f,.18f),"Plaster");
            Box("Repair shed damp base",V(-25.4f,.4f,6.73f),V(6.8f,.48f,.035f),"Damp",false);
            foreach(float x in new[]{-28.55f,-22.25f}) Box("Repair shed post",V(x,1.65f,2.4f),V(.15f,3,.15f),"Wood");
            Roof("River repair shed roof",-25.4f,4.65f,3.95f,7.2f,5.4f);
            Box("Repair shed rain barrier",V(-25.4f,3.16f,4.6f),V(7.2f,.08f,5.3f),"Wood").GetComponent<Renderer>().enabled=false;
            var fill=new GameObject("Shed bounced daylight").AddComponent<Light>();
            fill.type=LightType.Point; fill.color=Hex("CBD3BE"); fill.intensity=2; fill.range=7;
            fill.transform.position=V(-25.2f,2.8f,2.6f); fill.shadows=LightShadows.None;
            Bench(V(-23.1f,.15f,6));
            Box("Repair workbench",V(-27.4f,.72f,6.3f),V(1.65f,1.05f,.65f),"Wood");
            Box("Repair bench top",V(-27.4f,1.27f,6.3f),V(1.8f,.09f,.8f),"WoodLight");
            Beam("Spanner",V(-27.8f,1.34f,6.1f),V(-27.4f,1.34f,6.3f),.018f,"Steel");
            Cylinder("Oil tin",V(-27,1.48f,6.3f),.1f,.3f,"Steel");
            Box("Repair shed nameboard",V(-25.3f,2.65f,2.35f),V(3.4f,.5f,.05f),"Sign",false);
            Text("ಗಿರಿ ಬೈಸಿಕಲ್ ರಿಪೇರಿ",V(-25.3f,2.67f,2.30f),.11f,true,0);
            Text("REPAIR SHED  /  GIRI",V(-25.3f,2.42f,2.29f),.045f,false,0);
            var actor=Character("Giri",V(-23.5f,.16f,4.65f),false);
            actor.transform.rotation=Quaternion.Euler(0,180,0);
            var npc=actor.AddComponent<VillageNpc>(); npc.role=VillageNpcRole.RiverKeeper; npc.displayName="Giri"; npc.roleLabel="RIVER REPAIR SHED";
            npc.eyes=actor.GetComponentInChildren<VillageCharacter>().head;
            actor.GetComponentInChildren<VillageCharacter>().lookTarget=session.player.transform;
            var all=new List<VillageNpc>(session.villagers); all.Add(npc); session.villagers=all.ToArray();
            CreateBicycle(session);
            session.bicycleView=new GameObject("Bicycle inspection camera pose").transform; session.bicycleView.position=V(-26.3f,1.72f,2.05f);
            session.bicycleLookTarget=new GameObject("Bicycle inspection framing").transform; session.bicycleLookTarget.position=V(-26.3f,.2f,4.7f);
            Box("River path sign",V(-.15f,1.15f,6.1f),V(1.7f,.43f,.06f),"Sign",false);
            Text("RIVER PATH  ←",V(-.15f,1.15f,6.05f),.06f,false,0);
            Cylinder("River marker post",V(-.15f,.53f,6.1f),.045f,1.1f,"Wood");
            Box("River return sign",V(-20.7f,1.15f,7.8f),V(1.7f,.43f,.06f),"Sign",false);
            Text("TOWN  →",V(-20.7f,1.15f,7.75f),.06f,false,0);
        }
        static void Rail(Vector3 center,Vector3 size)
        {
            var barrier=Box("River path railing collider",center+Vector3.up*.65f,new Vector3(size.x,1.3f,size.z),"Wood");
            barrier.GetComponent<Renderer>().enabled=false;
            bool alongX=size.x>size.z; float length=alongX?size.x:size.z;
            for(float d=-length*.5f;d<=length*.5f+.1f;d+=1.5f)
                Box("River rail post",center+(alongX?V(d,.65f,0):V(0,.65f,d)),V(.12f,1.3f,.12f),"Wood");
            foreach(float y in new[]{.55f,1.13f})
                Box("River rail",center+Vector3.up*y,new Vector3(size.x,.08f,size.z),"Wood");
        }
        static void CreateBicycle(TeaStallSession session)
        {
            var root=new GameObject("Ananya green bicycle").transform; root.SetParent(environment,false); root.position=V(-26.3f,.16f,4.7f);
            var tyre=BikeRing("BicycleTyre",.43f,.033f,0,Mathf.PI*2);
            var rim=BikeRing("BicycleRim",.385f,.012f,0,Mathf.PI*2);
            var mudguard=BikeRing("BicycleMudguard",.463f,.025f,.08f,Mathf.PI-.08f);
            foreach(float x in new[]{-.8f,.8f})
            {
                BikeMesh("Bicycle tyre",V(x,.44f,0),tyre,"Tyre",root);
                BikeMesh("Steel wheel rim",V(x,.44f,0),rim,"Steel",root);
                BikeMesh("Green mudguard",V(x,.44f,0),mudguard,"BicycleGreen",root);
                for(int i=0;i<14;i++)
                {
                    float a=i*Mathf.PI*2/14;
                    Beam("Wheel spoke",V(x,.44f,.008f),V(x+Mathf.Cos(a)*.38f,.44f+Mathf.Sin(a)*.38f,.008f),.004f,"Steel",root);
                }
                Cylinder("Wheel hub",V(x,.44f,0),.035f,.12f,"Steel",false,root).transform.localRotation=Quaternion.Euler(90,0,0);
            }
            foreach(var pair in new[]{
                new[]{V(-.8f,.44f,0),V(-.18f,.37f,0)}, new[]{V(-.18f,.37f,0),V(-.28f,.98f,0)},
                new[]{V(-.28f,.87f,0),V(-.8f,.44f,0)},new[]{V(-.18f,.37f,0),V(.3f,.45f,0)},
                new[]{V(.3f,.45f,0),V(.61f,.98f,0)},new[]{V(-.28f,.88f,0),V(.25f,.63f,0)},
                new[]{V(.25f,.63f,0),V(.61f,.98f,0)},new[]{V(.61f,.99f,-.045f),V(.8f,.44f,-.045f)},
                new[]{V(.61f,.99f,.045f),V(.8f,.44f,.045f)}
            }) Beam("Green bicycle frame",pair[0],pair[1],.025f,"BicycleGreen",root);
            Ellipsoid("Bicycle saddle",V(-.28f,1.03f,0),V(.32f,.075f,.22f),"Hair",root);
            Beam("Handlebar stem",V(.61f,.98f,0),V(.57f,1.18f,0),.023f,"Steel",root);
            Beam("Handlebars",V(.57f,1.18f,-.23f),V(.57f,1.18f,.23f),.019f,"Steel",root);
            Beam("Handle grip",V(.57f,1.18f,-.24f),V(.45f,1.18f,-.24f),.026f,"Tyre",root);
            Beam("Handle grip",V(.57f,1.18f,.24f),V(.45f,1.18f,.24f),.026f,"Tyre",root);
            Ellipsoid("Bicycle bell",V(.57f,1.2f,-.13f),V(.07f,.045f,.07f),"Brass",root);
            Beam("Deployed bicycle stand",V(-.35f,.37f,0),V(-.49f,.02f,-.28f),.017f,"Steel",root);
            Beam("Pedal crank",V(-.18f,.37f,-.08f),V(-.3f,.28f,-.11f),.013f,"Steel",root);
            Box("Pedal",V(-.3f,.28f,-.17f),V(.14f,.04f,.12f),"Tyre",false,root);
            Box("Rear carrier",V(-.68f,.93f,0),V(.51f,.035f,.28f),"Steel",false,root);
            Beam("Carrier support",V(-.8f,.44f,.08f),V(-.88f,.92f,.08f),.009f,"Steel",root);
            var pocket=Box("Dry carrier pocket",V(-.7f,.85f,-.13f),V(.29f,.19f,.09f),"Green",false,root);
            session.bookingPaper=Box("Advance booking inside pocket",V(-.7f,.935f,-.19f),V(.13f,.07f,.009f),"Cream",false,root);
            // A light open basket keeps the frame and front wheel visible.
            Box("Cane basket base",V(.86f,.94f,0),V(.4f,.035f,.37f),"BasketCane",false,root);
            for(int i=0;i<6;i++)
            {
                float t=i/5f;
                foreach(float z in new[]{-.19f,.19f}) Beam("Basket weave",V(.65f+t*.42f,.95f,z),V(.65f+t*.42f,1.2f,z),.007f,"BasketCane",root);
                foreach(float x in new[]{.65f,1.07f}) Beam("Basket weave",V(x,.95f,-.19f+t*.38f),V(x,1.2f,-.19f+t*.38f),.007f,"BasketCane",root);
                foreach(float z in new[]{-.19f,.19f}) Beam("Basket weave",V(.65f,.97f+t*.23f,z),V(1.07f,.97f+t*.23f,z),.006f,"BasketCane",root);
                foreach(float x in new[]{.65f,1.07f}) Beam("Basket weave",V(x,.97f+t*.23f,-.19f),V(x,.97f+t*.23f,.19f),.006f,"BasketCane",root);
            }
            var collider=root.gameObject.AddComponent<BoxCollider>(); collider.center=V(0,.67f,0); collider.size=V(2.1f,1.34f,.6f);
            session.bicycle=new GameObject("Bicycle interaction anchor").transform; session.bicycle.position=V(-26.3f,.15f,4.45f);
        }
        static Mesh BikeRing(string name,float radius,float tube,float from,float to)
        {
            const int rings=40,sides=8; var vertices=new List<Vector3>(); var triangles=new List<int>();
            for(int i=0;i<=rings;i++) for(int j=0;j<=sides;j++)
            {
                float a=Mathf.Lerp(from,to,i/(float)rings),b=j/(float)sides*Mathf.PI*2;
                float r=radius+Mathf.Cos(b)*tube;
                vertices.Add(V(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*tube));
            }
            for(int i=0;i<rings;i++) for(int j=0;j<sides;j++)
            {
                int a=i*(sides+1)+j,b=a+1,c=a+sides+1,d=c+1;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
            return Save(mesh,Root+"/Generated/Meshes/"+name+".asset");
        }
        static void BikeMesh(string name,Vector3 position,Mesh mesh,string material,Transform parent)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=mats[material];
        }
    }
}
