using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddCheckpostChapter(TeaStallSession s) {
 Mat("GuardKhaki","817E63",.97f,0,41);
 Box("Checkpost clearing",V(55,-.15f,4),V(24,.3f,26),"Earth");
 Box("Forest approach road",V(60,.02f,-1),V(4,.04f,14),"Lane");
 Box("Ridge approach path",V(48,.02f,9),V(2.6f,.04f,12),"Lane");
 foreach(float x in new[]{43f,67f}) Rail(V(x,0,4),V(.15f,1,26));
 foreach(float z in new[]{-9f,17f}) Rail(V(55,0,z),V(24,1,.15f));
 Box("Checkpost shelter floor",V(51,.05f,4),V(5,.1f,4.5f),"StoneDark");
 Box("Checkpost damp wall",V(51,1.5f,6.1f),V(5,2.8f,.18f),"Plaster");
 Roof("Forest checkpost roof",51,4,4,5.8f,5.2f);
 foreach(float x in new[]{48.7f,53.3f}) Box("Checkpost shelter post",V(x,1.6f,1.9f),V(.16f,3.2f,.16f),"Wood");
 Bench(V(49.8f,.1f,5.2f));
 Box("Raincoat hanging at shelter",V(52.7f,1.65f,5.95f),V(.55f,1,.06f),"GuardKhaki",false);
 Box("Kencha field desk",V(51.5f,.63f,5.5f),V(1.3f,1.05f,.65f),"Wood");
 Box("Checkpost route sketch",V(51.5f,1.18f,5.5f),V(.6f,.03f,.4f),"Cream",false);
 var light=new GameObject("Checkpost bounced daylight").AddComponent<Light>(); light.type=LightType.Point; light.range=7; light.intensity=1.5f; light.color=Hex("CAD0BB"); light.transform.position=V(51,2.6f,3);
 Box("Checkpost closure board",V(55,1.4f,1.2f),V(1.8f,1.3f,.12f),"Wood");
 Text("ROAD CLOSED / RAIN",V(55,1.65f,1.12f),.065f,false,0);
 Text("ASK KENCHA / RIDGE LEFT",V(55,1.2f,1.12f),.05f,false,0);
 s.checkpostBoard=Anchor("Closure board interaction",V(55,.1f,.2f));
 Rail(V(60.5f,0,5),V(6.5f,1,.18f));
 Rail(V(57.3f,0,11),V(.18f,1,12)); Rail(V(63.7f,0,11),V(.18f,1,12));
 Box("Collapsed culvert shadow",V(60.5f,.025f,9),V(5.8f,.05f,4.8f),"StoneDark",false);
 foreach(float z in new[]{6.1f,11.9f}) Box("Broken road slab",V(60.5f,.12f,z),V(5.6f,.22f,1),"Stone");
 for(int i=0;i<9;i++) Ellipsoid("Washout rubble",V(58.2f+i*.55f,.12f,8+R(-.8f,.8f)),V(.65f,.3f,.8f),"StoneDark");
 Rail(V(46.5f,0,11),V(.12f,1,8)); Rail(V(49.5f,0,11),V(.12f,1,8));
 s.ridgeEntrance=Anchor("Ridge trailhead interaction",V(48,.1f,7));
 s.ridgeGate=Box("Ridge permission gate",V(48,.65f,8.6f),V(2.8f,1.3f,.15f),"Wood"); s.ridgeRouteChecked=true;
 Ellipsoid("Ridge stone marker",V(46.9f,.4f,7),V(.8f,.9f,.65f),"Stone");
 Text("RIDGE / KEEP LEFT",V(48,1.7f,8.6f),.055f,false,0);
 Box("Prototype ridge end fence",V(48,.65f,14.5f),V(2.8f,1.3f,.15f),"Wood");
 Text("ONWARD RIDGE / NEXT CHAPTER",V(48,1.8f,14.5f),.05f,false,0);
 for(int i=0;i<14;i++) {
 float x=i%2==0?44.5f:65.5f,z=-7+i*1.6f;
 Cylinder("Forest trunk",V(x,2.2f,z),.13f,4.4f,"Trunk",true);
 Ellipsoid("Forest canopy",V(x,4.7f,z),V(3,3.5f,3.2f),i%2==0?"Leaf":"LeafLight");
 Ellipsoid("Red bank planting",V(x,.3f,z),V(.7f,.8f,.8f),"Leaf"); }
 s.checkpostArrival=Anchor("Checkpost travel arrival",V(55,.15f,-5)); s.villageArrival=Anchor("Village auto arrival",V(6.5f,.15f,-4.7f));
 var all=new List<VillageNpc>(s.villagers);
 AddJourneyNpc(s,all,"Raghavendra",V(7.5f,.1f,-3.5f),VillageNpcRole.AutoDriver,"AUTO PICKUP");
 AddJourneyNpc(s,all,"Raghavendra",V(57,.1f,-3),VillageNpcRole.CheckpostDriver,"RETURN TO VILLAGE");
 var kencha=AddJourneyNpc(s,all,"Kencha",V(51,.1f,3.7f),VillageNpcRole.ForestGuard,"FOREST CHECKPOST");
 foreach(var r in kencha.GetComponentsInChildren<Renderer>()) if(r.name=="Work shirt") r.sharedMaterial=mats["GuardKhaki"];
 s.villagers=all.ToArray(); AutoRickshaw(V(10,.05f,-3)); AutoRickshaw(V(61,.05f,-4));
 }
 static Transform Anchor(string name,Vector3 p) { var t=new GameObject(name).transform; t.position=p; return t; }
 static GameObject AddJourneyNpc(TeaStallSession s,List<VillageNpc> all,string name,Vector3 p,VillageNpcRole role,string label) {
 var a=Character(name,p,false); a.transform.rotation=Quaternion.Euler(0,180,0);
 var c=a.GetComponentInChildren<VillageCharacter>(); c.lookTarget=s.player.transform;
 var n=a.AddComponent<VillageNpc>(); n.role=role; n.displayName=name; n.roleLabel=label; n.eyes=c.head; all.Add(n); return a;
 }
 static void AutoRickshaw(Vector3 p) {
 var root=new GameObject("Parked auto rickshaw").transform; root.SetParent(environment); root.position=p;
 Box("Auto body",V(0,.7f,0),V(1.4f,.7f,2.4f),"Green",true,root);
 Box("Auto canopy",V(0,1.8f,.25f),V(1.5f,.14f,2),"Tyre",true,root);
 Box("Auto rear seat",V(0,1.05f,.6f),V(1.2f,.25f,.6f),"Tyre",false,root);
 foreach(float x in new[]{-.65f,.65f}) Beam("Auto canopy upright",V(x,.9f,.9f),V(x,1.8f,.9f),.04f,"Steel",root);
 foreach(var w in new[]{V(0,.35f,-1.05f),V(-.7f,.35f,.75f),V(.7f,.35f,.75f)}) { var t=Cylinder("Auto wheel",w,.3f,.15f,"Tyre",false,root); t.transform.localRotation=Quaternion.Euler(0,0,90); }
 }
 }
}
