using UnityEngine;
namespace Paridhwani
{
    public sealed class TeaAmbience : MonoBehaviour
    {
        public Transform listener;
        public ParticleSystem rain;
        public bool muted;
        AudioSource rainSource, shelterSource, footsteps;
        VillageWalker walker; float nextStep; AudioClip stepClip;
        AudioClip noiseClip, dripClip;
        void Start()
        {
            rainSource=gameObject.AddComponent<AudioSource>();
            shelterSource=gameObject.AddComponent<AudioSource>();
            noiseClip=Noise("Monsoon rainfall",6,173,.95f);
            dripClip=Noise("Eaves and roof",8,94,.97f);
            rainSource.clip=noiseClip; shelterSource.clip=dripClip;
            walker=listener.GetComponent<VillageWalker>();
            footsteps=gameObject.AddComponent<AudioSource>();
            var samples=new float[3307]; var rng=new System.Random(18);
            for(int i=0;i<samples.Length;i++) samples[i]=(float)(rng.NextDouble()*2-1)*Mathf.Exp(-i/450f)*.3f;
            stepClip=AudioClip.Create("Soft wet footsteps",samples.Length,1,22050,false); stepClip.SetData(samples,0);
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
            if(!rainSource || !listener) return;
            if(rain) {
                var target=listener.position+Vector3.up*7;
                if((rain.transform.position-target).sqrMagnitude>900) rain.Clear();
                rain.transform.position=target;
            }
            if(walker && walker.Speed>.15f && !walker.blocked && Time.time>nextStep)
            {
                bool stone=listener.position.x<-.6f; footsteps.pitch=stone?1.25f:.72f;
                if(!muted) footsteps.PlayOneShot(stepClip,stone?.3f:.22f);
                nextStep=Time.time+(walker.Speed>3?.31f:.46f);
            }
            bool sheltered=Physics.Raycast(listener.position+Vector3.up*1.5f,Vector3.up,4,~(1<<2));
            rainSource.volume=Mathf.MoveTowards(rainSource.volume,muted?0:(sheltered?.13f:.34f),Time.deltaTime*.2f);
            shelterSource.volume=Mathf.MoveTowards(shelterSource.volume,muted?0:(sheltered?.16f:.03f),Time.deltaTime*.2f);
        }
        void OnDestroy() { if(noiseClip) Destroy(noiseClip); if(dripClip) Destroy(dripClip); if(stepClip) Destroy(stepClip); }
    }
}
