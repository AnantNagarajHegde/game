using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void AddFinaleChapter(TeaStallSession s) {
 s.finaleSet=new GameObject("Friday library court staging");
 var all=new List<VillageNpc>(s.villagers); var president=AddJourneyNpc(s,all,"Council president",V(-2.2f,.1f,9.8f),VillageNpcRole.CouncilPresident,"LIBRARY COURT / COUNCIL"); president.transform.SetParent(s.finaleSet.transform,true); s.councilPresident=president.transform; s.villagers=all.ToArray();
 var pending=Box("Clearance pending notice",V(.2f,1.6f,12.5f),V(2.1f,.6f,.08f),"Sign",false); pending.transform.SetParent(s.finaleSet.transform,true);
 var pt=GameObject.Find("Shaped sign: CLEARANCE PENDING");
 Text("CLEARANCE PENDING",V(.2f,1.6f,12.44f),.06f,false,0); pt=GameObject.Find("Shaped sign: CLEARANCE PENDING"); if(pt) pt.transform.SetParent(pending.transform,true); s.worksPendingMarker=pending;
 var suspended=Box("Council suspension notice",V(.2f,1.6f,12.5f),V(2.1f,.6f,.08f),"Sign",false); suspended.transform.SetParent(s.finaleSet.transform,true);
 Text("WORKS SUSPENDED / REVIEW",V(.2f,1.6f,12.44f),.05f,false,0); var st=GameObject.Find("Shaped sign: WORKS SUSPENDED / REVIEW"); if(st) st.transform.SetParent(suspended.transform,true); s.worksSuspendedMarker=suspended;
 var vehicle=new GameObject("Idle works vehicle").transform; vehicle.SetParent(s.finaleSet.transform); vehicle.position=V(11.4f,.1f,13.2f);
 Box("Works vehicle body",V(0,.9f,0),V(1.7f,1.1f,3.1f),"Cream",true,vehicle); Box("Works cab",V(0,1.6f,-.8f),V(1.6f,.8f,1.1f),"Green",true,vehicle);
 foreach(float x in new[]{-.8f,.8f}) foreach(float z in new[]{-1f,1f}) {var wheel=Cylinder("Works tyre",V(x,.4f,z),.35f,.16f,"Tyre",false,vehicle);wheel.transform.localRotation=Quaternion.Euler(0,0,90);}
 s.finaleCast=new Transform[3]; s.finaleCastPositions=new Transform[3];
 var roles=new[]{VillageNpcRole.Librarian,VillageNpcRole.PanchayatClerk,VillageNpcRole.MessOwner}; var positions=new[]{V(-4.6f,.1f,11.3f),V(5.5f,.1f,12.7f),V(4.6f,.1f,7.5f)};
 for(int i=0;i<roles.Length;i++) {foreach(var n in s.villagers) if(n.role==roles[i]) s.finaleCast[i]=n.transform; s.finaleCastPositions[i]=Anchor("Friday community position "+i,positions[i]);s.finaleCastPositions[i].rotation=Quaternion.Euler(0,180,0);}
 }
 }
}
