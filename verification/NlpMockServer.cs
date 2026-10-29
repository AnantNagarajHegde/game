using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Paridhwani;

// Dependency-free synthetic fixture, never an NLP implementation. Binds literal IPv4 loopback only.
sealed class NlpMockServer : IDisposable
{
    readonly TcpListener listener;
    readonly CancellationTokenSource stop=new CancellationTokenSource();
    readonly SemaphoreSlim slots=new SemaphoreSlim(16);
    readonly List<Task> workers=new List<Task>();
    readonly Task accepting;
    int requests;
    public int Requests=>Volatile.Read(ref requests);
    public Uri Endpoint {get;}
    public NlpMockServer(int port=0)
    {
        listener=new TcpListener(IPAddress.Loopback,port);listener.Start(16);
        Endpoint=new Uri("http://127.0.0.1:"+((IPEndPoint)listener.LocalEndpoint).Port+DialogueHttpContract.Route);
        accepting=Accept();
    }
    async Task Accept()
    {
        try
        {
            while(!stop.IsCancellationRequested)
            {
                await slots.WaitAsync(stop.Token);
                var socket=await listener.AcceptTcpClientAsync();
                lock(workers)
                {
                    workers.RemoveAll(t=>t.IsCompleted);
                    workers.Add(Handle(socket));
                }
            }
        }
        catch(Exception e) when(e is ObjectDisposedException || e is SocketException || e is OperationCanceledException) {}
    }
    async Task Handle(TcpClient socket)
    {
        using(socket)
        using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
        {
            deadline.CancelAfter(10000);
            try
            {
                var stream=socket.GetStream();var header=new List<byte>();var one=new byte[1];
                while(header.Count<8192)
                {
                    if(await stream.ReadAsync(one,0,1,deadline.Token)==0)return;
                    header.Add(one[0]);int n=header.Count;
                    if(n>=4 && header[n-4]==13 && header[n-3]==10 && header[n-2]==13 && header[n-1]==10)break;
                }
                var lines=Encoding.ASCII.GetString(header.ToArray()).Split(new[]{"\r\n"},StringSplitOptions.None);
                Interlocked.Increment(ref requests);
                if(lines[0]!="POST "+DialogueHttpContract.Route+" HTTP/1.1") {await Send(stream,404,"{}",deadline.Token);return;}
                int length=-1;bool contentType=false;
                foreach(string line in lines)
                {
                    if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase))int.TryParse(line.Substring(15).Trim(),out length);
                    if(line.StartsWith("Content-Type: application/json",StringComparison.OrdinalIgnoreCase))contentType=true;
                }
                if(length<1 || length>DialogueHttpContract.MaxBodyBytes || !contentType) {await Send(stream,400,"{}",deadline.Token);return;}
                var body=new byte[length];int offset=0;
                while(offset<length) {int n=await stream.ReadAsync(body,offset,length-offset,deadline.Token);if(n==0)return;offset+=n;}
                var plan=NlpMockResponse.Create(body);
                if(plan.DelayHeaders)await Task.Delay(600,deadline.Token);
                await Header(stream,plan.Status,plan.DeclareLength?plan.Body.Length:(int?)null,plan.Type,plan.Extra,deadline.Token);
                if(plan.DelayBody)await Task.Delay(600,deadline.Token);
                await stream.WriteAsync(plan.Body,0,plan.Body.Length,deadline.Token);
            }
            catch(Exception e) when(e is IOException || e is OperationCanceledException || e is SocketException ||
                e is JsonException || e is KeyNotFoundException || e is InvalidOperationException) {}
            finally {slots.Release();}
        }
    }
    static Task Send(NetworkStream stream,int status,string json,CancellationToken token,string type="application/json",string extra="",bool length=true)
        =>SendBytes(stream,status,Encoding.UTF8.GetBytes(json),token,type,extra,length);
    static async Task SendBytes(NetworkStream stream,int status,byte[] bytes,CancellationToken token,string type="application/json",string extra="",bool length=true)
    {
        await Header(stream,status,length?bytes.Length:(int?)null,type,extra,token);
        await stream.WriteAsync(bytes,0,bytes.Length,token);
    }
    static async Task Header(NetworkStream stream,int status,int? length,string type,string extra,CancellationToken token)
    {
        byte[] bytes=Encoding.ASCII.GetBytes("HTTP/1.1 "+status+" Mock\r\nContent-Type: "+type+"; charset=utf-8\r\n"+
            (length.HasValue?"Content-Length: "+length+"\r\n":"")+"Connection: close\r\n"+extra+"\r\n");
        await stream.WriteAsync(bytes,0,bytes.Length,token);
    }
    public void Dispose()
    {
        stop.Cancel();listener.Stop();accepting.GetAwaiter().GetResult();
        Task[] pending;lock(workers)pending=workers.ToArray();Task.WhenAll(pending).GetAwaiter().GetResult();
        stop.Dispose();slots.Dispose();
    }
}

