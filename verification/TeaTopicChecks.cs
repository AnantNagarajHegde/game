using System;
using System.Threading.Tasks;
using Paridhwani;
class TeaTopicChecks {
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static TeaTurn Apply(LocalTeaConversation provider,TeaTurn turn,TeaProgress p,string text,out TeaTopicPlan plan){plan=TeaTopicRouter.Plan(turn,p,text);foreach(int index in plan.Choices)turn=provider.Reply(turn,index,p);return turn;}
 static async Task Run(){
 var provider=new LocalTeaConversation();var p=new TeaProgress();var hello=provider.Start(p);
 foreach(string text in new[]{"where is Ananya?","where can I find Ananya?","Ananya iddara?","have you seen ananya"}){
  var turn=Apply(provider,hello,p,text,out var plan);Check(plan.LineId=="ananya"&&turn==hello,"Ananya before order responds without progression");Check(!p.drinkOrdered&&!p.directionsKnown&&!p.ananyaRoutineKnown,"early topic leaves save state intact");
  Check(LocalTeaConversation.AuthoredLine(plan.LineId,p).English=="Ananya didn't come yesterday. Ask Somanna at the library.","no invented current location");
 }
 var blocked=TeaTopicRouter.Plan(hello,p,"where is the library?");Check(blocked.Choices.Length==0&&blocked.LineId==null&&blocked.Feedback.Length>0,"library prerequisite with visible feedback");
 var empty=TeaTopicRouter.Plan(hello,p,"");Check(empty.Feedback.Length>0&&!empty.Assess,"empty input visible");
 var unknown=TeaTopicRouter.Plan(hello,p,"tell me the secret ending");Check(unknown.Feedback.Length>0&&!unknown.Assess,"unknown topic visible");
 var order=Apply(provider,hello,p,"hello",out _);Check(order.Id=="order"&&p.greeted,"typed greeting advances authored state");
 Check(TeaTopicRouter.Plan(order,p,"Nanage chaha beku").Assess,"Romanised order still needs assessment");
 Check(TeaTopicRouter.Plan(order,p,"Nanage kaapi beku").Assess,"unsupported item cannot bypass assessment");
 var clarify=Apply(provider,order,p,"no tea",out _);Check(clarify.Id=="clarify"&&!p.drinkOrdered,"refusal keeps authored clarify");
 Check(TeaTopicRouter.Plan(clarify,p,"Nanage neeru beku").Assess,"water practice works after refusal");
 Check(!TeaTopicRouter.Plan(order,p,"tell me about the weather").Assess,"unknown topic responds locally without service");
 var reopened=provider.Start(p);Check(TeaTopicRouter.Plan(reopened,p,"tea please").Feedback.Contains("hello first"),"unfinished reopened conversation asks greeting");
 Check(TeaTopicRouter.Plan(clarify,p,"Haudu, neeru kodi.").Choices.Length==1,"authored clarify wording is explicitly unscored");
 Check(TeaTopicRouter.Plan(order,p,"tea please 123").Choices.Length==0,"digits cannot be silently removed from an alias");
 Check(TeaTopicRouter.Plan(order,p,"\u0cac\u0cc7\u0ca1 tea please").Choices.Length==0,"mixed-script negation cannot become an order");
 var served=Apply(provider,clarify,p,"water please",out var recovery);Check(served.Id=="served"&&p.drink=="water"&&p.drinkOrdered,"explicit offline recovery preserves drink identity");Check(recovery.Feedback.Contains("no language score"),"recovery is explicitly unscored");
 var ananya=Apply(provider,served,p,"where is Ananya?",out _);Check(ananya.Id=="ananya"&&p.directionsKnown&&p.ananyaRoutineKnown,"Ananya after order traverses authored prerequisites");
 var repeat=Apply(provider,ananya,p,"where is Ananya?",out var repeated);Check(repeat==ananya&&repeated.LineId=="ananya"&&repeated.Choices.Length==0,"repeat does not replay effects");
 var library=TeaTopicRouter.Plan(ananya,p,"how do I get to the library?");Check(library.LineId=="directions"&&library.Choices.Length==0,"repeat library is authored read-only output");
 var closed=Apply(provider,ananya,p,"thank you",out _);Check(closed==null,"typed farewell can leave without displayed choices");
 foreach(string drink in new[]{"tea please","water please"}){
  var q=new TeaProgress();var initial=provider.Start(q);var early=TeaTopicRouter.Plan(initial,q,drink);Check(early.Choices.Length==0&&!q.drinkOrdered,"drink cannot skip greeting");
  var greeting=Apply(provider,initial,q,"Namaskara",out _);var done=Apply(provider,greeting,q,drink,out _);Check(done.Id=="served"&&q.drink==(drink=="tea please"?"tea":"water"),"recovery identity");
  var direction=Apply(provider,done,q,"Granthalaya ellide?",out _);Check(direction.Id=="directions"&&q.directionsKnown,"typed library mapped action");
 }
 var denied=new TeaProgress();var deniedTurn=provider.Start(denied);Check(!TeaTopicRouter.Plan(deniedTurn,denied,"not tea please").Assess&&!denied.drinkOrdered,"negation not substring-mapped");
 Check(TeaTopicRouter.Plan(ananya,p,new string('a',513)).Feedback.Length>0,"oversized visible failure");
 using(var requests=new DialogueTextRequests()){
  var late=new TaskCompletionSource<TeaLine>();var pending=requests.Send(_=>late.Task);requests.Cancel();late.SetResult(new TeaLine("","","late drink"));Check(!requests.IsCurrent(await pending),"local topic supersedes pending drink result");
  var error=await requests.Send(_=>throw new Exception("private error"));Check(error.Status==DialogueTextStatus.Unavailable,"service error has failure status");
  var never=new TaskCompletionSource<TeaLine>();var timeout=await requests.Send(_=>never.Task,1);Check(timeout.Status==DialogueTextStatus.TimedOut,"timeout visible to session");
 }
 Console.WriteLine("TEA_TOPIC_CHECKED: "+checks+" assertions; authored replies and effects; no sockets.");
 }
 static int Main(){Run().GetAwaiter().GetResult();return 0;}
}
