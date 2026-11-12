using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        const string QuarterName = "Residential village expansion";
        const string Homes = Root + "/Generated/Residences";
        static readonly string[] HomeNames = { "Limewash Cottage", "Indigo Veranda", "Ochre Garden House", "Laterite Family Home", "Sage Weaver House", "Rose Corner House" };
        static readonly string[] HomeColours = { "C6BD9C", "718E98", "BEA16F", "AA8170", "99AA90", "B9958B" };
        static readonly float[] HomeWidths = { 5.6f, 6.2f, 5.8f, 6.4f, 5.6f, 6.2f };
        static readonly float[] HomeDepths = { 5.0f, 5.2f, 5.4f, 5.2f, 5.4f, 5.0f };

        // Upgrade the current benchmark without regenerating story geometry, materials or bindings.
        [MenuItem("Paridhwani/Update residential village and river")]
        public static void UpgradeVillage()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before updating the village.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var open = SceneManager.GetSceneAt(i); if (!open.isDirty) continue;
                    string copy = Path.GetFullPath("../verification/Temp/VillageExpansion/OtherScene_" + i + "_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + ".unity");
                    Directory.CreateDirectory(Path.GetDirectoryName(copy));
                    if (!EditorSceneManager.SaveScene(open, copy, true)) throw new IOException("Could not preserve the edited scene: " + open.name);
                }
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            var session = UnityEngine.Object.FindAnyObjectByType<TeaStallSession>();
            if (!session) throw new InvalidOperationException("The benchmark session is missing.");
            string backup = Path.GetFullPath("../verification/Temp/VillageExpansion/BeforeExpansion_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".unity");
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            // A copy outside Assets preserves both the user's scene file and any unsaved edits.
            if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not back up the scene.");
            environment = GameObject.Find("Environment").transform;
            mats.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "/Generated/Materials" }))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                mats[material.name] = material;
            }
            random = new System.Random(1976);
            AddResidentialVillage(session);
            session.generatedRecipeSignature = WorldRecipeSignature.Compute(Path.Combine(Application.dataPath, "_Paridhwani/Scripts/Editor"));
            EditorUtility.SetDirty(session);
            // Repair the benchmark entry's serialized GUID without changing the user's other scenes.
            var entries = EditorBuildSettings.scenes;
            foreach (var entry in entries) if (entry.path == ScenePath) entry.guid = new GUID(AssetDatabase.AssetPathToGUID(ScenePath));
            EditorBuildSettings.scenes = entries;
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the expanded village.");
            ValidateResidentialVillage();
            Debug.Log("VILLAGE_EXPANSION_READY: houses, lanes, residents, continuous scenery and river saved. Backup: " + backup);
        }

        static void AddResidentialVillage(TeaStallSession session)
        {
            var existing = environment.Find(QuarterName);
            if (existing) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            Directory.CreateDirectory(Homes); AssetDatabase.Refresh();
            // The earlier vegetation pass creates these during full benchmark generation.
            foreach (string name in new[] { "VillageTreeBark", "VillagePalmLeaf", "VillageBanyanLeaf" })
                mats[name] = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Generated/Materials/" + name + ".mat");
            var outer = environment;
            var quarter = new GameObject(QuarterName).transform; quarter.SetParent(outer, false);
            environment = quarter;
            try
            {
                RemoveOldVillageEdges(outer);
                var prefabs = new GameObject[HomeNames.Length];
                for (int i = 0; i < prefabs.Length; i++) prefabs[i] = BuildResidenceAsset(i);
                BuildResidentialLanes();
                PlantResidentialPalms(outer);
                int house = 0;
                for (int row = 0; row < 5; row++)
                {
                    float z = -19 + row * 12;
                    PlaceResidence(prefabs[house % 6], V(14.8f, 0, z), -90, false, ++house);
                    PlaceResidence(prefabs[house % 6], V(26, 0, z), 90, false, ++house);
                }
                foreach (float z in new[] { -4f, 18f, 29f }) PlaceResidence(prefabs[house % 6], V(-15.8f, 0, z), -90, false, ++house);
                foreach (float x in new[] { -14f, -5f, 7f }) PlaceResidence(prefabs[house % 6], V(x, 0, -20.8f), 180, false, ++house);
                foreach (float x in new[] { -5f, 7f }) PlaceResidence(prefabs[house % 6], V(x, 0, 36.5f), 0, false, ++house);
                BuildVillageEdges();
                BuildContinuingNeighbourhood(prefabs);
                BuildResidentialLife(session);
                RebuildRiver(outer);
            }
            finally { environment = outer; }
            foreach (var sign in quarter.GetComponentsInChildren<VillageSign>(true))
                sign.theme = session.ui.GetComponent<UnityEngine.UIElements.UIDocument>().panelSettings.themeStyleSheet;
        }

        static void RemoveOldVillageEdges(Transform outer)
        {
            foreach (Transform item in outer.Cast<Transform>().ToArray())
            {
                if (item.name == "Benchmark boundary" || item.name == "West boundary south" || item.name == "West boundary north" ||
                    item.name == "Northern boundary wall" || item.name == "Northern end wall")
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
                // The old horizon blobs occupied the new streets; keep actual story gardens and palms.
                else if (item.name == "Distant canopy" || item.name == "Misty foothill")
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
                else if ((item.name == "Compound wall" || item.name == "Wall cap" || item.name == "Damp wall foot") &&
                    item.TryGetComponent<Renderer>(out var wall) && wall.bounds.min.z < 13.9f && wall.bounds.max.z > 12.2f)
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }

        static Material ResidentialMaterial(string name, string source, string colour, float relief = .035f)
        {
            var material = new Material(mats[source]); material.name = name;
            material.SetColor("_BaseColor", Hex(colour)); material.SetFloat("_Relief", relief);
            material.SetFloat("_VertexSurface", 1); material.SetFloat("_Weathering", .55f);
            material.SetFloat("_TextureStrength", source == "Plaster" ? .42f : .58f);
            return Save(material, Homes + "/" + name + ".mat");
        }

        static GameObject BuildResidenceAsset(int style)
        {
            string key = HomeNames[style].Replace(" ", "");
            var materials = new[] {
                ResidentialMaterial(key + "Limewash", "Plaster", HomeColours[style], .045f),
                ResidentialMaterial("ResidentialTiles", "Clay", "9B614A", .055f),
                ResidentialMaterial("ResidentialTimber", "Wood", "62523C"),
                ResidentialMaterial("ResidentialLaterite", "Stone", "827F6C"),
                ResidentialMaterial(key + "Shutters", "WoodLight", style % 2 == 0 ? "4F7169" : "817352"),
                ResidentialMaterial("ResidentialPlanting", "Leaf", "596E45", .01f)
            };
            var root = new GameObject(HomeNames[style]);
            var tag = root.AddComponent<VillageResidence>(); tag.style = HomeNames[style];
            float w = HomeWidths[style], d = HomeDepths[style], eave = style == 3 ? 3.25f : 2.95f;
            var lods = new List<LOD>();
            for (int level = 0; level < 2; level++)
            {
                var geometry = new ResidentialMesh();
                BuildHouseGeometry(geometry, w, d, eave, style, level == 0);
                var mesh = SaveResidentialMesh(geometry.Mesh(key + "LOD" + level), Homes + "/" + key + "LOD" + level + ".asset");
                var item = new GameObject("House LOD " + level, typeof(MeshFilter), typeof(MeshRenderer)); item.transform.SetParent(root.transform, false);
                item.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = item.GetComponent<MeshRenderer>(); renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = level == 0 ? ShadowCastingMode.On : ShadowCastingMode.Off;
                lods.Add(new LOD(level == 0 ? .16f : .006f, new Renderer[] { renderer }));
            }
            root.AddComponent<LODGroup>().SetLODs(lods.ToArray()); root.GetComponent<LODGroup>().RecalculateBounds();
            var wall = root.AddComponent<BoxCollider>(); wall.center = V(0, eave * .5f, 0); wall.size = V(w, eave, d);
            var porch = new GameObject("Walkable veranda", typeof(BoxCollider), typeof(VillageFootstepSurface)); porch.transform.SetParent(root.transform, false);
            var step = porch.GetComponent<BoxCollider>(); step.center = V(0, .10f, -d * .5f - .55f); step.size = V(w + .25f, .20f, 1.25f);
            porch.GetComponent<VillageFootstepSurface>().kind = FootstepSurface.Stone;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, Homes + "/" + key + ".prefab");
            UnityEngine.Object.DestroyImmediate(root); return prefab;
        }

        static void BuildHouseGeometry(ResidentialMesh g, float w, float d, float eave, int style, bool detail)
        {
            float peak = eave + d * .23f, front = -d * .5f;
            g.Box(V(0, .14f, 0), V(w + .3f, .28f, d + .3f), 3);
            g.Box(V(0, (eave + .28f) * .5f, 0), V(w, eave - .28f, d), 0);
            foreach (float x in new[] { -w * .5f, w * .5f })
                g.Triangle(V(x, eave, -d * .5f), V(x, peak, 0), V(x, eave, d * .5f), 0, x < 0);
            g.Roof(w + .85f, d + .85f, peak, .46f, detail);
            g.Box(V(0, .11f, front - .55f), V(w + .25f, .22f, 1.25f), 3);
            float doorX = style == 2 || style == 5 ? -.85f : 0;
            g.Box(V(doorX, 1.19f, front - .035f), V(1.05f, 1.95f, .10f), 2);
            foreach (float x in new[] { -w * .30f, w * .30f })
            {
                if (Mathf.Abs(x - doorX) < 1.05f) continue;
                g.Box(V(x, 1.85f, front - .035f), V(.9f, .92f, .12f), 2);
                g.Box(V(x, 1.85f, front - .11f), V(.75f, .76f, .07f), 4);
                g.Box(V(x, 1.31f, front - .12f), V(1.1f, .12f, .32f), 3);
                if (detail) for (int i = 0; i < 5; i++) g.Box(V(x - .3f + i * .15f, 1.85f, front - .16f), V(.045f, .75f, .045f), 2);
            }
            foreach (float x in new[] { -w * .5f - .025f, w * .5f + .025f })
            {
                g.Box(V(x, 1.8f, .5f), V(.10f, .95f, 1.05f), 2);
                g.Box(V(x + Mathf.Sign(x) * .04f, 1.8f, .5f), V(.06f, .79f, .89f), 4);
                g.Box(V(x, 1.27f, .5f), V(.30f, .12f, 1.2f), 3);
            }
            // Shallow tiled lean-to veranda, framed independently of the solid main house.
            g.Quad(V(-w * .5f - .25f, eave - .2f, front + .05f), V(w * .5f + .25f, eave - .2f, front + .05f),
                V(w * .5f + .25f, eave - .65f, front - 1.45f), V(-w * .5f - .25f, eave - .65f, front - 1.45f), 1);
            foreach (float x in new[] { -w * .5f + .18f, w * .5f - .18f })
            {
                g.Box(V(x, (eave - .5f) * .5f, front - 1.13f), V(.16f, eave - .5f, .16f), 2);
                g.Box(V(x, .28f, front - 1.13f), V(.30f, .32f, .30f), 3);
            }
            g.Box(V(0, eave - .64f, front - 1.44f), V(w + .6f, .16f, .12f), 2);
            if (!detail) return;
            // Bonded foundations, recessed joinery, exposed rafters and individually capped ridge.
            for (int course = 0; course < 2; course++) for (float x = -w * .5f; x < w * .5f; x += .68f)
                g.Box(V(x + .29f, .09f + course * .14f, front - .17f), V(.60f, .115f, .07f), 3);
            foreach (float x in new[] { doorX - .59f, doorX + .59f }) g.Box(V(x, 1.2f, front - .14f), V(.12f, 2.12f, .15f), 2);
            g.Box(V(doorX, 2.25f, front - .14f), V(1.3f, .16f, .19f), 2);
            for (int i = 0; i < 6; i++) g.Box(V(doorX - .44f + i * .176f, 1.18f, front - .10f), V(.012f, 1.9f, .02f), 3);
            g.Box(V(doorX + .34f, 1.16f, front - .19f), V(.05f, .16f, .045f), 3);
            foreach (float z in new[] { -d * .5f - .39f, d * .5f + .39f })
                g.Box(V(0, peak - Mathf.Abs(z) * .46f, z), V(w + .9f, .14f, .13f), 2);
            for (float x = -w * .5f; x <= w * .5f; x += .65f)
                g.Beam(V(x, eave - .3f, front), V(x, eave - .72f, front - 1.4f), .08f, 2);
            // Domestic props stay on the veranda, clear of the public lane.
            g.Box(V(w * .28f, .52f, front - .58f), V(1.1f, .12f, .4f), 2);
            foreach (float x in new[] { w * .28f - .4f, w * .28f + .4f }) g.Box(V(x, .28f, front - .58f), V(.09f, .46f, .30f), 2);
            g.Pot(V(-w * .4f, .22f, front - .60f), .18f, .35f, 1);
            for (int leaf = 0; leaf < 7; leaf++)
            {
                float a = leaf * 2.4f;
                Vector3 stem = V(-w * .4f, .56f, front - .60f);
                Vector3 end = stem + V(Mathf.Cos(a) * .27f, .22f + leaf * .025f, Mathf.Sin(a) * .23f);
                g.Beam(stem, end, .023f, 5);
                g.Quad(end, end + V(.13f, .10f, .04f), end + V(.18f, .02f, .09f), end + V(.04f, -.03f, .04f), 5, true);
            }
            if (style == 4 || style == 1)
            {
                g.Beam(V(-w * .40f, 2.1f, front - .95f), V(w * .35f, 2.1f, front - .95f), .018f, 2);
                for (int cloth = 0; cloth < 3; cloth++) g.Box(V(-.8f + cloth * .62f, 1.83f, front - .95f), V(.47f, .52f, .022f), cloth % 2 == 0 ? 4 : 0);
            }
        }

        static void PlaceResidence(GameObject prefab, Vector3 position, float yaw, bool scenery, int number)
        {
            var home = (GameObject)PrefabUtility.InstantiatePrefab(prefab, environment);
            home.name = (scenery ? "Beyond village " : "Village home ") + number.ToString("00") + " - " + prefab.name;
            home.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            home.GetComponent<VillageResidence>().scenery = scenery;
            if (scenery)
            {
                foreach (var collider in home.GetComponentsInChildren<Collider>()) collider.enabled = false;
                home.GetComponent<LODGroup>().ForceLOD(1);
                return;
            }
            // Each lot has an aligned approach; no clutter or collision on the 1.6 m entry.
            var path = Box("Home entry path " + number, home.transform.TransformPoint(V(0, .027f, -4.7f)), V(1.6f, .045f, 3.1f), "Lane");
            path.transform.rotation = home.transform.rotation;
        }

        static void Lane(string name, Vector3 a, Vector3 b, float width, string material = "Lane")
        {
            var delta = b - a; var item = Box(name, (a + b) * .5f + Vector3.up * .025f, V(width, .05f, delta.magnitude + width), material);
            item.transform.rotation = Quaternion.LookRotation(delta);
        }

        static void BuildResidentialLanes()
        {
            Box("Distant landscape foundation", V(0, -2.5f, 0), V(700, 1, 700), "Earth", false);
            // One continuous collision ground, terminating at the river terrace, preserves story floors above it.
            Box("Residential village ground", V(5.5f, -.22f, 7.5f), V(51, .4f, 65), "Earth");
            Lane("Eastern residential avenue", V(20.4f, 0, -25), V(20.4f, 0, 40), 4.2f);
            Lane("Southern neighbourhood lane", V(-20, 0, -14), V(31, 0, -14), 3.0f);
            Lane("Western garden lane", V(-10.6f, 0, -14), V(-10.6f, 0, 38), 1.8f);
            Lane("Northern neighbourhood lane", V(-20, 0, 32.8f), V(11, 0, 32.8f), 1.8f);
            Lane("Northern garden connector", V(11, 0, 32.8f), V(11, 0, 38), 1.4f);
            Lane("Northern residential link", V(11, 0, 38), V(30, 0, 38), 1.8f);
            Lane("Arrival lane extension", V(1.6f, 0, -25), V(1.6f, 0, -11), 3.5f);
            Lane("Temple lane extension", V(1, 0, 31), V(1, 0, 40), 2.6f);
            // This narrow link threads the existing market/workshop gap.
            Lane("Market residential link", V(3.3f, 0, 13.05f), V(20.4f, 0, 13.05f), 1.15f);
            Lane("Panchayat residential link", V(1, 0, 24.8f), V(20.4f, 0, 24.8f), 2.4f);
            foreach (float z in new[] { -13f, -1f, 11f, 23f, 35f, 38f })
                Lane("Residential cross lane", V(18.1f, 0, z), V(30.6f, 0, z), 1.5f);
            // Raised side verges establish an orderly street without obstructing pedestrian crossings.
            foreach (float x in new[] { 17.9f, 22.9f }) for (int i = 0; i < 16; i++)
            {
                float z = -23 + i * 4;
                if (Mathf.Abs(z - 13) < 2 || Mathf.Abs(z - 25) < 2 || Mathf.Abs(z - 33) < 2) continue;
                Box("Residential stone verge", V(x, .075f, z), V(.25f, .15f, 2.1f), "Stone");
            }
            foreach (var p in new[] { V(19, 0, -10), V(22, 0, 4), V(19, 0, 16), V(22, 0, 28), V(-9.2f, 0, -13) })
            {
                Cylinder("Neighbourhood lamp post", p + Vector3.up * 1.8f, .065f, 3.6f, "Wood");
                Box("Sheltered street lantern", p + V(0, 3.45f, 0), V(.30f, .35f, .30f), "Cream", false);
                Box("Lantern rain hood", p + V(0, 3.66f, 0), V(.46f, .08f, .46f), "Wood", false);
            }
            // A small shared well makes a destination at the southern street junction.
            Cylinder("Neighbourhood well footing", V(23, .08f, -22), 1.15f, .16f, "Stone");
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6; var block = Box("Well masonry", V(23 + Mathf.Cos(a) * .75f, .48f, -22 + Mathf.Sin(a) * .75f), V(.40f, .8f, .35f), "Stone");
                block.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
            }
            Cylinder("Well interior", V(23, .2f, -22), .6f, .025f, "Water");
            Beam("Well support", V(22, 0, -22), V(22, 2.3f, -22), .07f, "Wood");
            Beam("Well support", V(24, 0, -22), V(24, 2.3f, -22), .07f, "Wood");
            Beam("Well crossbeam", V(21.9f, 2.3f, -22), V(24.1f, 2.3f, -22), .08f, "Wood");
            Beam("Well rope", V(23, 2.3f, -22), V(23, .3f, -22), .015f, "WoodLight");
        }

        static void Boundary(string name, Vector3 a, Vector3 b, bool gate = false)
        {
            var delta = b - a; float length = delta.magnitude;
            var root = new GameObject("Village boundary - " + name).transform; root.SetParent(environment, false);
            root.position = (a + b) * .5f; root.rotation = Quaternion.LookRotation(delta);
            var barrier = root.gameObject.AddComponent<BoxCollider>(); barrier.center = V(0, 2, 0); barrier.size = V(.30f, 5, length + .3f);
            for (float z = -length * .5f; z <= length * .5f + .01f; z += 1.8f)
                Box("Boundary stone pier", V(0, .66f, z), V(.28f, 1.32f, .28f), "Stone", false, root);
            foreach (float y in new[] { .48f, .97f }) Box(gate ? "Closed lane timber" : "Neighbourhood rail", V(0, y, 0), V(.12f, .10f, length), "Wood", false, root);
            if (gate)
            {
                var board = Box("Closed lane notice", V(.09f, 1.20f, 0), V(.08f, .38f, 1.6f), "Sign", false, root);
                // Sign is authored in world space to use the shared shaped-text presentation.
                float inward = root.position.z < 0 ? -1 : 1;
                Vector3 location = root.TransformPoint(V(inward * .16f, 1.2f, 0));
                int before = environment.childCount; Text("LANE CLOSED / REPAIRS", location, .046f, false, 0);
                if (environment.childCount > before)
                {
                    var sign = environment.GetChild(environment.childCount - 1); sign.rotation = root.rotation * Quaternion.Euler(0, inward > 0 ? -90 : 90, 0);
                }
            }
        }

        static void BuildVillageEdges()
        {
            // Physical closure follows the whole playable outline, including the river spur.
            Boundary("east gardens", V(31, 0, -25), V(31, 0, 40));
            Boundary("south gardens west", V(-20, 0, -25), V(-.5f, 0, -25));
            Boundary("arrival road closure", V(-.5f, 0, -25), V(3.7f, 0, -25), true);
            Boundary("south gardens centre", V(3.7f, 0, -25), V(18, 0, -25));
            Boundary("residential road closure", V(18, 0, -25), V(22.8f, 0, -25), true);
            Boundary("south gardens east", V(22.8f, 0, -25), V(31, 0, -25));
            Boundary("north gardens west", V(-20, 0, 40), V(-.5f, 0, 40));
            Boundary("temple road closure", V(-.5f, 0, 40), V(2.5f, 0, 40), true);
            Boundary("north gardens centre", V(2.5f, 0, 40), V(18, 0, 40));
            Boundary("north residential closure", V(18, 0, 40), V(22.8f, 0, 40), true);
            Boundary("north gardens east", V(22.8f, 0, 40), V(31, 0, 40));
            Boundary("west south", V(-20, 0, -25), V(-20, 0, -1.8f));
            Boundary("west north", V(-20, 0, 12.5f), V(-20, 0, 40));
            Boundary("river south", V(-30.1f, 0, -1.8f), V(-20, 0, -1.8f));
            Boundary("river north", V(-30.1f, 0, 12.5f), V(-20, 0, 12.5f));
            // Match the visible existing weir rail, so there is no walk-through seam at its ends.
            Boundary("water edge", V(-29.85f, 0, -1.8f), V(-29.85f, 0, 12.5f));
        }

        static void BuildContinuingNeighbourhood(GameObject[] homes)
        {
            Box("Northern distant land", V(0, -.28f, 155), V(65, .35f, 230), "Earth", false);
            Box("Southern distant land", V(5, -.28f, -147.5f), V(75, .35f, 245), "Earth", false);
            Box("Across river distant land", V(-71, -.32f, 5), V(56, .35f, 240), "Earth", false);
            Box("Eastern distant gardens", V(36, -.28f, 7.5f), V(10, .35f, 66), "Earth", false);
            int number = 0;
            foreach (float z in new[] { 48f, 61f, 74f, 87f, 100f, -35f, -48f, -61f, -74f, -87f })
            {
                foreach (float x in new[] { -22f, -11f, 6f, 16.5f, 28f })
                    PlaceResidence(homes[(number * 5 + 1) % homes.Length], V(x, -.03f, z), z > 0 ? 0 : 180, true, ++number);
                Box("Continuing neighbourhood cross street", V(3, -.025f, z + (z > 0 ? -5 : 5)), V(62, .04f, 2.8f), "Lane", false);
            }
            foreach (float x in new[] { -52f, -65f, -78f }) foreach (float z in new[] { -24f, -10f, 4f, 18f, 32f, 46f, 60f })
                PlaceResidence(homes[(number + 2) % homes.Length], V(x, -.1f, z), -90, true, ++number);
            foreach (float z in new[] { -16f, -4f, 8f, 20f, 32f })
                PlaceResidence(homes[number % homes.Length], V(37, -.03f, z), 90, true, ++number);
            foreach (float x in new[] { 1f, 20.4f })
            {
                Box("Road continues north into village", V(x, -.022f, 155), V(3.5f, .035f, 230), "Lane", false);
                Box("Road continues south into village", V(x, -.022f, -147.5f), V(3.5f, .035f, 245), "Lane", false);
            }
            // Palm rows and layered roofs carry the horizon through monsoon haze.
            for (int i = 0; i < 20; i++)
            {
                float x = i % 2 == 0 ? -25 : 33, z = -31 + i * 7;
                Palm(V(x, 0, z), 7.5f + i % 4, 100 + i);
            }
            for (int i = 0; i < 8; i++) Palm(V(-46, 0, -30 + i * 15), 8.5f + i % 3, 130 + i);
        }

        static void BuildResidentialLife(TeaStallSession session)
        {
            var routes = new[] {
                new[]{V(19.7f,0,-19),V(19.7f,0,-8)}, new[]{V(21.1f,0,-5),V(21.1f,0,5)},
                new[]{V(19.7f,0,8),V(19.7f,0,19)}, new[]{V(21.1f,0,21),V(21.1f,0,31)},
                new[]{V(19.7f,0,33),V(19.7f,0,38)}, new[]{V(24,0,23),V(29,0,23)},
                new[]{V(-17,0,-14),V(-6,0,-14)}, new[]{V(-3,0,-14.5f),V(8,0,-14.5f)},
                new[]{V(-10.6f,0,-10),V(-10.6f,0,-2)}, new[]{V(-10.6f,0,16),V(-10.6f,0,24)},
                new[]{V(9.8f,0,24.8f),V(18,0,24.8f)}, new[]{V(-10.6f,0,27),V(-10.6f,0,32)}
            };
            var names = new[] { "Ravi", "Leela", "Vasanth", "Meera", "Nagesh", "Padma", "Suresh", "Lakshmi", "Ramesh", "Devaki", "Mohan", "Savitha" };
            var sources = new[] { VillageNpcRole.MarketVendor, VillageNpcRole.MessOwner, VillageNpcRole.Librarian, VillageNpcRole.FlowerSeller };
            for (int i = 0; i < routes.Length; i++)
            {
                var source = session.villagers.First(n => n.role == sources[i % sources.Length]);
                var actor = UnityEngine.Object.Instantiate(source.gameObject, environment); actor.name = "Resident " + names[i];
                foreach (var npc in actor.GetComponents<VillageNpc>()) UnityEngine.Object.DestroyImmediate(npc);
                foreach (var c in actor.GetComponents<Collider>()) UnityEngine.Object.DestroyImmediate(c);
                actor.SetActive(true); actor.transform.localScale = Vector3.one;
                actor.transform.position = routes[i][0] + Vector3.up * .12f;
                actor.transform.rotation = Quaternion.LookRotation(routes[i][1] - routes[i][0]);
                var motor = actor.AddComponent<CharacterController>(); motor.center = V(0, .9f, 0); motor.height = 1.76f; motor.radius = .25f; motor.stepOffset = .3f;
                var resident = actor.AddComponent<VillageAmbientResident>(); resident.route = routes[i]; resident.firstStop = 1;
                resident.pace = .85f + i % 4 * .08f; resident.pause = 2.5f + i % 3 * 1.2f;
                var character = actor.GetComponentInChildren<VillageCharacter>(); character.walker = null; character.working = character.talking = false;
                var cloth = CharacterColour("Resident" + i % 6, new[] { "6B8791", "B29968", "8B757D", "789180", "BEA98A", "887C60" }[i % 6]);
                foreach (var renderer in actor.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.name.EndsWith("_Shirt") || renderer.name.EndsWith("Sleeve")) renderer.sharedMaterial = cloth;
            }
        }

        static void PlantResidentialPalms(Transform outer)
        {
            // Retain the banyan; replant the original random palm scatter on clear garden verges.
            foreach (var renderer in outer.GetComponentsInChildren<MeshRenderer>())
                if (renderer.name == "NearVillagePalmWood" || renderer.name == "NearVillagePalmLeaves" ||
                    renderer.name == "Areca palm trunk" || renderer.name == "Areca crown") renderer.enabled = false;
            var wood = new SurfaceGeometry(); var leaves = new SurfaceGeometry(); var rng = new System.Random(71176);
            for (int i = 0; i < 18; i++)
            {
                var p = V(i % 2 == 0 ? 11.5f : -10, 0, -10 + (i / 2) * 5.3f);
                float height = 7.4f + i % 4 * .6f;
                var path = new Vector3[21]; var radius = new float[21];
                for (int row = 0; row < path.Length; row++)
                {
                    float t = row / 20f; path[row] = p + V(Mathf.Sin(t * Mathf.PI) * .05f, height * t, .08f * t);
                    radius[row] = Mathf.Lerp(.11f, .055f, t) * (row % 2 == 0 ? 1 : .96f);
                }
                TreeTube(wood, path, radius, 9, rng); FeatheredPalm(leaves, path[20], i, rng);
            }
            FrontageMesh("ResidentialGardenPalmWood", wood, mats["VillageTreeBark"], environment);
            FrontageMesh("ResidentialGardenPalmLeaves", leaves, mats["VillagePalmLeaf"], environment);
        }

        // Native setters invalidate loaded graphics buffers. CopySerialized alone can leave
        // a renderer displaying the previous topology until the Editor is restarted.
        static Mesh SaveResidentialMesh(Mesh source, string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (!mesh) return Save(source, path);
            mesh.Clear(); mesh.indexFormat = source.indexFormat;
            mesh.vertices = source.vertices; mesh.normals = source.normals; mesh.colors = source.colors; mesh.uv = source.uv;
            mesh.subMeshCount = source.subMeshCount;
            for (int sub = 0; sub < source.subMeshCount; sub++) mesh.SetTriangles(source.GetTriangles(sub), sub);
            mesh.bounds = source.bounds; mesh.UploadMeshData(false); EditorUtility.SetDirty(mesh);
            UnityEngine.Object.DestroyImmediate(source); return mesh;
        }

        // Per-material batches keep all six reusable styles to two mesh assets each.
        sealed class ResidentialMesh
        {
            readonly List<Vector3> vertices = new List<Vector3>();
            readonly List<Color> colours = new List<Color>();
            readonly List<int>[] faces = Enumerable.Range(0, 6).Select(_ => new List<int>()).ToArray();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, int material, bool reverse = false)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c });
                float shade = .94f + Mathf.PerlinNoise(a.x * 1.7f + 17, a.y * 2 + a.z) * .10f;
                colours.AddRange(new[] { Color.white * shade, Color.white * shade, Color.white * shade });
                faces[material].AddRange(reverse ? new[] { n, n + 2, n + 1 } : new[] { n, n + 1, n + 2 });
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, bool both = false)
            { Triangle(a, b, c, material); Triangle(a, c, d, material); if (both) { Triangle(c, b, a, material); Triangle(d, c, a, material); } }
            public void Box(Vector3 p, Vector3 s, int m, Quaternion rotation = default)
            {
                if (rotation == default) rotation = Quaternion.identity;
                var v = new Vector3[8]; for (int i = 0; i < 8; i++) v[i] = p + rotation * Vector3.Scale(s * .5f, V((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Quad(v[0], v[2], v[3], v[1], m); Quad(v[5], v[7], v[6], v[4], m);
                Quad(v[4], v[6], v[2], v[0], m); Quad(v[1], v[3], v[7], v[5], m);
                Quad(v[2], v[6], v[7], v[3], m); Quad(v[4], v[0], v[1], v[5], m);
            }
            public void Beam(Vector3 a, Vector3 b, float width, int material) => Box((a + b) * .5f, V(width, (b - a).magnitude, width), material, Quaternion.FromToRotation(Vector3.up, b - a));
            public void Pot(Vector3 p, float radius, float height, int material)
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI / 6, b = (i + 1) * Mathf.PI / 6;
                    Vector3 va = V(Mathf.Cos(a), 0, Mathf.Sin(a)), vb = V(Mathf.Cos(b), 0, Mathf.Sin(b));
                    Quad(p + va * radius * .7f, p + Vector3.up * height + va * radius, p + Vector3.up * height + vb * radius, p + vb * radius * .7f, material);
                }
            }
            public void Roof(float width, float depth, float peak, float pitch, bool detail)
            {
                foreach (int side in new[] { -1, 1 })
                {
                    float z = depth * .5f * side;
                    Quad(V(-width * .5f, peak, 0), V(-width * .5f, peak - Mathf.Abs(z) * pitch, z), V(width * .5f, peak - Mathf.Abs(z) * pitch, z), V(width * .5f, peak, 0), 1, true);
                    if (!detail) continue;
                    int columns = Mathf.CeilToInt(width / .25f), rows = Mathf.CeilToInt(depth * .5f / .48f);
                    float tileWidth = width / columns, tileDepth = depth * .5f / rows;
                    for (int col = 0; col < columns; col++) for (int row = 0; row < rows; row++) for (int arc = 0; arc < 5; arc++)
                    {
                        float x0 = -width * .5f + (col + arc / 5f) * tileWidth, x1 = x0 + tileWidth / 5;
                        float z0 = side * row * tileDepth, z1 = side * Mathf.Min(depth * .5f, (row + 1) * tileDepth + .045f);
                        float h0 = Mathf.Sin(arc * Mathf.PI / 5) * .045f + .018f, h1 = Mathf.Sin((arc + 1) * Mathf.PI / 5) * .045f + .018f;
                        Quad(V(x0, peak - Mathf.Abs(z0) * pitch + h0, z0), V(x0, peak - Mathf.Abs(z1) * pitch + h0, z1), V(x1, peak - Mathf.Abs(z1) * pitch + h1, z1), V(x1, peak - Mathf.Abs(z0) * pitch + h1, z0), 1, side < 0);
                    }
                }
                if (detail) for (float x = -width * .5f; x < width * .5f; x += .34f) for (int arc = 0; arc < 6; arc++)
                {
                    float a = arc * Mathf.PI / 6, b = (arc + 1) * Mathf.PI / 6;
                    var va = V(0, Mathf.Sin(a) * .13f, Mathf.Cos(a) * .13f); var vb = V(0, Mathf.Sin(b) * .13f, Mathf.Cos(b) * .13f);
                    Vector3 p = V(x, peak - .065f, 0); Quad(p + va, p + V(.38f, 0, 0) + va, p + V(.38f, 0, 0) + vb, p + vb, 1);
                }
            }
            public Mesh Mesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(vertices); mesh.SetColors(colours); mesh.subMeshCount = faces.Length;
                for (int i = 0; i < faces.Length; i++) mesh.SetTriangles(faces[i], i);
                mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
            }
        }
    }
}
