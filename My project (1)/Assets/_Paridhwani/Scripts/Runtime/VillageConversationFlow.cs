using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Paridhwani
{
    public sealed class VillageConversationResult
    {
        public DialogueTextStatus Status=>Receipt.Status;
        public DialogueFailure Failure=>Receipt.Failure;
        public int HttpStatus=>Receipt.HttpStatus;
        public string DiagnosticType=>Receipt.DiagnosticType;
        public readonly bool ActionRejected;
        public readonly string ActionId;
        public readonly TeaLine Line;
        internal readonly DialogueTextResult Receipt;
        internal VillageConversationResult(DialogueTextResult receipt, string actionId,bool actionRejected=false)
        { Receipt=receipt; ActionId=actionId; Line=receipt.Line; ActionRejected=actionRejected; }
    }

    // Owns conversation request state, independently of Unity UI, activities and story mutation.
    public sealed class VillageConversationFlow : IDisposable
    {
        // Health check (4s) plus the backend's total model budget (45s), with transport margin.
        public const int ReplyTimeoutMs=60000;
        readonly DialogueTextRequests requests=new DialogueTextRequests();
        readonly Dictionary<string,List<VillageConversationExchange>> history=new Dictionary<string,List<VillageConversationExchange>>();
        public string Input { get; set; }="";
        public string Feedback { get; private set; }="";
        public TeaLine Line { get; set; }
        public bool PhrasesVisible { get; private set; }
        public bool Busy=>requests.Busy;
        public string DisplayMessageId {get;private set;}="";
        string retryInput,retrySpeaker,retryTurn,retryMessageId;
        bool retryable;
        public bool IsCurrent(VillageConversationResult result)=>result!=null && requests.IsCurrent(result.Receipt);
        public VillageConversationExchange[] History(string speaker)=>history.TryGetValue(speaker,out var exchanges)?exchanges.ToArray():Array.Empty<VillageConversationExchange>();

        public void ResetTurn()
        { requests.Cancel(); Input=""; Feedback=""; Line=null; PhrasesVisible=false; retryable=false; DisplayMessageId=""; }
        public void ReceiveStreamingText(TeaLine line,string input) {
            Line=line; Feedback=""; PhrasesVisible=false;
            if((Input??"").Trim()==input)Input="";
        }
        public void ResetVisit() { ResetTurn(); history.Clear(); }
        public void Cancel()
        { requests.Cancel(); Feedback="Reply cancelled. Try your message again or choose a phrase."; }
        public void Suspend()
        { bool waiting=Busy; requests.Cancel(); if(waiting)Feedback="Reply cancelled. Try your message again or choose a phrase."; }
        public void TogglePhrases()
        { if(Busy)Cancel(); PhrasesVisible=!PhrasesVisible; }

        public async Task<VillageConversationResult> Send(VillageConversationRequest request,
            Func<VillageConversationRequest,CancellationToken,Task<VillageConversationReply>> transport=null,
            int timeoutMs=ReplyTimeoutMs)
        {
            VillageConversationReply reply=null;
            DisplayMessageId=retryable && request.Input==retryInput && request.Speaker==retrySpeaker && request.TurnId==retryTurn?retryMessageId:request.Id;
            retryInput=request.Input;retrySpeaker=request.Speaker;retryTurn=request.TurnId;retryMessageId=DisplayMessageId;retryable=true;
            PhrasesVisible=false;
            Feedback="Thinking… You can choose a phrase while you wait.";
            var receipt=await requests.Send(async token=> {
                reply=VillageConversationClient.NormalizeReply(request,await (transport??VillageConversationClient.Send)(request,token));
                return reply.Line;
            },timeoutMs);
            var result=new VillageConversationResult(receipt,receipt.Status==DialogueTextStatus.Ready?reply.ActionId:"none",reply?.ActionRejected??false);
            if(!IsCurrent(result)) return result;
            if(result.Status!=DialogueTextStatus.Ready)
            {
                Feedback=result.Status==DialogueTextStatus.TimedOut?FailureMessage(DialogueFailure.Timeout):
                    result.Status==DialogueTextStatus.Cancelled?"Reply cancelled. Your message is saved; retry it or choose a phrase.":FailureMessage(result.Failure);
                PhrasesVisible=true;
                return result;
            }
            Line=result.Line;
            retryable=false;
            PhrasesVisible=result.ActionId=="none" && request.Actions.Count>0;
            Feedback=result.ActionRejected?"The reply arrived, but task progress could not be verified. You can keep chatting or choose a phrase.":
                PhrasesVisible?"To move this conversation forward, try a phrase below. You can also keep chatting.":"";
            if((Input??"").Trim()==request.Input)Input="";
            Remember(request.Speaker,request.Input,VillageConversationClient.ContextText(result.Line));
            return result;
        }

        public static string FailureMessage(DialogueFailure failure)
        {
            string reason;
            switch(failure) {
                case DialogueFailure.Connection:reason="The conversation service could not be reached. Check that it is running and that its address is correct.";break;
                case DialogueFailure.Configuration:reason="The conversation connection or model settings need attention. Check the backend setup.";break;
                case DialogueFailure.ServiceAuthentication:reason="The conversation service rejected its access credentials. Check the service settings.";break;
                case DialogueFailure.ProviderAuthentication:reason="The model provider rejected the API key. Check the backend key and restart the conversation service.";break;
                case DialogueFailure.ProviderAccessDenied:reason="The model provider denied access. Check the selected model and account permissions.";break;
                case DialogueFailure.ProviderRequestBlocked:reason="The model provider blocked the backend request. Check the backend connection and provider access.";break;
                case DialogueFailure.RateLimited:reason="The model provider's usage limit has been reached. Wait before retrying.";break;
                case DialogueFailure.Busy:reason="The conversation service is busy. Wait a moment before retrying.";break;
                case DialogueFailure.ProviderRejected:reason="The model provider rejected the request. Check the backend's model settings.";break;
                case DialogueFailure.ProviderUnavailable:reason="The model provider is temporarily unavailable. Retry shortly.";break;
                case DialogueFailure.InvalidRequest:reason="The game could not prepare a valid conversation request.";break;
                case DialogueFailure.InvalidResponse:reason="A reply arrived in an unreadable format.";break;
                case DialogueFailure.Timeout:reason="The reply is taking too long.";break;
                default:reason="Chat could not complete this request. Check the connection diagnostics.";break;
            }
            return reason+" Your message is saved; you can retry or choose a phrase.";
        }

        public void PreparationFailed() { Feedback=FailureMessage(DialogueFailure.InvalidRequest);PhrasesVisible=true; }

        public void Remember(string speaker, string player, string npc)
        {
            if(!history.TryGetValue(speaker,out var exchanges))history[speaker]=exchanges=new List<VillageConversationExchange>();
            exchanges.Add(new VillageConversationExchange(player,npc));
            if(exchanges.Count>6)exchanges.RemoveAt(0);
        }
        public void Dispose()=>requests.Dispose();
    }
}
