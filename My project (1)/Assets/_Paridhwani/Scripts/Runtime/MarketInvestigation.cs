using System;
namespace Paridhwani
{
    public enum VillageActivity { None, MarketReceipt, BusTimetable, BicycleInspection, BookingSlip, RiverDeduction, ArchiveReference, PanchayatNotice, CheckpostTravel, VillageReturn, CheckpostDirections, RidgeTrailhead, RidgeWayfinding, TrunkLock, DeedReview }
    public interface IActivityConversationProvider
    {
        TeaTurn ResumeAfterActivity(TeaTurn current,TeaProgress progress);
    }

    // Authored chapter facts. Checking a timetable establishes intent, never witnessed boarding.
    public static class MarketInvestigation
    {
        public static bool TryReadReceipt(TeaProgress p,int answer)
        {
            if(!p.marketReceiptCollected || answer!=0) return false;
            p.purchaseTimeKnown=true; return true;
        }
        public static bool TryReadTimetable(TeaProgress p,int answer)
        {
            if(!p.marketAccountKnown || answer!=0) return false;
            p.busDepartureKnown=true; return true;
        }
        public static bool ReadRiverNotice(TeaProgress p)
        {
            if(!p.busDepartureKnown) return false;
            p.riverReportKnown=true; return true;
        }
    }

    public sealed class MarketConversation : ITeaConversationProvider,IActivityConversationProvider
    {
        public TeaTurn Start(TeaProgress p) => Node(
            !p.shoppingNoteCollected || !p.marketLeadKnown ? "early" :
            !p.marketReceiptCollected ? "hello" :
            !p.purchaseTimeKnown ? "receipt" :
            !p.marketAccountKnown ? "purchase" : "bus",p);
        public TeaTurn ResumeAfterActivity(TeaTurn current,TeaProgress p) => Start(p);
        public TeaTurn Reply(TeaTurn current,int index,TeaProgress p)
        {
            if(index<0 || index>=current.Choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var c=current.Choices[index];
            if(c.Effect=="receipt")
            {
                if(!p.shoppingNoteCollected || !p.marketLeadKnown) throw new InvalidOperationException("The shopping note must be introduced first.");
                p.marketReceiptCollected=true;
            }
            if(c.Effect=="account")
            {
                if(!p.purchaseTimeKnown) throw new InvalidOperationException("Read the purchase entry before interpreting the journey.");
                p.marketAccountKnown=true;
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
                case "early": t=new TeaTurn(id,"ನಮಸ್ಕಾರ! ಏನು ಬೇಕು?","Namaskara! Enu beku?","Hello! What would you like?",
                    C("ಧನ್ಯವಾದಗಳು. ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Dhanyavadagalu. Matte baruttene.","Thank you. I will return after visiting Shanthakka.","close")); break;
                case "hello": t=new TeaTurn(id,"ನಮಸ್ಕಾರ! ನಿಮ್ಮ ಬಳಿ ಏನಿದೆ?","Namaskara! Nimma bali enide?","Hello! What have you brought?",
                    C("ಈ ಚೀಟಿ ಅನನ್ಯ ಅವರದು.","Ee cheeti Ananya avaradu.","This shopping note belongs to Ananya.","receipt","receipt"),
                    C("ದಯವಿಟ್ಟು ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Dayavittu nidhanavagi heli.","Please speak slowly.","help")); break;
                case "help": t=new TeaTurn(id,"ಸರಿ. ಚೀಟಿ ತೋರಿಸಿ.","Sari. Cheeti torisi.","Of course. Show me the shopping note.",
                    C("ಇಲ್ಲಿ ನೋಡಿ.","Illi nodi.","Please look at this.","receipt","receipt")); break;
                case "receipt": t=new TeaTurn(id,"ಹೌದು, ಈ ಚೀಟಿ ಗೊತ್ತು. ಇಲ್ಲಿ ದಿನ ಮತ್ತು ಸಮಯ ಇದೆ.","Haudu, ee cheeti gottu. Illi dina mattu samaya ide.","Yes, I recognise this note. My ledger records the day and time.",
                    C("ಸ್ವಲ್ಪ ಕಾಯಿರಿ.","Swalpa kaayiri.","Let me read the entry.","receipt",activity:VillageActivity.MarketReceipt),
                    C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will come back to the entry.","close"));
                    t.Activity=VillageActivity.MarketReceipt; break;
                case "purchase": t=new TeaTurn(id,"ಹೌದು. ಸೋಮವಾರ ಸಂಜೆ ಆರು ಗಂಟೆಗೆ ಬಂದಿದ್ದರು.","Haudu. Somavaara sanje aaru gantege bandiddaru.","That's right. She bought the supplies on Monday at six in the evening.",
                    C("ಯಾವ ಬಸ್ಸಿನಲ್ಲಿ ಹೋಗಬೇಕಿತ್ತು?","Yaava bassinalli hogabekittu?","Which bus did she intend to take?","bus","account"),
                    C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will return in a moment.","close")); break;
                case "bus": t=new TeaTurn(id,"ಮಂಗಳವಾರ ಬೆಳಿಗ್ಗೆ ಗುಡ್ಡ ಗೇಟ್‌ಗೆ ಹೋಗಬೇಕಿತ್ತು. ಬಸ್ ನಿಲ್ದಾಣದಲ್ಲಿ ವೇಳಾಪಟ್ಟಿ ಇದೆ.","Mangalavaara beligge Gudda Gate-ge hogabekittu. Bus nildaanadalli velapatti ide.","She planned to go to Gudda Gate on Tuesday morning. Check Route 14 on the timetable beside the tea stall. This is her plan; I did not see her board.",
                    C("ಸರಿ. ಧನ್ಯವಾದಗಳು.","Sari. Dhanyavadagalu.","All right. I will check the timetable.","close")); break;
                default: throw new ArgumentException("Unknown market turn "+id);
            }
            t.Speaker="Basavaraju"; return t;
        }
    }
}
