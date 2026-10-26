using System;
namespace Paridhwani
{
    public static class PanchayatInvestigation
    {
        public static bool CanInvestigate(TeaProgress p) => p.thursdayStarted && p.deedReferenceFound;
        public static bool Answer(TeaProgress p,int index)
        {
            if(!CanInvestigate(p)) return false;
            if(!p.noticeIdentified) { if(index!=0) return false; p.noticeIdentified=true; return true; }
            if(!p.deadlineKnown) { if(index!=1) return false; p.deadlineKnown=true; return true; }
            if(index!=2) return false;
            p.deedRequired=true; p.checkpostDestinationKnown=true; return true;
        }
    }
    public sealed class PanchayatConversation : ITeaConversationProvider,IActivityConversationProvider
    {
        public TeaTurn Start(TeaProgress p) => Node(!PanchayatInvestigation.CanInvestigate(p)?"early":p.checkpostDestinationKnown?"return":"notice");
        public TeaTurn ResumeAfterActivity(TeaTurn t,TeaProgress p) => Start(p);
        public TeaTurn Reply(TeaTurn t,int i,TeaProgress p)
        { if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); return t.Choices[i].Next=="close"?null:Node(t.Choices[i].Next); }
        static TeaChoice C(string kn,string ro,string en,string next,VillageActivity activity=VillageActivity.None) => new TeaChoice(kn,ro,en,next){Activity=activity};
        static TeaTurn Node(string id)
        {
            TeaTurn t;
            switch(id)
            {
                case "early": t=new TeaTurn(id,"ನಮಸ್ಕಾರ.","Namaskara.","The public notice is on the board. Speak to Somanna about the library records first. Come back during Thursday's public counter hours.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "notice": t=new TeaTurn(id,"ಸೂಚನೆಯನ್ನು ಓದಿ.","Soochaneyannu odi.","Read the clearance notice and its document checklist. Your catalogue reference may identify a record, but it is not the original deed.",C("ಸೂಚನೆ ಓದಬಹುದಾ?","Soochane odabahuda?","May I read the notice?","notice",VillageActivity.PanchayatNotice),C("ಏಕೆ ಇಷ್ಟು ಬೇಗ?","Eke ishtu bega?","Why the hurry?","question"),C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will read the public board myself.","close")); break;
                case "question": t=new TeaTurn(id,"ಎಲ್ಲವೂ ಸೂಚನೆಯಲ್ಲಿದೆ.","Ellavoo soochaneyallide.","Everything relevant is on the public board. I will not discuss the proposed warehouse plan. You can still read the notice and document checklist.",C("ಸೂಚನೆಯನ್ನು ಓದೋಣ.","Soochaneyannu odona.","Read the notice and checklist.","notice",VillageActivity.PanchayatNotice),C("ಸರಿ.","Sari.","I will check it myself.","close")); break;
                case "return": t=new TeaTurn(id,"ಮೂಲ ಪತ್ರ ಬೇಕು.","Moola patra beku.","Bring the original deed for council review before Friday at 09:00. The catalogue alone does not suspend works. For Gudda Mane, first ask at the forest checkpost about a safe route; do not bypass road closures.",C("ಮತ್ತೆ ಓದೋಣ.","Matte odona.","Read the notice again.","return",VillageActivity.PanchayatNotice),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                default: throw new ArgumentException(id);
            }
            t.Speaker="Shresthi"; return t;
        }
    }
}
