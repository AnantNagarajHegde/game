using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowDeedActivity(VillageActivity a,string feedback="") {
 var card=Card("evidence","paper"); Class(card,"evidence-card"); var header=new VisualElement(); Class(header,"row"); card.Add(header);
 Add(header,"GUDDA MANE / FAMILY TRUNK","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll); if(feedback!="") Add(scroll,feedback,"feedback");
 if(!DeedInvestigation.CanInspect(session.Progress)) { Add(scroll,"Ask Kamalamma about the original document after hearing Ananya's account.","body"); return; }
 if(a==VillageActivity.TrunkLock) {
 Add(scroll,"Twelve months, seven days","title"); Line(scroll,new TeaLine("ಹನ್ನೆರಡು ತಿಂಗಳು. ಏಳು ದಿನಗಳು.","Hanneradu tingalu. Elu dinagalu.","Twelve months. Seven days."));
 if(!session.Progress.trunkOpened) {
 var dials=new VisualElement();Class(dials,"row");scroll.Add(dials);
 for(int i=0;i<4;i++) {int digit=i;var column=new VisualElement();column.style.flexGrow=1;dials.Add(column);
 Button(column,"+",()=>{session.TrunkAttempt=DeedInvestigation.AdjustCode(session.TrunkAttempt,digit,1);ShowDeedActivity(a);});
 string display=session.TrunkAttempt!=null && session.TrunkAttempt.Length==4?session.TrunkAttempt.Substring(digit,1):"0";Add(column,display,"title");
 Button(column,"-",()=>{session.TrunkAttempt=DeedInvestigation.AdjustCode(session.TrunkAttempt,digit,-1);ShowDeedActivity(a);});}
 var field=new TextField("Four-digit lock"){value=session.TrunkAttempt,maxLength=4}; field.RegisterValueChangedCallback(e=>session.TrunkAttempt=e.newValue); scroll.Add(field);
 Add(scroll,"Two digits for months, then two for days. No attempt limit.","body"); Button(scroll,"Try the lock",session.SubmitTrunkCode);
 Button(scroll,"Use the reminder: 12 / 07",()=>{session.TrunkAttempt="1207";ShowDeedActivity(a,"12 followed by 07 gives 1207. Try the lock when ready.");},"quiet-button");
 } else { Add(scroll,"The trunk is open. The original deed is inside.","success"); Button(scroll,"Read the original deed",()=>session.OpenActivity(VillageActivity.DeedReview)); } return;
 }
 if(!session.Progress.trunkOpened) { Add(scroll,"Open the trunk first.","body"); return; }
 Add(scroll,"Original deed / 1952","title"); Add(scroll,"Story document: the family register and signatories match the library catalogue. The land is dedicated to community education and the library, commercial use is restricted, and the community is the beneficiary. The council must still review the original.","body");
 Add(scroll,"Fictional story document; not a real legal instrument.","small");
 if(!session.Progress.deedPurposeKnown) {
 Line(scroll,new TeaLine("ಶಿಕ್ಷಣಕ್ಕಾಗಿ.","Shikshanakkaagi.","For education.")); Add(scroll,"WHAT PURPOSE DOES THE DEED RECORD?","section-title");
 Answers(scroll,new[]{new TeaLine("ಶಿಕ್ಷಣ","Shikshana","Community education and the library"),new TeaLine("ಗೋದಾಮು","Godaamu","A private warehouse"),new TeaLine("ಬಸ್","Bus","A bus route")});
 } else if(!session.Progress.deedRestrictionKnown) {
 Line(scroll,new TeaLine("ವಾಣಿಜ್ಯ ಬಳಕೆ ಬೇಡ.","Vaanijya balake beda.","No commercial use.")); Add(scroll,"WHICH USE IS RESTRICTED?","section-title");
 Answers(scroll,new[]{new TeaLine("ಓದುವುದು","Oduvudu","Reading"),new TeaLine("ಶಿಕ್ಷಣ","Shikshana","Education"),new TeaLine("ವಾಣಿಜ್ಯ ಬಳಕೆ","Vaanijya balake","Commercial use")});
 } else if(!session.Progress.deedBeneficiaryKnown) {
 Line(scroll,new TeaLine("ಸಮುದಾಯಕ್ಕಾಗಿ.","Samudaayakkaagi.","For the community.")); Add(scroll,"WHO BENEFITS?","section-title");
 Answers(scroll,new[]{new TeaLine("ವ್ಯಾಪಾರಿ","Vyaapaari","A private trader alone"),new TeaLine("ಸಮುದಾಯ","Samudaaya","The community"),new TeaLine("ಪ್ರಯಾಣಿಕ","Prayaanika","A bus passenger")});
 } else { Add(scroll,"Reviewed: education, restricted commercial use, community beneficiary. This is evidence for review, not an automatic protection decision.","success"); if(!session.Progress.deedRetrieved) Button(scroll,"Place the original in its protective folder",session.ProtectDeed); else Add(scroll,"Original safely carried. Ask Ananya about returning together.","success"); }
 var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids); var en=new Toggle("English meaning"){value=session.Progress.english}; en.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowDeedActivity(a);}); aids.Add(en);
 var ro=new Toggle("Roman letters"){value=session.Progress.roman}; ro.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowDeedActivity(a);}); aids.Add(ro);
 }
 }
}
