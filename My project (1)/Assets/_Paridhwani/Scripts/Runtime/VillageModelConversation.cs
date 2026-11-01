using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Paridhwani {
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
 // Opt-in in-process response for isolated Editor/development smoke tests.
 internal sealed class VillageModelTestReply {public TeaLine Line;public int Choice;public string Feedback;}
#endif
 public sealed partial class TeaStallSession {
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
  internal Func<TeaTurn,string,CancellationToken,Task<VillageModelTestReply>> VillageModelTestTransport;
#endif
  static readonly HttpClient villageModelClient = new HttpClient(new HttpClientHandler {
   AllowAutoRedirect=false, UseProxy=false, UseCookies=false, UseDefaultCredentials=false,
   AutomaticDecompression=DecompressionMethods.None
  }) { Timeout=Timeout.InfiniteTimeSpan };
  static readonly Uri villageModelEndpoint=new Uri("http://127.0.0.1:8767/v1/village/turn");
  readonly Dictionary<string,List<string[]>> villageModelHistory=new Dictionary<string,List<string[]>>();
  public string VillageModelObjective=>Provider is LocalTeaConversation && Turn?.Id=="hello" ? "Greet Manjunatha and introduce yourself by name." : Progress?.Objective??"Talk to the villager.";
  async void AskVillageModel() {
   if(Turn==null || !ui || ui.Mode!="conversation" || TeaAssessmentBusy || string.IsNullOrWhiteSpace(TeaAssessmentInput))return;
   teaAssessmentRequests.Cancel();textRequests.Cancel();teaTopicLine=null;
   var turn=Turn;var provider=Provider;var npc=ActiveNpc;
   string input=TeaAssessmentInput.Trim();
   if(input.Length>512){TeaAssessmentFeedback="Please keep your message under 512 characters.";ui.ShowConversation();return;}
   ui.RecordChatMessage(input);
   string id=Guid.NewGuid().ToString("N"),objective=VillageModelObjective,speaker=npc?npc.displayName:turn.Speaker,role=npc?npc.roleLabel:"Villager";int choice=-1;string feedback="";
   var history=villageModelHistory.TryGetValue(speaker,out var previous)?previous.ToArray():Array.Empty<string[]>();
   var pending=teaAssessmentRequests.Send(async token=>{
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
    if(VillageModelTestTransport!=null){
     var testReply=await VillageModelTestTransport(turn,input,token);
     if(testReply?.Line==null || testReply.Choice < -1 || testReply.Choice>=turn.Choices.Count || string.IsNullOrWhiteSpace(testReply.Line.English) || testReply.Line.English.Length>1000 || (testReply.Line.Roman?.Length??0)>1000 || (testReply.Feedback?.Length??0)>1000)throw new InvalidDataException("Invalid test reply");
     choice=testReply.Choice;feedback=testReply.Feedback;return testReply.Line;
    }
#endif
    using(var stream=new MemoryStream()) {
     using(var json=new Utf8JsonWriter(stream)) {
      json.WriteStartObject();json.WriteNumber("version",1);json.WriteString("request_id",id);
      json.WriteString("turn_id",turn.Id);json.WriteString("speaker",speaker);json.WriteString("role",role);
      json.WriteString("learner_text",input);json.WriteString("objective",objective);json.WriteString("npc_roman",turn.Line.Roman??"");json.WriteString("npc_english",turn.Line.English??"");
      json.WriteStartArray("history");foreach(var exchange in history){json.WriteStartObject();json.WriteString("player",exchange[0]);json.WriteString("npc",exchange[1]);json.WriteEndObject();}json.WriteEndArray();
      json.WriteStartArray("choices");
      foreach(var c in turn.Choices){json.WriteStartObject();json.WriteString("roman",c.Line.Roman??"");json.WriteString("english",c.Line.English??"");json.WriteEndObject();}
      json.WriteEndArray();json.WriteEndObject();
     }
     using(var message=new HttpRequestMessage(HttpMethod.Post,villageModelEndpoint)) {
      message.Content=new ByteArrayContent(stream.ToArray());message.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json"){CharSet="utf-8"};
      using(var response=await villageModelClient.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false)) {
       if(response.StatusCode!=HttpStatusCode.OK || response.Content.Headers.ContentLength>65536)throw new InvalidDataException("Model unavailable");
       using(var body=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))using(var output=new MemoryStream()) {
        var buffer=new byte[4096];int count;while((count=await body.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))>0){if(output.Length+count>65536)throw new InvalidDataException("Reply too large");output.Write(buffer,0,count);}
        using(var doc=JsonDocument.Parse(output.ToArray(),new JsonDocumentOptions{MaxDepth=8})) {
         var root=doc.RootElement;
         if(root.GetProperty("version").GetInt32()!=1 || root.GetProperty("request_id").GetString()!=id || root.GetProperty("turn_id").GetString()!=turn.Id)throw new InvalidDataException("Uncorrelated reply");
         choice=root.GetProperty("choice").GetInt32();if(choice < -1 || choice>=turn.Choices.Count)throw new InvalidDataException("Invalid choice");
         string roman=root.GetProperty("roman").GetString(),english=root.GetProperty("english").GetString();feedback=root.GetProperty("feedback").GetString();
         if(string.IsNullOrWhiteSpace(english)||english.Length>1000||(roman?.Length??0)>1000||(feedback?.Length??0)>1000)throw new InvalidDataException("Invalid model text");
         return new TeaLine("",roman??"",english);
        }
       }
      }
     }
    }
   },90000);
   TeaAssessmentFeedback="Thinking...";ui.ShowConversation();var result=await pending;
   if(!this || Turn!=turn || ActiveNpc!=npc || !ReferenceEquals(provider,Provider) || ui.Mode!="conversation" || !teaAssessmentRequests.IsCurrent(result))return;
   if(result.Status!=DialogueTextStatus.Ready){TeaAssessmentFeedback="The conversation service is unavailable. Start the local service and try sending your message again.";ui.ShowConversation();return;}
   teaTopicLine=result.Line;TeaAssessmentFeedback=feedback??"";
   if(!villageModelHistory.TryGetValue(speaker,out var exchanges)){exchanges=new List<string[]>();villageModelHistory[speaker]=exchanges;}
   exchanges.Add(new[]{input,result.Line.English});if(exchanges.Count>6)exchanges.RemoveAt(0);
   if(TeaAssessmentInput.Trim()==input)TeaAssessmentInput="";
   if(choice>=0){
    // Preserve the generated reply before the authored transition clears the turn.
    // The request correlation/current-turn checks above prevent stale or invalid actions.
    ui.RecordCurrentChatReply(result.Line);
    Choose(choice,false);
    // An unavailable activity can leave the same conversation open.
    if(ui.Mode=="conversation" && Turn==turn)ui.ShowConversation();
    return;
   }
   ui.ShowConversation();
  }
 }
}
