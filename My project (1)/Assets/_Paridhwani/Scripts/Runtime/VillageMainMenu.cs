using UnityEngine;
namespace Paridhwani {
 public sealed partial class TeaStallSession {
 bool skipQuitSave;
 public bool HasSavedVisit => saves!=null && saves.HasValidSave;
 public bool TitleSettings {get;private set;}
 public void OpenTitle() {EndForJourney();TitleSettings=false;ui.ShowMainMenu();SetCursor(true);}
 public void OpenTitleSettings() {TitleSettings=true;ui.ShowPause();SetCursor(true);}
 public void ContinueMainVisit() {TitleSettings=false;EndForJourney();if(!Progress.arrivalAcknowledged){ui.ShowArrival();SetCursor(true);}}
 public void BeginNewMainVisit() {
 if(IsPracticeVisit)return;
 var previous=Progress;
 if(HasSavedVisit){Save();if(!LastSaveSucceeded){ui.ShowMainMenu(SaveMessage);return;}if(!saves.Archive(previous,out var archiveError)){ui.ShowMainMenu(archiveError);return;}}
 var next=new TeaProgress();PresentationPolicy.Copy(previous,next);
 if(!saves.Write(next,out var error)){ui.ShowMainMenu(error);return;}
 EndForJourney();ResetActorsToOrigins();Progress=next;player.Restore(next.position,next.heading);returnTrail.Reset(next.position);ApplyState();Save();ui.ShowArrival();SetCursor(true);
 }
 public void RequestQuit() {Save();if(LastSaveSucceeded)ExitPlayer();else {ui.ShowQuitSaveError();SetCursor(true);}}
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
