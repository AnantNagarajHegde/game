using UnityEngine;
using UnityEngine.Rendering;
namespace Paridhwani
{
    // A stable overcast ambient probe, including in an unbaked standalone scene.
    public sealed class VillageLighting : MonoBehaviour
    {
        void OnEnable()
        {
            var probe=new SphericalHarmonicsL2();
            probe.AddAmbientLight(new Color(.24f,.27f,.23f));
            probe.AddDirectionalLight(Vector3.up,new Color(.12f,.14f,.13f),1);
            RenderSettings.ambientProbe=probe;
        }
    }
}
