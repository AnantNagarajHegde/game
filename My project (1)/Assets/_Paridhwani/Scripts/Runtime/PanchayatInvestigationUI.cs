using UnityEngine.UIElements;
namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        public void ShowPanchayatActivity(string feedback="")
        {
            var card=Card("evidence","paper"); Class(card,"evidence-card");
            var header=new VisualElement(); Class(header,"row"); card.Add(header);
            Add(header,"PANCHAYAT / PUBLIC NOTICE","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
            var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
            Add(scroll,"Community library clearance","title");
            Line(scroll,new TeaLine("ಗ್ರಂಥಾಲಯ / ಶುಕ್ರವಾರ ಬೆಳಿಗ್ಗೆ ಒಂಬತ್ತು ಗಂಟೆ","Granthalaya / Shukravaara beligge ombattu gante","Library / Friday at 09:00"));
            Add(scroll,"CLEARANCE SCHEDULE: Community library site. Friday, 09:00. Signed: Shresthi, public counter. Proposed use: warehouse. Original ownership documents may be submitted to the council before work begins for review.","body");
            if(feedback!="") Add(scroll,feedback,"feedback");
            if(!PanchayatInvestigation.CanInvestigate(session.Progress)) Add(scroll,"You can read this public board now. Bring Somanna's archive reference and return after resting to compare the documents.","body");
            else if(!session.Progress.noticeIdentified) {
                Add(scroll,"WHICH PLACE DOES THE NOTICE CONCERN?","section-title");
                Answers(scroll,new[]{new TeaLine("ಗ್ರಂಥಾಲಯ","Granthalaya","The community library"),new TeaLine("ದೇವಸ್ಥಾನ","Devasthaana","The temple"),new TeaLine("ಬಸ್ ನಿಲ್ದಾಣ","Bus nildaana","The bus stop")});
            } else if(!session.Progress.deadlineKnown) {
                Add(scroll,"WHEN IS CLEARANCE SCHEDULED?","section-title");
                Answers(scroll,new[]{new TeaLine("ಗುರುವಾರ ಸಂಜೆ","Guruvaara sanje","Thursday evening"),new TeaLine("ಶುಕ್ರವಾರ ಬೆಳಿಗ್ಗೆ ಒಂಬತ್ತು ಗಂಟೆ","Shukravaara beligge ombattu gante","Friday at 09:00"),new TeaLine("ಶುಕ್ರವಾರ ರಾತ್ರಿ","Shukravaara raatri","Friday night")});
            } else if(!session.Progress.deedRequired) {
                Add(scroll,"MATCH THE ARCHIVE REFERENCE TO THE CHECKLIST","section-title");
                Add(scroll,"The catalogue locates the original 1952 deed at Gudda Mane. The checklist requires the original ownership document. Which document must be sought?","body");
                Answers(scroll,new[]{new TeaLine("ಬಸ್ ಚೀಟಿ","Bus cheeti","The advance bus booking"),new TeaLine("ಕಡತದ ಸೂಚಿ","Kadatada soochi","The library catalogue entry alone"),new TeaLine("ಮೂಲ ಪತ್ರ","Moola patra","The family's original deed for council review")});
            } else {
                Add(scroll,"Recorded: library clearance scheduled Friday at 09:00; seek the original deed at Gudda Mane for council review. Submitting it does not itself guarantee protection.","success");
                Add(scroll,"PUBLIC ROUTE ADVISORY: Ask the forest checkpost about safe access to Gudda Mane. The river road may be closed after rain. Ask Raghavendra at the auto pickup for transport to the checkpost.","body");
            }
            var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids);
            var english=new VillageSwitch("English meaning"){value=session.Progress.english}; english.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowPanchayatActivity();}); aids.Add(english);
            var roman=new VillageSwitch("Roman letters"){value=session.Progress.roman}; roman.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowPanchayatActivity();}); aids.Add(roman);
        }
    }
}
