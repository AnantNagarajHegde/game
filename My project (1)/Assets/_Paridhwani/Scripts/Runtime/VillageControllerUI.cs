using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        int suppressSubmitFrame=-1;
        int controllerSubmitFrame=-1;
        public bool ControllerSubmitConsumed => controllerSubmitFrame==Time.frameCount;
        InputActionAsset controllerUIActions;
        public void SuppressControllerSubmit() {suppressSubmitFrame=Time.frameCount;}
        void BindControllerNavigation()
        {
            var module=EventSystem.current?.GetComponent<InputSystemUIInputModule>();
            if(module && module.actionsAsset)
            {
                // Unity's default UI map also navigates with the right stick. Reserve it for reading.
                controllerUIActions=Instantiate(module.actionsAsset);
                module.actionsAsset=controllerUIActions;
                var move=module.move?.action;
                if(move!=null) for(int i=0;i<move.bindings.Count;i++)
                    if(move.bindings[i].path.Contains("rightStick")) move.ApplyBindingOverride(i,"");
            }
            root.RegisterCallback<NavigationSubmitEvent>(e=> {
                if(KeyboardOpen){e.StopImmediatePropagation();return;}
                if(IsModal && Gamepad.current?.buttonSouth.wasPressedThisFrame==true) controllerSubmitFrame=Time.frameCount;
                if(!IsModal || Time.frameCount==suppressSubmitFrame) e.StopImmediatePropagation();
                else session.PlayControllerFeedback(ControllerFeedback.Confirm);
            },TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationMoveEvent>(e=> {
                if(KeyboardOpen){e.StopImmediatePropagation();return;}
                if(IsModal)session.PlayControllerFeedback(ControllerFeedback.Navigation);
            },TrickleDown.TrickleDown);
            root.RegisterCallback<NavigationCancelEvent>(e=> {
                if(KeyboardOpen){e.StopImmediatePropagation();return;}
                if(IsModal)session.PlayControllerFeedback(ControllerFeedback.Navigation);
            },TrickleDown.TrickleDown);
            // Let fields/dropdown popups handle Cancel first; unhandled B returns one screen.
            root.RegisterCallback<NavigationCancelEvent>(e=> {
                if(KeyboardOpen){e.StopImmediatePropagation();return;}
                if(!IsModal || Gamepad.current?.buttonEast.wasPressedThisFrame!=true) return;
                e.StopPropagation();
                if(Time.frameCount!=suppressSubmitFrame) session.Close();
            });
            root.RegisterCallback<FocusInEvent>(e=> {
                if(e.target is VisualElement element)
                    for(var parent=element.parent;parent!=null;parent=parent.parent)
                        if(parent is ScrollView scroll) {
                            if(scroll.contentContainer.Contains(element))scroll.ScrollTo(element);
                            break;
                        }
            });
        }
        static bool CanFocus(VisualElement element) => element!=null && element.focusable &&
            element.enabledInHierarchy && element.resolvedStyle.display!=DisplayStyle.None &&
            element.resolvedStyle.visibility==Visibility.Visible;
        public void EnsureControllerFocus()
        {
            if(!IsModal || modal==null) return;
            var focused=root.focusController?.focusedElement as VisualElement;
            // Popups are outside the card; preserve their focus until dismissed.
            if(CanFocus(focused) && focused.panel==root.panel) return;
            VisualElement target=null;
            modal.Query<VisualElement>().ForEach(element=> {
                if(target==null && CanFocus(element) && (element is Button || element is Toggle ||
                    element is Slider || element is DropdownField)) target=element;
            });
            target?.Focus();
        }
        System.Action PreserveModalFocus(string nextMode)
        {
            var focused=root.focusController?.focusedElement as VisualElement;
            if(Mode!=nextMode || focused==null || !modal.Contains(focused)) return null;
            // ScrollView creates/removes internal controls during layout. Count only the
            // authored fields, and prefer a stable name when a control provides one.
            while(focused!=null && focused!=modal && !AuthoredControl(focused))focused=focused.parent;
            if(focused==null || focused==modal)return null;
            int index=modal.Query<VisualElement>().ToList().FindAll(AuthoredControl).IndexOf(focused);
            if(index<0)return null;
            var type=focused.GetType();string name=focused.name;
            string text=focused is Button button?button.text:null;
            var oldScroll=focused.GetFirstAncestorOfType<ScrollView>();
            Vector2 offset=oldScroll?.scrollOffset??Vector2.zero;
            return ()=> {
                VisualElement target=string.IsNullOrEmpty(name)?null:modal.Q(name);
                if(target==null) {
                    var elements=modal.Query<VisualElement>().ToList().FindAll(AuthoredControl);
                    if(index>=elements.Count)return;
                    target=elements[index];
                }
                if(target.GetType()!=type || target.name!=name ||
                    (target is Button candidate && candidate.text!=text) || !CanFocus(target)) return;
                var scroll=target.GetFirstAncestorOfType<ScrollView>();
                if(scroll!=null)scroll.scrollOffset=offset;
                target.Focus();
            };
        }
        static bool AuthoredControl(VisualElement element) =>
            (element is Button || element is Toggle || element is Slider || element is DropdownField || element is TextField) &&
            element.GetFirstAncestorOfType<Scroller>()==null;
        public void ScrollController(float vertical)
        {
            if(!IsModal || Mathf.Abs(vertical)<.15f) return;
            var focused=root.focusController?.focusedElement as VisualElement;
            ScrollView target=Mode=="conversation"?root.Q<ScrollView>("dialogue-scroll"):null;
            for(var element=focused;element!=null;element=element.parent)
                if(target==null && element is ScrollView scroll) {target=scroll;break;}
            if(target==null) target=modal.Q<ScrollView>();
            if(target!=null) target.scrollOffset+=Vector2.down*(vertical*480*Time.unscaledDeltaTime);
        }
        string ControllerConfirm => Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad?"Cross":"A";
        string ControllerBack => Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad?"Circle":"B";
        void RefreshControllerHints()
        {
            if(controlsWalk==null) return;
            bool pad=session.UsingController;
            string jog=Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad?"R2":"RT";
            controlsWalk.text=pad?"Left stick  Walk    "+jog+"  Jog    Right stick  Look":"WASD  Walk    Shift  Jog    Mouse  Look";
            controlsActions.text=pad?ControllerConfirm+"  Interact    View / Share  Notebook    Menu / Options  Pause":"E  Interact    Tab  Notebook    Esc  Pause";
            promptKey.text=pad?ControllerConfirm:"E";
            controllerHelp.text="D-pad / LS: select   "+ControllerConfirm+": confirm   "+ControllerBack+": back   RS: scroll   View / Share: notes";
            controllerHelp.style.display=pad && IsModal && !KeyboardOpen && Mode!="conversation"?DisplayStyle.Flex:DisplayStyle.None;
        }
        Label controlsWalk,controlsActions,promptKey,controllerHelp;
        void ReleaseControllerNavigation()
        {
            if(controllerUIActions) {controllerUIActions.Disable();Destroy(controllerUIActions);}
        }
    }
}
