using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    public sealed partial class TeaStallUI
    {
        Image introMovie;
        Label introCaption,introStatus;
        Button introPause;
        public void ShowArrivalIntro()
        {
            var shell=Card("arrival-intro","intro-screen");shell.name="intro-screen";
            introMovie=new Image {name="intro-movie",scaleMode=ScaleMode.ScaleToFit,pickingMode=PickingMode.Ignore};Class(introMovie,"intro-movie");shell.Add(introMovie);
            introStatus=Add(shell,"Loading introduction…","intro-status");introStatus.name="intro-status";
            introCaption=Add(shell,"","intro-subtitle");introCaption.name="intro-caption";
            introCaption.style.fontSize=20*session.Progress.textScale;introCaption.style.display=DisplayStyle.None;
            var actions=new VisualElement {name="intro-actions"};Class(actions,"intro-actions");shell.Add(actions);
            introPause=Button(actions,"Pause",session.ToggleArrivalIntroPause,"quiet-button");introPause.name="intro-pause";
            Button(actions,"Replay",session.ReplayArrivalIntro,"quiet-button").name="intro-replay";
            Button(actions,"Skip intro",session.FinishArrivalIntro,"quiet-button").name="intro-skip";
            actions.Query<Button>().ForEach(button=>{button.style.fontSize=14*session.Progress.textScale;button.style.minHeight=40*session.Progress.textScale;});
            Add(actions,"Space / Menu: pause · Esc / B: title","intro-controls-hint");
            FocusLater(introPause);
        }
        public void SetArrivalMovieTexture(RenderTexture value){if(introMovie!=null)introMovie.image=value;}
        public void SetArrivalMovieStatus(string value)
        {if(Mode!="arrival-intro" || introStatus==null)return;introStatus.text=value;introStatus.style.display=string.IsNullOrEmpty(value)?DisplayStyle.None:DisplayStyle.Flex;}
        public void SetArrivalMovieCaption(string value)
        {if(Mode!="arrival-intro" || introCaption==null)return;introCaption.text=value;introCaption.style.display=string.IsNullOrEmpty(value)?DisplayStyle.None:DisplayStyle.Flex;}
        public void SetArrivalIntroPaused(bool value)
        {if(Mode=="arrival-intro" && introPause!=null)introPause.text=value?"Resume":"Pause";}
    }
}
