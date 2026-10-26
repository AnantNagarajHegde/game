using System;
using UnityEngine;
namespace Paridhwani {
 public static class FinaleInvestigation {
 public static bool CanPresent(TeaProgress p) => p.fridayFinaleStarted && p.returnedWithAnanya && p.deedRetrieved && p.deedConceptsReviewed;
 public static bool Answer(TeaProgress p,int answer,bool nearCouncil) {
 if(!CanPresent(p) || !nearCouncil) return false;
 if(!p.deedPresented) { if(answer!=0) return false; p.deedPresented=true; }
 else if(!p.finalePurposePresented) { if(answer!=1) return false; p.finalePurposePresented=true; }
 else if(!p.finaleRestrictionPresented) { if(answer!=0) return false; p.finaleRestrictionPresented=true; }
 else if(!p.finaleBeneficiaryPresented) { if(answer!=2) return false; p.finaleBeneficiaryPresented=true; }
 return true;
 }
 public static bool Decide(TeaProgress p,bool nearCouncil) {
 if(!CanPresent(p) || !nearCouncil || !p.deedPresented || !p.finalePurposePresented || !p.finaleRestrictionPresented || !p.finaleBeneficiaryPresented) return false;
 p.workSuspended=true; return true;
 }
 }
 public sealed class CouncilConversation : ITeaConversationProvider,IActivityConversationProvider {
 public TeaTurn Start(TeaProgress p) => !FinaleInvestigation.CanPresent(p)?T("early","Bring the reviewed original and return together before the council meeting.",C("ಸರಿ.","Sari.","All right.","close")):p.workSuspended?T("decision","The original and your explanation warrant review. I am suspending clearance work while the council examines the records. This is an interim decision; protection is not yet finally confirmed.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. We will await the review.","close")):T("evidence","We have the clearance notice and the catalogue. Show the original, then explain its purpose, restriction and beneficiary. You may consult your notebook or ask Ananya for help.",C("ಮೂಲ ಪತ್ರವನ್ನು ನೋಡಿ.","Moola patravannu nodi.","Please review the original deed.","evidence",VillageActivity.CouncilEvidence),C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will review my notes first.","close"));
 static TeaTurn T(string id,string en,params TeaChoice[] c) => new TeaTurn(id,"ಪತ್ರವನ್ನು ಪರಿಶೀಲಿಸೋಣ.","Patravannu parisheelisona.",en,c){Speaker="Council president"};
 static TeaChoice C(string k,string r,string e,string next,VillageActivity a=VillageActivity.None) => new TeaChoice(k,r,e,next){Activity=a};
 public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) { if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); return null; }
 public TeaTurn ResumeAfterActivity(TeaTurn t,TeaProgress p) => Start(p);
 }
 public sealed class FinaleCommunityConversation : ITeaConversationProvider {
 readonly string name; public FinaleCommunityConversation(string name) { this.name=name; }
 public TeaTurn Start(TeaProgress p) => new TeaTurn("community","ನಾವು ಒಟ್ಟಿಗೆ ಇದ್ದೇವೆ.","Naavu ottige iddeve.",p.workSuspended?"The works are suspended while the council reviews the original. We will wait for the formal outcome together.":"The original is here and the community is listening. Present it to the council president. Your notes and English or Roman support remain available.",new TeaChoice("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")){Speaker=name};
 public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) { if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); return null; }
 }
 public sealed partial class TeaStallSession {
 public Transform councilPresident;
 public GameObject finaleSet,worksPendingMarker,worksSuspendedMarker;
 public Transform[] finaleCast,finaleCastPositions;
 public bool AtCouncil => councilPresident && Vector3.Distance(player.transform.position,councilPresident.position)<3.5f;
 public bool FinaleHelp {get;set;}
 void ApplyFinaleState() {
 if(finaleSet) finaleSet.SetActive(Progress.fridayFinaleStarted);
 if(worksPendingMarker) worksPendingMarker.SetActive(Progress.fridayFinaleStarted && !Progress.workSuspended);
 if(worksSuspendedMarker) worksSuspendedMarker.SetActive(Progress.workSuspended);
 if(Progress.fridayFinaleStarted && finaleCast!=null && finaleCastPositions!=null) for(int i=0;i<Math.Min(finaleCast.Length,finaleCastPositions.Length);i++) if(finaleCast[i] && finaleCastPositions[i]) { finaleCast[i].position=finaleCastPositions[i].position; finaleCast[i].rotation=finaleCastPositions[i].rotation; }
 }
 public void RequestCouncilDecision() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.CouncilEvidence) return;
 bool ok=FinaleInvestigation.Decide(Progress,AtCouncil); if(ok) {activityChanged=true;ApplyState();Save();}
 ui.ShowCouncilActivity(ok?"Clearance suspended pending council review. Final protection will be confirmed only after that review.":"Present the original and all three clauses to the council president first.");
 }
 }
}
