using System;
using System.Threading;
using System.Threading.Tasks;
namespace Paridhwani {
 public enum DialogueTextStatus {Ready,Unavailable,TimedOut,Cancelled,Superseded}
 public enum DialogueFailure {None,Connection,Configuration,ServiceAuthentication,ProviderAuthentication,RateLimited,Busy,ProviderRejected,ProviderUnavailable,InvalidRequest,InvalidResponse,Timeout,Unknown,ProviderAccessDenied,ProviderRequestBlocked}
 public sealed class DialogueRequestException : Exception {
 public readonly DialogueFailure Failure;public readonly int HttpStatus;public readonly string DiagnosticType;
 public DialogueRequestException(DialogueFailure failure,int httpStatus=0,string diagnosticType=""):base("Conversation failure: "+failure){Failure=failure;HttpStatus=httpStatus;DiagnosticType=diagnosticType;}
 // Type names are safe metadata; exception messages/inner exception messages may contain secrets.
 public static string TypeNames(Exception error) {
 var names=new System.Collections.Generic.List<string>();
 for(int i=0;i<3 && error!=null;i++,error=error.InnerException)names.Add(error.GetType().Name);
 return string.Join("/",names);
 }
 }
 public sealed class DialogueTextResult {public DialogueTextStatus Status;public TeaLine Line;public DialogueFailure Failure;public int HttpStatus;public string DiagnosticType="";internal int Generation;}
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
 if(completed!=work)return Result(cancel.IsCancellationRequested?DialogueTextStatus.Cancelled:DialogueTextStatus.TimedOut,id,DialogueFailure.Timeout);
 var line=Snapshot(await work);
 if(!IsCurrent(id))return Result(DialogueTextStatus.Superseded,id);
 return line==null?Result(DialogueTextStatus.Unavailable,id):new DialogueTextResult{Status=DialogueTextStatus.Ready,Line=line,Generation=id};
 } catch(OperationCanceledException){return Result(IsCurrent(id)?DialogueTextStatus.Cancelled:DialogueTextStatus.Superseded,id);}
 catch(DialogueRequestException error){return Result(IsCurrent(id)?DialogueTextStatus.Unavailable:DialogueTextStatus.Superseded,id,error.Failure,error.HttpStatus,error.DiagnosticType);}
 catch(Exception error){return Result(IsCurrent(id)?DialogueTextStatus.Unavailable:DialogueTextStatus.Superseded,id,DialogueFailure.Unknown,0,DialogueRequestException.TypeNames(error));}
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
 if((string.IsNullOrWhiteSpace(line.Kannada) && string.IsNullOrWhiteSpace(line.Roman) && string.IsNullOrWhiteSpace(line.English)) || (line.Kannada?.Length??0)>4096 || (line.Roman?.Length??0)>4096 || (line.English?.Length??0)>4096)return null;
 return line;
 }
 static DialogueTextResult Result(DialogueTextStatus status,int id,DialogueFailure failure=DialogueFailure.None,int httpStatus=0,string diagnosticType="") => new DialogueTextResult{Status=status,Generation=id,Failure=failure,HttpStatus=httpStatus,DiagnosticType=diagnosticType};
 public void Dispose(){lock(gate)disposed=true;Cancel();}
 }
 public sealed partial class TeaStallSession {
 void NewDialogueTurn(){ResetTeaAssessment();}
 void SuspendDialogueText(){StopVillageSpeech();conversation.Suspend();}
 void OnDestroy(){StopVillageSpeech();conversation.Dispose();}
 }
}
