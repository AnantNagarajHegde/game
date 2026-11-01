using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Paridhwani {
 public sealed partial class TeaStallUI {
  readonly TeaKeyboardModel teaKeyboard=new TeaKeyboardModel();
  VisualElement teaKeyboardOverlay,teaKeyboardBackground,teaKeyboardReturnFocus;
  TextField teaKeyboardDraft;
  Button[] teaKeyboardKeys;
  Label teaKeyboardHelp;
  InputAction keyboardBackspaceAction,keyboardSpaceAction;
  readonly System.Collections.Generic.Queue<System.Collections.Generic.KeyValuePair<Gamepad,bool>> keyboardShoulderEvents=new System.Collections.Generic.Queue<System.Collections.Generic.KeyValuePair<Gamepad,bool>>();
  StyleLength savedTop,savedBottom,savedHeight,savedMaxHeight;
  int teaKeyboardOpenedFrame=-1;
  public bool KeyboardOpen=>teaKeyboardOverlay!=null;
  public void OpenTeaKeyboard(){
   if(KeyboardOpen||Mode!="conversation"||!session.ShowTeaAssessment||session.TeaAssessmentBusy)return;
   teaKeyboard.Open(session.TeaAssessmentInput);teaKeyboardOpenedFrame=Time.frameCount;
   root.AddToClassList("keyboard-open");
   teaKeyboardReturnFocus=root.focusController?.focusedElement as VisualElement;
   teaKeyboardBackground=root.Q("dialogue-card");
   if(teaKeyboardBackground!=null){var s=teaKeyboardBackground.style;savedTop=s.top;savedBottom=s.bottom;savedHeight=s.height;savedMaxHeight=s.maxHeight;s.top=0;s.bottom=StyleKeyword.Auto;s.height=Length.Percent(57/session.Progress.textScale);s.maxHeight=Length.Percent(57/session.Progress.textScale);}
   teaKeyboardOverlay=new VisualElement{name="tea-keyboard-overlay",focusable=true};
   teaKeyboardOverlay.style.position=Position.Absolute;teaKeyboardOverlay.style.left=0;teaKeyboardOverlay.style.right=0;teaKeyboardOverlay.style.bottom=0;teaKeyboardOverlay.style.height=Length.Percent(43);
   teaKeyboardOverlay.style.alignItems=Align.Stretch;
   modal.Add(teaKeyboardOverlay);
   var panel=new VisualElement{name="tea-keyboard-panel"};Class(panel,"keyboard-panel");panel.style.width=Length.Percent(100);panel.style.height=Length.Percent(100);panel.style.paddingLeft=14;panel.style.paddingRight=14;teaKeyboardOverlay.Add(panel);
   teaKeyboardDraft=root.Q<TextField>("dialogue-tea-input");
   teaKeyboardDraft?.RegisterValueChangedCallback(OnTeaKeyboardDraftChanged);
   teaKeyboardKeys=new Button[TeaKeyboardModel.KeyCount];
   for(int row=0;row<TeaKeyboardModel.RowCount;row++){
    var strip=new VisualElement();strip.style.flexDirection=FlexDirection.Row;strip.style.flexGrow=1;strip.style.flexBasis=0;panel.Add(strip);
    float inset=TeaKeyboardModel.RowInset(row);
    if(inset>0){var spacer=new VisualElement();spacer.style.flexGrow=inset;spacer.style.flexBasis=0;strip.Add(spacer);}
    for(int col=0;col<TeaKeyboardModel.RowLength(row);col++){
     int index=TeaKeyboardModel.RowStart(row)+col;
     var key=Button(strip,TeaKeyboardModel.Label(index),()=>{teaKeyboard.Select(index);RefreshTeaKeyboard();ApplyTeaKey(teaKeyboard.Activate(Time.frameCount));});
     key.name="tea-key-"+index;key.style.flexGrow=1;key.style.flexBasis=0;key.style.marginRight=4;key.style.marginBottom=4;key.style.fontSize=14;key.style.unityTextAlign=TextAnchor.MiddleCenter;
     key.focusable=false;teaKeyboardKeys[index]=key;
    }
    if(inset>0){var spacer=new VisualElement();spacer.style.flexGrow=inset;spacer.style.flexBasis=0;strip.Add(spacer);}
   }
   teaKeyboardHelp=Add(panel,"","small");teaKeyboardHelp.name="tea-keyboard-help";
   root.RegisterCallback<KeyDownEvent>(OnTeaKeyboardKey,TrickleDown.TrickleDown);
   keyboardShoulderEvents.Clear();
   keyboardBackspaceAction=new InputAction("Keyboard backspace",InputActionType.Button,"<Gamepad>/leftShoulder");
   keyboardSpaceAction=new InputAction("Keyboard space",InputActionType.Button,"<Gamepad>/rightShoulder");
   keyboardBackspaceAction.performed+=OnKeyboardBackspace;keyboardSpaceAction.performed+=OnKeyboardSpace;
   keyboardBackspaceAction.Enable();keyboardSpaceAction.Enable();
   RefreshTeaKeyboard();SuppressControllerSubmit();FocusLater(teaKeyboardOverlay);
  }
  void OnKeyboardBackspace(InputAction.CallbackContext e){if(KeyboardOpen && e.control.device is Gamepad pad)keyboardShoulderEvents.Enqueue(new System.Collections.Generic.KeyValuePair<Gamepad,bool>(pad,true));}
  void OnKeyboardSpace(InputAction.CallbackContext e){if(KeyboardOpen && e.control.device is Gamepad pad)keyboardShoulderEvents.Enqueue(new System.Collections.Generic.KeyValuePair<Gamepad,bool>(pad,false));}
  bool ConsumeShoulder(Gamepad pad,out bool backspace){
   while(keyboardShoulderEvents.Count>0){var press=keyboardShoulderEvents.Dequeue();if(press.Key==pad){backspace=press.Value;return true;}}
   // Covers a shoulder press already in flight when this keyboard was opened.
   if(pad.leftShoulder.wasPressedThisFrame){backspace=true;return true;}
   if(pad.rightShoulder.wasPressedThisFrame){backspace=false;return true;}
   backspace=false;return false;
  }
  void OnTeaKeyboardDraftChanged(ChangeEvent<string> e){teaKeyboard.SetText(e.newValue);}
  void RefreshTeaKeyboard(){
   if(!KeyboardOpen)return;
   for(int i=0;i<teaKeyboardKeys.Length;i++){teaKeyboardKeys[i].text=teaKeyboard.KeyLabel(i);teaKeyboardKeys[i].EnableInClassList("keyboard-selected",i==teaKeyboard.Selected);}
   string lb=Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad?"L1":"LB",rb=Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad?"R1":"RB";
   teaKeyboardHelp.text="Left stick: move   "+ControllerConfirm+": type   X / Square: send   "+ControllerBack+": hide   "+lb+": backspace   "+rb+": space   D-pad up: caps "+(teaKeyboard.Caps?"ON":"OFF")+"   D-pad down: "+(teaKeyboard.Symbols?"letters":"123 / symbols");
  }
  void OnTeaKeyboardKey(KeyDownEvent e){
   if(!KeyboardOpen)return;
   if(e.keyCode==KeyCode.Escape){e.StopImmediatePropagation();ApplyTeaKey(teaKeyboard.Dismiss(Time.frameCount));return;}
   if(e.keyCode==KeyCode.Return||e.keyCode==KeyCode.KeypadEnter){e.StopImmediatePropagation();ApplyTeaKey(teaKeyboard.Submit(Time.frameCount));return;}
   bool inField=false;for(var node=e.target as VisualElement;node!=null;node=node.parent)if(node==teaKeyboardDraft){inField=true;break;}
   if(inField)return;
   if(e.keyCode==KeyCode.Backspace){e.StopImmediatePropagation();ApplyTeaKey(teaKeyboard.Delete(Time.frameCount));return;}
   int x=e.keyCode==KeyCode.LeftArrow?-1:e.keyCode==KeyCode.RightArrow?1:0;
   int y=e.keyCode==KeyCode.UpArrow?-1:e.keyCode==KeyCode.DownArrow?1:0;
   if(x!=0||y!=0){e.StopImmediatePropagation();teaKeyboard.Move(0,0,Time.unscaledTimeAsDouble);if(teaKeyboard.Move(x,y,Time.unscaledTimeAsDouble))RefreshTeaKeyboard();return;}
   if(e.character!=0&&!char.IsControl(e.character)){e.StopImmediatePropagation();ApplyTeaKey(teaKeyboard.Type(e.character,Time.frameCount));}
  }
  public bool HandleTeaKeyboard(Gamepad pad){
   if(!KeyboardOpen)return false;
   if(pad==null||teaKeyboardOpenedFrame==Time.frameCount)return true;
   Vector2 direction=pad.leftStick.ReadValue();
   if(direction.sqrMagnitude<.1f)direction=new Vector2(pad.dpad.ReadValue().x,0);
   int x=Mathf.Abs(direction.x)>=.5f?(direction.x>0?1:-1):0;
   int y=Mathf.Abs(direction.y)>=.5f?(direction.y>0?-1:1):0;
   if(Mathf.Abs(direction.y)>Mathf.Abs(direction.x))x=0;
   if(teaKeyboard.Move(x,y,Time.unscaledTimeAsDouble)){RefreshTeaKeyboard();session.PlayControllerFeedback(ControllerFeedback.Navigation);}
   if(pad.buttonEast.wasPressedThisFrame||pad.startButton.wasPressedThisFrame)ApplyTeaKey(teaKeyboard.Dismiss(Time.frameCount));
   else if(pad.buttonWest.wasPressedThisFrame)ApplyTeaKey(teaKeyboard.Submit(Time.frameCount));
   else if(ConsumeShoulder(pad,out bool backspace))ApplyTeaKey(backspace?teaKeyboard.Delete(Time.frameCount):teaKeyboard.InsertSpace(Time.frameCount));
   else if(pad.dpad.up.wasPressedThisFrame){teaKeyboard.ToggleCaps();RefreshTeaKeyboard();}
   else if(pad.dpad.down.wasPressedThisFrame){teaKeyboard.ToggleSymbols();RefreshTeaKeyboard();}
   else if(pad.buttonSouth.wasPressedThisFrame)ApplyTeaKey(teaKeyboard.Activate(Time.frameCount));
   if(KeyboardOpen)ScrollController(pad.rightStick.ReadValue().y);
   return true;
  }
  void ApplyTeaKey(TeaKeyboardAction action){
   if(action==TeaKeyboardAction.None)return;
   session.TeaAssessmentInput=teaKeyboard.Text;
   if(action==TeaKeyboardAction.Edited){teaKeyboardDraft?.SetValueWithoutNotify(teaKeyboard.Text);session.PlayControllerFeedback(ControllerFeedback.Confirm);return;}
   CloseTeaKeyboard(true);
   if(action==TeaKeyboardAction.Submit&&!session.TeaAssessmentBusy)session.AskTeaConversation();
  }
  // Card rebuilds and leaving the conversation retire this overlay before deleting controls.
  public void CloseTeaKeyboard(bool restoreFocus=false){
   if(!KeyboardOpen)return;
   session.TeaAssessmentInput=teaKeyboard.Text;teaKeyboard.Close();root.UnregisterCallback<KeyDownEvent>(OnTeaKeyboardKey,TrickleDown.TrickleDown);teaKeyboardDraft?.UnregisterValueChangedCallback(OnTeaKeyboardDraftChanged);
   if(keyboardBackspaceAction!=null){keyboardBackspaceAction.performed-=OnKeyboardBackspace;keyboardBackspaceAction.Dispose();keyboardBackspaceAction=null;}
   if(keyboardSpaceAction!=null){keyboardSpaceAction.performed-=OnKeyboardSpace;keyboardSpaceAction.Dispose();keyboardSpaceAction=null;}
   keyboardShoulderEvents.Clear();
   teaKeyboardOverlay.RemoveFromHierarchy();teaKeyboardOverlay=null;teaKeyboardDraft=null;teaKeyboardKeys=null;
   root.RemoveFromClassList("keyboard-open");
   if(teaKeyboardBackground!=null){var s=teaKeyboardBackground.style;s.top=savedTop;s.bottom=savedBottom;s.height=savedHeight;s.maxHeight=savedMaxHeight;}teaKeyboardBackground=null;teaKeyboardHelp=null;
   var field=root.Q<TextField>("dialogue-tea-input");field?.SetValueWithoutNotify(session.TeaAssessmentInput);
   var restore=teaKeyboardReturnFocus;teaKeyboardReturnFocus=null;
   SuppressControllerSubmit();
   if(restoreFocus)FocusLater(restore!=null&&root.Contains(restore)&&restore.enabledInHierarchy?restore:field);
  }
 }
}

