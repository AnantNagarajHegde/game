using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

namespace Paridhwani
{
    // Opt-in built-player verification. Uses a unique temporary save, never a player's visit.
    public sealed partial class VillageSmokeCheck : MonoBehaviour
    {
        sealed class FailingSaveCodec : ITeaProgressCodec {
            public string Encode(TeaProgress progress) => throw new IOException("Deliberate isolated quit-save regression");
            public TeaProgress Decode(string json) => null;
        }
        TeaStallSession session;
        string output;
        int startFrame;
        bool originalBackground;
        readonly List<InputDevice> isolatedInputDevices=new List<InputDevice>();
        public void Begin(TeaStallSession value)
        {
            if(!Debug.isDebugBuild) {enabled=false;return;}
            originalBackground=Application.runInBackground;Application.runInBackground=true;
            // Disable only this Unity session's live input devices. The desktop remains usable;
            // regression gamepads are added later and continue through the normal input path.
            foreach(var device in InputSystem.devices)
                if(device.enabled && (device is Keyboard || device is Mouse || device is Gamepad))isolatedInputDevices.Add(device);
            foreach(var device in isolatedInputDevices)InputSystem.DisableDevice(device);
            session=value; output=Path.Combine(VillageTestStorage.Cache,"ParidhwaniSmoke","run-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output); startFrame=Time.frameCount;
            StartCoroutine(Guarded());
        }
        void OnDestroy() {
            Application.runInBackground=originalBackground;
            foreach(var device in isolatedInputDevices)if(device.added && !device.enabled)InputSystem.EnableDevice(device);
            isolatedInputDevices.Clear();
        }
        static void Finish(int code)
        {
#if UNITY_EDITOR
            Debug.Log("PARIDHWANI_EDITOR_SMOKE_EXIT "+code);
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(code);
#endif
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
                catch(Exception e) { Debug.LogException(e);File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Finish(2);yield break; }
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
                if(Time.realtimeSinceStartup>=until) {
                    var nearby=new List<string>();
                    foreach(var obstacle in Physics.OverlapCapsule(player.transform.position+Vector3.up*.4f,player.transform.position+Vector3.up*1.4f,.65f,~(1<<2),QueryTriggerInteraction.Ignore))
                        nearby.Add(obstacle.name);
                    throw new InvalidOperationException("Village smoke check: walking route blocked. Target="+target+" Player="+player.transform.position+" Nearby="+string.Join(", ",nearby));
                }
                player.visual.rotation=Quaternion.LookRotation(delta);
                controller.Move(delta.normalized*Mathf.Min(delta.magnitude,3.6f*Time.unscaledDeltaTime));
                yield return null;
            }
        }
        IEnumerator Run()
        {
            var player=session.player;
            yield return new WaitForSecondsRealtime(1);
            yield return VillagePresentationSmoke.CheckSigns();
            session.OpenTitle();session.AcknowledgeArrival();
            Check(session.ui.Mode=="main-menu" && !session.Progress.arrivalAcknowledged,"stale arrival callback cannot acknowledge from title");
            session.ContinueMainVisit();Check(session.ui.Mode=="arrival-intro" && player.blocked,"fresh visit starts the introduction");
            session.FinishArrivalIntro();Check(session.ui.Mode=="arrival" && session.Progress.arrivalIntroSeen && player.blocked,"skipping the introduction retains the welcome and first objective");
            session.Close();Check(session.ui.Mode=="main-menu" && player.blocked && !session.Progress.arrivalAcknowledged,"Escape at welcome returns to title without entering the village");
            session.ContinueMainVisit();session.AcknowledgeArrival();
            Check(!session.ui.IsModal && !player.blocked && session.Progress.arrivalAcknowledged,"explicit welcome action enters and acknowledges the visit");
            var store=(TeaSaveStore)typeof(TeaStallSession).GetField("saves",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(session);
            Check(session.LastSaveSucceeded && session.HasSavedVisit,"isolated arrival save failed: "+store.LastFailureDetail);
            yield return ControllerChecks();
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
            var speaker=player.view.WorldToViewportPoint(tea.eyes.position-Vector3.up*.55f);
            Check(speaker.z>0 && speaker.x>.08f && speaker.x<1-player.conversationPanelFraction-.04f,"camera frames NPC beside the chat panel");
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
            Vector3 librarianHome=librarian.transform.position;
            session.Save();librarian.transform.position+=Vector3.right*8;session.Load();
            Check(Vector3.Distance(librarian.transform.position,librarianHome)<.01f,"restore removes stale actor staging before applying saved chapter");
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
            // Take the open east side of the veranda, rather than squeezing between Somanna and his bench.
            yield return WalkTo(new Vector3(-3.5f,0,9.8f)); yield return WalkTo(new Vector3(-3.5f,0,12.3f));
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
            Check(session.Nearby=="Talk to Raghavendra","return driver focus; nearby="+session.Nearby+" mode="+session.ui.Mode+" position="+player.transform.position+" transitioning="+player.IsTransitioning);
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(0); session.ConfirmTravel();
            Check(player.transform.position.x<13 && session.Progress.checkpostVisited && session.Progress.trailPermission,"return trip preserves evidence; position="+player.transform.position+" mode="+session.ui.Mode+" activity="+session.ActiveActivity+" visited="+session.Progress.checkpostVisited+" permission="+session.Progress.trailPermission);
            yield return Shot("34-village-return"); session.Close();
            var townDriver=Array.Find(session.villagers,n=>n.role==VillageNpcRole.AutoDriver);
            session.StartConversation(townDriver); session.Choose(0); session.ConfirmTravel(); session.Close();
            player.Restore(new Vector3(48,.15f,10),0); player.blocked=true;
            yield return WalkTo(new Vector3(48,0,17.8f)); player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Read the ridge stone marker","cleared ridge reaches stone marker"); session.Interact();
            session.SubmitInvestigationAnswer(2); Check(!session.Progress.ridgeWayfindingKnown && session.farmGate.activeSelf,"wrong ridge choice leaves farm approach gated");
            session.OpenNotebook(); session.Close(); Check(session.ActiveActivity==VillageActivity.RidgeWayfinding,"notebook restores marker activity");
            session.SubmitInvestigationAnswer(0); Check(session.Progress.ridgeWayfindingKnown && !session.farmGate.activeSelf,"marker answer opens earned farm approach");
            yield return Shot("35-ridge-marker"); session.Close(); player.blocked=true;
            foreach(var point in new[]{new Vector3(48,0,20),new Vector3(46,0,21.5f),new Vector3(46,0,24.5f),new Vector3(48,0,26),new Vector3(48,0,30.5f),new Vector3(48,0,34.8f),new Vector3(48,0,38.5f)}) yield return WalkTo(point);
            Check(session.Progress.guddaManeReached && session.Progress.ridgeCrossed && !session.Progress.ananyaFound,"walking completes ridge without inventing reunion");
            player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.8f); yield return Shot("36-gudda-mane",true);
            yield return WalkTo(new Vector3(46,0,38.7f)); player.SetLookAngles(0,17); yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Talk to Ananya","farm veranda reaches Ananya"); session.Interact(); yield return new WaitForSecondsRealtime(.9f);
            Check(player.ConversationBlend>.99f && session.Turn.Id=="hello","reunion uses shared FPP framing");
            session.Choose(1); Check(!session.Progress.ananyaFound,"help does not grant reunion"); session.Choose(0);
            Check(session.Progress.ananyaFound && !session.Progress.disappearanceExplained,"meeting distinct from understood account");
            session.OpenNotebook(); session.Close(); Check(session.Turn.Id=="explain","notebook returns to direct account");
            yield return Shot("37-ananya-account"); session.Choose(0); session.Choose(1); session.Choose(0);
            Check(session.Progress.disappearanceExplained && session.Progress.farmHospitalityKnown && !session.Progress.trunkLocationKnown,"water valid without granting elder clue");
            yield return new WaitForSecondsRealtime(.9f); player.blocked=true; yield return WalkTo(new Vector3(50,0,38.7f)); player.SetLookAngles(0,17);
            yield return new WaitForSecondsRealtime(.8f); Check(session.Nearby=="Talk to Kamalamma","elder reachable on veranda");
            session.Interact(); yield return new WaitForSecondsRealtime(.9f); session.Choose(1); session.Choose(0);
            Check(session.Progress.trunkLocationKnown,"elder locates still-closed trunk"); yield return Shot("38-kamalamma-trunk"); session.Choose(0);
            session.Save(); session.Load(); Check(session.Progress.trunkLocationKnown && session.Progress.disappearanceExplained && player.transform.position.z>35,"farm save restores direct evidence and location");
            session.OpenNotebook(); yield return Shot("39-reunion-notebook"); session.Close();
            player.blocked=true;
            foreach(var point in new[]{new Vector3(48,0,38.5f),new Vector3(48,0,34.8f),new Vector3(48,0,30.5f),new Vector3(48,0,26),new Vector3(46,0,24.5f),new Vector3(46,0,21.5f),new Vector3(48,0,20),new Vector3(48,0,14)}) yield return WalkTo(point);
            Check(session.Progress.ananyaFound,"walking back preserves reunion");
            player.Restore(new Vector3(50.4f,.13f,45.6f),0);player.blocked=true;yield return new WaitForSecondsRealtime(.8f);
            Check(session.Nearby=="Inspect the family trunk","physical trunk prompt reachable");session.Interact();yield return new WaitForSecondsRealtime(.9f);
            session.TrunkAttempt="1200";session.SubmitTrunkCode();Check(!session.Progress.trunkOpened,"wrong lock grants no deed");
            session.OpenNotebook();session.Close();Check(session.ActiveActivity==VillageActivity.TrunkLock && session.TrunkAttempt=="1200","notebook preserves lock attempt");
            yield return ControllerTrunkChecks();
            session.TrunkAttempt="1207";session.SubmitTrunkCode();Check(session.Progress.trunkOpened && !session.Progress.deedRetrieved,"unlock distinct from collection");
            yield return Shot("40-trunk-open");session.OpenActivity(VillageActivity.DeedReview);
            yield return ControllerEvidenceChecks();
            session.SubmitInvestigationAnswer(0);session.SubmitInvestigationAnswer(2);session.SubmitInvestigationAnswer(1);session.ProtectDeed();
            Check(session.Progress.deedRetrieved && !session.deedPaper.activeSelf,"reviewed original protected");yield return Shot("41-original-deed");session.Close();session.Close();
            player.Restore(new Vector3(46,.13f,38.7f),0);player.blocked=true;yield return new WaitForSecondsRealtime(.8f);session.Interact();yield return new WaitForSecondsRealtime(.9f);
            session.Choose(0);session.Close();Check(!session.Progress.returnJourneyStarted,"cancelled return changes no state");session.Choose(0);session.SkipReturnJourney();
            Check(session.Progress.returnedWithAnanya && !session.Progress.fridayFinaleStarted && player.transform.position.x<13,"skip reaches village without consuming day");yield return Shot("42-return-together");session.Close();
            var companion=session.returnCompanion.GetComponent<VillageNpc>();session.StartConversation(companion);session.Choose(0);session.ConfirmFriday();
            Check(session.Progress.fridayFinaleStarted && session.finaleSet.activeSelf,"explicit Friday activates council court");yield return Shot("43-friday-court");session.Close();
            player.Restore(new Vector3(-2.2f,.1f,7.3f),0);player.blocked=true;yield return new WaitForSecondsRealtime(.8f);Check(session.Nearby=="Talk to Council president","president reachable in court");
            session.Interact();yield return new WaitForSecondsRealtime(.9f);Check(player.ConversationBlend>.99f,"president FPP transition");session.Choose(0);session.FinaleHelp=true;
            session.SubmitInvestigationAnswer(1);Check(!session.Progress.deedPresented,"booking cannot replace original at council");
            session.SubmitInvestigationAnswer(0);session.OpenNotebook();session.Close();Check(session.ActiveActivity==VillageActivity.CouncilEvidence,"notebook restores council evidence");
            session.SubmitInvestigationAnswer(1);session.SubmitInvestigationAnswer(0);session.SubmitInvestigationAnswer(2);session.RequestCouncilDecision();
            Check(session.Progress.workSuspended && !session.Progress.libraryProtected && session.worksSuspendedMarker.activeSelf,"interim suspension distinct from final protection");yield return Shot("44-council-suspension");
            session.OpenActivity(VillageActivity.ReviewOutcome);session.ConfirmReviewOutcome();
            Check(session.Progress.libraryProtected && session.Progress.epilogueStarted && session.epilogueSet.activeSelf && !session.finaleSet.activeSelf,"later review reopens library");yield return Shot("45-later-review");session.Close();
            session.StartConversation(companion);session.Choose(0);session.ChooseEnding(StoryEnding.Stay);Check(!session.Progress.storyCompleted,"ending choice still awaits explicit finish");session.FinishStory();
            Check(session.Progress.storyCompleted && session.ui.Mode=="credits","credits reached with recoverable ending");yield return Shot("46-ending-recap");
            session.ui.ShowChapterReplay();session.Close();yield return new WaitForSecondsRealtime(.1f);
            Check(session.ui.Mode=="credits" && (session.ui.GetComponent<UIDocument>().rootVisualElement.focusController.focusedElement as VisualElement)?.name=="credits-practice","chapter close returns to credits and its launch control");
            session.Close();session.ui.ShowPause();session.ui.ShowChapterReplay();session.Close();yield return new WaitForSecondsRealtime(.1f);
            Check(session.ui.Mode=="pause" && (session.ui.GetComponent<UIDocument>().rootVisualElement.focusController.focusedElement as VisualElement)?.name=="pause-practice","chapter close returns to Pause and its launch control");
            session.Close();
            session.StartPracticeVisit();Check(session.IsPracticeVisit && !session.Progress.storyCompleted && !session.Progress.deedRetrieved && !session.finaleSet.activeSelf && !session.epilogueSet.activeSelf,"practice resets staging without main story overwrite");
            session.Progress.signRead=true;session.Save();session.ReturnToCompletedStory();
            Check(!session.IsPracticeVisit && session.Progress.storyCompleted && session.Progress.libraryProtected && session.Progress.endingChoice==StoryEnding.Stay,"practice returns to original completed save");
            session.Save();session.Load();Check(session.Progress.storyCompleted && session.Progress.deedRetrieved,"final save survives restoration");
            session.OpenNotebook();yield return Shot("47-completed-notebook");session.Close();
            session.OpenTitle();Check(session.ui.Mode=="main-menu","title available from completed visit");
            session.ui.ShowChapterReplay();session.ui.ShowChapterReplay();session.Close();yield return new WaitForSecondsRealtime(.1f);
            Check(session.ui.Mode=="main-menu" && (session.ui.GetComponent<UIDocument>().rootVisualElement.focusController.focusedElement as VisualElement)?.name=="title-practice","chapter rebuild/close returns to title and its launch control");
            session.OpenTitleSettings();session.Close();Check(session.ui.Mode=="main-menu","title settings return to title");session.ContinueMainVisit();Check(!session.ui.IsModal,"continue returns to saved visit");
            session.StartChapterPractice(ReplayChapter.Deed);Check(session.IsPracticeVisit && session.Progress.trunkLocationKnown && !session.Progress.trunkOpened && player.transform.position.z>40,"chapter-select practice reaches unopened trunk");session.ReturnToCompletedStory();Check(session.Progress.storyCompleted,"chapter practice preserves main ending");
            session.BeginNewMainVisit();Check(!session.Progress.storyCompleted && !session.Progress.trunkOpened && session.ui.Mode=="arrival-intro","explicit new main visit resets story after archive");
            session.FinishArrivalIntro();
            session.OpenTitle();session.ui.ShowChapterReplay();session.Close();
            Check(session.ui.Mode=="main-menu" && !session.Progress.arrivalAcknowledged,"chapter close cannot bypass the new-story arrival gate");
            session.ContinueMainVisit();Check(session.ui.Mode=="arrival","continue still requires arrival acknowledgment");
            session.Progress.reducedMotion=true;session.Progress.invertY=true;session.Progress.masterVolume=.35f;session.Progress.ambienceVolume=.4f;session.Progress.footstepVolume=.2f;session.Progress.quality=VillageQuality.Low;session.SettingsChanged();
            Check(player.reducedMotion && player.invertY && Mathf.Abs(AudioListener.volume-.35f)<.01f,"accessibility and audio preferences applied");
            yield return null;
            Check(Mathf.Abs(session.ambience.volume-.4f)<.01f && Mathf.Abs(player.GetComponent<VillageFootsteps>().volume-.2f)<.01f,"ambience and footstep channels applied");
            session.AcknowledgeArrival();player.Restore(new Vector3(-4.3f,.2f,-1.1f),0);player.blocked=true;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            session.VillageModelTestTransport=(turn,input,token)=>System.Threading.Tasks.Task.FromResult(new VillageModelTestReply{Line=new TeaLine("","Namaskara","Welcome to the tea stall."),Choice=-1,Feedback=""});
#endif
            var vendor=Array.Find(session.villagers,n=>n.role==VillageNpcRole.TeaVendor);session.StartConversation(vendor);yield return new WaitForSecondsRealtime(.4f);
            Check(Mathf.Abs(player.view.fieldOfView-53)<.1f,"reduced motion keeps a stable conversation FOV");
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
            var authoredTurn=session.Turn;
            Check(!session.Progress.greeted && !session.Progress.deedRetrieved,"opening reply grants no story effects");
            var longReply=new System.Text.StringBuilder("<b>These characters are plain dialogue text.</b> ");for(int i=0;i<20;i++)longReply.Append("Read this response at your own pace. ");
            string replyText=longReply.ToString();
            session.VillageModelTestTransport=(turn,input,token)=>System.Threading.Tasks.Task.FromResult(new VillageModelTestReply{Line=new TeaLine("","",replyText),Choice=-1,Feedback=""});
            session.TeaAssessmentInput="Please explain that again.";session.AskTeaConversation();yield return null;
            float oldTextScale=session.Progress.textScale;session.Progress.textScale=1.3f;session.SettingsChanged();session.ui.ShowConversation();
            int oldWidth=Screen.width,oldHeight=Screen.height;var oldMode=Screen.fullScreenMode;
            Screen.SetResolution(960,540,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.5f);
            var uiRoot=session.ui.GetComponent<UIDocument>().rootVisualElement;var dialogueScroll=uiRoot.Q<ScrollView>("dialogue-scroll");
            Check(dialogueScroll!=null && dialogueScroll.contentViewport.worldBound.height>0 && dialogueScroll.verticalScroller.highValue>0,"long model response scrolls in a short window at maximum text size");
            foreach(string action in new[]{"dialogue-leave","dialogue-notebook"}) {
                var control=uiRoot.Q(action);var bounds=control.worldBound;var screen=uiRoot.worldBound;
                Check(bounds.width>0 && bounds.height>0 && bounds.xMin>=screen.xMin-1 && bounds.xMax<=screen.xMax+1 && bounds.yMin>=screen.yMin-1 && bounds.yMax<=screen.yMax+1,"fixed dialogue action remains inside the viewport: "+action);
            }
            var renderedSpeech=dialogueScroll.Q("dialogue-speech");Check(renderedSpeech!=null,"conversation exposes generated speech");
            var speechLabels=renderedSpeech.Query<Label>().ToList();
            Check(speechLabels.Exists(label=>label.text==replyText),"model reply keeps literal markup characters");
            foreach(var label in speechLabels)Check(!label.enableRichText,"model dialogue remains literal text");
            yield return Shot("49-long-response-large-text");
            session.Progress.textScale=oldTextScale;session.SettingsChanged();session.ui.ShowConversation();Screen.SetResolution(oldWidth,oldHeight,oldMode);yield return new WaitForSecondsRealtime(.5f);
            var retiredReply=new System.Threading.Tasks.TaskCompletionSource<VillageModelTestReply>();
            session.VillageModelTestTransport=(turn,input,token)=>retiredReply.Task;
            session.TeaAssessmentInput="A cancelled greeting";session.AskTeaConversation();
            Check(session.TeaAssessmentBusy,"model request can be cancelled");session.CancelTeaAssessment();
            retiredReply.SetResult(new VillageModelTestReply{Line=new TeaLine("","","Retired reply"),Choice=0,Feedback=""});yield return new WaitForSecondsRealtime(.1f);
            Check(session.Turn==authoredTurn && !session.Progress.greeted,"cancelled model reply cannot advance the story");
            session.VillageModelTestTransport=(turn,input,token)=>System.Threading.Tasks.Task.FromResult(new VillageModelTestReply{Line=new TeaLine("","","Welcome, Amogh."),Choice=0,Feedback=""});
            session.TeaAssessmentInput="Namaskara nanna hesaru Amogh";session.AskTeaConversation();yield return null;
            Check(session.Progress.greeted && session.Turn!=authoredTurn,"validated model action advances the story");
            session.VillageModelTestTransport=null;
#endif
            session.Close();session.Save();session.Load();
            Check(session.Progress.reducedMotion && session.Progress.invertY && session.Progress.quality==VillageQuality.Low && Mathf.Abs(session.Progress.ambienceVolume-.4f)<.01f && Mathf.Abs(session.Progress.footstepVolume-.2f)<.01f,"presentation and audio preferences survive restoration");
            int settingsWidth=Screen.width,settingsHeight=Screen.height;var settingsMode=Screen.fullScreenMode;float settingsScale=session.Progress.textScale;
            Screen.SetResolution(640,480,FullScreenMode.Windowed);yield return new WaitForSecondsRealtime(.4f);
            session.ui.ShowPause();session.ui.ShowPauseSection("accessibility");var menuRoot=session.ui.GetComponent<UIDocument>().rootVisualElement;
            var textSlider=menuRoot.Q<Slider>("settings-text-size");textSlider.value=.85f;yield return null;textSlider.value=1.3f;
            yield return new WaitForSecondsRealtime(.15f);
            foreach(string controlName in new[]{"pause-card","pause-back"}) {
                var controlBounds=menuRoot.Q(controlName).worldBound;var viewport=menuRoot.worldBound;
                Check(controlBounds.width>0 && controlBounds.height>0 && controlBounds.xMin>=viewport.xMin-1 && controlBounds.xMax<=viewport.xMax+1 && controlBounds.yMin>=viewport.yMin-1 && controlBounds.yMax<=viewport.yMax+1,"settings control remains inside the viewport after live scaling: "+controlName);
            }
            var settingsScroll=menuRoot.Q<ScrollView>("pause-scroll");
            Check(settingsScroll.contentViewport.worldBound.height>0 && settingsScroll.verticalScroller.highValue>0 && !settingsScroll.Contains(menuRoot.Q("pause-back")),"settings scroll retains a separate fixed Back button");
            settingsScroll.scrollOffset=new Vector2(0,settingsScroll.verticalScroller.highValue);yield return null;
            yield return Shot("50-settings-large-text");
            session.ui.ShowChapterReplay();yield return new WaitForSecondsRealtime(.15f);
            var chapterBack=menuRoot.Q("chapter-replay-back").worldBound;var chapterViewport=menuRoot.worldBound;
            Check(chapterBack.xMin>=chapterViewport.xMin-1 && chapterBack.xMax<=chapterViewport.xMax+1 && chapterBack.yMin>=chapterViewport.yMin-1 && chapterBack.yMax<=chapterViewport.yMax+1 && menuRoot.Q<ScrollView>("chapter-replay-scroll").verticalScroller.highValue>0,"chapter list scrolls beneath its visible Back control at maximum text scale");
            yield return Shot("51-chapter-menu-large-text");session.Close();
            Check(session.ui.Mode=="pause","large-text chapter selector returns to Pause");
            session.Progress.textScale=settingsScale;session.SettingsChanged();session.Close();Screen.SetResolution(settingsWidth,settingsHeight,settingsMode);
            Debug.Log("VILLAGE_MENU_LAYOUT_CHECKED");
            // Force a real Save() failure without touching the user's or isolated story's save files.
            var saveField=typeof(TeaStallSession).GetField("saves",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Check(saveField!=null,"quit regression can isolate save ownership");
            var visitStore=saveField.GetValue(session);var visitProgress=session.Progress;
            try {
                saveField.SetValue(session,new TeaSaveStore(Path.Combine(output,"failed-quit","visit.json"),new FailingSaveCodec()));
                session.OpenTitle();session.RequestQuit();yield return new WaitForSecondsRealtime(.1f);
                Check(session.ui.Mode=="quit-save-error" && !session.LastSaveSucceeded && player.blocked,"failed quit keeps the player in a modal");
                Check((menuRoot.focusController.focusedElement as VisualElement)?.name=="quit-cancel","failed quit focuses safe return rather than leaving without saving");
                session.RequestQuit();Check(session.ui.Mode=="quit-save-error" && session.QuitCancelLabel=="Back to title","retry retains original title context");
                yield return Shot("52-quit-save-failure");
                session.Close();Check(session.ui.Mode=="main-menu" && player.blocked,"Escape from failed title quit returns to title");
                session.OpenTitleSettings();session.RequestQuit();session.CancelQuit();
                Check(session.ui.Mode=="pause" && session.TitleSettings && player.blocked,"failed settings quit preserves title settings");
                session.Close();Check(session.ui.Mode=="main-menu","recovered title settings still return to title");
                session.ContinueMainVisit();session.ui.ShowPause();session.RequestQuit();session.Close();
                Check(session.ui.Mode=="pause" && !session.TitleSettings && player.blocked,"failed gameplay quit returns to Pause");
                Check(ReferenceEquals(session.Progress,visitProgress) && !session.Progress.storyCompleted,"quit failures retain the current visit");
            } finally {saveField.SetValue(session,visitStore);}
            session.Close();session.Save();Check(session.LastSaveSucceeded,"normal isolated save remains usable after failure regression");
            Debug.Log("VILLAGE_EXIT_FLOW_CHECKED");
            float previousBrightness=session.Progress.brightness;
            session.Progress.brightness=.9f;session.SettingsChanged();session.Load();
            Check(Mathf.Abs(session.Progress.brightness-.9f)<.001f,"display brightness survives native JSON save/restore");
            var displayVolumes=session.GetComponents<UnityEngine.Rendering.Volume>();
            Check(Array.Exists(displayVolumes,v=>v.sharedProfile && v.sharedProfile.TryGet<UnityEngine.Rendering.Universal.ColorAdjustments>(out var grading) && Mathf.Abs(grading.postExposure.value-.9f)<.001f),"display brightness reaches the runtime grading volume");
            bool mainGreeting=session.Progress.greeted;var mainPosition=player.transform.position;
            session.OpenTitle();yield return new WaitForSecondsRealtime(.1f);
            Check((menuRoot.focusController.focusedElement as VisualElement)?.name=="title-continue","saved visit focuses Continue on the title");
            session.ui.ShowPresentationDemo();session.Close();
            Check(session.ui.Mode=="main-menu" && !session.IsPracticeVisit,"demo selector can close without changing visit ownership");
            foreach(var demoChapter in PresentationDemo.Chapters) {
                session.ui.ShowPresentationDemo();session.StartPresentationDemo(demoChapter);
                Check(session.IsPresentationDemo && session.IsPracticeVisit && !session.Progress.storyCompleted,"demo scene is a separate prepared visit: "+demoChapter);
                Check(session.Progress.brightness==.9f,"demo copies the display preference");
                Check(session.PresentationChapter==demoChapter && menuRoot.Q("demo-arrival-card")!=null && menuRoot.Q<Button>("demo-enter")!=null,"demo arrival shows its current scene guide");
                Check(menuRoot.Query<Label>().ToList().Exists(label=>label.text==PresentationDemo.Route(demoChapter)),"demo arrival renders the presenter route literally");
                session.Close();session.OpenTitle();session.ui.ShowPause();session.ui.ShowPresentationDemo();
                session.StartPresentationDemo(demoChapter==ReplayChapter.Arrival?ReplayChapter.River:ReplayChapter.Arrival);
                Check(session.IsPresentationDemo,"switching demo scenes preserves separate save ownership");
                session.ReturnToCompletedStory();
                Check(!session.IsPracticeVisit && !session.IsPresentationDemo && session.Progress.greeted==mainGreeting && !session.Progress.riverReportKnown && !session.Progress.deedRetrieved && Vector3.Distance(player.transform.position,mainPosition)<.1f,"demo return restores the main visit without staged evidence: "+demoChapter);
            }
            session.Progress.brightness=previousBrightness;session.SettingsChanged();
            Debug.Log("VILLAGE_PRESENTATION_DEMO_CHECKED");


            Check(Time.frameCount>startFrame+50,"player actually advanced frames");
            File.WriteAllText(Path.Combine(output,"result.txt"),
                "VILLAGE_SMOKE_OK\nRender highlights, TPP/FPP transition and return, body visibility, fourteen NPC instances, complete story through protected deed, return, council decision, later review, credits and isolated practice, ridge walking, direct reunion and trunk lead, confirmed checkpost round-trip, route permission, panchayat counter and notice, temple witnesses, enterable archive, explicit day transition, river round-trip, 3D bicycle inspection, nested notebook/evidence return, booking/deduction retries and saved progress passed.\nFrames: "+(Time.frameCount-startFrame));
            Debug.Log("VILLAGE_SMOKE_OK "+output+" Frames="+(Time.frameCount-startFrame));
            Finish(0);
        }
    }
}
