using UnityEngine;
namespace Paridhwani
{
    public sealed partial class TeaStallSession
    {
        public Transform checkpostArrival,villageArrival,checkpostBoard,ridgeEntrance;
        public GameObject ridgeGate;
        public bool ridgeRouteChecked;
        ITeaConversationProvider CreateConversation(VillageNpc npc)
        {
            if(npc.role==VillageNpcRole.AutoDriver || npc.role==VillageNpcRole.CheckpostDriver) return new TravelConversation(AtCheckpost);
            if(npc.role==VillageNpcRole.Ananya && Progress.deedRetrieved) return new ReturnConversation();
            if(npc.role==VillageNpcRole.ForestGuard) return new CheckpostConversation(ridgeRouteChecked);
            return npc.role==VillageNpcRole.Librarian && Progress.guddaFamilyLeadKnown?new ArchiveConversation():npc.CreateProvider();
        }
        bool AtCheckpost => CheckpostInvestigation.IsCheckpostPosition(player.transform.position);
        void ApplyCheckpostState()
        {
            CheckpostInvestigation.RefreshPermission(Progress,ridgeRouteChecked);
            if(ridgeGate) ridgeGate.SetActive(!Progress.trailPermission);
        }
        bool FindCheckpostInteraction()
        {
            if(nearbyNpc) return false;
            if(checkpostBoard && Vector3.Distance(player.transform.position,checkpostBoard.position)<2.3f)
            { Nearby="Read the road closure and ridge directions"; nearbyInspection=7; return true; }
            if(ridgeEntrance && Vector3.Distance(player.transform.position,ridgeEntrance.position)<2.3f)
            { Nearby="Review the ridge trailhead"; nearbyInspection=8; return true; }
            return false;
        }
        public void ConfirmTravel()
        {
            bool outward=ActiveActivity==VillageActivity.CheckpostTravel;
            if(ui.Mode!="evidence" || (!outward && ActiveActivity!=VillageActivity.VillageReturn)) return;
            var target=outward?checkpostArrival:villageArrival;
            if(!target || (outward && !CheckpostInvestigation.CanTravel(Progress))) return;
            ResetModalContext(); Turn=null; ActiveNpc=null; player.EndConversation();
            if(outward) Progress.checkpostVisited=true;
            player.Restore(target.position,target.eulerAngles.y); ApplyState(); if(!outward) CompleteReturnAtVillage(); Save();
            ui.ShowTravelArrival(outward); SetCursor(true);
        }
        string CheckpostHint()
        {
            if(AtCheckpost) return Progress.trailPermission?"Marked ridge entrance / left at the stone marker":"KENCHA / CHECKPOST SHELTER";
            if(Progress.checkpostDestinationKnown && villagers!=null) foreach(var n in villagers)
                if(n && n.role==VillageNpcRole.AutoDriver) return Mathf.CeilToInt(Vector3.Distance(player.transform.position,n.transform.position))+" m / RAGHAVENDRA / AUTO PICKUP";
            return "";
        }
    }
}
