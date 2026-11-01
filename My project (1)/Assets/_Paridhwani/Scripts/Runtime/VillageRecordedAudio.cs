using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani
{
    public static class RecordedLoopPolicy
    {
        public static float Envelope(double age, double length, double overlap)
        {
            if(!double.IsFinite(age) || length<=0 || overlap<=0 || age<0 || age>=length)return 0;
            return (float)(System.Math.Clamp(age/overlap,0,1)*System.Math.Clamp((length-age)/overlap,0,1));
        }
    }
    // Imported assets are owned by Unity; never destroy them as runtime-created clips.
    public static class VillageRecordedAudio
    {
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        public static AudioClip Clip(string name)
        {
            if (!cache.TryGetValue(name, out var clip))
            {
                clip = Resources.Load<AudioClip>("VillageAudio/" + name);
                cache[name] = clip;
                if (!clip) Debug.LogWarning("Missing recorded village audio: " + name);
            }
            return clip;
        }

        public static AudioSource Source(GameObject owner, bool spatial = false, float radius = 15)
        {
            var source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false; source.volume = 0; source.dopplerLevel = 0;
            source.spatialBlend = spatial ? 1 : 0;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f; source.maxDistance = radius;
            return source;
        }
    }

    // Schedule overlapping copies against the audio clock. No silent loop seam,
    // synthetic modulation, frame-count timing or pitch variation on ambience.
    public sealed class VillageRecordedLoop : MonoBehaviour
    {
        AudioSource[] sources;
        readonly double[] starts = { -1, -1 };
        AudioClip clip;
        double next;
        int slot;
        float overlap;
        public float Gain;
        public float Cutoff = 18000;
        AudioLowPassFilter filter;

        public void Configure(string asset, bool spatial = false, float radius = 15)
        {
            clip = VillageRecordedAudio.Clip(asset);
            sources = new[] { VillageRecordedAudio.Source(gameObject, spatial, radius), VillageRecordedAudio.Source(gameObject, spatial, radius) };
            filter = gameObject.AddComponent<AudioLowPassFilter>();
            foreach (var source in sources) source.clip = clip;
            overlap = clip ? Mathf.Min(1.5f, clip.length * .15f) : 0;
            next = AudioSettings.dspTime + .15;
        }

        void Update()
        {
            if (!clip || sources == null || clip.loadState!=AudioDataLoadState.Loaded) return;
            double now = AudioSettings.dspTime;
            if (next < now - .1) { foreach (var source in sources) source.Stop(); starts[0] = starts[1] = -1; next = now + .15; }
            if (next - now < .3)
            {
                starts[slot] = next;
                sources[slot].PlayScheduled(next);
                next += clip.length - overlap;
                slot = 1 - slot;
            }
            for (int i = 0; i < 2; i++)
            {
                double age = now - starts[i];
                float envelope = starts[i] < 0 ? 0 : RecordedLoopPolicy.Envelope(age,clip.length,overlap);
                sources[i].volume = Mathf.Clamp01(Gain) * envelope;
            }
            if (filter) filter.cutoffFrequency = Mathf.Clamp(Cutoff, 200, 22000);
        }
        void OnDisable()
        {
            Gain = 0;
            if (sources != null) foreach (var source in sources) if (source) source.Stop();
            starts[0] = starts[1] = -1; next = AudioSettings.dspTime + .15;
        }
    }
}
