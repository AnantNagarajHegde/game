using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Net.Http;
using Paridhwani;

public static class ConversationResilienceChecks
{
    static VillageConversationRequest Request(string input="Namaskara")=>new VillageConversationRequest(
        "hello","Manjunatha","Tea vendor",input,"Introduce yourself","Welcome the visitor",
        new[]{new VillageConversationAction("a0","Greet","Offer tea or water",0)},
        Array.Empty<VillageConversationExchange>());
    static VillageConversationReply Reply(string action="a0",string text="Would you like tea or water?")=>
        new VillageConversationReply(action,new TeaLine("","",text));

    public static void Run(Action<bool,string> check)
    {
        using(var flow=new VillageConversationFlow())
        {
            var original=new TeaLine("","","Welcome to the tea stall.");flow.Line=original;flow.Input="Namaskara";
            var failure=flow.Send(Request(),(r,t)=>throw new InvalidOperationException("Offline")).GetAwaiter().GetResult();
            string failedBubble=flow.DisplayMessageId;
            check(failure.Status==DialogueTextStatus.Unavailable && flow.PhrasesVisible && !flow.Busy,"service failure offers phrases and releases the composer");
            check(failure.DiagnosticType=="InvalidOperationException" && !flow.Feedback.Contains("Offline"),"unexpected failures retain safe type metadata without exception messages");
            check(flow.Input=="Namaskara" && flow.Line==original && flow.History("Manjunatha").Length==0,"failure preserves the draft and authored line without adding invented history");
            var retry=flow.Send(Request(),(r,t)=>Task.FromResult(Reply())).GetAwaiter().GetResult();
            check(flow.DisplayMessageId==failedBubble,"retrying the same failed message reuses its visible message identity");
            check(retry.Status==DialogueTextStatus.Ready && flow.IsCurrent(retry) && retry.ActionId=="a0","a retry can accept a currently eligible action");
            check(flow.Input=="" && !flow.PhrasesVisible && flow.History("Manjunatha").Length==1,"accepted reply clears only its draft and records one exchange");
            flow.Send(Request(),(r,t)=>Task.FromResult(Reply())).GetAwaiter().GetResult();
            check(flow.DisplayMessageId!=failedBubble,"repeating a successfully delivered message creates a new visible message");
            flow.Cancel();check(!flow.IsCurrent(retry),"cancelling rejects even a completed response queued for story presentation");
        }
        using(var flow=new VillageConversationFlow())
        {
            flow.Input="My own wording";CancellationToken transportToken=default;
            var never=new TaskCompletionSource<VillageConversationReply>();
            var result=flow.Send(Request("My own wording"),(r,t)=>{transportToken=t;return never.Task;},10).GetAwaiter().GetResult();
            check(result.Status==DialogueTextStatus.TimedOut && transportToken.IsCancellationRequested && !flow.Busy,"a noncooperative model cannot keep the composer busy after the deadline");
            check(flow.PhrasesVisible && flow.Input=="My own wording" && flow.Feedback.Contains("taking too long"),"timeout retains the draft and explains how to continue");
            never.SetResult(Reply());
            check(flow.Line==null && flow.History("Manjunatha").Length==0,"late timed-out reply cannot replace local dialogue or add history");
        }
        using(var flow=new VillageConversationFlow())
        {
            flow.Input="Something else";
            var result=flow.Send(Request("Something else"),(r,t)=>Task.FromResult(Reply("none","Tell me more."))).GetAwaiter().GetResult();
            check(result.Status==DialogueTextStatus.Ready && result.ActionId=="none" && flow.PhrasesVisible,"unmatched free text remains chat and exposes supported phrases");
            check(flow.Line.English=="Tell me more." && flow.Feedback.Contains("keep chatting"),"unmatched reply does not imply that the learner completed the task");
        }
        using(var flow=new VillageConversationFlow())
        {
            flow.Input="Namaskara";
            foreach(var invalid in new[]{Reply("none",new string('x',1001)),
                new VillageConversationReply("a0",new TeaLine(new string('ಕ',1001),"","Hello"))})
            {
                var result=flow.Send(Request(),(r,t)=>Task.FromResult(invalid)).GetAwaiter().GetResult();
                check(result.Status==DialogueTextStatus.Unavailable && flow.PhrasesVisible && flow.Line==null,"invalid or ineligible model output falls back without presenting a story effect");
            }
            check(flow.Input=="Namaskara" && flow.History("Manjunatha").Length==0,"malformed replies do not consume the learner's message");
        }
        using(var flow=new VillageConversationFlow())
        {
            foreach(var action in new[]{"a99",null}) {
                var result=flow.Send(Request(),(r,t)=>Task.FromResult(Reply(action))).GetAwaiter().GetResult();
                check(result.Status==DialogueTextStatus.Ready && result.ActionId=="none" && result.ActionRejected,
                    "unverified action metadata cannot discard a readable reply or advance a task");
            }
            var translated=flow.Send(Request(),(r,t)=>Task.FromResult(new VillageConversationReply("none",new TeaLine("","Namaskara","")))).GetAwaiter().GetResult();
            check(translated.Status==DialogueTextStatus.Ready && flow.History("Manjunatha")[2].Npc=="Namaskara",
                "Romanised-only replies can be displayed and remembered without an English translation");
            check(translated.Line.DisplayFallback(false,true)=="Namaskara","missing English cannot leave a Romanised-only reply blank when only English aid is enabled");
            check(translated.Line.DisplayFallback(true,false)=="","already visible Romanised text is not duplicated by fallback rendering");
        }
        using(var flow=new VillageConversationFlow())
        {
            var delayed=new TaskCompletionSource<VillageConversationReply>();flow.Input="Namaskara";
            var pending=flow.Send(Request(),(r,t)=>delayed.Task);
            flow.TogglePhrases();
            check(!flow.Busy && flow.PhrasesVisible && flow.Input=="Namaskara","opening phrases during a request cancels it immediately without losing the draft");
            flow.ResetTurn();delayed.SetResult(Reply());var old=pending.GetAwaiter().GetResult();
            check(!flow.IsCurrent(old) && flow.Line==null && flow.History("Manjunatha").Length==0,"a late AI reply cannot overwrite a turn reached through an authored phrase");
        }
        using(var flow=new VillageConversationFlow())
        {
            var delayed=new TaskCompletionSource<VillageConversationReply>();flow.Input="Namaskara";
            var pending=flow.Send(Request(),(r,t)=>delayed.Task);flow.Input="A newer draft";delayed.SetResult(Reply());
            pending.GetAwaiter().GetResult();
            check(flow.Input=="A newer draft","completion preserves text edited after the request snapshot");
            for(int i=0;i<8;i++)flow.Remember("Manjunatha","Player "+i,"NPC "+i);
            check(flow.History("Manjunatha").Length==6 && flow.History("Manjunatha")[0].Player=="Player 2" && flow.History("Somanna").Length==0,"conversation history is bounded and separate per NPC");
            flow.ResetVisit();
            check(flow.History("Manjunatha").Length==0 && flow.Input=="" && !flow.PhrasesVisible,"practice, demo, load and new-story boundaries can clear all transient conversation state");
        }
        var actions=new[]{new VillageConversationAction("a0","Greet","Offer tea",0)};
        var history=new[]{new VillageConversationExchange("Hello","Welcome")};
        var snapshot=new VillageConversationRequest("hello","Manjunatha","Tea vendor","Hi","Greet","Welcome",actions,history);
        actions[0]=new VillageConversationAction("a99","Other","Other",99);history[0]=new VillageConversationExchange("Changed","Changed");
        check(snapshot.Actions[0].Id=="a0" && snapshot.History[0].Player=="Hello","a request owns detached action and history collections");

        OfflineIntroduction(check);
        ChapterConsistency(check);
        ProtocolChecks(check);
        TransportChecks(check).GetAwaiter().GetResult();
    }

