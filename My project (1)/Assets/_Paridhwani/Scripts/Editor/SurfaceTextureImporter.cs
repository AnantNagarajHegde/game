using UnityEditor;
using UnityEngine;
namespace Paridhwani.Editor {
 public sealed class SurfaceTextureImporter : AssetPostprocessor {
 void OnPreprocessTexture() {if(!assetPath.Contains("/_Paridhwani/Art/Textures/Surfaces/"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Compressed;}
 }
 public static partial class TeaStallSceneBuilder {
 static void ConfigureSurfaceTexture(Material material,string name) {
 string texture=null;float scale=.7f;
 if(name=="Earth" || name=="Lane") {texture="WetEarth";scale=.45f;}
 else if(name=="Stone" || name=="StoneDark")texture="WeatheredStone";
 else if(name=="Plaster" || name=="Damp") {texture="LimePlaster";scale=.5f;}
 else if(name=="Clay" || name=="ClayLight") {texture="ClayTile";scale=1.4f;}
 else if(name=="Wood" || name=="WoodLight")texture="Timber";
 else if(name=="Trunk")texture="Bark";
 else if(name=="Shirt" || name=="Trousers" || name=="MarketCanvas" || name=="Sacking" || name.Contains("Cloth") || name.Contains("Shirt") || name=="AnanyaMustard" || name=="GuardKhaki") {texture="CottonWeave";scale=4;}
 if(texture!=null) {var asset=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/Surfaces/"+texture+".png");if(!asset)throw new System.IO.FileNotFoundException("Missing surface source: "+texture);material.SetTexture("_BaseMap",asset);material.SetFloat("_TextureStrength",.75f);material.SetFloat("_TextureScale",scale);}
 if(name=="Leaf" || name=="LeafLight")material.SetFloat("_WindStrength",.025f);
 }
 }
}
