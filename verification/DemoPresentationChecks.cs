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
 static void RainChecks(Action<bool,string> check) {
 void RunFor(RainShelterMix mix,bool roof,bool audible,float gain,int frames,float dt=1f/60) {for(int i=0;i<frames;i++)mix.Step(roof,audible,gain,dt);}
 bool Near(float actual,float expected)=>Math.Abs(actual-expected)<.0001f;
 var outdoor=new RainShelterMix();RunFor(outdoor,false,true,1,120);
 check(Near(outdoor.RainGain,.34f)&&Near(outdoor.EaveGain,.03f),"outdoor rain retains authored settled levels");
 RunFor(outdoor,true,true,1,6);check(!outdoor.Sheltered,"brief overhead obstruction cannot switch to shelter");
 RunFor(outdoor,false,true,1,2);RunFor(outdoor,true,true,1,6);check(!outdoor.Sheltered,"roof confirmation requires continuous cover");
 RunFor(outdoor,true,true,1,120);check(outdoor.Sheltered&&Near(outdoor.RainGain,.13f)&&Near(outdoor.EaveGain,.16f),"sustained roof transitions to muffled rain and eaves");
 RunFor(outdoor,false,true,1,12);check(outdoor.Sheltered,"brief tiled-eave gap retains shelter");
 RunFor(outdoor,false,true,1,120);check(!outdoor.Sheltered&&Near(outdoor.RainGain,.34f)&&Near(outdoor.EaveGain,.03f),"leaving shelter restores outdoor mix");
 var flicker=new RainShelterMix();bool continuous=true;
 for(int i=0;i<240;i++){float old=flicker.RainGain;flicker.Step(i%2==0,true,1,1f/60);continuous&=Math.Abs(flicker.RainGain-old)<.025f&&!flicker.Sheltered;}
 check(continuous,"alternating roof-edge samples neither pump shelter state nor pop rain gain");
 outdoor.Step(false,false,1,1f/60);check(outdoor.RainGain==0&&outdoor.EaveGain==0,"mute/focus loss silences both channels immediately");
 RunFor(outdoor,true,false,1,120);check(outdoor.Sheltered&&outdoor.RainGain==0,"shelter detection continues while inaudible");
 outdoor.Step(true,true,1,1f/60);check(outdoor.RainGain>0&&outdoor.RainGain<.01f,"focus return fades in under the current roof");
 RunFor(outdoor,true,true,.25f,120);check(Near(outdoor.RainGain,.13f*.25f)&&Near(outdoor.EaveGain,.16f*.25f),"ambience slider scales both sheltered channels");
 outdoor.Step(true,true,0,0);check(outdoor.RainGain==0&&outdoor.EaveGain==0,"zero ambience silences without waiting for another frame");
 RunFor(outdoor,true,true,1,120);outdoor.Silence();check(outdoor.RainGain==0&&outdoor.EaveGain==0,"disable/listener loss can reset audibility immediately");
 outdoor.Step(true,true,1,1f/60);check(outdoor.RainGain<.01f,"re-enable does not restore stale full gain");
 var slow=new RainShelterMix();var fast=new RainShelterMix();RunFor(slow,true,true,1,15,1f/30);RunFor(fast,true,true,1,60,1f/120);
 check(Math.Abs(slow.RainGain-fast.RainGain)<.015f&&Math.Abs(slow.EaveGain-fast.EaveGain)<.015f,"30 and 120 Hz transitions agree within one probe frame");
 var invalid=new RainShelterMix();invalid.Step(true,true,float.NaN,float.PositiveInfinity);
 check(invalid.RainGain==0&&invalid.EaveGain==0&&!invalid.Sheltered,"non-finite time/gain cannot contaminate audio or shelter state");
 invalid.Step(true,true,1,-1);check(invalid.RainGain==0&&!invalid.Sheltered,"negative time cannot advance ambience");
 invalid.Step(true,true,1,60);check(!invalid.Sheltered&&invalid.RainGain<=.06801f,"long frame is bounded instead of snapping to a full new mix");
 }
 public static void Run(Action<bool,string> check) {
 RainChecks(check);
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
 var demo=PresentationDemo.Seed(chapter,main);
 check(demo.brightness==main.brightness && demo.muted && demo.reducedMotion,"demo keeps the presenter's accessibility and display preferences");
 check(demo.arrivalAcknowledged && TeaSaveStore.SupportedPosition(demo.position),"demo begins at a supported prepared spawn");
 check(!demo.storyCompleted && !demo.libraryProtected,"demo does not pre-complete the ending");
 }
 check(!PresentationDemo.Contains((ReplayChapter)(-1)) && !PresentationDemo.Contains((ReplayChapter)99) && !PresentationDemo.Contains(ReplayChapter.Farm),"unlisted and invalid demo scene IDs are rejected");
 check(!main.arrivalAcknowledged && !main.greeted && !main.deedRetrieved && !ChapterReplay.IsUnlocked(main,ReplayChapter.Council),"creating demo snapshots cannot grant main-story progress or replay unlocks");
 var river=PresentationDemo.Seed(ReplayChapter.River,main);
 check(river.riverReportKnown && !river.giriAccountKnown && !river.bookingSlipFound,"river demo leaves observation and inference activities playable");
 var deed=PresentationDemo.Seed(ReplayChapter.Deed,main);
 check(deed.trunkLocationKnown && !deed.trunkOpened && !deed.deedRetrieved,"deed demo leaves the lock and original deed playable");
 var council=PresentationDemo.Seed(ReplayChapter.Council,main);
 check(FinaleInvestigation.CanPresent(council) && !council.workSuspended,"council demo supplies evidence without granting the review outcome");
 }
}
