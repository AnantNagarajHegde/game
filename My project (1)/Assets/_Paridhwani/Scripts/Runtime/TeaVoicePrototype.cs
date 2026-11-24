using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani {
 public sealed partial class TeaStallSession {
  static bool TeaVoiceEnabled => Environment.GetEnvironmentVariable("PARIDHWANI_TEA_VOICE")=="1";
  static readonly bool TeaBenchmark = Array.IndexOf(Environment.GetCommandLineArgs(),"-tea-voice-benchmark")>=0;
  static readonly bool TeaDemo = Array.IndexOf(Environment.GetCommandLineArgs(),"-tea-voice-demo")>=0;
  TeaLine captionLine;
  float teaSubmittedAt;
  static float P95(System.Collections.Generic.List<float> frames){
   if(frames.Count==0)return 0;
   frames.Sort();return frames[Mathf.CeilToInt(.95f*frames.Count)-1]*1000;
  }
  public int HeardPhrases {get; private set;}
  public int CompletedVoiceLines {get; private set;}

  async Task SpeakTeaPhrases(TeaLine line,string speaker,CancellationToken token) {
   var phrases=Regex.Matches(line.Kannada,@"[^.!?।॥]+[.!?।॥]*\s*|[.!?।॥]+\s*");
   if(phrases.Count==0)return;
   captionLine=line;
   ui.SetTeaCaption(line,"");
   int end=0;
   try {
    for(int i=0;i<phrases.Count;i++) {
     var phrase=phrases[i];
     var bytes=await VillageConversationClient.Speech(speaker,"kn-IN",phrase.Value.Trim(),token);
     token.ThrowIfCancellationRequested();
     if(!VillageSpeechPcm.TryDecode(bytes,out var pcm,out int hz))throw new InvalidDataException("Invalid speech clip");
     if(!villageSpeechSource){villageSpeechSource=gameObject.AddComponent<AudioSource>();villageSpeechSource.playOnAwake=false;villageSpeechSource.spatialBlend=0;}
     while(villageSpeechSource.isPlaying){await Task.Yield();token.ThrowIfCancellationRequested();}
     if(villageSpeechClip)Destroy(villageSpeechClip);
     villageSpeechClip=AudioClip.Create("Tea phrase",pcm.Length,1,hz,false);
     if(!villageSpeechClip.SetData(pcm,0))throw new InvalidDataException("Clip upload failed");
     villageSpeechSource.clip=villageSpeechClip;villageSpeechSource.volume=Progress.masterVolume;villageSpeechSource.Play();
     // Wait for the playback cursor on the audio device before revealing this phrase.
     float until=Time.realtimeSinceStartup+3;
     while(villageSpeechSource.timeSamples==0){await Task.Yield();token.ThrowIfCancellationRequested();if(Time.realtimeSinceStartup>until)throw new TimeoutException("Audio did not advance");}
     end=phrase.Index+phrase.Length;
     string visible=line.Kannada.Substring(0,end).TrimEnd();
     ui.SetTeaCaption(line,visible);
     if(TeaBenchmark){
      if(ui.GetComponent<UIDocument>().rootVisualElement.Q("dialogue-speech")?.Q<Label>(className:"kannada")?.text!=visible)
       throw new InvalidOperationException("Caption label mismatch");
      if(i==0 && HeardPhrases==0)ScreenCapture.CaptureScreenshot(Environment.GetEnvironmentVariable("PARIDHWANI_TEA_CAPTURE"));
      if(i==0 && teaSubmittedAt>0){Debug.Log("TEA_FIRST_AUDIO_MS "+Mathf.RoundToInt((Time.realtimeSinceStartup-teaSubmittedAt)*1000));teaSubmittedAt=0;}
      Debug.Log("TEA_CAPTION samples="+villageSpeechSource.timeSamples+" prefix="+end);
     }
     HeardPhrases++;
    }
    while(villageSpeechSource.isPlaying){await Task.Yield();token.ThrowIfCancellationRequested();}
    CompletedVoiceLines++;
   } finally {if(ReferenceEquals(captionLine,line))RestoreTeaCaption();}
  }
  void RestoreTeaCaption(){if(captionLine!=null && ui)ui.SetTeaCaption(captionLine,captionLine.Kannada);captionLine=null;}

  IEnumerator OpenVoiceDemo() {
   Application.runInBackground=true;
   yield return null;
   var npc=Array.Find(villagers,n=>n && n.role==VillageNpcRole.TeaVendor);
   player.Restore(npc.transform.position+npc.transform.forward*2, npc.transform.eulerAngles.y+180);
   var baselineFrames=new System.Collections.Generic.List<float>();
   var activeFrames=new System.Collections.Generic.List<float>();
   if(TeaBenchmark){
    Progress.textScale=Environment.GetEnvironmentVariable("PARIDHWANI_TEA_SCALE")=="1.3"?1.3f:1;
    Progress.masterVolume=0;StartConversation(npc);
    yield return new WaitForSecondsRealtime(2);
    Debug.Log("TEA_BASELINE_READY");
    string marker=Environment.GetEnvironmentVariable("PARIDHWANI_TEA_START");
    float deadline=Time.realtimeSinceStartup+210;
    while(!File.Exists(marker) && Time.realtimeSinceStartup<deadline){baselineFrames.Add(Time.unscaledDeltaTime);yield return null;}
    if(!File.Exists(marker)){Application.Quit(2);yield break;}
    Close();Progress.masterVolume=1;
   }
   StartConversation(npc);
   if(!TeaBenchmark)yield break;
   for(int step=0;step<3;step++){
    float deadline=Time.realtimeSinceStartup+45;
    while(CompletedVoiceLines<=step && Time.realtimeSinceStartup<deadline){activeFrames.Add(Time.unscaledDeltaTime);yield return null;}
    if(CompletedVoiceLines<=step){Debug.LogError("TEA_BENCH_FAILURE step="+step);Application.Quit(2);yield break;}
    if(step==0){
     teaSubmittedAt=Time.realtimeSinceStartup;
     TeaAssessmentInput="Nanage chaha beku";AskTeaConversation();
    } else if(step==1){Choose(0);}
   }
   Debug.Log("TEA_FRAME_P95_MS baseline="+P95(baselineFrames)+" active="+P95(activeFrames));
   Debug.Log("TEA_BENCH_OK lines="+CompletedVoiceLines+" phrases="+HeardPhrases);
   yield return new WaitForSecondsRealtime(1);Application.Quit();
  }
 }
}
