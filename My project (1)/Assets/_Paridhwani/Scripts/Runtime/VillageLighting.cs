using UnityEngine;
using UnityEngine.Rendering;

namespace Paridhwani
{
    // Runtime ambient probe also works for the unbaked village and additive world.
    public sealed class VillageLighting : MonoBehaviour
    {
        SphericalHarmonicsL2 previousProbe;
        Material originalSky, runtimeSky;
        Light sun;
        Color fogColor, sunColor, horizon, zenith;
        float fogDensity, sunIntensity, shadowStrength;
        bool captured;
        float lastClear = -1;

        void OnEnable()
        {
            previousProbe = RenderSettings.ambientProbe;
            SetProbe(0);
        }

        static void SetProbe(float clear)
        {
            var probe = new SphericalHarmonicsL2();
            // Broad cool sky fill keeps faces and plaster readable beneath the eaves.
            // A small warm ground bounce separates timber from the rain-dark road.
            probe.AddAmbientLight(Color.Lerp(new Color(.35f,.38f,.39f), new Color(.40f,.42f,.39f), clear));
            probe.AddDirectionalLight(Vector3.up, Color.Lerp(new Color(.17f,.20f,.22f), new Color(.22f,.24f,.23f), clear), 1);
            probe.AddDirectionalLight(Vector3.down, new Color(.055f,.039f,.025f), 1);
            RenderSettings.ambientProbe = probe;
        }

        public void SetClearWeather(float clear)
        {
            clear = Mathf.Clamp01(clear);
            if (!captured)
            {
                captured = true;
                fogColor = RenderSettings.fogColor; fogDensity = RenderSettings.fogDensity;
                sun = RenderSettings.sun;
                if (!sun) { var sunObject = GameObject.Find("Overcast sun"); if (sunObject) sun = sunObject.GetComponent<Light>(); }
                if (sun) { sunColor = sun.color; sunIntensity = sun.intensity; shadowStrength = sun.shadowStrength; }
                originalSky = RenderSettings.skybox;
                if (originalSky && originalSky.HasProperty("_Horizon") && originalSky.HasProperty("_Zenith"))
                {
                    horizon = originalSky.GetColor("_Horizon"); zenith = originalSky.GetColor("_Zenith");
                    runtimeSky = Instantiate(originalSky); runtimeSky.name = "Village runtime weather sky";
                    RenderSettings.skybox = runtimeSky;
                }
            }
            if (Mathf.Abs(lastClear - clear) < .002f && ((clear > 0 && clear < 1) || Mathf.Approximately(lastClear,clear))) return;
            lastClear = clear;
            SetProbe(clear);
            RenderSettings.fogColor = Color.Lerp(fogColor, new Color(.69f,.72f,.66f), clear);
            RenderSettings.fogDensity = Mathf.Lerp(fogDensity, fogDensity * .65f, clear);
            if (sun)
            {
                sun.color = Color.Lerp(Color.Lerp(sunColor, new Color(.91f,.95f,1f), .3f), new Color(1f,.91f,.76f), clear);
                sun.intensity = Mathf.Lerp(sunIntensity * 1.12f, sunIntensity * 1.55f, clear);
                sun.shadowStrength = Mathf.Lerp(Mathf.Min(shadowStrength,.65f), Mathf.Max(shadowStrength,.55f), clear);
            }
            if (runtimeSky)
            {
                runtimeSky.SetColor("_Horizon", Color.Lerp(horizon, new Color(.72f,.76f,.72f), clear));
                runtimeSky.SetColor("_Zenith", Color.Lerp(zenith, new Color(.42f,.57f,.65f), clear));
            }
        }

        void OnDisable()
        {
            RenderSettings.ambientProbe = previousProbe;
            if (captured)
            {
                RenderSettings.fogColor = fogColor; RenderSettings.fogDensity = fogDensity;
                if (sun) { sun.color = sunColor; sun.intensity = sunIntensity; sun.shadowStrength = shadowStrength; }
                if (runtimeSky && RenderSettings.skybox == runtimeSky) RenderSettings.skybox = originalSky;
                if (runtimeSky) Destroy(runtimeSky);
                runtimeSky = null; captured = false; lastClear = -1;
            }
        }
    }
}
