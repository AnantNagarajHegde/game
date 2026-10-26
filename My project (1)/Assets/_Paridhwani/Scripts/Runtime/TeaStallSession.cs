using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Paridhwani
{
    public sealed class TeaStallSession : MonoBehaviour
    {
        public VillageWalker player;
        public Transform vendor,sign,libraryLane,busTimetable,bicycle,bicycleView,bicycleLookTarget;
        public GameObject bookingPaper,archiveDoor;
        public Transform archiveDesk;
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
        public string SaveMessage { get; private set; }="Your visit saves automatically";
        void Start()
        {
            bool smoke=Array.IndexOf(Environment.GetCommandLineArgs(),"-village-smoke")>=0;
            string path=smoke?Path.Combine(Application.temporaryCachePath,"village-smoke-"+Guid.NewGuid().ToString("N")+".json"):
                Path.Combine(Application.persistentDataPath,"paridhwani-tea-v1.json");
            saves=new TeaSaveStore(path);
            if(!smoke && !File.Exists(path))
            {
                string legacy=Path.Combine(Path.GetDirectoryName(Application.persistentDataPath),"My project (1)","paridhwani-tea-v1.json");
                var previous=new TeaSaveStore(legacy);
                if(previous.HasValidSave) saves.Write(previous.Read(out _),out _);
            }
            Progress=saves.Read(out string notice);
            if(notice!=null) SaveMessage=notice;
            player.Restore(Progress.position,Progress.heading);
            ApplyState(); ui.Bind(this); ready=true; nextSave=Time.unscaledTime+15;
            SetCursor(false);
            if(!Progress.arrivalAcknowledged && !smoke) { ui.ShowArrival(); SetCursor(true); }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(smoke) { Application.runInBackground=true; gameObject.AddComponent<VillageSmokeCheck>().Begin(this); }
#endif
        }
        void ApplyState()
        {
            if(archiveDoor) archiveDoor.SetActive(!Progress.libraryAccessGranted);
            if(bookingPaper) bookingPaper.SetActive(!Progress.bookingSlipFound);
            if(servedMeal) servedMeal.SetActive(Progress.mealOrdered);
            player.sensitivity=Progress.sensitivity; ambience.muted=Progress.muted;
            offeredTea.SetActive(Progress.drinkOrdered && Progress.drink=="tea");
            offeredWater.SetActive(Progress.drinkOrdered && Progress.drink=="water");
        }
        void Update()
        {
            if(!ready) return;
            FindInteraction();
            var keys=Keyboard.current;
            if(keys!=null && Application.isFocused)
            {
                if(ui.Mode=="arrival" && keys.enterKey.wasPressedThisFrame) AcknowledgeArrival();
                if(keys.escapeKey.wasPressedThisFrame)
                {
                    if(ui.IsModal) Close(); else {ui.ShowPause(); SetCursor(true);}
                }
                else if(keys.tabKey.wasPressedThisFrame)
                {
                    if(ui.Mode=="notebook") Close();
                    else OpenNotebook();
                }
                else if(keys.eKey.wasPressedThisFrame && !ui.IsModal) Interact();
                if(ui.Mode=="conversation" && Turn!=null)
                {
                    if(keys.digit1Key.wasPressedThisFrame) Choose(0);
                    else if(keys.digit2Key.wasPressedThisFrame) Choose(1);
                    else if(keys.digit3Key.wasPressedThisFrame) Choose(2);
                }
                else if(ui.Mode=="evidence" && ui.AcceptingEvidenceAnswers)
                {
                    if(keys.digit1Key.wasPressedThisFrame) SubmitInvestigationAnswer(0);
                    else if(keys.digit2Key.wasPressedThisFrame) SubmitInvestigationAnswer(1);
                    else if(keys.digit3Key.wasPressedThisFrame) SubmitInvestigationAnswer(2);
                }
                else if(ui.Mode=="evidence" && ui.SelectingBicycleParts)
                {
                    if(keys.digit1Key.wasPressedThisFrame) SelectBicyclePart(BicyclePart.Frame);
                    else if(keys.digit2Key.wasPressedThisFrame) SelectBicyclePart(BicyclePart.Stand);
                    else if(keys.digit3Key.wasPressedThisFrame) SelectBicyclePart(BicyclePart.CarrierPocket);
                }
                if(keys.f5Key.wasPressedThisFrame) Save();
                if(keys.f9Key.wasPressedThisFrame) Load();
            }
            ui.RefreshHUD();
            if(Time.unscaledTime>nextSave) { Save(); nextSave=Time.unscaledTime+15; }
        }
        void FindInteraction()
        {
            Nearby=null; nearbyNpc=null; nearbyInspection=0;
            if(ui.IsModal || player.IsTransitioning) return;
            float nearest=3.35f;
            if(villagers!=null) foreach(var npc in villagers)
            {
                if(!npc || !npc.eyes) continue;
                float d=Vector3.Distance(player.transform.position,npc.transform.position);
                Vector3 sight=npc.eyes.position-player.EyePosition;
                if(d>nearest || Vector3.Dot(player.view.transform.forward,sight.normalized)<.05f) continue;
                if(Physics.Linecast(player.EyePosition,npc.eyes.position,out RaycastHit hit,~(1<<2),QueryTriggerInteraction.Ignore) &&
                    hit.collider.GetComponentInParent<VillageNpc>()!=npc) continue;
                nearest=d; nearbyNpc=npc;
            }
            if(archiveDesk && Progress.libraryAccessGranted && Vector3.Distance(player.transform.position,archiveDesk.position)<2.3f && !nearbyNpc)
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
            ActiveNpc=npc; Provider=npc.role==VillageNpcRole.Librarian && Progress.guddaFamilyLeadKnown ? new ArchiveConversation() : npc.CreateProvider(); Turn=Provider.Start(Progress);
            player.BeginConversation(npc.eyes); ui.ShowConversation(); SetCursor(true);
        }
        public void Choose(int index)
        {
            if(Turn==null || index<0 || index>=Turn.Choices.Count) return;
            if(Turn.Choices[index].Activity!=VillageActivity.None) { OpenActivity(Turn.Choices[index].Activity); return; }
            bool wasThursday=Progress.thursdayStarted;
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
            ActiveActivity=activity;
            if(activity==VillageActivity.BicycleInspection) player.BeginInspection(bicycleView,bicycleLookTarget);
            else player.EndConversation();
            if(activity==VillageActivity.ArchiveReference) ui.ShowArchiveActivity(); else if(RiverActivity(activity)) ui.ShowRiverActivity(activity); else ui.ShowInvestigation(activity);
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
                case VillageActivity.ArchiveReference: correct=Progress.archiveShelfLocated?ArchiveInvestigation.ReadReference(Progress,index):ArchiveInvestigation.FindShelf(Progress,index); break;
                case VillageActivity.MarketReceipt: correct=MarketInvestigation.TryReadReceipt(Progress,index); break;
                case VillageActivity.BusTimetable: correct=MarketInvestigation.TryReadTimetable(Progress,index); break;
                case VillageActivity.BicycleInspection: if(SelectedBicyclePart==BicyclePart.CarrierPocket) correct=RiverInvestigation.FindBooking(Progress,index); break;
                case VillageActivity.BookingSlip: correct=RiverInvestigation.ReadBooking(Progress,index); break;
                case VillageActivity.RiverDeduction: correct=RiverInvestigation.Deduce(Progress,index); break;
            }
            if(correct) { activityChanged=true; ApplyState(); Save(); }
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
            notebookActivity=ui.Mode=="evidence"?ActiveActivity:VillageActivity.None;
            player.EndConversation(); ui.ShowNotebook(); SetCursor(true);
        }
        public void Close()
        {
            if(ui.Mode=="notebook" && notebookActivity!=VillageActivity.None)
            { var previous=notebookActivity; notebookActivity=VillageActivity.None; OpenActivity(previous,false); return; }
            if(ui.Mode=="evidence" && returnToNotebook)
            { returnToNotebook=false; player.EndConversation(); ui.ShowNotebook(); SetCursor(true); return; }
            if(ui.Mode=="evidence" && activityHistory.Count>0)
            { OpenActivity(activityHistory.Pop(),false); return; }
            if((ui.Mode=="notebook" || ui.Mode=="evidence") && ResumeAvailable)
            {
                if((ui.Mode=="evidence" || activityChanged) && Provider is IActivityConversationProvider activityProvider)
                    Turn=activityProvider.ResumeAfterActivity(Turn,Progress);
                ActiveActivity=VillageActivity.None; activityChanged=false;
                player.BeginConversation(ActiveNpc.eyes); ui.ShowConversation(); SetCursor(true); return;
            }
            ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation(); ui.HideModal(); SetCursor(false);
        }
        void ResetModalContext()
        { activityHistory.Clear(); notebookActivity=VillageActivity.None; ActiveActivity=VillageActivity.None; returnToNotebook=false; activityChanged=false; }
        void SetCursor(bool free)
        {
            player.blocked=free; player.SuppressLook();
            Cursor.lockState=free?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=free;
        }
        public void AcknowledgeArrival() { Progress.arrivalAcknowledged=true; Save(); Close(); }
        public void SettingsChanged() { ApplyState(); Save(); }
        public void Save()
        {
            if(Progress==null || saves==null) return;
            Progress.position=player.transform.position; Progress.heading=player.Heading;
            SaveMessage=saves.Write(Progress,out string error)?"Visit saved":error;
        }
        public void Load()
        {
            ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation();
            Progress=saves.Read(out string notice);
            player.Restore(Progress.position,Progress.heading); ApplyState();
            SaveMessage=notice??"Saved visit restored"; ui.HideModal(); SetCursor(false);
        }
        void OnApplicationFocus(bool focused) { if(!focused && ready) Save(); }
        void OnApplicationPause(bool paused) { if(paused) Save(); }
        void OnApplicationQuit() { Save(); Cursor.lockState=CursorLockMode.None; Cursor.visible=true; }
    }
}



