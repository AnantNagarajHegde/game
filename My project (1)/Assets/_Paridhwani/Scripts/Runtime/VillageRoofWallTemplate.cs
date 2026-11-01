using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Paridhwani
{
    // Wall-top template for the pitched roofs in the authored village. Applied on
    // scene load so existing scenes and regenerated/additive worlds receive it.
    public static class VillageRoofWallTemplate
    {
        const float Pitch = .31f;
        const string FillName = "Roof-fitted upper wall";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register()
        {
            SceneManager.sceneLoaded -= Apply;
            SceneManager.sceneLoaded += Apply;
        }

        static void Apply(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "TeaStall_Benchmark" && scene.name != "Village_World" && scene.name != "Forest_Farm_World") return;
            var objects = new Dictionary<string, GameObject>();
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (!objects.ContainsKey(t.name)) objects.Add(t.name, t.gameObject);

            // The silhouette houses are solid blocks; the library was later
            // replaced by individual walls and gets its own open-door treatment.
            SolidHouse(objects, "Opposite house plaster walls", 5.3f, 4.8f);
            SolidHouse(objects, "Distant workshop plaster walls", 16.2f, 4.6f);
            Side(objects, "Library west wall", 16f, 5f);
            Side(objects, "Library east wall", 16f, 5f);
            Across(objects, "Library north wall", 16f, 5f);
            AcrossSpan(objects, "Library front left", -8.29f, -1.31f, 13.4f, 3.25f, 16f, 5f);

            Across(objects, "Old limewashed back wall", 2f, 3.92f);
            Side(objects, "Side wall", 2f, 3.92f);
            Across(objects, "Panchayat rear wall", 21.2f, 4.2f);
            Side(objects, "Panchayat east wall", 21.2f, 4.2f);
            Across(objects, "Temple facade", 29.1f, 4f);
            Across(objects, "Repair shed back wall", 4.65f, 3.95f);
            Across(objects, "Checkpost damp wall", 4f, 4f);
            Side(objects, "Ridge shelter back", 25.5f, 3.5f);
            Across(objects, "Farmhouse rear wall", 45f, 5f);
            // Both farmhouse side walls have the same name.
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Farmhouse side wall") Side(t.gameObject, 45f, 5f);
            AcrossSpan(objects, "Farmhouse front left", 43.05f, 52.95f, 43f, 3.3f, 45f, 5f);

            if (objects.TryGetValue("Parked rural bus", out var bus))
            {
                // Only update the original placement, preserving any deliberate
                // future repositioning of the landmark.
                Vector3 p = bus.transform.position;
                if (Mathf.Abs(p.x - 7.5f) < .01f && Mathf.Abs(p.z + 8.9f) < .01f)
                    bus.transform.position = new Vector3(p.x, p.y, -10.5f);
            }
            VillageArchitecture.Apply(scene);
        }

        static void SolidHouse(Dictionary<string, GameObject> objects, string name, float roofZ, float peak)
        {
            if (!objects.TryGetValue(name, out var wall) || !wall.TryGetComponent<Renderer>(out var renderer)) return;
            Bounds b = renderer.bounds;
            AddPanel(wall, b.min.x, b.max.x, b.min.z, b.min.z, b.max.y, roofZ, peak, .18f, "front");
            AddPanel(wall, b.min.x, b.max.x, b.max.z, b.max.z, b.max.y, roofZ, peak, .18f, "back");
            AddPanel(wall, b.min.x, b.min.x, b.min.z, b.max.z, b.max.y, roofZ, peak, .18f, "west");
            AddPanel(wall, b.max.x, b.max.x, b.min.z, b.max.z, b.max.y, roofZ, peak, .18f, "east");
        }

        static void Across(Dictionary<string, GameObject> objects, string name, float roofZ, float peak)
        {
            if (!objects.TryGetValue(name, out var wall) || !wall.TryGetComponent<Renderer>(out var renderer)) return;
            Bounds b = renderer.bounds;
            AddPanel(wall, b.min.x, b.max.x, b.center.z, b.center.z, b.max.y, roofZ, peak, b.size.z, "across");
        }

        static void AcrossSpan(Dictionary<string, GameObject> objects, string name, float x0, float x1, float z, float top, float roofZ, float peak)
        {
            if (objects.TryGetValue(name, out var wall)) AddPanel(wall, x0, x1, z, z, top, roofZ, peak, .18f, "front");
        }

        static void Side(Dictionary<string, GameObject> objects, string name, float roofZ, float peak)
        {
            if (objects.TryGetValue(name, out var wall)) Side(wall, roofZ, peak);
        }

        static void Side(GameObject wall, float roofZ, float peak)
        {
            if (!wall.TryGetComponent<Renderer>(out var renderer)) return;
            Bounds b = renderer.bounds;
            AddPanel(wall, b.center.x, b.center.x, b.min.z, b.max.z, b.max.y, roofZ, peak, b.size.x, "side");
        }

        // The profile follows both roof slopes, splitting at the ridge. Both
        // faces render, including from inside the library and farmhouse.
        static void AddPanel(GameObject wall, float x0, float x1, float z0, float z1,
            float bottom, float roofZ, float peak, float thickness, string suffix)
        {
            string name = FillName + " " + suffix;
            if (wall.transform.Find(name)) return;
            bool alongX = Mathf.Abs(x1 - x0) > .001f;
            float start = alongX ? x0 : z0, end = alongX ? x1 : z1;
            var cuts = new List<float> { start };
            if (!alongX && roofZ > start && roofZ < end) cuts.Add(roofZ);
            cuts.Add(end);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < cuts.Count - 1; i++)
            {
                float a = cuts[i], b = cuts[i + 1];
                float za = alongX ? z0 : a, zb = alongX ? z0 : b;
                float ya = Mathf.Max(bottom, peak - Mathf.Abs(za - roofZ) * Pitch + .01f);
                float yb = Mathf.Max(bottom, peak - Mathf.Abs(zb - roofZ) * Pitch + .01f);
                if (ya <= bottom + .001f && yb <= bottom + .001f) continue;
                Vector3 p0 = alongX ? new Vector3(a, bottom, z0) : new Vector3(x0, bottom, a);
                Vector3 p1 = alongX ? new Vector3(b, bottom, z0) : new Vector3(x0, bottom, b);
                Vector3 p2 = new Vector3(p1.x, yb, p1.z), p3 = new Vector3(p0.x, ya, p0.z);
                Vector3 offset = (alongX ? Vector3.back : Vector3.right) * (thickness * .5f);
                Quad(vertices, triangles, p0-offset, p1-offset, p2-offset, p3-offset);
                Quad(vertices, triangles, p1+offset, p0+offset, p3+offset, p2+offset);
                Quad(vertices, triangles, p3-offset, p2-offset, p2+offset, p3+offset);
                Quad(vertices, triangles, p0+offset, p1+offset, p1-offset, p0-offset);
                Quad(vertices, triangles, p0-offset, p3-offset, p3+offset, p0+offset);
                Quad(vertices, triangles, p1+offset, p2+offset, p2-offset, p1-offset);
            }
            if (vertices.Count == 0) return;
            var fill = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(RoofWallMeshLifetime));
            SceneManager.MoveGameObjectToScene(fill, wall.scene);
            fill.transform.SetParent(wall.transform, true);
            for (int i = 0; i < vertices.Count; i++) vertices[i] = fill.transform.InverseTransformPoint(vertices[i]);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            fill.GetComponent<MeshFilter>().sharedMesh = mesh;
            fill.GetComponent<MeshRenderer>().sharedMaterial = wall.GetComponent<Renderer>().sharedMaterial;
        }

        static void Quad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            triangles.Add(n); triangles.Add(n+1); triangles.Add(n+2);
            triangles.Add(n); triangles.Add(n+2); triangles.Add(n+3);
        }
    }

    sealed class RoofWallMeshLifetime : MonoBehaviour
    {
        void OnDestroy()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh) Destroy(filter.sharedMesh);
        }
    }
}
