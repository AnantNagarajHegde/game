using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Paridhwani.Editor
{
    // Uses real device events and the authored input asset, without loading a visit.
    public static class VillageMenuInputChecks
    {
        [MenuItem("Paridhwani/Verify controller-only menu navigation")]
        public static void Run()
        {
            var source=AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if(!source)throw new InvalidOperationException("The project input asset was not found.");
            string original=source.ToJson();
            var actions=UnityEngine.Object.Instantiate(source);
            var keyboard=InputSystem.AddDevice<Keyboard>("ParidhwaniMenuRegressionKeyboard");
            var pad=InputSystem.AddDevice<Gamepad>("ParidhwaniMenuRegressionPad");
            var mouse=InputSystem.AddDevice<Mouse>("ParidhwaniMenuRegressionMouse");
            int checks=0;
            void Check(bool condition,string message)
            {if(!condition)throw new InvalidOperationException(message);checks++;}
            void Update(){InputSystem.Update();}
            try
            {
                var navigate=actions.FindAction("UI/Navigate",true);
                TeaStallUI.ConfigureControllerMenuNavigation(navigate);
                var walk=actions.FindAction("Player/Move",true);
                var point=actions.FindAction("UI/Point",true);
                var click=actions.FindAction("UI/Click",true);
                var scroll=actions.FindAction("UI/ScrollWheel",true);
                navigate.Enable();walk.Enable();point.Enable();click.Enable();scroll.Enable();

                // A connected controller must not make keyboard presses navigate menus.
                foreach(var key in new[]{Key.W,Key.A,Key.S,Key.D,Key.UpArrow,Key.DownArrow,Key.LeftArrow,Key.RightArrow})
                {
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState(key));Update();
                    Check(navigate.ReadValue<Vector2>().sqrMagnitude<.001f,key+" changed menu selection");
                    InputSystem.QueueStateEvent(keyboard,new KeyboardState());Update();
                }
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));Update();
                Check(walk.ReadValue<Vector2>().y>.5f,"Menu restrictions disabled WASD world movement");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());Update();
                string typed="";
                keyboard.onTextInput+=character=>typed+=character;
                foreach(char character in "wasd")InputSystem.QueueTextEvent(keyboard,character);
                Update();
                Check(typed=="wasd","Menu restrictions swallowed keyboard text");

                InputSystem.QueueStateEvent(pad,new GamepadState{leftStick=Vector2.up});Update();
                Check(navigate.ReadValue<Vector2>().y>.5f,"Left stick no longer navigates menus");
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadLeft));Update();
                Check(navigate.ReadValue<Vector2>().x<-.5f,"D-pad no longer navigates menus");
                InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=Vector2.down});Update();
                Check(navigate.ReadValue<Vector2>().sqrMagnitude<.001f,"Right stick changed selection instead of remaining available for scrolling");
                InputSystem.QueueStateEvent(pad,new GamepadState());Update();

                InputSystem.QueueStateEvent(mouse,new MouseState{position=new Vector2(123,234),scroll=new Vector2(0,120)}.WithButton(MouseButton.Left));Update();
                Check(point.ReadValue<Vector2>()==new Vector2(123,234),"Mouse pointer action was changed");
                Check(click.ReadValue<float>()>.5f,"Mouse clicks were disabled");
                Check(scroll.ReadValue<Vector2>().y>0,"Mouse scrolling was disabled");
                Check(source.ToJson()==original,"The runtime overrides changed the authored input asset");
                Debug.Log("VILLAGE_MENU_INPUT_CHECKS_OK: "+checks+" checks; keyboard navigation disabled, text/mouse/world movement and controller navigation preserved.");
            }
            finally
            {
                actions.Disable();UnityEngine.Object.DestroyImmediate(actions);
                InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(pad);InputSystem.RemoveDevice(keyboard);
            }
        }
    }
}
