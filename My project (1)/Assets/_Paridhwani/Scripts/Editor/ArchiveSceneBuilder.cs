using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddArchiveChapter(TeaStallSession s) {
 // Replace the solid silhouette with an enterable room, keeping its roof and plinth.
 foreach(string name in new[]{"Library silhouette plaster walls","Library silhouette door","Library silhouette damp lower wall"}) {
 var old=GameObject.Find(name); if(old) Object.DestroyImmediate(old);
 }
 Box("Library west wall",V(-8.2f,1.7f,16),V(.18f,3.1f,5.3f),"Plaster");
 Box("Library east wall",V(-1.4f,1.7f,16),V(.18f,3.1f,5.3f),"Plaster");
 Box("Library north wall",V(-4.8f,1.7f,18.56f),V(7,3.1f,.18f),"Plaster");
 Box("Library front left",V(-7.15f,1.7f,13.4f),V(2.3f,3.1f,.18f),"Plaster");
 Box("Library front right",V(-2.95f,1.7f,13.4f),V(3.1f,3.1f,.18f),"Plaster");
 Box("Library door lintel",V(-5.25f,2.9f,13.4f),V(1.5f,.7f,.18f),"Wood");
 s.archiveDoor=Box("Library master key door",V(-5.25f,1.3f,13.38f),V(1.35f,2.3f,.12f),"Wood");
 Box("Library threshold step",V(-5.25f,.13f,13),V(1.5f,.26f,.6f),"Stone");
 Box("Library catalogue desk",V(-4.5f,.75f,16.7f),V(1.8f,1,.8f),"Wood");
 Box("Open catalogue",V(-4.5f,1.28f,16.7f),V(.65f,.04f,.45f),"Cream",false);
 for(int i=0;i<2;i++) {
 float x=-7.3f+i*1.6f;
 Box("Archive cupboard",V(x,1.1f,18.1f),V(1.2f,1.6f,.55f),"Wood");
 for(int j=0;j<3;j++) Box("Archive shelf",V(x,.6f+j*.45f,17.78f),V(1.1f,.06f,.12f),"WoodLight");
 Text(i==0?"01 / LOCAL HISTORY":"02 / LAND / 1952",V(x,1.6f,17.79f),.04f,false,0);
 }
 var fill=new GameObject("Archive bounced window light").AddComponent<Light>(); fill.type=LightType.Point; fill.color=Hex("CAD0BB"); fill.range=8; fill.intensity=2; fill.transform.position=V(-4.8f,2.6f,15.2f);
 s.archiveDesk=new GameObject("Archive catalogue anchor").transform; s.archiveDesk.position=V(-4.5f,.4f,16.1f);
 Text("CATALOGUE / LAND RECORDS",V(-4.5f,1.5f,17.3f),.05f,false,0);
 }
 }
}

