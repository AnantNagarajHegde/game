using System;
using Paridhwani;
public static class ChapterChecks {
 static int checks;
 static void Check(bool ok,string why) { if(!ok) throw new Exception(why); checks++; }
 static void Witnesses(bool reverse) {
 var p=new TeaProgress(); var g=new TempleConversation(false); var r=new TempleConversation(true);
 Check(g.Start(p).Id=="early" && r.Start(p).Id=="early","early visitor gets no testimony");
 p.templeLeadKnown=true;
 var first=reverse?r:g; var second=reverse?g:r;
 var t=first.Start(p); t=first.Reply(t,1,p); Check(!p.gurumurthyAccountKnown && !p.rathnammaAccountKnown,"help grants no clue");
 t=first.Reply(t,0,p); first.Reply(t,0,p);
 Check(p.gurumurthyAccountKnown!=p.rathnammaAccountKnown,"sources independent");
 t=second.Start(p); t=second.Reply(t,0,p); t=second.Reply(t,0,p);
 Check(t.Id=="compare","both sources needed"); t=second.Reply(t,1,p);
 Check(!p.witnessAccountsCompared && !p.guddaFamilyLeadKnown,"incorrect inference blocked");
 t=second.Reply(t,0,p); t=second.Reply(t,0,p);
 Check(p.witnessAccountsCompared && !p.guddaFamilyLeadKnown,"comparison keeps family lead gated");
 t=r.Start(p); r.Reply(t,0,p); Check(p.guddaFamilyLeadKnown,"family lead earned from Rathnamma");
 var a=new ArchiveConversation(); t=a.Start(p); a.Reply(t,1,p); Check(!p.libraryAccessGranted,"declined access grants no key");
 t=a.Start(p); t=a.Reply(t,0,p); Check(p.libraryAccessGranted && !p.deedReferenceFound,"access does not grant deed reference");
 Check(!ArchiveInvestigation.FindShelf(p,0) && !ArchiveInvestigation.ReadReference(p,0),"wrong shelf and premature reading blocked");
 Check(ArchiveInvestigation.FindShelf(p,1),"second cupboard accepted"); Check(!ArchiveInvestigation.ReadReference(p,2),"legal protection inference blocked");
 Check(ArchiveInvestigation.ReadReference(p,0),"reference interpreted");
 t=a.ResumeAfterActivity(t,p); Check(t.Id=="rest" && !p.thursdayStarted,"activity resumes at rest offer"); a.Reply(t,1,p);
 Check(!p.thursdayStarted,"declining rest keeps day"); a.Reply(a.Start(p),0,p);
 Check(p.thursdayStarted && a.Start(p).Id=="morning","confirmed rest advances once");
 t=a.Start(p); Check(t.Choices[0].Activity==VillageActivity.ArchiveReference,"reference remains available after rest");
 }
 sealed class StorageCodec : ITeaProgressCodec {
 public string Encode(TeaProgress p) => "{\"version\":"+p.version+",\"sign\":"+(p.signRead?"true":"false")+"}";
 public TeaProgress Decode(string json) => new TeaProgress {version=json.Contains("\"version\":99")?99:1,signRead=json.Contains("\"sign\":true")};
 }
 static void FileChecks() {
 string directory=System.IO.Path.Combine(Environment.CurrentDirectory,"Temp","IterationVerification","ParidhwaniStore-"+Guid.NewGuid().ToString("N"));
 string file=System.IO.Path.Combine(directory,"visit.json");var codec=new StorageCodec();var store=new TeaSaveStore(file,codec);
 try {
 Check(!store.Read(out _).signRead,"new storage defaults");
 Check(store.Write(new TeaProgress {signRead=true},out _),"file write succeeds: "+store.LastFailureDetail);
 Check(store.Write(new TeaProgress(),out _),"second write creates backup");
 System.IO.File.Delete(file); Check(store.HasAnySaveFiles,"existing backup prevents legacy migration overwrite");Check(store.Read(out var recovered).signRead && recovered!=null,"missing primary recovers backup");
 Check(store.Write(new TeaProgress {signRead=true},out _),"recovered backup can be saved");
 System.IO.File.WriteAllText(file,"broken");Check(store.Read(out _).signRead,"corrupt primary recovers backup");
 Check(store.Write(new TeaProgress(),out _),"recovery writes fresh primary");
 System.IO.File.Delete(file);Check(store.Read(out _).signRead,"recovery did not overwrite the good backup with corrupt primary");
 System.IO.File.WriteAllText(file,"{\"version\":99}");System.IO.File.WriteAllText(file+".bak","{\"version\":99}");
 store.Read(out _);Check(store.Write(new TeaProgress(),out _),"unsupported files preserved before fresh write: "+store.LastFailureDetail);
 Check(System.IO.Directory.GetFiles(directory,"*.unreadable-*").Length>=2,"unsupported primary and backup retained");
 Check(store.Archive(new TeaProgress {signRead=true},out _) && System.IO.Directory.GetFiles(System.IO.Path.Combine(directory,"ArchivedVisits")).Length==1,"new-story archive preserves an explicit snapshot");
 Check(!System.IO.File.Exists(file+".tmp"),"atomic write leaves no temp file");
 } finally {if(System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory,true);}
 }
 static void AsyncDialogueChecks() {
 using(var requests=new DialogueTextRequests()) {
 Check(requests.Send(token=>System.Threading.Tasks.Task.FromResult(new TeaLine("","",new string('x',4097)))).GetAwaiter().GetResult().Status==DialogueTextStatus.Unavailable,"oversized external response rejected");
 var turn=new LocalTeaConversation().Start(new TeaProgress());var prompt=new DialoguePrompt("Manjunatha",turn,new string('x',600));prompt.AvailableReplies[0].English="changed";Check(turn.Choices[0].Line.English!="changed" && prompt.LearnerText.Length==512,"optional request is bounded and copies authored choices without effects");
 Check(requests.Send(null).GetAwaiter().GetResult().Status==DialogueTextStatus.Unavailable,"missing optional service falls back");
 Check(requests.Send(token=>System.Threading.Tasks.Task.FromResult(new TeaLine("","","A bounded reply"))).GetAwaiter().GetResult().Status==DialogueTextStatus.Ready,"optional text returns without quest effects");
 Check(requests.Send(token=>throw new InvalidOperationException()).GetAwaiter().GetResult().Status==DialogueTextStatus.Unavailable,"service failure falls back");
 var never=new System.Threading.Tasks.TaskCompletionSource<TeaLine>();Check(requests.Send(token=>never.Task,10).GetAwaiter().GetResult().Status==DialogueTextStatus.TimedOut && !requests.Busy,"ignored cancellation cannot trap a request");
 var late=new System.Threading.Tasks.TaskCompletionSource<TeaLine>();var old=requests.Send(token=>late.Task);requests.Cancel();late.SetResult(new TeaLine("","","Late reply"));Check(old.GetAwaiter().GetResult().Status==DialogueTextStatus.Superseded,"late replies after exit ignored");
 Check(requests.Send(token=>System.Threading.Tasks.Task.FromResult<TeaLine>(null)).GetAwaiter().GetResult().Status==DialogueTextStatus.Unavailable,"empty external text falls back");
 }
 }
 public static int Main() { FileChecks(); AsyncDialogueChecks();
 Check(PresentationPolicy.Volume(float.NaN)==1 && PresentationPolicy.Volume(-1)==0 && PresentationPolicy.Volume(4)==1,"audio preference limits");
 Check(PresentationPolicy.ValidQuality((VillageQuality)99)==VillageQuality.Balanced,"invalid quality defaults safely");
 var prefs=new TeaProgress {reducedMotion=true,invertY=true,masterVolume=.4f,quality=VillageQuality.Low};var copied=new TeaProgress();PresentationPolicy.Copy(prefs,copied);Check(copied.reducedMotion && copied.invertY && copied.masterVolume==.4f && copied.quality==VillageQuality.Low,"practice keeps accessibility preferences");
 Witnesses(false); Witnesses(true);
 var journey=new TeaProgress(); var driver=new TravelConversation(false); var guard=new CheckpostConversation(false);
 Check(driver.Start(journey).Id=="early" && !CheckpostInvestigation.CanTravel(journey),"early travel blocked");
 Check(!CheckpostInvestigation.Answer(journey,1,true),"public board alone grants no trail permission");
 journey.thursdayStarted=true; journey.deedRequired=true; journey.checkpostDestinationKnown=true;
 Check(CheckpostInvestigation.CanTravel(journey) && driver.Start(journey).Choices[0].Activity==VillageActivity.CheckpostTravel,"earned journey offers confirmation");
 driver.Reply(driver.Start(journey),1,journey); Check(!journey.checkpostVisited,"declined trip grants no arrival");
 Check(guard.Start(journey).Id=="early","guard requires arrival"); journey.checkpostVisited=true;
 var jt=guard.Start(journey); jt=guard.Reply(jt,1,journey); Check(!journey.checkpostAccountKnown,"language help grants no account");
 jt=guard.Reply(jt,0,journey); Check(journey.checkpostAccountKnown && journey.ananyaLocationReported && !journey.trailPermission,"account distinct from route permission");
 Check(!CheckpostInvestigation.Answer(journey,0,false) && !journey.trailInstructionsUnderstood,"unsafe road answer rejected");
 Check(CheckpostInvestigation.Answer(journey,1,false) && journey.trailInstructionsUnderstood && !journey.trailPermission,"correct language cannot clear an unchecked route");
 guard=new CheckpostConversation(true); jt=guard.Start(journey); guard.Reply(jt,1,journey);
 Check(journey.ridgeRouteOpen && journey.trailPermission,"physical clearance plus understood instructions grants entry");
 CheckpostInvestigation.RefreshPermission(journey,false); Check(!journey.trailPermission && !journey.ridgeRouteOpen,"route closure revokes permission despite saved facts");
 CheckpostInvestigation.RefreshPermission(journey,true); Check(journey.trailPermission,"reopening preserves understood directions");
 Check(new TravelConversation(true).Start(journey).Choices[0].Activity==VillageActivity.VillageReturn,"return journey retained");
 Check(TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(55,.15f,-5)),"checkpost arrival is saveable");
 Check(TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(-26,.15f,2)),"river saves retained");
 Check(!TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(30,0,0)),"empty gap between locations rejected");
 Check(!TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(float.NaN,0,0)),"invalid coordinates rejected");
 var lost=new TeaProgress(); var ananya=new FarmConversation(false); var kamalamma=new FarmConversation(true);
 Check(ananya.Start(lost).Id=="early" && kamalamma.Start(lost).Id=="early","no early reunion or trunk clue");
 Check(!RidgeInvestigation.Answer(lost,0,true,true) && !RidgeInvestigation.Arrive(lost,true,true),"ridge requires checkpost readiness");
 Check(!RidgeInvestigation.Answer(journey,2,true,true) && !journey.ridgeWayfindingKnown,"wrong path grants no waypoint");
 Check(!RidgeInvestigation.Answer(journey,0,true,false),"marker must be physically visited");
 Check(RidgeInvestigation.Answer(journey,0,true,true),"left path recalled at marker");
 Check(!RidgeInvestigation.Arrive(journey,true,false) && !journey.guddaManeReached,"correct directions alone do not grant arrival");
 Check(!RidgeInvestigation.Arrive(journey,false,true),"unchecked route cannot grant farm arrival");
 Check(RidgeInvestigation.Arrive(journey,true,true) && journey.ridgeCrossed && journey.guddaManeReached,"physical arrival completes ridge");
 Check(kamalamma.Start(journey).Id=="wait","elder does not reveal trunk before explanation");
 var ft=ananya.Start(journey); ft=ananya.Reply(ft,1,journey); Check(!journey.ananyaFound,"help does not record reunion");
 ft=ananya.Reply(ft,0,journey); Check(journey.ananyaFound && !journey.disappearanceExplained,"meeting distinct from hearing explanation");
 ananya.Reply(ft,2,journey); Check(ananya.Start(journey).Id=="explain" && !journey.disappearanceExplained,"early exit preserves unheard account");
 ft=ananya.Start(journey); ft=ananya.Reply(ft,1,journey); Check(!journey.disappearanceExplained,"repeat grants no unseen response");
 ft=ananya.Reply(ft,0,journey); Check(journey.disappearanceExplained && !journey.trunkLocationKnown,"account does not grant elder clue");
 ft=ananya.Reply(ft,2,journey); Check(journey.farmHospitalityKnown && ft.Id=="records","declining a drink is valid");
 ft=kamalamma.Start(journey); ft=kamalamma.Reply(ft,1,journey); Check(!journey.trunkLocationKnown,"elder help grants no trunk clue");
 kamalamma.Reply(ft,0,journey); Check(journey.trunkLocationKnown && kamalamma.Start(journey).Id=="elder_return","trunk clue earned and repeatable");
 for(int choice=0;choice<3;choice++) {
 var drink=new TeaProgress {ridgeCrossed=true,guddaManeReached=true,ridgeWayfindingKnown=true,ananyaFound=true,disappearanceExplained=true};
 ananya.Reply(ananya.Start(drink),choice,drink); Check(drink.farmHospitalityKnown,"kashaya, water and decline all accepted"); }
 Check(TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(48,.13f,41)),"farm position saveable");
 Check(!TeaSaveStore.SupportedPosition(new UnityEngine.Vector3(48,0,65)),"outside authored farm region rejected");
 var locked=new TeaProgress(); Check(!DeedInvestigation.Unlock(locked,"1207",true) && !DeedInvestigation.Answer(locked,0),"early lock/document gates");
 Check(!DeedInvestigation.Unlock(journey,"1207",false),"physical trunk proximity required");
 Check(!DeedInvestigation.Unlock(journey,"127",true) && !DeedInvestigation.Unlock(journey,"xxxx",true),"invalid lock input grants nothing");
 Check(DeedInvestigation.Unlock(journey,"1207",true) && !journey.deedRetrieved,"unlock does not collect original");
 Check(!DeedInvestigation.Answer(journey,1) && !journey.deedPurposeKnown,"wrong purpose rejected");
 Check(DeedInvestigation.Answer(journey,0) && !journey.deedConceptsReviewed,"purpose alone insufficient");
 Check(!DeedInvestigation.Protect(journey,true),"unreviewed deed cannot be collected");
 Check(!DeedInvestigation.Answer(journey,0) && DeedInvestigation.Answer(journey,2),"restriction retry");
 Check(!DeedInvestigation.Answer(journey,0) && DeedInvestigation.Answer(journey,1) && journey.deedConceptsReviewed,"beneficiary completes review");
 Check(!DeedInvestigation.Protect(journey,false) && DeedInvestigation.Protect(journey,true),"protect original at physical trunk");
 Check(DeedInvestigation.Unlock(journey,"1207",true) && journey.deedRetrieved,"repeat inspection preserves evidence");
 Check(!ReturnInvestigation.Begin(locked,true) && !ReturnInvestigation.Friday(locked),"no return or Friday before original");
 Check(!ReturnInvestigation.Begin(journey,false),"unchecked route prevents return preparation");
 Check(ReturnInvestigation.Begin(journey,true) && !journey.returnedWithAnanya,"begin return is not arrival");
 Check(!ReturnInvestigation.Complete(journey,true,false) && !ReturnInvestigation.Friday(journey),"no remote arrival or premature day transition");
 Check(ReturnInvestigation.Complete(journey,true,true) && journey.returnedWithAnanya && !journey.fridayFinaleStarted,"return completion does not consume day");
 Check(ReturnInvestigation.Friday(journey) && journey.fridayFinaleStarted,"explicit Friday after safe return");
 Check(ReturnInvestigation.Complete(journey,true,true) && journey.deedRetrieved,"return revisit preserves protected deed");
 var walk=new TeaProgress {deedRetrieved=true,deedConceptsReviewed=true,ananyaFound=true,disappearanceExplained=true,ridgeCrossed=true,thursdayStarted=true,deedRequired=true,checkpostDestinationKnown=true,checkpostVisited=true,checkpostAccountKnown=true,trailInstructionsUnderstood=true};
 var skip=new TeaProgress {deedRetrieved=true,deedConceptsReviewed=true,ananyaFound=true,disappearanceExplained=true,ridgeCrossed=true,thursdayStarted=true,deedRequired=true,checkpostDestinationKnown=true,checkpostVisited=true,checkpostAccountKnown=true,trailInstructionsUnderstood=true};
 ReturnInvestigation.Begin(walk,true); ReturnInvestigation.Complete(walk,true,true); ReturnInvestigation.Begin(skip,true); ReturnInvestigation.Complete(skip,true,true);
 Check(walk.returnedWithAnanya==skip.returnedWithAnanya && walk.fridayFinaleStarted==skip.fridayFinaleStarted,"walk and skip share story result");
 Check(!FinaleInvestigation.Answer(locked,0,true) && !FinaleInvestigation.Decide(locked,true),"no finale from early visit");
 Check(!FinaleInvestigation.Answer(journey,0,false),"presentation requires council proximity");
 Check(!FinaleInvestigation.Answer(journey,1,true) && !journey.deedPresented,"booking cannot replace original");
 Check(FinaleInvestigation.Answer(journey,0,true) && !journey.workSuspended,"presenting original alone insufficient");
 Check(!FinaleInvestigation.Decide(journey,true),"no interim decision before clauses");
 Check(!FinaleInvestigation.Answer(journey,0,true) && FinaleInvestigation.Answer(journey,1,true),"education purpose restated");
 Check(!FinaleInvestigation.Answer(journey,1,true) && FinaleInvestigation.Answer(journey,0,true),"restriction restated");
 Check(!FinaleInvestigation.Answer(journey,0,true) && FinaleInvestigation.Answer(journey,2,true),"beneficiary restated");
 Check(!FinaleInvestigation.Decide(journey,false) && FinaleInvestigation.Decide(journey,true) && journey.workSuspended,"council decision suspends works after full evidence");
 Check(new CouncilConversation().Start(journey).Id=="decision","revisit preserves decision");
 Check(!EpilogueInvestigation.Review(locked) && !EpilogueInvestigation.Choose(locked,StoryEnding.Stay),"no early protection or ending");
 Check(new EpilogueConversation("Somanna").Start(locked).Id=="pending","early epilogue dialogue reveals no ending");
 var incomplete=new TeaProgress {workSuspended=true}; Check(!EpilogueInvestigation.Review(incomplete),"suspension flag alone insufficient for later review");
 Check(EpilogueInvestigation.Review(journey) && journey.libraryProtected && journey.reviewOutcomeKnown && journey.epilogueStarted,"later explicit review confirms protection");
 Check(!journey.storyCompleted && journey.endingChoice==StoryEnding.None,"review does not choose personal ending");
 Check(!EpilogueInvestigation.Choose(journey,StoryEnding.None) && !EpilogueInvestigation.Choose(journey,(StoryEnding)99),"invalid ending rejected");
 Check(EpilogueInvestigation.Choose(journey,StoryEnding.Stay) && !journey.storyCompleted,"personal choice distinct from finishing");
 Check(EpilogueInvestigation.Complete(journey) && journey.storyCompleted,"explicit credits completion");
 Check(!EpilogueInvestigation.Choose(journey,StoryEnding.Depart) && journey.endingChoice==StoryEnding.Stay,"completed ending not overwritten by revisit");
 var depart=new TeaProgress {reviewOutcomeKnown=true,libraryProtected=true,epilogueStarted=true};
 Check(EpilogueInvestigation.Choose(depart,StoryEnding.Depart) && EpilogueInvestigation.Complete(depart) && depart.libraryProtected,"departure preserves same community outcome");
 var aided=new TeaProgress {reviewOutcomeKnown=true,libraryProtected=true,epilogueStarted=true,english=true,roman=true};
 var unaided=new TeaProgress {reviewOutcomeKnown=true,libraryProtected=true,epilogueStarted=true,english=false,roman=false};
 Check(EpilogueInvestigation.Choose(aided,StoryEnding.Stay) && EpilogueInvestigation.Choose(unaided,StoryEnding.Stay) && EpilogueInvestigation.Complete(aided) && EpilogueInvestigation.Complete(unaided),"assistance independent of story ending");
 Check(new EpilogueConversation("Ananya").Start(journey).Choices[0].Activity==VillageActivity.EndingReflection,"completed ending recap remains available");
 Check(DeedInvestigation.AdjustCode("9999",0,1)=="0999","lock digit increments wrap");
 Check(DeedInvestigation.AdjustCode("0000",3,-1)=="0009","lock digit decrements wrap");
 Check(DeedInvestigation.AdjustCode("oops",2,1)=="0010","invalid dial input normalizes without unlocking");
 Check(!ReturnInvestigation.Begin(journey,true),"completed return cannot restart from stale choice");
 for(int chapter=0;chapter<=14;chapter++) {
 var seeded=ChapterReplay.Seed((ReplayChapter)chapter,prefs);Check(TeaSaveStore.SupportedPosition(seeded.position),"chapter spawn is supported");Check(!seeded.storyCompleted && !seeded.libraryProtected,"practice does not pre-complete ending");Check(seeded.reducedMotion && seeded.quality==VillageQuality.Low,"chapter keeps preferences");
 }
 Check(!ChapterReplay.IsUnlocked(new TeaProgress(),ReplayChapter.Deed) && !ChapterReplay.IsUnlocked(journey,(ReplayChapter)99),"unreached and invalid chapter locked");
 Check(new TempleConversation(false).Start(ChapterReplay.Seed(ReplayChapter.Temple,prefs)).Id=="hello","temple replay starts at witness intro");
 Check(DeedInvestigation.CanInspect(ChapterReplay.Seed(ReplayChapter.Deed,prefs)) && !ChapterReplay.Seed(ReplayChapter.Deed,prefs).trunkOpened,"deed replay keeps puzzle unopened");
 Check(ReturnInvestigation.CanReturn(ChapterReplay.Seed(ReplayChapter.Return,prefs),true),"return replay has reviewed original");
 Check(FinaleInvestigation.CanPresent(ChapterReplay.Seed(ReplayChapter.Council,prefs)) && !ChapterReplay.Seed(ReplayChapter.Council,prefs).workSuspended,"council replay does not grant decision");
 Check(EpilogueInvestigation.Review(ChapterReplay.Seed(ReplayChapter.Epilogue,prefs)),"epilogue replay can reach later review");
 Check(WorldBindingPolicy.Resolve("trunkAnchor")!=null && WorldBindingPolicy.Resolve("finaleCast")!=null,"modular world fields recognized");
 Check(WorldBindingPolicy.Resolve("player")==null && WorldBindingPolicy.Resolve("villagers")==null,"bootstrap retains core and actor registry ownership");
 Check(WorldBindingPolicy.Resolve("ridgeRouteChecked")==null && WorldBindingPolicy.Resolve("typo")==null,"invalid modular binding rejected");
 var notice=new TeaProgress(); var clerk=new PanchayatConversation();
 Check(clerk.Start(notice).Id=="early" && !PanchayatInvestigation.Answer(notice,0),"early public board grants no story progression");
 notice.deedReferenceFound=true; Check(!PanchayatInvestigation.CanInvestigate(notice),"rest precedes office investigation"); notice.thursdayStarted=true;
 var nt=clerk.Start(notice); nt=clerk.Reply(nt,1,notice); Check(!notice.noticeIdentified && nt.Id=="question","optional evasion grants no facts");
 Check(nt.Choices[0].Activity==VillageActivity.PanchayatNotice,"evasion leaves public notice readable");
 Check(!PanchayatInvestigation.Answer(notice,1),"wrong site rejected");
 Check(PanchayatInvestigation.Answer(notice,0) && !notice.deadlineKnown,"site recorded independently");
 Check(!PanchayatInvestigation.Answer(notice,2) && !notice.deadlineKnown,"wrong time rejected");
 Check(PanchayatInvestigation.Answer(notice,1) && !notice.deedRequired,"deadline recorded without document inference");
 Check(!PanchayatInvestigation.Answer(notice,1) && !notice.checkpostDestinationKnown,"catalogue alone insufficient");
 Check(PanchayatInvestigation.Answer(notice,2) && notice.deedRequired && notice.checkpostDestinationKnown,"public route and document checklist sufficient without optional testimony");
 Check(clerk.ResumeAfterActivity(nt,notice).Id=="return","notice resumes relevant clerk turn");
 Check(PanchayatInvestigation.Answer(notice,2) && notice.deadlineKnown,"repeated reading preserves facts");
 var p=new TeaProgress(); Check(!ArchiveInvestigation.FindShelf(p,1) && !ArchiveInvestigation.ReadReference(p,0),"early archive tasks blocked");
 Check(!RiverInvestigation.Deduce(p,2) && !MarketInvestigation.TryReadTimetable(p,0),"earlier evidence gates retained");
 Console.WriteLine("CHAPTER_CHECKS_OK: "+checks+" assertions; arrival through ending, source gates, retries, lock, return, council review and assistance-independent outcomes."); return 0;
 }
}
