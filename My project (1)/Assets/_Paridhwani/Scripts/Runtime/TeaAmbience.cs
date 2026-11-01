using UnityEngine;
namespace Paridhwani
{
    public static class VillageWeatherPolicy
    {
        // Ananya's first greeting explicitly refers to rain. It can clear after
        // the player has heard her explanation, with the road still damaged.
        public static bool Raining(TeaProgress progress) => progress == null || !progress.disappearanceExplained;
        public static float Advance(float current, bool raining, float deltaTime)
        {
            float dt = float.IsFinite(deltaTime) ? Mathf.Clamp(deltaTime, 0, .1f) : 0;
            return Mathf.MoveTowards(Mathf.Clamp01(current), raining ? 1 : 0, dt / 18f);
        }
    }

    public sealed class TeaAmbience : MonoBehaviour
    {
        public Transform listener;
        public ParticleSystem rain;
        public bool muted;
        VillageRecordedLoop rainSource, shelterSource;
        VillageFootsteps footsteps;
        public float volume=1,footstepVolume=1;
        readonly RainShelterMix mix=new RainShelterMix();
        TeaStallSession session;
        TeaProgress weatherProgress;
        VillageLighting lighting;
        float rainAmount=1, initialEmission;
        bool weatherInitialized;
        void Awake() { Shader.SetGlobalFloat("_VillageRainAmount",1); }
        void Start()
        {
            session=GetComponent<TeaStallSession>();
            lighting=Object.FindAnyObjectByType<VillageLighting>();
            if(rain) initialEmission=rain.emission.rateOverTime.constantMax;
            var outdoor=new GameObject("Recorded outdoor rain");outdoor.transform.SetParent(transform,false);
            var roof=new GameObject("Recorded roof runoff");roof.transform.SetParent(transform,false);
            rainSource=outdoor.AddComponent<VillageRecordedLoop>();rainSource.Configure("Rain");
            shelterSource=roof.AddComponent<VillageRecordedLoop>();shelterSource.Configure("RoofRunoff");
            footsteps=listener.gameObject.AddComponent<VillageFootsteps>();
            gameObject.AddComponent<VillageEnvironmentalAudio>().Bind(this,session);
        }
        void Update()
        {
            if(!rainSource || !shelterSource) return;
            UpdateWeather();
            if(!listener) {SilenceSources();return;}
            if(rain) {
                var target=listener.position+Vector3.up*7;
                if((rain.transform.position-target).sqrMagnitude>900) rain.Clear();
                rain.transform.position=target;
            }
            if(footsteps) footsteps.volume=footstepVolume;
            bool sheltered=Physics.Raycast(listener.position+Vector3.up*1.5f,Vector3.up,4,~(1<<2),QueryTriggerInteraction.Ignore);
            mix.Step(sheltered,!muted && Application.isFocused,PresentationPolicy.Volume(volume),Time.unscaledDeltaTime);
            float duck=session && session.ui && session.ui.Mode=="conversation"?.65f:1;
            rainSource.Gain=mix.RainGain*rainAmount*duck;
            shelterSource.Gain=mix.EaveGain*rainAmount*duck;
            rainSource.Cutoff=Mathf.Lerp(rainSource.Cutoff,mix.Sheltered?2800:18000,Time.unscaledDeltaTime*3);
        }
        void UpdateWeather()
        {
            var progress=session?session.Progress:null;
            if(progress==null)return;
            bool raining=VillageWeatherPolicy.Raining(progress);
            // New/loaded/practice visits start at their own story weather. Only
            // changes within the current visit animate as the storm passes.
            if(!weatherInitialized || !ReferenceEquals(progress,weatherProgress))
            {
                weatherInitialized=true; weatherProgress=progress; rainAmount=raining?1:0;
                if(rain && rainAmount==0)rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else rainAmount=VillageWeatherPolicy.Advance(rainAmount,raining,Time.unscaledDeltaTime);
            Shader.SetGlobalFloat("_VillageRainAmount",rainAmount);
            if(!lighting)lighting=Object.FindAnyObjectByType<VillageLighting>();
            if(lighting)lighting.SetClearWeather(1-rainAmount);
            if(rain)
            {
                var emission=rain.emission; emission.rateOverTime=initialEmission*rainAmount;
                if(rainAmount>0 && !rain.isEmitting)rain.Play();
                else if(rainAmount<=0 && rain.isEmitting)rain.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            }
        }
        void SilenceSources()
        {mix.Silence();if(rainSource)rainSource.Gain=0;if(shelterSource)shelterSource.Gain=0;}
        void OnDisable() {SilenceSources();Shader.SetGlobalFloat("_VillageRainAmount",1);}
        void OnDestroy() { if(footsteps) Destroy(footsteps); }
    }
}
