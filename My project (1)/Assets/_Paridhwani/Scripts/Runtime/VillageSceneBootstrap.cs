using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using UnityEngine.Rendering;
namespace Paridhwani {
 public static class WorldBindingPolicy {
 public static FieldInfo Resolve(string name) {
 if(string.IsNullOrEmpty(name) || name=="player" || name=="ui" || name=="ambience" || name=="villagers")return null;
 var field=typeof(TeaStallSession).GetField(name,BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly);
 if(field==null || field.IsInitOnly)return null;var type=field.FieldType;return typeof(UnityEngine.Object).IsAssignableFrom(type) || (type.IsArray && typeof(UnityEngine.Object).IsAssignableFrom(type.GetElementType()))?field:null;
 }
 }
 [Serializable] public sealed class WorldReference {public string field;public UnityEngine.Object[] values;}
 public sealed class VillageWorldBindings : MonoBehaviour {
 public WorldReference[] references;public VillageNpc[] villagers;public RiverAmbience[] rivers;
 public void Bind(TeaStallSession session,List<VillageNpc> actors) {
 foreach(var reference in references??Array.Empty<WorldReference>()) {
 var field=WorldBindingPolicy.Resolve(reference.field);
 if(field==null)throw new InvalidOperationException("Unknown world binding: "+reference.field);
 if(field.FieldType.IsArray) {var element=field.FieldType.GetElementType();var array=Array.CreateInstance(element,reference.values.Length);for(int i=0;i<reference.values.Length;i++){if(!reference.values[i] || !element.IsInstanceOfType(reference.values[i]))throw new InvalidOperationException("Missing/invalid world reference "+reference.field);array.SetValue(reference.values[i],i);}field.SetValue(session,array);}
 else {if(reference.values.Length!=1 || !reference.values[0] || !field.FieldType.IsInstanceOfType(reference.values[0]))throw new InvalidOperationException("Missing/invalid world reference "+reference.field);field.SetValue(session,reference.values[0]);}
 }
 foreach(var npc in villagers??Array.Empty<VillageNpc>())if(npc && !actors.Contains(npc)) {actors.Add(npc);var character=npc.GetComponentInChildren<VillageCharacter>(true);if(character)character.lookTarget=session.player.transform;}
 foreach(var river in rivers??Array.Empty<RiverAmbience>())if(river)river.preferences=session.ambience;
 }
 }
 public sealed class VillageAtmosphere : MonoBehaviour {
 public Material sky;public Color fogColor,ambientColor;public bool fog;public float fogDensity,fogStart,fogEnd,ambientIntensity;public FogMode fogMode;public AmbientMode ambientMode;
 public void Apply() {RenderSettings.skybox=sky;RenderSettings.fog=fog;RenderSettings.fogColor=fogColor;RenderSettings.fogDensity=fogDensity;RenderSettings.fogStartDistance=fogStart;RenderSettings.fogEndDistance=fogEnd;RenderSettings.fogMode=fogMode;RenderSettings.ambientMode=ambientMode;RenderSettings.ambientLight=ambientColor;RenderSettings.ambientIntensity=ambientIntensity;}
 }
 public sealed class VillageSceneBootstrap : MonoBehaviour {
 public TeaStallSession session;public string[] worldScenes;bool running,failed;VisualElement panel;Label status;
 IEnumerator Start() {if(!session)session=GetComponent<TeaStallSession>();yield return LoadWorlds();}
 void LoadingPanel() {
 var doc=session.GetComponent<UIDocument>();var root=doc.rootVisualElement;root.Clear();panel=new VisualElement();panel.style.position=Position.Absolute;panel.style.left=panel.style.right=panel.style.top=panel.style.bottom=0;panel.style.backgroundColor=new Color(.06f,.09f,.075f);panel.style.justifyContent=Justify.Center;panel.style.alignItems=Align.Center;root.Add(panel);status=new Label("Opening Kalyanapura...");status.style.color=new Color(.9f,.88f,.78f);status.style.fontSize=22;panel.Add(status);
 }
 IEnumerator LoadWorlds() {
 if(running)yield break;running=true;failed=worldScenes==null || worldScenes.Length==0;session.enabled=false;session.player.enabled=false;session.player.GetComponent<CharacterController>().enabled=false;UnityEngine.Cursor.lockState=CursorLockMode.None;UnityEngine.Cursor.visible=true;LoadingPanel();
 foreach(string path in worldScenes??Array.Empty<string>()) {
 var existing=SceneManager.GetSceneByPath(path);if(existing.IsValid() && existing.isLoaded)continue;
 AsyncOperation operation=null;try {operation=SceneManager.LoadSceneAsync(path,LoadSceneMode.Additive);}catch(Exception error){Debug.LogException(error);failed=true;}
 if(operation==null){failed=true;break;}while(!operation.isDone){status.text="Opening the village... "+Mathf.RoundToInt(operation.progress*100)+"%";yield return null;}
 }
 if(!failed) {
 try {
 var actors=new List<VillageNpc>();foreach(string path in worldScenes){var scene=SceneManager.GetSceneByPath(path);if(!scene.IsValid() || !scene.isLoaded)throw new InvalidOperationException("Missing loaded world scene");foreach(var root in scene.GetRootGameObjects())foreach(var bindings in root.GetComponentsInChildren<VillageWorldBindings>(true))bindings.Bind(session,actors);}
 if(actors.Count==0)throw new InvalidOperationException("No world actors bound");session.villagers=actors.ToArray();SceneManager.SetActiveScene(gameObject.scene);GetComponent<VillageAtmosphere>().Apply();session.player.GetComponent<CharacterController>().enabled=true;session.player.enabled=true;session.enabled=true;
 } catch(Exception error){Debug.LogException(error);failed=true;}
 }
 running=false;
 if(failed) {status.text="The village could not be opened. Your saved visit is unchanged.";panel.Add(new Button(()=>StartCoroutine(LoadWorlds())){text="Try again"});panel.Add(new Button(()=>Application.Quit()){text="Return to desktop"});}
 }
 }
}
