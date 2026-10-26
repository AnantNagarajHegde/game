namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddReturnChapter(TeaStallSession s) {
 foreach(var n in s.villagers) if(n.role==VillageNpcRole.Ananya) s.returnCompanion=n.transform;
 s.returnWait=Anchor("Ananya village return wait",V(5.5f,.1f,-3.5f));
 s.fridayArrival=Anchor("Friday library court arrival",V(1.3f,.1f,9));
 s.fridayCompanion=Anchor("Ananya library court",V(2.7f,.1f,10.4f));
 s.returnWait.rotation=s.fridayCompanion.rotation=UnityEngine.Quaternion.Euler(0,180,0);
 }
 }
}
