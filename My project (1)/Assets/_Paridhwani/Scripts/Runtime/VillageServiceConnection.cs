using System;

namespace Paridhwani
{
    // Provider keys belong on the server. This optional token grants access to
    // a private demonstration service; it is not an OpenRouter or Azure key.
    public sealed class VillageServiceConnection
    {
        public readonly Uri TurnEndpoint, SpeechEndpoint, HealthEndpoint;
        public readonly string AccessToken;

        public VillageServiceConnection(string address, string accessToken)
        {
            address=string.IsNullOrWhiteSpace(address)?"http://127.0.0.1:8767/":address.Trim();
            if(!Uri.TryCreate(address,UriKind.Absolute,out var root) ||
                (root.Scheme!=Uri.UriSchemeHttps && !(root.Scheme==Uri.UriSchemeHttp && root.IsLoopback)) ||
                !string.IsNullOrEmpty(root.UserInfo) || !string.IsNullOrEmpty(root.Query) || !string.IsNullOrEmpty(root.Fragment))
                throw new ArgumentException("Use an HTTPS service address, or HTTP on localhost.",nameof(address));
            AccessToken=accessToken??"";
            if(AccessToken.Length>256 || (AccessToken.Length>0 && (AccessToken.Length<32 ||
                !System.Text.RegularExpressions.Regex.IsMatch(AccessToken,@"\A[A-Za-z0-9_-]+\z"))))
                throw new ArgumentException("Invalid service access token.",nameof(accessToken));
            if(!root.IsLoopback && AccessToken.Length==0)
                throw new ArgumentException("A remote private service requires its access token.",nameof(accessToken));
            var basis=new Uri(root.AbsoluteUri.TrimEnd('/')+"/");
            TurnEndpoint=new Uri(basis,"v2/village/turn");
            SpeechEndpoint=new Uri(basis,"v2/village/speech");
            HealthEndpoint=new Uri(basis,"health");
        }

        public static VillageServiceConnection FromEnvironment()
        {
            try {return new VillageServiceConnection(Environment.GetEnvironmentVariable("PARIDHWANI_CONVERSATION_URL"),
                Environment.GetEnvironmentVariable("PARIDHWANI_SERVICE_TOKEN"));}
            catch(ArgumentException){throw new DialogueRequestException(DialogueFailure.Configuration);}
        }
    }
}
