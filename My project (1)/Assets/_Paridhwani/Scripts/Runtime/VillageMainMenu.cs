using UnityEngine;
namespace Paridhwani {
 // Failed quit attempts retain their original menu, including retries from the error card.
 public sealed class VisitExitNavigation {
 public string OriginMode {get;private set;}="pause";
 public bool TitleSettings {get;private set;}
 public void Begin(string mode,bool titleSettings) {
 if(mode=="quit-save-error")return;
 OriginMode=mode=="main-menu" || mode=="arrival"?mode:"pause";
 TitleSettings=OriginMode=="pause" && titleSettings;
 }
 public string ReturnMode(bool arrivalAcknowledged) => OriginMode=="main-menu"?"main-menu":TitleSettings?"pause":!arrivalAcknowledged?"arrival":OriginMode;
 public string ReturnLabel(bool arrivalAcknowledged) => ReturnMode(arrivalAcknowledged)=="main-menu"?"Back to title":ReturnMode(arrivalAcknowledged)=="arrival"?"Back to welcome":"Back to Pause";
 public static bool CanAcknowledgeArrival(string mode) => mode=="arrival";
 }
 public sealed partial class TeaStallSession {
 bool skipQuitSave;
 readonly VisitExitNavigation exitNavigation=new VisitExitNavigation();
 public string QuitCancelLabel => exitNavigation.ReturnLabel(Progress.arrivalAcknowledged);
 public bool HasSavedVisit => saves!=null && saves.HasValidSave;
 public bool TitleSettings {get;private set;}
 public void OpenTitle() {EndForJourney();TitleSettings=false;ui.ShowMainMenu();SetCursor(true);}
 public void OpenTitleSettings() {TitleSettings=true;ui.ShowPause();SetCursor(true);}
 public void ContinueMainVisit() {TitleSettings=false;EndForJourney();if(!Progress.arrivalAcknowledged)ShowArrivalOrIntro();}
 public void BeginNewMainVisit() {
 if(IsPracticeVisit)return;
 var previous=Progress;
 if(HasSavedVisit){Save();if(!LastSaveSucceeded){ui.ShowMainMenu(SaveMessage);return;}if(!saves.Archive(previous,out var archiveError)){ui.ShowMainMenu(archiveError);return;}}
 var next=new TeaProgress();PresentationPolicy.Copy(previous,next);
 if(!saves.Write(next,out var error)){ui.ShowMainMenu(error);return;}
 EndForJourney();ResetActorsToOrigins();Progress=next;conversation.ResetVisit();player.Restore(next.position,next.heading);returnTrail.Reset(next.position);ApplyState();Save();ShowArrivalOrIntro();
 }
 public void RequestQuit() {
 exitNavigation.Begin(ui.Mode,TitleSettings);
 Save();if(LastSaveSucceeded)ExitPlayer();else {ui.ShowQuitSaveError();SetCursor(true);}
 }
 public void CancelQuit() {
 if(ui.Mode!="quit-save-error")return;
 TitleSettings=exitNavigation.TitleSettings;
 string target=exitNavigation.ReturnMode(Progress.arrivalAcknowledged);
 if(target=="main-menu") {TitleSettings=false;ui.ShowMainMenu();}
 else if(target=="arrival") {TitleSettings=false;ui.ShowArrival();}
 else ui.ShowPause();
 SetCursor(true);
 }
 public void QuitWithoutSaving(){skipQuitSave=true;ExitPlayer();}
 static void ExitPlayer(){
#if UNITY_EDITOR
 UnityEditor.EditorApplication.isPlaying=false;
#else
 Application.Quit();
#endif
 }
 }
}
