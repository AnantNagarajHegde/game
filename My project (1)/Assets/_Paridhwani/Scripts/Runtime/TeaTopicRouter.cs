using System;
using System.Text.RegularExpressions;

namespace Paridhwani {
 // Local authored topic routing. No language score and no server-provided effects.
 public sealed class TeaTopicPlan {
  public int[] Choices=Array.Empty<int>();
  public string LineId,Feedback;
  public bool Assess;
 }
 public static class TeaTopicRouter {
  static string Text(string text)=>Regex.Replace(Regex.Replace((text??"").ToLowerInvariant(),"[^a-z ]"," ")," +"," ").Trim();
  static bool Is(string text,params string[] aliases)=>Array.IndexOf(aliases,text)>=0;
  static TeaTopicPlan Reply(string line,string feedback="")=>new TeaTopicPlan{LineId=line,Feedback=feedback};
  static TeaTopicPlan Choice(params int[] choices)=>new TeaTopicPlan{Choices=choices,Feedback="Conversation reply; no language score awarded."};
  public static string Help(TeaProgress p)=>!p.greeted?"Try 'hello', 'where is Ananya?', or 'help'.":!p.drinkOrdered?"Try 'Nanage chaha beku' for language practice, 'tea please' or 'water please' for an unscored story reply, or 'no tea'.":"Ask 'where is Ananya?', 'where is the library?', or say 'thank you'.";
  public static TeaTopicPlan Plan(TeaTurn turn,TeaProgress p,string input) {
   if(turn==null||p==null)return Reply(null,"Open a conversation with Manjunatha first.");
   if(string.IsNullOrWhiteSpace(input)||input.Length>512)return Reply(null,"Type a short question or greeting. "+Help(p));
   if(!TeaAssessmentContract.RomanInput(input))return Reply(null,"Please type in Latin letters. Other scripts and numbers are not supported here yet.");
   string text=Text(input);
   if(Is(text,"hello","hi","namaskara","namaskara manjunnatha","namaskara manjunatha"))
    return turn.Id=="hello"||turn.Id=="help"?Choice(0):Reply("hello","Hello again. "+Help(p));
   if(Is(text,"help","i know a little kannada","nanage kannada swalpa gottu"))
    return turn.Id=="hello"?Choice(1):Reply("help",Help(p));
   if(Is(text,"where is ananya","where s ananya","where is ananya now","where can i find ananya","do you know where ananya is","have you seen ananya","is ananya there","ananya elli","ananya elliddare","ananya iddara")) {
    if(!p.drinkOrdered)return Reply("ananya","Manjunatha has no newer information. "+Help(p));
    if(turn.Id=="served"||turn.Id=="return")return Choice(0,0);
    if(turn.Id=="directions")return Choice(0);
    return Reply("ananya","Manjunatha has no newer information. Ask Somanna at the library.");
   }
   if(Is(text,"where is the library","where is library","how do i get to the library","directions to the library","granthalaya ellide","granthalaya yelli ide")) {
    if(!p.drinkOrdered)return Reply(null,"Let's greet one another and finish the tea-or-water exchange first. "+Help(p));
    if(turn.Id=="served"||turn.Id=="return")return Choice(0);
    return Reply("directions");
   }
   if(Is(text,"no tea","no tea thank you","chaha beda","i don t want tea","i do not want tea"))
    return turn.Id=="order"?Choice(2):Reply("clarify",Help(p));
   if(Is(text,"thank you","thanks","bye","goodbye","dhanyavadagalu","sari dhanyavadagalu")) {
    for(int i=0;i<turn.Choices.Count;i++)if(turn.Choices[i].Next=="close")return Choice(i);
    return Reply(null,"You're welcome. You can leave with Esc, or continue. "+Help(p));
   }
   if(turn.Id=="clarify"&&text=="haudu neeru kodi")return Choice(0);
   // Explicit, unscored recovery phrases keep an offline lesson from trapping the story.
   if(Is(text,"tea please","water please")) {
    if(!p.greeted||turn.Id=="hello"||turn.Id=="help")return Reply(null,"Say hello first, then order tea or water.");
    if(turn.Id=="order")return Choice(text=="tea please"?0:1);
    if(turn.Id=="clarify")return Choice(text=="tea please"?1:0);
    return Reply(null,"We have already finished ordering. "+Help(p));
   }
   if(TeaAssessmentContract.PracticeAvailable(new LocalTeaConversation(),turn,p)&&Regex.IsMatch(text,@"\b(chaha|neeru|cha|tea|nanage|beku|kodi)\b"))return new TeaTopicPlan{Assess=true};
   return Reply(null,"I can talk about tea or water, Ananya and the library. "+Help(p));
  }
 }
}
