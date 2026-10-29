using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace Paridhwani
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class VillageWalker : MonoBehaviour
    {
        public InputActionAsset actions;
        public Transform visual;
        public Camera view;
        public bool blocked,reducedMotion,invertY;
        float TransitionDuration => reducedMotion?.15f:Mathf.Max(.15f,conversationTransition);
        public float sensitivity=.12f;
        [Range(.15f,1.5f)] public float conversationTransition=.65f;
        public float Heading => yaw;
        public float Speed { get; private set; }
        public float ConversationBlend { get; private set; }
        public bool IsTransitioning => elapsed<TransitionDuration;
        public bool IsInspecting => inspectionPose;
        public Vector3 EyePosition => transform.position+Vector3.up*1.62f;
        InputActionAsset ownedActions;
        InputAction move,look,sprint;
        CharacterController controller;
        Transform focus,inspectionPose,inspectionTarget;
        Renderer[] bodyRenderers;
        ShadowCastingMode[] bodyShadows;
        float yaw=-18,pitch=17,gravity,suppressLookUntil,elapsed=10,transitionFov=53;
        bool hidden,returningFromInspection;
        Vector3 transitionPosition,cameraVelocity;
        Quaternion transitionRotation;
        void CaptureTransition()
        {
            transitionPosition=view.transform.position; transitionRotation=view.transform.rotation; transitionFov=view.fieldOfView;
            elapsed=0; cameraVelocity=Vector3.zero; SuppressLook();
        }
        public void BeginConversation(Transform eyeAnchor)
        {
            CaptureTransition(); focus=eyeAnchor; inspectionPose=null; inspectionTarget=null;
            returningFromInspection=false;
        }
        public void BeginInspection(Transform pose,Transform target)
        {
            CaptureTransition(); focus=null; inspectionPose=pose; inspectionTarget=target;
            returningFromInspection=true;
        }
        public void EndConversation()
        {
            if(!focus && !inspectionPose) return;
            CaptureTransition(); focus=null; inspectionPose=null; inspectionTarget=null;
        }
        public void SuppressLook() { suppressLookUntil=Time.unscaledTime+.2f; }
        public void SetLookAngles(float heading,float elevation)
        { yaw=heading; pitch=Mathf.Clamp(elevation,-50,65); cameraVelocity=Vector3.zero; }
        void Awake()
        {
            controller=GetComponent<CharacterController>();
            ownedActions=Instantiate(actions);
            move=ownedActions.FindAction("Player/Move",true); look=ownedActions.FindAction("Player/Look",true);
            sprint=ownedActions.FindAction("Player/Sprint",true); ownedActions.FindActionMap("Player").Enable();
            bodyRenderers=visual.GetComponentsInChildren<Renderer>(); bodyShadows=new ShadowCastingMode[bodyRenderers.Length];
            for(int i=0;i<bodyRenderers.Length;i++) bodyShadows[i]=bodyRenderers[i].shadowCastingMode;
        }
        void OnDestroy()
        { ShowBody(true); if(ownedActions) { ownedActions.Disable(); Destroy(ownedActions); } }
        void OnApplicationFocus(bool focused) { SuppressLook(); }
        public void Restore(Vector3 position,float heading)
        {
            controller.enabled=false; transform.position=position; controller.enabled=true;
            yaw=Mathf.Repeat(heading+180,360)-180; gravity=0; Speed=0; cameraVelocity=Vector3.zero;
            focus=null; inspectionPose=null; inspectionTarget=null; elapsed=TransitionDuration;
            returningFromInspection=false; ConversationBlend=0; ShowBody(true); SuppressLook(); UpdateView(true);
        }
        void Update()
        {
            bool canMove=!blocked && !IsTransitioning && Application.isFocused;
            Vector2 input=canMove?VillageInputPolicy.WalkInput(move.ReadValue<Vector2>()):Vector2.zero;
            if(canMove && Time.unscaledTime>suppressLookUntil && Cursor.lockState==CursorLockMode.Locked)
            {
                Vector2 delta=look.activeControl?.device is Gamepad
                    ? VillageInputPolicy.ControllerLookDelta(look.ReadValue<Vector2>(),Time.deltaTime,sensitivity)
                    : Vector2.ClampMagnitude(look.ReadValue<Vector2>(),200)*sensitivity;
                yaw+=delta.x; pitch=Mathf.Clamp(pitch-delta.y*(invertY?-1:1),-50,65);
            }
            Vector3 direction=Quaternion.Euler(0,yaw,0)*new Vector3(input.x,0,input.y);
            float speed=sprint.IsPressed()?3.6f:2.15f;
            if(controller.isGrounded && gravity<0) gravity=-2;
            gravity=Mathf.Max(gravity+Physics.gravity.y*Time.deltaTime,-25);
            Vector3 before=transform.position;
            controller.Move((direction*speed+Vector3.up*gravity)*Time.deltaTime);
            Vector3 travelled=transform.position-before; travelled.y=0;
            Speed=Time.deltaTime>0 ? Mathf.Min(travelled.magnitude/Time.deltaTime,direction.magnitude*speed) : 0;
            if(direction.sqrMagnitude>.001f)
                visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(direction),12*Time.deltaTime);
        }
        void LateUpdate() { UpdateView(false); }
        void UpdateView(bool snap)
        {
            if(!view) return;
            Vector3 pivot=transform.position+Vector3.up*1.48f;
            Quaternion orbit=Quaternion.Euler(pitch,yaw,0);
            Vector3 offset=orbit*new Vector3(.45f,.15f,-3.5f);
            float distance=offset.magnitude;
            if(Physics.SphereCast(pivot,.17f,offset.normalized,out RaycastHit hit,distance,~(1<<2),QueryTriggerInteraction.Ignore))
                distance=Mathf.Max(.25f,hit.distance-.12f);
            Vector3 desired=pivot+offset.normalized*distance;
            Quaternion rotation=Quaternion.LookRotation(pivot+orbit*Vector3.right*.35f-desired);
            bool active=focus || inspectionPose;
            if(inspectionPose && inspectionTarget)
            { desired=inspectionPose.position; rotation=Quaternion.LookRotation(inspectionTarget.position-desired); }
            else if(focus)
            { desired=EyePosition; rotation=Quaternion.LookRotation(focus.position-Vector3.up*.55f-desired); }
            elapsed=Mathf.Min(TransitionDuration,elapsed+Time.unscaledDeltaTime);
            float t=Mathf.SmoothStep(0,1,elapsed/TransitionDuration);
            ConversationBlend=active?t:1-t;
            if(!snap && IsTransitioning)
            { desired=Vector3.Lerp(transitionPosition,desired,t); rotation=Quaternion.Slerp(transitionRotation,rotation,t); }
            Vector3 sweep=desired-pivot;
            if(sweep.magnitude>.2f && Physics.SphereCast(pivot,.12f,sweep.normalized,out hit,sweep.magnitude,~(1<<2),QueryTriggerInteraction.Ignore))
                desired=pivot+sweep.normalized*Mathf.Max(.1f,hit.distance-.05f);
            if(snap || active || IsTransitioning || reducedMotion) view.transform.position=desired;
            else view.transform.position=Vector3.SmoothDamp(view.transform.position,desired,ref cameraVelocity,.06f);
            view.transform.rotation=rotation;
            float targetFov=reducedMotion?53:inspectionPose?48:focus?55:53;
            view.fieldOfView=IsTransitioning?Mathf.Lerp(transitionFov,targetFov,t):targetFov;
            bool inspectionHidesBody=inspectionPose || (returningFromInspection && IsTransitioning);
            ShowBody(!inspectionHidesBody && Vector3.Distance(view.transform.position,EyePosition)>.8f);
            if(!active && !IsTransitioning) returningFromInspection=false;
        }
        void ShowBody(bool visible)
        {
            if(bodyRenderers==null || hidden==!visible) return; hidden=!visible;
            for(int i=0;i<bodyRenderers.Length;i++) if(bodyRenderers[i])
                bodyRenderers[i].shadowCastingMode=visible?bodyShadows[i]:ShadowCastingMode.ShadowsOnly;
        }
    }
}
