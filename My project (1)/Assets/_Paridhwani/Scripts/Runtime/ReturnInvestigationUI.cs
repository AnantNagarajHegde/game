using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowReturnActivity(VillageActivity a) {
 var card=Card("evidence","paper"); Add(card,"ANANYA / RETURN TOGETHER","eyebrow"); Add(card,a==VillageActivity.FridayPreparation?"Prepare for Friday morning":"The familiar way home","title");
 if(a==VillageActivity.FridayPreparation) {
 Add(card,"Rest with the original safely protected, then gather at the library court on Friday morning before 09:00. This is an explicit story transition; reading and exploring do not consume the deadline.","body");
 if(session.Progress.returnedWithAnanya) Button(card,"Confirm rest and Friday morning",session.ConfirmFriday);
 } else {
 Add(card,"Walk back through the same sheltered bends to the checkpost. Ananya follows your path; Raghavendra takes both of you back to the village. Or skip the already explored return. Both choices preserve the same evidence and story state.","body");
 if(ReturnInvestigation.CanReturn(session.Progress,session.ridgeRouteChecked)) { Button(card,"Walk back with Ananya",session.StartReturnWalk); Button(card,"Skip the familiar return journey",session.SkipReturnJourney); }
 else Add(card,"Protect the reviewed original and confirm the route is checked before returning together.","body");
 }
 Button(card,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 }
 public void ShowFridayArrival() {
 var card=Card("friday-arrival","paper"); Add(card,"FRIDAY / BEFORE 09:00","eyebrow"); Add(card,"The library court","title"); Add(card,"Ananya and the protected original are with you. Present the evidence to the council; arrival alone does not stop the works.","body"); Button(card,"Step into the court",session.Close);
 }
 }
}
