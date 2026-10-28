using System;
using Paridhwani;
public static class DemoPresentationChecks {
 static void SignChecks(Action<bool,string> check) {
 var wide=new VillageSignLayout(4.1f,.225f,false);
 check(Math.Abs((double)wide.Width/wide.Height-4.1/.225)<.2,"long English boards retain their physical aspect ratio");
 var square=new VillageSignLayout(.5f,.5f,true);
 check(square.Width==square.Height,"square sign textures keep square text proportions");
 var mirrored=new VillageSignLayout(-4.1f,-.225f,false);
 check(mirrored.Width==wide.Width && mirrored.Height==wide.Height,"mirrored boards have the same text resolution");
 foreach(var dimensions in new[]{new[]{0f,0f},new[]{float.NaN,float.PositiveInfinity},new[]{1000f,.001f},new[]{.001f,1000f},new[]{1f,.2f}}) {
 var layout=new VillageSignLayout(dimensions[0],dimensions[1],true);
 check(layout.Width>=32 && layout.Width<=2048 && layout.Height>=64 && layout.Height<=512 && layout.Width%8==0 && layout.Height%8==0,"sign dimensions are bounded and aligned");
 check(float.IsFinite(layout.FontSize) && layout.FontSize>0 && layout.Padding>0 && layout.Padding*2<layout.Height,"sign text has finite size and positive padding");
 }
 check(VillageSignLayout.Fit(100,100,50,200,100)==100,"fitting text is not enlarged or repeatedly shrunk");
 float fitted=VillageSignLayout.Fit(100,800,200,400,100);
 check(fitted>0 && fitted<=50,"oversized labels shrink to fit both dimensions");
 check(VillageSignLayout.Fit(100,0,0,400,100)==100,"unready font measurements retain the current size");
 check(VillageSignLayout.Fit(100,float.NaN,20,400,100)==100 && VillageSignLayout.Fit(100,20,20,float.NaN,100)==100,"invalid shaping measurements cannot produce a non-finite font size");
 check(VillageSignLayout.Fit(float.NaN,20,20,400,100)==1,"invalid current font size has a safe fallback");
 }
 public static void Run(Action<bool,string> check) {
 SignChecks(check);
 check(PresentationPolicy.Brightness(float.NaN)==PresentationPolicy.DefaultBrightness && PresentationPolicy.Brightness(float.PositiveInfinity)==PresentationPolicy.DefaultBrightness,"invalid display exposure has a finite default");
 check(PresentationPolicy.Brightness(-9)==-.5f && PresentationPolicy.Brightness(9)==1.5f,"display exposure is bounded");
 check(PresentationPolicy.Brightness(.35f)==.35f,"valid display exposure is preserved");
 var main=new TeaProgress {brightness=.35f,muted=true,reducedMotion=true,english=false};
 var copy=new TeaProgress();PresentationPolicy.Copy(main,copy);
 check(copy.brightness==.35f && copy.muted && copy.reducedMotion && !copy.english,"display settings travel with existing presentation preferences");
 check(new TeaProgress().brightness==PresentationPolicy.DefaultBrightness,"new visits use the presentation brightness default");
 check(PresentationDemo.Chapters.Count==4,"presentation offers four scoped demo scenes");
 check(PresentationDemo.Route((ReplayChapter)99)=="","unknown scenes do not receive fabricated directions");
 var seen=new System.Collections.Generic.HashSet<ReplayChapter>();
 foreach(var chapter in PresentationDemo.Chapters) {
 check(seen.Add(chapter) && PresentationDemo.Contains(chapter),"demo scenes have unique recognised chapter IDs");
 check(!string.IsNullOrWhiteSpace(PresentationDemo.Route(chapter)) && PresentationDemo.Route(chapter).Contains("1.") && PresentationDemo.Route(chapter).Contains("4."),"each demo scene has a four-step presenter route");
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
