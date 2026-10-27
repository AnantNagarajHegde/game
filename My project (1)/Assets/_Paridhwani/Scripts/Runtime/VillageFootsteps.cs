using System;
using UnityEngine;

namespace Paridhwani
{
    public enum FootstepSurface { Earth,Stone,Timber }

    public static class FootstepPolicy
    {
        public static bool TryMaterial(string material,out FootstepSurface surface)
        {
            surface=FootstepSurface.Earth;
            switch(material)
            {
                case "Earth": case "Lane": return true;
                case "Stone": case "StoneDark": surface=FootstepSurface.Stone; return true;
                case "Wood": case "WoodLight": surface=FootstepSurface.Timber; return true;
                default: return false;
            }
        }
        // Original synthesized placeholders. Production recordings still need listening review.
        public static float[] Samples(FootstepSurface surface,int variant,int rate=22050)
        {
            if(rate<8000 || rate>96000) throw new ArgumentOutOfRangeException(nameof(rate));
            double duration=surface==FootstepSurface.Stone?.13:surface==FootstepSurface.Timber?.21:.18;
            var data=new float[(int)(rate*duration)];
            var random=new System.Random(1709+(int)surface*103+variant*17); double low=0;
            for(int i=0;i<data.Length;i++)
            {
                double t=(double)i/rate,white=random.NextDouble()*2-1;
                low=.78*low+.22*white;
                double attack=Math.Min(1,t/.004),tail=Math.Min(1,(data.Length-1-i)/(rate*.008));
                double envelope=attack*tail*Math.Exp(-t*(surface==FootstepSurface.Stone?38:23));
                double sound=surface==FootstepSurface.Stone ? white*.36+Math.Sin(t*2*Math.PI*(1400+variant*73))*.12 :
                    surface==FootstepSurface.Timber ? low*.3+Math.Sin(t*2*Math.PI*(155+variant*13))*.25 : low*.7+white*.08;
                data[i]=(float)(sound*envelope);
            }
            return data;
        }
    }
    public sealed class FootstepStride
    {
        double distance;
        public bool Travel(double metres,bool eligible,bool jogging)
        {
            if(!eligible || !double.IsFinite(metres) || metres<0 || metres>.8) {distance=0;return false;}
            distance+=metres;
            double stride=jogging?1.1:.95;
            if(distance<stride) return false;
            distance-=stride; return true;
        }
    }
    [RequireComponent(typeof(VillageWalker))]
    public sealed class VillageFootsteps : MonoBehaviour
    {
        public float volume=1;
        readonly FootstepStride stride=new FootstepStride();
        readonly AudioClip[,] clips=new AudioClip[3,3];
        VillageWalker walker; CharacterController controller; AudioSource source; int sequence;
        void Awake()
        {
            walker=GetComponent<VillageWalker>();controller=GetComponent<CharacterController>();
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            for(int surface=0;surface<3;surface++) for(int variant=0;variant<3;variant++)
            {
                var data=FootstepPolicy.Samples((FootstepSurface)surface,variant);
                var clip=AudioClip.Create(((FootstepSurface)surface)+" step "+variant,data.Length,1,22050,false);
                clip.SetData(data,0);clips[surface,variant]=clip;
            }
        }
        void LateUpdate()
        {
            float gain=PresentationPolicy.Volume(volume);
            bool eligible=gain>0 && Application.isFocused && !walker.blocked && walker.enabled &&
                controller.enabled && controller.isGrounded && walker.Speed>.15f;
            if(!eligible) {source.Stop();stride.Travel(0,false,false);return;}
            source.volume=gain*.55f;
            if(!stride.Travel(walker.Speed*Time.deltaTime,true,walker.Speed>3)) return;
            FootstepSurface surface=FootstepSurface.Earth;
            if(Physics.Raycast(transform.position+Vector3.up*.3f,Vector3.down,out var hit,.9f,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                var authored=hit.collider.GetComponentInParent<VillageFootstepSurface>();
                if(authored) surface=authored.kind;
            }
            int index=(int)surface;if(index<0 || index>=3) index=0;
            source.pitch=.96f+(sequence%3)*.04f;
            source.PlayOneShot(clips[index,sequence++%3]);
        }
        void OnDestroy()
        {
            if(source) {source.Stop();Destroy(source);}
            foreach(var clip in clips) if(clip) Destroy(clip);
        }
    }
}
