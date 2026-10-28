using System;
using System.Threading;
using System.Threading.Tasks;
namespace Paridhwani {
 public enum DialogueTextStatus {Ready,Unavailable,TimedOut,Cancelled,Superseded}
 public sealed class DialogueTextResult {public DialogueTextStatus Status;public TeaLine Line;internal int Generation;}
 // Optional transport-independent text service. It never receives or returns quest effects.
 public sealed class DialoguePrompt {
 public readonly string Speaker,TurnId,LearnerText;
 public readonly TeaLine AuthoredLine;
 public readonly TeaLine[] AvailableReplies;
 public DialoguePrompt(string speaker,TeaTurn turn,string input) {Speaker=speaker;TurnId=turn.Id;LearnerText=Limit(input);AuthoredLine=Copy(turn.Line);AvailableReplies=turn.Choices.ConvertAll(c=>Copy(c.Line)).ToArray();}
 static string Limit(string text) {if(string.IsNullOrEmpty(text))return "";var result=new System.Text.StringBuilder();var elements=System.Globalization.StringInfo.GetTextElementEnumerator(text);while(elements.MoveNext()){var item=elements.GetTextElement();if(result.Length+item.Length>512)break;result.Append(item);}return result.ToString();}
 static TeaLine Copy(TeaLine line) => new TeaLine(line.Kannada,line.Roman,line.English);
 }
 public sealed class DialogueTextRequests : IDisposable {
 readonly object gate=new object();
 int generation,cancellationCallbackFailures;CancellationTokenSource active;bool disposed;
 public bool Busy {get {lock(gate)return active!=null;}}
 public int CancellationCallbackFailures => Volatile.Read(ref cancellationCallbackFailures);
 public bool IsCurrent(DialogueTextResult result) {lock(gate)return !disposed && result!=null && result.Generation==generation;}
 bool IsCurrent(int id) {lock(gate)return !disposed && id==generation;}
 public void Cancel() {
 CancellationTokenSource previous;
 lock(gate){generation++;previous=active;active=null;}
 CancelTransport(previous);
 }
 void CancelTransport(CancellationTokenSource source) {
 if(source==null)return;
 try {source.Cancel();}
 catch(AggregateException error){Interlocked.Add(ref cancellationCallbackFailures,error.InnerExceptions.Count);}
 catch(ObjectDisposedException) { /* The old request already finished cleanup. */ }
 }
 public async Task<DialogueTextResult> Send(Func<CancellationToken,Task<TeaLine>> request,int timeoutMs=8000) {
 int id;CancellationTokenSource previous,cancel;
 lock(gate) {
 if(disposed)return Result(DialogueTextStatus.Unavailable,generation);
 id=++generation;previous=active;cancel=new CancellationTokenSource();active=cancel;
 }
 // Never invoke transport callbacks under the state lock; they may reenter the request coordinator.
 CancelTransport(previous);
 var wake=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
 var deadlineStop=new CancellationTokenSource();
 var registration=cancel.Token.Register(()=>wake.TrySetResult(true));
 Task<TeaLine> work=null;
 try {
 if(!IsCurrent(id))return Result(DialogueTextStatus.Superseded,id);
 if(request==null)return Result(DialogueTextStatus.Unavailable,id);
 work=request(cancel.Token);if(work==null)return Result(DialogueTextStatus.Unavailable,id);
 _=work.ContinueWith(t=>{var ignored=t.Exception;},CancellationToken.None,TaskContinuationOptions.OnlyOnFaulted|TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
 var deadline=Task.Delay(Math.Max(1,timeoutMs),deadlineStop.Token);
 var completed=await Task.WhenAny(work,deadline,wake.Task);
 if(!IsCurrent(id))return Result(DialogueTextStatus.Superseded,id);
 if(completed!=work)return Result(cancel.IsCancellationRequested?DialogueTextStatus.Cancelled:DialogueTextStatus.TimedOut,id);
 var line=Snapshot(await work);
 if(!IsCurrent(id))return Result(DialogueTextStatus.Superseded,id);
 return line==null?Result(DialogueTextStatus.Unavailable,id):new DialogueTextResult{Status=DialogueTextStatus.Ready,Line=line,Generation=id};
 } catch(OperationCanceledException){return Result(IsCurrent(id)?DialogueTextStatus.Cancelled:DialogueTextStatus.Superseded,id);}
 catch(Exception){return Result(IsCurrent(id)?DialogueTextStatus.Unavailable:DialogueTextStatus.Superseded,id);}
 finally {
 lock(gate){if(active==cancel)active=null;}
 if(work==null || !work.IsCompleted)CancelTransport(cancel);
 // Stopping the local deadline must not cancel a transport that already completed normally.
 deadlineStop.Cancel();registration.Dispose();deadlineStop.Dispose();cancel.Dispose();
 }
 }
 internal static TeaLine Snapshot(TeaLine source) {
 if(source==null)return null;
 var line=new TeaLine(source.Kannada,source.Roman,source.English);
 if((string.IsNullOrWhiteSpace(line.Kannada) && string.IsNullOrWhiteSpace(line.English)) || (line.Kannada?.Length??0)>4096 || (line.Roman?.Length??0)>4096 || (line.English?.Length??0)>4096)return null;
 return line;
 }
 static DialogueTextResult Result(DialogueTextStatus status,int id) => new DialogueTextResult{Status=status,Generation=id};
 public void Dispose(){lock(gate)disposed=true;Cancel();}
 }
 public sealed partial class TeaStallSession {
 Func<DialoguePrompt,CancellationToken,Task<TeaLine>> optionalDialogueText;
 // Configure/replace on the Unity thread. The service owner retains its connection/disposal lifetime.
 public Func<DialoguePrompt,CancellationToken,Task<TeaLine>> OptionalDialogueText {
 get=>optionalDialogueText;
 set {
 if(ReferenceEquals(optionalDialogueText,value))return;
 optionalDialogueText=value;textRequests.Cancel();optionalLine=null;OptionalDialogueStatus="";
 if(ui && ui.Mode=="conversation" && Turn!=null)ui.ShowConversation();
 }
 }
 public void SetOptionalDialogueService(IDialogueTextService service) {OptionalDialogueText=service==null?null:DialogueServiceAdapter.Bind(service);}
 readonly DialogueTextRequests textRequests=new DialogueTextRequests();TeaLine optionalLine;
 public string OptionalDialogueInput {get;set;}="";
 public string OptionalDialogueStatus {get;private set;}="";
 public bool OptionalDialogueBusy => textRequests.Busy;
 public TeaLine VisibleConversationLine => optionalLine??Turn?.Line;
 public bool HasOptionalDialogueLine => optionalLine!=null;
 public void UseAuthoredDialogue() {
 if(ui.Mode!="conversation" || Turn==null)return;
 textRequests.Cancel();optionalLine=null;OptionalDialogueStatus="Original conversation line. Your supported replies are unchanged.";ui.ShowConversation();
 }
 void NewDialogueTurn(){textRequests.Cancel();optionalLine=null;OptionalDialogueStatus="";OptionalDialogueInput="";}
 void SuspendDialogueText(){bool waiting=textRequests.Busy;textRequests.Cancel();if(waiting)OptionalDialogueStatus="Reply cancelled. Your conversation is still here.";}
 public void CancelOptionalDialogue(){textRequests.Cancel();OptionalDialogueStatus="You can continue with the supported replies.";if(ui.Mode=="conversation")ui.ShowConversation();}
 public async void AskOptionalDialogue() {
 if(OptionalDialogueText==null || ui.Mode!="conversation" || Turn==null || string.IsNullOrWhiteSpace(OptionalDialogueInput))return;
 optionalLine=null;var turn=Turn;var npc=ActiveNpc;var prompt=new DialoguePrompt(npc?npc.displayName:"",turn,OptionalDialogueInput);
 OptionalDialogueInput=prompt.LearnerText;var handler=OptionalDialogueText;
 var pending=textRequests.Send(token=>handler(prompt,token));
 if(this && Turn==turn && ActiveNpc==npc && ui.Mode=="conversation" && ReferenceEquals(OptionalDialogueText,handler) && textRequests.Busy) {
 OptionalDialogueStatus="Thinking...";ui.ShowConversation();
 }
 var reply=await pending;if(!this || Turn!=turn || ActiveNpc!=npc || ui.Mode!="conversation" || !textRequests.IsCurrent(reply) || reply.Status==DialogueTextStatus.Superseded)return;
 if(reply.Status==DialogueTextStatus.Ready){optionalLine=new TeaLine(reply.Line.Kannada,reply.Line.Roman,reply.Line.English);OptionalDialogueStatus="Use a supported reply when you are ready to continue.";}
 else OptionalDialogueStatus=reply.Status==DialogueTextStatus.TimedOut?"That reply took too long. Try again or use a supported reply.":"A reply is unavailable. Try again or use a supported reply.";
 ui.ShowConversation();
 }
 void OnDestroy(){textRequests.Dispose();}
 }
}
