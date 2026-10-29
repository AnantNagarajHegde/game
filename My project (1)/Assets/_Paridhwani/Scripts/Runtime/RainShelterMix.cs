using System;

namespace Paridhwani
{
    // Time-based mixing only; the Unity adapter owns the roof query and sources.
    public sealed class RainShelterMix
    {
        public bool Sheltered {get;private set;}
        public float RainGain {get;private set;}
        public float EaveGain {get;private set;}
        bool candidate;
        float candidateTime,cover,level;

        static float Approach(float value,float target,float step)
            =>value<target?Math.Min(value+step,target):Math.Max(value-step,target);
        public void Silence() {level=RainGain=EaveGain=0;}

        public void Step(bool roof,bool audible,float volume,float deltaTime)
        {
            float dt=float.IsFinite(deltaTime)?Math.Clamp(deltaTime,0,.1f):0;
            float gain=float.IsFinite(volume)?Math.Clamp(volume,0,1):0;
            if(roof==Sheltered){candidate=roof;candidateTime=0;}
            else
            {
                if(candidate!=roof){candidate=roof;candidateTime=0;}
                candidateTime+=dt;
                // Ignore short gaps under a tiled eave and very brief obstructions
                // while walking. Exit confirmation is deliberately more patient.
                if(candidateTime>=(roof?.12f:.30f)){Sheltered=roof;candidateTime=0;}
            }
            cover=Approach(cover,Sheltered?1:0,dt/(Sheltered?.6f:.9f));
            if(!audible || gain<=0){Silence();return;}
            // Resume and slider changes ramp; mute/focus loss always silence now.
            level=Approach(level,gain,dt*2);
            RainGain=(.34f+(.13f-.34f)*cover)*level;
            EaveGain=(.03f+(.16f-.03f)*cover)*level;
        }
    }
}
