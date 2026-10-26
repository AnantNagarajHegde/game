using System;
using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani {
 public static class ReturnInvestigation {
 public static bool CanReturn(TeaProgress p,bool routeChecked) => p.deedRetrieved && p.deedConceptsReviewed && p.ananyaFound && p.disappearanceExplained && p.ridgeCrossed && CheckpostInvestigation.CanPermit(p,routeChecked);
 public static bool Begin(TeaProgress p,bool routeChecked) { if(!CanReturn(p,routeChecked)) return false; p.returnJourneyStarted=true; return true; }
 public static bool Complete(TeaProgress p,bool routeChecked,bool atVillage) { if(!p.returnJourneyStarted || !CanReturn(p,routeChecked) || !atVillage) return false; p.returnedWithAnanya=true; return true; }
 public static bool Friday(TeaProgress p) { if(!p.returnedWithAnanya || !p.deedRetrieved || !p.deedConceptsReviewed) return false; p.fridayFinaleStarted=true; return true; }
 }
 public sealed class ReturnConversation : ITeaConversationProvider {
 public TeaTurn Start(TeaProgress p) {
 if(p.fridayFinaleStarted) return T("friday","The community is gathering at the library court. Keep the original ready for review.",C("ಸರಿ.","Sari.","Let's prepare.","close"));
 if(p.returnedWithAnanya) return T("rest","We have returned safely with the protected original. Ready to rest and meet the council at the library court on Friday morning?",C("ನಾಳೆ ಬೆಳಿಗ್ಗೆ.","Naale beligge.","Review Friday morning preparations.","rest",VillageActivity.FridayPreparation),C("ಇನ್ನೂ ಬೇಡ.","Innu beda.","Not yet.","close"));
 if(p.returnJourneyStarted) return T("walking","Follow the same marked ridge to Kencha, then take Raghavendra's auto back to town. You may also skip the familiar return when ready.",C("ಹಿಂತಿರುಗೋಣ.","Hintirugona.","Review return options.","walking",VillageActivity.ReturnTogether),C("ಸರಿ.","Sari.","Continue on foot.","close"));
 return T("prepare","The original is protected. Let's return together by the checked ridge. We can walk the familiar route, or skip that already explored journey. Neither choice changes the evidence or outcome.",C("ಒಟ್ಟಿಗೆ ಹೋಗೋಣ.","Ottige hogona.","Review our return journey.","prepare",VillageActivity.ReturnTogether),C("ಇನ್ನೂ ಬೇಡ.","Innu beda.","I want to look around first.","close"));
 }
 static TeaChoice C(string k,string r,string e,string next,VillageActivity a=VillageActivity.None) => new TeaChoice(k,r,e,next){Activity=a};
 static TeaTurn T(string id,string en,params TeaChoice[] c) => new TeaTurn(id,"ಒಟ್ಟಿಗೆ ಹೋಗೋಣ.","Ottige hogona.",en,c){Speaker="Ananya"};
 public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) { if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); return null; }
 }
 public sealed partial class TeaStallSession {
 public Transform returnCompanion,returnWait,fridayArrival,fridayCompanion;
 readonly Queue<Vector3> returnSteps=new Queue<Vector3>(); Vector3 lastReturnStep;
 void ApplyReturnState() {
 if(!returnCompanion) return;
 var body=returnCompanion.GetComponent<Collider>(); if(body) body.enabled=!Progress.returnJourneyStarted || Progress.returnedWithAnanya;
 if(Progress.returnedWithAnanya) { var anchor=Progress.fridayFinaleStarted?fridayCompanion:returnWait; if(anchor) { returnCompanion.position=anchor.position; returnCompanion.rotation=anchor.rotation; } }
 else if(Progress.returnJourneyStarted && Vector3.Distance(returnCompanion.position,player.transform.position)>12) { returnCompanion.position=player.transform.position; returnSteps.Clear(); lastReturnStep=player.transform.position; }
 }
 void UpdateReturnJourney() {
 if(!returnCompanion || !Progress.returnJourneyStarted || Progress.returnedWithAnanya) return;
 if(ui.IsModal) return;
 if(Vector3.Distance(lastReturnStep,player.transform.position)>.65f) { lastReturnStep=player.transform.position; returnSteps.Enqueue(lastReturnStep); }
 if(returnSteps.Count>2) {
 var target=returnSteps.Peek();
 if(Vector3.Distance(returnCompanion.position,player.transform.position)>10) returnCompanion.position=target;
 var direction=target-returnCompanion.position;
 if(direction.sqrMagnitude>.01f) returnCompanion.rotation=Quaternion.Slerp(returnCompanion.rotation,Quaternion.LookRotation(new Vector3(direction.x,0,direction.z)),Time.deltaTime*6);
 returnCompanion.position=Vector3.MoveTowards(returnCompanion.position,target,4*Time.deltaTime);
 if(Vector3.Distance(returnCompanion.position,target)<.12f) returnSteps.Dequeue();
 }
 if(villageArrival && Vector3.Distance(player.transform.position,villageArrival.position)<4) CompleteReturnAtVillage();
 }
 void CompleteReturnAtVillage() {
 if(ReturnInvestigation.Complete(Progress,ridgeRouteChecked,!AtCheckpost)) { ApplyState(); Save(); }
 }
 void EndForJourney() { ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation(); ui.HideModal(); SetCursor(false); }
 public void StartReturnWalk() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.ReturnTogether || !ReturnInvestigation.Begin(Progress,ridgeRouteChecked)) return;
 returnSteps.Clear(); lastReturnStep=player.transform.position; EndForJourney(); ApplyState(); Save();
 }
 public void SkipReturnJourney() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.ReturnTogether || !villageArrival || !ReturnInvestigation.Begin(Progress,ridgeRouteChecked)) return;
 EndForJourney(); player.Restore(villageArrival.position,villageArrival.eulerAngles.y); CompleteReturnAtVillage(); ui.ShowTravelArrival(false); SetCursor(true);
 }
 public void ConfirmFriday() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.FridayPreparation || !fridayArrival || !ReturnInvestigation.Friday(Progress)) return;
 EndForJourney(); player.Restore(fridayArrival.position,fridayArrival.eulerAngles.y); ApplyState(); Save(); ui.ShowFridayArrival(); SetCursor(true);
 }
 }
}
