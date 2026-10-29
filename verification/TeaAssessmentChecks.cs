using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Paridhwani;
using Paridhwani.AssessmentExample;
class TeaAssessmentChecks {
 static int checks;
 static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
 sealed class Handler:HttpMessageHandler {
 public byte[] Body;public bool Bad;public string Request;
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){Request=await request.Content.ReadAsStringAsync();return new HttpResponseMessage(Bad?HttpStatusCode.ServiceUnavailable:HttpStatusCode.OK){Content=new ByteArrayContent(Body){Headers={ContentType=new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")}}};}
 }
 static async Task Run(string root){
 var provider=new LocalTeaConversation();var progress=new TeaProgress();var hello=provider.Start(progress);var order=provider.Reply(hello,0,progress);
 foreach(string name in new[]{"tea","water","correction","coffee","milk","chapati","rejected","english-mix"}){
 var request=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root,name+"-request.json")));var r=request.RootElement;string id=r.GetProperty("request_id").GetString(),input=r.GetProperty("learner_text").GetString();var bytes=File.ReadAllBytes(Path.Combine(root,name+"-response.json"));
 var result=AssessmentConsumer.Read(bytes,id,"q1_2","q1_2_a",TeaAssessmentContract.Revision,new HashSet<string>{"a_tea","e_beda","e_nimage"});
 bool mapped=TeaAssessmentContract.Choice(order,input,result,out int index,out string feedback);bool expected=name=="tea"||name=="water"||name=="correction"||name=="english-mix";
 Check(mapped==expected,"production mapping "+name);if(mapped){Check(index==(name=="water"?1:0),"drink identity");Check(feedback.Length>0,"feedback retained");}
 var handler=new Handler{Body=bytes};using(var service=new LoopbackTeaAssessmentService(true,new Uri("http://127.0.0.1:8766"+TeaAssessmentContract.Route),7000,handler)){
 var received=await service.ReplyAsync(id,input,CancellationToken.None);Check(received.Length==bytes.Length,"real byte transport with in-memory handler");using(var payload=JsonDocument.Parse(handler.Request)){Check(payload.RootElement.GetProperty("step_id").GetString()=="q1_2_a","wire lesson identity");Check(payload.RootElement.EnumerateObject().GetEnumerator().MoveNext(),"payload present");}
 }
 }
 Check(!TeaAssessmentContract.RomanInput("\u0ca8\u0cae"),"script gated initially");Check(!TeaAssessmentContract.RomanInput(new string('x',513)),"submission bound");
 foreach(string url in new[]{"http://localhost:8766"+TeaAssessmentContract.Route,"https://example.com"+TeaAssessmentContract.Route,"http://127.0.0.1:8766/v1/dialogue/respond"}){bool rejected=false;try{using(var s=new LoopbackTeaAssessmentService(true,new Uri(url))) {}}catch(ArgumentException){rejected=true;}Check(rejected,"endpoint policy");}
 var fixture=File.ReadAllBytes(Path.Combine(root,"tea-response.json"));
 var badHandler=new Handler{Body=fixture,Bad=true};using(var service=new LoopbackTeaAssessmentService(true,new Uri("http://127.0.0.1:8766"+TeaAssessmentContract.Route),7000,badHandler)){bool failed=false;try{await service.ReplyAsync(new string('a',32),"Chaha beku",CancellationToken.None);}catch(DialogueTransportException){failed=true;}Check(failed,"HTTP fallback");}
 using(var requests=new DialogueTextRequests()){var delayed=new TaskCompletionSource<TeaLine>();var task=requests.Send(_=>delayed.Task);requests.Cancel();delayed.SetResult(new TeaLine("","","late"));Check((await task).Status==DialogueTextStatus.Superseded,"retired results rejected");}
 Console.WriteLine("TEA_PRODUCTION_CHECKED: "+checks+" assertions; real evaluator fixtures, production codec/consumer/mapping/transport; no sockets.");
 }
 static int Main(string[] args){Run(args[0]).GetAwaiter().GetResult();return 0;}
}
