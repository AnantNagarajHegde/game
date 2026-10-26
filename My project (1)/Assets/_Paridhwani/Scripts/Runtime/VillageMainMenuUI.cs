using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowWorldUpdateRequired() {
 var card=Card("world-update-required","paper");Add(card,"This world needs regeneration","title");
 Add(card,"The scene was generated from an older world recipe. Your saved visit has not been opened or changed. In Unity, use Paridhwani > Create Tea Stall Benchmark, or recreate the additive package, then press Play again.","body");
 Button(card,"Close this play session",session.RequestQuit);
 }
 public void ShowMainMenu(string feedback="") {
 var shell=Card("main-menu","paper");shell.style.height=Length.Percent(85);var card=new ScrollView();Class(card,"evidence-scroll");shell.Add(card);Add(card,"P A R I D H W A N I","brand");Add(card,"A language. A place. A way to belong.","title");
 Add(card,"Arrive in Kalyanapura, follow Ananya's trail and learn Kannada through the people and places along the way.","body");
 if(feedback!="")Add(card,feedback,"feedback");
 var resume=Button(card,session.IsPracticeVisit?"Continue practice":"Continue visit",session.ContinueMainVisit);resume.SetEnabled(session.HasSavedVisit);
 if(session.IsPracticeVisit)Button(card,"Return to the main visit",session.ReturnToCompletedStory);
 else {Button(card,"Begin a new story",()=>{if(session.HasSavedVisit)ShowNewVisitConfirmation();else session.BeginNewMainVisit();});Button(card,"Practise a reached chapter",ShowChapterReplay,"quiet-button");}
 Button(card,"Settings",session.OpenTitleSettings,"quiet-button");Button(card,"Save and return to desktop",session.RequestQuit,"quiet-button");
 }
 public void ShowNewVisitConfirmation() {
 var card=Card("new-visit-confirm","paper");Add(card,"BEGIN AGAIN","eyebrow");Add(card,"Start a new main story?","title");
 Add(card,"Your current main visit will be archived before the new one is saved. Accessibility and display preferences are kept. Use chapter practice if you only want to revisit part of the story.","body");
 Button(card,"Archive this visit and begin",session.BeginNewMainVisit);Button(card,"Keep my current visit",()=>ShowMainMenu(),"quiet-button");
 }
 public void ShowQuitSaveError() {
 var card=Card("quit-save-error","paper");Add(card,"Your latest changes could not be saved","title");Add(card,"You can return to the visit and try saving again, or leave without these latest changes.","body");Button(card,"Return to the visit",session.ContinueMainVisit);Button(card,"Leave without saving",session.QuitWithoutSaving,"quiet-button");
 }
 }
}
