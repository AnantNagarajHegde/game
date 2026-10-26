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
 var p=new TeaProgress(); Check(!ArchiveInvestigation.FindShelf(p,1) && !ArchiveInvestigation.ReadReference(p,0),"early archive tasks blocked");
 Check(!RiverInvestigation.Deduce(p,2) && !MarketInvestigation.TryReadTimetable(p,0),"earlier evidence gates retained");
 Console.WriteLine("CHAPTER_CHECKS_OK: "+checks+" assertions; both witness orders, retries, access, research inference and explicit rest."); return 0;
 }
}
