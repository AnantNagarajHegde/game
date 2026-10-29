using UnityEngine;
namespace Paridhwani
{
    public sealed class TeaAmbience : MonoBehaviour
    {
        public Transform listener;
        public ParticleSystem rain;
        public bool muted;
        AudioSource rainSource, shelterSource;
        VillageFootsteps footsteps;
        public float volume=1,footstepVolume=1;
        AudioClip noiseClip, dripClip;
        readonly RainShelterMix mix=new RainShelterMix();
        void Start()
        {
            rainSource=gameObject.AddComponent<AudioSource>();
            shelterSource=gameObject.AddComponent<AudioSource>();
            noiseClip=Noise("Monsoon rainfall",6,173,.95f);
            dripClip=Noise("Eaves and roof",8,94,.97f);
            rainSource.clip=noiseClip; shelterSource.clip=dripClip;
            footsteps=listener.gameObject.AddComponent<VillageFootsteps>();
            rainSource.playOnAwake=shelterSource.playOnAwake=false;
            rainSource.volume=shelterSource.volume=0;
            rainSource.loop=shelterSource.loop=true;
            rainSource.Play(); shelterSource.Play();
        }
        static AudioClip Noise(string name,int seconds,int seed,float filter)
        {
            const int rate=22050;
            float[] data=new float[rate*seconds];
            var random=new System.Random(seed); float low=0;
            for(int i=0;i<data.Length;i++)
            {
                float white=(float)(random.NextDouble()*2-1);
                low=filter*low+(1-filter)*white;
                float pulse=.7f+.3f*Mathf.Sin(i*.0009f);
                data[i]=(white*.10f+low*.8f)*pulse;
            }
            // Fade through the loop seam to avoid a click.
            for(int i=0;i<512;i++) { float t=i/512f; data[i]*=t; data[data.Length-1-i]*=t; }
            AudioClip clip=AudioClip.Create(name,data.Length,1,rate,false); clip.SetData(data,0); return clip;
        }
        void Update()
        {
            if(!rainSource || !shelterSource) return;
            if(!listener) {SilenceSources();return;}
            if(rain) {
                var target=listener.position+Vector3.up*7;
                if((rain.transform.position-target).sqrMagnitude>900) rain.Clear();
                rain.transform.position=target;
            }
            if(footsteps) footsteps.volume=footstepVolume;
            bool sheltered=Physics.Raycast(listener.position+Vector3.up*1.5f,Vector3.up,4,~(1<<2),QueryTriggerInteraction.Ignore);
            mix.Step(sheltered,!muted && Application.isFocused,PresentationPolicy.Volume(volume),Time.unscaledDeltaTime);
            rainSource.volume=mix.RainGain;shelterSource.volume=mix.EaveGain;
        }
        void SilenceSources()
        {mix.Silence();if(rainSource)rainSource.volume=0;if(shelterSource)shelterSource.volume=0;}
        void OnDisable() {SilenceSources();}
        void OnDestroy() { if(noiseClip) Destroy(noiseClip); if(dripClip) Destroy(dripClip); if(footsteps) Destroy(footsteps); }
    }
}
