using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Paridhwani
{
    // Retarget the authored character in memory. Source meshes and the saved scene
    // stay reusable; each knee has blended skin weights instead of a visible cut.
    public sealed class VillageCharacterGait
    {
        sealed class Limb
        {
            public Transform hip,knee,shoe,elbow;
            public Quaternion shoeRest;
        }
        readonly VillageCharacter character;
        readonly VillageWalker walker;
        readonly VillageAmbientResident resident;
        float Speed => walker ? walker.Speed : resident.Speed;
        float Distance => walker ? walker.TravelDistance : resident.TravelDistance;
        Vector3 Velocity => walker ? walker.Velocity : resident.Velocity;
        bool Grounded => walker ? walker.Grounded : resident.Grounded;
        bool Blocked => walker ? walker.blocked : resident.Stopped;
        bool ReducedMotion => walker ? walker.reducedMotion : resident.ReducedMotion;
        Transform Root => walker ? walker.transform : resident.transform;
        readonly Limb left,right;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly Vector3 rest;
        Vector3 previousVelocity;
        float cycle,blend,lean,bank,lastDistance,pelvis;
        public float Cycle=>cycle;
        public float Blend=>blend;
        public bool Footfall {get;private set;}

        public VillageCharacterGait(VillageCharacter value,VillageAmbientResident ambient=null)
        {
            character=value;walker=value.walker;resident=ambient;rest=value.transform.localPosition;
            left=Rig(value.leftLeg,value.leftArm);right=Rig(value.rightLeg,value.rightArm);
            if(walker)walker.RefreshBodyRenderers();lastDistance=Distance;
        }
        Limb Rig(Transform hip,Transform arm)
        {
            var limb=new Limb{hip=hip};
            if(!hip || !arm)return limb;
            limb.knee=new GameObject("Articulated knee").transform;
            limb.knee.SetParent(hip,false);limb.knee.localPosition=new Vector3(0,-.37f,0);
            MeshFilter trouser=null;
            foreach(var filter in hip.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.name.EndsWith("Trouser"))trouser=filter;
                if(filter.name.EndsWith("Shoe"))limb.shoe=filter.transform;
            }
            if(trouser)
            {
                var source=trouser.GetComponent<MeshRenderer>();
                var mesh=Object.Instantiate(trouser.sharedMesh);mesh.name=trouser.sharedMesh.name+" articulated";
                meshes.Add(mesh);var vertices=mesh.vertices;var weights=new BoneWeight[vertices.Length];
                for(int i=0;i<vertices.Length;i++)
                {
                    float kneeWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.46f,-.28f,vertices[i].y));
                    weights[i]=new BoneWeight{boneIndex0=0,weight0=1-kneeWeight,boneIndex1=1,weight1=kneeWeight};
                }
                mesh.boneWeights=weights;
                mesh.bindposes=new[]{hip.worldToLocalMatrix*trouser.transform.localToWorldMatrix,limb.knee.worldToLocalMatrix*trouser.transform.localToWorldMatrix};
                var skinObject=new GameObject("Articulated trousers",typeof(SkinnedMeshRenderer));
                skinObject.layer=hip.gameObject.layer;skinObject.transform.SetParent(trouser.transform,false);
                var skin=skinObject.GetComponent<SkinnedMeshRenderer>();skin.sharedMesh=mesh;skin.sharedMaterials=source.sharedMaterials;
                skin.bones=new[]{hip,limb.knee};skin.rootBone=hip;skin.quality=SkinQuality.Bone2;
                skin.localBounds=new Bounds(new Vector3(0,-.35f,0),new Vector3(.6f,1.3f,1.3f));
                skin.shadowCastingMode=source.shadowCastingMode;source.enabled=false;
            }
            if(limb.shoe)
            {
                var shoeMesh=limb.shoe;
                limb.shoe=new GameObject("Articulated ankle").transform;limb.shoe.SetParent(limb.knee,false);
                limb.shoe.localPosition=new Vector3(0,-.35f,0);shoeMesh.SetParent(limb.shoe,true);
                limb.shoeRest=limb.shoe.localRotation;
            }
            limb.elbow=new GameObject("Articulated elbow").transform;limb.elbow.SetParent(arm,false);
            limb.elbow.localPosition=new Vector3(0,-.235f,0);
            var children=new List<Transform>();foreach(Transform child in arm)children.Add(child);
            foreach(var child in children)if(child.name.EndsWith("Forearm") || child.name.EndsWith("Hand"))child.SetParent(limb.elbow,true);
            return limb;
        }
        public void Update(float seconds)
        {
            Footfall=false;
            float dt=Mathf.Min(seconds,.05f);if(dt<=0)return;
            float travel=Distance-lastDistance;lastDistance=Distance;
            float previousCycle=cycle;
            bool teleported=travel<0 || travel>.5f;
            bool moving=!Blocked && Grounded && Speed>.04f;
            if(teleported){cycle=0;blend=0;pelvis=lean=bank=0;previousVelocity=Vector3.zero;}
            else if(moving)cycle=Mathf.Repeat(cycle+travel/(VillageMotion.StepLength(Speed)*2),1);
            if(moving && !teleported)Footfall=(previousCycle<.5f && cycle>=.5f) || cycle<previousCycle;
            blend=Mathf.MoveTowards(blend,moving?Mathf.Clamp01(Speed/.65f):0,dt*7);
            float jog=Mathf.InverseLerp(2.15f,3.6f,Speed);
            var localVelocity=character.transform.InverseTransformDirection(Velocity);
            var acceleration=character.transform.InverseTransformDirection((Velocity-previousVelocity)/dt);
            previousVelocity=Velocity;
            lean=Mathf.Lerp(lean,Mathf.Clamp(localVelocity.z*1.2f+acceleration.z*.16f,-3,7)*blend,1-Mathf.Exp(-9*dt));
            bank=Mathf.Lerp(bank,Mathf.Clamp(-acceleration.x*.45f,-5,5)*blend,1-Mathf.Exp(-8*dt));
            float breath=ReducedMotion?0:Mathf.Sin(Time.time*1.7f)*.002f*(1-blend);
            FootPath(cycle,jog,out float leftZ,out _,out bool leftStance);
            FootPath(Mathf.Repeat(cycle+.5f,1),jog,out float rightZ,out _,out bool rightStance);
            float support=Mathf.Max(leftStance?Mathf.Abs(leftZ):0,rightStance?Mathf.Abs(rightZ):0);
            // Rise over the supporting leg, lower at heel strike. The stance foot
            // travels backward at ground speed instead of skating under the body.
            float height=Mathf.Sqrt(Mathf.Max(.25f,.716f*.716f-support*support))-.715f;
            if(!leftStance && !rightStance)height=.012f*jog;
            pelvis=Mathf.Lerp(pelvis,Mathf.Clamp(height,-.14f,.015f)*blend,1-Mathf.Exp(-24*dt));
            character.transform.localPosition=rest+Vector3.up*(pelvis+breath);
            // Keep player heading under the motor's control. Only small body lean is added.
            character.transform.rotation=Quaternion.Euler(0,character.transform.eulerAngles.y,0)*Quaternion.Euler(lean,0,bank);
            Pose(left,cycle,jog,dt);Pose(right,Mathf.Repeat(cycle+.5f,1),jog,dt);
            float swing=(leftZ-rightZ)*Mathf.Lerp(24f,34f,jog);
            character.leftArm.localRotation=Quaternion.Euler(swing-3*blend,0,5);
            character.rightArm.localRotation=Quaternion.Euler(-swing-3*blend,0,-5);
            if(left.elbow)left.elbow.localRotation=Quaternion.Euler(-(8+jog*27+Mathf.Max(0,-swing)*.35f)*blend,0,0);
            if(right.elbow)right.elbow.localRotation=Quaternion.Euler(-(8+jog*27+Mathf.Max(0,swing)*.35f)*blend,0,0);
            if(character.head)character.head.localRotation=Quaternion.Euler(-lean*.65f,0,-bank*.5f);
        }
        void Pose(Limb limb,float phase,float jog,float dt)
        {
            if(!limb.hip || !limb.knee)return;
            FootPath(phase,jog,out float z,out float lift,out _);
            var desired=character.transform.TransformPoint(new Vector3(limb.hip.localPosition.x,.115f+lift,z));
            desired.y=Root.position.y+rest.y+.115f+lift;
            // Modest terrain adaptation: don't reach through steps or pull a leg into a wall.
            if(Physics.Raycast(desired+Vector3.up*.4f,Vector3.down,out var hit,.8f,~(1<<2),QueryTriggerInteraction.Ignore) && hit.normal.y>.65f)
                desired.y=Mathf.Max(desired.y,Mathf.Min(desired.y+.12f,hit.point.y+.115f+lift));
            var delta=character.transform.InverseTransformDirection(desired-limb.hip.position);
            const float upper=.37f,lower=.35f;
            float length=Mathf.Clamp(new Vector2(delta.y,delta.z).magnitude,.2f,upper+lower-.002f);
            float bend=Mathf.Acos(Mathf.Clamp((upper*upper+lower*lower-length*length)/(2*upper*lower),-1,1))*Mathf.Rad2Deg;
            float knee=180-bend;
            float offset=Mathf.Acos(Mathf.Clamp((upper*upper+length*length-lower*lower)/(2*upper*length),-1,1))*Mathf.Rad2Deg;
            float hip=-Mathf.Atan2(delta.z,-delta.y)*Mathf.Rad2Deg-offset;
            var hipPose=Quaternion.Euler(hip*blend,0,0);var kneePose=Quaternion.Euler(knee*blend,0,0);
            limb.hip.localRotation=Quaternion.Slerp(limb.hip.localRotation,hipPose,1-Mathf.Exp(-24*dt));
            limb.knee.localRotation=Quaternion.Slerp(limb.knee.localRotation,kneePose,1-Mathf.Exp(-24*dt));
            if(limb.shoe)limb.shoe.localRotation=Quaternion.Euler(-(hip+knee)*blend,0,0)*limb.shoeRest;
        }
        void FootPath(float phase,float jog,out float z,out float lift,out bool stance)
        {
            float contact=Mathf.Lerp(.5f,.36f,jog),swingT=Mathf.Clamp01((phase-contact)/(1-contact));
            float reach=VillageMotion.StepLength(Speed)*contact*blend;
            stance=phase<contact;
            z=stance?Mathf.Lerp(reach,-reach,phase/contact):Mathf.Lerp(-reach,reach,Mathf.SmoothStep(0,1,swingT));
            lift=Mathf.Sin(swingT*Mathf.PI)*(.10f+jog*.055f)*blend;
        }
        public void Dispose(){foreach(var mesh in meshes)if(mesh)Object.Destroy(mesh);meshes.Clear();}
    }
}
