using UnityEngine.UIElements;
namespace Paridhwani {
 public sealed partial class TeaStallUI {
 public void ShowCouncilActivity(string feedback="") {
 var card=Card("evidence","paper"); Class(card,"evidence-card"); var header=new VisualElement(); Class(header,"row"); card.Add(header); Add(header,"LIBRARY COURT / COUNCIL REVIEW","eyebrow"); Button(header,session.EvidenceBackLabel+" [Esc]",session.Close,"quiet-button");
 var scroll=new ScrollView(); Class(scroll,"evidence-scroll"); card.Add(scroll); Add(scroll,"The original and its clauses","title"); if(feedback!="") Add(scroll,feedback,"feedback");
 if(!FinaleInvestigation.CanPresent(session.Progress)) { Add(scroll,"Return together with the protected and reviewed original, then confirm Friday preparations.","body"); return; }
 if(session.Progress.reviewOutcomeKnown) {Add(scroll,"Later review completed: the community library is protected. Friday's suspension was the interim step recorded in your notes.","success");return;}
 if(session.Progress.workSuspended) { Add(scroll,"Interim decision: clearance work is suspended while the council reviews the original. The community awaits the later review outcome.","success"); Button(scroll,"Review the later council outcome",()=>session.OpenActivity(VillageActivity.ReviewOutcome)); return; }
 if(!session.AtCouncil) { Add(scroll,"Review your notes here; stand near the council president to present the original and explain its clauses.","body"); return; }
 Button(scroll,session.FinaleHelp?"Hide Ananya's explanation":"Ask Ananya for help",()=>{session.FinaleHelp=!session.FinaleHelp;ShowCouncilActivity();},"quiet-button");
 if(session.FinaleHelp) Add(scroll,"Ananya: present the original, explain education, identify commercial use as restricted, then name the community as beneficiary. Asking for help does not change the ending.","feedback");
 if(!session.Progress.deedPresented) {
 Add(scroll,"WHICH DOCUMENT SHOULD THE COUNCIL REVIEW?","section-title");
 Answers(scroll,new[]{new TeaLine("ಮೂಲ ಪತ್ರ","Moola patra","The protected original matching the family register"),new TeaLine("ಬಸ್ ಚೀಟಿ","Bus cheeti","The bus booking"),new TeaLine("ಕಡತದ ಸೂಚಿ","Kadatada soochi","The catalogue without the original")});
 } else if(!session.Progress.finalePurposePresented) {
 Line(scroll,new TeaLine("ಶಿಕ್ಷಣಕ್ಕಾಗಿ ನೀಡಲಾಗಿದೆ.","Shikshanakkaagi needalaagide.","Dedicated for education."));
 Answers(scroll,new[]{new TeaLine("ಗೋದಾಮಿಗಾಗಿ","Godaamigaagi","For the warehouse"),new TeaLine("ಶಿಕ್ಷಣಕ್ಕಾಗಿ","Shikshanakkaagi","For community education and the library"),new TeaLine("ಪ್ರಯಾಣಕ್ಕಾಗಿ","Prayaanakkaagi","For a bus journey")});
 } else if(!session.Progress.finaleRestrictionPresented) {
 Line(scroll,new TeaLine("ವಾಣಿಜ್ಯ ಬಳಕೆ ಬೇಡ.","Vaanijya balake beda.","No commercial use."));
 Answers(scroll,new[]{new TeaLine("ವಾಣಿಜ್ಯ ಬಳಕೆ","Vaanijya balake","Commercial use is restricted"),new TeaLine("ಓದುವುದು","Oduvudu","Reading is restricted"),new TeaLine("ಶಿಕ್ಷಣ","Shikshana","Education is restricted")});
 } else if(!session.Progress.finaleBeneficiaryPresented) {
 Line(scroll,new TeaLine("ಸಮುದಾಯಕ್ಕಾಗಿ.","Samudaayakkaagi.","For the community."));
 Answers(scroll,new[]{new TeaLine("ವ್ಯಾಪಾರಿ","Vyaapaari","A private trader alone"),new TeaLine("ಪ್ರಯಾಣಿಕ","Prayaanika","A bus passenger"),new TeaLine("ಸಮುದಾಯ","Samudaaya","The community")});
 } else { Add(scroll,"Original, education purpose, commercial restriction and community beneficiary recorded. The council can now make its interim decision.","success"); Button(scroll,"Request the council's decision",session.RequestCouncilDecision); }
 var aids=new VisualElement(); Class(aids,"aids"); card.Add(aids); var en=new Toggle("English meaning"){value=session.Progress.english}; en.RegisterValueChangedCallback(e=>{session.Progress.english=e.newValue;session.SettingsChanged();ShowCouncilActivity();}); aids.Add(en); var ro=new Toggle("Roman letters"){value=session.Progress.roman}; ro.RegisterValueChangedCallback(e=>{session.Progress.roman=e.newValue;session.SettingsChanged();ShowCouncilActivity();}); aids.Add(ro);
 }
 }
}
