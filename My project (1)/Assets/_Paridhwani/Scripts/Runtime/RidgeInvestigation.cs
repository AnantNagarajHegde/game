using System;
namespace Paridhwani
{
    public static class RidgeInvestigation
    {
        public static bool CanWalk(TeaProgress p,bool routeChecked) => CheckpostInvestigation.CanPermit(p,routeChecked);
        public static bool Answer(TeaProgress p,int answer,bool routeChecked,bool nearMarker)
        { if(!CanWalk(p,routeChecked) || !nearMarker || answer!=0) return false; p.ridgeWayfindingKnown=true; return true; }
        public static bool Arrive(TeaProgress p,bool routeChecked,bool nearFarm)
        { if(!CanWalk(p,routeChecked) || !p.ridgeWayfindingKnown || !nearFarm) return false; p.ridgeCrossed=true; p.guddaManeReached=true; return true; }
        public static bool CanReunite(TeaProgress p) => p.ridgeCrossed && p.guddaManeReached && p.ridgeWayfindingKnown;
    }
    public sealed class FarmConversation : ITeaConversationProvider
    {
        readonly bool elder;
        public FarmConversation(bool elder) { this.elder=elder; }
        public TeaTurn Start(TeaProgress p) => Node(!RidgeInvestigation.CanReunite(p)?"early":elder?(!p.disappearanceExplained?"wait":p.trunkLocationKnown?"elder_return":"elder_hello"):!p.ananyaFound?"hello":!p.disappearanceExplained?"explain":!p.farmHospitalityKnown?"drink":"return");
        public TeaTurn Reply(TeaTurn t,int i,TeaProgress p)
        {
            if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); var c=t.Choices[i];
            if(c.Effect.Length>0 && !RidgeInvestigation.CanReunite(p)) throw new InvalidOperationException("Reach Gudda Mane by the checked ridge first.");
            switch(c.Effect) {
                case "found": p.ananyaFound=true; break;
                case "explain": if(!p.ananyaFound) throw new InvalidOperationException("Meet Ananya first."); p.disappearanceExplained=true; break;
                case "drink": p.farmHospitalityKnown=true; break;
                case "trunk": if(!p.disappearanceExplained) throw new InvalidOperationException("Hear Ananya's account first."); p.trunkLocationKnown=true; break;
            }
            return c.Next=="close"?null:Node(c.Next);
        }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="") => new TeaChoice(kn,ro,en,next,effect);
        TeaTurn Node(string id)
        {
            TeaTurn t;
            switch(id) {
                case "early": t=new TeaTurn(id,"ನಮಸ್ಕಾರ.","Namaskara.","Follow the marked approach and speak to Kencha before entering the farm.",C("ಸರಿ.","Sari.","All right.","close")); break;
                case "hello": t=new TeaTurn(id,"ನೀವು ಬಂದಿರಾ!","Neevu bandira!","You found me! I am Ananya. I'm all right. Has Somanna sent you? Come under the veranda out of the rain.",C("ನಮಸ್ಕಾರ, ಅನನ್ಯ.","Namaskara, Ananya.","Hello, Ananya. We were worried about you.","explain","found"),C("ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Nidhanavagi heli.","Please speak slowly.","help")); break;
                case "help": t=new TeaTurn(id,"ನಾನು ಅನನ್ಯ.","Naanu Ananya.","I am Ananya. You may take your time. Come into the shelter and tell me how you found this place.",C("ನಿಮ್ಮನ್ನು ಹುಡುಕುತ್ತಿದ್ದೆ.","Nimmannu hudukuttidde.","I was looking for you.","explain","found")); break;
                case "explain": t=new TeaTurn(id,"ಮಳೆಯ ಕಾರಣ ಹಿಂತಿರುಗಲು ಆಗಲಿಲ್ಲ.","Maleya kaarana hintirugalu aagalilla.","I came on Tuesday's bus to find the family's original deed. The culvert washed out after I arrived; the ridge was blocked too. My phone had no signal here. I had left Somanna a note, but it seems he never received it. Kamalamma sent word through Kencha when she could.",C("ಈಗ ಅರ್ಥವಾಯಿತು.","Eega arthavaayitu.","I understand. The blocked return and missed message explain the delay.","drink","explain"),C("ಮತ್ತೆ ಹೇಳಿ.","Matte heli.","Please repeat that.","explain"),C("ಸ್ವಲ್ಪ ಸಮಯ ಬೇಕು.","Swalpa samaya beku.","Let me read my notes first.","close")); break;
                case "drink": t=new TeaTurn(id,"ಸ್ವಲ್ಪ ಕಷಾಯ ಬೇಕೇ?","Swalpa kashaaya beke?","Would you like a little kashaya, or water? Kamalamma has kept both under the veranda. Then we can talk about the records.",C("ಸ್ವಲ್ಪ ಕಷಾಯ ಕೊಡಿ.","Swalpa kashaaya kodi.","A little kashaya, please.","records","drink"),C("ಸ್ವಲ್ಪ ನೀರು ಕೊಡಿ.","Swalpa neeru kodi.","A little water, please.","records","drink"),C("ಈಗ ಬೇಡ. ಧನ್ಯವಾದಗಳು.","Eega beda. Dhanyavadagalu.","Not now, thank you.","records","drink")); break;
                case "records": t=new TeaTurn(id,"ಕಮಲಮ್ಮನನ್ನು ಕೇಳೋಣ.","Kamalammannu kelona.","The catalogue led me to the right family records, but the original deed is still in Kamalamma's trunk. Ask her where it is kept. We must protect the document for the council's review.",C("ಕಮಲಮ್ಮನನ್ನು ಕೇಳುತ್ತೇನೆ.","Kamalammannu keluttene.","I will ask Kamalamma.","close")); break;
                case "return": t=new TeaTurn(id,"ಮತ್ತೆ ಮಾತನಾಡೋಣ.","Matte maatadona.","I'm here with Kamalamma. We still need the original deed; finding me has not resolved the library clearance. Ask her about the trunk.",C("ಏನಾಯಿತು?","Enaayitu?","Tell me again why you could not return.","explain"),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "wait": t=new TeaTurn(id,"ಒಳಗೆ ಬನ್ನಿ.","Olage banni.","Welcome to Gudda Mane. Ananya is on the veranda. Speak to her first; she can explain why she came.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. I will speak to her.","close")); break;
                case "elder_hello": t=new TeaTurn(id,"ಯಾವ ಪತ್ರ ಬೇಕು?","Yaava patra beku?","Which document are you looking for?",C("ಗ್ರಂಥಾಲಯದ ಮೂಲ ಪತ್ರ ಬೇಕು.","Granthalayada moola patra beku.","We need the original deed relating to the library.","trunk","trunk"),C("ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Nidhanavagi heli.","Please speak slowly.","elder_help")); break;
                case "elder_help": t=new TeaTurn(id,"ಮೂಲ ಪತ್ರವೇ?","Moola patrave?","Moola patra means the original document. Is that what you need?",C("ಹೌದು. ಮೂಲ ಪತ್ರ ಬೇಕು.","Haudu. Moola patra beku.","Yes, the original document, please.","trunk","trunk")); break;
                case "trunk": t=new TeaTurn(id,"ಕೋಣೆಯ ಒಳಗೆ ಪೆಟ್ಟಿಗೆ ಇದೆ.","Koneya olage pettige ide.","The tin trunk is inside the archive room. The old lock reminder is twelve months, seven days. Ananya has located the record, but the trunk is still closed. We will open it together.",C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. I have noted the trunk and reminder.","close")); break;
                case "elder_return": t=new TeaTurn(id,"ಪೆಟ್ಟಿಗೆ ಒಳಗಿದೆ.","Pettige olagide.","The trunk is in the archive room. Remember: twelve months, seven days. Opening the lock and reviewing the deed are the next implementation cycle.",C("ಮತ್ತೆ ಹೇಳಿ.","Matte heli.","Repeat where it is kept.","trunk"),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                default: throw new ArgumentException(id);
            }
            t.Speaker=elder?"Kamalamma":"Ananya"; return t;
        }
    }
}
