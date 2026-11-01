using System;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Paridhwani.AssessmentExample;
namespace Paridhwani {
 public interface ITeaAssessmentService {Task<byte[]> ReplyAsync(string requestId,string learnerText,CancellationToken cancellation);}
 public static class TeaAssessmentContract {
 public static bool PracticeVisible(ITeaConversationProvider provider,TeaTurn turn)=>provider is LocalTeaConversation&&turn!=null;
 public static bool PracticeAvailable(ITeaConversationProvider provider,TeaTurn turn,TeaProgress progress)=>provider is LocalTeaConversation&&(turn?.Id=="order"||turn?.Id=="clarify")&&progress!=null&&progress.greeted;
 public const string Route="/v1/assessment/evaluate";
 public const string Revision="3c1a09978760837bee0bf549e4be50edb2d8470ee381615906194ef7a69d3e0f";
 public static bool RomanInput(string input)=>!string.IsNullOrWhiteSpace(input)&&input.Length<=512&&Regex.IsMatch(input,@"^[a-zA-Z\s.,!?'-]+$");
 public static byte[] Encode(string id,string input) {
 if(id==null||!Regex.IsMatch(id,"^[0-9a-f]{32}$")||!RomanInput(input))throw new ArgumentException("Unsupported assessment input.");
 using(var stream=new MemoryStream()) {using(var json=new Utf8JsonWriter(stream)) {
 json.WriteStartObject();json.WriteNumber("version",1);json.WriteString("request_id",id);json.WriteString("quest_id","q1_2");json.WriteString("step_id","q1_2_a");json.WriteString("learner_text",input);json.WriteStartObject("player");json.WriteString("name","");json.WriteEndObject();json.WriteEndObject();}
 return stream.ToArray();}
 }
 static string Normalize(string input)=>Regex.Replace(Regex.Replace(input.ToLowerInvariant(),"[^a-z ]"," ")," +"," ").Trim();
 public static bool Choice(TeaTurn turn,string input,AssessmentResult result,out int index,out string feedback) {
 index=-1;feedback="Manjunatha looks puzzled. Try Nanage chaha beku or Nanage neeru beku, or type tea please or water please for an unscored story reply.";
 if(turn==null||(turn.Id!="order"&&turn.Id!="clarify")||!RomanInput(input)||result==null)return false;
 if(result.Outcome=="not_accepted")return false;
 if(result.AnswerId!="a_tea")return false;
 string text=Normalize(input);
 if(text=="nanage chaha beku"||text=="chaha beku"||text=="cha"||text=="tea beku")index=0;
 if(text=="nanage neeru beku"||text=="neeru beku")index=1;
 if(index<0){feedback="That request is understood, but this stall currently offers tea or water. Try Nanage chaha beku or Nanage neeru beku.";return false;}
 if(result.Outcome=="accepted_with_correction"&&result.CorrectionType!="missing_verb"){index=-1;return false;}
 string effect=index==0?"tea":"water",roman=index==0?"nanage chaha beku":turn.Id=="clarify"?"haudu neeru kodi":"nanage neeru beku";
 if(turn.Id=="clarify")index=1-index;
 if(turn.Choices.Count!=3||turn.Choices[index].Effect!=effect||turn.Choices[index].Next!="served"||Normalize(turn.Choices[index].Line.Roman)!=roman){index=-1;feedback="The authored ordering choices have changed. Leave and reopen the conversation before trying again.";return false;}
 feedback=result.Outcome=="accepted_with_correction"?"Understood. Add beku (want) or kodi (please give) to complete the request.":"Understood. Your order is ready.";
 return true;
 }
 }
}

