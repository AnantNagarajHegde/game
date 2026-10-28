using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace Paridhwani {
 public static class PresentationDemo {
 public static readonly System.Collections.Generic.IReadOnlyList<ReplayChapter> Chapters=Array.AsReadOnly(new[]{ReplayChapter.Arrival,ReplayChapter.River,ReplayChapter.Deed,ReplayChapter.Council});
 public static bool Contains(ReplayChapter chapter) {foreach(var entry in Chapters)if(entry==chapter)return true;return false;}
 public static string Description(ReplayChapter chapter) {
 switch(chapter) {
 case ReplayChapter.Arrival:return "Meet Manjunatha, order tea or water, and ask for the library. Shows exploration and Kannada conversation.";
 case ReplayChapter.River:return "Speak to Giri and inspect the bicycle. Shows observation, language tasks and evidence reasoning.";
 case ReplayChapter.Deed:return "Open the family trunk and review the original deed. Shows the lock activity and document interpretation.";
 case ReplayChapter.Council:return "Present the protected original at the library court. Shows how the gathered evidence resolves the story.";
 default:return "";
 }
 }
 }
 public sealed partial class TeaStallSession {
 bool presentationDemo;
 public bool IsPresentationDemo => IsPracticeVisit && presentationDemo;
 public void StartPresentationDemo(ReplayChapter chapter) {
 if(ui.Mode!="presentation-demo" || !PresentationDemo.Contains(chapter) || (IsPracticeVisit && !IsPresentationDemo))return;
 if(!IsPracticeVisit) {
 Save();if(!LastSaveSucceeded){ui.ShowMainMenu(SaveMessage);return;}
 completedStoryStore=saves;
 }
 presentationDemo=true;
 Progress=ChapterReplay.Seed(chapter,Progress);PracticeChapterName="Presentation demo / "+ChapterReplay.Name(chapter);
 saves=new TeaSaveStore(Path.Combine(Application.temporaryCachePath,"paridhwani-demo-"+Guid.NewGuid().ToString("N")+".json"));
 EndForJourney();ResetActorsToOrigins();player.Restore(Progress.position,Progress.heading);returnTrail.Reset(Progress.position);ApplyState();Save();ui.ShowPracticeArrival();SetCursor(true);
 }
 }
 public sealed partial class TeaStallUI {
 readonly ChapterReplayNavigation demoNavigation=new ChapterReplayNavigation();
 public void ShowPresentationDemo() {
 if(session.IsPracticeVisit && !session.IsPresentationDemo)return;
 demoNavigation.OpenFrom(Mode=="presentation-demo"?"chapter-replay":Mode);
 var shell=Card("presentation-demo","paper");shell.name="demo-card";shell.style.height=Length.Percent(85);
 Add(shell,"PRESENTATION / SEPARATE TEMPORARY VISIT","eyebrow").style.flexShrink=0;
 var back=Button(shell,demoNavigation.BackLabel,ClosePresentationDemo,"quiet-button");back.name="demo-back";back.style.flexShrink=0;FocusLater(back);
 var card=MenuScroll(shell,"demo-scroll");Add(card,"Choose a scene to demonstrate","title");
 Add(card,"These scenes supply earlier clues for a short demonstration and contain story spoilers. Your main visit is saved separately. Return to it from Pause; demo progress never unlocks its chapters.","body");
 foreach(var entry in PresentationDemo.Chapters) {
 var chapter=entry;Button(card,ChapterReplay.Name(chapter),()=>session.StartPresentationDemo(chapter)).name="demo-scene-"+chapter;
 Add(card,PresentationDemo.Description(chapter),"small");
 }
 }
 public void ClosePresentationDemo() {
 if(demoNavigation.OriginMode=="main-menu"){ShowMainMenu();FocusLater(root.Q<Button>("title-demo"));}
 else {ShowPause();FocusLater(root.Q<Button>("settings-demo-scenes"));}
 }
 void AddPresentationActions(VisualElement card) {
 if(session.IsPresentationDemo)Button(card,"Choose another demo scene",ShowPresentationDemo).name="settings-demo-scenes";
 }
 void AddDisplaySettings(VisualElement card) {
 var exposure=new Slider("Display brightness",-.5f,1.5f){name="settings-brightness",value=PresentationPolicy.Brightness(session.Progress.brightness)};
 exposure.RegisterValueChangedCallback(e=>{session.Progress.brightness=e.newValue;session.SettingsChanged();});card.Add(exposure);
 Add(card,"Adjust for your screen. The overcast weather and world colours are preserved.","small");
 }
 }
}
