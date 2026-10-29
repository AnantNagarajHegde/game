using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Paridhwani.AssessmentExample
{
    // Standalone example only. No Unity API, quest effect, save, or network access.
    public sealed class AssessmentResult
    {
        public readonly string Outcome, AnswerId, ErrorCategory, CorrectionType, ReactionId;
        public AssessmentResult(string outcome,string answer,string error,string correction,string reaction)
        {Outcome=outcome;AnswerId=answer;ErrorCategory=error;CorrectionType=correction;ReactionId=reaction;}
    }

    public static class AssessmentConsumer
    {
        static void Require(bool condition) {if(!condition)throw new InvalidDataException("Assessment unavailable.");}
        static void Shape(JsonElement obj,params string[] names)
        {
            Require(obj.ValueKind==JsonValueKind.Object);
            var found=new HashSet<string>(StringComparer.Ordinal);
            foreach(var p in obj.EnumerateObject())Require(Array.IndexOf(names,p.Name)>=0 && found.Add(p.Name));
            Require(found.Count==names.Length);
        }
        static string Text(JsonElement obj,string key)
        {
            var value=obj.GetProperty(key);Require(value.ValueKind==JsonValueKind.String);
            string text=value.GetString();Require(text!=null && text.Length<=128);
            foreach(char c in text)Require((c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='_');
            return text;
        }
        public static AssessmentResult Read(byte[] body,string requestId,string questId,string stepId,string expectedRevision,ISet<string> allowedAnswers)
        {
            Require(body!=null && body.Length<=65536 && allowedAnswers!=null);
            using(var document=JsonDocument.Parse(body,new JsonDocumentOptions{MaxDepth=8}))
            {
                var root=document.RootElement;
                // Errors/timeouts belong to the caller's unavailable/fallback path.
                Shape(root,"version","request_id","quest_id","step_id","status","content_revision","assessment");
                Require(root.GetProperty("version").TryGetInt32(out int version)&&version==1);
                Require(Text(root,"request_id")==requestId && Text(root,"quest_id")==questId && Text(root,"step_id")==stepId);
                Require(Text(root,"status")=="ok" && Text(root,"content_revision")==expectedRevision);
                var a=root.GetProperty("assessment");
                Shape(a,"outcome","answer_id","error_category","correction_type","reaction_id","detected_form","confidence");
                string outcome=Text(a,"outcome"),answer=Text(a,"answer_id"),error=Text(a,"error_category"),correction=Text(a,"correction_type"),reaction=Text(a,"reaction_id");
                Require(new HashSet<string>{"kanglish","kannada","english","hindi_roman","mixed","digits","noise"}.Contains(Text(a,"detected_form")));
                Require(a.GetProperty("confidence").TryGetDouble(out double confidence)&&double.IsFinite(confidence)&&confidence>=0&&confidence<=1);
                if(outcome=="not_accepted")Require(error.Length>0 && correction.Length==0 && (answer.Length==0 || allowedAnswers.Contains(answer)));
                else {Require(outcome=="accepted"||outcome=="accepted_with_correction");Require(allowedAnswers.Contains(answer)&&error.Length==0);Require((outcome=="accepted_with_correction")== (correction.Length>0));}
                // UI maps these IDs through its pinned content, with a safe fallback
                // for unknown error/reaction IDs. Never dispatch arbitrary commands.
                return new AssessmentResult(outcome,answer,error,correction,reaction);
            }
        }
        public static bool TryRead(byte[] body,string requestId,string questId,string stepId,string expectedRevision,ISet<string> allowedAnswers,out AssessmentResult result)
        {
            result=null;
            try {result=Read(body,requestId,questId,stepId,expectedRevision,allowedAnswers);return true;}
            catch(Exception e) when(e is InvalidDataException || e is JsonException || e is InvalidOperationException || e is KeyNotFoundException || e is ArgumentException)
            {return false;}
        }
    }

}
