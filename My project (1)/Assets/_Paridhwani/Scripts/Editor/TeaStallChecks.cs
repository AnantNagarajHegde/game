using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static class TeaStallChecks
    {
        static void Check(bool value,string reason) { if(!value) throw new Exception("Tea-stall check failed: "+reason); }
        [MenuItem("Paridhwani/Run Benchmark Checks")]
        public static void Run()
        {
            var provider=new LocalTeaConversation();
            var p=new TeaProgress();
            var turn=provider.Start(p);
            turn=provider.Reply(turn,1,p); // Request help.
            Check(!p.drinkOrdered && !p.directionsKnown,"help must not unlock clues");
            turn=provider.Reply(turn,0,p); // Greet.
            Check(p.greeted,"greeting recorded");
            turn=provider.Reply(turn,2,p); // Decline tea.
            Check(!p.drinkOrdered,"decline must not grant a drink");
            turn=provider.Reply(turn,0,p); // Choose water.
            Check(p.drinkOrdered && p.drink=="water","water is valid");
            turn=provider.Reply(turn,0,p); // Ask directions.
            Check(p.directionsKnown && turn.Id=="directions","directions recorded");
            provider.Reply(turn,1,p); // Leave.
            Check(provider.Start(p).Id=="return","revisit continues safely");

            var tea=new TeaProgress(); turn=provider.Start(tea);
            turn=provider.Reply(turn,0,tea); turn=provider.Reply(turn,0,tea);
            Check(tea.drink=="tea" && !tea.directionsKnown,"tea does not prematurely grant directions");
            turn=provider.Reply(turn,1,tea);
            Check(turn==null && provider.Start(tea).Id=="return","early exit preserves progress");

            Check(!p.ananyaRoutineKnown,"directions do not reveal unheard testimony");
            var librarianProvider=new VillageChapterConversation(VillageNpcRole.Librarian);
            turn=librarianProvider.Start(p); turn=librarianProvider.Reply(turn,0,p);
            Check(p.metSomanna && !p.messLeadKnown,"library greeting does not reveal lead");
            turn=librarianProvider.Reply(turn,0,p); librarianProvider.Reply(turn,0,p);
            Check(p.messLeadKnown,"polite library request grants lead");
            var cookProvider=new VillageChapterConversation(VillageNpcRole.MessOwner);
            var early=new TeaProgress(); Check(cookProvider.Start(early).Id=="mess_early","early visitor gets contextual greeting");
            turn=cookProvider.Start(p); turn=cookProvider.Reply(turn,1,p);
            Check(p.mealOrdered && !p.shoppingNoteCollected,"water is valid but not yet evidence");
            turn=cookProvider.Reply(turn,0,p); turn=cookProvider.Reply(turn,0,p);
            Check(p.servingComplete && p.shoppingNoteCollected,"mess evidence follows serving");
            Check(cookProvider.Start(p).Id=="mess_note" && !p.marketLeadKnown,"unheard next lead remains locked");
            turn=cookProvider.Reply(turn,0,p);
            Check(cookProvider.Start(p).Id=="mess_return" && p.marketLeadKnown,"mess revisit preserves evidence");

            var market=new MarketConversation();
            var unknown=new TeaProgress();
            Check(market.Start(unknown).Id=="early","market does not reveal evidence to an early visitor");
            Check(!MarketInvestigation.TryReadReceipt(unknown,0),"receipt requires an earned excerpt");
            Check(!MarketInvestigation.TryReadTimetable(unknown,0),"public timetable alone grants no journey clue");
            Check(!MarketInvestigation.ReadRiverNotice(unknown),"river notice follows timetable interpretation");
            turn=market.Start(p); turn=market.Reply(turn,0,p);
            Check(p.marketReceiptCollected && turn.Choices[0].Activity==VillageActivity.MarketReceipt,"reading action attached to earned receipt");
            Check(!MarketInvestigation.TryReadReceipt(p,1) && !p.purchaseTimeKnown,"wrong day does not grant evidence");
            Check(MarketInvestigation.TryReadReceipt(p,0) && p.purchaseTimeKnown,"purchase time interpreted");
            turn=market.ResumeAfterActivity(turn,p);
            Check(turn.Id=="purchase" && !p.marketAccountKnown,"receipt resumes its speaker without inventing journey facts");
            turn=market.Reply(turn,0,p);
            Check(p.marketAccountKnown && turn.Id=="bus","journey account heard");
            Check(!MarketInvestigation.TryReadTimetable(p,2) && !p.busDepartureKnown,"wrong weekday does not grant departure");
            Check(MarketInvestigation.TryReadTimetable(p,0) && p.busDepartureKnown,"intended departure matched");
            Check(MarketInvestigation.TryReadTimetable(p,0),"repeat interpretation is harmless");
            Check(MarketInvestigation.ReadRiverNotice(p) && p.riverReportKnown,"river report has explicit provenance");
            Check(market.Start(p).Id=="bus","market revisit preserves progression");

            var giri=new RiverConversation(); var earlyRiver=new TeaProgress();
            Check(giri.Start(earlyRiver).Id=="early","Giri contextual early visit");
            Check(!RiverInvestigation.Observe(earlyRiver,BicyclePart.Frame),"unintroduced bicycle grants no evidence");
            Check(!RiverInvestigation.FindBooking(earlyRiver,1) && !RiverInvestigation.Deduce(earlyRiver,2),"river evidence prerequisites");
            turn=giri.Start(p); turn=giri.Reply(turn,0,p);
            Check(p.giriAccountKnown && turn.Choices[0].Activity==VillageActivity.BicycleInspection,"Giri authorizes inspection");
            Check(RiverInvestigation.Observe(p,BicyclePart.Frame) && RiverInvestigation.Observe(p,BicyclePart.Stand),"physical observations recorded");
            Check(!RiverInvestigation.Deduce(p,2),"deduction needs booking context");
            Check(!RiverInvestigation.FindBooking(p,0) && !p.bookingSlipFound,"wrong spatial word grants no item");
            Check(RiverInvestigation.FindBooking(p,1) && p.bookingSlipFound,"inside the pocket reveals advance booking");
            Check(!RiverInvestigation.ReadBooking(p,1) && !p.bookingSlipRead,"advance booking is not boarding proof");
            Check(RiverInvestigation.ReadBooking(p,0) && RiverInvestigation.CanDeduce(p),"intent interpreted after observations");
            Check(!RiverInvestigation.Deduce(p,0) && !p.riverDeductionKnown,"accident inference rejected without penalty");
            Check(RiverInvestigation.Deduce(p,2) && p.riverDeductionKnown,"bounded inference accepted");
            turn=giri.ResumeAfterActivity(turn,p); Check(turn.Id=="witness","inspection resumes the relevant Giri turn");
            turn=giri.Reply(turn,0,p); Check(p.templeLeadKnown && giri.Start(p).Id=="return","temple handoff and revisit");

            string directory=Path.Combine(Path.GetTempPath(),"paridhwani-check-"+Guid.NewGuid().ToString("N"));
            string path=Path.Combine(directory,"visit.json");
            var store=new TeaSaveStore(path);
            try
            {
                Check(!store.Read(out _).directionsKnown,"new visit defaults");
                p.position=new Vector3(-26.3f,.16f,2.2f); p.english=false;
                Check(store.Write(p,out _),"first save");
                var restored=store.Read(out _);
                Check(restored.directionsKnown && restored.drink=="water" && !restored.english &&
                    restored.position==p.position && restored.shoppingNoteCollected && restored.messLeadKnown && restored.riverReportKnown && restored.busDepartureKnown && restored.templeLeadKnown && restored.bookingSlipRead,"round-trip facts, settings and position");
                p.position=new Vector3(float.NaN,0,1); store.Write(p,out _);
                restored=store.Read(out _); Check(float.IsFinite(restored.position.x),"invalid location recovers");
                File.WriteAllText(path,"not a saved visit");
                restored=store.Read(out string notice);
                Check(restored.directionsKnown && notice!=null,"corrupt primary recovers backup");
                File.WriteAllText(path,"{\"version\":99}");
                File.WriteAllText(path+".bak","{\"version\":99}");
                Check(!store.Read(out _).directionsKnown,"unknown schema fails safely");
            }
            finally { if(Directory.Exists(directory)) Directory.Delete(directory,true); }

            var w=new TeaProgress(); var keeper=new TempleConversation(false); var seller=new TempleConversation(true);
            Check(keeper.Start(w).Id=="early","early temple visit grants no testimony"); w.templeLeadKnown=true;
            var wt=keeper.Start(w); keeper.Reply(wt,0,w);
            Check(w.gurumurthyAccountKnown && !w.rathnammaAccountKnown,"independent testimony");
            wt=seller.Start(w); wt=seller.Reply(wt,0,w); wt=seller.Reply(wt,0,w);
            Check(wt.Id=="compare","both accounts precede comparison");
            wt=seller.Reply(wt,1,w); Check(!w.witnessAccountsCompared,"wrong inference grants no fact");
            wt=seller.Reply(wt,0,w); wt=seller.Reply(wt,0,w);
            Check(w.witnessAccountsCompared && !w.guddaFamilyLeadKnown,"family lead remains unheard");
            seller.Reply(wt,0,w); Check(w.guddaFamilyLeadKnown,"family lead earned");
            string templeSave=Path.Combine(Path.GetTempPath(),"temple-check-"+Guid.NewGuid().ToString("N")+".json");
            try { w.position=new Vector3(1,.1f,26); var ts=new TeaSaveStore(templeSave); Check(ts.Write(w,out _),"temple save written"); var wr=ts.Read(out _); Check(wr.gurumurthyAccountKnown && wr.rathnammaAccountKnown && wr.witnessAccountsCompared && wr.guddaFamilyLeadKnown && wr.position==w.position,"temple sources and position round-trip"); }
            finally { if(File.Exists(templeSave)) File.Delete(templeSave); if(File.Exists(templeSave+".bak")) File.Delete(templeSave+".bak"); }
            var a=new TeaProgress(); var archivist=new ArchiveConversation();
            Check(!ArchiveInvestigation.FindShelf(a,1) && !ArchiveInvestigation.ReadReference(a,0),"archive tasks require access");
            a.guddaFamilyLeadKnown=true; var at=archivist.Start(a); at=archivist.Reply(at,0,a);
            Check(a.libraryAccessGranted && !a.deedReferenceFound && !a.thursdayStarted,"access alone does not grant evidence or rest");
            Check(!ArchiveInvestigation.FindShelf(a,0) && ArchiveInvestigation.FindShelf(a,1),"second cupboard with retry");
            Check(!ArchiveInvestigation.ReadReference(a,1) && ArchiveInvestigation.ReadReference(a,0),"reference is not proof of arrival");
            at=archivist.ResumeAfterActivity(at,a); Check(at.Id=="rest","evidence resumes relevant dialogue");
            archivist.Reply(at,1,a); Check(!a.thursdayStarted,"declining rest preserves day");
            archivist.Reply(archivist.Start(a),0,a); Check(a.thursdayStarted,"rest requires explicit choice");
            var pc=new TeaProgress(); Check(!PanchayatInvestigation.Answer(pc,0),"early notice grants no investigation fact");
            pc.deedReferenceFound=true; pc.thursdayStarted=true;
            Check(!PanchayatInvestigation.Answer(pc,1) && !pc.noticeIdentified,"wrong subject grants nothing");
            Check(PanchayatInvestigation.Answer(pc,0) && pc.noticeIdentified && !pc.deadlineKnown,"subject precedes deadline");
            Check(!PanchayatInvestigation.Answer(pc,0) && PanchayatInvestigation.Answer(pc,1) && pc.deadlineKnown,"deadline retry");
            Check(!PanchayatInvestigation.Answer(pc,1) && !pc.deedRequired,"catalogue is not original deed");
            Check(PanchayatInvestigation.Answer(pc,2) && pc.deedRequired && pc.checkpostDestinationKnown,"public checklist supplies safe next lead");
            string checkpointSave=Path.Combine(Path.GetTempPath(),"checkpost-save-"+Guid.NewGuid().ToString("N")+".json");
            try {
                var cp=new TeaProgress { position=new Vector3(55,.15f,-5), checkpostVisited=true, checkpostAccountKnown=true, ananyaLocationReported=true, trailInstructionsUnderstood=true };
                var cs=new TeaSaveStore(checkpointSave); Check(cs.Write(cp,out _),"checkpost save written"); var cr=cs.Read(out _);
                Check(cr.position==cp.position && cr.checkpostVisited && cr.ananyaLocationReported && cr.trailInstructionsUnderstood,"checkpost position and facts round-trip");
            } finally { if(File.Exists(checkpointSave)) File.Delete(checkpointSave); if(File.Exists(checkpointSave+".bak")) File.Delete(checkpointSave+".bak"); }
            string farmSave=Path.Combine(Path.GetTempPath(),"farm-save-"+Guid.NewGuid().ToString("N")+".json");
            try {
                var fp=new TeaProgress {position=new Vector3(50,.13f,38.7f),ridgeWayfindingKnown=true,ridgeCrossed=true,guddaManeReached=true,ananyaFound=true,disappearanceExplained=true,trunkLocationKnown=true};
                var fs=new TeaSaveStore(farmSave); Check(fs.Write(fp,out _),"farm save written"); var fr=fs.Read(out _);
                Check(fr.position==fp.position && fr.ananyaFound && fr.disappearanceExplained && fr.trunkLocationKnown && fr.guddaManeReached,"farm position and direct evidence round-trip");
            } finally { if(File.Exists(farmSave)) File.Delete(farmSave); if(File.Exists(farmSave+".bak")) File.Delete(farmSave+".bak"); }
            string endingDir=Path.Combine(Path.GetTempPath(),"ending-save-"+Guid.NewGuid().ToString("N"));
            try {
                var main=new TeaSaveStore(Path.Combine(endingDir,"main.json"));var practice=new TeaSaveStore(Path.Combine(endingDir,"practice.json"));
                var ending=new TeaProgress {deedRetrieved=true,deedConceptsReviewed=true,workSuspended=true,reviewOutcomeKnown=true,libraryProtected=true,epilogueStarted=true,endingChoice=StoryEnding.Depart,storyCompleted=true};
                Check(main.Write(ending,out _) && practice.Write(new TeaProgress(),out _),"distinct ending and practice saves written");
                var saved=main.Read(out _);Check(saved.storyCompleted && saved.libraryProtected && saved.endingChoice==StoryEnding.Depart && !practice.Read(out _).storyCompleted,"practice storage leaves completed ending intact");
            } finally {if(Directory.Exists(endingDir)) Directory.Delete(endingDir,true);}
            foreach(string texture in new[]{"WetEarth","WeatheredStone","LimePlaster","ClayTile","Timber","CottonWeave","Bark"}) {var asset=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Paridhwani/Art/Textures/Surfaces/"+texture+".png");Check(asset && asset.width==512 && asset.height==512,"source surface texture imported");}
            var surfaces=UnityEngine.Object.FindObjectsByType<VillageFootstepSurface>();
            bool earthAudio=false,stoneAudio=false,timberAudio=false;
            foreach(var surface in surfaces) {
                Check(surface.GetComponent<Collider>(),"footstep surface belongs to collidable geometry");
                earthAudio|=surface.kind==FootstepSurface.Earth;stoneAudio|=surface.kind==FootstepSurface.Stone;timberAudio|=surface.kind==FootstepSurface.Timber;
            }
            Check(earthAudio && stoneAudio && timberAudio,"generated world provides earth, stone and timber contact metadata");
            foreach(var sign in UnityEngine.Object.FindObjectsByType<VillageSign>(FindObjectsInactive.Include))Check(sign.theme,"world sign has a runtime panel theme");
            string scene=TeaStallSceneBuilder.ScenePath;
            Check(File.Exists(scene),"scene generated");
            string sceneGuid=AssetDatabase.AssetPathToGUID(scene);
            Check(!string.IsNullOrEmpty(sceneGuid),"scene has imported GUID");
            foreach(var entry in EditorBuildSettings.scenes) if(entry.path==scene)
                Check(entry.guid.ToString()==sceneGuid,"Build Settings uses imported scene GUID");
            var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            Check(session && session.player && session.vendor && session.ui && session.ambience,"scene references");
            Check(session.villagers!=null && session.villagers.Length==14,"fourteen NPC instances share interaction system");
            foreach(var npc in session.villagers) Check(npc && npc.eyes,"NPC eye anchors");
            Check(session.checkpostArrival && session.villageArrival && session.checkpostBoard && session.ridgeEntrance && session.ridgeGate,"checkpost travel and gate references");
            Check(session.ridgeMarker && session.farmArrival && session.farmGate,"ridge and farm progression anchors");
            Check(session.councilPresident && session.finaleSet && session.worksPendingMarker && session.worksSuspendedMarker,"finale staging references");
            Check(session.trunkAnchor && session.trunkView && session.trunkLookTarget && session.trunkLid && session.deedPaper,"trunk inspection references");
            Check(session.returnCompanion && session.returnWait && session.fridayArrival && session.fridayCompanion && session.epilogueSet,"return and epilogue references");
            Check(session.panchayatNotice,"public notice anchor");
            Check(session.archiveDoor && session.archiveDesk,"archive room references");
            Check(session.busTimetable,"public timetable interaction anchor");
            Check(session.bicycle && session.bicycleView && session.bicycleLookTarget && session.bookingPaper,"bicycle and inspection references");
            Physics.SyncTransforms();
            foreach(var point in new[]{new Vector3(-10,.4f,6.5f),new Vector3(-13,.4f,6.5f),new Vector3(-18,.4f,6.5f),new Vector3(-21,.4f,6.5f),new Vector3(-23.5f,.4f,2.2f)})
                Check(!Physics.CheckCapsule(point+Vector3.up*.3f,point+Vector3.up*1.4f,.25f,~(1<<2)),"river route approach clearance");
            Check(Physics.CheckCapsule(new Vector3(-29.85f,.4f,0),new Vector3(-29.85f,1.3f,0),.25f,~(1<<2)),"water-edge barrier");
            Check(!ShaderUtil.ShaderHasError(Shader.Find("Paridhwani/River Water")),"river shader compile");
            foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())
                if(filter.name=="Sagging canvas awning" || filter.name=="Areca crown")
                    foreach(var normal in filter.sharedMesh.normals) Check(normal.sqrMagnitude>.5f,"fabric/foliage normals must not cancel");
            var volume=UnityEngine.Object.FindAnyObjectByType<UnityEngine.Rendering.Volume>();
            Check(volume && volume.sharedProfile && volume.sharedProfile.components.Count==4,"atmosphere profile");
            foreach(var effect in volume.sharedProfile.components) Check(effect && AssetDatabase.Contains(effect),"volume effects persisted as subassets");
            Check(RenderSettings.skybox && RenderSettings.skybox.shader.name=="Paridhwani/Overcast Sky","sunless sky shader");
            Check(!ShaderUtil.ShaderHasError(Shader.Find("Paridhwani/Overcast Sky")),"sky shader compile");
            Check(session.player.actions!=null,"input actions assigned");
            Check(session.ui.kannadaFont && session.ui.latinFont,"fonts bundled");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>().Length==1,"one audio listener");
            Check(UnityEngine.Object.FindObjectsByType<Camera>().Length==1,"one camera");
            Check(session.offeredTea && session.offeredWater,"drink props");
            Check(!ShaderUtil.ShaderHasError(Shader.Find("Paridhwani/Village Surface")),"village shader compile");
            Debug.Log("PARIDHWANI_CHECKS_OK: story source gates, fourteen NPC instances, evidence, ending/practice saves, recovery and generated scene references.");
        }
    }
}
