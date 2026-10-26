using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void AddMarketChapter(TeaStallSession session)
        {
            Mat("MarketCanvas","86765B",.97f,.08f,55).SetFloat("_Cull",0);
            Mat("Tomato","8D4432",.87f,.07f,32);
            Mat("Gourd","667046",.94f,.08f,35);
            Mat("Onion","A18C70",.98f,0,40);
            Mat("Aubergine","51454E",.8f,.1f,30);
            Mat("Sacking","8D805F",.98f,0,70);
            Box("Market raised stone floor",V(8.05f,.06f,10.3f),V(5,.12f,4),"Stone");
            for(int i=0;i<7;i++) Box("Market approach stepping stone",V(4.7f+i*.48f,.035f,8.9f),V(.43f,.065f,.65f),"StoneDark");
            Box("Market counter frame",V(8.1f,.58f,10.1f),V(3.7f,1,.7f),"Wood");
            Box("Market counter top",V(8.1f,1.11f,10.1f),V(3.85f,.1f,.82f),"WoodLight");
            for(int i=0;i<8;i++)
                Box("Market counter planking",V(6.42f+i*.48f,.62f,9.735f),V(.025f,.85f,.035f),"WoodLight",false);
            foreach(float x in new[]{5.75f,10.5f})
            {
                Box("Market canopy post",V(x,1.6f,9.2f),V(.12f,3.15f,.12f),"Wood");
                Beam("Market post brace",V(x,2.25f,9.2f),V(x+(x<8?.55f:-.55f),2.95f,9.2f),.045f,"Wood");
            }
            Beam("Market canopy ridge",V(5.6f,3.22f,11),V(10.65f,3.22f,11),.055f,"Wood");
            CanvasAwning();
            var rain=Box("Market canopy rain barrier",V(8.1f,2.88f,11),V(5.1f,.08f,3.8f),"Wood");
            rain.GetComponent<Renderer>().enabled=false;
            Box("Market sign backing",V(8.1f,2.6f,9.12f),V(3.55f,.55f,.065f),"Sign",false);
            Text("ಬಸವರಾಜು ತರಕಾರಿ ಅಂಗಡಿ",V(8.1f,2.67f,9.065f),.11f,true,0);
            Text("BASAVARAJU  /  PROVISIONS",V(8.1f,2.39f,9.06f),.043f,false,0);

            Crate(V(6.9f,1.16f,10.08f),.7f,.58f,"Tomato",0);
            Crate(V(8.0f,1.16f,10.08f),.7f,.58f,"Onion",1);
            Crate(V(10.1f,.13f,11.7f),.8f,.7f,"Gourd",2);
            Crate(V(10.1f,.13f,10.6f),.8f,.7f,"Aubergine",3);
            for(int i=0;i<3;i++)
            {
                Ellipsoid("Provision sack",V(6.1f+i*.43f,.48f,12),V(.44f,.73f,.42f),"Sacking");
                Cylinder("Tied sack neck",V(6.1f+i*.43f,.86f,12),.065f,.12f,"WoodLight");
            }
            var scales=new GameObject("Counter scales").transform; scales.position=V(9.2f,1.16f,10.1f);
            Cylinder("Scales base",V(0,.035f,0),.19f,.07f,"Steel",false,scales);
            Beam("Scales upright",V(0,.05f,0),V(0,.53f,0),.025f,"Steel",scales);
            Beam("Balance arm",V(-.32f,.5f,0),V(.32f,.5f,0),.017f,"Brass",scales);
            foreach(float x in new[]{-.3f,.3f})
            {
                Beam("Pan hanger",V(x,.5f,0),V(x,.24f,0),.009f,"Steel",scales);
                Cylinder("Weighing pan",V(x,.23f,0),.15f,.022f,"Steel",false,scales);
            }
            Box("Open ledger cover",V(8.55f,1.18f,9.97f),V(.53f,.025f,.42f),"Wood",false);
            Box("Ledger pages",V(8.55f,1.20f,9.97f),V(.49f,.018f,.39f),"Cream",false);
            for(int i=0;i<6;i++) Box("Ledger ruled line",V(8.55f,1.211f,9.83f+i*.05f),V(.43f,.001f,.0025f),"WoodLight",false);
            Beam("Ledger pencil",V(8.83f,1.23f,9.8f),V(8.89f,1.23f,10.09f),.009f,"Wood");

            var actor=Character("Basavaraju",V(8.15f,.14f,11.1f),false);
            actor.transform.rotation=Quaternion.Euler(0,215,0);
            var npc=actor.AddComponent<VillageNpc>();
            npc.displayName="Basavaraju"; npc.roleLabel="MARKET STALL"; npc.role=VillageNpcRole.MarketVendor;
            npc.eyes=actor.GetComponentInChildren<VillageCharacter>().head;
            actor.GetComponentInChildren<VillageCharacter>().lookTarget=session.player.transform;
            var all=new List<VillageNpc>(session.villagers); all.Add(npc); session.villagers=all.ToArray();

            Box("Market direction sign",V(3.8f,1.15f,8.2f),V(1.6f,.45f,.07f),"Sign",false);
            Text("MARKET  →",V(3.8f,1.15f,8.14f),.06f,false,0);
            Cylinder("Market marker post",V(3.8f,.53f,8.2f),.045f,1.1f,"Wood");

            // Public information remains readable before the player knows its connection to Ananya.
            Box("Timetable timber frame",V(3.35f,1.7f,-5.15f),V(2.3f,1.65f,.12f),"Wood");
            Box("Timetable board",V(3.35f,1.7f,-5.23f),V(2.15f,1.51f,.035f),"Sign",false);
            foreach(float x in new[]{2.35f,4.35f}) Box("Timetable stand",V(x,.78f,-5.15f),V(.08f,1.55f,.08f),"Wood");
            Text("BUS TIMETABLE",V(3.35f,2.25f,-5.26f),.065f,false,0);
            Text("14   GUDDA GATE   06:45",V(3.35f,1.89f,-5.27f),.065f,false,0);
            Text("08   MARKET         07:15",V(3.35f,1.58f,-5.27f),.06f,false,0);
            Text("14   GUDDA GATE   18:45",V(3.35f,1.27f,-5.27f),.065f,false,0);
            Box("Giri pinned notice",V(4.35f,1.0f,-5.27f),V(.42f,.35f,.012f),"Cream",false);
            Ellipsoid("Notice drawing pin",V(4.35f,1.15f,-5.29f),V(.025f,.025f,.01f),"Brass");
            session.busTimetable=new GameObject("Bus timetable interaction").transform;
            session.busTimetable.position=V(3.25f,.1f,-6.25f);
        }

        static void Crate(Vector3 origin,float width,float depth,string produce,int type)
        {
            Box("Crate bottom",origin+V(0,.035f,0),V(width,.07f,depth),"WoodLight");
            for(int level=0;level<3;level++)
            {
                float y=.09f+level*.08f;
                foreach(float z in new[]{-depth*.5f,depth*.5f}) Box("Crate slat",origin+V(0,y,z),V(width+.04f,.055f,.035f),"WoodLight",false);
                foreach(float x in new[]{-width*.5f,width*.5f}) Box("Crate slat",origin+V(x,y,0),V(.035f,.055f,depth),"WoodLight",false);
            }
            for(int x=0;x<4;x++) for(int z=0;z<3;z++)
            {
                Vector3 p=origin+V(-width*.34f+x*width*.22f,.18f+R(0,.025f),-depth*.3f+z*depth*.28f);
                Ellipsoid("Fresh "+produce,p,type==2?V(.13f,.12f,.24f):type==3?V(.105f,.10f,.21f):V(.13f,.115f,.13f),produce);
                if(type==0) Ellipsoid("Tomato stem",p+Vector3.up*.065f,V(.04f,.016f,.04f),"Leaf");
            }
        }
        static void CanvasAwning()
        {
            var vertices=new List<Vector3>(); var triangles=new List<int>();
            const int columns=24,rows=12;
            for(int z=0;z<=rows;z++) for(int x=0;x<=columns;x++)
            {
                float u=x/(float)columns,v=z/(float)rows;
                float sag=Mathf.Sin(u*Mathf.PI)*.09f+Mathf.Cos(v*Mathf.PI*2)*.035f;
                vertices.Add(V(5.5f+5.3f*u,3.25f-Mathf.Abs(v-.5f)*.72f-sag,9.15f+3.8f*v));
            }
            for(int z=0;z<rows;z++) for(int x=0;x<columns;x++)
            {
                int a=z*(columns+1)+x,b=a+1,c=a+columns+1,d=c+1;
                triangles.AddRange(new[]{a,c,b,b,c,d});
            }
            var mesh=new Mesh{name="Market canvas"}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
            var obj=new GameObject("Sagging canvas awning",typeof(MeshFilter),typeof(MeshRenderer)); obj.transform.SetParent(environment);
            obj.GetComponent<MeshFilter>().sharedMesh=Save(mesh,Root+"/Generated/Meshes/MarketCanvas.asset");
            obj.GetComponent<Renderer>().sharedMaterial=mats["MarketCanvas"];
            Beam("Canvas front seam",V(5.5f,2.89f,9.15f),V(10.8f,2.89f,9.15f),.022f,"WoodLight");
        }
    }
}
