using UnityEngine;
namespace Paridhwani {
 public sealed class VillageDistanceCulling : MonoBehaviour {
 struct Entry {public Renderer renderer;public bool originallyEnabled;}
 Entry[] entries;Transform viewer;float next,distance=80;
 public void Configure(Transform target,VillageQuality quality) {viewer=target;distance=quality==VillageQuality.Low?45:quality==VillageQuality.High?120:80;next=0;}
 void Start() {var renderers=Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);entries=new Entry[renderers.Length];for(int i=0;i<renderers.Length;i++)entries[i]=new Entry{renderer=renderers[i],originallyEnabled=renderers[i].enabled};}
 void LateUpdate() {if(!viewer || entries==null || Time.unscaledTime<next)return;next=Time.unscaledTime+.25f;float limit=distance*distance;foreach(var e in entries)if(e.renderer && e.originallyEnabled)e.renderer.enabled=e.renderer.bounds.SqrDistance(viewer.position)<=limit;}
 void OnDestroy() {if(entries==null)return;foreach(var e in entries)if(e.renderer)e.renderer.enabled=e.originallyEnabled;}
 }
}