// Same wire fixtures feed socket and in-memory tests. No request content is logged or retained.
sealed class NlpMockResponse
{
    public byte[] Body;
    public int Status=200;
    public string Type="application/json",Extra="";
    public bool DeclareLength=true,DelayHeaders,DelayBody;
    public static NlpMockResponse Create(byte[] body)
    {
        var plan=new NlpMockResponse();
        using(var document=JsonDocument.Parse(body))
        {
            var root=document.RootElement;string mode=root.GetProperty("learner_text").GetString();
            var keys=new HashSet<string>();foreach(var property in root.EnumerateObject())keys.Add(property.Name);
            if(!keys.SetEquals(new[]{"version","request_id","turn_id","speaker","learner_text","authored_line","available_replies"}) ||
                root.GetProperty("version").GetInt32()!=1 || mode.Length>512 || root.GetProperty("available_replies").GetArrayLength()>32)
            {plan.Status=400;plan.Body=Encoding.UTF8.GetBytes("{}");return plan;}
            var text=new Dictionary<string,object> { ["kn"]="ನಮಸ್ಕಾರ",["roman"]="Namaskara.",["en"]="Mock reply. Continue with a supported reply." };
            var reply=new Dictionary<string,object> { ["version"]=1,["request_id"]=root.GetProperty("request_id").GetString(),["turn_id"]=root.GetProperty("turn_id").GetString(),["line"]=text };
            if(mode=="mock:wrong-id")reply["request_id"]="00000000000000000000000000000000";
            if(mode=="mock:wrong-turn")reply["turn_id"]="other-turn";
            if(mode=="mock:wrong-version")reply["version"]=2;
            if(mode=="mock:typed-version")reply["version"]="1";
            if(mode=="mock:missing")reply.Remove("line");
            if(mode=="mock:null")reply["line"]=null;
            if(mode=="mock:empty") {text["kn"]=" ";text["en"]=" ";}
            if(mode=="mock:text-limit")text["en"]=new string('x',4097);
            if(mode=="mock:null-text")text["en"]=null;
            if(mode=="mock:command")reply["effect"]="unlock_everything";
            string json=JsonSerializer.Serialize(reply);
            if(mode=="mock:duplicate")json=json.Insert(1,"\"version\":1,");
            if(mode=="mock:malformed")json="{bad json";
            if(mode=="mock:oversize" || mode=="mock:stream-oversize")json=new string('x',65537);
            plan.Body=mode=="mock:invalid-utf8"?new byte[]{0xff,0xfe}:Encoding.UTF8.GetBytes(json);
            plan.DelayHeaders=mode=="mock:delay";plan.DelayBody=mode=="mock:slow-body";
            plan.DeclareLength=mode!="mock:stream-oversize";
            plan.Status=mode=="mock:redirect"?302:mode=="mock:unauthorized"?401:mode=="mock:forbidden"?403:mode=="mock:limited"?429:mode=="mock:error"?503:200;
            plan.Type=mode=="mock:content-type"?"text/html":"application/json";
            plan.Extra=mode=="mock:encoding"?"Content-Encoding: gzip\r\n":"";
            if(mode=="mock:redirect")plan.Extra="Location: http://127.0.0.1:8765"+DialogueHttpContract.Route+"\r\n";
            return plan;
        }
    }
}

