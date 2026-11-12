namespace Paridhwani {
 public sealed partial class TeaStallSession {
  readonly VillageConversationFlow conversation=new VillageConversationFlow();
  public string TeaAssessmentInput {get=>conversation.Input;set=>conversation.Input=value??"";}
  public string TeaAssessmentFeedback=>conversation.Feedback;
  public bool TeaAssessmentBusy=>conversation.Busy;
  public bool ShowTeaAssessment=>Turn!=null;
  public bool ConversationPhrasesVisible=>conversation.PhrasesVisible;
  public TeaLine ConversationLine=>conversation.Line??Turn?.Line;
  public void AskTeaConversation(){AskVillageModel();}
  void ResetTeaAssessment(){if(ui)ui.CloseTeaKeyboard();StopVillageSpeech();conversation.ResetTurn();}
  public void CancelTeaAssessment(){conversation.Cancel();if(ui&&ui.Mode=="conversation")ui.ShowConversation();}
  public void ToggleConversationPhrases(){
   if(!ui || ui.Mode!="conversation")return;
   ui.CloseTeaKeyboard();StopVillageSpeech();conversation.TogglePhrases();ui.ShowConversation();
  }
  public void ChooseConversationPhrase(TeaTurn expected,int index){
   if(!ui || ui.Mode!="conversation" || !ReferenceEquals(Turn,expected) || !ConversationPhrasesVisible || index<0 || index>=Turn.Choices.Count)return;
   ui.CloseTeaKeyboard();
   var phrase=Turn.Choices[index].Line;
   string speaker=ActiveNpc?ActiveNpc.displayName:Turn.Speaker;
   Choose(index);
   conversation.Remember(speaker,phrase.English,ConversationLine?.English??"");
  }
 }
}
