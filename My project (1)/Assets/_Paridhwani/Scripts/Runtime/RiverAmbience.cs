using UnityEngine;
namespace Paridhwani
{
    public sealed class RiverAmbience : MonoBehaviour
    {
        public TeaAmbience preferences;
        VillageRecordedLoop source;
        void Start()
        {
            source=gameObject.AddComponent<VillageRecordedLoop>();source.Configure("River",true,34);
        }
        void Update() {
            if(!source)return;
            var session=preferences?preferences.GetComponent<TeaStallSession>():null;
            float duck=session && session.ui && session.ui.Mode=="conversation"?.65f:1;
            source.Gain=!preferences || !preferences.isActiveAndEnabled || preferences.muted || !Application.isFocused?0:.32f*duck*PresentationPolicy.Volume(preferences.volume);
        }
        void OnDisable(){if(source)source.Gain=0;}
    }
}
