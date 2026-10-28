using System;
using Paridhwani;
public static class DemoPresentationChecks {
 public static void Run(Action<bool,string> check) {
 check(PresentationPolicy.Brightness(float.NaN)==PresentationPolicy.DefaultBrightness && PresentationPolicy.Brightness(float.PositiveInfinity)==PresentationPolicy.DefaultBrightness,"invalid display exposure has a finite default");
 check(PresentationPolicy.Brightness(-9)==-.5f && PresentationPolicy.Brightness(9)==1.5f,"display exposure is bounded");
 check(PresentationPolicy.Brightness(.35f)==.35f,"valid display exposure is preserved");
 var main=new TeaProgress {brightness=.35f,muted=true,reducedMotion=true,english=false};
 var copy=new TeaProgress();PresentationPolicy.Copy(main,copy);
 check(copy.brightness==.35f && copy.muted && copy.reducedMotion && !copy.english,"display settings travel with existing presentation preferences");
 check(new TeaProgress().brightness==PresentationPolicy.DefaultBrightness,"new visits use the presentation brightness default");
 check(PresentationDemo.Chapters.Count==4,"presentation offers four scoped demo scenes");
 var seen=new System.Collections.Generic.HashSet<ReplayChapter>();
 foreach(var chapter in PresentationDemo.Chapters) {
 check(seen.Add(chapter) && PresentationDemo.Contains(chapter),"demo scenes have unique recognised chapter IDs");
 check(!string.IsNullOrWhiteSpace(PresentationDemo.Description(chapter)),"each demo scene explains what it demonstrates");
 var demo=ChapterReplay.Seed(chapter,main);
 check(demo.brightness==main.brightness && demo.muted && demo.reducedMotion,"demo keeps the presenter's accessibility and display preferences");
 check(demo.arrivalAcknowledged && TeaSaveStore.SupportedPosition(demo.position),"demo begins at a supported prepared spawn");
 check(!demo.storyCompleted && !demo.libraryProtected,"demo does not pre-complete the ending");
 }
 check(!PresentationDemo.Contains((ReplayChapter)(-1)) && !PresentationDemo.Contains((ReplayChapter)99) && !PresentationDemo.Contains(ReplayChapter.Farm),"unlisted and invalid demo scene IDs are rejected");
 check(!main.arrivalAcknowledged && !main.greeted && !main.deedRetrieved && !ChapterReplay.IsUnlocked(main,ReplayChapter.Council),"creating demo snapshots cannot grant main-story progress or replay unlocks");
 var river=ChapterReplay.Seed(ReplayChapter.River,main);
 check(river.riverReportKnown && !river.giriAccountKnown && !river.bookingSlipFound,"river demo leaves observation and inference activities playable");
 var deed=ChapterReplay.Seed(ReplayChapter.Deed,main);
 check(deed.trunkLocationKnown && !deed.trunkOpened && !deed.deedRetrieved,"deed demo leaves the lock and original deed playable");
 var council=ChapterReplay.Seed(ReplayChapter.Council,main);
 check(FinaleInvestigation.CanPresent(council) && !council.workSuspended,"council demo supplies evidence without granting the review outcome");
 }
}
