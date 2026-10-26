using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace Paridhwani.Editor {
 public static partial class TeaStallSceneBuilder {
 static void ValidateModularScenes(Scene boot,Scene village,Scene forest) {
 var core=boot.GetRootGameObjects();
 if(core.SelectMany(g=>g.GetComponentsInChildren<TeaStallSession>(true)).Count()!=1 || core.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Count()!=1 || core.SelectMany(g=>g.GetComponentsInChildren<EventSystem>(true)).Count()!=1)throw new InvalidOperationException("Bootstrap must own exactly one session, camera and input event system");
 var fieldNames=new HashSet<string>();var actors=new HashSet<VillageNpc>();
 foreach(var scene in new[]{village,forest}) {
 var roots=scene.GetRootGameObjects();var modules=roots.SelectMany(g=>g.GetComponentsInChildren<VillageWorldBindings>(true)).ToArray();if(modules.Length!=1)throw new InvalidOperationException("Each world needs one reference module");
 if(roots.SelectMany(g=>g.GetComponentsInChildren<TeaStallSession>(true)).Any() || roots.SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).Any() || roots.SelectMany(g=>g.GetComponentsInChildren<AudioListener>(true)).Any())throw new InvalidOperationException("World scene contains a duplicate persistent owner");
 foreach(var reference in modules[0].references) {
 if(!fieldNames.Add(reference.field) || WorldBindingPolicy.Resolve(reference.field)==null)throw new InvalidOperationException("Duplicate/invalid world field "+reference.field);
 foreach(var value in reference.values)if(!value || !Owner(value) || Owner(value).scene!=scene)throw new InvalidOperationException("Cross-scene or missing object in "+reference.field);
 }
 foreach(var npc in modules[0].villagers)if(!npc || !npc.eyes || !actors.Add(npc))throw new InvalidOperationException("Invalid/duplicate NPC in world modules");
 foreach(var character in roots.SelectMany(g=>g.GetComponentsInChildren<VillageCharacter>(true)))if(character.lookTarget)throw new InvalidOperationException("NPC look target must be bound at runtime");
 }
 if(actors.Count!=14)throw new InvalidOperationException("Expected fourteen authored NPC instances in modular worlds");
 var loader=core.SelectMany(g=>g.GetComponentsInChildren<VillageSceneBootstrap>(true)).Single();if(loader.session.enabled || loader.worldScenes.Length!=2)throw new InvalidOperationException("Session must wait for both world scenes");
 Debug.Log("PARIDHWANI_MODULAR_VALIDATION_OK: ownership, field uniqueness, local references and fourteen NPCs");
 }
 }
}
