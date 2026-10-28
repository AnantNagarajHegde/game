using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Paridhwani {
 public enum VillageQuality {Balanced,Low,High}
 public static class PresentationPolicy {
 public const float DefaultBrightness=.7f;
 public static float Brightness(float value) => !float.IsFinite(value)?DefaultBrightness:System.Math.Min(1.5f,System.Math.Max(-.5f,value));
 public static VillageQuality ValidQuality(VillageQuality q) => q==VillageQuality.Low || q==VillageQuality.High?q:VillageQuality.Balanced;
 public static float Volume(float value) => !float.IsFinite(value)?1:System.Math.Min(1,System.Math.Max(0,value));
 public static void Copy(TeaProgress source,TeaProgress target) {target.english=source.english;target.roman=source.roman;target.muted=source.muted;target.textScale=source.textScale;target.sensitivity=source.sensitivity;target.reducedMotion=source.reducedMotion;target.invertY=source.invertY;target.masterVolume=source.masterVolume;target.ambienceVolume=source.ambienceVolume;target.footstepVolume=source.footstepVolume;target.quality=source.quality;target.brightness=Brightness(source.brightness);}
 }
 public sealed class VillagePresentationSettings : MonoBehaviour {
 RenderPipelineAsset originalDefault,originalQuality;UniversalRenderPipelineAsset owned;
 Volume displayVolume;VolumeProfile displayProfile;ColorAdjustments displayColor;
 VillageQuality applied=(VillageQuality)(-1);float originalVolume;bool captured;float originalLod,originalWind;int originalVSync,originalFrameRate;
 public void Apply(TeaProgress p) {
 if(!captured) {captured=true;originalWind=Shader.GetGlobalFloat("_VillageWindScale");originalLod=QualitySettings.lodBias;originalVSync=QualitySettings.vSyncCount;originalFrameRate=Application.targetFrameRate;originalDefault=GraphicsSettings.defaultRenderPipeline;originalQuality=QualitySettings.renderPipeline;originalVolume=AudioListener.volume;var source=originalQuality?originalQuality:originalDefault;if(source is UniversalRenderPipelineAsset urp) {owned=Instantiate(urp);owned.name="Paridhwani runtime quality";GraphicsSettings.defaultRenderPipeline=owned;QualitySettings.renderPipeline=owned;}}
 if(!displayProfile) {
 displayProfile=ScriptableObject.CreateInstance<VolumeProfile>();displayProfile.name="Paridhwani runtime display";
 displayColor=displayProfile.Add<ColorAdjustments>(false);
 displayVolume=gameObject.AddComponent<Volume>();displayVolume.isGlobal=true;displayVolume.priority=100;displayVolume.sharedProfile=displayProfile;
 }
 displayColor.postExposure.Override(PresentationPolicy.Brightness(p.brightness));
 Shader.SetGlobalFloat("_VillageWindScale",p.reducedMotion?0:1);
 AudioListener.volume=PresentationPolicy.Volume(p.masterVolume);
 var quality=PresentationPolicy.ValidQuality(p.quality);if(applied==quality)return;applied=quality;
 if(owned) {owned.renderScale=quality==VillageQuality.Low?.7f:quality==VillageQuality.High?1:.9f;owned.msaaSampleCount=quality==VillageQuality.Low?1:quality==VillageQuality.High?4:2;owned.shadowDistance=quality==VillageQuality.Low?18:quality==VillageQuality.High?50:35;}
 QualitySettings.lodBias=quality==VillageQuality.Low?.65f:quality==VillageQuality.High?1.5f:1;QualitySettings.vSyncCount=1;Application.targetFrameRate=60;
 }
 void OnDestroy() {if(displayVolume)Destroy(displayVolume);if(displayProfile){foreach(var component in displayProfile.components)if(component)Destroy(component);Destroy(displayProfile);}if(!captured)return;Shader.SetGlobalFloat("_VillageWindScale",originalWind);AudioListener.volume=originalVolume;QualitySettings.lodBias=originalLod;QualitySettings.vSyncCount=originalVSync;Application.targetFrameRate=originalFrameRate;if(owned) {if(GraphicsSettings.defaultRenderPipeline==owned)GraphicsSettings.defaultRenderPipeline=originalDefault;if(QualitySettings.renderPipeline==owned)QualitySettings.renderPipeline=originalQuality;Destroy(owned);}}
 }
 public sealed partial class TeaStallSession {
 VillagePresentationSettings presentationSettings;VillageDistanceCulling distanceCulling;
 void ApplyPresentation() {if(!presentationSettings)presentationSettings=gameObject.AddComponent<VillagePresentationSettings>();presentationSettings.Apply(Progress);if(!distanceCulling)distanceCulling=gameObject.AddComponent<VillageDistanceCulling>();distanceCulling.Configure(player.transform,Progress.quality);player.reducedMotion=Progress.reducedMotion;player.invertY=Progress.invertY;}
 }
}