sealed class NlpTestFixture : IDisposable
{
    readonly NlpMockServer network;
    int requests;
    public int Requests=>network?.Requests??Volatile.Read(ref requests);
    public NlpTestFixture(bool sockets) {if(sockets)network=new NlpMockServer();}
    public LoopbackDialogueTextService Service(int timeoutMs=7000)=>network!=null?
        new LoopbackDialogueTextService(true,network.Endpoint,timeoutMs):
        new LoopbackDialogueTextService(true,new Uri("http://127.0.0.1:8765"+DialogueHttpContract.Route),timeoutMs,
            new InMemoryHandler(()=>Interlocked.Increment(ref requests)));
    public void Dispose(){network?.Dispose();}
    public static LoopbackDialogueTextService Disconnected()=>new LoopbackDialogueTextService(true,
        new Uri("http://127.0.0.1:8765"+DialogueHttpContract.Route),1000,new InMemoryHandler(null));

    sealed class InMemoryHandler : HttpMessageHandler
    {
        readonly Action count;
        public InMemoryHandler(Action count) {this.count=count;}
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage message,CancellationToken token)
        {
            if(count==null)throw new HttpRequestException("Synthetic offline fixture");
            count();
            if(message.Method!=HttpMethod.Post || message.RequestUri.AbsolutePath!=DialogueHttpContract.Route ||
                message.Content.Headers.ContentType.MediaType!="application/json" || message.Headers.Accept.ToString()!="application/json")
                throw new InvalidOperationException("Unexpected HTTP request mapping");
            var plan=NlpMockResponse.Create(await message.Content.ReadAsByteArrayAsync());
            if(plan.DelayHeaders)await Task.Delay(600,token);
            var response=new HttpResponseMessage((HttpStatusCode)plan.Status);
            response.Content=new StreamContent(new FixtureStream(plan.Body,plan.DelayBody));
            response.Content.Headers.ContentType=new MediaTypeHeaderValue(plan.Type) {CharSet="utf-8"};
            if(plan.DeclareLength)response.Content.Headers.ContentLength=plan.Body.Length;
            if(plan.Extra.StartsWith("Content-Encoding"))response.Content.Headers.ContentEncoding.Add("gzip");
            if(plan.Status==302)response.Headers.Location=new Uri("http://127.0.0.1:8765"+DialogueHttpContract.Route);
            return response;
        }
    }
    sealed class FixtureStream : Stream
    {
        readonly MemoryStream inner;
        bool delay;
        public FixtureStream(byte[] bytes,bool delay) {inner=new MemoryStream(bytes);this.delay=delay;}
        public override bool CanRead=>true;
        public override bool CanSeek=>false;
        public override bool CanWrite=>false;
        public override long Length=>throw new NotSupportedException();
        public override long Position {get=>inner.Position;set=>throw new NotSupportedException();}
        public override async Task<int> ReadAsync(byte[] bytes,int offset,int count,CancellationToken token)
        {if(delay) {delay=false;await Task.Delay(600,token);}return await inner.ReadAsync(bytes,offset,count,token);}
        public override int Read(byte[] bytes,int offset,int count)=>inner.Read(bytes,offset,count);
        public override void Flush() {}
        public override long Seek(long offset,SeekOrigin origin)=>throw new NotSupportedException();
        public override void SetLength(long value)=>throw new NotSupportedException();
        public override void Write(byte[] bytes,int offset,int count)=>throw new NotSupportedException();
        protected override void Dispose(bool disposing) {if(disposing)inner.Dispose();base.Dispose(disposing);}
    }
}
