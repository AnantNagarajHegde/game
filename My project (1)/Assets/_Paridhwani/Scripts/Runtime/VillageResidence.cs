using UnityEngine;

namespace Paridhwani
{
    // Authored, shared house meshes already include their construction detail.
    public sealed class VillageResidence : MonoBehaviour
    {
        public string style;
        public bool scenery;
        void Awake()
        {
            // ForceLOD is not serialized by Unity; reapply it when loading a saved scene/player.
            if (scenery && TryGetComponent<LODGroup>(out var group)) group.ForceLOD(1);
        }
    }
}
