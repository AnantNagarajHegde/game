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
        public const int Variants = 5;
        public static int SelectVariant(int previous, int draw)
        {
            int candidate = Math.Abs(draw % (Variants - 1));
            return candidate >= previous && previous >= 0 ? candidate + 1 : candidate;
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
        public event Action<bool> Stepped;
        readonly FootstepStride stride=new FootstepStride();
        readonly AudioClip[,] clips=new AudioClip[3,FootstepPolicy.Variants];
        VillageWalker walker; CharacterController controller; AudioSource source;
        VillageCharacter character;
        readonly System.Random random=new System.Random();
        readonly int[] previous={-1,-1,-1};
        void Awake()
        {
            walker=GetComponent<VillageWalker>();controller=GetComponent<CharacterController>();
            character=GetComponentInChildren<VillageCharacter>();
            source=gameObject.AddComponent<AudioSource>();source.playOnAwake=false;source.spatialBlend=0;
            for(int surface=0;surface<3;surface++) for(int variant=0;variant<FootstepPolicy.Variants;variant++)
                clips[surface,variant]=VillageRecordedAudio.Clip(((FootstepSurface)surface).ToString()+variant);
        }

        void LateUpdate()
        {
            float gain=PresentationPolicy.Volume(volume);
            bool eligible=Application.isFocused && !walker.blocked && walker.enabled &&
                controller.enabled && controller.isGrounded && walker.Speed>.15f;
            if(!eligible) {source.Stop();stride.Travel(0,false,false);return;}
            source.volume=gain*.65f;
            if(gain<=0)source.Stop();
            bool footfall=character && character.Gait!=null?character.Gait.Footfall:stride.Travel(walker.Speed*Time.deltaTime,true,walker.Speed>3);
            if(!footfall)return;
            Stepped?.Invoke(walker.Speed>3);
            if(gain<=0)return;
            FootstepSurface surface=FootstepSurface.Earth;
            if(Physics.Raycast(transform.position+Vector3.up*.3f,Vector3.down,out var hit,.9f,~(1<<2),QueryTriggerInteraction.Ignore))
            {
                var authored=hit.collider.GetComponentInParent<VillageFootstepSurface>();
                if(authored) surface=authored.kind;
            }
            int index=(int)surface;if(index<0 || index>=3) index=0;
            int choice=FootstepPolicy.SelectVariant(previous[index],random.Next());previous[index]=choice;
            source.pitch=.98f+(float)random.NextDouble()*.04f;
            if(clips[index,choice])source.PlayOneShot(clips[index,choice]);
        }
        void OnDestroy()
        {
            if(source) {source.Stop();Destroy(source);}
            // Resource clips are shared, imported assets.
        }
    }
}
