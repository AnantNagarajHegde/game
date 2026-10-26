using UnityEngine.UIElements;
namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        public bool SelectingBicycleParts { get; private set; }
        public void ShowRiverActivity(VillageActivity activity,string feedback="")
        {
            if(activity==VillageActivity.BicycleInspection) { ShowBicycle(feedback); return; }
            var card=Card("evidence","paper"); Class(card,"evidence-card");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,activity==VillageActivity.BookingSlip?"ADVANCE BOOKING  /  CARRIER POCKET":"COMPARE THE RIVER EVIDENCE","eyebrow");
            Button(header,session.EvidenceBackLabel+"  [Esc]",session.Close,"quiet-button");
            var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
            if(feedback!="") Add(scroll,feedback,"feedback");
            if(activity==VillageActivity.BookingSlip)
            {
                Add(scroll,"A plan, not a boarding record","title");
                Add(scroll,"Issued on Monday · reserved for Tuesday","eyebrow");
                Line(scroll,new TeaLine("ಮಂಗಳವಾರ ಬೆಳಿಗ್ಗೆ · ಬಸ್ ೧೪","Mangalavaara beligge · bus 14","Tuesday · 06:45 · Route 14 → Gudda Gate"));
                Add(scroll,"This is an advance booking slip from the dry carrier pocket. It could have been placed here before Ananya walked to the bus stop. It has no boarding stamp.","body");
                if(!session.Progress.bookingSlipRead)
                {
                    Add(scroll,"WHAT DOES THIS DOCUMENT ESTABLISH?","section-title");
                    Answers(scroll,new[]{
                        new TeaLine("ಪ್ರಯಾಣ ಮಾಡುವ ಉದ್ದೇಶ","Prayaana maaduva uddesha","An intention to make this journey"),
                        new TeaLine("ಬಸ್ ಹತ್ತಿದ ಸಾಕ್ಷಿ","Bus hattida saakshi","Proof that she boarded"),
                        new TeaLine("ನದಿಯಲ್ಲಿ ಅಪಘಾತ","Nadiyalli apaghaata","Proof of a river accident")
                    });
                }
                else Add(scroll,"Recorded as travel intent. Actual boarding and Ananya’s safety remain unconfirmed.","success");
            }
            else
            {
                Add(scroll,"What can we reasonably conclude?","title");
                Add(scroll,"Observed: no obvious crash damage; the bicycle stands upright on its deployed stand; an advance booking was inside a dry pocket.","body");
                Add(scroll,"Giri found the bicycle here. He did not witness Ananya leaving, boarding a bus, or entering the river.","body");
                if(!session.Progress.riverDeductionKnown)
                    Answers(scroll,new[]{
                        new TeaLine("ಅಪಘಾತ ಆಗಿದೆ","Apaghaata aagide","The bicycle proves she had an accident"),
                        new TeaLine("ಬಸ್ ಹತ್ತಿದ್ದಾರೆ","Bus hattiddare","The booking proves she boarded"),
                        new TeaLine("ಪ್ರಯಾಣದ ಉದ್ದೇಶ ಇದೆ","Prayaanada uddesha ide","The evidence supports a planned trip; ask a witness")
                    });
                else Add(scroll,"The accident rumour is weakened, not conclusively disproved. Seek an independent witness near the northern stop.","success");
            }
            Button(card,session.EvidenceBackLabel,session.Close);
            RiverAids(card,activity);
        }
        void ShowBicycle(string feedback)
        {
            var card=Card("evidence","conversation"); Class(card,"bicycle-panel");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"ANANYA’S BICYCLE  /  REPAIR SHED","eyebrow");
            Button(header,session.EvidenceBackLabel+"  [Esc]",session.Close,"quiet-button");
            if(!RiverInvestigation.CanInspect(session.Progress))
            {
                Add(card,"The bicycle is under the shed. Speak to Giri about his notice before opening its pocket.","body");
                return;
            }
            var part=session.SelectedBicyclePart;
            if(part==BicyclePart.Frame)
                Line(card,new TeaLine("ಬೈಸಿಕಲ್ ಚೆನ್ನಾಗಿದೆ.","Bicycle chennaagide.","The frame and rims have no obvious crash damage. That alone does not establish the owner’s safety."));
            else if(part==BicyclePart.Stand)
                Line(card,new TeaLine("ಸ್ಟ್ಯಾಂಡ್ ಹಾಕಿ ನಿಲ್ಲಿಸಿದ್ದಾರೆ.","Stand haaki nillisiddare.","The stand is deployed and supports the upright bicycle. This looks like deliberate parking."));
            else if(part==BicyclePart.CarrierPocket && !session.Progress.bookingSlipFound)
                Line(card,new TeaLine("ಕ್ಯಾರಿಯರ್ ಕೆಳಗಿನ ಚೀಲದ ಒಳಗೆ.","Carrier kelagina cheelada olage.","The dry pocket is below the rear carrier. Which word means inside?"));
            else if(part==BicyclePart.CarrierPocket)
                Add(card,"You found a dry advance booking slip inside the carrier pocket. It is kept in your field notes.","success");
            else Add(card,"Look at the frame, the stand, and the pocket below the rear carrier. The view shows the actual bicycle in the shed.","body");
            if(feedback!="") Add(card,feedback,"feedback");
            if(part==BicyclePart.CarrierPocket && !session.Progress.bookingSlipFound)
            {
                var answers=new VisualElement(); Class(answers,"choices"); card.Add(answers);
                Answers(answers,new[]{
                    new TeaLine("ಮೇಲೆ","Mele","On top"),
                    new TeaLine("ಒಳಗೆ","Olage","Inside"),
                    new TeaLine("ಹೊರಗೆ","Horage","Outside")
                });
            }
            else
            {
                SelectingBicycleParts=true;
                var choices=new VisualElement(); Class(choices,"choices"); card.Add(choices);
                var labels=new[]{
                    new TeaLine("ಬೈಸಿಕಲ್","Bicycle","1 · Inspect frame"),
                    new TeaLine("ಸ್ಟ್ಯಾಂಡ್","Stand","2 · Inspect stand"),
                    new TeaLine("ಚೀಲ","Cheela","3 · Inspect carrier pocket")
                };
                for(int i=0;i<3;i++)
                {
                    int index=i; var b=new Button(()=>session.SelectBicyclePart((BicyclePart)index)); Class(b,"choice"); choices.Add(b);
                    var text=new VisualElement(); Class(text,"choice-lines"); b.Add(text); Line(text,labels[i],true);
                }
            }
            if(session.Progress.bookingSlipFound)
                Button(card,"Read the advance booking slip",()=>session.OpenActivity(VillageActivity.BookingSlip));
            Add(card,"Frame: "+(session.Progress.bicycleFrameObserved?"observed":"not yet examined")+
                "  ·  Stand: "+(session.Progress.bicycleStandObserved?"observed":"not yet examined"),"small");
            RiverAids(card,VillageActivity.BicycleInspection);
        }
        void RiverAids(VisualElement card,VillageActivity activity)
        {
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new Toggle("English meaning"){value=session.Progress.english};
            english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowRiverActivity(activity);}); aids.Add(english);
            var roman=new Toggle("Roman letters"){value=session.Progress.roman};
            roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowRiverActivity(activity);}); aids.Add(roman);
            Add(aids,"1–3 choose a part or answer · Tab opens field notes","small");
        }
        void AddRiverNotebook(VisualElement parent)
        {
            if(session.Progress.giriAccountKnown)
            {
                Add(parent,"GIRI’S ACCOUNT  /  REPAIR SHED","section-title");
                Add(parent,"Giri found the bicycle under his shed on Tuesday. He did not see who left it or anyone fall into the river.","body");
            }
            if(session.Progress.bicycleFrameObserved || session.Progress.bicycleStandObserved)
            {
                Add(parent,"DIRECT OBSERVATIONS","section-title");
                if(session.Progress.bicycleFrameObserved) Add(parent,"Frame and rims: no obvious crash damage.","body");
                if(session.Progress.bicycleStandObserved) Add(parent,"Stand: deployed; bicycle upright.","body");
                Word(parent,"ಕೆಳಗೆ","Kelage","Below"); Word(parent,"ಒಳಗೆ","Olage","Inside");
            }
            if(session.Progress.bookingSlipFound)
            {
                Add(parent,"ADVANCE BOOKING  /  DRY CARRIER POCKET","section-title");
                Button(parent,"Read the booking slip",()=>session.OpenActivity(VillageActivity.BookingSlip));
                if(session.Progress.bookingSlipRead) Add(parent,"Issued Monday for Tuesday 06:45, Route 14 → Gudda Gate. Supports intended travel, not witnessed boarding.","body");
            }
            if(session.Progress.riverDeductionKnown)
                Add(parent,"The parked bicycle and booking weaken the accident rumour. They do not establish Ananya’s location or safety.","body");
            if(session.Progress.templeLeadKnown)
            {
                Add(parent,"NEXT WITNESSES  /  GIRI’S SUGGESTION","section-title");
                Add(parent,"Ask Gurumurthy and Rathnamma near the temple/northern stop. Giri suggests they may have seen something; their testimony has not been collected.","body");
            }
        }
    }
}
