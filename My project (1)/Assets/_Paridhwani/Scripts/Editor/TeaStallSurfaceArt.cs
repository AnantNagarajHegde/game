using System;
using System.Collections.Generic;
using UnityEngine;

namespace Paridhwani.Editor
{
    public static partial class TeaStallSceneBuilder
    {
        sealed class SurfaceGeometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Color> colors = new List<Color>();
            public readonly List<int> triangles = new List<int>();
            public int Point(Vector3 p, Color color) { vertices.Add(p); colors.Add(color); return vertices.Count - 1; }
            public void Triangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
            public void Stone(Vector3 center, Vector3 size, System.Random rng, float wetness, float tint = 1)
            {
                float w = size.x * .5f, d = size.z * .5f;
                float bevel = Mathf.Min(.025f, Mathf.Min(Mathf.Min(w, d) * .22f, size.y * .15f));
                var outline = new[] {new Vector2(-w+bevel,-d),new Vector2(w-bevel,-d),new Vector2(w,-d+bevel),new Vector2(w,d-bevel),
                    new Vector2(w-bevel,d),new Vector2(-w+bevel,d),new Vector2(-w,d-bevel),new Vector2(-w,-d+bevel)};
                int start = vertices.Count;
                float variation = tint * (.88f + (float)rng.NextDouble() * .22f);
                var color = new Color(variation, variation, variation * .98f, wetness);
                for (int ring = 0; ring < 3; ring++) for (int corner = 0; corner < 8; corner++)
                {
                    var p = outline[corner];
                    if (ring == 2) p *= 1 - Mathf.Min(.08f, bevel / Mathf.Max(w, d));
                    float y = ring == 0 ? -size.y * .5f : ring == 1 ? size.y * .5f - bevel : size.y * .5f;
                    Point(center + new Vector3(p.x, y, p.y), color);
                }
                for (int ring = 0; ring < 2; ring++) for (int corner = 0; corner < 8; corner++)
                {
                    int a = start + ring * 8 + corner, b = start + ring * 8 + (corner + 1) % 8, c = a + 8, e = b + 8;
                    Triangle(a, c, b); Triangle(c, e, b);
                }
                int top = Point(center + Vector3.up * size.y * .5f, color);
                for (int corner = 0; corner < 8; corner++) Triangle(top, start + 16 + (corner + 1) % 8, start + 16 + corner);
            }
        }
        struct RainDepression
        {
            public Vector2 center, radius;
            public RainDepression(float x, float z, float width, float length) { center = new Vector2(x, z); radius = new Vector2(width, length); }
            public float Distance(float x, float z) => new Vector2((x - center.x) / radius.x, (z - center.y) / radius.y).magnitude;
        }
        static readonly RainDepression[] teaDepressions = {
            new RainDepression(.65f,-5.8f,.34f,.75f), new RainDepression(2.25f,-2.8f,.42f,.9f),
            new RainDepression(.6f,1.6f,.29f,.6f), new RainDepression(-7.8f,-3.7f,.38f,.54f)
        };
        static Material TeaSurfaceMaterial(string name, string source, string color, float roughness, float wetness, float textureStrength = -1)
        {
            var material = new Material(mats[source]);
            material.SetColor("_BaseColor", Hex(color)); material.SetFloat("_Roughness", roughness);
            material.SetFloat("_Wet", wetness); material.SetFloat("_VertexSurface", 1);
            if (textureStrength >= 0) material.SetFloat("_TextureStrength", textureStrength);
            return Save(material, Root + "/Generated/Materials/" + name + ".mat");
        }
        static void SaveTeaGeometry(string name, SurfaceGeometry geometry, Material material, Transform parent)
        {
            var mesh = new Mesh(); mesh.SetVertices(geometry.vertices); mesh.SetColors(geometry.colors);
            mesh.SetTriangles(geometry.triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = Save(mesh, Root + "/Generated/Meshes/" + name + ".asset");
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); item.transform.SetParent(parent, false);
            item.GetComponent<MeshFilter>().sharedMesh = mesh; item.GetComponent<MeshRenderer>().sharedMaterial = material;
            // Visual relief only: retain the original ground, crossing, plinth and slab colliders.
        }
        static float TeaArtBlend(float x, float z)
        {
            float edge = Mathf.Min(Mathf.Min(x + 9.4f, 3.8f - x), Mathf.Min(z + 7.4f, 4.4f - z));
            return Mathf.SmoothStep(0, 1, Mathf.Clamp01(edge / .8f));
        }
        static float TeaGroundHeight(float x, float z)
        {
            float noise = Mathf.PerlinNoise(x * .7f + 28, z * .7f + 12);
            float lane = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-2.2f, -.7f, x));
            float height = .033f + noise * .018f + lane * .012f;
            foreach (var pool in teaDepressions) height = Mathf.Lerp(.034f, height, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.9f, 1.6f, pool.Distance(x, z))));
            float originalSurface = x >= -1.3f ? .027f : 0;
            return Mathf.Lerp(originalSurface + .001f, height, TeaArtBlend(x, z));
        }
        static void TeaPlinthCourse(SurfaceGeometry stone, System.Random rng, bool alongX, float fixedAxis, float start, float end, int course)
        {
            float cursor = start; bool first = true;
            while (cursor < end - .001f)
            {
                float length = Mathf.Min(end - cursor, first && course == 1 ? .425f : .85f);
                float center = cursor + length * .5f, y = .055f + course * .125f;
                stone.Stone(alongX ? V(center, y, fixedAxis) : V(fixedAxis, y, center),
                    alongX ? V(length - .018f, .12f, .17f) : V(.17f, .12f, length - .018f), rng, 1, .85f);
                cursor += length; first = false;
            }
        }
        static void TeaStallSurfaceArt()
        {
            var parent = new GameObject("Tea Stall Surface Art").transform; parent.SetParent(environment, false);
            // A separate seed keeps all existing world placement and character generation unchanged.
            var rng = new System.Random(8421);
            var earthMaterial = TeaSurfaceMaterial("TeaApronEarth", "Earth", "795A44", .94f, .8f);
            var stoneMaterial = TeaSurfaceMaterial("TeaApronStone", "StoneDark", "777669", .94f, .6f);
            var waterMaterial = TeaSurfaceMaterial("TeaApronRainwater", "StoneDark", "51483A", .62f, .8f, .12f);
            Color waterPigment = waterMaterial.GetColor("_BaseColor").linear;
            Color mudPigment = earthMaterial.GetColor("_BaseColor").linear;
            var waterEdge = new Color(mudPigment.r/waterPigment.r*.9f,mudPigment.g/waterPigment.g*.9f,mudPigment.b/waterPigment.b*.9f,.2f);
            // Keep the inherited earth/lane texture scale aligned at the edge of the local patch.
            Color heroColor = earthMaterial.GetColor("_BaseColor").linear;
            var earth = new SurfaceGeometry(); var stone = new SurfaceGeometry(); var water = new SurfaceGeometry(); var gravel = new SurfaceGeometry();
            const int columns = 49, rows = 43;
            for (int zIndex = 0; zIndex <= rows; zIndex++) for (int xIndex = 0; xIndex <= columns; xIndex++)
            {
                float x = Mathf.Lerp(-9.4f, 3.8f, xIndex / (float)columns), z = Mathf.Lerp(-7.4f, 4.4f, zIndex / (float)rows);
                float track = Mathf.Max(Mathf.Exp(-Mathf.Pow((x - .65f - Mathf.Sin(z * .35f) * .12f) / .3f, 2)),
                    Mathf.Exp(-Mathf.Pow((x - 2.2f - Mathf.Sin(z * .35f) * .12f) / .32f, 2)));
                float shelter = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-1.5f, -.75f, z)) * (x < -1.65f && x > -8.3f ? 1 : 0);
                float standing = Mathf.Exp(-Mathf.Pow((x + 4.2f) / 2.5f, 4) - Mathf.Pow((z + 1.3f) / .7f, 4));
                float pigment = .85f + Mathf.PerlinNoise(x * .24f + 13, z * .24f + 21) * .27f;
                pigment *= 1 - track * .15f; pigment += standing * .09f;
                float wetness = Mathf.Clamp01(.65f + track * .3f - shelter * .6f - standing * .35f);
                float blend = TeaArtBlend(x, z);
                var original = mats[x >= -1.3f ? "Lane" : "Earth"];
                Color originalColor = original.GetColor("_BaseColor").linear;
                var edgeColor = new Color(originalColor.r / heroColor.r, originalColor.g / heroColor.g, originalColor.b / heroColor.b,
                    original.GetFloat("_Wet") / earthMaterial.GetFloat("_Wet"));
                Color tint = Color.Lerp(edgeColor, new Color(pigment, pigment * .98f, pigment * .93f, wetness), blend);
                earth.Point(V(x, TeaGroundHeight(x, z) - track * .004f * blend, z), tint);
            }
            for (int zIndex = 0; zIndex < rows; zIndex++) for (int xIndex = 0; xIndex < columns; xIndex++)
            {
                int a = zIndex * (columns + 1) + xIndex, b = a + 1, c = a + columns + 1, d = c + 1;
                float x = (earth.vertices[a].x + earth.vertices[d].x) * .5f;
                if (x > -1.6f && x < -.75f) continue; // Keep the physical roadside drain and crossing open.
                earth.Triangle(a, c, b); earth.Triangle(b, c, d);
            }
            foreach (var pool in teaDepressions)
            {
                int center = water.Point(V(pool.center.x, .041f, pool.center.y), new Color(.72f, .76f, .7f, 1));
                const int sides = 15;
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * Mathf.PI * 2 / sides, irregular = .88f + .1f * Mathf.Sin(side * 2.8f + pool.center.x);
                    water.Point(V(pool.center.x + Mathf.Cos(angle) * pool.radius.x * irregular, .041f,
                        pool.center.y + Mathf.Sin(angle) * pool.radius.y * irregular), waterEdge);
                }
                for (int side = 0; side < sides; side++) water.Triangle(center, center + 1 + (side + 1) % sides, center + 1 + side);
            }
            foreach (var renderer in environment.GetComponentsInChildren<MeshRenderer>())
            {
                string name = renderer.gameObject.name; var position = renderer.transform.position;
                if (name == "Raised stone plinth") renderer.sharedMaterial = mats["StoneDark"]; // Recessed mortar backing; collider unchanged.
                if (name == "Sheltered floor slab") renderer.enabled = false;
                if (name == "Drain stone" && position.z >= -7.4f && position.z <= 4.4f)
                {
                    renderer.enabled = false;
                    stone.Stone(position + V(0, -.008f, 0), V(.17f, .17f, .74f + (float)rng.NextDouble() * .045f), rng, 1, .82f);
                }
            }
            for (int x = 0; x < 10; x++) for (int z = 0; z < 7; z++)
                stone.Stone(V(-7.6f + x * .66f, .259f, -.2f + z * .66f), V(.648f, .042f, .648f), rng, z == 0 ? .38f : .1f, 1.08f);
            // Bonded plinth courses replace the perfect slab face while keeping its solid collider.
            for (int course = 0; course < 2; course++)
            {
                for (int side = 0; side < 2; side++)
                {
                    TeaPlinthCourse(stone, rng, true, side == 0 ? -.55f : 4.55f, -8.085f, -1.115f, course);
                    // End before the front/back strips so their top surfaces cannot overlap at corners.
                    TeaPlinthCourse(stone, rng, false, side == 0 ? -8 : -1.2f, -.465f, 4.465f, course);
                }
            }
            for (int i = 0; i < 38; i++)
            {
                float x = i < 20 ? -1.75f - (float)rng.NextDouble() * .24f : -8.1f + (float)rng.NextDouble() * 5.6f;
                float z = i < 20 ? -7.2f + (float)rng.NextDouble() * 11 : -1.9f - (float)rng.NextDouble() * .35f;
                float width = .035f + (float)rng.NextDouble() * .065f;
                gravel.Stone(V(x, TeaGroundHeight(x, z) + .004f, z), V(width, .014f, width * 1.3f), rng, .8f, 1.12f);
            }
            // Dark, rough local drain surface, with a small run-out stain below the existing downpipe.
            int ribbon = water.Point(V(-1.30f, .082f, -7.4f), Color.white);
            water.Point(V(-1.06f, .082f, -7.4f), Color.white); water.Point(V(-1.30f, .082f, 4.4f), Color.white); water.Point(V(-1.06f, .082f, 4.4f), Color.white);
            water.Triangle(ribbon, ribbon + 2, ribbon + 1); water.Triangle(ribbon + 1, ribbon + 2, ribbon + 3);
            int runoff = water.Point(V(-1.58f, .18f, -1.10f), Color.white);
            water.Point(V(-1.58f, .18f, -1), Color.white); water.Point(V(-1.18f, .084f, -.91f), Color.white); water.Point(V(-1.18f, .084f, -1.19f), Color.white);
            water.Triangle(runoff, runoff + 1, runoff + 2); water.Triangle(runoff, runoff + 2, runoff + 3);
            SaveTeaGeometry("TeaApronRelief", earth, earthMaterial, parent);
            SaveTeaGeometry("TeaWeatheredStone", stone, stoneMaterial, parent);
            SaveTeaGeometry("TeaRainDepressions", water, waterMaterial, parent);
            SaveTeaGeometry("TeaDrainGravel", gravel, stoneMaterial, parent);
        }
    }
}