    sealed class FakeHttp : HttpMessageHandler
    {
        public Func<HttpRequestMessage,Task<HttpResponseMessage>> Reply;
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message,CancellationToken token)
        {token.ThrowIfCancellationRequested();Requests++;return Reply(message);}
    }
    static HttpResponseMessage Http(int status,string json)=>new HttpResponseMessage((HttpStatusCode)status)
        {Content=new StringContent(json,Encoding.UTF8,"application/json")};
    const string Healthy="{\"version\":2,\"provider\":\"groq\",\"model_configured\":true,\"speech_configured\":false,\"authentication_required\":false}";

    static async Task TransportChecks(Action<bool,string> check)
    {
        var connection=new VillageServiceConnection("http://127.0.0.1:8767/","");
        using(var fake=new FakeHttp())using(var http=new HttpClient(fake)) {
            fake.Reply=async message=> {
                if(message.Method==HttpMethod.Get) {
                    check(message.RequestUri.AbsolutePath=="/health","game checks the health route at its configured service address");
                    return Http(200,Healthy);
                }
                check(message.RequestUri.AbsolutePath=="/v2/village/turn","game sends to the current v2 turn route");
                using(var doc=JsonDocument.Parse(await message.Content.ReadAsStringAsync())) {
                    var root=doc.RootElement;
                    return Http(200,JsonSerializer.Serialize(new{version=2,request_id=root.GetProperty("request_id").GetString(),
                        turn_id=root.GetProperty("turn_id").GetString(),action_id="a99",kannada="",roman="Namaskara",english=""}));
                }
            };
            var reply=await VillageConversationClient.Send(Request(),connection,http,CancellationToken.None);
            check(fake.Requests==2 && reply.Line.Roman=="Namaskara" && reply.ActionId=="none" && reply.ActionRejected,
                "real HTTP client path retains readable text while suppressing unknown actions");
        }
        foreach(var pair in new (string code,DialogueFailure kind)[]{("model_authentication_failed",DialogueFailure.ProviderAuthentication),
            ("model_access_denied",DialogueFailure.ProviderAccessDenied),("model_request_blocked",DialogueFailure.ProviderRequestBlocked),
            ("model_rate_limited",DialogueFailure.RateLimited),("model_request_rejected",DialogueFailure.ProviderRejected),
            ("model_timed_out",DialogueFailure.Timeout),("invalid_model_response",DialogueFailure.InvalidResponse)}) {
            using(var fake=new FakeHttp())using(var http=new HttpClient(fake))using(var flow=new VillageConversationFlow()) {
                fake.Reply=message=>Task.FromResult(message.Method==HttpMethod.Get?Http(200,Healthy):
                    Http(503,JsonSerializer.Serialize(new{error=pair.code,detail="synthetic-private-key"})));
                flow.Input="Namaskara";
                var result=await flow.Send(Request(),(r,t)=>VillageConversationClient.Send(r,connection,http,t));
                check(result.Failure==pair.kind && result.HttpStatus==503 && flow.Input=="Namaskara" && flow.Line==null,
                    "provider failure survives HTTP and coordination without consuming the draft: "+pair.code);
                check(!flow.Feedback.Contains("synthetic-private-key") && !flow.Feedback.Contains("Chat is unavailable"),
                    "feedback uses a specific allowlisted reason without copying provider data");
            }
        }
        check(VillageConversationFlow.FailureMessage(DialogueFailure.ProviderAccessDenied).Contains("account permissions") &&
            VillageConversationFlow.FailureMessage(DialogueFailure.ProviderRequestBlocked).Contains("blocked the backend request"),
            "provider permissions and request blocks have separate recovery guidance from a rejected API key");
        foreach(var health in new[]{Healthy.Replace("\"model_configured\":true","\"model_configured\":false"),Healthy.Replace("\"version\":2","\"version\":1")}) {
            using(var fake=new FakeHttp())using(var http=new HttpClient(fake)) {
                fake.Reply=message=>Task.FromResult(Http(200,health));
                try {await VillageConversationClient.Send(Request(),connection,http,CancellationToken.None);check(false,"unconfigured health should fail");}
                catch(DialogueRequestException error) {check(error.Failure==DialogueFailure.Configuration && fake.Requests==1,
                    "incompatible or unconfigured service is detected before any model POST");}
            }
        }
        using(var fake=new FakeHttp())using(var http=new HttpClient(fake))using(var flow=new VillageConversationFlow()) {
            fake.Reply=message=>throw new HttpRequestException("synthetic-private-network-detail");
            var result=await flow.Send(Request(),(r,t)=>VillageConversationClient.Send(r,connection,http,t));
            check(result.Failure==DialogueFailure.Connection && flow.Feedback.Contains("could not be reached") &&
                !flow.Feedback.Contains("synthetic-private-network-detail"),"connectivity failures are identified without leaking exception messages");
        }
        using(var fake=new FakeHttp())using(var http=new HttpClient(fake))using(var flow=new VillageConversationFlow()) {
            fake.Reply=message=>Task.FromResult(message.Method==HttpMethod.Get?Http(200,Healthy):
                Http(200,"{\"version\":2,\"request_id\":\"other-request\",\"turn_id\":\"hello\",\"action_id\":\"a0\",\"english\":\"Readable but unrelated\"}"));
            flow.Input="Namaskara";
            var result=await flow.Send(Request(),(r,t)=>VillageConversationClient.Send(r,connection,http,t));
            check(result.Failure==DialogueFailure.InvalidResponse && flow.Line==null && flow.Input=="Namaskara",
                "a readable response for a different request cannot reach dialogue or story progress");
        }
        var prefix=new VillageServiceConnection("https://example.invalid/private-demo/",new string('a',32));
        check(prefix.HealthEndpoint.AbsolutePath=="/private-demo/health" && prefix.TurnEndpoint.AbsolutePath=="/private-demo/v2/village/turn",
            "health and chat use the same configured service path prefix");
    }

    static void OfflineIntroduction(Action<bool,string> check)
    {
        var p=new TeaProgress();var provider=new LocalTeaConversation();var turn=provider.Start(p);
        turn=provider.Reply(turn,1,p);
        check(!p.greeted && !p.drinkOrdered,"choosing language help does not award introduction or ordering progress");
        turn=provider.Reply(turn,0,p);turn=provider.Reply(turn,1,p);
        check(p.greeted && p.drinkOrdered && p.drink=="water" && !p.directionsKnown,"authored phrases support a real drink choice without granting the clue early");
        turn=provider.Reply(turn,0,p);
        check(p.directionsKnown && p.Objective=="Follow the lane towards the library","the first chapter can progress to the library using authored phrases alone");
        check(!p.libraryAccessGranted && !p.deedRetrieved && !p.storyCompleted,"assisted conversation preserves later evidence and story gates");
    }

    static void ChapterConsistency(Action<bool,string> check)
    {
        string[] objectives={"Introduce yourself at the tea stall","Follow the lane towards the library",
            "Visit Shanthakka at the mess across the lane","Show Ananya’s shopping note to Basavaraju",
            "Follow the river path and speak to Giri","Speak to Gurumurthy and Rathnamma at the northern stop",
            "Bring the Gudda Mane family name to Somanna","Read the public panchayat notice",
            "Ask Raghavendra for a ride to the forest checkpost","Checked ridge approach available - review the trailhead",
            "Meet Ananya on the farmhouse veranda","Open the family trunk in the farm archive",
            "Prepare the return journey with Ananya","Present the protected original at the library court",
            "Works suspended - await the council review outcome"};
        var main=new TeaProgress {textScale=1.3f};
        for(int n=0;n<objectives.Length;n++)
        {
            var chapter=(ReplayChapter)n;var prepared=ChapterReplay.Seed(chapter,main);
            check(prepared.Objective==objectives[n],"prepared chapter has its own next objective: "+chapter);
            for(int i=0;i<objectives.Length;i++)
                check(ChapterReplay.IsUnlocked(prepared,(ReplayChapter)i)==(i<=n),"practice preparation unlocks exactly the reached chapter prefix: "+chapter+" / "+(ReplayChapter)i);
            check(prepared.textScale==1.3f && !main.greeted && !main.storyCompleted,"chapter preparation retains preferences without mutating the main visit: "+chapter);
        }
        check(!ChapterReplay.IsUnlocked(main,(ReplayChapter)(-1)) && !ChapterReplay.IsUnlocked(main,(ReplayChapter)99),"unknown chapter values cannot unlock content");
        bool rejected=false;try{ChapterReplay.Seed((ReplayChapter)99,main);}catch(ArgumentOutOfRangeException){rejected=true;}
        check(rejected,"unknown chapter values cannot seed a playable visit");
    }

    static void ProtocolChecks(Action<bool,string> check)
    {
        var request=Request("ನಮಸ್ಕಾರ \"quoted\"\nmessage");
        using(var payload=JsonDocument.Parse(VillageConversationClient.EncodeRequest(request)))
        {
            var root=payload.RootElement;
            check(root.GetProperty("event").GetString()=="message" && root.GetProperty("version").GetInt32()==2,"typed chat retains the v2 message protocol");
            check(root.GetProperty("learner_text").GetString()==request.Input && root.GetProperty("request_id").GetString()==request.Id,"Kannada, quoted text and request correlation survive serialization");
            check(root.GetProperty("actions")[0].GetProperty("next_fact").GetString()=="Offer tea or water","only snapshotted eligible actions and their previewed facts reach the bridge");
        }
        string response="{\"version\":2,\"request_id\":\""+request.Id+"\",\"turn_id\":\"hello\",\"action_id\":\"a0\",\"kannada\":\"ನಮಸ್ಕಾರ\",\"roman\":\"Namaskara\",\"english\":\"Welcome\"}";
        check(VillageConversationClient.DecodeReply(request,Encoding.UTF8.GetBytes(response)).Line.Kannada=="ನಮಸ್ಕಾರ","correlated reply preserves the Kannada-script line");
        foreach(var invalid in new[]{response.Replace(request.Id,"other-request"),response.Replace("\"hello\"","\"other-turn\""),response.Replace("\"version\":2","\"version\":1")})
        {
            bool rejected=false;
            try{VillageConversationClient.DecodeReply(request,Encoding.UTF8.GetBytes(invalid));}
            catch(System.IO.InvalidDataException){rejected=true;}
            check(rejected,"reply correlation rejects a different request, turn or protocol version");
        }
    }
}
