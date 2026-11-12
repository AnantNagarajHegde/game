using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

namespace Paridhwani
{
    [Serializable] public sealed class ArrivalMovieCue
    {
        public string id,caption;
        public float start,end;
    }
    [Serializable] public sealed class ArrivalMovieTimeline
    {
        public int version,width,height;
        public float duration;
        public ArrivalMovieCue[] cues;
        public string Caption(double seconds)
        {
            if(cues!=null)foreach(var cue in cues)if(seconds>=cue.start && seconds<cue.end)return cue.caption??"";
            return "";
        }
    }
    public static class ArrivalIntroStory
    {
        public static bool ShouldPlay(TeaProgress progress,bool practice) =>
            progress!=null && !practice && !progress.arrivalAcknowledged && !progress.arrivalIntroSeen;
    }

    // Plays a bundled, rendered film. The world camera and actors are never animated
    // here. Subtitles follow the decoder clock and remain scalable in the game UI.
    public sealed class VillageArrivalIntro : MonoBehaviour
    {
        TeaStallSession session;
        VideoPlayer video;
        AudioSource audioSource;
        RenderTexture texture;
        ArrivalMovieTimeline timeline;
        readonly Dictionary<AudioSource,bool> worldMute=new Dictionary<AudioSource,bool>();
        bool running,paused,ready,failed;
        float prepareStarted;
        string lastCaption;
        public bool Running=>running;
        public bool Paused=>paused;
        public bool Ready=>ready;
        public bool Failed=>failed;
        public double PlaybackTime=>video && ready?video.time:0;
        public ArrivalMovieTimeline Timeline=>timeline;
#if UNITY_EDITOR
        internal VideoPlayer ReviewPlayer=>video;
        internal bool ReviewIgnoreFocus;
#endif
        bool HasFocus {
            get {
#if UNITY_EDITOR
                if(ReviewIgnoreFocus && session && session.IsIsolatedTestVisit)return true;
#endif
                return Application.isFocused;
            }
        }
        public void Begin(TeaStallSession owner)
        {
            Stop();session=owner;running=true;prepareStarted=Time.realtimeSinceStartup;lastCaption=null;
            session.ui.ShowArrivalIntro();
            var text=Resources.Load<TextAsset>("ArrivalIntro/MovieTimeline");
            try {timeline=text?JsonUtility.FromJson<ArrivalMovieTimeline>(text.text):null;}catch(Exception){timeline=null;}
            if(timeline==null || timeline.version!=1 || timeline.cues==null || timeline.cues.Length==0){Fail("Movie timing is unavailable.");return;}
            texture=new RenderTexture(timeline.width,timeline.height,0,RenderTextureFormat.ARGB32) {
                name="Paridhwani arrival movie",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,antiAliasing=1
            };
            texture.Create();var previous=RenderTexture.active;RenderTexture.active=texture;GL.Clear(true,true,Color.black);RenderTexture.active=previous;
            session.ui.SetArrivalMovieTexture(texture);
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            foreach(var source in FindObjectsByType<AudioSource>())if(source!=audioSource){worldMute[source]=source.mute;source.mute=true;}
            video=gameObject.AddComponent<VideoPlayer>();video.playOnAwake=false;video.isLooping=false;video.waitForFirstFrame=true;
            video.source=VideoSource.Url;
            string path=Application.streamingAssetsPath.TrimEnd('/','\\')+"/ArrivalMovie/arrival.mp4";
            video.url=path.Contains("://")?path:new Uri(path).AbsoluteUri;
            video.renderMode=VideoRenderMode.RenderTexture;video.targetTexture=texture;video.aspectRatio=VideoAspectRatio.FitInside;
            video.audioOutputMode=VideoAudioOutputMode.AudioSource;video.controlledAudioTrackCount=1;video.EnableAudioTrack(0,true);video.SetTargetAudioSource(0,audioSource);
            video.timeUpdateMode=VideoTimeUpdateMode.UnscaledGameTime;
            video.prepareCompleted+=Prepared;video.loopPointReached+=Completed;video.errorReceived+=PlaybackError;
            audioSource.mute=session.Progress.muted;
            session.ui.SetArrivalMovieStatus("Loading introduction…");video.Prepare();
        }
        void Prepared(VideoPlayer source)
        {
            if(!running || source!=video)return;
            ready=true;session.ui.SetArrivalMovieStatus("");
            if(!paused && HasFocus)source.Play();else {paused=true;session.ui.SetArrivalIntroPaused(true);}
        }
        void Completed(VideoPlayer source){if(running && source==video)session.FinishArrivalIntro();}
        void PlaybackError(VideoPlayer source,string message){if(running && source==video)Fail(message);}
        void Fail(string detail)
        {
            failed=true;ready=false;if(video)video.Stop();if(audioSource)audioSource.Stop();
            session.ui.SetArrivalMovieStatus("The introduction could not be played. Select Replay to try again, or Skip intro to continue.");
            Debug.LogWarning("Arrival movie unavailable: "+detail);
        }
        public void TogglePause()
        {
            if(!running || failed)return;
            paused=!paused;
            if(ready){if(paused)video.Pause();else video.Play();}
            session.ui.SetArrivalIntroPaused(paused);
        }
        public void Replay()
        {
            if(!running)return;
            if(failed){Begin(session);return;}
            paused=false;lastCaption=null;
            if(ready){video.time=0;video.Play();}
            session.ui.SetArrivalIntroPaused(false);
        }
        void Update()
        {
            if(!running)return;
            if(!session || !session.isActiveAndEnabled || !session.ui || session.ui.Mode!="arrival-intro"){Stop();return;}
            if(!HasFocus && !paused)PauseForFocus();
            if(!ready && !failed && Time.realtimeSinceStartup-prepareStarted>20)Fail("Video preparation timed out.");
            if(audioSource)audioSource.mute=session.Progress.muted;
            if(!ready)return;
            string caption=timeline.Caption(video.time);
            if(caption!=lastCaption){lastCaption=caption;session.ui.SetArrivalMovieCaption(caption);}
        }
        void PauseForFocus()
        {
            if(!running)return;
            paused=true;if(video && ready)video.Pause();session.ui.SetArrivalIntroPaused(true);
        }
        void OnApplicationFocus(bool value){if(!value)PauseForFocus();}
        void OnApplicationPause(bool value){if(value)PauseForFocus();}
        public void Stop()
        {
            running=false;ready=false;paused=false;failed=false;
            if(video){video.prepareCompleted-=Prepared;video.loopPointReached-=Completed;video.errorReceived-=PlaybackError;video.Stop();video.targetTexture=null;Destroy(video);video=null;}
            if(audioSource){audioSource.Stop();Destroy(audioSource);audioSource=null;}
            foreach(var pair in worldMute)if(pair.Key)pair.Key.mute=pair.Value;worldMute.Clear();
            if(session && session.ui)session.ui.SetArrivalMovieTexture(null);
            if(texture){texture.Release();Destroy(texture);texture=null;}
        }
        void OnDisable(){Stop();}
        void OnDestroy(){Stop();}
    }

