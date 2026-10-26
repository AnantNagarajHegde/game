using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Paridhwani {
 public enum VillageQuality {Balanced,Low,High}
 public static class PresentationPolicy {
 public static VillageQuality ValidQuality(VillageQuality q) => q==VillageQuality.Low || q==VillageQuality.High?q:VillageQuality.Balanced;
 public static float Volume(float value) => !float.IsFinite(value)?1:System.Math.Min(1,System.Math.Max(0,value));
 public static void Copy(TeaProgress source,TeaProgress target) {target.english=source.english;target.roman=source.roman;target.muted=source.muted;target.textScale=source.textScale;target.sensitivity=source.sensitivity;target.reducedMotion=source.reducedMotion;target.invertY=source.invertY;target.masterVolume=source.masterVolume;target.quality=source.quality;}
 }
 public sealed class VillagePresentationSettings : MonoBehaviour {
 RenderPipelineAsset originalDefault,originalQuality;UniversalRenderPipelineAsset owned;
 VillageQuality applied=(VillageQuality)(-1);float originalVolume;bool captured;float originalLod;int originalVSync,originalFrameRate;
 public void Apply(TeaProgress p) {
 if(!captured) {captured=true;originalLod=QualitySettings.lodBias;originalVSync=QualitySettings.vSyncCount;originalFrameRate=Application.targetFrameRate;originalDefault=GraphicsSettings.defaultRenderPipeline;originalQuality=QualitySettings.renderPipeline;originalVolume=AudioListener.volume;var source=originalQuality?originalQuality:originalDefault;if(source is UniversalRenderPipelineAsset urp) {owned=Instantiate(urp);owned.name="Paridhwani runtime quality";GraphicsSettings.defaultRenderPipeline=owned;QualitySettings.renderPipeline=owned;}}
 AudioListener.volume=p.muted?0:PresentationPolicy.Volume(p.masterVolume);
 var quality=PresentationPolicy.ValidQuality(p.quality);if(applied==quality)return;applied=quality;
 if(owned) {owned.renderScale=quality==VillageQuality.Low?.7f:quality==VillageQuality.High?1:.9f;owned.msaaSampleCount=quality==VillageQuality.Low?1:quality==VillageQuality.High?4:2;owned.shadowDistance=quality==VillageQuality.Low?18:quality==VillageQuality.High?50:35;}
 QualitySettings.lodBias=quality==VillageQuality.Low?.65f:quality==VillageQuality.High?1.5f:1;QualitySettings.vSyncCount=1;Application.targetFrameRate=60;
 }
 void OnDestroy() {if(!captured)return;AudioListener.volume=originalVolume;QualitySettings.lodBias=originalLod;QualitySettings.vSyncCount=originalVSync;Application.targetFrameRate=originalFrameRate;if(owned) {if(GraphicsSettings.defaultRenderPipeline==owned)GraphicsSettings.defaultRenderPipeline=originalDefault;if(QualitySettings.renderPipeline==owned)QualitySettings.renderPipeline=originalQuality;Destroy(owned);}}
 }
 public sealed partial class TeaStallSession {
 VillagePresentationSettings presentationSettings;
 void ApplyPresentation() {if(!presentationSettings)presentationSettings=gameObject.AddComponent<VillagePresentationSettings>();presentationSettings.Apply(Progress);player.reducedMotion=Progress.reducedMotion;player.invertY=Progress.invertY;}
 }
}
