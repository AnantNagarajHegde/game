#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace Paridhwani
{
    // Opt-in built-player verification. Uses a unique temporary save, never a player's visit.
    public sealed class VillageSmokeCheck : MonoBehaviour
    {
        TeaStallSession session;
        string output;
        int startFrame;
        public void Begin(TeaStallSession value)
        {
            session=value; output=Path.Combine(Path.GetTempPath(),"ParidhwaniSmoke","run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output); startFrame=Time.frameCount;
            StartCoroutine(Guarded());
        }
        static void Check(bool condition,string message)
        { if(!condition) throw new InvalidOperationException("Village smoke check: "+message); }
        IEnumerator Guarded()
        {
            var stack=new Stack<IEnumerator>(); stack.Push(Run());
            while(stack.Count>0)
            {
                bool next; object current=null;
                try { next=stack.Peek().MoveNext(); if(next) current=stack.Peek().Current; }
                catch(Exception e) { Debug.LogException(e); Application.Quit(2); yield break; }
                if(!next) { stack.Pop(); continue; }
                if(current is IEnumerator nested) stack.Push(nested); else yield return current;
            }
        }

        IEnumerator Shot(string name,bool checkHighlights=false)
        {
            yield return new WaitForEndOfFrame();
            if(checkHighlights)
            {
                var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0); pixels.Apply();
                var values=pixels.GetPixels32(); int clipped=0; double luminance=0;
                foreach(var v in values) { if(v.r>250 && v.g>250 && v.b>250) clipped++; luminance+=(v.r+v.g+v.b)/765.0; }
                Check(luminance/values.Length>.03,"frame was not rendered");
                float ratio=(float)clipped/values.Length;
                Destroy(pixels);
                Debug.Log("VILLAGE_HIGHLIGHT_RATIO "+name+" "+ratio.ToString("F5"));
                Check(ratio<.025f,"large white glare in "+name);
            }
            string file=Path.Combine(output,name+".png");
            ScreenCapture.CaptureScreenshot(file);
            float until=Time.realtimeSinceStartup+4;
            yield return null;
            while(!File.Exists(file) && Time.realtimeSinceStartup<until) yield return null;
            Check(File.Exists(file) && new FileInfo(file).Length>5000,"rendered capture missing: "+name);
        }
        IEnumerator WalkTo(Vector3 target)
        {
            var player=session.player; player.blocked=true; var controller=player.GetComponent<CharacterController>();
            float until=Time.realtimeSinceStartup+20;
            while(true)
            {
                Vector3 delta=target-player.transform.position; delta.y=0;
                if(delta.magnitude<.16f) break;
                Check(Time.realtimeSinceStartup<until,"river route blocked at "+target);
                player.visual.rotation=Quaternion.LookRotation(delta);
                controller.Move(delta.normalized*Mathf.Min(delta.magnitude,3.6f*Time.unscaledDeltaTime));
                yield return null;
            }
        }
        IEnumerator Run()
        {
            var player=session.player;
            yield return new WaitForSecondsRealtime(1);
            player.blocked=true;
            player.Restore(new Vector3(0,.2f,-4.8f),-32);
            yield return new WaitForSecondsRealtime(.8f);
            yield return Shot("01-village",true);
            player.SetLookAngles(-32,-38);
            yield return new WaitForSecondsRealtime(.7f);
            yield return Shot("02-roofs-and-sky",true);
            player.SetLookAngles(-32,17);
            var tea=Array.Find(session.villagers,n=>n.role==VillageNpcRole.TeaVendor);
            player.Restore(new Vector3(-4.2f,.28f,-1.65f),0);
            yield return new WaitForSecondsRealtime(.4f);
            Vector3 before=player.view.transform.position;
            session.StartConversation(tea);
            yield return new WaitForSecondsRealtime(.15f);
            Check(Vector3.Distance(player.view.transform.position,before)>.1f,"conversation begins moving camera");
            Check(player.ConversationBlend>0 && player.ConversationBlend<1,"camera has an intermediate state");
            yield return new WaitForSecondsRealtime(.8f);
            Check(Vector3.Distance(player.view.transform.position,player.EyePosition)<.08f,"first person uses player eyes");
            Check(player.ConversationBlend>.99f,"camera reaches first person");
            Check(Quaternion.Angle(player.view.transform.rotation,
                Quaternion.LookRotation(tea.eyes.position-Vector3.up*.55f-player.EyePosition))<2,"camera frames NPC");
            foreach(var r in player.visual.GetComponentsInChildren<Renderer>())
                Check(r.shadowCastingMode==ShadowCastingMode.ShadowsOnly,"own head/body hidden in first person");
            Check(player.view.WorldToViewportPoint(tea.eyes.position).y>.62f,"NPC face stays above dialogue panel");
            yield return Shot("03-first-person-tea");
            string turn=session.Turn.Id;
            session.OpenNotebook(); yield return new WaitForSecondsRealtime(.8f);
            Check(session.ui.Mode=="notebook","notebook opens over conversation");
            session.Close(); yield return new WaitForSecondsRealtime(.8f);
            Check(session.ui.Mode=="conversation" && session.Turn.Id==turn && player.ConversationBlend>.99f,"notebook returns to same NPC turn");
            session.Choose(1); session.Choose(0); session.Choose(0); session.Choose(0);
            Check(session.Progress.drinkOrdered && session.Progress.directionsKnown,"tea and directions");
            Check(!session.Progress.ananyaRoutineKnown,"no unheard clue");
            session.Choose(0); Check(session.Progress.ananyaRoutineKnown,"heard clue recorded");
            session.Choose(0); yield return new WaitForSecondsRealtime(.9f);
            Check(Vector3.Distance(player.view.transform.position,player.EyePosition)>2,"smooth return to third person");
            Check(Mathf.Abs(player.Heading)<.01f,"exploration heading preserved");
            foreach(var r in player.visual.GetComponentsInChildren<Renderer>())
                Check(r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly,"body visible after conversation");

            player.Restore(new Vector3(-4.5f,.1f,9.1f),0);
            var librarian=Array.Find(session.villagers,n=>n.role==VillageNpcRole.Librarian);
            session.StartConversation(librarian); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f,"librarian shares first-person camera");
            yield return Shot("04-first-person-library");
            session.Choose(0); session.Choose(0); session.Choose(0);
            Check(session.Progress.metSomanna && session.Progress.messLeadKnown,"library lead recorded");

            player.Restore(new Vector3(7.4f,.2f,-.05f),0);
            var cook=Array.Find(session.villagers,n=>n.role==VillageNpcRole.MessOwner);
            session.StartConversation(cook); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f,"cook shares first-person camera");
            session.Choose(1); session.Choose(1); session.Choose(0);
            Check(session.Progress.servingComplete && !session.Progress.shoppingNoteCollected,"serving task before evidence");
            session.Choose(0);
            Check(session.Progress.shoppingNoteCollected,"shopping note earned");
            yield return Shot("05-first-person-mess");
            session.Choose(0); session.Choose(0);
            session.Save(); session.Load();
            Check(session.Progress.shoppingNoteCollected && session.Progress.messLeadKnown,"expanded save round trip");
            yield return new WaitForSecondsRealtime(.9f);
            session.OpenNotebook(); yield return Shot("06-notebook");
            session.Close();
            session.OpenActivity(VillageActivity.BusTimetable);
            Check(!session.ui.AcceptingEvidenceAnswers && !session.Progress.busDepartureKnown,"early timetable is public but grants no clue");
            session.Close();
            player.Restore(new Vector3(3.2f,.2f,8.5f),60); player.blocked=true;
            yield return new WaitForSecondsRealtime(.6f); yield return Shot("07a-market-overview",true);
            player.Restore(new Vector3(6.35f,.2f,8.75f),45); player.blocked=true;
            yield return new WaitForSecondsRealtime(.5f);
            Check(session.Nearby=="Talk to Basavaraju","market NPC can be approached through the open lane");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f,"market uses shared first-person framing");
            Check(player.view.WorldToViewportPoint(session.ActiveNpc.eyes.position).y>.62f,"market speaker stays above dialogue");
            yield return Shot("07-market-conversation");
            session.Choose(0); session.Choose(0);
            Check(session.ui.Mode=="evidence" && session.ActiveActivity==VillageActivity.MarketReceipt,"reply opens ledger without losing speaker");
            session.SubmitInvestigationAnswer(1);
            Check(!session.Progress.purchaseTimeKnown,"incorrect receipt answer preserves progress");
            yield return Shot("08-ledger-hint");
            session.SubmitInvestigationAnswer(0);
            Check(session.Progress.purchaseTimeKnown,"receipt interpreted");
            yield return Shot("09-ledger-recorded");
            session.Close(); yield return new WaitForSecondsRealtime(.9f);
            Check(session.ui.Mode=="conversation" && session.Turn.Id=="purchase" && player.ConversationBlend>.99f,"ledger returns to the relevant NPC turn");
            session.Choose(0); Check(session.Progress.marketAccountKnown,"journey account heard"); session.Choose(0);
            player.Restore(new Vector3(3.25f,.1f,-7.2f),0); player.blocked=true;
            yield return new WaitForSecondsRealtime(.5f);
            Check(session.Nearby=="Read the bus timetable","world timetable has a reachable prompt");
            session.Interact(); session.SubmitInvestigationAnswer(1);
            Check(!session.Progress.busDepartureKnown && !session.Progress.riverReportKnown,"wrong service grants neither clue");
            session.SubmitInvestigationAnswer(0);
            Check(session.Progress.busDepartureKnown,"timetable match recorded");
            yield return Shot("10-timetable");
            session.ReadRiverNotice(); Check(session.Progress.riverReportKnown,"pinned notice records an unverified report");
            yield return Shot("11-giri-notice"); session.Close(); session.Save(); session.Load();
            Check(session.Progress.riverReportKnown && session.Progress.purchaseTimeKnown && session.Progress.busDepartureKnown,"market evidence survives restore");
            session.OpenNotebook(); yield return Shot("12-market-notebook"); session.Close();
            player.Restore(new Vector3(1.4f,.2f,6.5f),-90); player.blocked=true;
            foreach(var point in new[]{new Vector3(-10,0,6.5f),new Vector3(-14,0,6.5f),new Vector3(-19,0,6.5f),new Vector3(-21,0,6.5f)})
                yield return WalkTo(point);
            player.SetLookAngles(-105,17); yield return new WaitForSecondsRealtime(.5f); yield return Shot("13-river-overview",true);
            yield return WalkTo(new Vector3(-23.5f,0,2.2f)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.5f);
            Check(session.Nearby=="Talk to Giri","walkable route reaches Giri prompt");
            player.SetLookAngles(0,-44); yield return new WaitForSecondsRealtime(.7f); yield return Shot("14a-shed-roof",true);
            player.SetLookAngles(0,17);
            session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f && !player.IsInspecting,"Giri uses NPC first person");
            yield return Shot("14-giri-conversation"); session.Choose(0); session.Choose(0);
            yield return new WaitForSecondsRealtime(.9f);
            Check(player.IsInspecting && Vector3.Distance(player.view.transform.position,session.bicycleView.position)<.08f,"camera focuses actual bicycle");
            session.SelectBicyclePart(BicyclePart.Frame); session.SelectBicyclePart(BicyclePart.Stand); session.SelectBicyclePart(BicyclePart.CarrierPocket);
            session.SubmitInvestigationAnswer(0); Check(!session.Progress.bookingSlipFound,"wrong spatial answer grants no booking");
            session.OpenNotebook(); session.Close(); yield return new WaitForSecondsRealtime(.8f);
            Check(player.IsInspecting && session.SelectedBicyclePart==BicyclePart.CarrierPocket && session.ui.AcceptingEvidenceAnswers,"notebook restores exact inspection context");
            yield return Shot("15-bicycle-spatial-task");
            session.SubmitInvestigationAnswer(1); Check(session.Progress.bookingSlipFound && !session.bookingPaper.activeSelf,"booking collected once from pocket");
            session.OpenActivity(VillageActivity.BookingSlip); session.SubmitInvestigationAnswer(1);
            Check(!session.Progress.bookingSlipRead,"booking cannot establish witnessed boarding");
            session.SubmitInvestigationAnswer(0); Check(session.Progress.bookingSlipRead,"booking read as intended travel");
            yield return Shot("16-advance-booking");
            session.OpenNotebook(); session.Close();
            Check(session.ActiveActivity==VillageActivity.BookingSlip,"notebook returns to booking page");
            session.Close(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.IsInspecting && session.ActiveActivity==VillageActivity.BicycleInspection,"booking returns to bicycle before NPC");
            session.Close(); yield return new WaitForSecondsRealtime(.9f);
            Check(!player.IsInspecting && session.Turn.Id=="deduce" && player.ConversationBlend>.99f,"inspection returns to relevant Giri conversation");
            session.Choose(0); session.SubmitInvestigationAnswer(0);
            Check(!session.Progress.riverDeductionKnown,"accident inference rejected");
            session.SubmitInvestigationAnswer(2); Check(session.Progress.riverDeductionKnown,"qualified river deduction recorded");
            yield return Shot("17-river-evidence"); session.Close(); session.Choose(0);
            Check(session.Progress.templeLeadKnown,"temple witnesses introduced");
            yield return Shot("18-temple-lead"); session.Choose(0); session.Save(); session.Load();
            Check(session.Progress.templeLeadKnown && session.Progress.bookingSlipRead && player.transform.position.x<-20,"river save position and progress survive restore");
            session.OpenNotebook(); yield return Shot("19-river-notebook"); session.Close();
            player.Restore(new Vector3(-26.3f,.16f,2.1f),0); player.blocked=true;
            yield return new WaitForSecondsRealtime(.5f);
            Check(session.Nearby=="Inspect the bicycle","bicycle has a reachable physical interaction");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            session.SelectBicyclePart(BicyclePart.CarrierPocket);
            Check(session.Progress.bookingSlipRead && !session.bookingPaper.activeSelf,"repeat inspection preserves collected item");
            yield return Shot("20-bicycle-revisit"); session.Close(); yield return new WaitForSecondsRealtime(.9f);
            Check(!player.IsInspecting && Vector3.Distance(player.view.transform.position,player.EyePosition)>2,"inspection returns to exploration");
            foreach(var point in new[]{new Vector3(-26.3f,0,1.5f),new Vector3(-21,0,1.5f),new Vector3(-21,0,6.5f),new Vector3(-10,0,6.5f),new Vector3(1.4f,0,6.5f)})
                yield return WalkTo(point);
            player.Restore(new Vector3(-28,.16f,10.3f),-90); player.SetLookAngles(-90,17); player.blocked=true;
            yield return new WaitForSecondsRealtime(.7f); yield return Shot("21-weir",true);
            player.Restore(new Vector3(1,.2f,17),0); player.blocked=true;
            yield return WalkTo(new Vector3(1,0,23.4f));
            yield return WalkTo(new Vector3(-5.6f,0,23.4f));
            player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Talk to Gurumurthy","northern lane reaches temple witness");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f,"temple witness uses shared FPP camera");
            session.Choose(0); yield return Shot("22-gurumurthy"); session.Choose(0); session.Choose(1);
            yield return new WaitForSecondsRealtime(.9f); player.blocked=true;
            yield return WalkTo(new Vector3(-1.7f,0,23.4f)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Rathnamma","flower seller reachable");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0); session.Choose(0);
            session.Choose(1); Check(!session.Progress.witnessAccountsCompared,"unwitnessed arrival rejected");
            session.Choose(0); session.Choose(0); session.Choose(0);
            Check(session.Progress.guddaFamilyLeadKnown,"temple yields family lead");
            yield return Shot("23-rathnamma-family"); session.Choose(0);
            yield return new WaitForSecondsRealtime(.9f); player.blocked=true;
            yield return WalkTo(new Vector3(1,0,23.4f)); yield return WalkTo(new Vector3(1,0,11.2f));
            yield return WalkTo(new Vector3(-4.6f,0,9)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Somanna","archive return reaches Somanna");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0);
            Check(session.Progress.libraryAccessGranted && !session.archiveDoor.activeSelf,"master key opens physical doorway");
            session.Choose(0); session.SubmitInvestigationAnswer(0); Check(!session.Progress.archiveShelfLocated,"wrong shelf grants no reference");
            session.SubmitInvestigationAnswer(1); session.SubmitInvestigationAnswer(2); Check(!session.Progress.deedReferenceFound,"catalogue is not a protection decision");
            session.OpenNotebook(); session.Close(); Check(session.ActiveActivity==VillageActivity.ArchiveReference,"notebook returns to archive task");
            session.SubmitInvestigationAnswer(0); yield return Shot("24-archive-reference"); session.Close();
            Check(session.Turn.Id=="rest","catalogue returns to Somanna rest offer");
            session.Choose(1); Check(!session.Progress.thursdayStarted,"rest can be declined");
            yield return new WaitForSecondsRealtime(.9f); player.blocked=true;
            yield return WalkTo(new Vector3(-5.25f,0,12.3f)); yield return WalkTo(new Vector3(-5.25f,0,14.7f));
            yield return WalkTo(new Vector3(-4.5f,0,15.2f));
            player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.6f);
            Check(session.Nearby=="Read the archive catalogue","room doorway and catalogue approach are walkable");
            session.Interact(); yield return Shot("25-library-interior"); session.Close();
            var somanna=Array.Find(session.villagers,n=>n.role==VillageNpcRole.Librarian);
            yield return WalkTo(new Vector3(-5.25f,0,14.7f)); yield return WalkTo(new Vector3(-5.25f,0,12.3f));
            session.StartConversation(somanna); yield return new WaitForSecondsRealtime(.9f); session.Choose(0);
            Check(session.Progress.thursdayStarted && session.ui.Mode=="day-transition","explicit rest advances day with transition");
            yield return Shot("26-thursday-transition"); session.ui.ShowConversation(); session.Choose(1); session.Save(); session.Load();
            Check(session.Progress.thursdayStarted && session.Progress.deedReferenceFound && !session.archiveDoor.activeSelf,"archive access, evidence and day survive save");
            session.OpenNotebook(); yield return Shot("27-archive-notebook"); session.Close();
            player.blocked=true;
            yield return WalkTo(new Vector3(1,0,12.3f)); yield return WalkTo(new Vector3(1,0,19.8f));
            player.SetLookAngles(90,17); yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Read the panchayat notice","public notice reachable independently of clerk");
            session.Interact(); session.SubmitInvestigationAnswer(1); Check(!session.Progress.noticeIdentified,"wrong notice subject rejected");
            session.SubmitInvestigationAnswer(0); session.SubmitInvestigationAnswer(2); Check(!session.Progress.deadlineKnown,"wrong deadline rejected");
            session.OpenNotebook(); session.Close(); Check(session.ActiveActivity==VillageActivity.PanchayatNotice,"notebook restores public notice task");
            session.SubmitInvestigationAnswer(1); session.SubmitInvestigationAnswer(1); Check(!session.Progress.deedRequired,"catalogue alone insufficient");
            session.SubmitInvestigationAnswer(2); Check(session.Progress.checkpostDestinationKnown,"public checklist yields safe route lead");
            yield return Shot("28-panchayat-notice"); session.Close(); yield return new WaitForSecondsRealtime(.9f);
            player.blocked=true; yield return WalkTo(new Vector3(5,0,18.8f)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Shresthi","public counter reaches clerk without entering office");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f && session.Turn.Id=="return","clerk uses shared FPP and earned evidence");
            yield return Shot("29-shresthi"); session.Choose(0); session.Close();
            Check(session.ui.Mode=="conversation" && session.Turn.Id=="return","notice returns to clerk");
            session.Choose(1); session.Save(); session.Load();
            Check(session.Progress.noticeIdentified && session.Progress.deadlineKnown && session.Progress.deedRequired && session.Progress.checkpostDestinationKnown,"panchayat evidence survives restore");
            session.OpenNotebook(); yield return Shot("30-panchayat-notebook"); session.Close();
            player.Restore(new Vector3(6.5f,.15f,-5.8f),24); player.blocked=true;
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Raghavendra","town pickup reachable");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0);
            Check(!session.Progress.checkpostVisited,"travel requires confirmation"); session.Close();
            Check(session.ui.Mode=="conversation" && !session.Progress.checkpostVisited,"cancelled journey returns to driver");
            session.Choose(0); session.ConfirmTravel();
            Check(session.Progress.checkpostVisited && player.transform.position.x>40 && session.ui.Mode=="travel-arrival","confirmed trip reaches safe approach");
            yield return Shot("31-checkpost-arrival"); session.Close(); player.blocked=true;
            yield return WalkTo(new Vector3(55,0,-.8f)); player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Read the road closure and ridge directions","public closure board reachable"); session.Interact();
            Check(!session.Progress.checkpostAccountKnown && session.ridgeGate.activeSelf,"closure alone grants no route access"); session.Close();
            player.blocked=true; yield return WalkTo(new Vector3(51,0,1.1f)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Kencha","shelter reaches Kencha");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); Check(player.ConversationBlend>.99f,"guard uses FPP transition");
            session.Choose(1); session.Choose(0); Check(session.Progress.ananyaLocationReported && !session.Progress.trailPermission,"reported location is not trail permission");
            yield return Shot("32-kencha-account"); session.Choose(0); session.Choose(0);
            session.SubmitInvestigationAnswer(0); Check(!session.Progress.trailPermission,"unsafe road choice rejected");
            session.OpenNotebook(); session.Close(); Check(session.ActiveActivity==VillageActivity.CheckpostDirections,"notebook preserves directions task");
            session.SubmitInvestigationAnswer(1); Check(session.Progress.trailPermission && !session.ridgeGate.activeSelf,"checked ridge gate opens after understanding");
            yield return Shot("33-ridge-directions"); session.Close(); Check(session.Turn.Id=="ready","directions resume guard at ready turn");
            session.Choose(1); yield return new WaitForSecondsRealtime(.9f); player.blocked=true;
            yield return WalkTo(new Vector3(47.5f,0,1.1f)); yield return WalkTo(new Vector3(48,0,6.8f)); yield return WalkTo(new Vector3(48,0,10));
            Check(Physics.CheckCapsule(new Vector3(60.5f,.4f,5),new Vector3(60.5f,1.3f,5),.25f,~(1<<2)),"road closure stays solid after ridge permission");
            session.Save(); session.Load(); Check(player.transform.position.x>40 && session.Progress.trailPermission,"checkpost position and learned directions restore");
            player.Restore(new Vector3(56.5f,.15f,-5.5f),0); player.blocked=true; yield return new WaitForSecondsRealtime(.8f);
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0); session.ConfirmTravel();
            Check(player.transform.position.x<13 && session.Progress.checkpostVisited && session.Progress.trailPermission,"return trip preserves evidence");
            yield return Shot("34-village-return"); session.Close();
            Check(Time.frameCount>startFrame+50,"player actually advanced frames");
            File.WriteAllText(Path.Combine(output,"result.txt"),
                "VILLAGE_SMOKE_OK\nRender highlights, TPP/FPP transition and return, body visibility, eleven NPC instances, confirmed checkpost round-trip, route permission, panchayat counter and notice, temple witnesses, enterable archive, explicit day transition, river round-trip, 3D bicycle inspection, nested notebook/evidence return, booking/deduction retries and saved progress passed.\nFrames: "+(Time.frameCount-startFrame));
            Debug.Log("VILLAGE_SMOKE_OK "+output+" Frames="+(Time.frameCount-startFrame));
            Application.Quit(0);
        }
    }
}
#endif
