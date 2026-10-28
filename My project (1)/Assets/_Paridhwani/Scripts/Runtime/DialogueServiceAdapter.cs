using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paridhwani
{
    // In-process Unity contract. A transport maps the real engine's agreed wire schema to these types.
    public interface IDialogueTextService
    {
        Task<DialogueServiceReply> ReplyAsync(DialogueServiceRequest request,CancellationToken cancellation);
    }

    public sealed class DialogueServiceText
    {
        public readonly string Kannada,Roman,English;
        public DialogueServiceText(string kannada,string roman,string english)
        {Kannada=kannada??"";Roman=roman??"";English=english??"";}
        internal static DialogueServiceText Copy(TeaLine line) => new DialogueServiceText(line.Kannada,line.Roman,line.English);
    }

    public sealed class DialogueServiceRequest
    {
        public const int CurrentVersion=1;
        public readonly int Version=CurrentVersion;
        public readonly string RequestId,TurnId,Speaker,LearnerText;
        public readonly DialogueServiceText AuthoredLine;
        public readonly IReadOnlyList<DialogueServiceText> AvailableReplies;
        public DialogueServiceRequest(DialoguePrompt prompt)
        {
            if(prompt==null)throw new ArgumentNullException(nameof(prompt));
            RequestId=Guid.NewGuid().ToString("N");TurnId=prompt.TurnId;Speaker=prompt.Speaker;LearnerText=prompt.LearnerText;
            AuthoredLine=DialogueServiceText.Copy(prompt.AuthoredLine);
            var replies=new DialogueServiceText[prompt.AvailableReplies.Length];
            for(int i=0;i<replies.Length;i++)replies[i]=DialogueServiceText.Copy(prompt.AvailableReplies[i]);
            AvailableReplies=Array.AsReadOnly(replies);
        }
    }

    public sealed class DialogueServiceReply
    {
        public readonly int Version;
        public readonly string RequestId,TurnId;
        public readonly DialogueServiceText Line;
        public DialogueServiceReply(int version,string requestId,string turnId,DialogueServiceText line)
        {Version=version;RequestId=requestId;TurnId=turnId;Line=line;}
    }

    public static class DialogueServiceAdapter
    {
        public static Func<DialoguePrompt,CancellationToken,Task<TeaLine>> Bind(IDialogueTextService service)
        {
            if(service==null)throw new ArgumentNullException(nameof(service));
            return (prompt,cancellation)=>Send(service,prompt,cancellation);
        }
        static async Task<TeaLine> Send(IDialogueTextService service,DialoguePrompt prompt,CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            var request=new DialogueServiceRequest(prompt);
            var reply=await service.ReplyAsync(request,cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if(reply==null || reply.Version!=request.Version ||
                !string.Equals(reply.RequestId,request.RequestId,StringComparison.Ordinal) ||
                !string.Equals(reply.TurnId,request.TurnId,StringComparison.Ordinal))
                throw new InvalidOperationException("The dialogue service returned an incompatible or unrelated reply.");
            var line=reply.Line==null?null:DialogueTextRequests.Snapshot(new TeaLine(reply.Line.Kannada,reply.Line.Roman,reply.Line.English));
            if(line==null)throw new InvalidOperationException("The dialogue service returned empty or oversized text.");
            return line;
        }
    }
}
