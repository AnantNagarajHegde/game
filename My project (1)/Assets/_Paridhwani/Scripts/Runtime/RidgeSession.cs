using UnityEngine;
namespace Paridhwani
{
    public sealed partial class TeaStallSession
    {
        public Transform ridgeMarker,farmArrival;
        public GameObject farmGate;
        void UpdateFarmArrival()
        {
            if(!ui.IsModal && !Progress.guddaManeReached && farmArrival && RidgeInvestigation.Arrive(Progress,ridgeRouteChecked,Vector3.Distance(player.transform.position,farmArrival.position)<2)) { ApplyState(); Save(); }
        }
        bool FindRidgeInteraction()
        {
            if(nearbyNpc || !ridgeMarker || Vector3.Distance(player.transform.position,ridgeMarker.position)>2.3f) return false;
            Nearby="Read the ridge stone marker"; nearbyInspection=9; return true;
        }
        string RidgeHint()
        {
            if(!AtCheckpost) return "";
            if(Progress.deedRetrieved) return "ANANYA / RETURN TOGETHER";
            if(Progress.trunkLocationKnown) return "FARM ARCHIVE / FAMILY TRUNK";
            if(Progress.guddaManeReached) return Progress.trunkLocationKnown?"Trunk recorded / lock and deed come next":Progress.disappearanceExplained?"KAMALAMMA / FAMILY RECORDS":"ANANYA / FARM VERANDA";
            if(Progress.ridgeWayfindingKnown && Progress.trailPermission) return "Follow the fenced ridge path to Gudda Mane";
            return Progress.trailPermission?"STONE MARKER / LEFT PATH / FOLLOW THE FENCE":"";
        }
    }
}
