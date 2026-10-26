using System;
namespace Paridhwani {
 public static class ArchiveInvestigation {
 public static bool FindShelf(TeaProgress p,int answer) { if(!p.libraryAccessGranted || answer!=1) return false; p.archiveShelfLocated=true; return true; }
 public static bool ReadReference(TeaProgress p,int answer) { if(!p.libraryAccessGranted || !p.archiveShelfLocated || answer!=0) return false; p.deedReferenceFound=true; return true; }
 }
 public sealed class ArchiveConversation : ITeaConversationProvider,IActivityConversationProvider {
 public TeaTurn Start(TeaProgress p) => Node(!p.libraryAccessGranted?"welcome":!p.deedReferenceFound?"search":!p.thursdayStarted?"rest":"morning");
 public TeaTurn ResumeAfterActivity(TeaTurn t,TeaProgress p) => Start(p);
 public TeaTurn Reply(TeaTurn t,int i,TeaProgress p) {
 if(i<0 || i>=t.Choices.Count) throw new ArgumentOutOfRangeException(nameof(i)); var c=t.Choices[i];
 if(c.Effect=="access") { if(!p.guddaFamilyLeadKnown) throw new InvalidOperationException("Bring the family lead first."); p.libraryAccessGranted=true; }
 if(c.Effect=="rest") { if(!p.deedReferenceFound) throw new InvalidOperationException("Read the reference first."); p.thursdayStarted=true; }
 return c.Next=="close"?null:Node(c.Next);
 }
 static TeaChoice C(string kn,string ro,string en,string next,string effect="",VillageActivity activity=VillageActivity.None) => new TeaChoice(kn,ro,en,next,effect){Activity=activity};
 static TeaTurn Node(string id) {
 TeaTurn t;
 switch(id) {
 case "welcome": t=new TeaTurn(id,"ಒಳಗೆ ಬನ್ನಿ.","Olage banni.","You found a family name, not proof of arrival. I have arranged access with my master key. Let's check Ananya's catalogue reference.",C("ಬಾಗಿಲು ತೆರೆಯಿರಿ.","Baagilu tereyiri.","Please open the reading room.","search","access"),C("ಮತ್ತೆ ಬರುತ್ತೇನೆ.","Matte baruttene.","I will return shortly.","close")); break;
 case "search": t=new TeaTurn(id,"ಎರಡನೇ ಕಪಾಟಿನ ಒಳಗೆ ನೋಡಿ.","Eradane kapaatina olage nodi.","Look inside the second cupboard. The catalogue points to a land-record reference. Read it with me, or at the desk inside.",C("ಕಡತವನ್ನು ನೋಡೋಣ.","Kadatavannu nodona.","Read the catalogue with Somanna.","search",activity:VillageActivity.ArchiveReference),C("ಒಳಗೆ ಹೋಗುತ್ತೇನೆ.","Olage hoguttene.","I will look inside the reading room.","close")); break;
 case "rest": t=new TeaTurn(id,"ನಾಳೆ ಬೆಳಿಗ್ಗೆ ಹೋಗೋಣ.","Naale beligge hogona.","The reference points to the original 1952 deed held by Shankare Gowda's family at Gudda Mane. It explains Ananya's research, not her arrival. Tomorrow we should ask at the panchayat. Ready to rest in the guest room?",C("ಸರಿ. ನಾಳೆ ಬೆಳಿಗ್ಗೆ.","Sari. Naale beligge.","Yes. Rest until Thursday morning.","morning","rest"),C("ಇನ್ನೂ ಸ್ವಲ್ಪ ಸಮಯ ಬೇಕು.","Innu swalpa samaya beku.","Not yet. I want to keep exploring.","close")); break;
 case "morning": t=new TeaTurn(id,"ಶುಭೋದಯ.","Shubhodaya.","Thursday morning. Your evidence is safe. Read the panchayat clearance notice at the public counter along the northern lane. Bring the archive reference.",C("ಕಡತವನ್ನು ಮತ್ತೆ ನೋಡೋಣ.","Kadatavannu matte nodona.","Read the archive reference again.","morning",activity:VillageActivity.ArchiveReference),C("ಧನ್ಯವಾದಗಳು.","Dhanyavadagalu.","Thank you.","close")); break;
 default: throw new ArgumentException(id);
 }
 t.Speaker="Somanna"; return t;
 }
 }
}
