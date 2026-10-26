using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddDeedChapter(TeaStallSession s) {
 var old=GameObject.Find("Closed family tin trunk"); if(old) Object.DestroyImmediate(old);
 Box("Family trunk base",V(50.4f,.4f,47.8f),V(1.2f,.55f,.7f),"Steel");
 s.trunkLid=Anchor("Trunk hinged lid",V(50.4f,.7f,48.15f)); Box("Trunk lid",V(0,0,-.35f),V(1.25f,.08f,.75f),"Steel",false,s.trunkLid);
 s.deedPaper=Box("Original deed in trunk",V(50.4f,.69f,47.8f),V(.55f,.015f,.4f),"Cream",false);
 s.trunkAnchor=Anchor("Family trunk interaction",V(50.4f,.13f,47)); s.trunkView=Anchor("Trunk inspection camera",V(50.4f,1.75f,45.9f)); s.trunkLookTarget=Anchor("Trunk target",V(50.4f,.5f,47.8f));
 Box("Protective cloth folder",V(49.5f,.3f,47.8f),V(.5f,.05f,.4f),"Green",false);
 }
 }
}
