using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowReviewOutcome() {
 var card=Card("evidence","paper");Add(card,"AFTER THE COUNCIL MEETING","eyebrow");Add(card,"Wait for the formal review","title");
 Add(card,"The works remain suspended while the original and family records are reviewed. Advance to the later outcome when you are ready; you can keep exploring first.","body");
 if(session.Progress.workSuspended) Button(card,"Advance to the later review outcome",session.ConfirmReviewOutcome);
 Button(card,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 }
 public void ShowReviewArrival() {
 var card=Card("review-arrival","paper");Add(card,"SOME WEEKS LATER / KALYANAPURA","eyebrow");Add(card,"The library remains a library","title");
 Add(card,"The council has completed its review of the original and confirmed protection for the community library. The earlier suspension kept the site safe while that decision was made. Meet Somanna or Ananya in the reopened library court.","body");Button(card,"Return to the community",session.Close);
 }
 public void ShowEndingReflection() {
 var card=Card("evidence","paper");Class(card,"evidence-card");var scroll=new ScrollView();Class(scroll,"evidence-scroll");card.Add(scroll);Add(scroll,"A place in the village","title");
 if(!session.Progress.libraryProtected || !session.Progress.reviewOutcomeKnown) {Add(scroll,"Await the formal review outcome first.","body");Button(card,session.EvidenceBackLabel,session.Close);return;}
 Line(scroll,new TeaLine("ಮತ್ತೆ ಬನ್ನಿ.","Matte banni.","Come again."));
 Add(scroll,"The library has reopened after review. Both choices keep the same community outcome. Translation help and mistakes do not alter your welcome.","body");
 if(!session.Progress.storyCompleted) {Button(scroll,"Stay and help catalogue the library",()=>session.ChooseEnding(StoryEnding.Stay));Button(scroll,"Depart with an invitation to return",()=>session.ChooseEnding(StoryEnding.Depart));}
 if(session.Progress.endingChoice!=StoryEnding.None) {
 Add(scroll,session.Progress.endingChoice==StoryEnding.Stay?"You choose to stay. Somanna makes space at the archive desk, and Ananya has another stack of records waiting.":"You choose to depart. Shanthakka packs food for the journey and the community invites you back.","success");
 if(!session.Progress.storyCompleted) Button(scroll,"Finish this chapter and view the recap",session.FinishStory);else Button(scroll,"View credits and phrase recap",ShowCredits);
 }
 Button(card,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 }
 public void ShowCredits() {
 var card=Card("credits","paper");Class(card,"evidence-card");var scroll=new ScrollView();Class(scroll,"evidence-scroll");card.Add(scroll);
 Add(scroll,"PARIDHWANI","eyebrow");Add(scroll,"A library, a journey, a community","title");
 Add(scroll,session.Progress.endingChoice==StoryEnding.Stay?"You stayed to continue the archive.":"You left with an invitation to return.","body");
 Add(scroll,"PHRASES TO PRACTISE AGAIN","section-title");
 Word(scroll,"ನಮಸ್ಕಾರ","Namaskara","A respectful greeting");Word(scroll,"ಸ್ವಲ್ಪ ನೀರು ಕೊಡಿ","Swalpa neeru kodi","A little water, please");Word(scroll,"ಎಡಕ್ಕೆ ಹೋಗಿ","Edakke hogi","Go left");Word(scroll,"ಮೂಲ ಪತ್ರ","Moola patra","Original document");Word(scroll,"ಸಮುದಾಯಕ್ಕಾಗಿ","Samudaayakkaagi","For the community");Word(scroll,"ಮತ್ತೆ ಬನ್ನಿ","Matte banni","Come again");
 Add(scroll,"This recap records encountered phrases. Independent recall grows through practice.","body");
 Add(scroll,"CREDITS","section-title");Add(scroll,"Paridhwani / Kannada through a village story. Procedural prototype environment and authored local dialogue. Noto Sans and Noto Sans Kannada fonts, distributed under the SIL Open Font License. Characters, places and the deed are fictional.","body");
 Button(card,"Revisit the completed village",session.Close);if(session.IsPracticeVisit) Button(card,"Return to the main visit",session.ReturnToCompletedStory,"quiet-button"); else Button(card,"Choose a chapter to practise",ShowChapterReplay,"quiet-button").name="credits-practice";
 }
 public void ShowPracticeArrival() {
 if(session.IsPresentationDemo){ShowPresentationArrival();return;}
 var card=Card("practice-arrival","paper");Add(card,"SEPARATE PRACTICE VISIT","eyebrow");Add(card,session.PracticeChapterName,"title");Add(card,"Your main visit is preserved. This chapter has its own temporary save and the required earlier clues. Pause lets you return to the main visit; quitting and reopening also returns to the main save.","body");Button(card,"Begin practice",session.Close);
 }
 }
}
