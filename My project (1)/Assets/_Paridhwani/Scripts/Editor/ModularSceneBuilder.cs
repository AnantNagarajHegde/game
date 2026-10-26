using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 public const string BootstrapPath=Root+"/Scenes/Bootstrap.unity",VillagePath=Root+"/Scenes/Village_World.unity",ForestPath=Root+"/Scenes/Forest_Farm_World.unity";
 static GameObject Owner(UnityEngine.Object value) => value is GameObject go?go:value is Component component?component.gameObject:null;
 static Vector3 Centre(GameObject go) {var renderers=go.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return go.transform.position;var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds.center;}
 [MenuItem("Paridhwani/Create Additive World Package")]
 public static void GenerateModular() {
 // Generate already preserves a dirty review copy before replacing the authored benchmark.
 Generate();TeaStallChecks.Run();var source=SceneManager.GetActiveScene();var session=UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
 var sky=RenderSettings.skybox;var fog=RenderSettings.fog;var fogColor=RenderSettings.fogColor;var density=RenderSettings.fogDensity;var start=RenderSettings.fogStartDistance;var end=RenderSettings.fogEndDistance;var fogMode=RenderSettings.fogMode;var ambientMode=RenderSettings.ambientMode;var ambient=RenderSettings.ambientLight;var intensity=RenderSettings.ambientIntensity;
 var core=new HashSet<GameObject>{session.gameObject,session.player.gameObject,session.player.view.gameObject,session.ambience.rain.gameObject};
 foreach(var root in source.GetRootGameObjects())if(root.GetComponent<EventSystem>() || root.GetComponent<VillageLighting>() || (root.TryGetComponent<Light>(out var light) && light.type==LightType.Directional) || (root.TryGetComponent<Volume>(out var volume) && volume.isGlobal))core.Add(root);
 var captures=new Dictionary<FieldInfo,UnityEngine.Object[]>();
 foreach(var field in typeof(TeaStallSession).GetFields(BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly)) {
 if(field.Name=="villagers")continue;var type=field.FieldType;var value=field.GetValue(session);UnityEngine.Object[] refs=null;
 if(type.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(type.GetElementType()) && value is Array array)refs=array.Cast<UnityEngine.Object>().ToArray();
 else if(typeof(UnityEngine.Object).IsAssignableFrom(type) && value is UnityEngine.Object obj && obj)refs=new[]{obj};
 if(refs==null || refs.Length==0 || refs.Any(v=>!Owner(v)) || refs.All(v=>core.Contains(Owner(v))))continue;
 if(refs.Any(v=>core.Contains(Owner(v))))throw new InvalidOperationException("Mixed core/world array "+field.Name);
 captures.Add(field,refs);
 }
 var boot=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var village=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var forest=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
 foreach(var root in core)SceneManager.MoveGameObjectToScene(root,boot);
 var environmentRoots=source.GetRootGameObjects().FirstOrDefault(g=>g.name=="Environment");if(environmentRoots)foreach(var child in environmentRoots.transform.Cast<Transform>().ToArray())child.SetParent(null,true);
 foreach(var root in source.GetRootGameObjects()) {
 if(root==environmentRoots)continue;var centre=Centre(root);SceneManager.MoveGameObjectToScene(root,centre.x>=40 && centre.z>=-10 && centre.z<=55?forest:village);
 }
 var byScene=new Dictionary<Scene,List<WorldReference>>{{village,new List<WorldReference>()},{forest,new List<WorldReference>()}};
 foreach(var capture in captures) {
 var target=Owner(capture.Value[0]).scene;if(!byScene.ContainsKey(target) || capture.Value.Any(v=>Owner(v).scene!=target))throw new InvalidOperationException("Cross-world array cannot be serialized: "+capture.Key.Name);
 byScene[target].Add(new WorldReference{field=capture.Key.Name,values=capture.Value});capture.Key.SetValue(session,null);
 }
 session.villagers=Array.Empty<VillageNpc>();
 foreach(var entry in byScene) {
 var container=new GameObject("World bindings");SceneManager.MoveGameObjectToScene(container,entry.Key);var bindings=container.AddComponent<VillageWorldBindings>();bindings.references=entry.Value.ToArray();
 bindings.villagers=entry.Key.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<VillageNpc>(true)).ToArray();
 foreach(var npc in bindings.villagers){var character=npc.GetComponentInChildren<VillageCharacter>(true);if(character)character.lookTarget=null;}
 bindings.rivers=entry.Key.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RiverAmbience>(true)).ToArray();foreach(var river in bindings.rivers)river.preferences=null;
 }
 var loader=session.gameObject.AddComponent<VillageSceneBootstrap>();loader.session=session;loader.worldScenes=new[]{VillagePath,ForestPath};session.enabled=false;
 var atmosphere=session.gameObject.AddComponent<VillageAtmosphere>();atmosphere.sky=sky;atmosphere.fog=fog;atmosphere.fogColor=fogColor;atmosphere.fogDensity=density;atmosphere.fogStart=start;atmosphere.fogEnd=end;atmosphere.fogMode=fogMode;atmosphere.ambientMode=ambientMode;atmosphere.ambientColor=ambient;atmosphere.ambientIntensity=intensity;
 SaveModularScene(village,VillagePath);SaveModularScene(forest,ForestPath);SceneManager.SetActiveScene(boot);atmosphere.Apply();SaveModularScene(boot,BootstrapPath);
 ValidateModularScenes(boot,village,forest);
 EditorSceneManager.CloseScene(source,true);AssetDatabase.Refresh();
 foreach(var path in new[]{BootstrapPath,VillagePath,ForestPath})AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
 var scenes=new List<EditorBuildSettingsScene>();foreach(var path in new[]{BootstrapPath,VillagePath,ForestPath})scenes.Add(new EditorBuildSettingsScene(path,true){guid=new GUID(AssetDatabase.AssetPathToGUID(path))});
 foreach(var old in EditorBuildSettings.scenes)if(!scenes.Any(s=>s.path==old.path))scenes.Add(new EditorBuildSettingsScene(old.path,old.path==ScenePath?false:old.enabled));EditorBuildSettings.scenes=scenes.ToArray();AssetDatabase.SaveAssets();
 Debug.Log("PARIDHWANI_MODULAR_PACKAGE_READY: persistent bootstrap + village + forest/farm. Legacy benchmark retained.");
 }
 static void SaveModularScene(Scene scene,string path) {if(!EditorSceneManager.SaveScene(scene,path))throw new System.IO.IOException("Could not save "+path);}
 [MenuItem("Paridhwani/Build Additive World Package")]
 public static void BuildModular() {
 GenerateModular();var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{BootstrapPath,VillagePath,ForestPath},locationPathName="Builds/Modular/Paridhwani.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new InvalidOperationException("Additive build failed: "+result.summary.result);Debug.Log("PARIDHWANI_MODULAR_BUILD_OK");
 }
 }
}
