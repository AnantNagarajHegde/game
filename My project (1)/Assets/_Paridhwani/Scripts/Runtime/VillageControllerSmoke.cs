using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Motor commands are recorded rather than sent to hardware during isolated smoke tests.
    public sealed class RecordingHapticsGamepad : Gamepad
    {
        public int Starts {get;private set;}
        public float Low {get;private set;}
        public float High {get;private set;}
        public override void SetMotorSpeeds(float lowFrequency,float highFrequency)
        {Low=lowFrequency;High=highFrequency;if(lowFrequency>0 || highFrequency>0)Starts++;}
    }
    public sealed partial class VillageSmokeCheck
    {
        IEnumerator ControllerState(Gamepad pad,GamepadState state,float seconds=.12f)
        {
            if(!Application.isFocused) {
                Debug.Log("VILLAGE_CONTROLLER_NEEDS_FOCUS: foreground the smoke player to continue input checks");
                float deadline=Time.realtimeSinceStartup+30;
                while(!Application.isFocused && Time.realtimeSinceStartup<deadline)yield return null;
                Check(Application.isFocused,"controller input checks require the smoke player in the foreground");
            }
            InputSystem.QueueStateEvent(pad,state);
            yield return new WaitForSecondsRealtime(seconds);
        }
        IEnumerator ControllerPress(Gamepad pad,GamepadButton button)
        {
            yield return ControllerState(pad,new GamepadState().WithButton(button));
            yield return ControllerState(pad,new GamepadState());
        }
        IEnumerator ControllerChecks()
        {
            // Actual InputSystem state events exercise the same EventSystem/UI route as hardware.
            // The normal smoke run already owns an isolated save and never reads a player's visit.
            var pad=InputSystem.AddDevice<RecordingHapticsGamepad>("ParidhwaniRegressionPad");
            var recordingPad=pad;
            var root=session.ui.GetComponent<UIDocument>().rootVisualElement;
            var player=session.player;
            try
            {
                session.OpenTitle();yield return ControllerState(pad,new GamepadState());
                Check(root.focusController.focusedElement is Button,"title automatically focuses an enabled button");
                yield return ControllerPress(pad,GamepadButton.South);
                var inputModule=UnityEngine.EventSystems.EventSystem.current.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Check(!session.ui.IsModal,"controller continues a saved visit with one submit; mode="+session.ui.Mode+
                    " focused="+(root.focusController.focusedElement as Button)?.text+" appFocus="+Application.isFocused+
                    " submitEnabled="+inputModule.submit.action.enabled+" usingPad="+session.UsingController);
                player.Restore(new Vector3(0,.2f,-4.8f),0);
                yield return new WaitForSecondsRealtime(.3f);
                Vector3 before=player.transform.position;
                yield return ControllerState(pad,new GamepadState{leftStick=new Vector2(0,.6f)},.4f);
                yield return ControllerState(pad,new GamepadState());
                Check(Vector3.Distance(before,player.transform.position)>.1f,"analogue stick walks through the real controller");
                int motorStarts=pad.Starts;float previousFootstepVolume=session.Progress.footstepVolume;
                session.Progress.footstepVolume=0;session.SettingsChanged();
                yield return ControllerState(pad,new GamepadState{leftStick=Vector2.up},.65f);
                yield return ControllerState(pad,new GamepadState());
                Check(pad.Starts>motorStarts,"actual footsteps vibrate even with footstep audio turned off");
                Check(pad.Low==0 && pad.High==0,"footstep motor pulse finishes after stopping");
                session.Progress.footstepVolume=previousFootstepVolume;session.SettingsChanged();
                float yaw=player.Heading;
                yield return ControllerState(pad,new GamepadState{rightStick=Vector2.right},.3f);
                yield return ControllerState(pad,new GamepadState());
                Check(Mathf.Abs(Mathf.DeltaAngle(yaw,player.Heading))>15,"right stick turns the gameplay camera");
                motorStarts=pad.Starts;yield return ControllerPress(pad,GamepadButton.Start);
                Check(session.ui.Mode=="pause" && player.blocked,"Start opens Pause and blocks movement");
                Check(pad.Starts>motorStarts,"opening Pause produces controller feedback");
                var vibration=root.Q<Slider>("settings-controller-vibration");
                Check(vibration!=null && vibration.lowValue==0 && vibration.highValue==1,"settings expose the full vibration intensity range");
                session.PlayControllerFeedback(ControllerFeedback.Interaction);
                Check(pad.Low>0 || pad.High>0,"interaction feedback starts the active controller motors");
                vibration.value=0;
                Check(pad.Low==0 && pad.High==0,"setting vibration to zero immediately stops the motors");
                motorStarts=pad.Starts;session.PlayControllerFeedback(ControllerFeedback.Confirm);
                yield return ControllerPress(pad,GamepadButton.DpadDown);
                Check(pad.Starts==motorStarts && pad.Low==0 && pad.High==0,"disabled vibration suppresses UI and action pulses");
                vibration.value=.3f;
                Check(pad.Starts>motorStarts,"changing intensity previews the selected strength on the controller");
                session.Save();
                var codec=new UnityTeaProgressCodec();var restored=codec.Decode(codec.Encode(session.Progress));
                Check(Mathf.Abs(restored.controllerVibration-.3f)<.001f,"vibration intensity survives native JSON serialization");
                session.PlayControllerFeedback(ControllerFeedback.Interaction);
                session.SendMessage("OnApplicationFocus",false);
                Check(pad.Low==0 && pad.High==0,"focus loss explicitly stops both controller motors");
                session.PlayControllerFeedback(ControllerFeedback.Interaction);
                session.SendMessage("OnApplicationPause",true);
                Check(pad.Low==0 && pad.High==0,"application pause explicitly stops both controller motors");
                session.SendMessage("OnApplicationPause",false);
                session.PlayControllerFeedback(ControllerFeedback.Interaction);session.enabled=false;
                Check(pad.Low==0 && pad.High==0,"disabling the session stops both controller motors");session.enabled=true;
                vibration.value=ControllerHapticPolicy.DefaultIntensity;
                yield return new WaitForSecondsRealtime(.15f);
                Debug.Log("VILLAGE_HAPTICS_CHECKED: actual footsteps, audio independence, menus, actions, intensity/off, save serialization, focus/pause/disable shutdown");
                before=player.transform.position;yaw=player.Heading;
                yield return ControllerState(pad,new GamepadState{leftStick=Vector2.up,rightStick=Vector2.right},.2f);
                yield return ControllerState(pad,new GamepadState());
                var distance=player.transform.position-before;distance.y=0;
                Check(distance.magnitude<.01f && Mathf.Abs(Mathf.DeltaAngle(yaw,player.Heading))<.01f,"modal navigation cannot walk or rotate the world");
                var size=root.Q<Slider>("settings-text-size");size.Focus();float oldSize=size.value;
                yield return ControllerPress(pad,GamepadButton.DpadRight);
                Check(size.value>oldSize,"controller adjusts a settings slider");
                session.Progress.textScale=oldSize;session.SettingsChanged();
                // Reserve right stick for document scrolling, without changing focus.
                var focused=root.focusController.focusedElement;
                var scroll=root.Q<ScrollView>("pause-scroll");float offset=scroll.scrollOffset.y;
                yield return ControllerState(pad,new GamepadState{rightStick=Vector2.down},.3f);
                yield return ControllerState(pad,new GamepadState());
                Check(scroll.scrollOffset.y>offset && ReferenceEquals(focused,root.focusController.focusedElement),"right stick scrolls settings without navigating controls");
                var quality=root.Q<DropdownField>();quality.Focus();
                yield return ControllerPress(pad,GamepadButton.South);
                yield return ControllerPress(pad,GamepadButton.East);
                Check(session.ui.Mode=="pause","B dismisses a settings dropdown before leaving Pause");
                root.Q<Button>("pause-practice").Focus();
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.ui.Mode=="chapter-replay","controller opens chapter practice from Pause");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(session.ui.Mode=="pause" && (root.focusController.focusedElement as VisualElement)?.name=="pause-practice","controller chapter Back restores its launcher");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(!session.ui.IsModal,"B closes Pause once");
                yield return ControllerPress(pad,GamepadButton.Select);
                Check(session.ui.Mode=="notebook","View opens notebook");
                yield return ControllerPress(pad,GamepadButton.Select);
                Check(!session.ui.IsModal,"View closes notebook");
                var tea=Array.Find(session.villagers,n=>n.role==VillageNpcRole.TeaVendor);
                player.Restore(new Vector3(-4.2f,.28f,-1.65f),0);
                yield return new WaitForSecondsRealtime(.4f);
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.ui.Mode=="conversation" && session.Turn.Id=="hello","A interacts without answering the new turn");
                var turn=session.Turn;
                yield return new WaitForSecondsRealtime(.25f);
                Check(session.ui.KeyboardOpen,"controller chat opens its keyboard automatically");
                Check(root.Q<Button>("dialogue-choice-0")==null,"conversation does not expose old reply buttons");
                Check(root.Q<Button>("tea-key-0")?.text=="q" && root.Q<Button>("tea-key-9")?.text=="p" && root.Q<Button>("tea-key-10")?.text=="a","keyboard uses QWERTY rows");
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.TeaAssessmentInput=="q","A types the selected QWERTY key once");
                yield return ControllerState(pad,new GamepadState().WithButton(GamepadButton.RightShoulder),.35f);
                yield return ControllerState(pad,new GamepadState());
                Check(session.TeaAssessmentInput=="q ","holding RB inserts exactly one space");
                yield return ControllerPress(pad,GamepadButton.LeftShoulder);
                Check(session.TeaAssessmentInput=="q","LB deletes the previous character");
                yield return ControllerPress(pad,GamepadButton.RightShoulder);
                yield return ControllerPress(pad,GamepadButton.RightShoulder);
                Check(session.TeaAssessmentInput=="q  ","two separate RB presses insert two spaces");
                yield return ControllerPress(pad,GamepadButton.LeftShoulder);
                yield return ControllerPress(pad,GamepadButton.LeftShoulder);
                yield return ControllerState(pad,new GamepadState{leftStick=Vector2.right},.12f);
                yield return ControllerState(pad,new GamepadState());
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.TeaAssessmentInput=="qw","one left-stick selection step does not skip a QWERTY key");
                for(int i=0;i<12;i++)session.ui.RecordChatMessage("A longer chat message for the scrolling regression "+i);
                session.ui.ShowConversation();yield return new WaitForSecondsRealtime(.35f);
                var chat=root.Q<ScrollView>("dialogue-scroll");Check(chat!=null && chat.verticalScroller.highValue>0,"chat transcript can scroll");
                chat.scrollOffset=Vector2.zero;
                yield return ControllerState(pad,new GamepadState{rightStick=Vector2.down},.3f);
                yield return ControllerState(pad,new GamepadState());
                Check(chat.scrollOffset.y>0,"right stick scrolls chat while keyboard is open");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(!session.ui.KeyboardOpen && session.TeaAssessmentInput=="qw","hiding the keyboard preserves the draft");
                chat.scrollOffset=Vector2.zero;
                yield return ControllerState(pad,new GamepadState{rightStick=Vector2.down},.3f);
                yield return ControllerState(pad,new GamepadState());
                Check(chat.scrollOffset.y>0,"right stick scrolls chat with keyboard hidden");
                root.Q<Button>("dialogue-notebook").Focus();
                yield return ControllerPress(pad,GamepadButton.Select);
                Check(session.ui.Mode=="notebook","controller notebook suspends a conversation");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(session.ui.Mode=="conversation" && ReferenceEquals(turn,session.Turn),"B resumes the same conversation");
                yield return new WaitForSecondsRealtime(.2f);
                Check(session.ui.KeyboardOpen && session.TeaAssessmentInput=="qw","returning to chat reopens keyboard and preserves draft");
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
                // Exercise the real chat/turn code with an isolated in-process transport.
                yield return ControllerPress(pad,GamepadButton.East);
                var delayed=new System.Threading.Tasks.TaskCompletionSource<VillageModelTestReply>();
                session.VillageModelTestTransport=(current,input,token)=>delayed.Task;
                session.TeaAssessmentInput="A greeting that will be cancelled";session.AskTeaConversation();
                Check(session.TeaAssessmentBusy,"the delayed reply is pending");
                session.CancelTeaAssessment();
                delayed.SetResult(new VillageModelTestReply{Line=new TeaLine("","Namaskara","A stale greeting"),Choice=0,Feedback=""});
                yield return new WaitForSecondsRealtime(.2f);
                Check(!session.Progress.greeted && ReferenceEquals(session.Turn,turn),"cancelled late reply cannot advance the story");
                Check(session.ui.KeyboardOpen,"controller keyboard returns after a cancelled reply");
                session.VillageModelTestTransport=(current,input,token)=>System.Threading.Tasks.Task.FromResult(new VillageModelTestReply{Line=new TeaLine("","Namaskara","Welcome, Amogh."),Choice=0,Feedback=""});
                root.Q<TextField>("dialogue-tea-input").value="Namaskara nanna hesaru Amogh";
                yield return ControllerPress(pad,GamepadButton.West);
                Check(session.Progress.greeted && session.Turn!=null && session.Turn.Id=="order","a matched typed message advances the authored story automatically");
                Check(root.Query<Label>().ToList().Exists(label=>label.text=="Welcome, Amogh."),"the generated NPC reply remains in chat after progression");
                Check(root.Q<Button>("dialogue-choice-0")==null && root.Q<TextField>("dialogue-tea-input")!=null,"conversation uses the message field without story reply buttons");
                session.VillageModelTestTransport=null;
                yield return new WaitForSecondsRealtime(.2f);
