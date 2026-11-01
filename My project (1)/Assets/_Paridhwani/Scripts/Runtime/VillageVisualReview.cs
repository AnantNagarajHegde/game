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
            session.BeginNewMainVisit();yield return Shot("02-welcome");Visible("arrival-enter");
            session.AcknowledgeArrival();session.player.Restore(new Vector3(0,.2f,-4.8f),-32);
            yield return Shot("03-village");
            session.OpenNotebook();yield return Shot("04-notebook");session.Close();
            session.ui.ShowPause();yield return Shot("05-settings");Visible("pause-back");session.Close();
            VillageNpc vendor=null;
            foreach(var npc in session.villagers)if(npc && npc.role==VillageNpcRole.TeaVendor)vendor=npc;
            Check(vendor,"Missing tea vendor");
            session.player.Restore(new Vector3(-4.3f,.2f,-1.35f),0);
            session.StartConversation(vendor);
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
            }
            session.Close();session.ui.ShowPause();yield return Shot("09-settings-large-text");Visible("pause-back");
            session.Close();session.Progress.textScale=1;session.SettingsChanged();
            // Native after-rain sample uses the authored prepared chapter and its own save.
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Deed);session.Close();
            session.player.Restore(new Vector3(0,.2f,-4.8f),-32);
            yield return Shot("10-after-rain");
            Check(!VillageWeatherPolicy.Raining(session.Progress),"Prepared later chapter did not clear weather");
            File.WriteAllText(Path.Combine(output,"result.txt"),"UI_VISUAL_REVIEW_OK\nChecks: "+checks+"\nViewport: "+Screen.width+"x"+Screen.height+"\nText scales: 1.0, 1.3\nIsolated save; sample chat bubble only; no service request.\n");
            Debug.Log("UI_VISUAL_REVIEW_OK "+output+" checks="+checks);
            UnityEditor.EditorApplication.isPlaying=false;
        }
    }
}
#endif
