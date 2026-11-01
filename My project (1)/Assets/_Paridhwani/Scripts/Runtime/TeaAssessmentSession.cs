using System;
using System.Collections.Generic;
using Paridhwani.AssessmentExample;
namespace Paridhwani {
 public sealed partial class TeaStallSession {
 ITeaAssessmentService teaAssessmentService;
 LoopbackTeaAssessmentService ownedTeaAssessmentService;
 readonly DialogueTextRequests teaAssessmentRequests=new DialogueTextRequests();
 public string TeaAssessmentInput {get;set;}="";
 public string TeaAssessmentFeedback {get;private set;}="";
 public bool TeaAssessmentBusy=>teaAssessmentRequests.Busy;
 public bool ShowTeaAssessment=>Turn!=null;
 public bool CanAssessTea=>TeaAssessmentContract.PracticeAvailable(Provider,Turn,Progress);
 TeaLine teaTopicLine;
 public void AskTeaConversation() {
 AskVillageModel();
 }
 public void SetTeaAssessmentService(ITeaAssessmentService service) {
 teaAssessmentRequests.Cancel();if(!ReferenceEquals(ownedTeaAssessmentService,service))DisposeOwnedTeaAssessment();teaAssessmentService=service;TeaAssessmentFeedback="";
 if(ui&&ui.Mode=="conversation")ui.ShowConversation();
 }
 void DisposeOwnedTeaAssessment(){ownedTeaAssessmentService?.Dispose();ownedTeaAssessmentService=null;}
 void ResetTeaAssessment(){teaAssessmentRequests.Cancel();TeaAssessmentInput="";TeaAssessmentFeedback="";teaTopicLine=null;}
 public void CancelTeaAssessment(){teaAssessmentRequests.Cancel();TeaAssessmentFeedback="Reply cancelled. You can send your message again.";if(ui&&ui.Mode=="conversation")ui.ShowConversation();}
 public void TeaAssessmentHint(){teaAssessmentRequests.Cancel();TeaAssessmentFeedback="You may write in Kannada, Romanised Kannada, or English.";ui.ShowConversation();}
 public async void AskTeaAssessment() {
 if(!CanAssessTea||ui.Mode!="conversation")return;
 if(!TeaAssessmentContract.RomanInput(TeaAssessmentInput)){TeaAssessmentFeedback="Type Romanised Kannada, such as Nanage chaha beku. For an unscored story reply, type tea please or water please.";ui.ShowConversation();return;}
 if(teaAssessmentService==null){
 try {ownedTeaAssessmentService=new LoopbackTeaAssessmentService(true,new Uri("http://127.0.0.1:8766"+TeaAssessmentContract.Route));teaAssessmentService=ownedTeaAssessmentService;}
 catch(Exception){TeaAssessmentFeedback="Language practice is unavailable. Type tea please or water please for an unscored story reply, or ask about Ananya.";ui.ShowConversation();return;}
 }
 textRequests.Cancel();var turn=Turn;var provider=Provider;var npc=ActiveNpc;var service=teaAssessmentService;string input=TeaAssessmentInput,id=Guid.NewGuid().ToString("N");AssessmentResult assessment=null;
 var pending=teaAssessmentRequests.Send(async token=>{
 var bytes=await service.ReplyAsync(id,input,token).ConfigureAwait(false);token.ThrowIfCancellationRequested();
 assessment=AssessmentConsumer.Read(bytes,id,"q1_2","q1_2_a",TeaAssessmentContract.Revision,new HashSet<string>{"a_tea","e_beda","e_nimage"});
 return new TeaLine("","","Assessment received.");});
 TeaAssessmentFeedback="Thinking...";ui.ShowConversation();var reply=await pending;
 if(!this||Turn!=turn||ActiveNpc!=npc||ui.Mode!="conversation"||!ReferenceEquals(service,teaAssessmentService)||!teaAssessmentRequests.IsCurrent(reply))return;
 if(!ReferenceEquals(provider,Provider)){TeaAssessmentFeedback="Conversation changed. Please ask again.";ui.ShowConversation();return;}
 if(reply.Status!=DialogueTextStatus.Ready){TeaAssessmentFeedback="Language practice is unavailable. Try again, or type tea please or water please for an unscored story reply. You can still ask about Ananya or the library.";ui.ShowConversation();return;}
 if(TeaAssessmentContract.Choice(Turn,input,assessment,out int choice,out string feedback))Choose(choice);
 TeaAssessmentFeedback=feedback;if(ui.Mode=="conversation")ui.ShowConversation();
 }
 }
}
