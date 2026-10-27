using UnityEditor;
using UnityEngine;
namespace Paridhwani.Editor {
 public sealed class SurfaceTextureImporter : AssetPostprocessor {
 void OnPreprocessTexture() {if(!assetPath.Contains("/_Paridhwani/Art/Textures/Surfaces/"))return;var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.mipmapEnabled=true;importer.maxTextureSize=512;importer.textureCompression=TextureImporterCompression.Compressed;}
 }
 public static partial class TeaStallSceneBuilder {
 static readonly System.Collections.Generic.Dictionary<string,Texture2D> surfaceTextures=new System.Collections.Generic.Dictionary<string,Texture2D>();
 public static readonly string[] RequiredSurfaceSources={"WetEarth","WeatheredStone","LimePlaster","ClayTile","Timber","CottonWeave","Bark"};
 [MenuItem("Paridhwani/Repair Surface Texture Imports")]
 public static void PrepareSurfaceTextures() {
 surfaceTextures.Clear();
 // Validate every source before importing or replacing any open scene.
 foreach(string source in RequiredSurfaceSources) {
 string file=System.IO.Path.Combine(Application.dataPath,"_Paridhwani","Art","Textures","Surfaces",source+".png");
 if(!System.IO.File.Exists(file))throw new System.IO.FileNotFoundException("Required surface PNG is missing from disk. Restore it from Git before generating the village.",file);
 }
 var imported=new System.Collections.Generic.Dictionary<string,Texture2D>();
 foreach(string source in RequiredSurfaceSources) {
 string path=Root+"/Art/Textures/Surfaces/"+source+".png";
 // A source can exist while its previous import artifact is unusable (for example, old .meta schemas).
 AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
 var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
 if(!texture)throw new System.InvalidOperationException("Surface PNG exists but Unity could not import it as a Texture2D: "+path+". Inspect texture import errors in the Console; the current scene has not been replaced.");
 if(texture.width!=512 || texture.height!=512)throw new System.InvalidOperationException("Surface must import at 512x512: "+path);
 imported.Add(source,texture);
 }
 foreach(var entry in imported)surfaceTextures.Add(entry.Key,entry.Value);
 Debug.Log("PARIDHWANI_SURFACE_IMPORTS_OK: seven source textures imported before scene generation.");
 }

 static void ConfigureSurfaceTexture(Material material,string name) {
 string texture=null;float scale=.7f;
 if(name=="Earth" || name=="Lane") {texture="WetEarth";scale=.45f;}
 else if(name=="Stone" || name=="StoneDark")texture="WeatheredStone";
 else if(name=="Plaster" || name=="Damp") {texture="LimePlaster";scale=.5f;}
 else if(name=="Clay" || name=="ClayLight") {texture="ClayTile";scale=1.4f;}
 else if(name=="Wood" || name=="WoodLight")texture="Timber";
 else if(name=="Trunk")texture="Bark";
 else if(name=="Shirt" || name=="Trousers" || name=="MarketCanvas" || name=="Sacking" || name.Contains("Cloth") || name.Contains("Shirt") || name=="AnanyaMustard" || name=="GuardKhaki") {texture="CottonWeave";scale=4;}
 if(texture!=null) {if(!surfaceTextures.TryGetValue(texture,out var asset) || !asset)throw new System.InvalidOperationException("Surface import preflight must succeed before creating materials: "+texture);material.SetTexture("_BaseMap",asset);material.SetFloat("_TextureStrength",.75f);material.SetFloat("_TextureScale",scale);}
 if(name=="Leaf" || name=="LeafLight")material.SetFloat("_WindStrength",.025f);
 }
 }
}
