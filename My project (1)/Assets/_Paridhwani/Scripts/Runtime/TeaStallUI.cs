using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.TextCore.Text;

namespace Paridhwani
{
    [RequireComponent(typeof(UIDocument))]
    public sealed partial class TeaStallUI : MonoBehaviour
    {
        public StyleSheet stylesheet;
        public Font latinFont,kannadaFont;
        TeaStallSession session;
        VisualElement root,modal,prompt;
        Label objective,saveStatus,promptText,routeHint;
        FontAsset latinAsset,kannadaAsset;
        public string Mode { get; private set; } = "";
        public bool IsModal => Mode!="";
        static void Class(VisualElement e,string name) { e.AddToClassList(name); }
        public void Bind(TeaStallSession value)
        {
            session=value;
            root=GetComponent<UIDocument>().rootVisualElement;
            root.Clear(); root.styleSheets.Add(stylesheet); Class(root,"root");
            latinAsset=FontAsset.CreateFontAsset(latinFont);
            kannadaAsset=FontAsset.CreateFontAsset(kannadaFont);
            root.style.unityFontDefinition=FontDefinition.FromSDFFont(latinAsset);
            root.style.unityTextGenerator=TextGeneratorType.Advanced;
            root.pickingMode=PickingMode.Ignore;
            var top=new VisualElement(); Class(top,"top"); root.Add(top);
            var brand=new VisualElement(); top.Add(brand);
            Add(brand,"P A R I D H W A N I","brand");
            Add(brand,"A language. A place. A way to belong.","tagline");
            var place=new VisualElement(); Class(place,"place"); top.Add(place);
            Add(place,"KALYANAPURA","eyebrow");
            Add(place,"Wednesday  ·  09:00  ·  Monsoon","small");

            var task=new VisualElement(); Class(task,"task"); root.Add(task);
            Add(task,"YOUR FIRST MORNING","eyebrow");
            objective=Add(task,"","objective");
            routeHint=Add(task,"","small");
            saveStatus=Add(task,"","small");
            var controls=new VisualElement(); Class(controls,"controls"); root.Add(controls);
            Add(controls,"WASD  Walk    Shift  Jog    Mouse  Look","small");
            Add(controls,"E  Interact    Tab  Notebook    Esc  Pause","small");
            prompt=new VisualElement(); Class(prompt,"prompt"); root.Add(prompt);
            Add(prompt,"E","keycap"); promptText=Add(prompt,"","prompt-text");
            modal=new VisualElement(); Class(modal,"modal-layer"); root.Add(modal);
            modal.style.display=DisplayStyle.None;
            RefreshHUD();
        }
        public void RefreshHUD()
        {
            if(session==null || root==null) return;
            root.Q<VisualElement>(className:"task").style.display=IsModal?DisplayStyle.None:DisplayStyle.Flex;
            root.Q<VisualElement>(className:"controls").style.display=IsModal?DisplayStyle.None:DisplayStyle.Flex;
            objective.text=session.Progress.Objective; saveStatus.text=session.SaveMessage;
            routeHint.text=session.NavigationHint;
            prompt.style.display=!IsModal && session.Nearby!=null ? DisplayStyle.Flex:DisplayStyle.None;
            promptText.text=session.Nearby??"";
        }
        Label Add(VisualElement parent,string text,string cls="")
        {
            var label=new Label(text); if(cls!="") Class(label,cls);
            parent.Add(label); return label;
        }
        Label Kannada(VisualElement parent,string text,string cls="kannada")
        {
            var label=Add(parent,text,cls);
            label.style.unityFontDefinition=FontDefinition.FromSDFFont(kannadaAsset);
            label.style.unityTextGenerator=TextGeneratorType.Advanced;
            return label;
        }
        Button Button(VisualElement parent,string text,System.Action action,string cls="button")
        { var b=new Button(action){text=text}; Class(b,cls); parent.Add(b); return b; }
        VisualElement Card(string mode,string cls)
        {
            AcceptingEvidenceAnswers=false; SelectingBicycleParts=false;
            Mode=mode; modal.Clear(); modal.style.display=DisplayStyle.Flex;
            var card=new VisualElement(); Class(card,cls); modal.Add(card);
            card.style.scale=new Scale(Vector3.one*session.Progress.textScale);
            card.style.maxHeight=Length.Percent(85/session.Progress.textScale); RefreshHUD(); return card;
        }
        void Line(VisualElement parent,TeaLine line,bool choice=false)
        {
            Kannada(parent,line.Kannada,choice?"choice-kn":"kannada");
            if(session.Progress.roman) Add(parent,line.Roman,choice?"choice-ro":"roman");
            if(session.Progress.english) Add(parent,line.English,choice?"choice-en":"meaning");
        }
        public void ShowConversation()
        {
            TeaTurn t=session.Turn; if(t==null) return;
            var card=Card("conversation","conversation");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,session.ConversationTitle,"eyebrow");
            Button(header,"Leave  [Esc]",session.Close,"quiet-button");
            var speech=new VisualElement(); Class(speech,"speech"); card.Add(speech); Line(speech,t.Line);
            Add(card,"YOUR REPLY","eyebrow");
            var choices=new VisualElement(); Class(choices,"choices"); card.Add(choices);
            for(int i=0;i<t.Choices.Count;i++)
            {
                int index=i;
                var b=new Button(()=>session.Choose(index)); Class(b,"choice");
                var num=Add(b,(i+1).ToString("00"),"choice-number");
                var lines=new VisualElement(); Class(lines,"choice-lines"); b.Add(lines); Line(lines,t.Choices[i].Line,true);
                choices.Add(b);
            }
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new Toggle("English meaning"){value=session.Progress.english};
            english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue; session.SettingsChanged(); ShowConversation();});
            aids.Add(english);
            var roman=new Toggle("Roman letters"){value=session.Progress.roman};
            roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue; session.SettingsChanged(); ShowConversation();});
            aids.Add(roman);
            Add(aids,"Use 1–3 or choose a reply. Take your time.","small");
            choices.Q<Button>()?.Focus();
        }
        public void ShowInspection(string title,string kn,string ro,string body)
        {
            var card=Card("inspection","paper");
            Add(card,"A NOTE FROM THE VILLAGE","eyebrow"); Add(card,title,"title");
            Kannada(card,kn); Add(card,ro,"roman");
            // Mixed Kannada/Latin sections use the Kannada font with Latin fallback.
            var content=Kannada(card,body,"body");
            Button(card,"Return to the lane",session.Close);
        }
        public void ShowNotebook()
        {
            var card=Card("notebook","notebook");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"FIELD NOTES  /  KALYANAPURA","eyebrow"); Button(header,session.NotebookBackLabel+"  [Tab]",session.Close,"quiet-button");
            Add(card,session.Progress.thursdayStarted?"Thursday morning":"The first morning","title");
            var scroll=new ScrollView(); Class(scroll,"notebook-scroll"); card.Add(scroll);
            Add(scroll,"WORDS IN USE","section-title");
            if(session.Progress.greeted) Word(scroll,"ನಮಸ್ಕಾರ","Namaskara","Hello / a respectful greeting");
            if(session.Progress.greeted || session.Progress.drinkOrdered) { Word(scroll,"ಬೇಕು","Beku","Want / would like"); Word(scroll,"ಬೇಡ","Beda","Do not want / no, thank you"); }
            else Add(scroll,"Speak with Manjunatha to begin collecting useful phrases.","body");
            if(session.Progress.drinkOrdered)
            {
                Word(scroll,"ಚಹಾ","Chaha","Tea"); Word(scroll,"ನೀರು","Neeru","Water");
                Add(scroll,"You ordered "+(session.Progress.drink=="tea"?"tea":"water")+
                    " at Manjunatha's stall. You can ask for either; both are valid.","body");
            }
            if(session.Progress.directionsKnown)
            {
                Word(scroll,"ಎಲ್ಲಿದೆ?","Ellide?","Where is it?");
                Word(scroll,"ಮುಂದೆ ಹೋಗಿ","Munde hogi","Go straight");
                Word(scroll,"ಎಡಕ್ಕೆ ತಿರುಗಿ","Edakke tirugi","Turn left");
                Add(scroll,"THE ROUTE","section-title");
                Add(scroll,"Tea stall → straight along the lane → left at the compound wall → library.","body");
                Add(scroll,"PEOPLE & OBSERVATIONS","section-title");
                Add(scroll,"Manjunatha · Tea-stall owner. He directed you to the library.\n"+
                    "Ananya · The archivist who was meant to receive you.\n"+
                    (session.Progress.ananyaRoutineKnown ? "Observation · Manjunatha says Ananya did not visit yesterday. This alone does not explain her absence." : "You have not yet asked Manjunatha about Ananya."),"body");
            }
            if(session.Progress.metSomanna)
            {
                Add(scroll,"THE LIBRARY VERANDA","section-title");
                Word(scroll,"ದಯವಿಟ್ಟು","Dayavittu","Please"); Word(scroll,"ಕೀಲಿ","Keeli","Key");
                Add(scroll,"Somanna has the master key. Ananya carries the duplicate. He is arranging access to the reading room.","body");
            }
            if(session.Progress.messLeadKnown)
            {
                Add(scroll,"SHANTHAKKA’S MESS","section-title");
                Add(scroll,"Somanna said Ananya visited Shanthakka. The mess opens onto the other side of the main lane, towards the bus stand.","body");
            }
            if(session.Progress.mealOrdered)
            {
                Word(scroll,"ಊಟ","Oota","Meal"); Word(scroll,"ಸ್ವಲ್ಪ","Swalpa","A little");
                if(session.Progress.servingComplete) Word(scroll,"ಸಾಕು","Saaku","Enough");
            }
            if(session.Progress.shoppingNoteCollected)
            {
                Add(scroll,"ANANYA’S SHOPPING NOTE","section-title");
                Add(scroll,"Source: Shanthakka. Ananya collected two packed meals on Tuesday morning. This shopping slip was left at the mess on Monday.","body");
                if(session.Progress.marketLeadKnown) Add(scroll,"Basavaraju at the market can interpret the shopping note.","body");
                else Add(scroll,"Ask Shanthakka who can help you understand the shopping note.","body");
            }
            if(session.Progress.marketReceiptCollected)
            {
                Add(scroll,"BASAVARAJU’S LEDGER EXCERPT","section-title");
                Add(scroll,"The shopping note matches dried fruit, batteries and candles in Basavaraju’s ledger.","body");
                Button(scroll,"Inspect the ledger excerpt",()=>session.OpenActivity(VillageActivity.MarketReceipt));
                if(session.Progress.purchaseTimeKnown)
                {
                    Word(scroll,"ಸೋಮವಾರ","Somavaara","Monday"); Word(scroll,"ಸಂಜೆ","Sanje","Evening");
                    Add(scroll,"Purchase: Monday at 18:00. Source: Basavaraju’s ledger. This is not the bus departure time.","body");
                }
            }
            if(session.Progress.marketAccountKnown)
            {
                Add(scroll,"ANANYA’S PLANNED JOURNEY","section-title");
                Add(scroll,"Basavaraju says she intended to go to Gudda Gate on Tuesday morning. He did not witness her boarding.","body");
                Word(scroll,"ಮಂಗಳವಾರ","Mangalavaara","Tuesday"); Word(scroll,"ಬೆಳಿಗ್ಗೆ","Beligge","Morning");
            }
            if(session.Progress.busDepartureKnown)
            {
                Add(scroll,"TIMETABLE MATCH","section-title");
                Add(scroll,"Published service matching the plan: Route 14, Tuesday 06:45, towards Gudda Gate. Boarding remains unconfirmed.","body");
                Button(scroll,"Review the published timetable",()=>session.OpenActivity(VillageActivity.BusTimetable));
            }
            if(session.Progress.riverReportKnown)
            {
                Add(scroll,"UNVERIFIED RIVER REPORT","section-title");
                Add(scroll,"Giri’s notice beside the bus timetable reports a bicycle near the river weir. The notice alone does not prove an accident or Ananya’s whereabouts. See the direct observations below if you have inspected the bicycle.","body");
            }
            AddRiverNotebook(scroll);
            if(session.Progress.trunkOpened) { Add(scroll,"FAMILY TRUNK / OPENED","section-title"); Button(scroll,"Review the original deed",()=>session.OpenActivity(VillageActivity.DeedReview)); }
            if(session.Progress.deedConceptsReviewed) Add(scroll,"Original / 1952: education purpose; restricted commercial use; community beneficiary. Family register/signatories match the catalogue reference. Council review is still required.","body");
            if(session.Progress.deedRetrieved) Add(scroll,"Original deed protected in its folder. Assistance use does not alter its value or the story outcome.","body");
            if(session.Progress.ridgeWayfindingKnown) { Add(scroll,"RIDGE / STONE MARKER","section-title"); Add(scroll,"Turn left at the stone marker and stay beside the fence through the sheltered bend. The old road remains closed. Return by the same marked trail.","body"); }
            if(session.Progress.guddaManeReached) Add(scroll,"Reached Gudda Mane by the marked ridge. This is your own journey, distinct from earlier witness reports.","body");
            if(session.Progress.ananyaFound) { Add(scroll,"ANANYA / DIRECT REUNION","section-title"); Add(scroll,"Met Ananya under the Gudda Mane veranda. She identifies herself and says she is all right. The library's clearance is still unresolved.","body"); }
            if(session.Progress.disappearanceExplained) { Word(scroll,"ಹಿಂತಿರುಗಲು ಆಗಲಿಲ್ಲ","Hintirugalu aagalilla","Could not return"); Add(scroll,"Ananya: she came on Tuesday's bus for the family deed; the culvert washed out after arrival and the ridge was blocked. Her phone had no signal and her note to Somanna did not reach him. Kamalamma later relayed word via Kencha.","body"); }
            if(session.Progress.trunkLocationKnown) { Add(scroll,"KAMALAMMA / ORIGINAL DEED","section-title"); Add(scroll,"The closed tin trunk is inside the farm archive room. Lock reminder: twelve months, seven days. Open it, review the deed, and protect the original for council review.","body"); }
            if(session.Progress.checkpostVisited) { Add(scroll,"FOREST CHECKPOST / THURSDAY","section-title"); Add(scroll,"Raghavendra stopped on the open approach. The river road beyond the barrier is closed. Return transport remains available.","body"); }
            if(session.Progress.ananyaLocationReported) Add(scroll,"Kencha relays Kamalamma's report: a young woman in yellow matching Ananya's description reached Gudda Mane. At that stage this was relayed testimony. Any later reunion entry records the direct account separately.","body");
            if(session.Progress.checkpostAccountKnown) { Word(scroll,"ಮಳೆಯ ಕಾರಣ ರಸ್ತೆ ಮುಚ್ಚಿದೆ","Maleya kaarana raste muchchide","The road is closed because of rain"); Button(scroll,"Review closure and ridge directions",()=>session.OpenActivity(VillageActivity.CheckpostDirections)); }
            if(session.Progress.trailInstructionsUnderstood) { Word(scroll,"ಎಡಕ್ಕೆ ಹೋಗಿ","Edakke hogi","Go left"); Add(scroll,"Kencha: left at the stone marker, then follow the fence uphill. Route permission also requires a checked physical approach. The damaged road remains closed.","body"); }
            if(session.Progress.trailPermission) Add(scroll,"Checked ridge approach available. Follow the marked ridge and read the stone marker before the farmhouse approach.","body");
            if(session.Progress.noticeIdentified) { Add(scroll,"PANCHAYAT / PUBLIC NOTICE","section-title"); Add(scroll,"Signed by Shresthi: community library site listed for clearance. Proposed use: warehouse. This is a posted administrative claim, not a final ownership ruling.","body"); Button(scroll,"Reopen panchayat notice",()=>session.OpenActivity(VillageActivity.PanchayatNotice)); }
            if(session.Progress.deadlineKnown) { Word(scroll,"ಶುಕ್ರವಾರ ಬೆಳಿಗ್ಗೆ ಒಂಬತ್ತು ಗಂಟೆ","Shukravaara beligge ombattu gante","Friday at 09:00"); Add(scroll,"Source: public clearance notice. Submit original ownership documentation to the council before scheduled work begins. Reading and conversations have no countdown.","body"); }
            if(session.Progress.deedRequired) Add(scroll,"Checklist + library catalogue: seek the family's original 1952 deed at Gudda Mane for council review. A catalogue or bus booking cannot substitute for it; protection is not yet secured.","body");
            if(session.Progress.checkpostDestinationKnown) Add(scroll,"Route advisory: ask the forest checkpost about safe access before travelling to Gudda Mane. Do not bypass a closure. Ask Raghavendra at the auto pickup for a ride to the checkpost.","body");
            if(session.Progress.libraryAccessGranted) { Add(scroll,"SOMANNA / LIBRARY ACCESS","section-title"); Add(scroll,"Master-key access arranged. The catalogue points inside the second cupboard.","body"); Button(scroll,"Reopen archive catalogue",()=>session.OpenActivity(VillageActivity.ArchiveReference)); }
            if(session.Progress.deedReferenceFound) Add(scroll,"Catalogue / 1952 land record: original deed retained by Shankare Gowda's family at Gudda Mane. Ananya consulted the reference on Monday. This establishes her research target, not arrival or legal protection.","body");
            if(session.Progress.thursdayStarted) Add(scroll,"THURSDAY MORNING: rested with evidence preserved. Next lead: read the panchayat clearance notice at the public counter.","section-title");
            if(session.Progress.gurumurthyAccountKnown) { Add(scroll,"GURUMURTHY / TUESDAY MORNING","section-title"); Add(scroll,"Saw Ananya come alone towards the stop; went inside before the bus arrived. Did not see boarding.","body"); Word(scroll,"ಒಬ್ಬಳೇ ಬಂದಳು","Obbale bandalu","She came alone"); }
            if(session.Progress.rathnammaAccountKnown) { Add(scroll,"RATHNAMMA / TUESDAY MORNING","section-title"); Add(scroll,"Reports seeing Ananya board Route 14. Did not see where she got off.","body"); Word(scroll,"ಬಸ್ಸು ಹತ್ತಿದಳು","Bassu hattidalu","She boarded the bus"); }
            if(session.Progress.witnessAccountsCompared) Add(scroll,"Witness comparison: boarding had an eyewitness account; those witnesses could not establish arrival or present safety.","body");
            if(session.Progress.guddaFamilyLeadKnown) Add(scroll,"Rathnamma: Ananya asked about Shankare Gowda's family at Gudda Mane. Check this name against Somanna's records next.","body");
            Add(scroll,"NEXT STEP","section-title"); Add(scroll,session.Progress.Objective,"body");
            Add(card,"A word encountered is a beginning. Use it again in a new place.","small");
        }
        void Word(VisualElement parent,string kn,string ro,string en)
        {
            var row=new VisualElement(); Class(row,"word-row"); parent.Add(row);
            Kannada(row,kn,"word-kn"); var text=new VisualElement(); row.Add(text);
            Add(text,ro,"word-ro"); Add(text,en,"small");
        }
        public void ShowArrival()
        {
            var card=Card("arrival","paper");
            Add(card,"WEDNESDAY / YOUR FIRST MORNING","eyebrow");
            Add(card,"Welcome to Kalyanapura","title");
            Add(card,"You came to help archive the community library. Ananya was meant to meet you, but she is not at the bus stand. The tea-stall owner may know the way.","body");
            Add(card,"Walk with WASD, look with the mouse, and press E when a nearby person or sign offers a prompt. Conversations take their time. Your notebook is always one Tab away.","body");
            Button(card,"Step into the village  [Enter]",session.AcknowledgeArrival);
        }
        public void ShowPause()
        {
            var card=Card("pause","paper");
            Add(card,"PAUSE  /  UNDER THE EAVES","eyebrow"); Add(card,"A moment to yourself","title");
            Add(card,"Rain can wait. Your reading and conversations have no countdown.","body");
            Button(card,"Return to the village",session.Close);
            Button(card,"Open field notebook",ShowNotebook);
            Button(card,"Save this visit",()=>{session.Save(); ShowPause();});
            Button(card,"Restore saved visit",session.Load);
            var audio=new Toggle("Mute ambience"){value=session.Progress.muted};
            audio.RegisterValueChangedCallback(e=>{session.Progress.muted=e.newValue;session.SettingsChanged();}); card.Add(audio);
            var size=new Slider("Text size",.85f,1.3f){value=session.Progress.textScale};
            size.RegisterValueChangedCallback(e=>{session.Progress.textScale=e.newValue;card.style.scale=new Scale(Vector3.one*e.newValue);card.style.maxHeight=Length.Percent(85/e.newValue);session.SettingsChanged();}); card.Add(size);
            var sensitivity=new Slider("Look sensitivity",.05f,.3f){value=session.Progress.sensitivity};
            sensitivity.RegisterValueChangedCallback(e=>{session.Progress.sensitivity=e.newValue;session.SettingsChanged();}); card.Add(sensitivity);
            Add(card,"F5  Save visit    F9  Restore visit","small");
        }
        public void HideModal() { Mode=""; modal.Clear(); modal.style.display=DisplayStyle.None; }
        void OnDestroy() { if(latinAsset) Destroy(latinAsset); if(kannadaAsset) Destroy(kannadaAsset); }
    }
}
