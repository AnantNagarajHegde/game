using System;
using System.Threading;
using System.Threading.Tasks;
namespace Paridhwani {
 public enum DialogueTextStatus {Ready,Unavailable,TimedOut,Cancelled,Superseded}
 public sealed class DialogueTextResult {public DialogueTextStatus Status;public TeaLine Line;}
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
 int generation;CancellationTokenSource active;
 public bool Busy {get;private set;}
 public void Cancel() {generation++;if(active!=null){active.Cancel();active=null;}Busy=false;}
 public async Task<DialogueTextResult> Send(Func<CancellationToken,Task<TeaLine>> request,int timeoutMs=8000) {
 Cancel();int id=generation;var cancel=new CancellationTokenSource();active=cancel;Busy=true;
 try {
 if(request==null)return Result(DialogueTextStatus.Unavailable);
 var work=request(cancel.Token);if(work==null)return Result(DialogueTextStatus.Unavailable);
 _=work.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
 var deadline=Task.Delay(Math.Max(1,timeoutMs),cancel.Token);var completed=await Task.WhenAny(work,deadline);
 if(id!=generation)return Result(DialogueTextStatus.Superseded);
 if(completed!=work)return Result(cancel.IsCancellationRequested?DialogueTextStatus.Cancelled:DialogueTextStatus.TimedOut);
 var line=Snapshot(await work);if(line==null)return Result(DialogueTextStatus.Unavailable);
 return new DialogueTextResult{Status=DialogueTextStatus.Ready,Line=line};
 } catch(OperationCanceledException){return Result(id==generation?DialogueTextStatus.Cancelled:DialogueTextStatus.Superseded);}catch(Exception){return Result(id==generation?DialogueTextStatus.Unavailable:DialogueTextStatus.Superseded);}
 finally {if(active==cancel){active=null;Busy=false;}cancel.Cancel();cancel.Dispose();}
 }
 internal static TeaLine Snapshot(TeaLine source) {
 if(source==null)return null;
 var line=new TeaLine(source.Kannada,source.Roman,source.English);
 if((string.IsNullOrWhiteSpace(line.Kannada) && string.IsNullOrWhiteSpace(line.English)) || (line.Kannada?.Length??0)>4096 || (line.Roman?.Length??0)>4096 || (line.English?.Length??0)>4096)return null;
 return line;
 }
 static DialogueTextResult Result(DialogueTextStatus status) => new DialogueTextResult{Status=status};
 public void Dispose(){Cancel();}
 }
 public sealed partial class TeaStallSession {
 public Func<DialoguePrompt,CancellationToken,Task<TeaLine>> OptionalDialogueText {get;set;}
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
 var pending=textRequests.Send(token=>handler(prompt,token));OptionalDialogueStatus="Thinking...";ui.ShowConversation();
 var reply=await pending;if(!this || Turn!=turn || ActiveNpc!=npc || ui.Mode!="conversation" || reply.Status==DialogueTextStatus.Superseded)return;
 if(reply.Status==DialogueTextStatus.Ready){optionalLine=new TeaLine(reply.Line.Kannada,reply.Line.Roman,reply.Line.English);OptionalDialogueStatus="Use a supported reply when you are ready to continue.";}
 else OptionalDialogueStatus=reply.Status==DialogueTextStatus.TimedOut?"That reply took too long. Try again or use a supported reply.":"A reply is unavailable. Try again or use a supported reply.";
 ui.ShowConversation();
 }
 void OnDestroy(){textRequests.Dispose();}
 }
}
