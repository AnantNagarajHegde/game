using UnityEngine;
namespace Paridhwani
{
    public sealed class VillageCharacter : MonoBehaviour
    {
        public Transform leftArm,rightArm,leftLeg,rightLeg,head;
        public VillageWalker walker;
        public Transform lookTarget;
        float phase;
        void Update()
        {
            float speed=walker?walker.Speed:0;
            phase+=Time.deltaTime*speed*3;
            float swing=Mathf.Sin(phase)*Mathf.Min(speed*13,32);
            if(leftArm) leftArm.localRotation=Quaternion.Euler(swing,0,8);
            if(rightArm) rightArm.localRotation=Quaternion.Euler(-swing,0,-8);
            if(leftLeg) leftLeg.localRotation=Quaternion.Euler(-swing,0,0);
            if(rightLeg) rightLeg.localRotation=Quaternion.Euler(swing,0,0);
            if(head && lookTarget)
            {
                Vector3 d=lookTarget.position-head.position;
                Vector3 local=head.parent.InverseTransformDirection(d);
                float angle=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-35,35);
                head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(0,angle,0),Time.deltaTime*2);
            }
        }
    }
}
