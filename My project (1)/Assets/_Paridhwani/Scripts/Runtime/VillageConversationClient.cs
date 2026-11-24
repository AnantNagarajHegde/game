using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Paridhwani
{
    public sealed class VillageConversationAction
    {
        public readonly string Id, Description, NextFact;
        public readonly int Choice;
        public VillageConversationAction(string id, string description, string nextFact, int choice)
        { Id=id; Description=description; NextFact=nextFact; Choice=choice; }
    }

    public sealed class VillageConversationExchange
    {
        public readonly string Player, Npc;
        public VillageConversationExchange(string player, string npc) { Player=player; Npc=npc; }
    }

    // A detached turn snapshot. The transport never receives the live provider or saved progress.
    public sealed class VillageConversationRequest
    {
        public readonly string Id=Guid.NewGuid().ToString("N");
        public readonly string TurnId, Speaker, Role, Input, Objective, SceneFact;
        public readonly System.Collections.Generic.IReadOnlyList<VillageConversationAction> Actions;
        public readonly System.Collections.Generic.IReadOnlyList<VillageConversationExchange> History;
        public VillageConversationRequest(string turnId, string speaker, string role, string input,
            string objective, string sceneFact, VillageConversationAction[] actions, VillageConversationExchange[] history)
        {
            TurnId=turnId; Speaker=speaker; Role=role; Input=input; Objective=objective; SceneFact=sceneFact;
            Actions=Array.AsReadOnly((VillageConversationAction[])actions.Clone());
            History=Array.AsReadOnly((VillageConversationExchange[])history.Clone());
        }
    }

    public sealed class VillageConversationReply
    {
        public readonly string ActionId;
        public readonly TeaLine Line;
        public readonly bool ActionRejected;
        public VillageConversationReply(string actionId, TeaLine line, bool actionRejected=false)
        { ActionId=actionId; Line=line; ActionRejected=actionRejected; }
    }

    public sealed class VillageConnectionStatus
    {
        public readonly string Provider;
        public readonly bool SpeechConfigured;
        public VillageConnectionStatus(string provider,bool speechConfigured)
        {Provider=provider;SpeechConfigured=speechConfigured;}
    }

    public static class VillageConversationClient
    {
        static readonly HttpClient client=new HttpClient(new HttpClientHandler {
            AllowAutoRedirect=false, UseProxy=false, UseCookies=false, UseDefaultCredentials=false,
            AutomaticDecompression=DecompressionMethods.None
        }) { Timeout=Timeout.InfiniteTimeSpan };

        public static async Task<VillageConversationReply> Send(VillageConversationRequest request, CancellationToken token)
        {
            var connection=VillageServiceConnection.FromEnvironment();
            return await Send(request,connection,client,token).ConfigureAwait(false);
        }

        // The same transport path is exercised by an in-memory HttpMessageHandler in verification.
        public static async Task<VillageConversationReply> Send(VillageConversationRequest request,
            VillageServiceConnection connection,HttpClient transport,CancellationToken token)
        {
            var payload=EncodeRequest(request);
            await CheckConnection(connection,transport,token).ConfigureAwait(false);
            var response=await Exchange(HttpMethod.Post,connection.TurnEndpoint,connection.AccessToken,payload,65536,transport,token).ConfigureAwait(false);
            try {return NormalizeReply(request,DecodeReply(request,response));}
            catch(InvalidDataException){throw new DialogueRequestException(DialogueFailure.InvalidResponse);}
        }

        public static Task<VillageConnectionStatus> CheckConnection(CancellationToken token)=>
            CheckConnection(VillageServiceConnection.FromEnvironment(),client,token);

        public static async Task<VillageConnectionStatus> CheckConnection(VillageServiceConnection connection,
            HttpClient transport,CancellationToken token)
        {
            using(var deadline=CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(4000);
                try {
                    var bytes=await Exchange(HttpMethod.Get,connection.HealthEndpoint,connection.AccessToken,null,4096,transport,deadline.Token).ConfigureAwait(false);
                    using(var doc=JsonDocument.Parse(bytes)) {
                        var root=doc.RootElement;
                        if(root.GetProperty("version").GetInt32()!=2)throw new DialogueRequestException(DialogueFailure.Configuration);
                        var provider=root.GetProperty("provider").GetString();
                        if(provider!="groq" && provider!="openrouter" && provider!="ollama" && provider!="tea-local")throw new DialogueRequestException(DialogueFailure.InvalidResponse);
                        if(!root.GetProperty("model_configured").GetBoolean())throw new DialogueRequestException(DialogueFailure.Configuration);
                        if(root.GetProperty("authentication_required").GetBoolean() && connection.AccessToken.Length==0)
                            throw new DialogueRequestException(DialogueFailure.ServiceAuthentication);
                        return new VillageConnectionStatus(provider,root.GetProperty("speech_configured").GetBoolean());
                    }
                }
                catch(OperationCanceledException) when(!token.IsCancellationRequested) {throw new DialogueRequestException(DialogueFailure.Connection);}
                catch(Exception error) when(error is JsonException || error is KeyNotFoundException || error is InvalidOperationException || error is FormatException)
                {throw new DialogueRequestException(DialogueFailure.InvalidResponse);}
            }
        }

        public static byte[] EncodeRequest(VillageConversationRequest request)
        {
            ValidateRequest(request);
            using(var stream=new MemoryStream())
            {
                using(var json=new Utf8JsonWriter(stream))
                {
                    json.WriteStartObject(); json.WriteNumber("version",2); json.WriteString("request_id",request.Id);
                    json.WriteString("event","message"); json.WriteString("turn_id",request.TurnId);
                    json.WriteString("speaker",request.Speaker); json.WriteString("role",request.Role);
                    json.WriteString("learner_text",request.Input); json.WriteString("objective",request.Objective);
                    json.WriteString("scene_fact",request.SceneFact);
                    json.WriteStartArray("history");
                    foreach(var exchange in request.History) {
                        json.WriteStartObject(); json.WriteString("player",exchange.Player);
                        json.WriteString("npc",exchange.Npc); json.WriteEndObject();
                    }
                    json.WriteEndArray(); json.WriteStartArray("actions");
                    foreach(var action in request.Actions) {
                        json.WriteStartObject(); json.WriteString("id",action.Id);
                        json.WriteString("description",action.Description); json.WriteString("next_fact",action.NextFact);
                        json.WriteEndObject();
                    }
                    json.WriteEndArray(); json.WriteEndObject();
                }
                if(stream.Length>65536)throw new DialogueRequestException(DialogueFailure.InvalidRequest);
                return stream.ToArray();
            }
        }

        public static VillageConversationReply DecodeReply(VillageConversationRequest request, byte[] response)
        {
            try { using(var doc=JsonDocument.Parse(response,new JsonDocumentOptions { MaxDepth=8 }))
            {
                var root=doc.RootElement;
                if(root.ValueKind!=JsonValueKind.Object)throw new InvalidDataException("Invalid reply");
                var names=new System.Collections.Generic.HashSet<string>();
                foreach(var property in root.EnumerateObject())if(!names.Add(property.Name))throw new InvalidDataException("Duplicate reply field");
                if(root.GetProperty("version").GetInt32()!=2 || root.GetProperty("request_id").GetString()!=request.Id ||
                    root.GetProperty("turn_id").GetString()!=request.TurnId) throw new InvalidDataException("Uncorrelated reply");
                string action=root.TryGetProperty("action_id",out var value) && value.ValueKind==JsonValueKind.String?value.GetString():null;
                return new VillageConversationReply(action,new TeaLine(Text(root,"kannada"),Text(root,"roman"),Text(root,"english")));
            }} catch(Exception error) when(error is JsonException || error is KeyNotFoundException || error is InvalidOperationException || error is FormatException)
            {throw new InvalidDataException("Invalid conversation reply");}
        }

        static string Text(JsonElement root,string name)
        {
            if(!root.TryGetProperty(name,out var value) || value.ValueKind==JsonValueKind.Null)return "";
            if(value.ValueKind!=JsonValueKind.String)throw new InvalidDataException("Invalid reply text");
            return value.GetString()??"";
        }

        public static VillageConversationReply NormalizeReply(VillageConversationRequest request,VillageConversationReply reply)
        {
            var line=reply?.Line;
            if(line==null || string.IsNullOrWhiteSpace(ContextText(line)) || (line.English?.Length??0)>1000 ||
                (line.Kannada?.Length??0)>1000 || (line.Roman?.Length??0)>1000)
                throw new DialogueRequestException(DialogueFailure.InvalidResponse);
            bool valid=reply.ActionId=="none" || System.Linq.Enumerable.Any(request.Actions,a=>a.Id==reply.ActionId);
            return new VillageConversationReply(valid?reply.ActionId:"none",new TeaLine(line.Kannada??"",line.Roman??"",line.English??""),reply.ActionRejected || !valid);
        }

        public static string ContextText(TeaLine line)=>!string.IsNullOrWhiteSpace(line?.English)?line.English:
            !string.IsNullOrWhiteSpace(line?.Kannada)?line.Kannada:line?.Roman??"";

        static void ValidateRequest(VillageConversationRequest request)
        {
            bool TextOk(string value,int limit)=>value!=null && value.Length<=limit;
            if(request==null || !TextOk(request.TurnId,128) || string.IsNullOrWhiteSpace(request.TurnId) ||
                !TextOk(request.Speaker,128) || !TextOk(request.Role,128) || !TextOk(request.Input,512) || string.IsNullOrWhiteSpace(request.Input) ||
                !TextOk(request.Objective,512) || !TextOk(request.SceneFact,4096) || request.Actions.Count>32 || request.History.Count>6)
                throw new DialogueRequestException(DialogueFailure.InvalidRequest);
            var ids=new System.Collections.Generic.HashSet<string>();
            foreach(var action in request.Actions)
                if(action==null || !System.Text.RegularExpressions.Regex.IsMatch(action.Id??"",@"\Aa(?:0|[1-9][0-9]?)\z") || !ids.Add(action.Id) ||
                    !TextOk(action.Description,4096) || !TextOk(action.NextFact,4096))throw new DialogueRequestException(DialogueFailure.InvalidRequest);
            foreach(var item in request.History)
                if(item==null || !TextOk(item.Player,1000) || !TextOk(item.Npc,1000))throw new DialogueRequestException(DialogueFailure.InvalidRequest);
        }

        public static async Task<byte[]> Speech(string speaker, string language, string text, CancellationToken token)
        {
            using(var stream=new MemoryStream())
            {
                using(var json=new Utf8JsonWriter(stream)) {
                    json.WriteStartObject(); json.WriteString("speaker",speaker); json.WriteString("language",language);
                    json.WriteString("text",text); json.WriteEndObject();
                }
                var connection=VillageServiceConnection.FromEnvironment();
                return await Exchange(HttpMethod.Post,connection.SpeechEndpoint,connection.AccessToken,stream.ToArray(),4*1024*1024,client,token).ConfigureAwait(false);
            }
        }

        public static DialogueFailure DecodeFailure(int status,byte[] body)
        {
            string code="";
            try {using(var doc=JsonDocument.Parse(body,new JsonDocumentOptions {MaxDepth=4}))
                if(doc.RootElement.TryGetProperty("error",out var value) && value.ValueKind==JsonValueKind.String)code=value.GetString();}
            catch(Exception error) when(error is JsonException || error is InvalidOperationException || error is ArgumentException) {}
            switch(code) {
                case "model_authentication_failed":return DialogueFailure.ProviderAuthentication;
                case "model_access_denied":return DialogueFailure.ProviderAccessDenied;
                case "model_request_blocked":return DialogueFailure.ProviderRequestBlocked;
                case "model_rate_limited":return DialogueFailure.RateLimited;
                case "service_busy":return DialogueFailure.Busy;
                case "model_request_rejected":case "model_resource_not_found":return DialogueFailure.ProviderRejected;
                case "model_timed_out":return DialogueFailure.Timeout;
                case "model_unavailable":return DialogueFailure.ProviderUnavailable;
                case "unauthorized":return DialogueFailure.ServiceAuthentication;
                case "invalid_request":return DialogueFailure.InvalidRequest;
                case "invalid_request_or_model_response":case "invalid_model_response":return DialogueFailure.InvalidResponse;
            }
            if(status==401 || status==403)return DialogueFailure.ServiceAuthentication;
            if(status==429)return DialogueFailure.RateLimited;
            if(status==400)return DialogueFailure.InvalidRequest;
            if(status==404 || (status>=300 && status<400))return DialogueFailure.Configuration;
            return status>=500?DialogueFailure.ProviderUnavailable:DialogueFailure.InvalidResponse;
        }

        static async Task<byte[]> Exchange(HttpMethod method,Uri endpoint,string accessToken,byte[] data,int limit,HttpClient transport,CancellationToken token)
        {
            try { using(var message=new HttpRequestMessage(method,endpoint))
            {
                if(accessToken.Length>0)message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",accessToken);
                if(data!=null) {
                    message.Content=new ByteArrayContent(data);
                    message.Content.Headers.ContentType=new MediaTypeHeaderValue("application/json") { CharSet="utf-8" };
                }
                using(var response=await transport.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,token).ConfigureAwait(false))
                {
                    int status=(int)response.StatusCode;
                    int bodyLimit=response.StatusCode==HttpStatusCode.OK?limit:4096;
                    if(response.Content==null || response.Content.Headers.ContentLength>bodyLimit)
                        throw new DialogueRequestException(DialogueFailure.InvalidResponse,status);
                    using(var body=await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using(var output=new MemoryStream())
                    {
                        var buffer=new byte[4096]; int count;
                        while((count=await body.ReadAsync(buffer,0,buffer.Length,token).ConfigureAwait(false))>0) {
                            if(output.Length+count>bodyLimit) throw new DialogueRequestException(DialogueFailure.InvalidResponse,status);
                            output.Write(buffer,0,count);
                        }
                        var bytes=output.ToArray();
                        if(response.StatusCode!=HttpStatusCode.OK)throw new DialogueRequestException(DecodeFailure(status,bytes),status);
                        return bytes;
                    }
                }
            }} catch(HttpRequestException error){throw new DialogueRequestException(DialogueFailure.Connection,0,DialogueRequestException.TypeNames(error));}
            catch(IOException error){throw new DialogueRequestException(DialogueFailure.Connection,0,DialogueRequestException.TypeNames(error));}
        }
    }
}
