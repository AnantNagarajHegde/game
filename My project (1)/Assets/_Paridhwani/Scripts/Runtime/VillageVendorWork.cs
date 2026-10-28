using UnityEngine;

namespace Paridhwani
{
    // Runtime-only binding: works with the current generated scene without changing its recipe.
    // Story staging and all modal screens take precedence over ambient work.
    sealed class VillageVendorWork
    {
        readonly VillageNpc npc;
        readonly VillageCharacter character;
        readonly CapsuleCollider body;
        readonly Vector3 origin;
        readonly Collider[] overlaps=new Collider[24];
        readonly VendorWorkRoutine routine;
        readonly System.Func<float,float,float,float,bool> allowMovement;
        Vector3 lastPosition;
        Transform player;
        public bool AtWorkplace => routine.PresentationEnabled;

        public VillageVendorWork(VillageNpc npc,VillageCharacter character)
        {
            this.npc=npc;this.character=character;origin=lastPosition=npc.transform.position;
            body=npc.GetComponent<CapsuleCollider>();
            allowMovement=AllowMove;
            routine=new VendorWorkRoutine(npc.role==VillageNpcRole.TeaVendor?VendorWorkRole.Tea:VendorWorkRole.Market,
                npc.role==VillageNpcRole.TeaVendor?173:941);
            character.workRoutine=routine;
        }

        public void Update(float seconds,Transform player,bool paused,bool staged)
        {
            this.player=player;
            var root=npc.transform;
            bool displaced=(root.position-origin).sqrMagnitude>(VendorWorkRoutine.MaximumRadius+.1f)*(VendorWorkRoutine.MaximumRadius+.1f);
            routine.PresentationEnabled=!staged && !displaced && body && body.enabled && root.gameObject.activeInHierarchy;
            if(!routine.PresentationEnabled) {lastPosition=root.position;return;}
            // Save/practice restoration can return the actors to their authored origins.
            if((root.position-lastPosition).sqrMagnitude>.0001f)
                routine.Reset(root.position.x-origin.x,root.position.z-origin.z);
            bool hold=paused || !Application.isFocused || !player || Vector3.Distance(player.position,root.position)<1.35f;
            routine.Tick(seconds,hold,allowMovement);
            var target=origin+new Vector3(routine.X,0,routine.Z);
            if(!hold)
            {
                var delta=target-root.position;
                float yaw=delta.sqrMagnitude>.000001f?Mathf.Atan2(delta.x,delta.z)*Mathf.Rad2Deg:routine.Facing;
                root.rotation=Quaternion.RotateTowards(root.rotation,Quaternion.Euler(0,yaw,0),100*Mathf.Min(seconds,VendorWorkRoutine.MaximumStepSeconds));
                root.position=target;
            }
            lastPosition=root.position;
            character.presentationPaused|=(!Application.isFocused || !player) && !character.talking;
        }

        bool AllowMove(float oldX,float oldZ,float newX,float newZ)
        {
            var root=npc.transform;
            Vector3 next=origin+new Vector3(newX,0,newZ);
            Vector3 old=root.position;
            // Leave room for approach, including the player on Ignore Raycast layer.
            if(player && Mathf.Abs(player.position.y-next.y)<2 &&
                (player.position-next).sqrMagnitude<.9f*.9f &&
                (player.position-next).sqrMagnitude<(player.position-old).sqrMagnitude) return false;
            // Require the same flat supported work floor; never route across a drop/step.
            if(!Physics.Raycast(next+Vector3.up*.35f,Vector3.down,out var ground,.7f,~(1<<2),QueryTriggerInteraction.Ignore) ||
                ground.point.y>origin.y+.08f || ground.point.y<origin.y-.18f || ground.normal.y<.85f) return false;
            float radius=body.radius-.015f;
            Vector3 center=next+body.center;
            float half=Mathf.Max(0,body.height*.5f-body.radius);
            int count=Physics.OverlapCapsuleNonAlloc(center+Vector3.up*half,center-Vector3.up*half,radius,overlaps,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length) return false;
            for(int i=0;i<count;i++)
            {
                var obstacle=overlaps[i];
                if(!obstacle || obstacle.transform.IsChildOf(root)) continue;
                if(!Physics.ComputePenetration(body,next,root.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float depth)) continue;
                // The old tea anchor slightly intersects its counter. Only allow movement
                // that reduces existing penetration, so it can step back into its work lane.
                if(!Physics.ComputePenetration(body,old,root.rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float oldDepth) || depth>=oldDepth-.0001f) return false;
            }
            return true;
        }
    }

    public sealed partial class TeaStallSession
    {
        float NpcInteractionDistance(VillageNpc npc)
        {
            float distance=Vector3.Distance(player.transform.position,npc.transform.position);
            // Keep a service counter reachable while its vendor moves behind it.
            // Finales/external staging continue to use the actor's actual position.
            if(vendorWork.TryGetValue(npc,out var work) && work.AtWorkplace && actorOrigins.TryGetValue(npc.transform,out var anchor))
                distance=Mathf.Min(distance,Vector3.Distance(player.transform.position,anchor));
            return distance;
        }
    }
}
