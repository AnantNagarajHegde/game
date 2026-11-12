using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        static void RebuildRiver(Transform outer)
        {
            foreach (Transform item in outer.Cast<Transform>().ToArray())
                if (item.name == "River upstream" || item.name == "River downstream" || item.name == "Water across crest" || item.name == "Weir falling water" || item.name == "Far bank" || item.name == "Far bank vegetation")
                    UnityEngine.Object.DestroyImmediate(item.gameObject);
            var water = new Material(Shader.Find("Paridhwani/River Water"));
            water.SetColor("_BaseColor", Hex("294C48")); water.SetColor("_ShallowColor", Hex("587D71"));
            water.SetColor("_FoamColor", Hex("C0CEBD")); water.SetColor("_ReflectionColor", Hex("95B5B3"));
            water.SetFloat("_FlowSpeed", .38f); water.SetFloat("_RippleStrength", .32f); water.SetFloat("_Smoothness", .83f);
            var surface = Save(water, Root + "/Generated/Materials/RiverWater.mat");
            mats["River"] = surface;
            var fall = new Material(surface); fall.SetFloat("_Waterfall", 1);
            var falling = Save(fall, Root + "/Generated/Materials/WeirCascade.mat");
            RiverRibbon("River upstream", 2.8f, 270, -.72f, surface);
            RiverRibbon("River downstream", -270, 1.91f, -1.48f, surface);
            Box("Water across crest", V(-35.2f, -.66f, 2.35f), V(9.75f, .035f, .9f), "River", false);
            // A real vertical sheet has a useful tangent frame and bank UV, rather than a thin cube.
            var mesh = new Mesh { name = "WeirCascade" };
            mesh.vertices = new[] { V(-40.05f,-.64f,1.92f), V(-30.35f,-.64f,1.92f), V(-40.05f,-1.48f,1.92f), V(-30.35f,-1.48f,1.92f) };
            mesh.uv = new[] {new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};
            mesh.triangles = new[] {0,1,2,1,3,2}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            RiverMesh("Weir falling water", SaveResidentialMesh(mesh, Homes + "/WeirCascade.asset"), falling);
            // Broad sloped banks hide the old rectangular ends while preserving the investigation terrace.
            foreach (bool west in new[] { true, false })
            {
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                for (int i = 0; i <= 270; i++)
                {
                    float z = -270 + i * 2, edge = RiverCentre(z) + (west ? -4.85f : 4.85f);
                    float level = z > 2 ? -.72f : -1.48f;
                    vertices.Add(V(edge, level - .1f, z));
                    vertices.Add(V(edge + (west ? -2.4f : 1.9f), -.06f, z));
                    if (i == 270) continue;
                    int n = i * 2;
                    triangles.AddRange(west ? new[] { n,n+1,n+2,n+1,n+3,n+2 } : new[] { n,n+2,n+1,n+1,n+2,n+3 });
                }
                string bankName = west ? "FarRiverBank" : "NearRiverBank";
                var bank = new Mesh { name = bankName }; bank.SetVertices(vertices); bank.SetTriangles(triangles, 0); bank.RecalculateNormals(); bank.RecalculateBounds();
                RiverMesh(bankName, SaveResidentialMesh(bank, Homes + "/" + bankName + ".asset"), mats["Earth"]);
                var land = new List<Vector3>();
                for (int i = 0; i <= 270; i++)
                {
                    float z = -270 + i * 2, edge = RiverCentre(z) + (west ? -7.25f : 6.75f);
                    land.Add(V(edge, -.06f, z)); land.Add(V(west ? -140 : -20, -.06f, z));
                }
                var ground = new Mesh {name = bankName + "Ground"}; ground.SetVertices(land); ground.SetTriangles(triangles, 0); ground.RecalculateNormals(); ground.RecalculateBounds();
                RiverMesh(ground.name, SaveResidentialMesh(ground, Homes + "/" + ground.name + ".asset"), mats["Earth"]);
            }
            var reeds = new ResidentialMesh();
            for (int leaf = 0; leaf < 5; leaf++)
            {
                float bend = (leaf - 2) * .12f;
                reeds.Quad(V(-.07f, 0, bend), V(.07f, 0, bend), V(bend + .02f, .6f + leaf * .12f, bend * 1.4f), V(bend - .02f, .65f + leaf * .12f, bend * 1.4f), 5, true);
            }
            var reedMesh = SaveResidentialMesh(reeds.Mesh("RiverReeds"), Homes + "/RiverReeds.asset");
            for (int i = 0; i < 30; i++)
            {
                float z = -57 + i * 4.2f;
                if (z > -2 && z < 14) continue;
                float x = RiverCentre(z) + 5.3f;
                var plant = new GameObject("Riverbank reeds", typeof(MeshFilter), typeof(MeshRenderer)); plant.transform.SetParent(environment, false); plant.transform.position = V(x, -.1f, z);
                plant.GetComponent<MeshFilter>().sharedMesh = reedMesh; plant.GetComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(mats["LeafLight"], 6).ToArray();
            }
            var scrub = new SurfaceGeometry(); var rng = new System.Random(70526);
            for (int i = 0; i < 25; i++)
            {
                float z = -35 + i * 4.1f, x = RiverCentre(z) - 6.85f;
                TreeCrown(scrub, V(x, .35f, z), V(.75f, .52f, 1.05f), rng);
                TreeCrown(scrub, V(x - .45f, .28f, z + .55f), V(.8f, .38f, .8f), rng);
            }
            FrontageMesh("RiverbankLowScrub", scrub, mats["VillageBanyanLeaf"], environment);
        }
        static float RiverCentre(float z)
        {
            float distance = Mathf.Max(0, Mathf.Abs(z) - 18);
            return -35.2f - Mathf.Sin(distance * .027f) * Mathf.Min(distance * .06f, 3.4f);
        }
        static void RiverRibbon(string name, float from, float to, float level, Material material)
        {
            int rows = Mathf.CeilToInt((to - from) / 2), columns = 12;
            var points = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int row = 0; row <= rows; row++)
            {
                float z = Mathf.Lerp(from, to, row / (float)rows);
                for (int col = 0; col <= columns; col++)
                {
                    float u = col / (float)columns;
                    points.Add(V(RiverCentre(z) + (u - .5f) * 9.7f, level, z)); uv.Add(new Vector2(u, z));
                    if (row == rows || col == columns) continue;
                    int n = row * (columns + 1) + col;
                    triangles.AddRange(new[] { n,n+columns+1,n+1,n+1,n+columns+1,n+columns+2 });
                }
            }
            var mesh = new Mesh { name = name }; mesh.SetVertices(points); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            RiverMesh(name, SaveResidentialMesh(mesh, Homes + "/" + name.Replace(" ", "") + ".asset"), material);
        }
        static void RiverMesh(string name, Mesh mesh, Material material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); item.transform.SetParent(environment, false);
            item.GetComponent<MeshFilter>().sharedMesh = mesh; item.GetComponent<MeshRenderer>().sharedMaterial = material;
            item.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
