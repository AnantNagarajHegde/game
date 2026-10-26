using System;
namespace Paridhwani
{
    // Authored scene fixtures; the external NLP engine can replace this provider later.
    public sealed class VillageChapterConversation : ITeaConversationProvider
    {
        readonly VillageNpcRole role;
        public VillageChapterConversation(VillageNpcRole role) { this.role=role; }
        public TeaTurn Start(TeaProgress p) => Node(role==VillageNpcRole.Librarian ?
            (p.metSomanna?"library_return":"library_hello") :
            (!p.messLeadKnown?"mess_early":p.shoppingNoteCollected?(p.marketLeadKnown?"mess_return":"mess_note"):p.servingComplete?"meal_ready":p.mealOrdered?"meal":"mess_hello"),p);
        public TeaTurn Reply(TeaTurn turn,int index,TeaProgress p)
        {
            if(index<0 || index>=turn.Choices.Count) throw new ArgumentOutOfRangeException(nameof(index));
            TeaChoice choice=turn.Choices[index];
            switch(choice.Effect)
            {
                case "library_greet": p.metSomanna=true; break;
                case "mess_lead": p.messLeadKnown=true; break;
                case "market_lead": p.marketLeadKnown=true; break;
                case "meal": p.mealOrdered=true; break;
                case "water": p.mealOrdered=true; break;
                case "enough": p.servingComplete=true; break;
                case "note":
                    if(!p.messLeadKnown || !p.servingComplete) throw new InvalidOperationException("Finish the daily exchange before collecting evidence.");
                    p.shoppingNoteCollected=true; break;
            }
            return choice.Next=="close"?null:Node(choice.Next,p);
        }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="") =>
            new TeaChoice(kn,ro,en,next,effect);
        TeaTurn Node(string id,TeaProgress p)
        {
            TeaTurn turn;
            switch(id)
            {
                case "library_hello": turn=new TeaTurn(id,"ನಮಸ್ಕಾರ. ನೀವು ಯಾರು?","Namaskara. Neevu yaaru?","Hello. Who are you?",
                    C("ನಮಸ್ಕಾರ. ನಾನು ಹೊಸ ಕೆಲಸಕ್ಕೆ ಬಂದಿದ್ದೇನೆ.","Namaskara. Naanu hosa kelasakke bandiddene.","Hello. I have come for the new job.","library_key","library_greet"),
                    C("ದಯವಿಟ್ಟು ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Dayavittu nidhanavagi heli.","Please speak slowly.","library_help")); break;
                case "library_help": turn=new TeaTurn(id,"ಸರಿ. ನಿಮ್ಮ ಹೆಸರು ಏನು?","Sari. Nimma hesaru enu?","Of course. What is your name?",
                    C("ನಾನು ಹೊಸ ಕೆಲಸಕ್ಕೆ ಬಂದಿದ್ದೇನೆ.","Naanu hosa kelasakke bandiddene.","I am here for the new job.","library_key","library_greet")); break;
                case "library_key": turn=new TeaTurn(id,"ಅನನ್ಯ ಇಲ್ಲ. ಅವರ ಬಳಿ ಇನ್ನೊಂದು ಕೀಲಿ ಇದೆ.","Ananya illa. Avara bali innondu keeli ide.","Ananya isn't here. She has the duplicate key.",
                    C("ದಯವಿಟ್ಟು ಬಾಗಿಲು ತೆರೆಯಿರಿ.","Dayavittu baagilu tereyiri.","Please open the door.","library_lead","mess_lead"),
                    C("ಅನನ್ಯ ಎಲ್ಲಿದ್ದಾರೆ?","Ananya elliddare?","Where is Ananya?","library_lead","mess_lead")); break;
                case "library_return": turn=new TeaTurn(id,"ಮತ್ತೆ ನಮಸ್ಕಾರ. ಏನು ಬೇಕು?","Matte namaskara. Enu beku?","Hello again. What do you need?",
                    C("ಅನನ್ಯ ಎಲ್ಲಿದ್ದಾರೆ?","Ananya elliddare?","Where is Ananya?","library_lead","mess_lead"),
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "library_lead": turn=new TeaTurn(id,"ನನ್ನ ಬಳಿ ಮುಖ್ಯ ಕೀಲಿ ಇದೆ. ಸ್ವಲ್ಪ ಕಾಯಿರಿ. ಅನನ್ಯ ಶಾಂತಕ್ಕನ ಮನೆಗೆ ಹೋಗಿದ್ದರು.","Nanna bali mukhya keeli ide. Swalpa kaayiri. Ananya Shanthakkana manege hogiddaru.","I have the master key. Give me a little time to arrange access. Ananya went to Shanthakka's mess. Ask her, across the lane.",
                    C("ಸರಿ. ಧನ್ಯವಾದಗಳು.","Sari. Dhanyavadagalu.","All right. Thank you.","close")); break;
                case "mess_early": turn=new TeaTurn(id,"ನಮಸ್ಕಾರ! ನೀರು ಬೇಕಾ?","Namaskara! Neeru beka?","Welcome. Would you like water? Somanna at the library is expecting the new archivist.",
                    C("ಧನ್ಯವಾದಗಳು. ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Dhanyavadagalu. Matte baruttene.","Thank you. I'll come back after meeting him.","close")); break;
                case "mess_hello": turn=new TeaTurn(id,"ನಮಸ್ಕಾರ! ಊಟ ಬೇಕಾ?","Namaskara! Oota beka?","Hello! Would you like a meal?",
                    C("ಹೌದು, ಊಟ ಕೊಡಿ.","Haudu, oota kodi.","Yes, a meal please.","meal","meal"),
                    C("ಸ್ವಲ್ಪ ನೀರು ಕೊಡಿ.","Swalpa neeru kodi.","Just a little water, please.","meal","water"),
                    C("ಊಟ ಬೇಡ. ಧನ್ಯವಾದಗಳು.","Oota beda. Dhanyavadagalu.","No meal, thank you. I'll return.","close")); break;
                case "meal": turn=new TeaTurn(id,"ಇನ್ನೂ ಸ್ವಲ್ಪ ಬೇಕಾ?","Innu swalpa beka?","Would you like a little more?",
                    C("ಸಾಕು. ಧನ್ಯವಾದಗಳು.","Saaku. Dhanyavadagalu.","That's enough. Thank you.","meal_ready","enough"),
                    C("ಸ್ವಲ್ಪ ಕೊಡಿ.","Swalpa kodi.","A little more, please.","meal_more")); break;
                case "meal_more": turn=new TeaTurn(id,"ಸರಿ. ಇನ್ನೂ ಸ್ವಲ್ಪ ಕೊಡುತ್ತೇನೆ.","Sari. Innu swalpa koduttene.","Of course. Here is a little more.",
                    C("ಸಾಕು. ಧನ್ಯವಾದಗಳು.","Saaku. Dhanyavadagalu.","That's enough. Thank you.","meal_ready","enough")); break;
                case "meal_ready": turn=new TeaTurn(id,"ನಿಮಗೆ ಇನ್ನೇನು ಬೇಕು?","Nimage innenu beku?","What else can I help you with?",
                    C("ಅನನ್ಯ ನಿನ್ನೆ ಬಂದಿದ್ದರಾ?","Ananya ninne bandiddara?","Did Ananya come yesterday?","mess_note","note")); break;
                case "mess_note": turn=new TeaTurn(id,"ಹೌದು. ಬೆಳಿಗ್ಗೆ ಎರಡು ಊಟ ತೆಗೆದುಕೊಂಡರು. ಈ ಚೀಟಿ ಇಲ್ಲೇ ಬಿಟ್ಟಿದ್ದರು.","Haudu. Beligge eradu oota tegedukondaru. Ee cheeti ille bittiddaru.","Yes. She collected two packed meals yesterday morning. She left this shopping note here.",
                    C("ಈ ಚೀಟಿ ಯಾರದು?","Ee cheeti yaaradu?","Whose note is this?","mess_market","market_lead")); break;
                case "mess_market": turn=new TeaTurn(id,"ಅನನ್ಯ ಅವರದು. ಮಾರುಕಟ್ಟೆಯಲ್ಲಿ ಬಸವರಾಜುವನ್ನು ಕೇಳಿ.","Ananya avaradu. Maarukatteyalli Basavarajuvannu keli.","It is Ananya's. Ask Basavaraju at the market about the purchases.",
                    C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                case "mess_return": turn=new TeaTurn(id,"ಚೀಟಿ ನಿಮ್ಮ ಬಳಿ ಇದೆಯಾ?","Cheeti nimma bali ideya?","Do you still have the note? Basavaraju can help you with it.",
                    C("ಹೌದು. ಧನ್ಯವಾದಗಳು.","Haudu. Dhanyavadagalu.","Yes. Thank you.","close")); break;
                default: throw new ArgumentException("Unknown village turn "+id);
            }
            turn.Speaker=role==VillageNpcRole.Librarian?"Somanna":"Shanthakka";
            return turn;
        }
    }
}
