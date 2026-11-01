using UnityEngine;

namespace Paridhwani
{
    public static class VillageMotion
    {
        public const float WalkStep=.78f, JogStep=1.04f;
        public static Vector3 Step(Vector3 velocity,Vector3 target,float seconds)
        {
            if(!float.IsFinite(seconds) || seconds<=0)return velocity;
            float rate=target.sqrMagnitude<.001f?15f:
                Vector3.Dot(velocity,target)<0?18f:target.magnitude<velocity.magnitude?13f:9.5f;
            return Vector3.MoveTowards(velocity,target,rate*seconds);
        }
        public static float StepLength(float speed)=>Mathf.Lerp(WalkStep,JogStep,Mathf.InverseLerp(2.15f,3.6f,speed));
    }
}
