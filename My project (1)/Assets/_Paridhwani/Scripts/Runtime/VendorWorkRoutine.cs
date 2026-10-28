using System;

namespace Paridhwani
{
    public enum VendorWorkRole { Tea, Market }
    public enum VendorWorkTask { Rest, Walk, CheckBoiler, ArrangeCups, CheckLedger, AdjustScales }

    // Small, transient work loops, not a clock, quest or saved NPC schedule.
    // Coordinates are world-axis offsets from the existing stall anchor.
    public sealed class VendorWorkRoutine
    {
        readonly struct Stop
        {
            public readonly float X,Z,Yaw,Minimum,Variation;
            public readonly VendorWorkTask Task;
            public Stop(float x,float z,float yaw,VendorWorkTask task,float minimum,float variation)
            {X=x;Z=z;Yaw=yaw;Task=task;Minimum=minimum;Variation=variation;}
        }
        readonly Stop[] stops;
        readonly Random random;
        int index;
        float remaining,actionTime;
        bool arrived;
        public const float Speed=.48f, MaximumStepSeconds=.1f, MaximumRadius=1.12f;
        public float X {get;private set;}
        public float Z {get;private set;}
        public float Facing => stops[index].Yaw;
        public float ActionTime => actionTime;
        public VendorWorkTask Task {get;private set;}=VendorWorkTask.Rest;
        public bool PresentationEnabled {get;set;}

        public VendorWorkRoutine(VendorWorkRole role,int seed)
        {
            random=new Random(seed);
            stops=role==VendorWorkRole.Tea ? new[] {
                // Keep the existing front approach inside its 3.35 m interaction range.
                new Stop(0,.352f,180,VendorWorkTask.Rest,2.5f,3),
                new Stop(-.35f,.352f,210,VendorWorkTask.CheckBoiler,3,2.5f),
                new Stop(.45f,.352f,175,VendorWorkTask.ArrangeCups,4,3),
                new Stop(0,.352f,165,VendorWorkTask.Rest,4,4)
            } : new[] {
                new Stop(.40f,-.25f,180,VendorWorkTask.CheckLedger,4.5f,3.5f),
                new Stop(0,0,215,VendorWorkTask.Rest,3,4),
                new Stop(.90f,-.25f,180,VendorWorkTask.AdjustScales,3,3),
                new Stop(0,0,200,VendorWorkTask.Rest,5,3)
            };
        }

        public void Reset(float x=0,float z=0)
        {
            X=x;Z=z;index=0;remaining=actionTime=0;arrived=false;Task=VendorWorkTask.Rest;
        }

        // Rejected movement consumes no work time. The Unity owner checks occupancy/support.
        public void Tick(float seconds,bool paused,Func<float,float,float,float,bool> allowMove)
        {
            if(paused || !float.IsFinite(seconds) || seconds<=0) return;
            float dt=Math.Min(seconds,MaximumStepSeconds);
            var stop=stops[index];
            if(!arrived)
            {
                float dx=stop.X-X,dz=stop.Z-Z;
                float distance=(float)Math.Sqrt(dx*dx+dz*dz);
                if(distance>.0001f)
                {
                    float fraction=Math.Min(1,Speed*dt/distance);
                    float nx=X+dx*fraction,nz=Z+dz*fraction;
                    if(allowMove==null || !allowMove(X,Z,nx,nz)) {Task=VendorWorkTask.Rest;return;}
                    X=nx;Z=nz;Task=VendorWorkTask.Walk;actionTime=0;
                    if(fraction<1) return;
                }
                arrived=true;remaining=stop.Minimum+(float)random.NextDouble()*stop.Variation;
                actionTime=0;
            }
            Task=stop.Task;actionTime+=dt;remaining-=dt;
            if(remaining<=0) {index=(index+1)%stops.Length;arrived=false;}
        }
    }
}
