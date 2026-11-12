using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
namespace Paridhwani {
 // Device preferences stay separate from story saves and chapter/demo restores.
 public static class DeveloperDisplaySettings {
 const string EnabledKey="Paridhwani.Display.Override",ScaleKey="Paridhwani.Display.Scale";
 public static bool Enabled {get=>PlayerPrefs.GetInt(EnabledKey,0)==1;set{PlayerPrefs.SetInt(EnabledKey,value?1:0);PlayerPrefs.Save();}}
 public static float Scale {get=>SanitizeScale(PlayerPrefs.GetFloat(ScaleKey,1));set{PlayerPrefs.SetFloat(ScaleKey,SanitizeScale(value));PlayerPrefs.Save();}}
 public static float SanitizeScale(float value)=>float.IsFinite(value)?Mathf.Clamp(value,1,2):1;
 public static float RenderScale(VillageQuality quality)=>Enabled?Scale:quality==VillageQuality.Low?.7f:1;
 }
 public sealed partial class TeaStallUI {
 void AddDeveloperDisplaySettings(UnityEngine.UIElements.VisualElement parent) {
 var section=new UnityEngine.UIElements.Foldout {text="Developer options / display",name="developer-display"};parent.Add(section);
 Add(section,"Increase clarity for development and presentations. Supersampling increases GPU work; UI text stays at the window resolution.","small");
 var boost=new VillageSwitch("Override render resolution"){name="developer-resolution-override",value=DeveloperDisplaySettings.Enabled};section.Add(boost);
 var scales=new System.Collections.Generic.List<string>{"100% / Native","125% / Sharper","150% / Supersampled","200% / Maximum"};
 float[] values={1,1.25f,1.5f,2};int selected=0;
 for(int i=0;i<values.Length;i++)if(Mathf.Approximately(values[i],DeveloperDisplaySettings.Scale))selected=i;
 var scale=new UnityEngine.UIElements.DropdownField("3D render scale",scales,selected){name="developer-render-scale"};section.Add(scale);scale.SetEnabled(boost.value);
 boost.RegisterValueChangedCallback(e=>{DeveloperDisplaySettings.Enabled=e.newValue;scale.SetEnabled(e.newValue);session.SettingsChanged();});
 scale.RegisterValueChangedCallback(e=>{DeveloperDisplaySettings.Scale=values[scale.index];session.SettingsChanged();});
 var output=new UnityEngine.UIElements.DropdownField("Game window",new System.Collections.Generic.List<string>{"1280 x 720","1920 x 1080","2560 x 1440","3840 x 2160","Native display / borderless"},1){name="developer-output-resolution"};section.Add(output);
 var apply=Button(section,"Apply window resolution",()=>{
 int[] widths={1280,1920,2560,3840};int[] heights={720,1080,1440,2160};int i=output.index;
 if(i==4){var native=Screen.currentResolution;Screen.SetResolution(native.width,native.height,FullScreenMode.FullScreenWindow);}
 else Screen.SetResolution(widths[i],heights[i],FullScreenMode.Windowed);
 },"button");apply.name="developer-apply-resolution";
 output.SetEnabled(!Application.isEditor);apply.SetEnabled(!Application.isEditor);
 Add(section,Application.isEditor?"In the Unity Editor, set Game view to Free Aspect or 1920 x 1080 and Scale to 1x. Window controls apply in a standalone build; render scale works here too.":"Window changes apply immediately. Native display uses your monitor's resolution; supersampling increases detail inside that output.","small");
 var status=Add(section,"","small");status.name="developer-resolution-status";
 System.Action refresh=()=>{float factor=DeveloperDisplaySettings.RenderScale(session.Progress.quality);status.text=$"Output {Screen.width} x {Screen.height}  /  3D target {Mathf.RoundToInt(Screen.width*factor)} x {Mathf.RoundToInt(Screen.height*factor)} ({factor*100:0}%)";};
 refresh();status.schedule.Execute(refresh).Every(500);
 }
 }
 public enum VillageQuality {Balanced,Low,High}
 public static class PresentationPolicy {
 public const float DefaultBrightness=.7f;
 public static float Brightness(float value) => !float.IsFinite(value)?DefaultBrightness:System.Math.Min(1.5f,System.Math.Max(-.5f,value));
 public static VillageQuality ValidQuality(VillageQuality q) => q==VillageQuality.Low || q==VillageQuality.High?q:VillageQuality.Balanced;
 public static float Volume(float value) => !float.IsFinite(value)?1:System.Math.Min(1,System.Math.Max(0,value));
 public static void Copy(TeaProgress source,TeaProgress target) {target.english=source.english;target.roman=source.roman;target.muted=source.muted;target.textScale=source.textScale;target.sensitivity=source.sensitivity;target.reducedMotion=source.reducedMotion;target.invertY=source.invertY;target.masterVolume=source.masterVolume;target.ambienceVolume=source.ambienceVolume;target.footstepVolume=source.footstepVolume;target.quality=source.quality;target.brightness=Brightness(source.brightness);target.controllerVibration=ControllerHapticPolicy.Intensity(source.controllerVibration);}
 }
 public sealed class VillagePresentationSettings : MonoBehaviour {
 RenderPipelineAsset originalDefault,originalQuality;UniversalRenderPipelineAsset owned;
 Volume displayVolume;VolumeProfile displayProfile;ColorAdjustments displayColor;
 VillageQuality applied=(VillageQuality)(-1);float appliedScale=-1;float originalVolume;bool captured;float originalLod,originalWind;int originalVSync,originalFrameRate;
 public void Apply(TeaProgress p) {
 if(!captured) {captured=true;originalWind=Shader.GetGlobalFloat("_VillageWindScale");originalLod=QualitySettings.lodBias;originalVSync=QualitySettings.vSyncCount;originalFrameRate=Application.targetFrameRate;originalDefault=GraphicsSettings.defaultRenderPipeline;originalQuality=QualitySettings.renderPipeline;originalVolume=AudioListener.volume;var source=originalQuality?originalQuality:originalDefault;if(source is UniversalRenderPipelineAsset urp) {owned=Instantiate(urp);owned.name="Paridhwani runtime quality";GraphicsSettings.defaultRenderPipeline=owned;QualitySettings.renderPipeline=owned;}}
 if(!displayProfile) {
 displayProfile=ScriptableObject.CreateInstance<VolumeProfile>();displayProfile.name="Paridhwani runtime display";
 displayColor=displayProfile.Add<ColorAdjustments>(false);
 VillageArtDirection.Configure(displayProfile,displayColor);
 displayVolume=gameObject.AddComponent<Volume>();displayVolume.isGlobal=true;displayVolume.priority=100;displayVolume.sharedProfile=displayProfile;
 }
 displayColor.postExposure.Override(PresentationPolicy.Brightness(p.brightness));
 Shader.SetGlobalFloat("_VillageWindScale",p.reducedMotion?0:1);
 AudioListener.volume=PresentationPolicy.Volume(p.masterVolume);
 var quality=PresentationPolicy.ValidQuality(p.quality);
 float scale=DeveloperDisplaySettings.RenderScale(quality);
 if(applied==quality && Mathf.Approximately(appliedScale,scale))return;applied=quality;appliedScale=scale;
 if(owned) {owned.renderScale=scale;owned.msaaSampleCount=quality==VillageQuality.Low?1:4;owned.shadowDistance=quality==VillageQuality.Low?18:quality==VillageQuality.High?60:42;}
 QualitySettings.lodBias=quality==VillageQuality.Low?.65f:quality==VillageQuality.High?1.5f:1;QualitySettings.vSyncCount=1;Application.targetFrameRate=60;
 }
 void OnDestroy() {if(displayVolume)Destroy(displayVolume);if(displayProfile){foreach(var component in displayProfile.components)if(component)Destroy(component);Destroy(displayProfile);}if(!captured)return;Shader.SetGlobalFloat("_VillageWindScale",originalWind);AudioListener.volume=originalVolume;QualitySettings.lodBias=originalLod;QualitySettings.vSyncCount=originalVSync;Application.targetFrameRate=originalFrameRate;if(owned) {if(GraphicsSettings.defaultRenderPipeline==owned)GraphicsSettings.defaultRenderPipeline=originalDefault;if(QualitySettings.renderPipeline==owned)QualitySettings.renderPipeline=originalQuality;Destroy(owned);}}
 }
 public sealed partial class TeaStallSession {
 VillagePresentationSettings presentationSettings;VillageDistanceCulling distanceCulling;
 void ApplyPresentation() {if(!presentationSettings)presentationSettings=gameObject.AddComponent<VillagePresentationSettings>();presentationSettings.Apply(Progress);if(!distanceCulling)distanceCulling=gameObject.AddComponent<VillageDistanceCulling>();distanceCulling.Configure(player.transform,Progress.quality);player.reducedMotion=Progress.reducedMotion;player.invertY=Progress.invertY;}
 }
}
