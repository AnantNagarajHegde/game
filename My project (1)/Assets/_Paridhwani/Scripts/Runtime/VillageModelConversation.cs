using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Paridhwani {
 // Azure returns 16-bit mono PCM WAV. Decode only that bounded format; never
 // write generated speech to disk or relax Unity's global HTTP policy.
 public static class VillageSpeechPcm {
  static bool Tag(byte[] b,int p,string tag) => p>=0 && p+4<=b.Length && b[p]==tag[0] && b[p+1]==tag[1] && b[p+2]==tag[2] && b[p+3]==tag[3];
  static uint U32(byte[] b,int p) => (uint)(b[p] | b[p+1]<<8 | b[p+2]<<16 | b[p+3]<<24);
  static int U16(byte[] b,int p) => b[p] | b[p+1]<<8;
  public static bool TryDecode(byte[] b,out float[] samples,out int frequency) {
   samples=null;frequency=0;
   if(b==null || b.Length<44 || b.Length>4*1024*1024 || !Tag(b,0,"RIFF") || !Tag(b,8,"WAVE"))return false;
   long end=8L+U32(b,4);if(end>b.Length || end<44)return false;
   int dataStart=-1,dataSize=0;bool format=false;
   for(long p=12;p+8<=end;) {
    int at=(int)p;long size=U32(b,at+4),start=p+8,next=start+size+(size&1);
    if(start+size>end || next>end)return false;
    if(Tag(b,at,"fmt ")) {
     if(size<16 || U16(b,at+8)!=1 || U16(b,at+10)!=1 || U16(b,at+20)!=2 || U16(b,at+22)!=16)return false;
     uint rate=U32(b,at+12);if(rate<8000 || rate>48000)return false;
     frequency=(int)rate;format=true;
    } else if(Tag(b,at,"data")) {dataStart=(int)start;dataSize=(int)size;}
    p=next;
   }
   if(!format || dataStart<0 || dataSize<=0 || (dataSize&1)!=0)return false;
   samples=new float[dataSize/2];
   for(int i=0;i<samples.Length;i++)samples[i]=unchecked((short)U16(b,dataStart+i*2))/32768f;
   return true;
  }
 }
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
 internal sealed class VillageModelTestReply {public TeaLine Line;public int Choice;public string Feedback;}
#endif
 public sealed partial class TeaStallSession {
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
  internal Func<TeaTurn,string,CancellationToken,Task<VillageModelTestReply>> VillageModelTestTransport;
#endif
  CancellationTokenSource villageSpeechCancel;
  int villageSpeechGeneration;
  AudioSource villageSpeechSource;
  AudioClip villageSpeechClip;
  TeaLine villageLastSpokenLine;
  string villageLastSpokenSpeaker;
  public string VillageModelObjective=>Provider is LocalTeaConversation && (Turn?.Id=="hello"||Turn?.Id=="help") ? "Greet Manjunatha and introduce yourself by name." : Progress?.Objective??"Talk to the villager.";

  VillageConversationAction[] SnapshotVillageActions(TeaTurn turn,ITeaConversationProvider provider) {
   var result=new List<VillageConversationAction>();
   for(int i=0;i<turn.Choices.Count;i++) {
    var current=turn.Choices[i];string nextFact="";
    if(current.Activity==VillageActivity.None) {
     try {
      var copy=JsonUtility.FromJson<TeaProgress>(JsonUtility.ToJson(Progress));
      var next=provider.Reply(turn,i,copy);nextFact=next?.Line?.English??"";
     } catch(Exception error) when(error is ArgumentException || error is InvalidOperationException) {continue;}
    }
    string description=current.Line.English??"";
    if(provider is LocalTeaConversation && (turn.Id=="hello" || turn.Id=="help")) {
     if(current.Effect=="greet")description="Greet Manjunatha and introduce yourself by providing your name.";
     else if(turn.Id=="hello" && i==1)description="Explicitly ask Manjunatha to speak slowly or say you know only a little Kannada.";
    }
    result.Add(new VillageConversationAction("a"+i,description,nextFact,i));
   }
   return result.ToArray();
  }
  async void AskVillageModel() {
   if(Turn==null || !ui || ui.Mode!="conversation" || TeaAssessmentBusy || string.IsNullOrWhiteSpace(TeaAssessmentInput))return;
   ui.CloseTeaKeyboard();StopVillageSpeech();
   var turn=Turn;var provider=Provider;var npc=ActiveNpc;
   string input=TeaAssessmentInput.Trim(),speaker=npc?npc.displayName:turn.Speaker;
   if(input.Length>512)return;
   VillageConversationAction[] actions;VillageConversationRequest request;
   try {
    actions=SnapshotVillageActions(turn,provider);
    request=new VillageConversationRequest(turn.Id,speaker,npc?npc.roleLabel??"Villager":"Villager",input,
     VillageModelObjective,turn.Line.English??"",actions,conversation.History(speaker));
   } catch(Exception) {
    conversation.PreparationFailed();ui.ShowConversation();
    Debug.LogWarning("PARIDHWANI_CHAT_FAILURE kind=InvalidRequest stage=prepare");return;
   }
   Func<VillageConversationRequest,CancellationToken,Task<VillageConversationReply>> transport=null;
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
   var testTransport=VillageModelTestTransport;
   if(testTransport!=null)transport=async (message,token)=> {
    var reply=await testTransport(turn,input,token);
    if(reply==null || reply.Choice < -1 || reply.Choice>=turn.Choices.Count)throw new InvalidDataException("Invalid test reply");
    return new VillageConversationReply(reply.Choice<0?"none":"a"+reply.Choice,reply.Line);
   };
#endif
   var pending=conversation.Send(request,transport);
   string deliveryId=conversation.DisplayMessageId;
   ui.RecordChatMessage(input,deliveryId);
   if(!pending.IsCompleted)ui.ShowConversation();
   var result=await pending;
   if(this && ui)ui.CompleteChatMessage(deliveryId,result.Status==DialogueTextStatus.Ready && conversation.IsCurrent(result));
   if(!this || Turn!=turn || ActiveNpc!=npc || !ReferenceEquals(provider,Provider) || ui.Mode!="conversation" || !conversation.IsCurrent(result))return;
   if(result.Status!=DialogueTextStatus.Ready){
    Debug.LogWarning("PARIDHWANI_CHAT_FAILURE request="+request.Id+" kind="+result.Failure+" http="+result.HttpStatus+" type="+result.DiagnosticType);
    ui.ShowConversation();return;
   }
   if(result.ActionRejected)Debug.LogWarning("PARIDHWANI_CHAT_ACTION_IGNORED request="+request.Id+"; readable reply retained without story progress.");
   int choice=Array.FindIndex(actions,a=>a.Id==result.ActionId);
   if(choice>=0)Choose(actions[choice].Choice,false,true,result.Line);
   else ui.ShowConversation();
   if(ui.Mode=="conversation")PlayVillageSpeech(result.Line,speaker);
  }

  public void SpeakConversationLine(TeaLine line) {
   PlayVillageSpeech(line,ActiveNpc?ActiveNpc.displayName:Turn?.Speaker??"Villager");
  }
  async void PlayVillageSpeech(TeaLine line,string speaker) {
   if(line==null || IsAutomatedTest || (!Application.isFocused && !TeaBenchmark) || (Progress!=null && Progress.masterVolume<=0) ||
      (ReferenceEquals(line,villageLastSpokenLine) && speaker==villageLastSpokenSpeaker))return;
   string speech=string.IsNullOrWhiteSpace(line.Kannada)?line.English:line.Kannada;
   string language=string.IsNullOrWhiteSpace(line.Kannada)?"en-IN":"kn-IN";
   if(string.IsNullOrWhiteSpace(speech))return;
   StopVillageSpeech();villageLastSpokenLine=line;villageLastSpokenSpeaker=speaker;
   int generation=++villageSpeechGeneration;
   var cancel=new CancellationTokenSource();cancel.CancelAfter(35000);villageSpeechCancel=cancel;
   try {
    if(TeaVoiceEnabled && ActiveNpc && ActiveNpc.role==VillageNpcRole.TeaVendor && language=="kn-IN"){
     await SpeakTeaPhrases(line,speaker,cancel.Token);return;
    }
    var data=await VillageConversationClient.Speech(speaker,language,speech,cancel.Token);
    if(!VillageSpeechPcm.TryDecode(data,out var samples,out int rate))throw new InvalidDataException("Invalid speech audio");
    if(!this || generation!=villageSpeechGeneration || cancel.IsCancellationRequested || !Application.isFocused)return;
    villageSpeechClip=AudioClip.Create("Villager speech",samples.Length,1,rate,false);
    if(!villageSpeechClip || !villageSpeechClip.SetData(samples,0))throw new InvalidDataException("Could not load speech audio");
    if(!villageSpeechSource){villageSpeechSource=gameObject.AddComponent<AudioSource>();villageSpeechSource.playOnAwake=false;villageSpeechSource.spatialBlend=0;}
    villageSpeechSource.clip=villageSpeechClip;villageSpeechSource.volume=1;villageSpeechSource.Play();
   } catch(OperationCanceledException) {} catch(Exception error) {
    if(TeaVoiceEnabled)Debug.LogWarning("TEA_VOICE_FAILURE "+error.GetType().Name);
    // Speech is optional. Its failure must not overwrite conversation guidance.
   } finally {if(villageSpeechCancel==cancel)villageSpeechCancel=null;cancel.Dispose();}
  }
  void StopVillageSpeech() {
   RestoreTeaCaption();villageSpeechGeneration++;var cancel=villageSpeechCancel;villageSpeechCancel=null;
   if(cancel!=null)try{cancel.Cancel();}catch(ObjectDisposedException){}
   if(villageSpeechSource){villageSpeechSource.Stop();villageSpeechSource.clip=null;}
   if(villageSpeechClip){Destroy(villageSpeechClip);villageSpeechClip=null;}
  }
 }
}
