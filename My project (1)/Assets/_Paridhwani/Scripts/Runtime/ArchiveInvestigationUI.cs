using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowRestTransition() {
 var card=Card("day-transition","paper");
 Add(card,"WEDNESDAY EVENING / GUEST ROOM","eyebrow");
 Add(card,"Rain through the night","title");
 Add(card,"You rest while Somanna keeps the reading room secure. Your evidence and notebook carry into Thursday morning.","body");
 card.style.opacity=0;
 card.schedule.Execute(()=>{ if(Mode=="day-transition") card.style.opacity=1; }).StartingIn(200);
 Button(card,"Continue to Thursday morning",()=>{ if(session.ResumeAvailable) ShowConversation(); else session.Close(); });
 }
 public void ShowArchiveActivity(string feedback="") {
 var card=Card("evidence","paper"); Class(card,"evidence-card");
 var header=new VisualElement(); Class(header,"row"); card.Add(header);
 Add(header,"COMMUNITY LIBRARY / CATALOGUE","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll);
 if(feedback!="") Add(scroll,feedback,"feedback"); Add(scroll,"Ananya's archive trail","title");
 if(!session.Progress.archiveShelfLocated) {
 Line(scroll,new TeaLine("ಎರಡನೇ ಕಪಾಟಿನ ಒಳಗೆ","Eradane kapaatina olage","Inside the second cupboard"));
 Add(scroll,"CATALOGUE: LAND / 1952 / FAMILY RECORDS. Where should you look?","body");
 Answers(scroll,new[]{new TeaLine("ಮೊದಲ ಕಪಾಟು","Modala kapaata","First cupboard"),new TeaLine("ಎರಡನೇ ಕಪಾಟಿನ ಒಳಗೆ","Eradane kapaatina olage","Inside the second cupboard"),new TeaLine("ಮೇಜಿನ ಕೆಳಗೆ","Mejina kelage","Under the desk")});
 } else {
 Add(scroll,"LAND RECORD / 1952 / SHANKARE GOWDA FAMILY","section-title");
 Line(scroll,new TeaLine("ಮೂಲ ಪತ್ರ ಗುಡ್ಡ ಮನೆಯಲ್ಲಿದೆ.","Moola patra Gudda Maneyallide.","The original document is at Gudda Mane."));
 Add(scroll,"Catalogue: the original registered deed is retained by the family at Gudda Mane. This entry is a reference, not the deed. Ananya consulted it on Monday.","body");
 if(!session.Progress.deedReferenceFound) {
 Add(scroll,"WHAT SHOULD BE SOUGHT AT GUDDA MANE?","section-title");
 Answers(scroll,new[]{new TeaLine("ಮೂಲ ಪತ್ರ","Moola patra","The original deed held by the family"),new TeaLine("ಬಸ್ ಚೀಟಿ","Bus cheeti","Proof that Ananya arrived"),new TeaLine("ಕೆಲಸ ಮುಗಿದಿದೆ","Kelasa mugidide","Proof the library is already protected")});
 } else { Add(scroll,"Recorded: the original deed is the research target. This reference proves neither arrival nor a final protection decision.","success"); Button(scroll,session.EvidenceBackLabel,session.Close); }
 }
 }
 }
}