#endif
                if(session.ui.KeyboardOpen)yield return ControllerPress(pad,GamepadButton.East);
                yield return ControllerPress(pad,GamepadButton.East);
                Check(!session.ui.IsModal,"B leaves conversation without opening Pause");
                session.Progress.greeted=false;
                // Device removal must stop exploration and offer the keyboard-accessible Pause menu.
                session.PlayControllerFeedback(ControllerFeedback.Interaction);
                InputSystem.RemoveDevice(pad);pad=null;
                yield return new WaitForSecondsRealtime(.15f);
                Check(session.ui.Mode=="pause" && !session.UsingController,"controller disconnect pauses exploration");
                Check(recordingPad.Low==0 && recordingPad.High==0,"controller removal stops its motors before releasing ownership");
                session.Close();
                Debug.Log("VILLAGE_CONTROLLER_CHECKED: virtual gamepad movement, camera, menus, settings, scrolling, interaction, dialogue, notebook and disconnect");
            }
            finally {
#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
                session.VillageModelTestTransport=null;
#endif
                if(pad!=null && pad.added)InputSystem.RemoveDevice(pad);
            }
        }
        IEnumerator ControllerTrunkChecks()
        {
            var pad=InputSystem.AddDevice<Gamepad>("ParidhwaniLockRegressionPad");
            var root=session.ui.GetComponent<UIDocument>().rootVisualElement;
            try
            {
                var plus=root.Query<Button>().ToList().FindAll(b=>b.text=="+");
                Check(plus.Count==4,"all four trunk digits expose controller buttons");
                plus[3].Focus();
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.TrunkAttempt=="1201" && (root.focusController.focusedElement as Button)?.name=="trunk-increase-3","trunk redraw preserves the focused digit; attempt="+session.TrunkAttempt+" focus="+(root.focusController.focusedElement as VisualElement)?.name+" appFocus="+Application.isFocused);
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.TrunkAttempt=="1202","repeated submit adjusts the same trunk digit once each");
                Debug.Log("VILLAGE_CONTROLLER_TRUNK_CHECKED");
            }
            finally {if(pad.added)InputSystem.RemoveDevice(pad);}
        }
        IEnumerator ControllerEvidenceChecks()
        {
            var pad=InputSystem.AddDevice<Gamepad>("ParidhwaniEvidenceRegressionPad");
            var root=session.ui.GetComponent<UIDocument>().rootVisualElement;
            try
            {
                // Let the newly opened document lay out and finish scheduled focus
                // restoration before sending a physical navigation event.
                yield return ControllerState(pad,new GamepadState(),.2f);
                var answers=root.Query<Button>(className:"evidence-answer").ToList();
                Check(answers.Count==3,"deed answers expose three focusable choices");answers[0].Focus();
                yield return ControllerState(pad,new GamepadState(),.15f);
                yield return ControllerPress(pad,GamepadButton.DpadDown);
                Check(ReferenceEquals(root.focusController.focusedElement,answers[1]),"D-pad navigates evidence choices; focused="+(root.focusController.focusedElement as VisualElement)?.name+" index="+answers.FindIndex(b=>ReferenceEquals(b,root.focusController.focusedElement)));
                yield return ControllerPress(pad,GamepadButton.South);
                Check(!session.Progress.deedPurposeKnown && session.ui.Mode=="evidence","wrong controller answer retains the retry screen");
                Debug.Log("VILLAGE_CONTROLLER_EVIDENCE_CHECKED");
            }
            finally {if(pad.added)InputSystem.RemoveDevice(pad);}
        }
    }
}