    public sealed partial class TeaStallSession
    {
        VillageArrivalIntro arrivalIntro;
        public bool ArrivalIntroPlaying=>arrivalIntro && arrivalIntro.Running;
        void ShowArrivalOrIntro()
        {
            if(ArrivalIntroStory.ShouldPlay(Progress,IsPracticeVisit))PlayArrivalIntro();
            else {ui.ShowArrival();SetCursor(true);}
        }
        public void PlayArrivalIntro()
        {
            if(Progress.arrivalAcknowledged || IsPracticeVisit)return;
            if(!arrivalIntro)arrivalIntro=gameObject.AddComponent<VillageArrivalIntro>();
            StopVillageSpeech();SetCursor(true);arrivalIntro.Begin(this);
        }
        public void ToggleArrivalIntroPause(){if(ui.Mode=="arrival-intro" && arrivalIntro)arrivalIntro.TogglePause();}
        public void ReplayArrivalIntro(){if(ui.Mode=="arrival-intro" && arrivalIntro)arrivalIntro.Replay();}
        public void CancelArrivalIntro(){if(arrivalIntro)arrivalIntro.Stop();}
        public void FinishArrivalIntro()
        {
            if(ui.Mode!="arrival-intro" || !ArrivalIntroPlaying)return;
            CancelArrivalIntro();Progress.arrivalIntroSeen=true;Save();ui.ShowArrival();SetCursor(true);
        }
    }
}
