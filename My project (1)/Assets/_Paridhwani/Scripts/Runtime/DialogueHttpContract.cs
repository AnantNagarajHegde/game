using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Paridhwani
{
    // Explicit JSON mapping: no Unity objects, save state, effect commands or reflection serializer.
    public static class DialogueHttpContract
    {
        public const string Route="/v1/dialogue/respond";
        public const int MaxBodyBytes=65536, MaxTextUnits=4096, MaxChoices=32;
        static readonly UTF8Encoding StrictUtf8=new UTF8Encoding(false,true);

        public static byte[] Encode(DialogueServiceRequest request)
        {
            if(request==null || request.Version!=1 || !ValidId(request.RequestId) ||
                !Bounded(request.TurnId,128,false) || !Bounded(request.Speaker,128,false) ||
                !Bounded(request.LearnerText,512,false) || request.AvailableReplies.Count>MaxChoices)
                throw Invalid();
            using(var output=new MemoryStream())
            {
                using(var json=new Utf8JsonWriter(output))
                {
                    json.WriteStartObject();json.WriteNumber("version",request.Version);
                    json.WriteString("request_id",request.RequestId);json.WriteString("turn_id",request.TurnId);
                    json.WriteString("speaker",request.Speaker);json.WriteString("learner_text",request.LearnerText);
                    json.WritePropertyName("authored_line");WriteText(json,request.AuthoredLine);
                    json.WriteStartArray("available_replies");
                    foreach(var line in request.AvailableReplies)WriteText(json,line);
                    json.WriteEndArray();json.WriteEndObject();
                }
                if(output.Length>MaxBodyBytes)throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
                return output.ToArray();
            }
        }

        public static DialogueServiceReply Decode(byte[] utf8,DialogueServiceRequest request)
        {
            if(request==null)throw Invalid();
            return Decode(utf8,request.RequestId,request.TurnId);
        }

        // Also supports checking a saved backend response against explicit correlation, without a service call.
        public static DialogueServiceReply Decode(byte[] utf8,string requestId,string turnId)
        {
            if(!ValidId(requestId) || !Bounded(turnId,128,false))throw Invalid();
            if(utf8==null || utf8.Length>MaxBodyBytes)
                throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
            try
            {
                // Reject malformed UTF-8 as well as malformed/extra JSON values and deep documents.
                StrictUtf8.GetCharCount(utf8);
                using(var document=JsonDocument.Parse(utf8,new JsonDocumentOptions {MaxDepth=8}))
                {
                    var root=document.RootElement;
                    Object(root,"version","request_id","turn_id","line");
                    var version=root.GetProperty("version");
                    if(version.ValueKind!=JsonValueKind.Number || !version.TryGetInt32(out int number) || number!=1 ||
                        Text(root,"request_id",32,false)!=requestId || Text(root,"turn_id",128,false)!=turnId)
                        throw Invalid();
                    var line=root.GetProperty("line");Object(line,"kn","roman","en");
                    var text=new DialogueServiceText(Text(line,"kn",MaxTextUnits,true),Text(line,"roman",MaxTextUnits,true),Text(line,"en",MaxTextUnits,true));
                    if(string.IsNullOrWhiteSpace(text.Kannada) && string.IsNullOrWhiteSpace(text.English))throw Invalid();
                    return new DialogueServiceReply(number,requestId,turnId,text);
                }
            }
            catch(DialogueTransportException){throw;}
            catch(Exception error) when(error is JsonException || error is InvalidOperationException ||
                error is KeyNotFoundException || error is DecoderFallbackException || error is ArgumentException)
            {throw Invalid();}
        }

        static void Object(JsonElement element,params string[] names)
        {
            if(element.ValueKind!=JsonValueKind.Object)throw Invalid();
            var found=new HashSet<string>(StringComparer.Ordinal);
            foreach(var property in element.EnumerateObject())
                if(Array.IndexOf(names,property.Name)<0 || !found.Add(property.Name))throw Invalid();
            if(found.Count!=names.Length)throw Invalid();
        }
        static string Text(JsonElement element,string name,int max,bool empty)
        {
            var field=element.GetProperty(name);
            if(field.ValueKind!=JsonValueKind.String)throw Invalid();
            string value=field.GetString();if(!Bounded(value,max,empty))throw Invalid();return value;
        }
        static bool Bounded(string value,int max,bool empty)
        {
            if(value==null || value.Length>max || (!empty && string.IsNullOrWhiteSpace(value)))return false;
            try {StrictUtf8.GetByteCount(value);return true;}catch(EncoderFallbackException){return false;}
        }
        static bool ValidId(string value)
        {
            if(value==null || value.Length!=32)return false;
            foreach(char c in value)if(!(c>='0' && c<='9') && !(c>='a' && c<='f'))return false;
            return true;
        }
        static void WriteText(Utf8JsonWriter json,DialogueServiceText line)
        {
            if(line==null || !Bounded(line.Kannada,MaxTextUnits,true) || !Bounded(line.Roman,MaxTextUnits,true) ||
                !Bounded(line.English,MaxTextUnits,true))throw Invalid();
            json.WriteStartObject();json.WriteString("kn",line.Kannada);json.WriteString("roman",line.Roman);
            json.WriteString("en",line.English);json.WriteEndObject();
        }
        static DialogueTransportException Invalid()=>new DialogueTransportException(DialogueTransportFailure.InvalidContract);
    }
}
