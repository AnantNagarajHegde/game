using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("TeaAssessmentChecks")]

namespace Paridhwani
{
    // Development-only transport policy. No DNS, proxy, redirects, cookies, credentials or remote endpoint.
    // It uses no Unity API and can cancel on worker threads. Owner disposes after detaching the session.
    public sealed class LoopbackTeaAssessmentService : ITeaAssessmentService,IDisposable
    {
        readonly Uri endpoint;
        readonly HttpClient client;
        readonly int timeoutMs;
        int disposed;
        public LoopbackTeaAssessmentService(bool enableMockNetworking,Uri endpoint,int timeoutMs=7000)
            :this(enableMockNetworking,endpoint,timeoutMs,null) {}
        // Friend test assembly can exercise the real HTTP lifecycle without socket permissions.
        internal LoopbackTeaAssessmentService(bool enableMockNetworking,Uri endpoint,int timeoutMs,HttpMessageHandler testHandler)
        {
            if(!enableMockNetworking)throw new InvalidOperationException("Mock networking requires explicit opt-in.");
            if(endpoint==null || !endpoint.IsAbsoluteUri || endpoint.Scheme!="http" || endpoint.Host!="127.0.0.1" ||
                endpoint.Port<1 || endpoint.AbsolutePath!=TeaAssessmentContract.Route || endpoint.Query!="" ||
                endpoint.Fragment!="" || endpoint.UserInfo!="")
                throw new ArgumentException("Use http://127.0.0.1:<port>"+TeaAssessmentContract.Route,nameof(endpoint));
            if(timeoutMs<1 || timeoutMs>7000)throw new ArgumentOutOfRangeException(nameof(timeoutMs));
            this.endpoint=endpoint;this.timeoutMs=timeoutMs;
            client=new HttpClient(testHandler ?? new HttpClientHandler {
                AllowAutoRedirect=false,UseProxy=false,UseCookies=false,UseDefaultCredentials=false,
                AutomaticDecompression=DecompressionMethods.None
            }) {Timeout=Timeout.InfiniteTimeSpan};
        }

        public async Task<byte[]> ReplyAsync(string requestId,string learnerText,CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            if(Volatile.Read(ref disposed)!=0)throw new DialogueTransportException(DialogueTransportFailure.Disposed);
            byte[] body=TeaAssessmentContract.Encode(requestId,learnerText);
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(cancellation))
            using(var message=new HttpRequestMessage(HttpMethod.Post,endpoint))
            {
                deadline.CancelAfter(timeoutMs);
                message.Content=new ByteArrayContent(body);
                message.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json") {CharSet="utf-8"};
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                try
                {
                    using(var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,deadline.Token).ConfigureAwait(false))
                    {
                        if(response.StatusCode!=HttpStatusCode.OK)throw new DialogueTransportException(DialogueTransportFailure.HttpStatus);
                        var type=response.Content.Headers.ContentType;
                        if(type==null || !string.Equals(type.MediaType,"application/json",StringComparison.OrdinalIgnoreCase) ||
                            (type.CharSet!=null && !string.Equals(type.CharSet.Trim('"'),"utf-8",StringComparison.OrdinalIgnoreCase)) ||
                            response.Content.Headers.ContentEncoding.Count!=0)
                            throw new DialogueTransportException(DialogueTransportFailure.ContentType);
                        if(response.Content.Headers.ContentLength>65536)
                            throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
                        using(var stream=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using(var output=new MemoryStream())
                        {
                            var buffer=new byte[4096];int read;
                            while((read=await stream.ReadAsync(buffer,0,buffer.Length,deadline.Token).ConfigureAwait(false))!=0)
                            {
                                if(output.Length+read>65536)
                                    throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
                                output.Write(buffer,0,read);
                            }
                            deadline.Token.ThrowIfCancellationRequested();
                            return output.ToArray();
                        }
                    }
                }
                catch(OperationCanceledException)
                {
                    cancellation.ThrowIfCancellationRequested();
                    throw new DialogueTransportException(Volatile.Read(ref disposed)!=0?DialogueTransportFailure.Disposed:DialogueTransportFailure.Timeout);
                }
                catch(Exception error) when(error is HttpRequestException || error is IOException || error is ObjectDisposedException)
                {throw new DialogueTransportException(Volatile.Read(ref disposed)!=0?DialogueTransportFailure.Disposed:DialogueTransportFailure.Connectivity);}
            }
        }
        public void Dispose(){if(Interlocked.Exchange(ref disposed,1)==0)client.Dispose();}
    }
}
