using UnityEngine;
namespace Paridhwani {
 [DefaultExecutionOrder(100)]
 public sealed class VillageCharacter : MonoBehaviour {
 public Transform leftArm,rightArm,leftLeg,rightLeg,head;
 public VillageWalker walker;public Transform lookTarget;
 public bool talking,working,presentationPaused;
 public VendorWorkRoutine workRoutine {get;set;}
 float phase,animationTime;Vector3 previousPosition,restPosition;bool restCaptured;
 VillageCharacterGait gait;
 public VillageCharacterGait Gait=>gait;
 void Start() {if(walker && leftLeg && rightLeg && leftArm && rightArm)gait=new VillageCharacterGait(this);}
 void OnDestroy() {gait?.Dispose();}
 void OnEnable() {previousPosition=transform.position;if(!restCaptured){restPosition=transform.localPosition;restCaptured=true;}phase=Mathf.Repeat(transform.position.x*1.3f+transform.position.z*.7f,6.28f);}
 void Update() {
 if(gait!=null) {gait.Update(Time.deltaTime);return;}
 if(presentationPaused) {previousPosition=transform.position;return;}
 animationTime+=Time.deltaTime;
 var delta=transform.position-previousPosition;previousPosition=transform.position;delta.y=0;
 float observed=delta.magnitude<3?delta.magnitude/Mathf.Max(.001f,Time.deltaTime):0;
 float speed=walker?walker.Speed:Mathf.Min(4,observed);phase+=Time.deltaTime*Mathf.Max(.45f,speed*3);
 float swing=Mathf.Sin(phase)*Mathf.Min(speed*13,32);
 bool work=workRoutine!=null && workRoutine.PresentationEnabled && !talking;
 float gesture=talking?Mathf.Sin(animationTime*2.1f+phase*.1f)*6:working && workRoutine==null?Mathf.Sin(animationTime*.9f+phase)*4:Mathf.Sin(phase)*1.2f;
 float left=swing+gesture*.5f,right=-swing+gesture,headPitch=0;
 if(work) {
 float beat=Mathf.Sin(workRoutine.ActionTime*2.6f),slow=Mathf.Sin(workRoutine.ActionTime*1.3f);
 switch(workRoutine.Task) {
 case VendorWorkTask.CheckBoiler:left=-18;right=-48+slow*5;headPitch=9;break;
 case VendorWorkTask.ArrangeCups:left=-27+beat*3;right=-42-beat*7;headPitch=12;break;
 case VendorWorkTask.CheckLedger:left=-36;right=-40+slow*2;headPitch=19;break;
 case VendorWorkTask.AdjustScales:left=-25+slow*3;right=-50+slow*6;headPitch=11;break;
 }
 }
 if(leftArm)leftArm.localRotation=workRoutine!=null?Quaternion.RotateTowards(leftArm.localRotation,Quaternion.Euler(left,0,8),Time.deltaTime*95):Quaternion.Euler(left,0,8);
 if(rightArm)rightArm.localRotation=workRoutine!=null?Quaternion.RotateTowards(rightArm.localRotation,Quaternion.Euler(right,0,-8),Time.deltaTime*95):Quaternion.Euler(right,0,-8);
 if(leftLeg)leftLeg.localRotation=Quaternion.Euler(-swing,0,0);
 if(rightLeg)rightLeg.localRotation=Quaternion.Euler(swing,0,0);
 if(!walker && speed<.1f)transform.localPosition=restPosition+Vector3.up*(Mathf.Sin(phase)*.006f);
 if(head && lookTarget && (!work || talking || (workRoutine.Task==VendorWorkTask.Rest && Vector3.Distance(lookTarget.position,transform.position)<2.4f))) {
 var direction=lookTarget.position-head.position;
 if(talking && transform.parent) {var flat=lookTarget.position-transform.parent.position;flat.y=0;if(flat.sqrMagnitude>.01f)transform.parent.rotation=Quaternion.Slerp(transform.parent.rotation,Quaternion.LookRotation(flat),Time.deltaTime*3);}
 var local=head.parent.InverseTransformDirection(direction);float angle=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-35,35);
 head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(talking?Mathf.Sin(animationTime*1.6f)*1.2f:0,angle,0),Time.deltaTime*2);
 }
 else if(head && work) head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(headPitch,workRoutine.Task==VendorWorkTask.Rest?Mathf.Sin(phase*.5f)*9:0,0),Time.deltaTime*2);
 }
 }
 public sealed partial class TeaStallSession {
 readonly System.Collections.Generic.Dictionary<VillageNpc,VillageCharacter> characterPresentation=new System.Collections.Generic.Dictionary<VillageNpc,VillageCharacter>();
 readonly System.Collections.Generic.Dictionary<VillageNpc,VillageVendorWork> vendorWork=new System.Collections.Generic.Dictionary<VillageNpc,VillageVendorWork>();
 void BindCharacterPresentation() {characterPresentation.Clear();vendorWork.Clear();foreach(var n in villagers)if(n) {var c=n.GetComponentInChildren<VillageCharacter>(true);if(c) {characterPresentation[n]=c;c.working=n.role==VillageNpcRole.TeaVendor || n.role==VillageNpcRole.MarketVendor || n.role==VillageNpcRole.FlowerSeller;if(n.role==VillageNpcRole.TeaVendor || n.role==VillageNpcRole.MarketVendor)vendorWork[n]=new VillageVendorWork(n,c);}}}
 void UpdateCharacterPresentation() {
 foreach(var pair in characterPresentation)if(pair.Key && pair.Value) {
 pair.Value.talking=pair.Key==ActiveNpc && ui.Mode=="conversation";
 pair.Value.presentationPaused=ui.IsModal && !pair.Value.talking;
 if(vendorWork.TryGetValue(pair.Key,out var routine))routine.Update(Time.deltaTime,player.transform,ui.IsModal || player.IsTransitioning,Progress.fridayFinaleStarted && !Progress.epilogueStarted);
 }
 }
 }
}
