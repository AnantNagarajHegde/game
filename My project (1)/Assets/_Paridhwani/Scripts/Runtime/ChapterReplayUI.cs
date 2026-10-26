using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowChapterReplay() {
 var card=Card("chapter-replay","paper");card.style.height=Length.Percent(85);Add(card,"SEPARATE PRACTICE / CHAPTER SELECT","eyebrow");Add(card,"Revisit a chapter","title");
 Add(card,"Choose a chapter you have reached. A separate practice save supplies its earlier clues. Your main visit is preserved; you can return to it from Pause.","body");
 var scroll=new ScrollView();Class(scroll,"evidence-scroll");card.Add(scroll);
 if(session.IsPracticeVisit) Button(scroll,"Return to the main visit",session.ReturnToCompletedStory);
 else for(int i=0;i<=14;i++) {var chapter=(ReplayChapter)i;var b=Button(scroll,ChapterReplay.Name(chapter),()=>session.StartChapterPractice(chapter));b.SetEnabled(ChapterReplay.IsUnlocked(session.Progress,chapter));}
 Button(card,"Back to Pause",ShowPause,"quiet-button");
 }
 }
}
