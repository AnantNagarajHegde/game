using UnityEngine;
namespace Paridhwani {
 public sealed class VillageCharacter : MonoBehaviour {
 public Transform leftArm,rightArm,leftLeg,rightLeg,head;
 public VillageWalker walker;public Transform lookTarget;
 public bool talking,working;
 float phase;Vector3 previousPosition,restPosition;bool restCaptured;
 void OnEnable() {previousPosition=transform.position;if(!restCaptured){restPosition=transform.localPosition;restCaptured=true;}phase=Mathf.Repeat(transform.position.x*1.3f+transform.position.z*.7f,6.28f);}
 void Update() {
 var delta=transform.position-previousPosition;previousPosition=transform.position;delta.y=0;
 float observed=delta.magnitude<3?delta.magnitude/Mathf.Max(.001f,Time.deltaTime):0;
 float speed=walker?walker.Speed:Mathf.Min(4,observed);phase+=Time.deltaTime*Mathf.Max(.45f,speed*3);
 float swing=Mathf.Sin(phase)*Mathf.Min(speed*13,32);
 float gesture=talking?Mathf.Sin(Time.time*2.1f)*6:working?Mathf.Sin(Time.time*.9f)*4:Mathf.Sin(phase)*1.2f;
 if(leftArm)leftArm.localRotation=Quaternion.Euler(swing+gesture*.5f,0,8);
 if(rightArm)rightArm.localRotation=Quaternion.Euler(-swing+gesture,0,-8);
 if(leftLeg)leftLeg.localRotation=Quaternion.Euler(-swing,0,0);
 if(rightLeg)rightLeg.localRotation=Quaternion.Euler(swing,0,0);
 if(!walker && speed<.1f)transform.localPosition=restPosition+Vector3.up*(Mathf.Sin(phase)*.006f);
 if(head && lookTarget) {
 var direction=lookTarget.position-head.position;
 if(talking && transform.parent) {var flat=lookTarget.position-transform.parent.position;flat.y=0;if(flat.sqrMagnitude>.01f)transform.parent.rotation=Quaternion.Slerp(transform.parent.rotation,Quaternion.LookRotation(flat),Time.deltaTime*3);}
 var local=head.parent.InverseTransformDirection(direction);float angle=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-35,35);
 head.localRotation=Quaternion.Slerp(head.localRotation,Quaternion.Euler(talking?Mathf.Sin(Time.time*1.6f)*1.2f:0,angle,0),Time.deltaTime*2);
 }
 }
 }
 public sealed partial class TeaStallSession {
 readonly System.Collections.Generic.Dictionary<VillageNpc,VillageCharacter> characterPresentation=new System.Collections.Generic.Dictionary<VillageNpc,VillageCharacter>();
 void BindCharacterPresentation() {characterPresentation.Clear();foreach(var n in villagers)if(n) {var c=n.GetComponentInChildren<VillageCharacter>(true);if(c) {characterPresentation[n]=c;c.working=n.role==VillageNpcRole.TeaVendor || n.role==VillageNpcRole.MarketVendor || n.role==VillageNpcRole.FlowerSeller;}}}
 void UpdateCharacterPresentation() {foreach(var pair in characterPresentation)if(pair.Value)pair.Value.talking=pair.Key==ActiveNpc && ui.Mode=="conversation";}
 }
}
