using UnityEngine;
namespace Paridhwani {
 [DefaultExecutionOrder(250)] // Capture renderers after articulated gait replaces the source legs.
 public sealed class VillageDistanceCulling : MonoBehaviour {
 struct Entry {public Renderer renderer;public bool originallyEnabled,scenery;}
 Entry[] entries;Transform viewer;float next,distance=80;
 public void Configure(Transform target,VillageQuality quality) {viewer=target;distance=quality==VillageQuality.Low?45:quality==VillageQuality.High?120:80;next=0;}
 void Start() {var renderers=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);entries=new Entry[renderers.Length];for(int i=0;i<renderers.Length;i++){var home=renderers[i].GetComponentInParent<VillageResidence>();entries[i]=new Entry{renderer=renderers[i],originallyEnabled=renderers[i].enabled,scenery=home && home.scenery};}}
 void LateUpdate() {if(!viewer || entries==null || Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;float limit=distance*distance;foreach(var e in entries)if(e.renderer && e.originallyEnabled)e.renderer.enabled=e.renderer.bounds.SqrDistance(viewer.position)<=(e.scenery?180*180:limit);}
 void OnDestroy() {if(entries==null)return;foreach(var e in entries)if(e.renderer)e.renderer.enabled=e.originallyEnabled;}
 }
}
