using System;
using System.Collections.Generic;
using UnityEngine;
namespace Paridhwani {
 public enum StoryEnding { None,Stay,Depart }
 public static class EpilogueInvestigation {
 public static bool Review(TeaProgress p) {
 if(!p.workSuspended || !FinaleInvestigation.CanPresent(p) || !p.deedPresented || !p.finalePurposePresented || !p.finaleRestrictionPresented || !p.finaleBeneficiaryPresented) return false;
 p.reviewOutcomeKnown=true; p.libraryProtected=true; p.epilogueStarted=true; return true;
 }
 public static bool Choose(TeaProgress p,StoryEnding ending) {
 if(!p.reviewOutcomeKnown || !p.libraryProtected || !p.epilogueStarted || p.storyCompleted || (ending!=StoryEnding.Stay && ending!=StoryEnding.Depart)) return false;
 p.endingChoice=ending; return true;
 }
 public static bool Complete(TeaProgress p) {
 if(!p.reviewOutcomeKnown || !p.libraryProtected || !p.epilogueStarted || (p.endingChoice!=StoryEnding.Stay && p.endingChoice!=StoryEnding.Depart)) return false;
 p.storyCompleted=true; return true;
 }
 }
 public sealed class EpilogueConversation : ITeaConversationProvider {
 readonly string name; public EpilogueConversation(string name) {this.name=name;}
 public TeaTurn Start(TeaProgress p) => !p.libraryProtected || !p.reviewOutcomeKnown ? new TeaTurn("pending","ನಮಸ್ಕಾರ.","Namaskara.","The later review outcome is still pending.",new TeaChoice("ಸರಿ.","Sari.","All right.","close")){Speaker=name} : new TeaTurn("epilogue","ಮತ್ತೆ ಬನ್ನಿ.","Matte banni.",p.storyCompleted?(p.endingChoice==StoryEnding.Stay?"The archive work continues. There are more stories to catalogue together.":"You will always have a place here. Come back to the library whenever you return."):"After reviewing the original, the council confirmed protection for the community library. The shelves are open again. Will you stay to help archive, or depart with an invitation to return?",new TeaChoice("ಮುಂದೆ ಹೋಗೋಣ.","Munde hogona.",p.storyCompleted?"Revisit the ending and phrase recap.":"Reflect on what comes next.","epilogue"){Activity=VillageActivity.EndingReflection},new TeaChoice("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")){Speaker=name};
 public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) {if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i));return null;}
 }
 public sealed partial class TeaStallSession {
 public GameObject epilogueSet;
 readonly Dictionary<Transform,Vector3> actorOrigins=new Dictionary<Transform,Vector3>();
 readonly Dictionary<Transform,Quaternion> actorRotations=new Dictionary<Transform,Quaternion>();
 TeaSaveStore completedStoryStore;
 public bool IsPracticeVisit => completedStoryStore!=null;
 void CaptureActorOrigins() {foreach(var n in villagers) if(n) {actorOrigins[n.transform]=n.transform.position;actorRotations[n.transform]=n.transform.rotation;}}
 void ResetActorsToOrigins() {foreach(var entry in actorOrigins) if(entry.Key) {entry.Key.position=entry.Value;entry.Key.rotation=actorRotations[entry.Key];}}
 void ApplyEpilogueState() {if(epilogueSet) epilogueSet.SetActive(Progress.epilogueStarted);}
 public void ConfirmReviewOutcome() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.ReviewOutcome || !EpilogueInvestigation.Review(Progress)) return;
 EndForJourney();ResetActorsToOrigins(); if(fridayArrival) player.Restore(fridayArrival.position,fridayArrival.eulerAngles.y);ApplyState();Save();ui.ShowReviewArrival();SetCursor(true);
 }
 public void ChooseEnding(StoryEnding ending) {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.EndingReflection || !EpilogueInvestigation.Choose(Progress,ending)) return;
 Save();ui.ShowEndingReflection();
 }
 public void FinishStory() {
 if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.EndingReflection || !EpilogueInvestigation.Complete(Progress)) return;
 EndForJourney();ApplyState();Save();ui.ShowCredits();SetCursor(true);
 }
 public void StartPracticeVisit() {if(Progress.storyCompleted)StartChapterPractice(ReplayChapter.Arrival);}
 public void ReturnToCompletedStory() {
 if(!IsPracticeVisit) return;
 Save();saves=completedStoryStore;completedStoryStore=null;ResetActorsToOrigins();Load();
 }
 }
}
