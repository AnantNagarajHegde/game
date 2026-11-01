using System;
using System.Collections.Generic;

namespace Paridhwani
{
    public sealed class TeaLine
    {
        public string Kannada, Roman, English;
        public TeaLine(string kn, string ro, string en) { Kannada=kn; Roman=ro; English=en; }
    }
    public sealed class TeaChoice
    {
        public TeaLine Line;
        public string Next, Effect;
        public VillageActivity Activity;
        public TeaChoice(string kn, string ro, string en, string next, string effect="")
        { Line=new TeaLine(kn,ro,en); Next=next; Effect=effect; }
    }
    public sealed class TeaTurn
    {
        public string Id, Speaker;
        public VillageActivity Activity;
        public TeaLine Line;
        public List<TeaChoice> Choices = new List<TeaChoice>();
        public TeaTurn(string id, string kn, string ro, string en, params TeaChoice[] choices)
        { Id=id; Speaker="Manjunatha"; Line=new TeaLine(kn,ro,en); Choices.AddRange(choices); }
    }
    public interface ITeaConversationProvider
    {
        TeaTurn Start(TeaProgress progress);
        TeaTurn Reply(TeaTurn current, int choice, TeaProgress progress);
    }
    // Authored fixture. No NLP, network access, or string-driven quest commands.
    public sealed class LocalTeaConversation : ITeaConversationProvider
    {
        public TeaTurn Start(TeaProgress p) => Node(p.drinkOrdered ? "return" : "hello", p);
        public TeaTurn Reply(TeaTurn turn, int choice, TeaProgress p)
        {
            if (choice < 0 || choice >= turn.Choices.Count) throw new ArgumentOutOfRangeException(nameof(choice));
            TeaChoice c = turn.Choices[choice];
            switch(c.Effect)
            {
                case "greet": p.greeted=true; break;
                case "routine": p.ananyaRoutineKnown=true; break;
                case "tea": p.drinkOrdered=true; p.drink="tea"; break;
                case "water": p.drinkOrdered=true; p.drink="water"; break;
                case "directions":
                    if (!p.drinkOrdered) throw new InvalidOperationException("A daily exchange must precede the clue.");
                    p.directionsKnown=true; break;
            }
            return c.Next == "close" ? null : Node(c.Next,p);
        }
        public static TeaLine AuthoredLine(string id,TeaProgress p) { var line=Node(id,p).Line; return new TeaLine(line.Kannada,line.Roman,line.English); }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="") =>
            new TeaChoice(kn,ro,en,next,effect);
        static TeaTurn Node(string id, TeaProgress p)
        {
            switch(id)
            {
                case "hello": return new TeaTurn(id,"ನಮಸ್ಕಾರ! ಏನು ಬೇಕು?","Namaskara! Enu beku?","Hello! What would you like?",
                    C("ನಮಸ್ಕಾರ.","Namaskara.","Hello.","order","greet"),
                    C("ನನಗೆ ಕನ್ನಡ ಸ್ವಲ್ಪ ಗೊತ್ತು.","Nanage Kannada swalpa gottu.","I know a little Kannada.","help"));
                case "help": return new TeaTurn(id,"ಪರವಾಗಿಲ್ಲ. ನಿಧಾನವಾಗಿ ಮಾತನಾಡೋಣ.","Paravagilla. Nidhanavagi matadona.","That's all right. Let's speak slowly.",
                    C("ನಮಸ್ಕಾರ.","Namaskara.","Hello.","order","greet"));
                case "order": return new TeaTurn(id,"ಚಹಾ ಬೇಕಾ? ನೀರು ಬೇಕಾ?","Chaha beka? Neeru beka?","Would you like tea or water?",
                    C("ನನಗೆ ಚಹಾ ಬೇಕು.","Nanage chaha beku.","I would like tea.","served","tea"),
                    C("ನನಗೆ ನೀರು ಬೇಕು.","Nanage neeru beku.","I would like water.","served","water"),
                    C("ಚಹಾ ಬೇಡ.","Chaha beda.","No tea, thank you.","clarify"));
                case "clarify": return new TeaTurn(id,"ಸರಿ. ನೀರು ಬೇಕಾ?","Sari. Neeru beka?","Of course. Would you like some water?",
                    C("ಹೌದು, ನೀರು ಕೊಡಿ.","Haudu, neeru kodi.","Yes, water please.","served","water"),
                    C("ನನಗೆ ಚಹಾ ಬೇಕು.","Nanage chaha beku.","I would like tea.","served","tea"),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. I will return.","close"));
                case "served": return new TeaTurn(id,"ಇಲ್ಲಿ ಕುಳಿತುಕೊಳ್ಳಿ. ನೀವು ಎಲ್ಲಿಗೆ ಹೋಗಬೇಕು?","Illi kulithukolli. Neevu ellige hogabeku?","Take a seat here. Where do you need to go?",
                    C("ಗ್ರಂಥಾಲಯ ಎಲ್ಲಿದೆ?","Granthalaya ellide?","Where is the library?","directions","directions"),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. I'll ask in a moment.","close"));
                case "return": return new TeaTurn(id,"ಮತ್ತೆ ನಮಸ್ಕಾರ! ಏನು ಸಹಾಯ ಬೇಕು?","Matte namaskara! Enu sahaya beku?","Hello again! How can I help?",
                    C("ಗ್ರಂಥಾಲಯ ಎಲ್ಲಿದೆ?","Granthalaya ellide?","Where is the library?","directions","directions"),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you. See you later.","close"));
                case "directions": return new TeaTurn(id,"ಮುಂದೆ ಹೋಗಿ. ಎಡಕ್ಕೆ ತಿರುಗಿ. ಅಲ್ಲಿ ಗ್ರಂಥಾಲಯ ಇದೆ.","Munde hogi. Edakke tirugi. Alli granthalaya ide.","Go straight along the lane, then turn left. The library is there.",
                    C("ಅನನ್ಯ ಇದ್ದಾರಾ?","Ananya iddara?","Is Ananya there?","ananya","routine"),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close"));
                case "ananya": return new TeaTurn(id,"ಅನನ್ಯ ನಿನ್ನೆ ಬಂದಿಲ್ಲ. ಸೋಮಣ್ಣನನ್ನು ಕೇಳಿ.","Ananya ninne bandilla. Somannanna keli.","Ananya didn't come yesterday. Ask Somanna at the library.",
                    C("ಸರಿ. ಧನ್ಯವಾದಗಳು.","Sari. Dhanyavadagalu.","All right. Thank you.","close"));
                default: throw new ArgumentException("Unknown authored turn: "+id);
            }
        }
    }
}
