using System;
namespace Paridhwani
{
    public sealed class TempleConversation : ITeaConversationProvider
    {
        readonly bool flowers;
        public TempleConversation(bool flowers) { this.flowers=flowers; }
        public TeaTurn Start(TeaProgress p) => Node(!p.templeLeadKnown?"early":!(flowers?p.rathnammaAccountKnown:p.gurumurthyAccountKnown)?"hello":p.gurumurthyAccountKnown && p.rathnammaAccountKnown && !p.witnessAccountsCompared?"compare":p.witnessAccountsCompared && !p.guddaFamilyLeadKnown?"family":"return",p);
        public TeaTurn Reply(TeaTurn t,int index,TeaProgress p)
        {
            if(index<0 || index>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
            var c=t.Choices[index];
            if(c.Effect=="account" && p.templeLeadKnown) { if(flowers) p.rathnammaAccountKnown=true; else p.gurumurthyAccountKnown=true; }
            if(c.Effect=="compare" && p.gurumurthyAccountKnown && p.rathnammaAccountKnown) p.witnessAccountsCompared=true;
            if(c.Effect=="family" && p.witnessAccountsCompared) p.guddaFamilyLeadKnown=true;
            return c.Next=="close"?null:c.Next=="start"?Start(p):Node(c.Next,p);
        }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="") => new TeaChoice(kn,ro,en,next,effect);
        TeaTurn Node(string id,TeaProgress p)
        {
            TeaTurn t;
            switch(id)
            {
                case "early": t=new TeaTurn(id,"ನಮಸ್ಕಾರ.","Namaskara.","Welcome to the public temple forecourt. The northern bus stop is beside the road.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "hello": t=new TeaTurn(id,"ನಮಸ್ಕಾರ.","Namaskara.","Hello. How can I help?",C("ಅನನ್ಯಳನ್ನು ನೋಡಿದಿರಾ?","Ananyalannu nodidira?","Did you see Ananya on Tuesday morning?","account","account"),C("ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Nidhanavagi heli.","Please speak slowly.","help")); break;
                case "help": t=new TeaTurn(id,"ನೋಡಿದಿರಾ?","Nodidira?","Nodidira respectfully asks: did you see? Accounts remain available in your notebook.",C("ಅನನ್ಯಳನ್ನು ನೋಡಿದಿರಾ?","Ananyalannu nodidira?","Did you see Ananya?","account","account")); break;
                case "account": t=new TeaTurn(id,flowers?"ಅವಳು ಬಸ್ಸು ಹತ್ತಿದಳು.":"ಅವಳು ಒಬ್ಬಳೇ ಬಂದಳು.",flowers?"Avalu bassu hattidalu.":"Avalu obbale bandalu.",flowers?"On Tuesday morning I saw Ananya board Route 14 at this stop while I arranged flowers. I did not travel with her and cannot say where she got off.":"On Tuesday morning I saw Ananya come alone along this lane towards the northern stop. I went inside before the bus arrived. I did not see her board.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Record this account and continue.","start")); break;
                case "compare": t=new TeaTurn(id,"ಬಂದಳು. ಹತ್ತಿದಳು.","Bandalu. Hattidalu.","Compare the two accounts. What did each witness see?",C("ಬಂದಳು; ಹತ್ತಿದಳು.","Bandalu; hattidalu.","Gurumurthy saw her come alone; Rathnamma saw her board.","family","compare"),C("ಮನೆಗೆ ತಲುಪಿದಳು.","Manege talupidalu.","Both saw her reach the farmhouse.","retry"),C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will review my notebook first.","close")); break;
                case "retry": t=new TeaTurn(id,"ಮತ್ತೆ ನೋಡಿ.","Matte nodi.","Bandalu means came; hattidalu means boarded. Neither witness followed the bus. Try again.",C("ಸರಿ.","Sari.","Compare again.","compare")); break;
                case "family": t=new TeaTurn(id,"ರತ್ನಮ್ಮನನ್ನು ಕೇಳಿ.","Rathnammannu keli.",flowers?"Ananya asked about Shankare Gowda's family at Gudda Mane. Somanna should check the library records before you choose a route.":"Rathnamma knows the families around Gudda Mane. Ask her about Ananya's question.",flowers?C("ಯಾರ ಮನೆ?","Yaara mane?","Whose home was she asking about?","lead","family"):C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","I will ask Rathnamma.","close")); break;
                case "lead": t=new TeaTurn(id,"ಸೋಮಣ್ಣನನ್ನು ಕೇಳಿ.","Somannannu keli.","Shankare Gowda's family, at Gudda Mane. Return to Somanna with this name. Boarding is witnessed; arrival and Ananya's present safety remain unknown.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. I will return to the library.","close")); break;
                case "return": t=new TeaTurn(id,"ಮತ್ತೆ ಹೇಳಿ.","Matte heli.",p.guddaFamilyLeadKnown?"Your notes keep our accounts separate. Return to Somanna; the archive sequence is the next chapter.":"Speak to the other witness as well. My account remains in your notebook.",C("ಮತ್ತೆ ಹೇಳಿ.","Matte heli.","Please repeat what you saw.","account"),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                default: throw new ArgumentException(id);
            }
            t.Speaker=flowers?"Rathnamma":"Gurumurthy"; return t;
        }
    }
}
