using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Paridhwani
{
    // Runtime-only overrides: regeneration and saved display preferences remain independent.
    public static class VillageArtDirection
    {
        public static void Configure(VolumeProfile profile, ColorAdjustments color)
        {
            color.contrast.Override(9);
            color.saturation.Override(-4);
            color.colorFilter.Override(new Color(1f, .985f, .955f));
            var tone = profile.Add<Tonemapping>(false);
            tone.mode.Override(TonemappingMode.ACES);
            var vignette = profile.Add<Vignette>(false);
            vignette.color.Override(new Color(.025f, .035f, .045f));
            vignette.intensity.Override(.12f);
            vignette.smoothness.Override(.65f);
            // Keep wet roofs and signage crisp. No depth blur, grain or bloom washout.
            var bloom = profile.Add<Bloom>(false);
            bloom.intensity.Override(0);
        }
    }
}
