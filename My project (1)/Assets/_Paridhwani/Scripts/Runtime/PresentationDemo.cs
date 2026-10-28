using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace Paridhwani {
 public static class PresentationDemo {
 public static readonly System.Collections.Generic.IReadOnlyList<ReplayChapter> Chapters=Array.AsReadOnly(new[]{ReplayChapter.Arrival,ReplayChapter.River,ReplayChapter.Deed,ReplayChapter.Council});
 public static bool Contains(ReplayChapter chapter) {foreach(var entry in Chapters)if(entry==chapter)return true;return false;}
 public static TeaProgress Seed(ReplayChapter chapter,TeaProgress preferences) {
 if(!Contains(chapter))throw new ArgumentOutOfRangeException(nameof(chapter));
 var prepared=ChapterReplay.Seed(chapter,preferences);
 // Frame the first conversation from the open lane, away from the timetable prompt.
 if(chapter==ReplayChapter.Arrival){prepared.position=new Vector3(0,.2f,-4.8f);prepared.heading=-32;}
 return prepared;
 }
 public static string Route(ReplayChapter chapter) {
 switch(chapter) {
 case ReplayChapter.Arrival:return "1. Approach the tea stall and talk to Manjunatha.\n2. Greet him and order tea or water.\n3. Ask where the library is.\n4. Open the notebook to show the words and source notes.";
 case ReplayChapter.River:return "1. Follow the river path to Giri's repair shed.\n2. Hear what he actually observed.\n3. Inspect the bicycle's frame, stand and carrier pocket.\n4. Read the booking and compare observation with inference.";
 case ReplayChapter.Deed:return "1. Enter the farm archive and inspect the family trunk.\n2. Read its lock reminder; help remains available.\n3. Open the trunk and read the original deed.\n4. Review its three clauses, then protect the original.";
 case ReplayChapter.Council:return "1. Approach the council president at the library court.\n2. Present the protected original.\n3. Explain the educational purpose, commercial-use restriction and community beneficiary.\n4. Show the council's response and the recorded outcome.";
 default:return "";
 }
 }
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
 public ReplayChapter PresentationChapter {get;private set;}
 public bool IsPresentationDemo => IsPracticeVisit && presentationDemo;
 public void StartPresentationDemo(ReplayChapter chapter) {
 if(ui.Mode!="presentation-demo" || !PresentationDemo.Contains(chapter) || (IsPracticeVisit && !IsPresentationDemo))return;
 if(!IsPracticeVisit) {
 Save();if(!LastSaveSucceeded){ui.ShowMainMenu(SaveMessage);return;}
 completedStoryStore=saves;
 }
 presentationDemo=true;PresentationChapter=chapter;
 Progress=PresentationDemo.Seed(chapter,Progress);PracticeChapterName="Presentation demo / "+ChapterReplay.Name(chapter);
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
 if(session.IsPresentationDemo) {
 Button(card,"Choose another demo scene",ShowPresentationDemo).name="settings-demo-scenes";
 Button(card,"Review this demo scene's instructions",ShowPresentationArrival,"quiet-button").name="settings-demo-guide";
 }
 }
 public void ShowPresentationArrival() {
 if(!session.IsPresentationDemo)return;
 var shell=Card("practice-arrival","paper");shell.name="demo-arrival-card";shell.style.height=Length.Percent(85);
 Add(shell,"PRESENTATION DEMO / TEMPORARY VISIT","eyebrow").style.flexShrink=0;
 var enter=Button(shell,"Enter the demo scene",session.Close);enter.name="demo-enter";enter.style.flexShrink=0;FocusLater(enter);
 var card=MenuScroll(shell,"demo-arrival-scroll");Add(card,ChapterReplay.Name(session.PresentationChapter),"title");
 Add(card,PresentationDemo.Description(session.PresentationChapter),"body");
 Add(card,"SUGGESTED ROUTE","section-title");Add(card,PresentationDemo.Route(session.PresentationChapter),"body");
 Add(card,"CURRENT OBJECTIVE","section-title");Add(card,session.Progress.Objective,"body");
 Add(card,"Your main visit is preserved. Earlier clues were supplied for this scene. Pause lets you switch demo scenes, review these instructions, or return to the main visit.","small");
 }
 void AddDisplaySettings(VisualElement card) {
 var exposure=new Slider("Display brightness",-.5f,1.5f){name="settings-brightness",value=PresentationPolicy.Brightness(session.Progress.brightness)};
 exposure.RegisterValueChangedCallback(e=>{session.Progress.brightness=e.newValue;session.SettingsChanged();});card.Add(exposure);
 Add(card,"Adjust for your screen. The overcast weather and world colours are preserved.","small");
 }
 }
}
