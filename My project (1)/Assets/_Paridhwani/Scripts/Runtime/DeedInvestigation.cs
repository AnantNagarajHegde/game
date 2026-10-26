namespace Paridhwani {
 public static class DeedInvestigation {
 public static string AdjustCode(string code,int index,int delta) {
 if(index<0 || index>3) return code;
 if(code==null || code.Length!=4) code="0000";
 foreach(char digit in code) if(digit<'0' || digit>'9') {code="0000";break;}
 var chars=code.ToCharArray();chars[index]=(char)('0'+((chars[index]-'0'+delta%10+10)%10));return new string(chars);
 }
 public static bool CanInspect(TeaProgress p) => RidgeInvestigation.CanReunite(p) && p.disappearanceExplained && p.trunkLocationKnown;
 public static bool Unlock(TeaProgress p,string code,bool nearTrunk) { if(!CanInspect(p) || !nearTrunk || code!="1207") return false; p.trunkOpened=true; return true; }
 public static bool Answer(TeaProgress p,int answer) {
 if(!CanInspect(p) || !p.trunkOpened) return false;
 if(!p.deedPurposeKnown) { if(answer!=0) return false; p.deedPurposeKnown=true; }
 else if(!p.deedRestrictionKnown) { if(answer!=2) return false; p.deedRestrictionKnown=true; }
 else if(!p.deedBeneficiaryKnown) { if(answer!=1) return false; p.deedBeneficiaryKnown=true; }
 p.deedConceptsReviewed=p.deedPurposeKnown && p.deedRestrictionKnown && p.deedBeneficiaryKnown; return true;
 }
 public static bool Protect(TeaProgress p,bool nearTrunk) { if(!CanInspect(p) || !p.trunkOpened || !p.deedConceptsReviewed || !nearTrunk) return false; p.deedRetrieved=true; return true; }
 }
 public sealed partial class TeaStallSession {
 public UnityEngine.Transform trunkAnchor,trunkView,trunkLookTarget,trunkLid;
 public UnityEngine.GameObject deedPaper;
 public string TrunkAttempt {get;set;}="0000";
 bool NearTrunk => trunkAnchor && UnityEngine.Vector3.Distance(player.transform.position,trunkAnchor.position)<3;
 void ApplyDeedState() { if(trunkLid) trunkLid.localRotation=UnityEngine.Quaternion.Euler(Progress.trunkOpened?65:0,0,0); if(deedPaper) deedPaper.SetActive(Progress.trunkOpened && !Progress.deedRetrieved); }
 bool FindDeedInteraction() { if(nearbyNpc || !trunkAnchor || UnityEngine.Vector3.Distance(player.transform.position,trunkAnchor.position)>2.5f) return false; Nearby="Inspect the family trunk"; nearbyInspection=10; return true; }
 bool OpenDeedActivity(VillageActivity a) {
 if(a!=VillageActivity.TrunkLock && a!=VillageActivity.DeedReview) return false;
 if(trunkView && trunkLookTarget && NearTrunk) player.BeginInspection(trunkView,trunkLookTarget); else player.EndConversation(); ui.ShowDeedActivity(a); return true;
 }
 public void SubmitTrunkCode() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.TrunkLock) return;
 bool ok=DeedInvestigation.Unlock(Progress,TrunkAttempt,NearTrunk); if(ok) { activityChanged=true; ApplyState(); Save(); }
 ui.ShowDeedActivity(ActiveActivity,ok?"The lock opens. Review the paper before collecting it.":"Twelve months, then seven days: two digits each. You must be beside the trunk.");
 }
 public void ProtectDeed() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.DeedReview) return;
 bool ok=DeedInvestigation.Protect(Progress,NearTrunk); if(ok) { activityChanged=true; ApplyState(); Save(); }
 ui.ShowDeedActivity(ActiveActivity,ok?"Original placed in its protective folder. Prepare the return with Ananya.":"Review every clause, then return beside the trunk to protect the original.");
 }
 }
}
