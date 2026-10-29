using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Paridhwani
{
    public sealed partial class VillageSmokeCheck
    {
        IEnumerator ControllerState(Gamepad pad,GamepadState state,float seconds=.12f)
        {
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
            var pad=InputSystem.AddDevice<Gamepad>("ParidhwaniRegressionPad");
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
                float yaw=player.Heading;
                yield return ControllerState(pad,new GamepadState{rightStick=Vector2.right},.3f);
                yield return ControllerState(pad,new GamepadState());
                Check(Mathf.Abs(Mathf.DeltaAngle(yaw,player.Heading))>15,"right stick turns the gameplay camera");
                yield return ControllerPress(pad,GamepadButton.Start);
                Check(session.ui.Mode=="pause" && player.blocked,"Start opens Pause and blocks movement");
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
                yield return ControllerPress(pad,GamepadButton.Select);
                Check(session.ui.Mode=="notebook","controller notebook suspends a conversation");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(session.ui.Mode=="conversation" && ReferenceEquals(turn,session.Turn),"B resumes the same conversation");
                root.Q<Button>("dialogue-choice-0").Focus();
                yield return ControllerPress(pad,GamepadButton.South);
                Check(session.Progress.greeted,"UI submit selects a story reply");
                yield return ControllerPress(pad,GamepadButton.East);
                Check(!session.ui.IsModal,"B leaves conversation without opening Pause");
                session.Progress.greeted=false;
                // Device removal must stop exploration and offer the keyboard-accessible Pause menu.
                InputSystem.RemoveDevice(pad);pad=null;
                yield return new WaitForSecondsRealtime(.15f);
                Check(session.ui.Mode=="pause" && !session.UsingController,"controller disconnect pauses exploration");
                session.Close();
                Debug.Log("VILLAGE_CONTROLLER_CHECKED: virtual gamepad movement, camera, menus, settings, scrolling, interaction, dialogue, notebook and disconnect");
            }
            finally {if(pad!=null && pad.added)InputSystem.RemoveDevice(pad);}
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
                Check(session.TrunkAttempt=="1201" && (root.focusController.focusedElement as Button)?.text=="+","trunk redraw preserves the focused digit");
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
                var answers=root.Query<Button>(className:"evidence-answer").ToList();
                Check(answers.Count==3,"deed answers expose three focusable choices");answers[0].Focus();
                yield return ControllerPress(pad,GamepadButton.DpadDown);
                Check(ReferenceEquals(root.focusController.focusedElement,answers[1]),"D-pad navigates evidence choices");
                yield return ControllerPress(pad,GamepadButton.South);
                Check(!session.Progress.deedPurposeKnown && session.ui.Mode=="evidence","wrong controller answer retains the retry screen");
                Debug.Log("VILLAGE_CONTROLLER_EVIDENCE_CHECKED");
            }
            finally {if(pad.added)InputSystem.RemoveDevice(pad);}
        }
    }
}
