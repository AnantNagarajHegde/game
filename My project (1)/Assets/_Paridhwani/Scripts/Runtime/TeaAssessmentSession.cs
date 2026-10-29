using System;
using System.Collections.Generic;
using Paridhwani.AssessmentExample;
namespace Paridhwani {
 public sealed partial class TeaStallSession {
 ITeaAssessmentService teaAssessmentService;
 readonly DialogueTextRequests teaAssessmentRequests=new DialogueTextRequests();
 public string TeaAssessmentInput {get;set;}="";
 public string TeaAssessmentFeedback {get;private set;}="";
 public bool TeaAssessmentBusy=>teaAssessmentRequests.Busy;
 public bool CanAssessTea=>teaAssessmentService!=null&&Provider is LocalTeaConversation&&Turn?.Id=="order"&&Progress.greeted;
 public void SetTeaAssessmentService(ITeaAssessmentService service) {
 teaAssessmentRequests.Cancel();teaAssessmentService=service;TeaAssessmentFeedback="";
 if(ui&&ui.Mode=="conversation")ui.ShowConversation();
 }
 void ResetTeaAssessment(){teaAssessmentRequests.Cancel();TeaAssessmentInput="";TeaAssessmentFeedback="";}
 public void CancelTeaAssessment(){teaAssessmentRequests.Cancel();TeaAssessmentFeedback="You can still use the replies below.";if(ui&&ui.Mode=="conversation")ui.ShowConversation();}
 public void TeaAssessmentHint(){teaAssessmentRequests.Cancel();TeaAssessmentFeedback="Nanage means to me; beku means want. Try Nanage chaha beku or Nanage neeru beku.";ui.ShowConversation();}
 public async void AskTeaAssessment() {
 if(!CanAssessTea||ui.Mode!="conversation")return;
 if(!TeaAssessmentContract.RomanInput(TeaAssessmentInput)){TeaAssessmentFeedback="Type Romanised Kannada, such as Nanage chaha beku. The replies below remain available.";ui.ShowConversation();return;}
 textRequests.Cancel();var turn=Turn;var npc=ActiveNpc;var service=teaAssessmentService;string input=TeaAssessmentInput,id=Guid.NewGuid().ToString("N");AssessmentResult assessment=null;
 var pending=teaAssessmentRequests.Send(async token=>{
 var bytes=await service.ReplyAsync(id,input,token).ConfigureAwait(false);token.ThrowIfCancellationRequested();
 assessment=AssessmentConsumer.Read(bytes,id,"q1_2","q1_2_a",TeaAssessmentContract.Revision,new HashSet<string>{"a_tea","e_beda","e_nimage"});
 return new TeaLine("","","Assessment received.");});
 TeaAssessmentFeedback="Thinking...";ui.ShowConversation();var reply=await pending;
 if(!this||Turn!=turn||ActiveNpc!=npc||ui.Mode!="conversation"||!ReferenceEquals(service,teaAssessmentService)||!teaAssessmentRequests.IsCurrent(reply))return;
 if(reply.Status!=DialogueTextStatus.Ready){TeaAssessmentFeedback="A reply is unavailable. Please use an authored reply below or try again.";ui.ShowConversation();return;}
 if(TeaAssessmentContract.Choice(Turn,input,assessment,out int choice,out string feedback))Choose(choice);
 TeaAssessmentFeedback=feedback;if(ui.Mode=="conversation")ui.ShowConversation();
 }
 }
}
