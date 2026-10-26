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
 public static int Main() {
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
 Console.WriteLine("CHAPTER_CHECKS_OK: "+checks+" assertions; both witness orders, retries, access, research inference explicit rest, panchayat notice, deadline and document prerequisites."); return 0;
 }
}
