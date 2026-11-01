using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        public bool AcceptingEvidenceAnswers { get; private set; }
        public void ShowInvestigation(VillageActivity activity,string feedback="")
        {
            var card=Card("evidence","paper"); Class(card,"evidence-card");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,activity==VillageActivity.MarketReceipt?"BASAVARAJU’S LEDGER":"PUBLIC BUS TIMETABLE","eyebrow");
            Button(header,session.EvidenceBackLabel+"  [Esc]",session.Close,"quiet-button");
            var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
            if(feedback!="") Add(scroll,feedback,"feedback");
            bool receipt=activity==VillageActivity.MarketReceipt;
            if(receipt)
            {
                Add(scroll,"Purchase ledger","title");
                Line(scroll,new TeaLine("ಒಣಹಣ್ಣು · ಬ್ಯಾಟರಿ · ಮೇಣದಬತ್ತಿ","Onahannu · battery · menadabatti","Dried fruit · batteries · candles"),true);
                var entry=new VisualElement(); Class(entry,"ledger-entry"); scroll.Add(entry);
                var writing=new VisualElement(); Class(writing,"ledger-writing"); entry.Add(writing);
                Kannada(writing,"ಸೋಮವಾರ · ಸಂಜೆ ಆರು ಗಂಟೆ","choice-kn");
                if(session.Progress.roman) Add(writing,"Somavaara · sanje aaru gante","roman");
                if(session.Progress.english) Add(writing,"Monday · 18:00","meaning");
                Clock(entry);
                Add(scroll,"This entry records the purchase. The journey happened on a different day.","small");
                if(!session.Progress.purchaseTimeKnown)
                {
                    Add(scroll,"WHEN WERE THE SUPPLIES BOUGHT?","section-title");
                    Answers(scroll,new[]{
                        new TeaLine("ಸೋಮವಾರ ಸಂಜೆ ಆರು ಗಂಟೆ","Somavaara sanje aaru gante","Monday · 18:00"),
                        new TeaLine("ಮಂಗಳವಾರ ಬೆಳಿಗ್ಗೆ ಆರು ಗಂಟೆ","Mangalavaara beligge aaru gante","Tuesday · 06:00"),
                        new TeaLine("ಬುಧವಾರ ಸಂಜೆ ಆರು ಗಂಟೆ","Budhavaara sanje aaru gante","Wednesday · 18:00")
                    });
                }
                else
                {
                    Add(scroll,"Recorded: Monday at 18:00. Buying supplies does not prove when Ananya travelled.","success");
                    Button(card,session.ResumeAvailable?"Ask Basavaraju about the journey":"Return to the village",session.Close);
                }
            }
            else
            {
                Add(scroll,"North road departures","title");
                TimetableRow(scroll,"14","ಗುಡ್ಡ ಗೇಟ್","Gudda Gate","06:45  ·  Morning");
                TimetableRow(scroll,"08","ಮಾರುಕಟ್ಟೆ","Local market","07:15  ·  Morning");
                TimetableRow(scroll,"14","ಗುಡ್ಡ ಗೇಟ್","Gudda Gate","18:45  ·  Evening");
                Add(scroll,"Published daily schedule. A timetable records a service, not who boarded it.","small");
                if(!session.Progress.marketAccountKnown)
                    Add(scroll,"You do not yet know which service Ananya intended to take. Basavaraju can help interpret the shopping note.","body");
                else if(!session.Progress.busDepartureKnown)
                {
                    Add(scroll,"ANANYA PLANNED TO GO ON TUESDAY MORNING. WHICH DEPARTURE MATCHES?","section-title");
                    Answers(scroll,new[]{
                        new TeaLine("ಮಂಗಳವಾರ ಬೆಳಿಗ್ಗೆ · ಬಸ್ ೧೪","Mangalavaara beligge · bus 14","Tuesday · 06:45 · Route 14"),
                        new TeaLine("ಮಂಗಳವಾರ ಸಂಜೆ · ಬಸ್ ೧೪","Mangalavaara sanje · bus 14","Tuesday · 18:45 · Route 14"),
                        new TeaLine("ಸೋಮವಾರ ಬೆಳಿಗ್ಗೆ · ಬಸ್ ೧೪","Somavaara beligge · bus 14","Monday · 06:45 · Route 14")
                    });
                }
                else
                {
                    Add(scroll,"Intended service: Route 14, Tuesday at 06:45, towards Gudda Gate. No witness has yet confirmed boarding.","success");
                    Button(card,session.Progress.riverReportKnown?"Read Giri’s notice again":"Read the small notice pinned beside this timetable",session.ReadRiverNotice);
                }
            }
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new Toggle("English meaning"){value=session.Progress.english};
            english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowInvestigation(activity);}); aids.Add(english);
            var roman=new Toggle("Roman letters"){value=session.Progress.roman};
            roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowInvestigation(activity);}); aids.Add(roman);
            Add(card,AcceptingEvidenceAnswers?"Use 1–3 or choose an answer. Hints do not cost progress.":"Your notebook keeps the evidence and its source.","small");
        }
        void Answers(VisualElement parent,TeaLine[] lines)
        {
            AcceptingEvidenceAnswers=true;
            var choices=new Button[lines.Length];
            for(int i=0;i<lines.Length;i++)
            {
                int index=i;
                var button=new Button(()=>session.SubmitInvestigationAnswer(index)){name="evidence-answer-"+i};choices[i]=button;Class(button,"evidence-answer");parent.Add(button);
                // Document answers form an ordered list. Spatial focus navigation
                // cannot reliably reach the next row while it is clipped by scrolling.
                button.RegisterCallback<NavigationMoveEvent>(e=>{
                    int step=e.direction==NavigationMoveEvent.Direction.Down?1:e.direction==NavigationMoveEvent.Direction.Up?-1:0;
                    int next=index+step;
                    if(step==0 || next<0 || next>=choices.Length || !choices[next].enabledInHierarchy)return;
                    e.StopImmediatePropagation();choices[next].Focus();
                    var scroll=parent as ScrollView ?? parent.GetFirstAncestorOfType<ScrollView>();
                    scroll?.ScrollTo(choices[next]);
                });
                var number=Add(button,(i+1).ToString("00"),"choice-number");
                var content=new VisualElement(); Class(content,"choice-lines"); button.Add(content);
                Line(content,lines[i],true);
            }
        }
        void TimetableRow(VisualElement parent,string number,string kn,string roman,string time)
        {
            var row=new VisualElement(); Class(row,"schedule-row"); parent.Add(row);
            Add(row,number,"route-number"); var dest=new VisualElement(); Class(dest,"schedule-destination"); row.Add(dest);
            Kannada(dest,kn,"choice-kn"); if(session.Progress.roman) Add(dest,roman,"small");
            Add(row,time,"schedule-time");
        }
        void Clock(VisualElement parent)
        {
            var clock=new VisualElement(); Class(clock,"clock"); parent.Add(clock);
            Add(clock,"12","clock-twelve"); Add(clock,"3","clock-three"); Add(clock,"6","clock-six"); Add(clock,"9","clock-nine");
            var minute=new VisualElement(); Class(minute,"clock-minute"); clock.Add(minute);
            var hour=new VisualElement(); Class(hour,"clock-hour"); clock.Add(hour);
            var pin=new VisualElement(); Class(pin,"clock-pin"); clock.Add(pin);
        }
        public void ShowRiverNotice()
        {
            var card=Card("evidence","paper");
            Add(card,"PINNED BY THE BUS TIMETABLE  /  GIRI","eyebrow");
            Add(card,"A bicycle near the weir","title");
            Line(card,new TeaLine("ನದಿಯ ಹತ್ತಿರ ಬೈಸಿಕಲ್ ಇದೆ. ಗಿರಿಯನ್ನು ಕೇಳಿ.","Nadiya hattira bicycle ide. Giriyannu keli.","A bicycle is near the river. Ask Giri."));
            Add(card,"Source: Giri’s public notice. The bicycle has not been inspected and this report does not explain Ananya’s whereabouts.","body");
            Add(card,"Follow the marked river path to Giri’s repair shed. Keep the purchase, planned journey and unverified report separate in your field notes.","body");
            Button(card,"Keep the notice in my field notes",session.Close);
        }
    }
}
