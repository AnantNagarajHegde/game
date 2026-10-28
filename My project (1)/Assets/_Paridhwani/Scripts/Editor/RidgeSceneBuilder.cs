using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void AddRidgeChapter(TeaStallSession s)
        {
            Mat("AnanyaMustard","A48C46",.97f,0,40); Mat("KamalammaCloth","817168",.98f,0,35);
            Box("Ridge earth bank",V(47,-.15f,25),V(8,.3f,24),"Earth");
            Box("Ridge first path",V(48,.02f,18),V(2.6f,.04f,7),"Lane");
            Box("Ridge left bend",V(46.8f,.02f,21.5f),V(5,.04f,2.8f),"Lane");
            Box("Ridge sheltered path",V(46,.02f,25.5f),V(2.6f,.04f,8),"Lane");
            Box("Ridge second bend",V(47,.02f,30),V(5,.04f,2.8f),"Lane");
            Box("Ridge farm approach",V(48,.02f,34),V(2.6f,.04f,7),"Lane");
            Rail(V(44.5f,0,25),V(.12f,1,24)); Rail(V(49.5f,0,25),V(.12f,1,24));
            Rail(V(48.4f,0,23),V(2.1f,1,.12f)); Rail(V(45.5f,0,29),V(2,1,.12f));
            Ellipsoid("Ridge direction stone",V(46.2f,.4f,18),V(.75f,.85f,.65f),"Stone");
            Text("GUDDA MANE / LEFT",V(47.8f,1.3f,19),.055f,false,0);
            s.ridgeMarker=Anchor("Ridge marker interaction",V(48,.1f,18));
            Box("Ridge shelter back",V(44.9f,1.1f,25.5f),V(.15f,2.2f,3),"Wood");
            Roof("Ridge resting shelter",46,25.5f,3.5f,3,4);
            Bench(V(46,.02f,26));
            for(int i=0;i<5;i++) Beam("Cleared storm branch",V(44.1f,.15f+i*.08f,20+i*.3f),V(43.5f,.3f+i*.08f,22+i*.25f),.08f,"Trunk");
            for(int i=0;i<10;i++) {
                Ellipsoid("Ridge wet rock",V(43.6f,.2f,15+i*2),V(1,.8f,1.5f),"StoneDark");
                Ellipsoid("Ridge outside planting",V(50.2f,.5f,15+i*2),V(1.1f,1.5f,1.6f),"Leaf");
            }
            s.farmGate=Box("Farm route comprehension gate",V(47,.65f,34),V(5,1.3f,.14f),"Wood");
            Text("GUDDA MANE / FOOTPATH",V(48,1.6f,34),.05f,false,0);
            Box("Farm garden earth",V(48,-.15f,43),V(14,.3f,16),"Earth");
            Box("Farm stepping path",V(48,.02f,38),V(2.6f,.04f,6),"Lane");
            foreach(float x in new[]{41f,55f}) Rail(V(x,0,43),V(.12f,1,16));
            Rail(V(48,0,51),V(14,1,.12f)); Rail(V(43.5f,0,35),V(5,1,.12f)); Rail(V(52.5f,0,35),V(5,1,.12f));
            s.farmArrival=Anchor("Gudda Mane arrival",V(48,.1f,38.5f));
            Box("Farmhouse stone plinth",V(48,.06f,45),V(10,.12f,9),"Stone");
            Box("Farmhouse rear wall",V(48,1.7f,49.3f),V(10,3.2f,.18f),"Plaster");
            foreach(float x in new[]{43.1f,52.9f}) Box("Farmhouse side wall",V(x,1.7f,46),V(.18f,3.2f,6.6f),"Plaster");
            Box("Farmhouse front left",V(44.8f,1.7f,43),V(3.5f,3.2f,.18f),"Plaster");
            Box("Farmhouse front right",V(50.8f,1.7f,43),V(4.3f,3.2f,.18f),"Plaster");
            Box("Farmhouse lintel",V(47.5f,2.9f,43),V(2,.8f,.18f),"Wood");
            Roof("Gudda Mane tiled roof",48,45,5,11,10);
            foreach(float x in new[]{43.5f,47.9f,52.5f}) Box("Farmhouse veranda post",V(x,1.6f,40.5f),V(.18f,3.1f,.18f),"Wood");
            Bench(V(50,.13f,42.3f));
            Box("Farmhouse hospitality table",V(44.5f,.6f,41.8f),V(1.2f,.9f,.6f),"Wood");
            Cylinder("Kashaya pot",V(44.5f,1.2f,41.8f),.15f,.3f,"Steel");
            Cylinder("Water cup",V(44.2f,1.1f,41.7f),.06f,.14f,"Steel");
            Box("Ananya travel bag",V(46.2f,.4f,42.6f),V(.5f,.55f,.3f),"Green");
            Ellipsoid("Brass bag bell",V(46.45f,.6f,42.4f),V(.08f,.09f,.08f),"Brass");
            Box("Closed family tin trunk",V(50.4f,.45f,47.8f),V(1.2f,.65f,.7f),"Steel");
            Box("Trunk brass clasp",V(50.4f,.5f,47.42f),V(.12f,.18f,.04f),"Brass",false);
            Text("FAMILY ARCHIVE",V(50.4f,1.5f,49.17f),.05f,false,0);
            var fill=new GameObject("Farm veranda bounced daylight").AddComponent<Light>(); fill.type=LightType.Point; fill.intensity=2; fill.range=9; fill.color=Hex("CAD0BB"); fill.transform.position=V(48,2.6f,41.5f);
            var all=new List<VillageNpc>(s.villagers);
            var ananya=AddJourneyNpc(s,all,"Ananya",V(46,.13f,41.1f),VillageNpcRole.Ananya,"GUDDA MANE VERANDA");
            var elder=AddJourneyNpc(s,all,"Kamalamma",V(50,.13f,41.1f),VillageNpcRole.Kamalamma,"FAMILY ARCHIVE");
            s.villagers=all.ToArray();
        }
    }
}
