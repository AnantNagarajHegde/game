using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        [MenuItem("Paridhwani/Check residential village")]
        public static void ValidateResidentialVillage()
        {
            int checks = 0;
            var failures = new System.Collections.Generic.HashSet<string>();
            Action<bool, string> check = (ok, description) => { if (!ok) failures.Add(description); checks++; };
            var session = UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            check(session && session.villagers.Length == 14, "Story cast/bindings changed");
            var houses = UnityEngine.Object.FindObjectsByType<VillageResidence>(FindObjectsInactive.Include);
            var playable = houses.Where(h => !h.scenery).ToArray();
            check(playable.Length == 18, "Expected 18 accessible residential lots");
            check(houses.Count(h => h.scenery) == 76, "Expected 76 scenic homes");
            check(playable.Select(h => h.style).Distinct().Count() == 6, "Six reusable house styles must be represented");
            Physics.SyncTransforms();
            foreach (var house in playable)
            {
                check(PrefabUtility.IsPartOfPrefabInstance(house), "House is not a reusable prefab: " + house.name);
                var body = house.GetComponent<BoxCollider>();
                check(body && body.enabled, "House needs a solid collision shell");
                var meshes = house.GetComponentsInChildren<MeshFilter>();
                check(meshes.Length == 2 && meshes[1].sharedMesh.vertexCount < 1800, "House LOD budget: " + house.name);
                var a = body.bounds;
                foreach (var other in playable)
                {
                    if (other == house) continue;
                    var b = other.GetComponent<BoxCollider>().bounds;
                    float gapX = Mathf.Max(b.min.x - a.max.x, a.min.x - b.max.x);
                    float gapZ = Mathf.Max(b.min.z - a.max.z, a.min.z - b.max.z);
                    check(Mathf.Max(gapX, gapZ) >= 1.4f, "Residential lots overlap or lack a setback: " + house.name + " / " + other.name);
                }
            }
            foreach (var house in houses.Where(h => h.scenery)) check(house.GetComponentsInChildren<Collider>().All(c => !c.enabled), "Scenery has a physical obstacle");
            var residents = UnityEngine.Object.FindObjectsByType<VillageAmbientResident>();
            check(residents.Length == 12, "Expected 12 ambient residents");
            foreach (var resident in residents)
            {
                check(!resident.GetComponent<VillageNpc>(), "Ambient resident accidentally participates in story progression");
                for (int segment = 0; segment < resident.route.Length; segment++)
                {
                    var from = resident.route[segment]; var to = resident.route[(segment + 1) % resident.route.Length];
                    for (float t = 0; t <= 1; t += .05f)
                    {
                        var p = Vector3.Lerp(from, to, t);
                        var blockers = Physics.OverlapCapsule(p + Vector3.up * .6f, p + Vector3.up * 1.5f, .27f, ~0, QueryTriggerInteraction.Ignore)
                            .Where(c => !c.GetComponentInParent<VillageAmbientResident>() && !c.GetComponentInParent<VillageWalker>()).ToArray();
                        check(blockers.Length == 0, resident.name + " route obstructed at " + p + ": " + string.Join(", ", blockers.Select(c => c.name)));
                        check(Physics.Raycast(p + Vector3.up * .3f, Vector3.down, .6f, ~(1 << 2)), "Resident route lacks walkable ground");
                    }
                }
            }
            // Public lane connections and original story approach are sampled at controller height.
            foreach (var link in new[] {
                new[]{new Vector3(1.6f,0,-24),new Vector3(1.6f,0,-12)},
                new[]{new Vector3(-18,0,-14),new Vector3(29,0,-14)},
                new[]{new Vector3(20.4f,0,-24),new Vector3(20.4f,0,39)},
                new[]{new Vector3(3.6f,0,13.05f),new Vector3(20.4f,0,13.05f)},
                new[]{new Vector3(1,0,24.8f),new Vector3(20.4f,0,24.8f)},
                new[]{new Vector3(1,0,31),new Vector3(1,0,39)},
                new[]{new Vector3(-10,0,6.5f),new Vector3(-21,0,6.5f)} })
            {
                int samples = Mathf.CeilToInt(Vector3.Distance(link[0], link[1]) / .35f);
                for (int i = 0; i <= samples; i++)
                {
                    var p = Vector3.Lerp(link[0], link[1], i / (float)samples);
                    var obstacles = Physics.OverlapCapsule(p + Vector3.up * .6f, p + Vector3.up * 1.5f, .28f, ~(1 << 2), QueryTriggerInteraction.Ignore)
                        // Friday's existing works vehicle temporarily closes the market shortcut;
                        // the southern lane and panchayat link remain open in that story state.
                        .Where(c => !c.GetComponentInParent<VillageAmbientResident>() && c.name != "Works vehicle body" && c.name != "Works cab").ToArray();
                    check(obstacles.Length == 0, "Public route obstructed at " + p + ": " + string.Join(", ", obstacles.Select(c => c.name)));
                }
            }
            var edge = UnityEngine.Object.FindObjectsByType<BoxCollider>().Where(c => c.name.StartsWith("Village boundary -")).ToArray();
            check(edge.Length == 16, "Playable outline must be completely enclosed");
            foreach (var boundary in edge) check(boundary.size.y >= 5 && boundary.enabled && !boundary.isTrigger, "Boundary is not a physical barrier");
            foreach (string key in new[] { "River upstream", "River downstream" })
            {
                var river = GameObject.Find(key).GetComponent<MeshRenderer>();
                check(river.bounds.size.z > 120 && !river.GetComponent<Collider>(), "River must continue into the distance: " + key);
            }
            foreach (string key in new[] { "FarRiverBank", "NearRiverBank", "FarRiverBankGround", "NearRiverBankGround" })
            {
                var bank = GameObject.Find(key).GetComponent<MeshFilter>().sharedMesh;
                check(bank.normals.All(n => n.y > .5f), "Riverbank faces must point up: " + key);
            }
            var shader = Shader.Find("Paridhwani/River Water");
            check(shader && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "River shader must compile");
            if (failures.Count > 0) throw new InvalidOperationException("Village expansion:\n" + string.Join("\n", failures));
            Debug.Log("VILLAGE_EXPANSION_CHECKS_OK: " + checks + " checks; 18 walkable lots, 76 scenic houses, 12 residents; clear routes and physical perimeter.");
        }
    }
}
