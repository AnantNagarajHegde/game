using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paridhwani;

static class DialogueHttpChecks
{
    static int assertions;
    static void Check(bool condition,string message) {if(!condition)throw new Exception(message);assertions++;}
    static DialoguePrompt Prompt(string input="Synthetic greeting")=>new DialoguePrompt("Synthetic speaker",
        new TeaTurn("test-turn","","Namaskara.","Hello.",new TeaChoice("","","Hello.","next","quest-effect-must-stay-local")),input);
    static async Task Failure(Func<Task> action,DialogueTransportFailure expected)
    {
        try {await action();throw new Exception("Expected "+expected);}
        catch(DialogueTransportException e) {Check(e.Failure==expected,"Failure code "+expected+", got "+e.Failure);Check(e.InnerException==null && e.Message=="Dialogue transport: "+expected,"sanitized error");}
    }
    static async Task Run(bool sockets)
    {
        DialogueContractFixtures.Run(Check);
        foreach(string url in new[]{"https://example.com/v1/dialogue/respond","http://localhost:8765/v1/dialogue/respond",
            "http://0.0.0.0:8765/v1/dialogue/respond","http://127.0.0.1:8765/wrong","http://127.0.0.1:8765/v1/dialogue/respond?token=secret",
            "http://user:secret@127.0.0.1:8765/v1/dialogue/respond","http://127.0.0.1:8765/v1/dialogue/respond#x","http://[::1]:8765/v1/dialogue/respond"})
        {
            bool rejected=false;try {using(var service=new LoopbackDialogueTextService(true,new Uri(url))) {}}catch(ArgumentException){rejected=true;}
            Check(rejected,"endpoint policy rejects "+url);
        }
        bool optIn=false;try {using(var service=new LoopbackDialogueTextService(false,new Uri("http://127.0.0.1:8765"+DialogueHttpContract.Route))) {}}catch(InvalidOperationException){optIn=true;}
        Check(optIn,"explicit networking opt-in required");
        var request=new DialogueServiceRequest(Prompt("Kannada: ನಮಸ್ಕಾರ 😀"));
        byte[] encoded=DialogueHttpContract.Encode(request);
        using(var json=JsonDocument.Parse(encoded))
        {
            var root=json.RootElement;
            Check(root.GetProperty("learner_text").GetString()==request.LearnerText,"Unicode request round trip");
            Check(root.EnumerateObject().Count()==7,"only seven approved context fields");
            Check(root.GetProperty("available_replies")[0].EnumerateObject().Count()==3,"choices contain text only");
            Check(!Encoding.UTF8.GetString(encoded).Contains("quest-effect"),"authored effects excluded");
        }
        var bounded=new DialogueServiceRequest(Prompt(new string('x',511)+"😀"));
        Check(bounded.LearnerText.Length==511,"input bound preserves text element");
        var huge=new TeaTurn("test","","",new string('x',4097));
        await Failure(()=>Task.FromResult(DialogueHttpContract.Encode(new DialogueServiceRequest(new DialoguePrompt("NPC",huge,"test")))),DialogueTransportFailure.InvalidContract);
        var choices=new TeaTurn("test","","","hello");for(int i=0;i<33;i++)choices.Choices.Add(new TeaChoice("","","hello","next"));
        await Failure(()=>Task.FromResult(DialogueHttpContract.Encode(new DialogueServiceRequest(new DialoguePrompt("NPC",choices,"test")))),DialogueTransportFailure.InvalidContract);
        var large=new TeaTurn("test","","","hello");for(int i=0;i<20;i++)large.Choices.Add(new TeaChoice("","",new string('x',4000),"next"));
        await Failure(()=>Task.FromResult(DialogueHttpContract.Encode(new DialogueServiceRequest(new DialoguePrompt("NPC",large,"test")))),DialogueTransportFailure.BodyTooLarge);
        using(var mock=new NlpTestFixture(sockets))
        using(var service=mock.Service())
        {
            var direct=await service.ReplyAsync(request,CancellationToken.None);
            Check(direct.Line.Kannada=="ನಮಸ್ಕಾರ","direct HTTP mapping success");
            var handler=DialogueServiceAdapter.Bind(service);
            using(var requests=new DialogueTextRequests())
            {
                var success=await requests.Send(c=>handler(Prompt(),c));
                Check(success.Status==DialogueTextStatus.Ready && success.Line.Kannada=="ನಮಸ್ಕಾರ","HTTP mapped through adapter and coordinator");
                foreach(string mode in new[]{"wrong-id","wrong-turn","wrong-version","typed-version","missing","null","empty","text-limit","null-text","command","duplicate","malformed","invalid-utf8","oversize","stream-oversize","redirect","unauthorized","forbidden","limited","error","content-type","encoding"})
                {
                    int before=mock.Requests;
                    var result=await requests.Send(c=>handler(Prompt("mock:"+mode),c));
                    Check(result.Status==DialogueTextStatus.Unavailable,"HTTP fallback: "+mode);
                    Check(mock.Requests==before+1,"no retries/redirect follow: "+mode);
                }
                var timed=await requests.Send(c=>handler(Prompt("mock:delay"),c),40);
                Check(timed.Status==DialogueTextStatus.TimedOut && !requests.Busy,"coordinator deadline preserves fallback");
                var old=requests.Send(c=>handler(Prompt("mock:delay"),c));
                var current=await requests.Send(c=>handler(Prompt(),c));
                Check((await old).Status==DialogueTextStatus.Superseded && current.Status==DialogueTextStatus.Ready && requests.IsCurrent(current),"superseded concurrent HTTP request cannot replace current");
            }
            using(var cancelled=new CancellationTokenSource())
            {
                cancelled.Cancel();int before=mock.Requests;
                try {await service.ReplyAsync(request,cancelled.Token);throw new Exception("Expected pre-cancellation");}catch(OperationCanceledException) {Check(mock.Requests==before,"pre-cancelled request does not send");}
            }
            using(var cancellation=new CancellationTokenSource())
            {
                var work=service.ReplyAsync(new DialogueServiceRequest(Prompt("mock:slow-body")),cancellation.Token);
                await Task.Delay(40);await Task.Run(()=>cancellation.Cancel());
                try {await work;throw new Exception("Expected worker-thread cancellation");}catch(OperationCanceledException) {Check(true,"body-read cancellation from worker thread");}
            }
            using(var shortService=mock.Service(60))
            {
                await Failure(()=>shortService.ReplyAsync(new DialogueServiceRequest(Prompt("mock:delay")),CancellationToken.None),DialogueTransportFailure.Timeout);
                await Failure(()=>shortService.ReplyAsync(new DialogueServiceRequest(Prompt("mock:slow-body")),CancellationToken.None),DialogueTransportFailure.Timeout);
            }
            var parallel=await Task.WhenAll(service.ReplyAsync(request,CancellationToken.None),service.ReplyAsync(new DialogueServiceRequest(Prompt()),CancellationToken.None));
            Check(parallel[0].RequestId!=parallel[1].RequestId,"parallel callers retain independent correlation");
            using(var cancelOne=new CancellationTokenSource())
            {
                var cancelledCall=service.ReplyAsync(new DialogueServiceRequest(Prompt("mock:delay")),cancelOne.Token);
                var survivingRequest=new DialogueServiceRequest(Prompt("mock:delay"));
                var survivingCall=service.ReplyAsync(survivingRequest,CancellationToken.None);
                cancelOne.Cancel();
                try {await cancelledCall;throw new Exception("Expected one caller cancellation");}
                catch(OperationCanceledException) {Check(true,"one shared-service caller cancels independently");}
                Check((await survivingCall).RequestId==survivingRequest.RequestId,"cancelling one caller preserves a concurrent shared-service request");
            }
            var disposable=mock.Service();
            var pending=disposable.ReplyAsync(new DialogueServiceRequest(Prompt("mock:delay")),CancellationToken.None);
            await Task.Delay(30);disposable.Dispose();disposable.Dispose();
            await Failure(()=>pending,DialogueTransportFailure.Disposed);
            await Failure(()=>disposable.ReplyAsync(request,CancellationToken.None),DialogueTransportFailure.Disposed);
        }
        using(var offline=NlpTestFixture.Disconnected())
        {
            await Failure(()=>offline.ReplyAsync(request,CancellationToken.None),DialogueTransportFailure.Connectivity);
            using(var requests=new DialogueTextRequests())
            {var result=await requests.Send(c=>DialogueServiceAdapter.Bind(offline)(Prompt(),c));Check(result.Status==DialogueTextStatus.Unavailable,"connectivity failure preserves authored fallback");}
        }
        Console.WriteLine("NLP_HTTP_CHECKS_OK: "+assertions+" assertions; "+(sockets?"actual loopback sockets":"in-memory HTTP handler (no sockets)")+"; synthetic context only.");
    }
    public static int Main(string[] args)
    {
        try
        {
            if(args.Length>0 && args[0]=="--check-response")
            {
                if(args.Length!=4)throw new ArgumentException("Use --check-response <request-id> <turn-id> <response-file>.");
                DialogueHttpContract.Decode(DialogueContractFixtures.ReadBounded(args[3]),args[1],args[2]);
                Console.WriteLine("NLP_RESPONSE_CONFORMS: v1 response and correlation accepted; no sockets.");return 0;
            }
            if(args.Length>0 && args[0]=="--serve")
            {
                using(var mock=new NlpMockServer(args.Length>1?int.Parse(args[1]):8765))
                using(var quit=new ManualResetEventSlim())
                {
                    Console.CancelKeyPress+=(s,e)=>{e.Cancel=true;quit.Set();};
                    Console.WriteLine("NLP_MOCK_READY "+mock.Endpoint+" (synthetic replies; Ctrl+C to stop; payload logging disabled)");
                    quit.Wait();return 0;
                }
            }
            Run(args.Length>0 && args[0]=="--network").GetAwaiter().GetResult();return 0;
        }
        catch(DialogueTransportException e) when(args.Length>0 && args[0]=="--check-response")
        {Console.Error.WriteLine("NLP_RESPONSE_REJECTED: "+e.Failure);return 1;}
        catch(System.IO.IOException) when(args.Length>0 && args[0]=="--check-response")
        {Console.Error.WriteLine("NLP_RESPONSE_REJECTED: FileUnavailable");return 1;}
        catch(UnauthorizedAccessException) when(args.Length>0 && args[0]=="--check-response")
        {Console.Error.WriteLine("NLP_RESPONSE_REJECTED: FileUnavailable");return 1;}
        catch(Exception e) {Console.Error.WriteLine("NLP_HTTP_CHECKS_FAILED: "+e.Message);return 1;}
    }
}
