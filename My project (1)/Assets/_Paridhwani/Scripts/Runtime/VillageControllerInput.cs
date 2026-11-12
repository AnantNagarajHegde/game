using UnityEngine;
using UnityEngine.InputSystem;

namespace Paridhwani
{
    public sealed partial class TeaStallSession
    {
        public bool UsingController { get; private set; }
        Gamepad activeController;
        bool controllerLost;

        void OnEnable() { InputSystem.onDeviceChange+=ControllerChanged; }
        void OnDisable() { ReleaseControllerHaptics();InputSystem.onDeviceChange-=ControllerChanged; }
        void ControllerChanged(InputDevice device,InputDeviceChange change)
        {
            if(device==rumbleController && (change==InputDeviceChange.Disconnected ||
                change==InputDeviceChange.Removed || change==InputDeviceChange.Disabled))StopControllerHaptics();
            if(device!=activeController || (change!=InputDeviceChange.Disconnected &&
                change!=InputDeviceChange.Removed && change!=InputDeviceChange.Disabled)) return;
            activeController=null; controllerLost=UsingController; UsingController=false;
        }
        void UpdateInputDevice()
        {
            if(!Application.isFocused) return;
            var pad=Gamepad.current;
            bool padUsed=pad!=null && (pad.leftStick.ReadValue().sqrMagnitude>.04f ||
                pad.rightStick.ReadValue().sqrMagnitude>.04f || pad.dpad.ReadValue().sqrMagnitude>.1f ||
                pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
                pad.rightTrigger.isPressed || pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame ||
                pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame);
            bool keyboardUsed=Keyboard.current?.anyKey.wasPressedThisFrame==true;
            var mouse=Mouse.current;
            bool mouseUsed=mouse!=null && (mouse.delta.ReadValue().sqrMagnitude>1 ||
                mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                mouse.scroll.ReadValue().sqrMagnitude>0);
            bool wasUsingController=UsingController;
            if(padUsed) {UsingController=true;activeController=pad;}
            else if(keyboardUsed || mouseUsed) UsingController=false;
            if(UsingController && !wasUsingController && ui.Mode=="conversation" && ShowTeaAssessment && !TeaAssessmentBusy && !ConversationPhrasesVisible)ui.OpenTeaKeyboard();
            else if(!UsingController && wasUsingController && ui.KeyboardOpen)ui.CloseTeaKeyboard(true);
            if(ui.IsModal) Cursor.visible=!UsingController;
        }
        bool HandleController()
        {
            if(!Application.isFocused) return false;
            if(controllerLost)
            {
                controllerLost=false;
                if(!ui.IsModal) {ui.ShowPause();SetCursor(true);}
                return true;
            }
            var pad=activeController!=null && activeController.added?activeController:Gamepad.current;
            if(ui.KeyboardOpen)return ui.HandleTeaKeyboard(pad);
            if(pad==null || !UsingController) return false;
            if(ui.Mode=="conversation" && ShowTeaAssessment && !TeaAssessmentBusy && pad.buttonWest.wasPressedThisFrame){ui.OpenTeaKeyboard();return true;}
            if(ui.ControllerSubmitConsumed) return true;
            // UI Submit/Cancel/Move are dispatched once by InputSystemUIInputModule.
            // Only world interaction and global shortcuts belong to this polling path.
            if(pad.startButton.wasPressedThisFrame)
            {
                if(ui.Mode=="arrival-intro"){ToggleArrivalIntroPause();return true;}
                if(ui.IsModal) Close(); else {ui.ShowPause();SetCursor(true);}
                ui.SuppressControllerSubmit();return true;
            }
            if(pad.selectButton.wasPressedThisFrame &&
                VillageInputPolicy.ControllerNotebookShortcut(ui.Mode,ui.HasTextEntryFocus))
            {
                if(ui.Mode=="notebook") Close(); else OpenNotebook();
                ui.SuppressControllerSubmit();return true;
            }
            if(!ui.IsModal && pad.buttonSouth.wasPressedThisFrame)
            {Interact();return true;}
            if(ui.IsModal) {ui.EnsureControllerFocus();ui.ScrollController(pad.rightStick.ReadValue().y);}
            return false;
        }
    }
}
