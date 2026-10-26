using UnityEngine;
namespace Paridhwani
{
    public sealed class RiverAmbience : MonoBehaviour
    {
        public TeaAmbience preferences;
        AudioSource source; AudioClip clip;
        void Start()
        {
            const int rate=22050; var samples=new float[rate*5]; var rng=new System.Random(53); float low=0;
            for(int i=0;i<samples.Length;i++)
            {
                float white=(float)(rng.NextDouble()*2-1); low=low*.92f+white*.08f;
                samples[i]=(low*.8f+white*.06f)*(.8f+.2f*Mathf.Sin(i*.00024f));
            }
            for(int i=0;i<600;i++) { float f=i/600f; samples[i]*=f; samples[samples.Length-1-i]*=f; }
            clip=AudioClip.Create("River weir current",samples.Length,1,rate,false); clip.SetData(samples,0);
            source=gameObject.AddComponent<AudioSource>(); source.clip=clip; source.loop=true;
            source.spatialBlend=1; source.minDistance=5; source.maxDistance=34; source.rolloffMode=AudioRolloffMode.Linear;
            source.Play();
        }
        void Update() { if(source) source.volume=preferences && preferences.muted?0:.45f; }
        void OnDestroy() { if(clip) Destroy(clip); }
    }
}
