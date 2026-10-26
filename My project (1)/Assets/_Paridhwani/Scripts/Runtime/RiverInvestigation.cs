using System;
namespace Paridhwani
{
    public enum BicyclePart { None=-1, Frame=0, Stand=1, CarrierPocket=2 }
    public static class RiverInvestigation
    {
        public static bool CanInspect(TeaProgress p) => p.riverReportKnown && p.giriAccountKnown;
        public static bool Observe(TeaProgress p,BicyclePart part)
        {
            if(!CanInspect(p)) return false;
            if(part==BicyclePart.Frame) { p.bicycleFrameObserved=true; return true; }
            if(part==BicyclePart.Stand) { p.bicycleStandObserved=true; return true; }
            return part==BicyclePart.CarrierPocket;
        }
        public static bool FindBooking(TeaProgress p,int answer)
        {
            if(!CanInspect(p) || answer!=1) return false;
            p.bookingSlipFound=true; return true;
        }
        public static bool ReadBooking(TeaProgress p,int answer)
        {
            if(!p.bookingSlipFound || answer!=0) return false;
            p.bookingSlipRead=true; return true;
        }
        public static bool CanDeduce(TeaProgress p) =>
            CanInspect(p) && p.bicycleFrameObserved && p.bicycleStandObserved && p.bookingSlipRead;
        public static bool Deduce(TeaProgress p,int answer)
        {
            if(!CanDeduce(p) || answer!=2) return false;
            p.riverDeductionKnown=true; return true;
        }
    }
    public sealed class RiverConversation : ITeaConversationProvider,IActivityConversationProvider
    {
        public TeaTurn Start(TeaProgress p) => Node(!p.riverReportKnown?"early":
            !p.giriAccountKnown?"hello":p.templeLeadKnown?"return":
            p.riverDeductionKnown?"witness":RiverInvestigation.CanDeduce(p)?"deduce":"inspect",p);
        public TeaTurn ResumeAfterActivity(TeaTurn current,TeaProgress p) => Start(p);
        public TeaTurn Reply(TeaTurn t,int index,TeaProgress p)
        {
            if(index<0 || index>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var c=t.Choices[index];
            if(c.Effect=="account")
            {
                if(!p.riverReportKnown) throw new InvalidOperationException("Introduce Giri's report first.");
                p.giriAccountKnown=true;
            }
            if(c.Effect=="temple")
            {
                if(!p.riverDeductionKnown) throw new InvalidOperationException("Review the bicycle evidence first.");
                p.templeLeadKnown=true;
            }
            return c.Next=="close"?null:Node(c.Next,p);
        }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="",VillageActivity activity=VillageActivity.None) =>
            new TeaChoice(kn,ro,en,next,effect){Activity=activity};
        static TeaTurn Node(string id,TeaProgress p)
        {
            TeaTurn t;
            switch(id)
            {
                case "early": t=new TeaTurn(id,"ನಮಸ್ಕಾರ. ನದಿಯ ಹತ್ತಿರ ಹೋಗಬೇಡಿ.","Namaskara. Nadiya hattira hogabedi.","Hello. Stay behind the river railing. My notice is beside the bus timetable.",
                    C("ಸರಿ. ಧನ್ಯವಾದಗಳು.","Sari. Dhanyavadagalu.","All right. Thank you.","close")); break;
                case "hello": t=new TeaTurn(id,"ನಮಸ್ಕಾರ. ನೀವು ಚೀಟಿ ನೋಡಿದಿರಾ?","Namaskara. Neevu cheeti nodidira?","Hello. Did you see my notice?",
                    C("ಹೌದು. ಬೈಸಿಕಲ್ ಎಲ್ಲಿದೆ?","Haudu. Bicycle ellide?","Yes. Where is the bicycle?","inspect","account"),
                    C("ದಯವಿಟ್ಟು ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Dayavittu nidhanavagi heli.","Please speak slowly.","help")); break;
                case "help": t=new TeaTurn(id,"ಸರಿ. ಇಲ್ಲಿ ನೋಡಿ.","Sari. Illi nodi.","Of course. Look over here.",
                    C("ಬೈಸಿಕಲ್ ತೋರಿಸಿ.","Bicycle torisi.","Please show me the bicycle.","inspect","account")); break;
                case "inspect": t=new TeaTurn(id,"ಬೈಸಿಕಲ್ ಶೆಡ್ ಒಳಗೆ ಇದೆ. ಸ್ಟ್ಯಾಂಡ್ ಹಾಕಿ ನಿಲ್ಲಿಸಿದ್ದಾರೆ.","Bicycle shed olage ide. Stand haaki nillisiddare.","The bicycle is under the shed, upright on its stand. I found it here on Tuesday. I did not see who left it, or anyone fall into the river. You may inspect it.",
                    C("ಇಲ್ಲಿ ನೋಡಬಹುದಾ?","Illi nodabahuda?","May I examine it?","inspect",activity:VillageActivity.BicycleInspection),
                    C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will come back in a moment.","close")); break;
                case "deduce": t=new TeaTurn(id,"ನೀವು ಏನು ಕಂಡಿರಿ?","Neevu enu kandiri?","What did you find? Compare what you observed with what the booking actually proves.",
                    C("ಸಾಕ್ಷಿಯನ್ನು ನೋಡೋಣ.","Saakshiyannu nodona.","Let's review the evidence.","deduce",activity:VillageActivity.RiverDeduction),
                    C("ಬೈಸಿಕಲ್ ಮತ್ತೆ ನೋಡುತ್ತೇನೆ.","Bicycle matte noduttene.","I will inspect the bicycle again.","inspect",activity:VillageActivity.BicycleInspection)); break;
                case "witness": t=new TeaTurn(id,"ಬಸ್ ಹತ್ತಿದ್ದಾರಾ ಎಂದು ಯಾರನ್ನಾದರೂ ಕೇಳಬೇಕು.","Bus hattiddara endu yaarannaadaru kelabeku.","We still need a witness to boarding. Ask near the northern stop, beside the temple.",
                    C("ಯಾರನ್ನು ಕೇಳಬೇಕು?","Yaarannu kelabeku?","Who should I ask?","temple","temple")); break;
                case "temple": t=new TeaTurn(id,"ದೇವಸ್ಥಾನದ ಹತ್ತಿರ ಗುರುಮೂರ್ತಿ ಮತ್ತು ರತ್ನಮ್ಮ ಇದ್ದಾರೆ.","Devasthaanada hattira Gurumurthy mattu Rathnamma iddare.","Gurumurthy and Rathnamma are often near the temple steps. They may have seen the northern stop on Tuesday.",
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "return": t=new TeaTurn(id,"ರತ್ನಮ್ಮನನ್ನು ಕೇಳಿ. ಅವರು ಏನಾದರೂ ನೋಡಿರಬಹುದು.","Rathnammannu keli. Avaru enaadaru nodirabahudu.","Ask Rathnamma by the temple. She may have seen something; I cannot confirm where Ananya is.",
                    C("ಬೈಸಿಕಲ್ ಮತ್ತೆ ನೋಡುತ್ತೇನೆ.","Bicycle matte noduttene.","I will look at the bicycle again.","inspect",activity:VillageActivity.BicycleInspection),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                default: throw new ArgumentException("Unknown river turn "+id);
            }
            t.Speaker="Giri"; return t;
        }
    }
}
