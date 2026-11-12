using UnityEngine;
namespace Paridhwani {
 public enum ReplayChapter {Arrival,Library,Mess,Market,River,Temple,Archive,Panchayat,Checkpost,Ridge,Farm,Deed,Return,Council,Epilogue}
 public sealed class ChapterReplayNavigation {
 public string OriginMode {get;private set;}="pause";
 public string BackLabel => OriginMode=="main-menu"?"Back to title":OriginMode=="credits"?"Back to credits":"Back to Pause";
 public void OpenFrom(string mode) {
 if(mode=="chapter-replay")return;
 OriginMode=mode=="main-menu" || mode=="credits"?mode:"pause";
 }
 }
 public static class ChapterReplay {
 public static string Name(ReplayChapter chapter)=>VillageStory.ChapterName(chapter);
 public static bool IsUnlocked(TeaProgress progress,ReplayChapter chapter)=>VillageStory.Reached(progress,chapter);
 public static TeaProgress Seed(ReplayChapter chapter,TeaProgress preferences)=>VillageStory.PrepareChapter(chapter,preferences);
 }
 public sealed partial class TeaStallSession {
 public string PracticeChapterName {get;private set;}="Arrival and tea";
 public void StartChapterPractice(ReplayChapter chapter) {
 if(IsPracticeVisit || !ChapterReplay.IsUnlocked(Progress,chapter))return;
 Save();if(!LastSaveSucceeded)return;completedStoryStore=saves;
 presentationDemo=false;var next=ChapterReplay.Seed(chapter,Progress);PracticeChapterName=ChapterReplay.Name(chapter);Progress=next;conversation.ResetVisit();
 saves=new TeaSaveStore(System.IO.Path.Combine(VillageTestStorage.Cache,"paridhwani-practice-"+System.Guid.NewGuid().ToString("N")+".json"));
 EndForJourney();ResetActorsToOrigins();player.Restore(Progress.position,Progress.heading);returnTrail.Reset(Progress.position);ApplyState();Save();ui.ShowPracticeArrival();SetCursor(true);
 }
 }
}
