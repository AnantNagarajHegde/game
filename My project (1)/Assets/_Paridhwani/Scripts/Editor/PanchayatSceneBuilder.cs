using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void AddPanchayatChapter(TeaStallSession s)
        {
            Box("Panchayat stone floor",V(5,.04f,21.2f),V(5,.08f,4),"StoneDark");
            Box("Panchayat rear wall",V(5,1.7f,23.1f),V(5,3.2f,.18f),"Plaster");
            Box("Panchayat damp plinth",V(5,.4f,22.99f),V(5,.5f,.035f),"Damp",false);
            Box("Panchayat east wall",V(7.4f,1.7f,21.2f),V(.18f,3.2f,4),"Plaster");
            Roof("Panchayat public veranda roof",5,21.2f,4.2f,5.6f,4.8f);
            foreach(float x in new[]{2.7f,7.3f}) Box("Panchayat veranda post",V(x,1.65f,19.3f),V(.16f,3.3f,.16f),"Wood");
            Box("Panchayat public counter",V(5,.6f,20.3f),V(3.4f,1.2f,.65f),"Wood");
            Box("Panchayat counter top",V(5,1.25f,20.3f),V(3.5f,.1f,.75f),"WoodLight");
            for(int i=0;i<4;i++) Box("Office file stack",V(6.1f,1.35f+i*.055f,20.3f),V(.55f,.05f,.4f),i%2==0?"Cream":"Green",false);
            Cylinder("Office stamp",V(4.4f,1.4f,20.3f),.06f,.2f,"Wood");
            Box("Warehouse plan roll",V(5.7f,1.5f,22.96f),V(1,.6f,.025f),"Cream",false);
            Text("PROPOSED / WAREHOUSE",V(5.7f,1.5f,22.93f),.035f,false,0);
            Box("Panchayat office sign",V(5,2.8f,19.23f),V(3,.45f,.06f),"Sign",false);
            Text("PANCHAYAT / PUBLIC COUNTER",V(5,2.8f,19.18f),.065f,false,0);
            Box("Public notice board",V(2.95f,1.5f,20.2f),V(.9f,1.3f,.09f),"Wood");
            Box("Clearance notice paper",V(2.95f,1.5f,20.14f),V(.7f,1,.02f),"Cream",false);
            Text("LIBRARY / FRI 09:00",V(2.95f,1.6f,20.11f),.04f,false,0);
            s.panchayatNotice=new GameObject("Panchayat notice interaction anchor").transform; s.panchayatNotice.position=V(2.95f,.1f,19.8f);
            var a=Character("Shresthi",V(5,.08f,21.4f),false); a.transform.rotation=Quaternion.Euler(0,180,0);
            var c=a.GetComponentInChildren<VillageCharacter>(); c.lookTarget=s.player.transform;
            var n=a.AddComponent<VillageNpc>(); n.role=VillageNpcRole.PanchayatClerk; n.displayName="Shresthi"; n.roleLabel="PANCHAYAT COUNTER"; n.eyes=c.head;
            var all=new List<VillageNpc>(s.villagers); all.Add(n); s.villagers=all.ToArray();
        }
    }
}
