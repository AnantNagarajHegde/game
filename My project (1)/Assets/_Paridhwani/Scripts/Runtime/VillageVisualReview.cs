#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Opt-in visual/layout regression. No requests to the dialogue service or real save.
    public sealed class VillageVisualReview : MonoBehaviour
    {
        TeaStallSession session;
        string output;
        int checks;
        bool background;
        readonly List<InputDevice> devices=new List<InputDevice>();
        VisualElement Root=>session.ui.GetComponent<UIDocument>().rootVisualElement;
        public void Begin(TeaStallSession value)
        {
            if(!value.IsIsolatedTestVisit)throw new InvalidOperationException("Visual review requires an isolated visit.");
            session=value;background=Application.runInBackground;Application.runInBackground=true;
            foreach(var device in InputSystem.devices)
                if(device.enabled && (device is Keyboard || device is Mouse || device is Gamepad))devices.Add(device);
            foreach(var device in devices)InputSystem.DisableDevice(device);
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification/Temp/UIReview",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
            Directory.CreateDirectory(output);
            StartCoroutine(Guarded());
        }
        void OnDestroy()
        {
            Application.runInBackground=background;
            foreach(var device in devices)if(device.added && !device.enabled)InputSystem.EnableDevice(device);
        }
        void Check(bool condition,string message)
        {
            if(!condition)throw new InvalidOperationException(message);
            checks++;
        }
        void Visible(string name)
        {
            var element=Root.Q(name);
            Check(element!=null && element.resolvedStyle.display!=DisplayStyle.None,"Missing visible control: "+name);
            var r=element.worldBound;var bounds=Root.worldBound;
            Check(r.width>0 && r.height>0 && r.xMin>=bounds.xMin-2 && r.yMin>=bounds.yMin-2 && r.xMax<=bounds.xMax+2 && r.yMax<=bounds.yMax+2,"Control outside viewport: "+name+" "+r+" in "+bounds);
            if(element is Toggle)
            {
                var box=element.Q(className:"unity-toggle__input");
                Check(box!=null && box.worldBound.width>=10 && box.worldBound.height>=10,"Checkbox collapsed: "+name);
            }
        }
        IEnumerator Shot(string name)
        {
            for(int i=0;i<8;i++)yield return null;
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            try {File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());}
            finally {Destroy(texture);}
        }
        void CheckSettingsControls()
        {
            Root.Query<VillageSlider>().ForEach(slider => {
                var track=slider.Q(className:"unity-base-slider__tracker").worldBound;
                var thumb=slider.Q(className:"unity-base-slider__dragger").worldBound;
                Check(Mathf.Abs(track.center.y-thumb.center.y)<2,"Slider handle is not centred on its track: "+slider.label);
                Check(thumb.width>=14 && thumb.height>=14,"Slider handle is too small: "+slider.label);
                Check(!string.IsNullOrEmpty(slider.Q<Label>("slider-value").text),"Slider value is missing: "+slider.label);
            });
            Root.Query<VillageSwitch>().ForEach(toggle => {
                Check(toggle.Q<Label>("switch-state").text==(toggle.value?"On":"Off"),"Switch state does not match the stored value");
                var track=toggle.Q(className:"unity-toggle__input").worldBound;
                var thumb=toggle.Q(className:"switch-thumb").worldBound;
                Check(thumb.xMin>=track.xMin && thumb.xMax<=track.xMax && thumb.yMin>=track.yMin && thumb.yMax<=track.yMax,"Switch thumb escaped its track");
            });
        }
        IEnumerator Guarded()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool next;object current=null;
                try{next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);UnityEditor.EditorApplication.isPlaying=false;yield break;}
                if(!next){stack.Pop();continue;}
                if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        IEnumerator Run()
        {
            session.Progress.textScale=1;session.SettingsChanged();session.ui.ShowMainMenu();
            yield return Shot("01-title");Visible("title-start");Visible("title-settings");Visible("title-exit");
            session.BeginNewMainVisit();session.FinishArrivalIntro();yield return Shot("02-welcome");Visible("arrival-enter");
            session.AcknowledgeArrival();session.player.Restore(new Vector3(0,.2f,-4.8f),-32);
            yield return Shot("03-village");
            session.OpenNotebook();yield return Shot("04-notebook");session.Close();
            foreach(float menuScale in new[]{1f,1.3f})
            {
                session.Progress.textScale=menuScale;session.SettingsChanged();session.ui.ShowPause();
                foreach(string section in new[]{"visit","display","audio","controls","accessibility"})
                {
                    session.ui.ShowPauseSection(section);
                    yield return Shot("05-"+section+"-"+menuScale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                    Visible("pause-back");Visible("pause-nav-accessibility");
                    Check(Root.Q<ScrollView>("pause-scroll").contentViewport.worldBound.height>60,"Settings lost its reading area");
                    Check(!Root.Q<ScrollView>("pause-scroll").Contains(Root.Q("pause-back")),"Back must stay outside scrolling content");
                    if(section=="controls")
                    {
                        Root.Q<Slider>("settings-controller-vibration").Focus();
                        yield return Shot("05-vibration-focused-"+menuScale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                        Visible("settings-controller-vibration");
                    }
                    if(section=="accessibility")Visible("settings-text-size");
                    CheckSettingsControls();
                    if(section=="audio")
                    {
                        var sound=Root.Q<Toggle>("settings-mute");var volume=Root.Q<Slider>("settings-master-volume");
                        float savedVolume=volume.value;bool savedAudio=sound.value;
                        sound.value=true;volume.value=.42f;
                        yield return Shot("05-audio-on-"+menuScale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                        Check(!session.Progress.muted && Mathf.Abs(session.Progress.masterVolume-.42f)<.001f && volume.Q<Label>("slider-value").text=="42%","Audio controls did not update the saved level and readout together");
                        CheckSettingsControls();
                        sound.value=false;
                        yield return Shot("05-audio-off-"+menuScale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                        Check(session.Progress.muted && !volume.enabledInHierarchy && Mathf.Abs(volume.value-.42f)<.001f,"Turning audio off must disable controls without losing their levels");
                        CheckSettingsControls();volume.value=savedVolume;sound.value=savedAudio;
                    }
                }
                session.Close();
                Check(session.ui.Mode=="pause" && Root.Q("pause-notebook")!=null,"Back from a category must return to the visit page");
                session.OpenNotebook();yield return Shot("05-notebook-"+menuScale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                Visible("notebook-back");Visible("notebook-tab-story");Visible("notebook-tab-words");
                session.Close();Check(session.ui.Mode=="pause","Notebook opened from pause must return to pause");
                session.Close();Check(!session.ui.IsModal,"Back from the visit page must resume the world");
            }
            session.Progress.textScale=1;session.SettingsChanged();
            VillageNpc vendor=null;
            foreach(var npc in session.villagers)if(npc && npc.role==VillageNpcRole.TeaVendor)vendor=npc;
            Check(vendor,"Missing tea vendor");
            session.player.Restore(new Vector3(-4.3f,.2f,-1.35f),0);
            session.StartConversation(vendor);
            Check(!session.TeaAssessmentBusy && session.ConversationLine==session.Turn.Line,"Conversation must open immediately with an authored line and no model request");
            yield return new WaitForSecondsRealtime(1);
            session.ui.RecordChatMessage("Namaskara, nanna hesaru Amogh.");session.ui.ShowConversation();
            yield return Shot("06-conversation");Visible("dialogue-leave");Visible("dialogue-tea-input");Visible("dialogue-send");
            foreach(float scale in new[]{1f,1.3f})
            {
                session.Progress.textScale=scale;session.SettingsChanged();session.ui.ShowConversation();
                yield return Shot("07-chat-scale-"+scale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                Visible("dialogue-leave");Visible("dialogue-tea-input");Visible("dialogue-send");Visible("dialogue-roman");Visible("dialogue-english");
                var camera=session.player.view;var before=camera.rect;var beforeRotation=camera.transform.rotation;var beforePosition=camera.transform.position;
                session.ui.OpenTeaKeyboard();
                yield return Shot("08-keyboard-scale-"+scale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                Check(session.ui.KeyboardOpen,"Keyboard did not open");Visible("dialogue-tea-input");Visible("dialogue-send");Visible("dialogue-roman");Visible("dialogue-english");Visible("dialogue-notebook");
                var panel=Root.Q("dialogue-card");var keyboard=Root.Q("tea-keyboard-overlay");
                Check(panel.worldBound.yMax<=keyboard.worldBound.yMin+2,"Chat overlaps keyboard");
                Check(Root.Q("dialogue-tea-input").worldBound.yMax<=keyboard.worldBound.yMin+2,"Typed message is hidden by keyboard");
                Check(Root.Q("dialogue-notebook").worldBound.yMax<=keyboard.worldBound.yMin+2,"Notebook action is hidden by keyboard");
                Check(camera.rect==before,"Keyboard changed the world viewport");
                Check(Vector3.Distance(beforePosition,camera.transform.position)<.001f && Quaternion.Angle(beforeRotation,camera.transform.rotation)<.05f,"Keyboard moved the world camera");
                session.ui.CloseTeaKeyboard();
                session.ToggleConversationPhrases();
                yield return Shot("08-phrases-scale-"+scale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture));
                Visible("dialogue-phrases");Visible("dialogue-phrase-0");Visible("dialogue-tea-input");Visible("dialogue-send");
                Check(!session.ui.KeyboardOpen && session.ConversationPhrasesVisible,"Phrase choices must stay accessible without the keyboard");
                Root.Q("dialogue-phrase-list").Query<Button>().ForEach(button=> {
                    Check(button.worldBound.height>=45,"Phrase button collapsed below its readable content height");
                    button.Query<Label>().ForEach(label=> {
                        if(string.IsNullOrWhiteSpace(label.text))return;
                        Check(label.worldBound.yMin>=button.worldBound.yMin-2 && label.worldBound.yMax<=button.worldBound.yMax+2,
                            "Phrase text overflows its button");
                    });
                });
                session.ToggleConversationPhrases();
            }
            var initial=session.Turn;
            session.VillageModelTestTransport=(turn,input,token)=>System.Threading.Tasks.Task.FromException<VillageModelTestReply>(new InvalidOperationException("Isolated offline regression"));
            session.TeaAssessmentInput="Namaskara nanna hesaru Amogh";session.AskTeaConversation();
            yield return Shot("08-offline-phrases");
            Check(!session.Progress.greeted && !session.TeaAssessmentBusy && session.ConversationPhrasesVisible,"Unavailable AI must offer phrases without granting progress");
            Check(session.TeaAssessmentInput=="Namaskara nanna hesaru Amogh","Failed chat must preserve the typed message");
            int failedMessageCount=session.ui.ChatMessageCount;
            session.AskTeaConversation();yield return Shot("08-offline-retry");
            Check(session.ui.ChatMessageCount==failedMessageCount,"Retrying a failed delivery duplicated the player's bubble");
            Visible("dialogue-phrase-0");Visible("dialogue-leave");Visible("dialogue-send");
            session.ChooseConversationPhrase(initial,0);
            Check(session.Progress.greeted && session.Turn.Id=="order","An authored phrase must advance the introduction without a service");
            session.ToggleConversationPhrases();
            session.ChooseConversationPhrase(initial,0);
            Check(!session.Progress.drinkOrdered,"A callback from the previous turn must not select a choice in the new turn");
            session.ChooseConversationPhrase(session.Turn,1);
            Check(session.Progress.drinkOrdered && session.Progress.drink=="water" && !session.Progress.directionsKnown,"Assisted ordering must preserve the drink choice and clue gate");
            session.ToggleConversationPhrases();session.ChooseConversationPhrase(session.Turn,0);
            Check(session.Progress.directionsKnown && !session.Progress.libraryAccessGranted,"Offline introduction must reach the library route without granting archive access");
            session.VillageModelTestTransport=null;
            session.Close();session.ui.ShowPause();yield return Shot("09-settings-large-text");Visible("pause-back");
            session.Close();session.Progress.textScale=1;session.SettingsChanged();
            // Native after-rain sample uses the authored prepared chapter and its own save.
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Deed);session.Close();
            session.player.Restore(new Vector3(0,.2f,-4.8f),-32);
            yield return Shot("10-after-rain");
            Check(!VillageWeatherPolicy.Raining(session.Progress),"Prepared later chapter did not clear weather");
            File.WriteAllText(Path.Combine(output,"result.txt"),"UI_VISUAL_REVIEW_OK\nChecks: "+checks+"\nViewport: "+Screen.width+"x"+Screen.height+"\nText scales: 1.0, 1.3\nIsolated save; offline phrase progression and injected unavailable transport; no external service request.\n");
            Debug.Log("UI_VISUAL_REVIEW_OK "+output+" checks="+checks);
            UnityEditor.EditorApplication.isPlaying=false;
        }
    }
}
#endif
