using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Paridhwani;

static class DialogueContractFixtures
{
    internal const string RequestId="0123456789abcdef0123456789abcdef",TurnId="fixture-greeting";
    internal static byte[] ReadBounded(string path)
    {
        using(var input=File.OpenRead(path))
        {
            if(input.Length>DialogueHttpContract.MaxBodyBytes)throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
            using(var output=new MemoryStream())
            {
                var buffer=new byte[4096];int read;
                while((read=input.Read(buffer,0,buffer.Length))!=0)
                {
                    if(output.Length+read>DialogueHttpContract.MaxBodyBytes)throw new DialogueTransportException(DialogueTransportFailure.BodyTooLarge);
                    output.Write(buffer,0,read);
                }
                return output.ToArray();
            }
        }
    }
    internal static byte[] Response(string english)=>Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
        version=1,request_id=RequestId,turn_id=TurnId,line=new {kn="",roman="",en=english}
    }));
    internal static void Run(Action<bool,string> check)
    {
        string root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"..","..","..","docs","contracts","nlp-v1-fixtures"));
        using(var manifest=JsonDocument.Parse(ReadBounded(Path.Combine(root,"manifest.json"))))
        using(var request=JsonDocument.Parse(ReadBounded(Path.Combine(root,"request.json"))))
        {
            check(manifest.RootElement.GetProperty("contract_version").GetInt32()==1,"fixture manifest version");
            var sample=request.RootElement;
            var turn=new TeaTurn(TurnId,"","Namaskara.","Hello.",new TeaChoice("","","Hello.","ignored-local-next"));
            using(var encoded=JsonDocument.Parse(DialogueHttpContract.Encode(new DialogueServiceRequest(new DialoguePrompt("Synthetic speaker",turn,"Synthetic greeting")))))
            {
                check(sample.GetProperty("request_id").GetString()==RequestId,"fixture request correlation");
                check(sample.EnumerateObject().Count()==encoded.RootElement.EnumerateObject().Count(),"fixture request envelope matches game encoder");
                foreach(var property in encoded.RootElement.EnumerateObject())
                    if(property.Name!="request_id")check(Equal(property.Value,sample.GetProperty(property.Name)),"fixture request field matches game encoder: "+property.Name);
            }
            foreach(var item in manifest.RootElement.GetProperty("cases").EnumerateArray())
            {
                string file=item.GetProperty("response_file").GetString(),expected=item.GetProperty("expected").GetString();
                try
                {
                    var reply=DialogueHttpContract.Decode(ReadBounded(Path.Combine(root,file)),RequestId,TurnId);
                    check(expected=="accepted","fixture unexpectedly accepted: "+file);
                    if(file=="accepted-unicode-literal.json")
                        check(reply.Line.English=="<b>Synthetic reply.</b> \U0001f600" && reply.Line.Kannada=="\u0ca8\u0cae\u0cb8\u0ccd\u0c95\u0cbe\u0cb0","Unicode and literal markup preserved");
                }
                catch(DialogueTransportException error) {check(expected==error.Failure.ToString(),"fixture failure mismatch: "+file);}
            }
        }
        check(DialogueHttpContract.Decode(Response(new string('x',4094)+"\U0001f600"),RequestId,TurnId).Line.English.Length==4096,"supplementary Unicode exactly fits UTF-16 reply bound");
        Reject(()=>DialogueHttpContract.Decode(Response(new string('x',4095)+"\U0001f600"),RequestId,TurnId),check,"supplementary Unicode exceeding UTF-16 reply bound");
        Reject(()=>DialogueHttpContract.Decode(Response("hello"),(DialogueServiceRequest)null),check,"null request context");
        Reject(()=>DialogueHttpContract.Decode(Response("hello"),"invalid",TurnId),check,"invalid expected request ID");
        Reject(()=>DialogueHttpContract.Decode(Response("hello"),RequestId," "),check,"invalid expected turn ID");
        Reject(()=>DialogueHttpContract.Decode(Encoding.UTF8.GetBytes(new string('[',9)+new string(']',9)),RequestId,TurnId),check,"JSON nesting bound");
        Console.WriteLine("NLP_CONTRACT_FIXTURES_OK: 8 portable response cases; no sockets.");
    }
    static void Reject(Action action,Action<bool,string> check,string label)
    {
        try {action();check(false,label+" unexpectedly accepted");}
        catch(DialogueTransportException error) {check(error.Failure==DialogueTransportFailure.InvalidContract,label);}
    }
    static bool Equal(JsonElement left,JsonElement right)
    {
        if(left.ValueKind!=right.ValueKind)return false;
        if(left.ValueKind==JsonValueKind.Object)
            return left.EnumerateObject().Count()==right.EnumerateObject().Count() && left.EnumerateObject().All(p=>right.TryGetProperty(p.Name,out var value) && Equal(p.Value,value));
        if(left.ValueKind==JsonValueKind.Array)
            return left.GetArrayLength()==right.GetArrayLength() && left.EnumerateArray().Zip(right.EnumerateArray(),Equal).All(value=>value);
        return left.ValueKind==JsonValueKind.String?left.GetString()==right.GetString():left.GetRawText()==right.GetRawText();
    }
}
