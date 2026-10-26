using System;
using UnityEngine;
namespace Paridhwani
{
    public static class CheckpostInvestigation
    {
        public static bool CanTravel(TeaProgress p) => p.thursdayStarted && p.deedRequired && p.checkpostDestinationKnown;
        public static bool IsCheckpostPosition(Vector3 v) => v.x>=40 && v.x<=70 && v.z>=-10 && v.z<=20;
        public static bool CanPermit(TeaProgress p,bool checkedRoute) => CanTravel(p) && p.checkpostVisited && p.checkpostAccountKnown && p.trailInstructionsUnderstood && checkedRoute;
        public static void RefreshPermission(TeaProgress p,bool checkedRoute)
        { p.ridgeRouteOpen=p.checkpostAccountKnown && checkedRoute; p.trailPermission=CanPermit(p,checkedRoute); }
        public static bool Answer(TeaProgress p,int answer,bool checkedRoute)
        {
            if(!CanTravel(p) || !p.checkpostVisited || !p.checkpostAccountKnown || answer!=1) return false;
            p.trailInstructionsUnderstood=true; RefreshPermission(p,checkedRoute); return true;
        }
    }
    public sealed class TravelConversation : ITeaConversationProvider
    {
        readonly bool returning;
        public TravelConversation(bool returning) { this.returning=returning; }
        public TeaTurn Start(TeaProgress p)
        {
            var activity=returning?VillageActivity.VillageReturn:VillageActivity.CheckpostTravel;
            if(!returning && !CheckpostInvestigation.CanTravel(p)) return new TeaTurn("early","ನಮಸ್ಕಾರ.","Namaskara.","I drive as far as the open forest-checkpost approach. Finish checking the library documents and the panchayat notice first.",new TeaChoice("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")){Speaker="Raghavendra"};
            return new TeaTurn("travel","ಹೋಗೋಣವೇ?","Hogonaave?",returning?"Ready to return to the village? Your evidence stays in the notebook.":"I can take you to the forest checkpost, not across the damaged river road. Ask Kencha before continuing on foot.",new TeaChoice("ಹೋಗೋಣ.","Hogona.",returning?"Review the return journey.":"Review the checkpost journey.","travel"){Activity=activity},new TeaChoice("ಇನ್ನೂ ಬೇಡ.","Innu beda.","Not yet.","close")){Speaker="Raghavendra"};
        }
        public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) { if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); return null; }
    }
    public sealed class CheckpostConversation : ITeaConversationProvider,IActivityConversationProvider
    {
        readonly bool routeChecked;
        public CheckpostConversation(bool routeChecked) { this.routeChecked=routeChecked; }
        public TeaTurn Start(TeaProgress p) => Node(!CheckpostInvestigation.CanTravel(p) || !p.checkpostVisited?"early":!p.checkpostAccountKnown?"purpose":p.trailPermission?"ready":"route");
        public TeaTurn ResumeAfterActivity(TeaTurn t,TeaProgress p) => Start(p);
        public TeaTurn Reply(TeaTurn t,int i,TeaProgress p)
        {
            if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); var c=t.Choices[i];
            if(c.Effect=="account") { if(!CheckpostInvestigation.CanTravel(p) || !p.checkpostVisited) throw new InvalidOperationException("Reach the checkpost after the notice."); p.checkpostAccountKnown=true; p.ananyaLocationReported=true; CheckpostInvestigation.RefreshPermission(p,routeChecked); }
            if(c.Effect=="recheck") CheckpostInvestigation.RefreshPermission(p,routeChecked);
            return c.Next=="close"?null:Node(c.Next);
        }
        static TeaChoice C(string kn,string ro,string en,string next,string effect="",VillageActivity activity=VillageActivity.None) => new TeaChoice(kn,ro,en,next,effect){Activity=activity};
        TeaTurn Node(string id)
        {
            TeaTurn t;
            switch(id)
            {
                case "early": t=new TeaTurn(id,"ದಾರಿ ಮುಚ್ಚಿದೆ.","Daari muchchide.","Stay behind the road barrier. Speak to the village office before planning the Gudda Mane journey.",C("ಸರಿ.","Sari.","All right.","close")); break;
                case "purpose": t=new TeaTurn(id,"ಏಕೆ ಬಂದಿದ್ದೀರಿ?","Eke bandiddiri?","Why have you come?",C("ಅನನ್ಯಳನ್ನು ಹುಡುಕುತ್ತಿದ್ದೇನೆ.","Ananyalannu hudukuttiddene.","I am looking for Ananya and the family records at Gudda Mane.","account","account"),C("ನಿಧಾನವಾಗಿ ಹೇಳಿ.","Nidhanavagi heli.","Please speak slowly.","help")); break;
                case "help": t=new TeaTurn(id,"ಯಾವ ಸಹಾಯ ಬೇಕು?","Yaava sahaaya beku?","What help do you need? Tell me who you are looking for. You may use the supported phrase.",C("ಅನನ್ಯಳನ್ನು ಹುಡುಕುತ್ತಿದ್ದೇನೆ.","Ananyalannu hudukuttiddene.","I am looking for Ananya.","account","account")); break;
                case "account": t=new TeaTurn(id,"ಮಳೆಯ ಕಾರಣ ರಸ್ತೆ ಮುಚ್ಚಿದೆ.","Maleya kaarana raste muchchide.","Rain washed out the road culvert after Tuesday's bus passed. Kamalamma reported that a young woman in yellow, matching Ananya's description, reached Gudda Mane. That is a report from the house; you have not met her yet. The ridge was blocked by fallen branches.",C("ಈಗ ಯಾವ ದಾರಿ?","Eega yaava daari?","Which route is safe now?","route")); break;
                case "route": t=new TeaTurn(id,"ಗುಡ್ಡದ ದಾರಿ.","Guddada daari.",routeChecked?"Our team has checked and cleared the marked ridge approach today. Take the left path at the stone marker, then follow the fence uphill. Do not cross the road barrier. Review the route before entering.":"The ridge has not been checked yet. You may learn the directions, but stay at the shelter until it is cleared. A correct sentence does not open an unsafe route.",C("ದಾರಿ ತೋರಿಸಿ.","Daari torisi.","Show me the route instructions.","route",activity:VillageActivity.CheckpostDirections),C("ದಾರಿ ತೆರೆದಿದೆಯೇ?","Daari teredideye?","Please check the route status again.","route","recheck"),C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will wait or return to town.","close")); break;
                case "ready": t=new TeaTurn(id,"ಎಡಕ್ಕೆ ಹೋಗಿ.","Edakke hogi.","Use the checked ridge entrance: left at the stone marker, then follow the fence uphill. There is a raincoat at the shelter for the onward hike. The damaged road stays closed.",C("ಮತ್ತೆ ತೋರಿಸಿ.","Matte torisi.","Review the route again.","ready",activity:VillageActivity.CheckpostDirections),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
                default: throw new ArgumentException(id);
            }
            t.Speaker="Kencha"; return t;
        }
    }
}
