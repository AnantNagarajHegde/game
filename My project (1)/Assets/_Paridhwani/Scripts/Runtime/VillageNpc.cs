using UnityEngine;
namespace Paridhwani
{
    public enum VillageNpcRole { TeaVendor, Librarian, MessOwner, MarketVendor, RiverKeeper, TempleKeeper, FlowerSeller, PanchayatClerk }
    public sealed class VillageNpc : MonoBehaviour
    {
        public string displayName,roleLabel;
        public VillageNpcRole role;
        public Transform eyes;
        public ITeaConversationProvider CreateProvider() => role==VillageNpcRole.TeaVendor ?
            new LocalTeaConversation() : role==VillageNpcRole.PanchayatClerk ? new PanchayatConversation() : role==VillageNpcRole.MarketVendor ? new MarketConversation() : role==VillageNpcRole.RiverKeeper ? new RiverConversation() : role==VillageNpcRole.TempleKeeper || role==VillageNpcRole.FlowerSeller ? new TempleConversation(role==VillageNpcRole.FlowerSeller) : new VillageChapterConversation(role);
    }
}
