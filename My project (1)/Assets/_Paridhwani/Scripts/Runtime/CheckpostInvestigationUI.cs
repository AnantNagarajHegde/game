using UnityEngine.UIElements;
namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        public void ShowTravelArrival(bool outward)
        {
            var card=Card("travel-arrival","paper");
            Add(card,outward?"THURSDAY / FOREST CHECKPOST":"THURSDAY / VILLAGE PICKUP","eyebrow");
            Add(card,outward?"The open road ends here":"Back in Kalyanapura","title");
            Add(card,outward?"Raghavendra stops on the open approach. The washed-out road lies beyond the barrier. Kencha is at the shelter ahead; ask him before using the ridge.":"Your investigation notes are preserved. Raghavendra can take you back to the checkpost when you are ready.","body");
            Button(card,"Continue on foot",session.Close);
        }
        public void ShowCheckpostActivity(VillageActivity activity,string feedback="")
        {
            var card=Card("evidence","paper"); Class(card,"evidence-card");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"FOREST ROAD / SAFE ACCESS","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
            var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
            if(activity==VillageActivity.CheckpostTravel || activity==VillageActivity.VillageReturn)
            {
                bool outward=activity==VillageActivity.CheckpostTravel;
                Add(scroll,outward?"Travel to the forest checkpost?":"Return to the village?","title");
                Add(scroll,outward?"This skips the uneventful drive and stops before the road closure. You will continue on foot only after checking with Kencha. Your evidence is retained; no money or timed deadline is consumed.":"Return to the village pickup. Your checkpost testimony and learned directions remain saved.","body");
                if(!outward || CheckpostInvestigation.CanTravel(session.Progress)) Button(scroll,outward?"Confirm journey to checkpost":"Confirm return to village",session.ConfirmTravel);
                else Add(scroll,"Complete the archive and panchayat investigation first.","body");
                return;
            }
            Add(scroll,"Road closed after rain","title");
            Line(scroll,new TeaLine("ಮಳೆಯ ಕಾರಣ ರಸ್ತೆ ಮುಚ್ಚಿದೆ.","Maleya kaarana raste muchchide.","The road is closed because of rain."));
            Add(scroll,"The damaged culvert is beyond the fixed road barrier. The auto stops on this side. Do not cross the washout. Ask Kencha about the marked ridge entrance.","body");
            if(feedback!="") Add(scroll,feedback,"feedback");
            if(!session.Progress.checkpostAccountKnown) Add(scroll,"Speak to Kencha at the shelter first. A posted closure does not by itself grant permission for the ridge.","body");
            else {
                Line(scroll,new TeaLine("ಕಲ್ಲಿನ ಬಳಿ ಎಡಕ್ಕೆ ಹೋಗಿ. ಬೇಲಿಯ ಪಕ್ಕದಲ್ಲಿ ನಡೆಯಿರಿ.","Kallina bali edakke hogi. Beliya pakkadalli nadeyiri.","Turn left at the stone marker. Walk beside the fence."));
                Add(scroll,session.ridgeRouteChecked?"KENCHA / CHECKED TODAY: fallen branches cleared from the marked approach. The road closure remains in force.":"RIDGE NOT YET CHECKED: wait at the shelter. Learning the directions does not clear a physical closure.","body");
                if(!session.Progress.trailInstructionsUnderstood) {
                    Add(scroll,"WHICH INSTRUCTIONS MATCH THE SAFE ROUTE?","section-title");
                    Answers(scroll,new[]{new TeaLine("ರಸ್ತೆಯಲ್ಲಿ ಹೋಗಿ","Rasteyalli hogi","Cross the road barrier"),new TeaLine("ಎಡಕ್ಕೆ ಹೋಗಿ. ಬೇಲಿಯ ಪಕ್ಕದಲ್ಲಿ ನಡೆಯಿರಿ.","Edakke hogi. Beliya pakkadalli nadeyiri.","Left at the stone; follow the fence uphill"),new TeaLine("ನದಿಯನ್ನು ದಾಟಿ","Nadiyannu daati","Cross the river")});
                } else Add(scroll,session.Progress.trailPermission?"Directions recorded. The checked ridge approach is available; the farm journey will be added next.":"Directions understood. Permission remains withheld until Kencha confirms the ridge is checked.","success");
                Add(scroll,"Ridge trailhead: this prototype stops at the marked approach. Gudda Mane and the reunion are not implemented yet. Return via Raghavendra whenever you wish.","small");
            }
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new Toggle("English meaning"){value=session.Progress.english}; english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowCheckpostActivity(activity);}); aids.Add(english);
            var roman=new Toggle("Roman letters"){value=session.Progress.roman}; roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowCheckpostActivity(activity);}); aids.Add(roman);
        }
    }
}
