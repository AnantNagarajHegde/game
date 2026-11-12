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
        TeaTurn displayedTurn;
        bool displayedPhrases;
        sealed class ChatMessage { public bool player,pending,failed; public TeaLine line;public string deliveryId; }
        readonly System.Collections.Generic.List<ChatMessage> chatMessages=new System.Collections.Generic.List<ChatMessage>();
        TeaTurn chatTurn; TeaLine chatLine; bool chatAutoScroll; Vector2 chatScrollPosition;
        public void BeginChat() {chatMessages.Clear();chatTurn=null;chatLine=null;displayedTurn=null;chatAutoScroll=true;chatScrollPosition=Vector2.zero;}
        internal int ChatMessageCount=>chatMessages.Count;
        public void RecordChatMessage(string message,string deliveryId=null) {
            if(string.IsNullOrWhiteSpace(message))return;
            ChatMessage existing=string.IsNullOrEmpty(deliveryId)?null:chatMessages.Find(item=>item.deliveryId==deliveryId);
            if(existing!=null){existing.pending=true;existing.failed=false;}
            else chatMessages.Add(new ChatMessage{player=true,line=new TeaLine("","",message.Trim()),deliveryId=deliveryId,pending=!string.IsNullOrEmpty(deliveryId)});
            chatAutoScroll=true;
        }
        public void CompleteChatMessage(string deliveryId,bool delivered) {
            var message=chatMessages.Find(item=>item.deliveryId==deliveryId && !string.IsNullOrEmpty(deliveryId));
            if(message!=null){message.pending=false;message.failed=!delivered;}
        }
        public void RecordChatChoice(TeaLine line) {if(line!=null){chatMessages.Add(new ChatMessage{player=true,line=new TeaLine(line.Kannada,line.Roman,line.English)});chatAutoScroll=true;}}
        void RecordChatReply(TeaTurn turn,TeaLine line) {
            if(line==null)return;
            if(!object.ReferenceEquals(chatTurn,turn) || (!object.ReferenceEquals(line,turn.Line) && !object.ReferenceEquals(chatLine,line))) {
                chatMessages.Add(new ChatMessage{line=new TeaLine(line.Kannada,line.Roman,line.English)});chatTurn=turn;chatLine=line;chatAutoScroll=true;
                session.SpeakConversationLine(line);
            }
        }
        public void RecordCurrentChatReply(TeaLine line){RecordChatReply(session.Turn,line);}
        Label objective,saveStatus,promptText,routeHint,storyDay,placeDay;
        FontAsset latinAsset,kannadaAsset;
        PanelSettings originalPanel,ownedPanel;
        public string Mode { get; private set; } = "";
        public bool IsModal => Mode!="";
        public bool HasTextEntryFocus
        {
            get
            {
                var focused=root?.focusController?.focusedElement as VisualElement;
                for(var element=focused;element!=null;element=element.parent)
                    if(element is TextField) return true;
                return false;
            }
        }
        static void Class(VisualElement e,string name) { e.AddToClassList(name); }
        public void Bind(TeaStallSession value)
        {
            session=value;
            var document=GetComponent<UIDocument>();
            originalPanel=document.panelSettings;
            ownedPanel=Instantiate(originalPanel);
            ownedPanel.name="Paridhwani runtime interface";
            ownedPanel.referenceResolution=new Vector2Int(1280,720);
            document.panelSettings=ownedPanel;
            root=document.rootVisualElement;
            root.Clear(); root.styleSheets.Add(stylesheet); Class(root,"root");
            latinAsset=FontAsset.CreateFontAsset(latinFont);
            kannadaAsset=FontAsset.CreateFontAsset(kannadaFont);
            root.style.unityFontDefinition=FontDefinition.FromSDFFont(latinAsset);
            root.style.unityTextGenerator=TextGeneratorType.Advanced;
            root.pickingMode=PickingMode.Ignore;
            root.RegisterCallback<GeometryChangedEvent>(e=>{
                root.EnableInClassList("compact",Screen.width<1100 || Screen.height<650 || e.newRect.width<1100);
                var conversation=root.Q("dialogue-card");if(conversation!=null)LayoutConversation(conversation);
            });
            var top=new VisualElement(); Class(top,"top"); root.Add(top);
            var brand=new VisualElement(); top.Add(brand);
            Add(brand,"PARIDHWANI","brand");
            // Keep the exploration brand quiet; the objective is the primary information.
            var place=new VisualElement(); Class(place,"place"); top.Add(place);
            Add(place,"KALYANAPURA","eyebrow");
            placeDay=Add(place,"Wednesday / Monsoon","small");

            var task=new VisualElement(); Class(task,"task"); root.Add(task);
            storyDay=Add(task,"YOUR FIRST MORNING","eyebrow");
            objective=Add(task,"","objective");
            routeHint=Add(task,"","small");
            saveStatus=Add(task,"","small");Class(saveStatus,"save-status");
            Class(routeHint,"route-hint");
            var controls=new VisualElement(); Class(controls,"controls"); root.Add(controls);
            controlsWalk=Add(controls,"","small");
            controlsActions=Add(controls,"","small");
            prompt=new VisualElement(); Class(prompt,"prompt"); root.Add(prompt);
            prompt.name="interaction-prompt";
            Add(prompt,"Press","prompt-prefix");
            promptKey=Add(prompt,"E","keycap"); promptText=Add(prompt,"","prompt-text");
            modal=new VisualElement(); Class(modal,"modal-layer"); root.Add(modal);
            modal.style.display=DisplayStyle.None;
            controllerHelp=Add(root,"","controller-help");controllerHelp.pickingMode=PickingMode.Ignore;
            if(!GetComponent<VillageTitleBackdrop>())gameObject.AddComponent<VillageTitleBackdrop>().Bind(session);
            BindControllerNavigation();
            RefreshHUD();
        }
        public void RefreshHUD()
        {
            if(session==null || root==null) return;
            RefreshControllerHints();
            root.Q<VisualElement>(className:"top").style.display=IsModal?DisplayStyle.None:DisplayStyle.Flex;
            root.Q<VisualElement>(className:"task").style.display=IsModal?DisplayStyle.None:DisplayStyle.Flex;
            root.Q<VisualElement>(className:"controls").style.display=IsModal?DisplayStyle.None:DisplayStyle.Flex;
            string day=session.Progress.epilogueStarted?"SOME WEEKS LATER":session.Progress.fridayFinaleStarted?"FRIDAY / BEFORE 09:00":session.Progress.thursdayStarted?"THURSDAY":"WEDNESDAY";
            storyDay.text=(session.IsPracticeVisit?"PRACTICE / ":session.IsIsolatedTestVisit?"TEST VISIT / ":"")+day;
            placeDay.text=VillageWeatherPolicy.Raining(session.Progress)?"MONSOON / RAIN":"AFTER THE RAIN";
            objective.text=session.Progress.Objective; saveStatus.text=session.SaveMessage;
            saveStatus.style.display=session.LastSaveSucceeded?DisplayStyle.None:DisplayStyle.Flex;
            routeHint.text=session.NavigationHint;
            prompt.style.display=!IsModal && session.Nearby!=null ? DisplayStyle.Flex:DisplayStyle.None;
            string action=session.Nearby??"";
            promptText.text=action.Length==0?"":"to "+char.ToLowerInvariant(action[0])+action.Substring(1);
        }
        Label Add(VisualElement parent,string text,string cls="")
        {
            var label=new Label(text){enableRichText=false}; if(cls!="") Class(label,cls);
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
            if(mode!="arrival-intro")session.CancelArrivalIntro();
            if(Mode=="conversation") {var oldChat=root.Q<ScrollView>("dialogue-scroll");if(oldChat!=null)chatScrollPosition=oldChat.scrollOffset;}
            if(Mode!=mode)session.PlayControllerFeedback(ControllerFeedback.Menu);
            CloseTeaKeyboard();
            var restoreFocus=PreserveModalFocus(mode);
            AcceptingEvidenceAnswers=false; SelectingBicycleParts=false;
            Mode=mode; modal.Clear(); modal.style.display=DisplayStyle.Flex;
            modal.EnableInClassList("chat-modal",mode=="conversation");
            modal.EnableInClassList("title-modal",mode=="main-menu");
            modal.EnableInClassList("intro-modal",mode=="arrival-intro");
            var card=new VisualElement(); Class(card,cls); modal.Add(card);
            SuppressControllerSubmit();
            card.schedule.Execute(()=> {if(modal.Contains(card)){restoreFocus?.Invoke();EnsureControllerFocus();}});
            ApplyCardScale(card,mode);RefreshHUD();return card;
        }
        void ApplyCardScale(VisualElement card,string mode)
        {
            if(mode=="conversation" || mode=="arrival-intro")return;
            card.style.scale=new Scale(Vector3.one*session.Progress.textScale);
            card.style.maxWidth=Length.Percent((mode=="conversation"?94:92)/session.Progress.textScale);
            card.style.maxHeight=Length.Percent(85/session.Progress.textScale);
        }
        ScrollView MenuScroll(VisualElement shell,string name)
        {
            var scroll=new ScrollView(ScrollViewMode.Vertical){name=name};Class(scroll,"evidence-scroll");
            scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;scroll.style.minHeight=48;
            scroll.contentContainer.style.flexShrink=0;shell.Add(scroll);return scroll;
        }
        void FocusLater(VisualElement element)
        {
            if(element!=null)element.schedule.Execute(()=>{if(root.Contains(element))element.Focus();});
        }
        void Line(VisualElement parent,TeaLine line,bool choice=false)
        {
            if(!string.IsNullOrWhiteSpace(line.Kannada))Kannada(parent,line.Kannada,choice?"choice-kn":"kannada");
            if(session.Progress.roman && !string.IsNullOrWhiteSpace(line.Roman)) Add(parent,line.Roman,choice?"choice-ro":"roman");
            if(session.Progress.english && !string.IsNullOrWhiteSpace(line.English)) Add(parent,line.English,choice?"choice-en":"meaning");
            string fallback=line.DisplayFallback(session.Progress.roman,session.Progress.english);
            if(!string.IsNullOrWhiteSpace(fallback))Add(parent,fallback,choice?"choice-en":"meaning");
        }
        void LayoutConversation(VisualElement card)
        {
            float scale=session.Progress.textScale;
            card.style.scale=new Scale(Vector3.one*scale);
            card.style.transformOrigin=new TransformOrigin(Length.Percent(100),Length.Percent(0),0);
            card.style.width=Length.Percent((root.ClassListContains("compact")?48:40)/scale);
            card.style.minWidth=280/scale;card.style.maxWidth=620/scale;
            card.style.height=Length.Percent((KeyboardOpen?57:100)/scale);
            card.style.maxHeight=Length.Percent((KeyboardOpen?57:100)/scale);
            card.style.bottom=StyleKeyword.Auto;
        }
        public void ShowConversation()
        {
            TeaTurn t=session.Turn; if(t==null) return;
            RecordChatReply(t,session.ConversationLine);
            bool sameTurn=Mode=="conversation" && object.ReferenceEquals(displayedTurn,t);
            bool focusPhrases=session.ConversationPhrasesVisible && (!sameTurn || !displayedPhrases);
            displayedPhrases=session.ConversationPhrasesVisible;
            var previous=root.Q<ScrollView>("dialogue-scroll");
            Vector2 offset=previous!=null?previous.scrollOffset:chatScrollPosition;
            string focusKey="";
            if(sameTurn) for(var e=root.focusController?.focusedElement as VisualElement;e!=null;e=e.parent)
                if(!string.IsNullOrEmpty(e.name) && e.name.StartsWith("dialogue-")) {focusKey=e.name;break;}
            displayedTurn=t;
            var card=Card("conversation","conversation");card.name="dialogue-card";
            LayoutConversation(card);
            card.RegisterCallback<GeometryChangedEvent>(e=>{
                if(session.player && root.worldBound.width>0)
                    session.player.conversationPanelFraction=Mathf.Clamp01(card.worldBound.width/root.worldBound.width);
            });
            var header=new VisualElement(); Class(header,"row");Class(header,"dialogue-header");card.Add(header);
            string speaker=session.ActiveNpc?session.ActiveNpc.displayName:"Villager";
            Add(header,speaker.Substring(0,1).ToUpperInvariant(),"speaker-avatar");
            var identity=new VisualElement();Class(identity,"speaker-identity");header.Add(identity);
            Add(identity,speaker,"speaker-name");
            Add(identity,session.ActiveNpc?session.ActiveNpc.roleLabel:"CONVERSATION","eyebrow");
            Button(header,"Notes",session.OpenNotebook,"quiet-button").name="dialogue-notebook";
            Button(header,"Leave",session.Close,"quiet-button").name="dialogue-leave";
            var scroll=new ScrollView(ScrollViewMode.Vertical){name="dialogue-scroll"};Class(scroll,"dialogue-scroll");card.Add(scroll);
            scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;
            VisualElement latestMessage=null,latestNpcMessage=null;
            foreach(var message in chatMessages) {
                var bubble=new VisualElement();Class(bubble,"chat-message");Class(bubble,message.player?"chat-player":"chat-npc");scroll.Add(bubble);
                Add(bubble,message.player?"YOU":session.ConversationTitle,"chat-speaker");Line(bubble,message.line);
                if(message.player && (message.pending || message.failed))
                    Add(bubble,message.pending && session.TeaAssessmentBusy?"Sending...":"Not delivered · retry","chat-delivery");
                latestMessage=bubble;
                if(!message.player)latestNpcMessage=bubble;
            }
            if(latestNpcMessage!=null)latestNpcMessage.name="dialogue-speech";
            var composer=new VisualElement{name="dialogue-composer"};Class(composer,"dialogue-composer");
            if(session.ShowTeaAssessment) {
                var inputRow=new VisualElement();Class(inputRow,"composer-input-row");composer.Add(inputRow);
                var assessmentInput=new TextField("Your message"){name="dialogue-tea-input",value=session.TeaAssessmentInput,maxLength=512};
                assessmentInput.tooltip="Type a message and press Enter to send.";
                assessmentInput.style.unityTextGenerator=TextGeneratorType.Standard;
                var textInput=assessmentInput.Q<VisualElement>(className:"unity-text-field__input");
                if(textInput!=null){textInput.style.color=new Color(.95f,.97f,.95f);textInput.style.backgroundColor=new Color(.04f,.05f,.05f,.85f);}
                assessmentInput.RegisterValueChangedCallback(e=>session.TeaAssessmentInput=e.newValue);
                assessmentInput.RegisterCallback<KeyDownEvent>(e=>{if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter){e.StopImmediatePropagation();if(!session.TeaAssessmentBusy)session.AskTeaConversation();}},TrickleDown.TrickleDown);
                assessmentInput.SetEnabled(!session.TeaAssessmentBusy);
                inputRow.Add(assessmentInput);
                var send=Button(inputRow,"Send",session.AskTeaConversation);send.name="dialogue-send";Class(send,"send-button");send.SetEnabled(!session.TeaAssessmentBusy);
                var actions=new VisualElement();Class(actions,"chat-actions");composer.Add(actions);
                if(!session.UsingController && !session.TeaAssessmentBusy)Button(actions,"Keyboard",OpenTeaKeyboard,"quiet-button").name="dialogue-tea-keyboard";
                if(t.Choices.Count>0)Button(actions,session.ConversationPhrasesVisible?"Hide phrases":"Phrases",session.ToggleConversationPhrases,"quiet-button").name="dialogue-phrases";
                if(session.TeaAssessmentBusy)Button(actions,"Cancel",session.CancelTeaAssessment,"quiet-button").name="dialogue-cancel";
            }
            if(session.TeaAssessmentFeedback.Length>0)Add(scroll,session.TeaAssessmentFeedback,"small");
            var phraseList=AddConversationPhrases(scroll,t);
            card.Add(composer);
            var aids=new VisualElement(); Class(aids,"aids");Class(aids,"dialogue-footer");card.Add(aids);
            var english=new VillageSwitch("English"){name="dialogue-english",value=session.Progress.english};
            english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowConversation();});aids.Add(english);
            var roman=new VillageSwitch("Romanized"){name="dialogue-roman",value=session.Progress.roman};
            roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowConversation();});aids.Add(roman);
            card.schedule.Execute(()=> {
                if(Mode!="conversation" || root.Q("dialogue-card")!=card) return;
                if(chatAutoScroll){chatAutoScroll=false;if(phraseList==null)scroll.schedule.Execute(()=>{if(latestMessage!=null && scroll.Contains(latestMessage))scroll.ScrollTo(latestMessage);});}
                else if(phraseList==null)scroll.scrollOffset=offset;
                if(phraseList!=null) {
                    VisualElement target=focusPhrases || string.IsNullOrEmpty(focusKey)?null:card.Q(focusKey);
                    if(target==null || !target.enabledInHierarchy)target=phraseList.Q<Button>();
                    if(target!=null) {
                        target.Focus();
                        if(phraseList.Contains(target))scroll.schedule.Execute(()=>{if(scroll.Contains(target))scroll.ScrollTo(target);});
                        else scroll.scrollOffset=offset;
                    }
                    return;
                }
                if(session.ShowTeaAssessment && session.UsingController && !session.TeaAssessmentBusy){OpenTeaKeyboard();return;}
                VisualElement focus=string.IsNullOrEmpty(focusKey)?null:card.Q(focusKey);
                // A response refresh must not pull focus away from the input or current aid.
                if(focus!=null && focus.enabledInHierarchy)focus.Focus();
                else if(!sameTurn && session.ShowTeaAssessment && !session.TeaAssessmentBusy)card.Q<TextField>("dialogue-tea-input")?.Focus();
            });
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
            var card=Card("notebook","notebook");card.style.height=Length.Percent(85);
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"YOUR FIELD NOTEBOOK","eyebrow"); Button(header,session.NotebookBackLabel,session.Close,"quiet-button").name="notebook-back";
            Add(card,session.Progress.epilogueStarted?"The reopened library":session.Progress.fridayFinaleStarted?"Friday at the library court":session.Progress.thursdayStarted?"Thursday morning":"The first morning","title");
            var nextStep=new VisualElement();Class(nextStep,"notebook-objective");card.Add(nextStep);
            Add(nextStep,"NEXT STEP","eyebrow");Add(nextStep,session.Progress.Objective,"body");
            var scroll=new ScrollView(ScrollViewMode.Vertical){name="notebook-story"}; Class(scroll,"notebook-scroll");scroll.horizontalScrollerVisibility=ScrollerVisibility.Hidden;card.Add(scroll);
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
            if(session.Progress.reviewOutcomeKnown) { Add(scroll,"LATER COUNCIL REVIEW / OUTCOME","section-title"); Add(scroll,"After reviewing the original and records, the council confirmed protection for the community library. This later decision is separate from Friday's interim suspension.","body"); }
            if(session.Progress.storyCompleted) Add(scroll,session.Progress.endingChoice==StoryEnding.Stay?"You chose to stay and help archive. The library is protected.":"You chose to depart with an invitation to return. The library is protected.","body");
            if(session.Progress.deedPresented) { Add(scroll,"LIBRARY COURT / ORIGINAL PRESENTED","section-title"); Button(scroll,"Review council evidence",()=>session.OpenActivity(VillageActivity.CouncilEvidence)); }
            if(session.Progress.workSuspended) Add(scroll,"Council president: clearance works suspended pending review of the original, its educational purpose, commercial-use restriction and community beneficiary. That was the interim decision; any later outcome is recorded separately.","body");
            if(session.Progress.returnJourneyStarted) { Add(scroll,"RETURN / ORIGINAL PROTECTED","section-title"); Add(scroll,"Returning with Ananya by the checked ridge and open auto approach. Walking and skipping the familiar journey preserve the same evidence.","body"); }
            if(session.Progress.returnedWithAnanya) Add(scroll,"Returned safely to Kalyanapura with Ananya and the original. Friday begins only after confirming preparations with her.","body");
            if(session.Progress.fridayFinaleStarted) Add(scroll,"FRIDAY / BEFORE 09:00: meet the council at the library court. Work is not suspended until the evidence is presented and reviewed.","body");
            if(session.Progress.trunkOpened) { Add(scroll,"FAMILY TRUNK / OPENED","section-title"); Button(scroll,"Review the original deed",()=>session.OpenActivity(VillageActivity.DeedReview)); }
            if(session.Progress.deedConceptsReviewed) Add(scroll,"Original / 1952: education purpose; restricted commercial use; community beneficiary. Family register/signatories match the catalogue reference. These clauses provided the basis for council review.","body");
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
            if(session.Progress.thursdayStarted) Add(scroll,"THURSDAY: rested with evidence preserved before the public panchayat notice investigation.","section-title");
            if(session.Progress.gurumurthyAccountKnown) { Add(scroll,"GURUMURTHY / TUESDAY MORNING","section-title"); Add(scroll,"Saw Ananya come alone towards the stop; went inside before the bus arrived. Did not see boarding.","body"); Word(scroll,"ಒಬ್ಬಳೇ ಬಂದಳು","Obbale bandalu","She came alone"); }
            if(session.Progress.rathnammaAccountKnown) { Add(scroll,"RATHNAMMA / TUESDAY MORNING","section-title"); Add(scroll,"Reports seeing Ananya board Route 14. Did not see where she got off.","body"); Word(scroll,"ಬಸ್ಸು ಹತ್ತಿದಳು","Bassu hattidalu","She boarded the bus"); }
            if(session.Progress.witnessAccountsCompared) Add(scroll,"Witness comparison: boarding had an eyewitness account; those witnesses could not establish arrival or present safety.","body");
            if(session.Progress.guddaFamilyLeadKnown) Add(scroll,"Rathnamma: Ananya asked about Shankare Gowda's family at Gudda Mane. Check this name against Somanna's records next.","body");
            OrganizeNotebook(card,scroll);
        }
        void Word(VisualElement parent,string kn,string ro,string en)
        {
            var row=new VisualElement(); Class(row,"word-row"); parent.Add(row);
            Kannada(row,kn,"word-kn"); var text=new VisualElement(); row.Add(text);
            Add(text,ro,"word-ro"); Add(text,en,"small");
        }
        public void ShowArrival()
        {
            var shell=Card("arrival","paper");shell.name="arrival-card";shell.style.height=Length.Percent(85);
            Add(shell,"WEDNESDAY / YOUR FIRST MORNING","eyebrow").style.flexShrink=0;
            var card=MenuScroll(shell,"arrival-scroll");
            Add(card,"Welcome to Kalyanapura","title");
            Add(card,"You came to help archive the community library. Ananya was meant to meet you, but she is not at the bus stand. The tea-stall owner may know the way.","body");
            Add(card,"FIRST: greet Manjunatha at the tea stall, order tea or water, then ask for the library. English meanings, Roman lettering and Phrases can help you reply.","body");
            Add(card,"Walk with WASD or the left stick, and look with the mouse or right stick. Press E or A / Cross at a nearby person or sign. Use Tab in the village, or View / Share, for your notebook. Conversations take their time.","body");
            var enter=Button(shell,"Step into the village  [Enter]",session.AcknowledgeArrival);enter.name="arrival-enter";enter.style.flexShrink=0;FocusLater(enter);
            var replay=Button(shell,"Replay introduction",session.PlayArrivalIntro,"quiet-button");replay.name="arrival-replay";replay.style.flexShrink=0;
            var back=Button(shell,"Back to title  [Esc]",session.OpenTitle,"quiet-button");back.name="arrival-title";back.style.flexShrink=0;
        }
        public void ShowPause()
        {
            if(Mode!="pause")pauseSection=session.TitleSettings?"display":"visit";
            BuildPauseMenu();
        }
        public void HideModal() {session.CancelArrivalIntro();CloseTeaKeyboard();if(IsModal)session.PlayControllerFeedback(ControllerFeedback.Menu); Mode=""; modal.Clear(); modal.style.display=DisplayStyle.None; }
        void OnDestroy() { CloseTeaKeyboard();ReleaseControllerNavigation();if(latinAsset) Destroy(latinAsset); if(kannadaAsset) Destroy(kannadaAsset);var document=GetComponent<UIDocument>();if(document && document.panelSettings==ownedPanel)document.panelSettings=originalPanel;if(ownedPanel)Destroy(ownedPanel); }
    }
}
