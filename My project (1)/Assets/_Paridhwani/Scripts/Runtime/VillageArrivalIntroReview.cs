#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Paridhwani
{
    // Native video decoding and UI review; no computer input or real saved visit.
    public sealed class VillageArrivalIntroReview : MonoBehaviour
    {
        TeaStallSession session;
        RenderTexture panelTexture;
        string output;
        int checks;
        bool background;
        VisualElement Root=>session.ui.GetComponent<UIDocument>().rootVisualElement;
        VillageArrivalIntro Intro=>session.GetComponent<VillageArrivalIntro>();
        public void Begin(TeaStallSession owner)
        {
            if(!owner.IsIsolatedTestVisit)throw new InvalidOperationException("Movie review requires an isolated save.");
            session=owner;background=Application.runInBackground;Application.runInBackground=true;
            output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../verification/Temp/ArrivalIntro",Screen.width+"x"+Screen.height));
            Directory.CreateDirectory(output);StartCoroutine(Guarded());
        }
        void OnDestroy()
        {
            Application.runInBackground=background;
            if(session && session.ui)session.ui.GetComponent<UIDocument>().panelSettings.targetTexture=null;
            if(panelTexture){panelTexture.Release();Destroy(panelTexture);}
        }
        void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);checks++;}
        void Visible(string name)
        {
            var item=Root.Q(name);Check(item!=null && item.resolvedStyle.display!=DisplayStyle.None,"Missing control: "+name);
            var r=item.worldBound;var bounds=Root.worldBound;
            Check(r.width>0 && r.height>0 && r.xMin>=bounds.xMin-1 && r.yMin>=bounds.yMin-1 && r.xMax<=bounds.xMax+1 && r.yMax<=bounds.yMax+1,"Control outside viewport: "+name+" "+r+" in "+bounds);
        }
        IEnumerator Frames(int count=8){for(int i=0;i<count;i++)yield return null;}
        IEnumerator Ready()
        {
            Intro.ReviewIgnoreFocus=true;
            float deadline=Time.realtimeSinceStartup+25;
            while(!Intro.Ready && !Intro.Failed && Time.realtimeSinceStartup<deadline)yield return null;
            Check(Intro.Ready && !Intro.Failed,"Bundled MP4 did not prepare in the native decoder.");
            if(Intro.Paused)session.ToggleArrivalIntroPause();
        }
        IEnumerator Seek(double seconds)
        {
            if(Intro.Paused)session.ToggleArrivalIntroPause();
            Intro.ReviewPlayer.time=seconds;
            float deadline=Time.realtimeSinceStartup+12;
            while((Intro.ReviewPlayer.frame<seconds*24-3 || Intro.PlaybackTime<seconds+.1) && Time.realtimeSinceStartup<deadline)yield return null;
            Check(Intro.PlaybackTime>=seconds,"Movie seek did not reach its requested scene.");
            if(!Intro.Paused)session.ToggleArrivalIntroPause();yield return Frames();
        }
        IEnumerator Shot(string name)
        {
            yield return Frames(12);
            var image=new Texture2D(panelTexture.width,panelTexture.height,TextureFormat.RGB24,false);
            var previous=RenderTexture.active;RenderTexture.active=panelTexture;
            try {image.ReadPixels(new Rect(0,0,panelTexture.width,panelTexture.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());}
            finally {RenderTexture.active=previous;Destroy(image);}
        }
        void SetSize(int width)
        {
            var panel=session.ui.GetComponent<UIDocument>().panelSettings;
            if(panelTexture){panel.targetTexture=null;panelTexture.Release();Destroy(panelTexture);}
            panelTexture=new RenderTexture(width,width*9/16,0,RenderTextureFormat.ARGB32);panelTexture.Create();panel.targetTexture=panelTexture;
        }
        IEnumerator Guarded()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0) {
                bool next;object current=null;
                try {next=stack.Peek().MoveNext();if(next)current=stack.Peek().Current;}
                catch(Exception error){File.WriteAllText(Path.Combine(output,"failure.txt"),error.ToString());Debug.LogException(error);UnityEditor.EditorApplication.isPlaying=false;yield break;}
                if(!next){stack.Pop();continue;}if(current is IEnumerator nested)stack.Push(nested);else yield return current;
            }
        }
        IEnumerator Run()
        {
            var sourceMutes=new Dictionary<AudioSource,bool>();
            foreach(var source in FindObjectsByType<AudioSource>())sourceMutes[source]=source.mute;
            session.BeginNewMainVisit();Intro.ReviewIgnoreFocus=true;yield return Ready();
            Check(session.ui.Mode=="arrival-intro" && session.player.blocked,"New visit must show the video before exploration.");
            Check(Intro.ReviewPlayer.url.EndsWith("/ArrivalMovie/arrival.mp4") && Intro.ReviewPlayer.audioTrackCount==1,"Movie must use the bundled file with an audio track.");
            // The normal CharacterController still settles onto the ground under a modal.
            float groundedDeadline=Time.realtimeSinceStartup+3;
            while(!session.player.Grounded && Time.realtimeSinceStartup<groundedDeadline)yield return null;
            Check(session.player.Grounded,"Arrival spawn did not settle onto the ground.");
            var playerPosition=session.player.transform.position;float heading=session.player.Heading;
            foreach(var pair in sourceMutes)if(pair.Key)Check(pair.Key.mute,"World audio leaked into the movie soundtrack.");
            bool oldMute=session.Progress.muted;
            session.Progress.muted=true;yield return Frames(2);Check(Intro.ReviewPlayer.GetTargetAudioSource(0).mute,"Movie ignored the player's mute setting.");
            session.Progress.muted=oldMute;
            session.AcknowledgeArrival();Check(!session.Progress.arrivalAcknowledged,"A stale welcome callback bypassed the movie.");
            yield return Seek(3);double frozen=Intro.PlaybackTime;yield return Frames(20);
            Check(Math.Abs(Intro.PlaybackTime-frozen)<.08,"Paused video clock advanced.");
            session.ReplayArrivalIntro();yield return Frames();Check(Intro.PlaybackTime<2,"Replay did not restart the video.");
            Intro.SendMessage("OnApplicationFocus",false);Check(Intro.Paused,"Focus loss must pause the video.");
            session.Close();Check(session.ui.Mode=="main-menu" && !session.Progress.arrivalIntroSeen && !Intro.Running,"Returning to title marked an unfinished movie complete.");
            session.ContinueMainVisit();yield return Ready();Check(session.ui.Mode=="arrival-intro","Continue bypassed an unfinished movie.");
            foreach(int width in new[]{960,1920}) {
                SetSize(width);
                foreach(float scale in new[]{1f,1.3f}) {
                    session.Progress.textScale=scale;session.SettingsChanged();session.ui.ShowArrivalIntro();session.ui.SetArrivalMovieTexture(Intro.ReviewPlayer.targetTexture);session.ui.SetArrivalMovieStatus("");
                    foreach(var cue in Intro.Timeline.cues) {
                        yield return Seek(cue.start+2);session.ui.SetArrivalMovieCaption(cue.caption);
                        yield return Shot(width+"-scale-"+scale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"-"+cue.id);
                        foreach(string name in new[]{"intro-movie","intro-caption","intro-pause","intro-replay","intro-skip"})Visible(name);
                        Check(Root.Q("intro-caption").worldBound.yMax<Root.Q("intro-actions").worldBound.yMin,"Subtitles overlap playback actions.");
                    }
                }
            }
            session.FinishArrivalIntro();Check(session.Progress.arrivalIntroSeen && session.ui.Mode=="arrival" && !session.Progress.arrivalAcknowledged,"Skipping must retain the welcome instructions.");
            foreach(var pair in sourceMutes)if(pair.Key)Check(pair.Key.mute==pair.Value,"Movie did not restore a world audio source's mute state.");
            foreach(int width in new[]{960,1920}) {
                SetSize(width);
                foreach(float scale in new[]{1f,1.3f}) {
                    session.Progress.textScale=scale;session.SettingsChanged();session.ui.ShowArrival();
                    yield return Shot(width+"-scale-"+scale.ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"-welcome");
                    Visible("arrival-enter");Visible("arrival-replay");
                }
            }
            session.Load();Check(session.ui.Mode=="arrival" && session.Progress.arrivalIntroSeen,"Restore must remember completion without acknowledging arrival.");
            session.Close();session.ContinueMainVisit();Check(session.ui.Mode=="arrival","Completed movie replayed on Continue.");
            session.PlayArrivalIntro();yield return Ready();
            Intro.ReviewPlayer.time=Intro.Timeline.duration-1;
            float until=Time.realtimeSinceStartup+12;while(session.ArrivalIntroPlaying && Time.realtimeSinceStartup<until)yield return null;
            Check(session.ui.Mode=="arrival" && !session.ArrivalIntroPlaying,"Native movie end did not return to welcome.");
            Check(Vector3.Distance(playerPosition,session.player.transform.position)<.05f && Mathf.Approximately(heading,session.player.Heading),"Video changed the saved player pose: "+playerPosition+" / "+heading+" to "+session.player.transform.position+" / "+session.player.Heading);
            Check(!session.Progress.greeted && !session.Progress.drinkOrdered && !session.Progress.directionsKnown,"Video awarded unearned story progress.");
            session.AcknowledgeArrival();Check(!session.ui.IsModal && !session.player.blocked,"Welcome did not release control.");
            session.Load();Check(!session.ui.IsModal,"An existing visit replayed the movie.");
            session.ui.ShowPresentationDemo();session.StartPresentationDemo(ReplayChapter.Arrival);Check(session.IsPresentationDemo && !session.ArrivalIntroPlaying,"Demo visit unexpectedly played the introduction.");
            File.WriteAllText(Path.Combine(output,"result.txt"),"ARRIVAL_MOVIE_REVIEW_OK\nChecks: "+checks+"\nNative MP4 prepare/audio/seek/pause/replay/end, save/skip/welcome, no story awards, demo isolation.\nCaptures: 960x540 and 1920x1080; text scale 1.0 and 1.3; all six scenes.\n");
            Debug.Log("ARRIVAL_MOVIE_REVIEW_OK "+output);UnityEditor.EditorApplication.isPlaying=false;
        }
    }
}
#endif
