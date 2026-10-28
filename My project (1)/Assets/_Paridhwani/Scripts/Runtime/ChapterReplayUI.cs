using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 readonly ChapterReplayNavigation replayNavigation=new ChapterReplayNavigation();
 public void ShowChapterReplay() {
 replayNavigation.OpenFrom(Mode);
 var shell=Card("chapter-replay","paper");shell.name="chapter-replay-card";shell.style.height=Length.Percent(85);
 Add(shell,"SEPARATE PRACTICE / CHAPTER SELECT","eyebrow").style.flexShrink=0;
 var back=Button(shell,replayNavigation.BackLabel+" [Esc]",session.Close,"quiet-button");back.name="chapter-replay-back";back.style.flexShrink=0;FocusLater(back);
 var card=MenuScroll(shell,"chapter-replay-scroll");Add(card,"Revisit a chapter","title");
 Add(card,"Choose a chapter you have reached. A separate practice save supplies its earlier clues. Your main visit is preserved; you can return to it from Pause.","body");
 if(session.IsPracticeVisit) Button(card,"Return to the main visit",session.ReturnToCompletedStory);
 else for(int i=0;i<=14;i++) {var chapter=(ReplayChapter)i;var b=Button(card,ChapterReplay.Name(chapter),()=>session.StartChapterPractice(chapter));b.SetEnabled(ChapterReplay.IsUnlocked(session.Progress,chapter));}
 }
 public void CloseChapterReplay() {
 string launcher;
 if(replayNavigation.OriginMode=="main-menu"){ShowMainMenu();launcher="title-practice";}
 else if(replayNavigation.OriginMode=="credits"){ShowCredits();launcher="credits-practice";}
 else {ShowPause();launcher="pause-practice";}
 FocusLater(root.Q<Button>(launcher));
 }
 }
}
