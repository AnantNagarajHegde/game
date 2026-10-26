#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
namespace Paridhwani {
 // Explicitly opt-in; never sends telemetry or changes the target's settings.
 public sealed class VillageFrameProfile : MonoBehaviour {
 [Serializable] sealed class Report {public int frames,width,height;public string quality;public float meanFrameMs,p95FrameMs,maxFrameMs;public long allocatedBytes,reservedBytes;public string note="Frame intervals include CPU/GPU waits and presentation. This is not a measured minimum-spec certification.";}
 readonly List<float> intervals=new List<float>();int warmup=120;bool written,begun;
 void Update() {if(written)return;if(!begun){var session=GetComponent<TeaStallSession>();if(!session || session.ui.IsModal || session.player.Speed<.1f)return;begun=true;}if(warmup-->0)return;intervals.Add(Time.unscaledDeltaTime*1000);if(intervals.Count>=600)Write();}
 void Write() {
 if(written || intervals.Count==0)return;written=true;intervals.Sort();float sum=0;foreach(var ms in intervals)sum+=ms;
 var session=GetComponent<TeaStallSession>();var data=new Report{frames=intervals.Count,width=Screen.width,height=Screen.height,quality=session?session.Progress.quality.ToString():"unknown",meanFrameMs=sum/intervals.Count,p95FrameMs=intervals[(int)((intervals.Count-1)*.95f)],maxFrameMs=intervals[intervals.Count-1],allocatedBytes=Profiler.GetTotalAllocatedMemoryLong(),reservedBytes=Profiler.GetTotalReservedMemoryLong()};
 try {string directory=Path.Combine(Application.persistentDataPath,"profiles");Directory.CreateDirectory(directory);string path=Path.Combine(directory,"frames-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".json");File.WriteAllText(path,JsonUtility.ToJson(data,true));Debug.Log("PARIDHWANI_FRAME_PROFILE "+path);}catch(Exception error){Debug.LogWarning("Frame report could not be saved: "+error.Message);}
 }
 void OnApplicationQuit(){Write();}
 }
}
#endif
