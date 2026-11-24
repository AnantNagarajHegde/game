using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani {
 public sealed partial class TeaStallUI {
  internal Label FollowJointCaption(TeaLine line) {
   var scroll=root?.Q<ScrollView>("dialogue-scroll");
   var label=scroll?.Q("dialogue-speech")?.Q<Label>(className:"kannada");
   if(label==null || !ReferenceEquals(label.userData,line))return null;
   void Follow() {
    if(label.panel!=null && label.layout.height>0 && scroll.contentViewport.layout.height>0)scroll.ScrollTo(label);
   }
   Follow();
   // The panel rebuild and wrapped text receive their final bounds on the next layout pass.
   label.schedule.Execute(Follow).StartingIn(25);
   return label;
  }

  internal static bool JointCaptionInView(Label label) {
   var scroll=label?.GetFirstAncestorOfType<ScrollView>();
   if(scroll==null || label.panel==null)return false;
   Rect caption=label.worldBound, view=scroll.contentViewport.worldBound;
   return caption.width>0 && caption.height>0 && view.height>0 &&
    caption.xMin>=view.xMin-2 && caption.xMax<=view.xMax+2 &&
    caption.yMin>=view.yMin-2 && caption.yMax<=view.yMax+2;
  }
 }

 public sealed partial class TeaStallSession {
  static readonly bool JointTeaEnabled=Environment.GetEnvironmentVariable("PARIDHWANI_MANJUNATHA_JOINT")=="1";
  static readonly HttpClient jointClient=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false,UseCookies=false}){Timeout=Timeout.InfiniteTimeSpan};
  TeaLine jointDisplayLine;
  bool UseJointTea=>JointTeaEnabled && ActiveNpc && ActiveNpc.role==VillageNpcRole.TeaVendor;

  // Read one correlated text/audio packet at a time. The source's playback cursor drives the caption.
  async Task<VillageConversationReply> StreamJointTea(VillageConversationRequest request,CancellationToken requestToken) {
   var endpoint=VillageServiceConnection.FromEnvironment().TurnEndpoint;
   if(!endpoint.IsLoopback)throw new DialogueRequestException(DialogueFailure.Configuration);
   using(var cancel=CancellationTokenSource.CreateLinkedTokenSource(requestToken))
   using(var post=new HttpRequestMessage(HttpMethod.Post,new Uri(endpoint,"/joint/turn"))) {
    var token=cancel.Token;
    villageSpeechCancel=cancel;
    var line=new TeaLine("","","");
    jointDisplayLine=line;
    string payload=JsonSerializer.Serialize(new Dictionary<string,string>{{"id",request.Id},{"prompt",request.Input}});
    post.Content=new StringContent(payload,Encoding.UTF8,"application/json");
    int sequence=0;
    bool first=true;
    double priorEnd=0;
    try {
     using(var response=await jointClient.SendAsync(post,HttpCompletionOption.ResponseHeadersRead,token)) {
      response.EnsureSuccessStatusCode();
      using(var registration=token.Register(response.Dispose))
      using(var reader=new StreamReader(await response.Content.ReadAsStreamAsync(),Encoding.UTF8)) {
       while(sequence<=32) {
        var raw=await reader.ReadLineAsync();
        token.ThrowIfCancellationRequested();
        if(raw==null || raw.Length>950000)throw new InvalidDataException("Incomplete joint audio stream");
        using(var doc=JsonDocument.Parse(raw)) {
         var packet=doc.RootElement;
         if(packet.GetProperty("id").GetString()!=request.Id || packet.GetProperty("seq").GetInt32()!=sequence)
          throw new InvalidDataException("Uncorrelated joint audio");
         if(packet.TryGetProperty("done",out var done) && done.GetBoolean()) {
          if(first)throw new InvalidDataException("Empty joint reply");
          line.Roman=packet.GetProperty("roman").GetString()??"";
          line.English=packet.GetProperty("english").GetString()??"";
          if(line.Roman.Length>1000 || line.English.Length>1000)throw new InvalidDataException("Reading aid too long");
          break;
         }
         var text=packet.GetProperty("text").GetString();
         byte[] data=packet.GetProperty("wav").GetBytesFromBase64();
         if(string.IsNullOrWhiteSpace(text)||text.Length>160||line.Kannada.Length+text.Length>1000||
            !VillageSpeechPcm.TryDecode(data,out var pcm,out int hz)||hz!=16000)
          throw new InvalidDataException("Invalid joint text/audio packet");
         bool audible=!Progress.muted && Progress.masterVolume>0;
         if(audible) {
          if(!villageSpeechSource){villageSpeechSource=gameObject.AddComponent<AudioSource>();villageSpeechSource.playOnAwake=false;villageSpeechSource.spatialBlend=0;}
          while(villageSpeechSource.isPlaying){await Task.Yield();token.ThrowIfCancellationRequested();}
          if(villageSpeechClip)Destroy(villageSpeechClip);
          villageSpeechClip=AudioClip.Create("Manjunatha",pcm.Length,1,hz,false);
          if(!villageSpeechClip || !villageSpeechClip.SetData(pcm,0))throw new InvalidDataException("Clip upload failed");
          villageSpeechSource.clip=villageSpeechClip;villageSpeechSource.volume=Progress.masterVolume;villageSpeechSource.Play();
          float deadline=Time.realtimeSinceStartup+3;
          while(villageSpeechSource.timeSamples==0){await Task.Yield();token.ThrowIfCancellationRequested();if(Time.realtimeSinceStartup>deadline)throw new TimeoutException("Audio did not advance");}
          double began=AudioSettings.dspTime-(double)villageSpeechSource.timeSamples/hz;
          if(TeaBenchmark && priorEnd>0)Debug.Log("JOINT_AUDIO_GAP_MS "+Mathf.RoundToInt((float)Math.Max(0,began-priorEnd)*1000));
          priorEnd=began+(double)pcm.Length/hz;
         }
         line.Kannada+=(first?"":" ")+text;
         if(first){conversation.ReceiveStreamingText(line,request.Input);ui.CompleteChatMessage(conversation.DisplayMessageId,true);ui.ShowConversation();}
         ui.SetTeaCaption(line,line.Kannada);
         var label=ui.FollowJointCaption(line);
         if(TeaBenchmark && audible) {
          float layoutDeadline=Time.realtimeSinceStartup+0.6f;
          await Task.Yield();
          while(!TeaStallUI.JointCaptionInView(label) && Time.realtimeSinceStartup<layoutDeadline){await Task.Yield();token.ThrowIfCancellationRequested();}
          if(!TeaStallUI.JointCaptionInView(label) || label.text!=line.Kannada || villageSpeechSource.timeSamples<=0)
           throw new InvalidDataException("Caption outside the viewport or out of sync with the joint packet");
          if(first && teaSubmittedAt>0){Debug.Log("TEA_FIRST_AUDIO_MS "+Mathf.RoundToInt((Time.realtimeSinceStartup-teaSubmittedAt)*1000));teaSubmittedAt=0;}
          if(first && !jointCaptured){jointCaptured=true;ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("PARIDHWANI_TEA_CAPTURE")),"joint.png"));}
          Debug.Log("JOINT_CAPTION seq="+sequence+" cursor="+villageSpeechSource.timeSamples+" chars="+line.Kannada.Length+" visible=true");
         }
         ++sequence;if(audible)HeardPhrases++;first=false;
        }
       }
       if(sequence>32)throw new InvalidDataException("Joint reply too long");
       while(villageSpeechSource && villageSpeechSource.isPlaying){await Task.Yield();token.ThrowIfCancellationRequested();}
       CompletedVoiceLines++;
       return new VillageConversationReply("none",line);
      }
     }
    } finally {
     if(villageSpeechCancel==cancel) {
      villageSpeechCancel=null;
      if(villageSpeechSource){villageSpeechSource.Stop();villageSpeechSource.clip=null;}
      if(villageSpeechClip){Destroy(villageSpeechClip);villageSpeechClip=null;}
     }
    }
   }
  }

  bool jointCaptured;
  IEnumerator CheckJointTea(List<float> beforeFrames,List<float> activeFrames) {
   string[] prompts={"ಎರಡು ಚಹಾ ಕೊಡಿ. ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಬೇಡ.","three hot coffees please with less sugar",
    "naalku cup chaha ugurubechchage sakkare beda","ಒಂದು ಬಿಸಿ ಕಾಫಿ ಬೇಕು","four warm coffees one without sugar","ಮೂರು ಕಪ್ ಚಹಾ ಉಗುರುಬೆಚ್ಚಗೆ ಕೊಡಿ"};
   string[] expected={"ಸರಿ, ಎರಡು ಕಪ್ ಚಹಾ ಕೊಡುತ್ತೇನೆ. ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ.","ಸರಿ, ಮೂರು ಕಪ್ ಕಾಫಿ ಕೊಡುತ್ತೇನೆ. ಸಕ್ಕರೆ ಕಡಿಮೆ ಹಾಕುತ್ತೇನೆ. ಬಿಸಿಯಾಗಿ ತರುತ್ತೇನೆ.",
    "ಸರಿ, ನಾಲ್ಕು ಕಪ್ ಚಹಾ ಕೊಡುತ್ತೇನೆ. ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ. ಬೆಚ್ಚಗೆ ತರುತ್ತೇನೆ.","ಸರಿ, ಒಂದು ಕಪ್ ಕಾಫಿ ಕೊಡುತ್ತೇನೆ. ಬಿಸಿಯಾಗಿ ತರುತ್ತೇನೆ.",
    "ಸರಿ, ನಾಲ್ಕು ಕಪ್ ಕಾಫಿ ಕೊಡುತ್ತೇನೆ. ಒಂದಕ್ಕೆ ಸಕ್ಕರೆ ಹಾಕುವುದಿಲ್ಲ. ಬೆಚ್ಚಗೆ ತರುತ್ತೇನೆ.","ಸರಿ, ಮೂರು ಕಪ್ ಚಹಾ ಕೊಡುತ್ತೇನೆ. ಬೆಚ್ಚಗೆ ತರುತ್ತೇನೆ."};
   float until=Time.realtimeSinceStartup+40;
   while(CompletedVoiceLines<1 && Time.realtimeSinceStartup<until)yield return null;
   if(CompletedVoiceLines<1){Debug.LogError("JOINT_TEST_FAILED opening");Application.Quit(2);yield break;}
   var opening=Turn;
   for(int i=0;i<prompts.Length*2;i++) {
    int n=i%prompts.Length,before=CompletedVoiceLines;
    teaSubmittedAt=Time.realtimeSinceStartup;TeaAssessmentInput=prompts[n];AskTeaConversation();
    until=Time.realtimeSinceStartup+45;
    while((CompletedVoiceLines<=before || TeaAssessmentBusy) && Time.realtimeSinceStartup<until){activeFrames.Add(Time.unscaledDeltaTime);yield return null;}
    if(CompletedVoiceLines<=before || TeaAssessmentBusy || Turn!=opening || ConversationLine?.Kannada!=expected[n] || string.IsNullOrWhiteSpace(ConversationLine.Roman) || string.IsNullOrWhiteSpace(ConversationLine.English)){
     Debug.LogError("JOINT_TEST_FAILED case="+n+" answer="+ConversationLine?.Kannada);Application.Quit(2);yield break;
    }
    Debug.Log("JOINT_TEST_CASE "+n);
    if(i==0){
     yield return null;
     var label=ui.GetComponent<UIDocument>().rootVisualElement.Q("dialogue-speech")?.Q<Label>(className:"kannada");
     float deadline=Time.realtimeSinceStartup+1;
     while(!TeaStallUI.JointCaptionInView(label) && Time.realtimeSinceStartup<deadline)yield return null;
     if(!TeaStallUI.JointCaptionInView(label)){Debug.LogError("JOINT_TEST_FAILED completed caption offscreen");Application.Quit(2);yield break;}
     ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("PARIDHWANI_TEA_CAPTURE")),"joint-complete.png"));
     yield return new WaitForEndOfFrame();yield return null;
    }
   }
   Debug.Log("TEA_FRAME_P95_MS baseline="+P95(beforeFrames)+" active="+P95(activeFrames));
   Debug.Log("TEA_BENCH_OK lines="+CompletedVoiceLines+" phrases="+HeardPhrases);
   yield return new WaitForSecondsRealtime(1);Application.Quit();
  }
 }
}
