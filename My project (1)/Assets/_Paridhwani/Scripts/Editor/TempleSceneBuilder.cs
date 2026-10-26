using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddTempleChapter(TeaStallSession s) {
 Box("Northern earth",V(0,-.15f,25),V(18,.3f,15),"Earth");
 Box("Northern lane",V(1,.02f,25),V(3,.04f,15),"Lane");
 Box("Temple public forecourt",V(-4,.04f,27),V(7,.08f,9),"StoneDark");
 Box("Temple facade",V(-5,1.7f,30.7f),V(5,3.2f,.25f),"Plaster");
 Box("Temple door",V(-5,1.25f,30.5f),V(1.25f,2.2f,.12f),"Wood");
 Roof("Temple tiled shelter",-5,29.1f,4,6,4.4f);
 foreach(float x in new[]{-7.6f,-2.4f}) Box("Temple timber post",V(x,1.65f,27.2f),V(.17f,3.3f,.17f),"Wood");
 for(int i=0;i<3;i++) Box("Low temple step",V(-5,.06f+i*.06f,28+i*.4f),V(4,.12f+i*.12f,1.6f-i*.3f),"Stone");
 Bench(V(4,.05f,27));
 Box("Northern stop board",V(3.8f,1.9f,29.8f),V(2.1f,1.1f,.1f),"Sign");
 Text("NORTHERN STOP",V(3.8f,2.1f,29.72f),.085f,false,0);
 Text("ROUTE 14 / 06:45",V(3.8f,1.75f,29.72f),.065f,false,0);
 Box("Flower table",V(-2.8f,.55f,26),V(1.3f,1.1f,.65f),"Wood");
 for(int i=0;i<18;i++) Ellipsoid("Flower bundle",V(-3.25f+(i%6)*.16f,1.14f,25.8f+(i/6)*.15f),V(.12f,.06f,.1f),"Cream");
 var all=new List<VillageNpc>(s.villagers);
 foreach(bool flowers in new[]{false,true}) {
 var a=Character(flowers?"Rathnamma":"Gurumurthy",V(flowers?-1.7f:-5.6f,.09f,26),false,flowers); a.transform.rotation=Quaternion.Euler(0,180,0);
 var c=a.GetComponentInChildren<VillageCharacter>(); c.lookTarget=s.player.transform;
 var n=a.AddComponent<VillageNpc>(); n.displayName=flowers?"Rathnamma":"Gurumurthy"; n.role=flowers?VillageNpcRole.FlowerSeller:VillageNpcRole.TempleKeeper; n.roleLabel="TEMPLE / NORTHERN STOP"; n.eyes=c.head; all.Add(n);
 }
 s.villagers=all.ToArray();
 foreach(float x in new[]{-9f,8f}) Box("Northern boundary wall",V(x,.6f,25),V(.3f,1.2f,15),"Stone");
 Box("Northern end wall",V(0,.6f,32.5f),V(18,1.2f,.3f),"Stone");
 Text("TEMPLE / NORTH STOP",V(2,1.3f,17),.06f,false,0);
 }
 }
}
