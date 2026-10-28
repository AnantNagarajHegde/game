using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Paridhwani
{
    public sealed partial class TeaStallSession : MonoBehaviour
    {
        public VillageWalker player;
        public Transform vendor,sign,libraryLane,busTimetable,bicycle,bicycleView,bicycleLookTarget;
        public GameObject bookingPaper,archiveDoor;
        public Transform archiveDesk,panchayatNotice;
        public VillageNpc[] villagers;
        public GameObject offeredTea,offeredWater,servedMeal;
        public TeaStallUI ui;
        public TeaAmbience ambience;
        public TeaProgress Progress { get; private set; }
        public TeaTurn Turn { get; private set; }
        public VillageNpc ActiveNpc { get; private set; }
        public ITeaConversationProvider Provider { get; set; }=new LocalTeaConversation();
        public string Nearby { get; private set; }
        public string ConversationTitle => ActiveNpc ? ActiveNpc.displayName.ToUpperInvariant()+"  /  "+ActiveNpc.roleLabel : "CONVERSATION";
        public VillageActivity ActiveActivity { get; private set; }
        public bool ResumeAvailable => Turn!=null && ActiveNpc;
        public string NavigationHint
        {
            get
            {
                if(Progress==null || ui.IsModal) return "";
                if(Progress.epilogueStarted) return "REOPENED LIBRARY / SOMANNA OR ANANYA";
                if(Progress.workSuspended) return "COUNCIL PRESIDENT / LATER REVIEW OUTCOME";
                if(Progress.fridayFinaleStarted) return "LIBRARY COURT / COUNCIL REVIEW";
                if(Progress.returnedWithAnanya) return "ANANYA / VILLAGE PICKUP / FRIDAY PREPARATIONS";
                if(Progress.returnJourneyStarted) return AtCheckpost?"RETURN WITH ANANYA / CHECKPOST AUTO":"ANANYA / VILLAGE PICKUP";
                string ridgeHint=RidgeHint(); if(ridgeHint.Length>0) return ridgeHint;
                string journeyHint=CheckpostHint(); if(journeyHint.Length>0) return journeyHint;
                if(Progress.thursdayStarted && panchayatNotice)
                    return Progress.checkpostDestinationKnown?"Next chapter: safe travel via the forest checkpost":Mathf.CeilToInt(Vector3.Distance(player.transform.position,panchayatNotice.position))+" m / PANCHAYAT PUBLIC NOTICE";
                if(Progress.templeLeadKnown)
                {
                    var target=Progress.guddaFamilyLeadKnown?VillageNpcRole.Librarian:!Progress.gurumurthyAccountKnown?VillageNpcRole.TempleKeeper:VillageNpcRole.FlowerSeller;
                    foreach(var npc in villagers) if(npc && npc.role==target)
                        return Mathf.CeilToInt(Vector3.Distance(player.transform.position,npc.transform.position))+" m / "+npc.displayName.ToUpperInvariant();
                }
                if(Progress.riverReportKnown)
                {
                    if(Progress.giriAccountKnown && !RiverInvestigation.CanDeduce(Progress) && !Progress.riverDeductionKnown && bicycle)
                        return Mathf.CeilToInt(Vector3.Distance(player.transform.position,bicycle.position))+" m · BICYCLE UNDER THE SHED";
                    if(villagers!=null) foreach(var npc in villagers) if(npc && npc.role==VillageNpcRole.RiverKeeper)
                        return Mathf.CeilToInt(Vector3.Distance(player.transform.position,npc.transform.position))+" m · GIRI / RIVER PATH";
                }
                if(Progress.marketAccountKnown && !Progress.riverReportKnown && busTimetable)
                    return Mathf.CeilToInt(Vector3.Distance(player.transform.position,busTimetable.position))+" m · BUS-STAND TIMETABLE";
                VillageNpcRole role=Progress.marketLeadKnown?VillageNpcRole.MarketVendor:Progress.messLeadKnown?VillageNpcRole.MessOwner:
                    Progress.directionsKnown?VillageNpcRole.Librarian:VillageNpcRole.TeaVendor;
                if(villagers!=null) foreach(var npc in villagers) if(npc && npc.role==role)
                    return Mathf.CeilToInt(Vector3.Distance(player.transform.position,npc.transform.position))+" m · "+npc.roleLabel;
                return "";
            }
        }
        readonly Stack<VillageActivity> activityHistory=new Stack<VillageActivity>();
        VillageActivity notebookActivity;
        bool returnToNotebook,activityChanged;
        public BicyclePart SelectedBicyclePart { get; private set; }=BicyclePart.None;
        public string EvidenceBackLabel => returnToNotebook?"Back to field notes":activityHistory.Count>0?(activityHistory.Peek()==VillageActivity.BicycleInspection?"Back to bicycle":"Back to previous evidence"):ResumeAvailable?"Back to conversation":"Return to village";
        public string NotebookBackLabel => notebookActivity!=VillageActivity.None?"Back to evidence":ResumeAvailable?"Back to conversation":"Close";
        TeaSaveStore saves;
        VillageNpc nearbyNpc;
        int nearbyInspection;
        float nextSave;
        bool ready;
        public bool LastSaveSucceeded {get;private set;}=true;
        public bool IsIsolatedTestVisit {get;private set;}
        public bool IsAutomatedTest {get;private set;}
#if UNITY_EDITOR
        public const string IsolatedTestKey="Paridhwani.IsolatedEditorPlaytest", AutomatedTestKey="Paridhwani.AutomatedEditorPlaytest";
#endif
        public string SaveMessage { get; private set; }="Your visit saves automatically";
        void Start()
        {
            if(!RuntimeWorldReady()) {
                Progress=new TeaProgress();player.enabled=false;player.GetComponent<CharacterController>().enabled=false;ui.Bind(this);ui.ShowWorldUpdateRequired();SetCursor(true);Debug.LogError("PARIDHWANI_WORLD_OUTDATED: regenerate the world before playing; no saved visit was read or changed.");return;
            }
            bool smoke=Debug.isDebugBuild && Array.IndexOf(Environment.GetCommandLineArgs(),"-village-smoke")>=0;
#if UNITY_EDITOR
            smoke|=UnityEditor.SessionState.GetBool(AutomatedTestKey,false);
#endif
            IsAutomatedTest=smoke;
            IsIsolatedTestVisit=smoke;
#if UNITY_EDITOR
            IsIsolatedTestVisit|=UnityEditor.SessionState.GetBool(IsolatedTestKey,false);
#endif
            string path=IsIsolatedTestVisit?Path.Combine(Application.temporaryCachePath,"village-playtest-"+Guid.NewGuid().ToString("N")+".json"):
                Path.Combine(Application.persistentDataPath,"paridhwani-tea-v1.json");
            saves=new TeaSaveStore(path);
            if(!IsIsolatedTestVisit && !saves.HasAnySaveFiles)
            {
                string legacy=Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"My project (1)","paridhwani-tea-v1.json");
                var previous=new TeaSaveStore(legacy);
                if(previous.HasValidSave) saves.Write(previous.Read(out _),out _);
            }
            Progress=saves.Read(out string notice);
            if(notice!=null) SaveMessage=notice;
            player.Restore(Progress.position,Progress.heading);returnTrail.Reset(Progress.position);
            CaptureActorOrigins(); BindCharacterPresentation(); ApplyState(); ui.Bind(this); ready=true; nextSave=Time.unscaledTime+15;
            SetCursor(false);
            if(!smoke) { ui.ShowMainMenu(); SetCursor(true); }
            if(Debug.isDebugBuild)
            {
                if(Array.IndexOf(Environment.GetCommandLineArgs(),"-village-profile")>=0) gameObject.AddComponent<VillageFrameProfile>();
                if(smoke) gameObject.AddComponent<VillageSmokeCheck>().Begin(this);
            }
        }
        void ApplyState()
        {
            ApplyPresentation();
            ApplyCheckpostState();
            ApplyDeedState();
            ApplyReturnState();
            ApplyFinaleState();
            ApplyEpilogueState();
            if(farmGate) farmGate.SetActive(!Progress.ridgeWayfindingKnown || !Progress.trailPermission);
            if(archiveDoor) archiveDoor.SetActive(!Progress.libraryAccessGranted);
            if(bookingPaper) bookingPaper.SetActive(!Progress.bookingSlipFound);
            if(servedMeal) servedMeal.SetActive(Progress.mealOrdered);
            player.sensitivity=Progress.sensitivity; ambience.muted=Progress.muted; ambience.volume=Progress.ambienceVolume; ambience.footstepVolume=Progress.footstepVolume;
            offeredTea.SetActive(Progress.drinkOrdered && Progress.drink=="tea");
            offeredWater.SetActive(Progress.drinkOrdered && Progress.drink=="water");
        }
        void Update()
        {
            if(!ready) return;
            UpdateCharacterPresentation();
            UpdateReturnJourney();
            UpdateFarmArrival();
            FindInteraction();
            HandleKeyboard();
            ui.RefreshHUD();
            if(Time.unscaledTime>nextSave && ui.Mode!="main-menu" && ui.Mode!="new-visit-confirm" && !TitleSettings) { Save(); nextSave=Time.unscaledTime+15; }
        }
        void HandleKeyboard()
        {
            var keys=Keyboard.current;
            if(keys==null || !Application.isFocused) return;
            // One command per frame: a close/restore must not also answer a newly shown turn.
            if(keys.escapeKey.wasPressedThisFrame)
            {
                if(ui.IsModal) Close(); else { ui.ShowPause(); SetCursor(true); }
                return;
            }
            bool editing=ui.HasTextEntryFocus;
            if(ui.Mode=="arrival" && keys.enterKey.wasPressedThisFrame && !editing)
            { AcknowledgeArrival(); return; }
            if(keys.tabKey.wasPressedThisFrame && VillageInputPolicy.NotebookShortcut(ui.Mode,editing))
            { if(ui.Mode=="notebook") Close(); else OpenNotebook(); return; }
            if(VillageInputPolicy.SaveShortcut(ui.Mode,editing,TitleSettings))
            {
                if(keys.f5Key.wasPressedThisFrame) { Save(); return; }
                if(keys.f9Key.wasPressedThisFrame) { Load(); return; }
            }
            if(keys.eKey.wasPressedThisFrame && !ui.IsModal) { Interact(); return; }
            if(!VillageInputPolicy.ChoiceShortcut(ui.Mode,editing)) return;
            int choice=keys.digit1Key.wasPressedThisFrame?0:keys.digit2Key.wasPressedThisFrame?1:keys.digit3Key.wasPressedThisFrame?2:-1;
            if(choice<0) return;
            if(ui.Mode=="conversation" && Turn!=null) Choose(choice);
            else if(ui.AcceptingEvidenceAnswers) SubmitInvestigationAnswer(choice);
            else if(ui.SelectingBicycleParts) SelectBicyclePart(choice==0?BicyclePart.Frame:choice==1?BicyclePart.Stand:BicyclePart.CarrierPocket);
        }
        void FindInteraction()
        {
            Nearby=null; nearbyNpc=null; nearbyInspection=0;
            if(ui.IsModal || player.IsTransitioning) return;
            float nearest=3.35f;
            if(villagers!=null) foreach(var npc in villagers)
            {
                if(!npc || !npc.eyes || !npc.gameObject.activeInHierarchy) continue;
                float d=Vector3.Distance(player.transform.position,npc.transform.position);
                Vector3 sight=npc.eyes.position-player.EyePosition;
                if(d>nearest || Vector3.Dot(player.view.transform.forward,sight.normalized)<.05f) continue;
                if(Physics.Linecast(player.EyePosition,npc.eyes.position,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<VillageNpc>()!=npc) continue;
                nearest=d; nearbyNpc=npc;
            }
            if(FindDeedInteraction()) return;
            if(FindRidgeInteraction()) return;
            if(FindCheckpostInteraction()) return;
            if(panchayatNotice && Vector3.Distance(player.transform.position,panchayatNotice.position)<2.3f && !nearbyNpc)
            { Nearby="Read the panchayat notice"; nearbyInspection=6; }
            else if(archiveDesk && Progress.libraryAccessGranted && Vector3.Distance(player.transform.position,archiveDesk.position)<2.3f && !nearbyNpc)
            { Nearby="Read the archive catalogue"; nearbyInspection=5; }
            else if(bicycle && Vector3.Distance(player.transform.position,bicycle.position)<2.5f && !nearbyNpc)
            { Nearby="Inspect the bicycle"; nearbyInspection=4; }
            else if(busTimetable && Vector3.Distance(player.transform.position,busTimetable.position)<2.3f && !nearbyNpc)
            { Nearby="Read the bus timetable"; nearbyInspection=3; }
            else if(nearbyNpc) Nearby="Talk to "+nearbyNpc.displayName;
            else if(sign && Vector3.Distance(player.transform.position,sign.position)<2.4f)
            { Nearby="Read the tea-stall sign"; nearbyInspection=1; }
            else if(libraryLane && Vector3.Distance(player.transform.position,libraryLane.position)<2.3f)
            { Nearby="Read the library notice"; nearbyInspection=2; }
        }
        public void Interact()
        {
            if(nearbyNpc) { StartConversation(nearbyNpc); return; }
            if(nearbyInspection==10) { OpenActivity(VillageActivity.TrunkLock); return; }
            if(nearbyInspection==9) { OpenActivity(VillageActivity.RidgeWayfinding); return; }
            if(nearbyInspection==7) { OpenActivity(VillageActivity.CheckpostDirections); return; }
            if(nearbyInspection==8) { OpenActivity(VillageActivity.RidgeTrailhead); return; }
            if(nearbyInspection==6) { OpenActivity(VillageActivity.PanchayatNotice); return; }
            if(nearbyInspection==5) { OpenActivity(VillageActivity.ArchiveReference); return; }
            if(nearbyInspection==4) { OpenActivity(VillageActivity.BicycleInspection); return; }
            if(nearbyInspection==3) { OpenActivity(VillageActivity.BusTimetable); return; }
            if(nearbyInspection==1)
            {
                Progress.signRead=true; Save();
                ui.ShowInspection("Manjunatha's tea stall","ಮಂಜುನಾಥ ಚಹಾ ಅಂಗಡಿ","Manjunatha chaha angadi",
                    "Manjunatha's tea shop\n\nಚಹಾ · Chaha · Tea\nನೀರು · Neeru · Water\n\nReturn to the sign whenever you want to practise.");
            }
            else if(nearbyInspection==2)
                ui.ShowInspection("The community library","ಗ್ರಂಥಾಲಯ","Granthalaya",
                    Progress.libraryAccessGranted?"The reading room is open. Read the archive catalogue at the desk inside.":"The reading room is closed for now.\n\nSomanna, the librarian, is waiting on the veranda. Greet him and ask about Ananya.");
            else return;
            SetCursor(true);
        }
        public void StartConversation(VillageNpc npc)
        {
            if(!npc || !npc.eyes) return;
            activityHistory.Clear(); notebookActivity=VillageActivity.None; returnToNotebook=false; activityChanged=false;
            NewDialogueTurn(); ActiveNpc=npc; Provider=CreateConversation(npc); Turn=Provider.Start(Progress);
            player.BeginConversation(npc.eyes); ui.ShowConversation(); SetCursor(true);
        }
        public void Choose(int index)
        {
            if(Turn==null || index<0 || index>=Turn.Choices.Count) return;
            if(Turn.Choices[index].Activity!=VillageActivity.None) { OpenActivity(Turn.Choices[index].Activity); return; }
            NewDialogueTurn(); bool wasThursday=Progress.thursdayStarted;
            Turn=Provider.Reply(Turn,index,Progress); ApplyState(); Save();
            if(!wasThursday && Progress.thursdayStarted) { ui.ShowRestTransition(); return; }
            if(Turn==null) Close(); else ui.ShowConversation();
        }
        static bool RiverActivity(VillageActivity a) => a==VillageActivity.BicycleInspection || a==VillageActivity.BookingSlip || a==VillageActivity.RiverDeduction;
        public void OpenActivity(VillageActivity activity,bool remember=true)
        {
            if(activity==VillageActivity.ArchiveReference && !Progress.libraryAccessGranted) return;
            if(activity==VillageActivity.None || (activity==VillageActivity.MarketReceipt && !Progress.marketReceiptCollected) ||
                (activity==VillageActivity.BookingSlip && !Progress.bookingSlipFound) ||
                (activity==VillageActivity.RiverDeduction && !RiverInvestigation.CanDeduce(Progress))) return;
            if(activity==VillageActivity.BicycleInspection && (!bicycleView || !bicycleLookTarget)) return;
            if(ui.Mode=="notebook" && remember) returnToNotebook=true;
            if(remember && ui.Mode=="evidence" && ActiveActivity!=VillageActivity.None && ActiveActivity!=activity) activityHistory.Push(ActiveActivity);
            if(activity==VillageActivity.BicycleInspection && remember && ui.Mode!="evidence") SelectedBicyclePart=BicyclePart.None;
            SuspendDialogueText(); ActiveActivity=activity;
            if(activity==VillageActivity.ReturnTogether || activity==VillageActivity.FridayPreparation) { player.EndConversation(); ui.ShowReturnActivity(activity); SetCursor(true); return; }
            if(activity==VillageActivity.CouncilEvidence) { player.EndConversation(); ui.ShowCouncilActivity(); SetCursor(true); return; }
            if(activity==VillageActivity.ReviewOutcome || activity==VillageActivity.EndingReflection) { player.EndConversation(); if(activity==VillageActivity.ReviewOutcome) ui.ShowReviewOutcome(); else ui.ShowEndingReflection(); SetCursor(true); return; }
            if(OpenDeedActivity(activity)) { SetCursor(true); return; }
            if(activity==VillageActivity.BicycleInspection) player.BeginInspection(bicycleView,bicycleLookTarget);
            else player.EndConversation();
            if(activity==VillageActivity.RidgeWayfinding) ui.ShowRidgeActivity(); else if(activity==VillageActivity.CheckpostTravel || activity==VillageActivity.VillageReturn || activity==VillageActivity.CheckpostDirections || activity==VillageActivity.RidgeTrailhead) ui.ShowCheckpostActivity(activity); else if(activity==VillageActivity.PanchayatNotice) ui.ShowPanchayatActivity(); else if(activity==VillageActivity.ArchiveReference) ui.ShowArchiveActivity(); else if(RiverActivity(activity)) ui.ShowRiverActivity(activity); else ui.ShowInvestigation(activity);
            SetCursor(true);
        }
        public void SelectBicyclePart(BicyclePart part)
        {
            if(ui.Mode!="evidence" || ActiveActivity!=VillageActivity.BicycleInspection || !RiverInvestigation.Observe(Progress,part)) return;
            SelectedBicyclePart=part; activityChanged=true; Save(); ui.ShowRiverActivity(ActiveActivity);
        }
        public void SubmitInvestigationAnswer(int index)
        {
            if(ui.Mode!="evidence" || !ui.AcceptingEvidenceAnswers) return;
            bool correct=false;
            switch(ActiveActivity)
            {
                case VillageActivity.CheckpostDirections: case VillageActivity.RidgeTrailhead: correct=CheckpostInvestigation.Answer(Progress,index,ridgeRouteChecked); break;
                case VillageActivity.RidgeWayfinding: correct=RidgeInvestigation.Answer(Progress,index,ridgeRouteChecked,ridgeMarker && Vector3.Distance(player.transform.position,ridgeMarker.position)<3); break;
                case VillageActivity.DeedReview: correct=DeedInvestigation.Answer(Progress,index); break;
                case VillageActivity.CouncilEvidence: correct=FinaleInvestigation.Answer(Progress,index,AtCouncil); break;
                case VillageActivity.PanchayatNotice: correct=PanchayatInvestigation.Answer(Progress,index); break;
                case VillageActivity.ArchiveReference: correct=Progress.archiveShelfLocated?ArchiveInvestigation.ReadReference(Progress,index):ArchiveInvestigation.FindShelf(Progress,index); break;
                case VillageActivity.MarketReceipt: correct=MarketInvestigation.TryReadReceipt(Progress,index); break;
                case VillageActivity.BusTimetable: correct=MarketInvestigation.TryReadTimetable(Progress,index); break;
                case VillageActivity.BicycleInspection: if(SelectedBicyclePart==BicyclePart.CarrierPocket) correct=RiverInvestigation.FindBooking(Progress,index); break;
                case VillageActivity.BookingSlip: correct=RiverInvestigation.ReadBooking(Progress,index); break;
                case VillageActivity.RiverDeduction: correct=RiverInvestigation.Deduce(Progress,index); break;
            }
            if(correct) { activityChanged=true; ApplyState(); Save(); }
            if(ActiveActivity==VillageActivity.CheckpostDirections || ActiveActivity==VillageActivity.RidgeTrailhead) { ui.ShowCheckpostActivity(ActiveActivity,correct?"Directions recorded. Check the route status before proceeding.":"Edakke means left. Follow the fence; the damaged road and river are closed."); return; }
            if(ActiveActivity==VillageActivity.RidgeWayfinding) { ui.ShowRidgeActivity(correct?"Direction recorded. Continue beside the fence.":"Edakke means left. Stay beside the fence; do not climb it or use the old road."); return; }
            if(ActiveActivity==VillageActivity.DeedReview) { ui.ShowDeedActivity(ActiveActivity,correct?"Clause recorded with the original.":"Read the highlighted clause: education, restricted commercial use, or community beneficiary. Try again."); return; }
            if(ActiveActivity==VillageActivity.CouncilEvidence) { ui.ShowCouncilActivity(correct?"Evidence presented and recorded.":"Consult the reviewed original or ask Ananya for help. Try the clause again."); return; }
            if(ActiveActivity==VillageActivity.PanchayatNotice) { ui.ShowPanchayatActivity(correct?"Recorded with its source in your field notes.":!Progress.noticeIdentified?"Granthalaya means library. Match the named place.":!Progress.deadlineKnown?"Shukravaara is Friday; beligge means morning; ombattu is nine.":"Moola patra is the original document. The catalogue identifies it but cannot replace it."); return; }
            if(ActiveActivity==VillageActivity.ArchiveReference) { ui.ShowArchiveActivity(correct?"Recorded with its source.":Progress.archiveShelfLocated?"Moola patra means original document. A catalogue is not proof of arrival.":"Eradane means second; olage means inside. Try again."); return; }
            string hint=correct?"Recorded with its source in your field notes.":
                ActiveActivity==VillageActivity.MarketReceipt?"Check the weekday and sanje (evening). This is the purchase, not the departure.":
                ActiveActivity==VillageActivity.BusTimetable?"Compare Tuesday with Monday, and morning with evening. Look for Route 14.":
                ActiveActivity==VillageActivity.BicycleInspection?"Olage means inside; mele means on top; horage means outside. Look inside the pocket below the carrier.":
                ActiveActivity==VillageActivity.BookingSlip?"An advance booking records an intended journey. Look for a boarding stamp before claiming actual travel.":
                "Separate observation from inference: an upright bicycle and an advance booking are not a witness to an accident or boarding.";
            if(RiverActivity(ActiveActivity)) ui.ShowRiverActivity(ActiveActivity,hint); else ui.ShowInvestigation(ActiveActivity,hint);
        }
        public void ReadRiverNotice()
        { if(!MarketInvestigation.ReadRiverNotice(Progress)) return; Save(); ui.ShowRiverNotice(); }
        public void OpenNotebook()
        {
            SuspendDialogueText();
            notebookActivity=ui.Mode=="evidence"?ActiveActivity:VillageActivity.None;
            player.EndConversation(); ui.ShowNotebook(); SetCursor(true);
        }
        public void Close()
        {
            if(ui.Mode=="presentation-demo"){ui.ClosePresentationDemo();return;}
            if(ui.Mode=="arrival"){OpenTitle();return;}
            if(ui.Mode=="quit-save-error"){CancelQuit();return;}
            if(ui.Mode=="world-update-required")return;
            if(ui.Mode=="chapter-replay"){ui.CloseChapterReplay();return;}
            if(ui.Mode=="main-menu"){if(Progress.arrivalAcknowledged)ContinueMainVisit();return;}
            if(ui.Mode=="new-visit-confirm"){ui.ShowMainMenu();return;}
            if(ui.Mode=="pause" && TitleSettings){TitleSettings=false;ui.ShowMainMenu();return;}
            if(ui.Mode=="notebook" && notebookActivity!=VillageActivity.None)
            { var previous=notebookActivity; notebookActivity=VillageActivity.None; OpenActivity(previous,false); return; }
            if(ui.Mode=="evidence" && returnToNotebook)
            { returnToNotebook=false; player.EndConversation(); ui.ShowNotebook(); SetCursor(true); return; }
            if(ui.Mode=="evidence" && activityHistory.Count>0)
            { OpenActivity(activityHistory.Pop(),false); return; }
            if((ui.Mode=="notebook" || ui.Mode=="evidence") && ResumeAvailable)
            {
                if((ui.Mode=="evidence" || activityChanged) && Provider is IActivityConversationProvider activityProvider)
                    {Turn=activityProvider.ResumeAfterActivity(Turn,Progress);NewDialogueTurn();}
                ActiveActivity=VillageActivity.None; activityChanged=false;
                player.BeginConversation(ActiveNpc.eyes); ui.ShowConversation(); SetCursor(true); return;
            }
            ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation(); ui.HideModal(); SetCursor(false);
        }
        void ResetModalContext()
        { NewDialogueTurn(); activityHistory.Clear(); notebookActivity=VillageActivity.None; ActiveActivity=VillageActivity.None; returnToNotebook=false; activityChanged=false; }
        void SetCursor(bool free)
        {
            player.blocked=free; player.SuppressLook();
            Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=free;
        }
        public void AcknowledgeArrival() {
            if(!VisitExitNavigation.CanAcknowledgeArrival(ui.Mode))return;
            Progress.arrivalAcknowledged=true;Save();EndForJourney();
        }
        public void SettingsChanged() { ApplyState(); Save(); }
        public void Save()
        {
            if(skipQuitSave || Progress==null || saves==null) return;
            Progress.position=player.transform.position; Progress.heading=player.Heading;
            LastSaveSucceeded=saves.Write(Progress,out string error);
            SaveMessage=LastSaveSucceeded?"Visit saved":error;
        }
        public void Load() { RestoreVisit(saves,false); }
        bool RestoreVisit(TeaSaveStore source,bool returningToMain)
        {
            if(!source.TryRestore(out var restored,out string notice))
            {
                SuspendDialogueText();
                SaveMessage="Saved visit could not be restored. Your current visit is still open.";
                ui.ShowRestoreError(source.LastReadStatus==TeaSaveReadStatus.Missing);SetCursor(true);return false;
            }
            // Switch save ownership only after a valid read. Failed main-visit returns stay in practice.
            saves=source;if(returningToMain) completedStoryStore=null;
            ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation();ResetActorsToOrigins();
            Progress=restored;
            player.Restore(Progress.position,Progress.heading);returnTrail.Reset(Progress.position);ApplyState();
            SaveMessage=notice??"Saved visit restored";
            if(Progress.arrivalAcknowledged) {ui.HideModal();SetCursor(false);}
            else {ui.ShowArrival();SetCursor(true);}
            return true;
        }
        void OnApplicationFocus(bool focused)
        {
            if(focused || !ready) return;
            Save();
            if(!ui.IsModal && !IsAutomatedTest) { ui.ShowPause(); SetCursor(true); }
        }
        void OnApplicationPause(bool paused) { if(paused) Save(); }
        void OnApplicationQuit() { Save(); Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
}
