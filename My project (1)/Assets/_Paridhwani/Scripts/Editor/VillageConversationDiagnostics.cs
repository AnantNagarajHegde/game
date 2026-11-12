using System;
using System.Diagnostics;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Debug=UnityEngine.Debug;

namespace Paridhwani.Editor
{
    // Runs in the actual Editor process, using the same connection and transport as the game.
    public static class VillageConversationDiagnostics
    {
        static CancellationTokenSource pending;

        [MenuItem("Paridhwani/Check conversation connection")]
        public static void Check()=>Run(false);

        [MenuItem("Paridhwani/Test one conversation reply")]
        public static void TestReply()=>Run(true);

        static void Cancel()=>pending?.Cancel();

        static async void Run(bool testReply)
        {
            if(pending!=null){Debug.Log("A conversation check is already running.");return;}
            var cancellation=new CancellationTokenSource(VillageConversationFlow.ReplyTimeoutMs);
            pending=cancellation;
            AssemblyReloadEvents.beforeAssemblyReload+=Cancel;
            EditorApplication.quitting+=Cancel;
            string target="invalid settings";
            var elapsed=Stopwatch.StartNew();
            try {
                var connection=VillageServiceConnection.FromEnvironment();
                target=connection.TurnEndpoint.GetLeftPart(UriPartial.Authority);
                var status=await VillageConversationClient.CheckConnection(cancellation.Token);
                Debug.Log("PARIDHWANI_CHAT_CONNECTION_OK target="+target+" provider="+status.Provider+
                    " speechConfigured="+status.SpeechConfigured+". Configuration check only; provider credentials are not validated.");
                if(testReply) {
                    Debug.Log("Testing one synthetic conversation turn. This uses provider quota and does not touch a saved visit.");
                    var request=new VillageConversationRequest("hello","Manjunatha","Tea stall","Hello",
                        "Greet Manjunatha and introduce yourself by name.","Hello! What would you like?",
                        new[]{new VillageConversationAction("a0","Greet Manjunatha and introduce yourself by providing your name.","Offer tea or water.",0)},
                        Array.Empty<VillageConversationExchange>());
                    var reply=await VillageConversationClient.Send(request,cancellation.Token);
                    Debug.Log("PARIDHWANI_CHAT_REPLY_OK target="+target+" seconds="+
                        elapsed.Elapsed.TotalSeconds.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+
                        " actionMetadataAccepted="+!reply.ActionRejected+". Review actual language quality during a playthrough.");
                }
            }
            catch(DialogueRequestException error) {Debug.LogWarning("PARIDHWANI_CHAT_DIAGNOSTIC_FAILURE target="+target+" kind="+error.Failure+" http="+error.HttpStatus+" type="+error.DiagnosticType);}
            catch(OperationCanceledException) {Debug.LogWarning("PARIDHWANI_CHAT_DIAGNOSTIC_FAILURE target="+target+" kind=TimeoutOrCancelled");}
            catch(Exception error) {Debug.LogWarning("PARIDHWANI_CHAT_DIAGNOSTIC_FAILURE target="+target+" kind=Unknown type="+DialogueRequestException.TypeNames(error));}
            finally {
                AssemblyReloadEvents.beforeAssemblyReload-=Cancel;
                EditorApplication.quitting-=Cancel;
                if(pending==cancellation)pending=null;
                cancellation.Dispose();
            }
        }
    }
}
