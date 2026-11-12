using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowRestoreError(bool missing) {
 var card=Card("restore-failed","paper");Add(card,"CURRENT VISIT KEPT","eyebrow");Add(card,"Could not restore the saved visit","title");
 Add(card,missing?"No saved visit or backup was found. Your current story progress and position are still open.":"Neither the saved visit nor its backup could be read. Your current story progress and position are still open. The unreadable files will be preserved before any new save.","body");
 Button(card,"Return to the current visit",session.Close);
 Button(card,"Back to Pause",ShowPause,"quiet-button");
 }

 public void ShowWorldUpdateRequired() {
 var card=Card("world-update-required","paper");Add(card,"This world needs regeneration","title");
 Add(card,"The scene was generated from an older world recipe. Your saved visit has not been opened or changed. In Unity, use Paridhwani > Create Tea Stall Benchmark, or recreate the additive package, then press Play again.","body");
 Button(card,"Close this play session",session.RequestQuit);
 }
 public void ShowMainMenu(string feedback="") {
 bool saved=session.HasSavedVisit;
 var shell=Card("main-menu","paper");shell.name="title-card";Class(shell,"title-card");shell.style.height=Length.Percent(85);
 var card=MenuScroll(shell,"title-scroll");
 Add(card,"A KANNADA STORY ADVENTURE","eyebrow");
 Add(card,"PARIDHWANI","brand");
 var rule=new VisualElement();Class(rule,"title-rule");card.Add(rule);
 Add(card,"Every conversation\nbrings you closer.","title");
 Add(card,"Follow a missing archivist's trail through Kalyanapura, one Kannada conversation at a time.","body");
 if(feedback!="")Add(card,feedback,"feedback");
 var resume=Button(card,session.IsPresentationDemo?"Continue presentation demo":session.IsPracticeVisit?"Continue practice":"Continue visit",session.ContinueMainVisit);resume.name="title-continue";resume.SetEnabled(saved);
 if(!saved)resume.style.display=DisplayStyle.None;
 Button start=null;
 if(session.IsPracticeVisit)Button(card,"Return to the main visit",session.ReturnToCompletedStory);
 else {
 start=Button(card,saved?"Begin a new story":"Begin your visit",()=>{if(session.HasSavedVisit)ShowNewVisitConfirmation();else session.BeginNewMainVisit();});start.name="title-start";
 }
 Class(saved?resume:start??resume,"primary-button");
 var footer=new VisualElement();Class(footer,"title-actions");shell.Add(footer);
 Button(footer,"Settings",session.OpenTitleSettings,"quiet-button").name="title-settings";
 if(!session.IsPracticeVisit)Button(footer,"Chapter practice",ShowChapterReplay,"quiet-button").name="title-practice";
 if(!session.IsPracticeVisit)Button(footer,"Scene demo",ShowPresentationDemo,"quiet-button").name="title-demo";
 Button(footer,"Save & exit",session.RequestQuit,"quiet-button").name="title-exit";
 Add(shell,"KALYANAPURA  /  KARNATAKA","title-location");
 FocusLater(saved?resume:start);
 }
 public void ShowNewVisitConfirmation() {
 var card=Card("new-visit-confirm","paper");Add(card,"BEGIN AGAIN","eyebrow");Add(card,"Start a new main story?","title");
 Add(card,"Your current main visit will be archived before the new one is saved. Accessibility and display preferences are kept. Use chapter practice if you only want to revisit part of the story.","body");
 Button(card,"Archive this visit and begin",session.BeginNewMainVisit);Button(card,"Keep my current visit",()=>ShowMainMenu(),"quiet-button");
 }
 public void ShowQuitSaveError() {
 var shell=Card("quit-save-error","paper");shell.name="quit-save-card";shell.style.height=UnityEngine.UIElements.Length.Percent(85);
 Add(shell,"VISIT STILL OPEN","eyebrow").style.flexShrink=0;
 var back=Button(shell,session.QuitCancelLabel,session.CancelQuit,"quiet-button");back.name="quit-cancel";back.style.flexShrink=0;FocusLater(back);
 var card=MenuScroll(shell,"quit-save-scroll");
 Add(card,"Your latest changes could not be saved","title");
 Add(card,"Retry saving before leaving, return to the previous screen, or leave without the latest changes.","body");
 Add(card,session.SaveMessage,"feedback");
 Button(card,"Retry save and exit",session.RequestQuit).name="quit-retry";
 Button(card,"Leave without saving",session.QuitWithoutSaving,"quiet-button").name="quit-without-saving";
 }
 }
}
