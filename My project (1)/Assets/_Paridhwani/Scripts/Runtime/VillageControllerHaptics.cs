using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Paridhwani
{
    public enum ControllerFeedback { Step,JogStep,Menu,Navigation,Confirm,Interaction }

    public static class ControllerHapticPolicy
    {
        public const float DefaultIntensity=.5f;
        public static float Intensity(float value) => !float.IsFinite(value)?DefaultIntensity:Math.Clamp(value,0,1);
    }

    // Short, fading pulses share one motor owner. Overlapping actions never add their strengths.
    // The injected output lets timing and shutdown be checked without hardware or the Unity player.
    public sealed class ControllerHapticPulse
    {
        readonly Action<float,float> output;
        double began,until;
        float low,high;
        public bool Active {get;private set;}
        public ControllerHapticPulse(Action<float,float> output) {this.output=output??throw new ArgumentNullException(nameof(output));}
        public void Play(ControllerFeedback feedback,double now,float intensity)
        {
            float gain=ControllerHapticPolicy.Intensity(intensity);
            if(!double.IsFinite(now) || gain<=0) {Stop();return;}
            float l,h;double duration;
            switch(feedback)
            {
                case ControllerFeedback.Step:l=.14f;h=.07f;duration=.045;break;
                case ControllerFeedback.JogStep:l=.19f;h=.1f;duration=.055;break;
                case ControllerFeedback.Navigation:l=.04f;h=.09f;duration=.025;break;
                case ControllerFeedback.Menu:l=.1f;h=.18f;duration=.06;break;
                case ControllerFeedback.Confirm:l=.14f;h=.24f;duration=.07;break;
                case ControllerFeedback.Interaction:l=.2f;h=.28f;duration=.08;break;
                default:return;
            }
            if(!Active || now>=until || now<began) {low=0;high=0;until=now;}
            low=Math.Max(low,l*gain);high=Math.Max(high,h*gain);
            began=now;until=Math.Max(until,now+duration);Active=true;Write(low,high);
        }
        public void Tick(double now,bool eligible)
        {
            if(!Active)return;
            if(!eligible || !double.IsFinite(now) || now<began || now>=until) {Stop();return;}
            float fade=(float)(1-(now-began)/(until-began));Write(low*fade,high*fade);
        }
        public void Stop()
        {
            if(!Active)return;
            Active=false;low=high=0;Write(0,0);
        }
        void Write(float left,float right)
        {
            try {output(left,right);}
            // Unsupported or unavailable rumble must never interrupt input or the story.
            catch(Exception) {Active=false;low=high=0;try {output(0,0);}catch(Exception) {}}
        }
    }

    public sealed partial class TeaStallSession
    {
        ControllerHapticPulse controllerHaptics;
        Gamepad rumbleController;
        VillageFootsteps controllerFootsteps;
        bool controllerHapticsPaused;

        void BindControllerFootsteps()
        {
            if(controllerFootsteps || !player)return;
            controllerFootsteps=player.GetComponent<VillageFootsteps>();
            if(controllerFootsteps)controllerFootsteps.Stepped+=ControllerStep;
        }
        void ControllerStep(bool jogging) => PlayControllerFeedback(jogging?ControllerFeedback.JogStep:ControllerFeedback.Step);
        void SendControllerRumble(float low,float high) {rumbleController?.SetMotorSpeeds(low,high);}
        public void PlayControllerFeedback(ControllerFeedback feedback)
        {
            if(!isActiveAndEnabled || controllerHapticsPaused || !Application.isFocused || Progress==null)return;
            // UI events can precede Update on the first gamepad press after keyboard use.
            UpdateInputDevice();
            var pad=UsingController?Gamepad.current:null;
            float intensity=ControllerHapticPolicy.Intensity(Progress.controllerVibration);
            if(pad==null || !pad.added || !pad.enabled || intensity<=0) {StopControllerHaptics();return;}
            if(rumbleController!=pad) {StopControllerHaptics();rumbleController=pad;}
            if(controllerHaptics==null)controllerHaptics=new ControllerHapticPulse(SendControllerRumble);
            controllerHaptics.Play(feedback,Time.unscaledTimeAsDouble,intensity);
        }
        void UpdateControllerHaptics()
        {
            BindControllerFootsteps();
            bool eligible=UsingController && !controllerHapticsPaused && Application.isFocused &&
                Progress!=null && ControllerHapticPolicy.Intensity(Progress.controllerVibration)>0 &&
                rumbleController!=null && rumbleController==Gamepad.current && rumbleController.added && rumbleController.enabled;
            controllerHaptics?.Tick(Time.unscaledTimeAsDouble,eligible);
        }
        void StopControllerHaptics() {controllerHaptics?.Stop();rumbleController=null;}
        void ReleaseControllerHaptics()
        {
            StopControllerHaptics();
            if(controllerFootsteps)controllerFootsteps.Stepped-=ControllerStep;
            controllerFootsteps=null;
        }
        public void SetControllerVibration(float value)
        {
            Progress.controllerVibration=ControllerHapticPolicy.Intensity(value);
            StopControllerHaptics();SettingsChanged();
            PlayControllerFeedback(ControllerFeedback.Confirm);
        }
    }
}
