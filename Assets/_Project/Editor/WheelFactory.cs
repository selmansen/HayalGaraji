using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Chibi tekerlek fabrikası: yan duvarları şişkin balon lastikler, diş desenleri ve
    /// kişilikli jantlar (kollu, yıldız, çiçek, şeker spirali, kedi patisi, dişli, donut).
    /// Aks X ekseni, merkez orijin. Her tekerlek: Tire + Rim (renklendirilebilir) + süsler.
    /// </summary>
    public static class WheelFactory
    {
        public enum Tread { Block, Chevron, Slick, Paddle }
        enum Style { Spokes, Star, Flower, Candy, Paw, Gear, Donut }

        public class Mats
        {
            public Material tire, rim, white, dark, pink, yellow, blue, dough, icing, chrome;
        }

        public static List<(string id, GameObject prefab, string word)> BuildAll(string dir, Mats m, float R, float width, float thick)
        {
            return new List<(string, GameObject, string)>
            {
                ("wheel_spokes", Make(dir, "Wheel_Spokes", m, R, width, thick, Tread.Chevron, Style.Spokes, false), "part_wheel"),
                ("wheel_star",   Make(dir, "Wheel_Star",   m, R, width, thick, Tread.Block,   Style.Star,   false), "shape_star"),
                ("wheel_flower", Make(dir, "Wheel_Flower", m, R, width, thick, Tread.Slick,   Style.Flower, true),  "shape_flower"),
                ("wheel_candy",  Make(dir, "Wheel_Candy",  m, R, width, thick, Tread.Slick,   Style.Candy,  true),  "food_candy"),
                ("wheel_paw",    Make(dir, "Wheel_Paw",    m, R, width, thick, Tread.Block,   Style.Paw,    false), "part_paw"),
                ("wheel_gear",   Make(dir, "Wheel_Gear",   m, R, width * 1.1f, thick * 1.1f, Tread.Paddle, Style.Gear, false), "part_gear"),
                ("wheel_donut",  Make(dir, "Wheel_Donut",  m, R, width, thick, Tread.Slick,   Style.Donut,  false), "food_donut"),
            };
        }

        // ---------------- lastik ----------------

        public static Mesh Tire(string name, float R, float width, float thick, Tread tread)
        {
            float rm = R - thick * 0.5f, a = width * 0.5f, b = thick * 0.5f;
            const int P = 30;
            var prof = new List<Vector2>(); var outward = new float[P]; var xs = new float[P];
            for (int k = 0; k < P; k++)
            {
                float psi = 2f * Mathf.PI * k / P;
                var p = SuperEllipse(psi, a, b, 2.6f);   // şişkin, yuvarlak kesit
                prof.Add(new Vector2(p.x, rm + p.y));
                outward[k] = Mathf.Sin(psi);
                xs[k] = p.x / a;
            }
            float depth = thick * (tread == Tread.Paddle ? 0.28f : 0.17f);
            int count = tread == Tread.Block ? 18 : tread == Tread.Chevron ? 20 : 10;
            return Revolve(name, prof, true, 144, false, (phi, k) =>
            {
                if (tread == Tread.Slick) return 0f;
                float mask = Mathf.SmoothStep(0f, 1f, (outward[k] - 0.35f) / 0.45f);
                if (mask <= 0f) return 0f;
                float u = phi / (2f * Mathf.PI) * count, pat;
                switch (tread)
                {
                    case Tread.Block:
                        pat = Band(Frac(u + (xs[k] > 0 ? 0.5f : 0f)), 0.12f, 0.62f, 0.05f);
                        if (Mathf.Abs(xs[k]) < 0.08f) pat = 0f; // orta oluk
                        break;
                    case Tread.Chevron: pat = Band(Frac(u + Mathf.Abs(xs[k]) * 0.35f), 0.1f, 0.55f, 0.05f); break;
                    default: pat = Band(Frac(u), 0.0f, 0.22f, 0.04f); break; // Paddle: iri kürekler
                }
                return -depth * (1f - pat) * mask; // oluklar içe oyulur, dış çap R kalır
            });
        }

        static float Frac(float x) => x - Mathf.Floor(x);

        // ---------------- tekerlek ----------------

        static GameObject Make(string dir, string name, Mats m, float R, float width, float thick, Tread tread, Style style, bool stripe)
        {
            var root = new GameObject(name);
            float rm = R - thick * 0.5f, b = thick * 0.5f;
            float rimR = R - thick * 0.92f, xf = width * 0.39f, dish = 0.025f;
            float faceX = xf - dish + 0.004f;

            if (style == Style.Donut)
            {
                BuildDonut(dir, name, root, m, R, width, thick);
                return Save(dir, root, name);
            }

            Child(dir, root, name, "Tire", Tire(name + "_Tire", R, width, thick, tread), m.tire);

            // Jant gövdesi: çukur yüz + yuvarlak dudak
            var rimProf = new List<Vector2>
            {
                new Vector2( xf - dish, 0f),               new Vector2( xf - dish, rimR * 0.25f),
                new Vector2( xf - dish * 0.5f, rimR * 0.62f), new Vector2( xf, rimR * 0.86f),
                new Vector2( xf * 0.97f, rimR * 0.97f),    new Vector2( xf * 0.85f, rimR),
                new Vector2(-xf * 0.85f, rimR),            new Vector2(-xf * 0.97f, rimR * 0.97f),
                new Vector2(-xf, rimR * 0.86f),            new Vector2(-xf + dish * 0.5f, rimR * 0.62f),
                new Vector2(-xf + dish, rimR * 0.25f),     new Vector2(-xf + dish, 0f),
            };
            System.Func<float, int, float> gear = null;
            if (style == Style.Gear)
                gear = (phi, k) => (k >= 3 && k <= 8) ? Band(Frac(phi / (2f * Mathf.PI) * 12f), 0.15f, 0.55f, 0.04f) * 0.045f : 0f;

            var rimParts = new List<(Mesh, Matrix4x4)> { (Revolve("RimBase", rimProf, false, 72, false, gear), Matrix4x4.identity) };
            var acc = new List<(Mesh, Matrix4x4)>();
            var acc2 = new List<(Mesh, Matrix4x4)>();
            Material accMat = m.white, acc2Mat = m.yellow;

            foreach (float s in new[] { 1f, -1f })
            {
                float fx = s * faceX;
                switch (style)
                {
                    case Style.Spokes:
                        for (int i = 0; i < 5; i++)
                        {
                            float a = 2f * Mathf.PI * i / 5f + Mathf.PI / 2f;
                            rimParts.Add((Ellipsoid(new Vector3(0.022f, rimR * 0.3f, rimR * 0.085f)),
                                TRS(new Vector3(fx, Mathf.Cos(a) * rimR * 0.45f, Mathf.Sin(a) * rimR * 0.45f), Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.right))));
                        }
                        acc.Add((Ellipsoid(new Vector3(0.03f, rimR * 0.2f, rimR * 0.2f)), At(new Vector3(fx + s * 0.008f, 0, 0))));
                        break;

                    case Style.Star:
                        accMat = m.yellow;
                        for (int i = 0; i < 5; i++)
                        {
                            float a = Mathf.PI / 2f + 2f * Mathf.PI * i / 5f;
                            acc.Add((Ellipsoid(new Vector3(0.024f, rimR * 0.32f, rimR * 0.13f)),
                                TRS(new Vector3(fx, Mathf.Cos(a) * rimR * 0.36f, Mathf.Sin(a) * rimR * 0.36f), Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.right))));
                        }
                        acc.Add((Ellipsoid(new Vector3(0.034f, rimR * 0.24f, rimR * 0.24f)), At(new Vector3(fx, 0, 0))));
                        break;

                    case Style.Flower:
                        accMat = m.pink; acc2Mat = m.yellow;
                        for (int i = 0; i < 6; i++)
                        {
                            float a = 2f * Mathf.PI * i / 6f;
                            acc.Add((Ellipsoid(new Vector3(0.024f, rimR * 0.26f, rimR * 0.16f)),
                                TRS(new Vector3(fx, Mathf.Cos(a) * rimR * 0.42f, Mathf.Sin(a) * rimR * 0.42f), Quaternion.AngleAxis(a * Mathf.Rad2Deg, Vector3.right))));
                        }
                        acc2.Add((Ellipsoid(new Vector3(0.036f, rimR * 0.22f, rimR * 0.22f)), At(new Vector3(fx + s * 0.006f, 0, 0))));
                        break;

                    case Style.Candy:
                        var disc = DomedDisc(xf - dish + 0.002f, s, rimR * 0.82f, 0.02f, 12, 96,
                            (ang, rn) => Frac(ang / (2f * Mathf.PI) * 3f + rn * 1.1f) < 0.5f ? 1 : 0, 2);
                        rimParts.Add((disc[0], Matrix4x4.identity));
                        acc.Add((disc[1], Matrix4x4.identity));
                        acc2Mat = m.white;
                        acc2.Add((Ellipsoid(new Vector3(0.03f, rimR * 0.14f, rimR * 0.14f)), At(new Vector3(fx + s * 0.02f, 0, 0))));
                        break;

                    case Style.Paw:
                        accMat = m.pink;
                        acc.Add((Ellipsoid(new Vector3(0.03f, rimR * 0.27f, rimR * 0.32f)), At(new Vector3(fx, -rimR * 0.14f, 0))));
                        foreach (var t in new[] { new Vector2(0.2f, -0.36f), new Vector2(0.38f, -0.13f), new Vector2(0.38f, 0.13f), new Vector2(0.2f, 0.36f) })
                            acc.Add((Ellipsoid(new Vector3(0.028f, rimR * 0.11f, rimR * 0.11f)), At(new Vector3(fx, t.x * rimR, t.y * rimR))));
                        break;

                    case Style.Gear:
                        accMat = m.dark; acc2Mat = m.chrome;
                        acc.Add((Ellipsoid(new Vector3(0.035f, rimR * 0.3f, rimR * 0.3f)), At(new Vector3(fx, 0, 0))));
                        for (int i = 0; i < 6; i++)
                        {
                            float a = 2f * Mathf.PI * i / 6f;
                            acc2.Add((Ellipsoid(new Vector3(0.02f, rimR * 0.07f, rimR * 0.07f)), At(new Vector3(fx, Mathf.Cos(a) * rimR * 0.55f, Mathf.Sin(a) * rimR * 0.55f))));
                        }
                        break;
                }
            }

            var rimGo = Child(dir, root, name, "Rim", Combine(name + "_Rim", rimParts), m.rim);
            rimGo.AddComponent<TintTarget>().channel = TintChannel.Rim;
            if (acc.Count > 0) Child(dir, root, name, "Accent", Combine(name + "_Accent", acc), accMat);
            if (acc2.Count > 0) Child(dir, root, name, "Accent2", Combine(name + "_Accent2", acc2), acc2Mat);

            if (stripe)
            {
                // Beyaz yan şerit (klasik "beyaz duvar" lastik)
                var ring = Circle(new Vector2(0, 0), 0.011f, 10);
                var parts = new List<(Mesh, Matrix4x4)>();
                foreach (float s in new[] { 1f, -1f })
                {
                    var prof = new List<Vector2>();
                    foreach (var p in ring) prof.Add(new Vector2(s * width * 0.49f + p.x, rm + p.y));
                    parts.Add((Revolve("Stripe", prof, true, 96), Matrix4x4.identity));
                }
                Child(dir, root, name, "Stripe", Combine(name + "_Stripe", parts), m.white);
            }
            return Save(dir, root, name);
        }

        static void BuildDonut(string dir, string name, GameObject root, Mats m, float R, float width, float thick)
        {
            float b = width * 0.5f, rm = R - b;
            Child(dir, root, name, "Tire", Revolve(name + "_Dough", Circle(new Vector2(0, rm), b, 28), true, 96), m.dough);
            var icing = new List<(Mesh, Matrix4x4)>();
            foreach (float s in new[] { 1f, -1f })
            {
                float c = s > 0 ? 0f : Mathf.PI;
                var arc = Circle(new Vector2(0, rm), b * 1.08f, 20, c - 1.25f, c + 1.25f, false);
                icing.Add((Revolve("Icing", arc, false, 96), Matrix4x4.identity));
            }
            Child(dir, root, name, "Icing", Combine(name + "_Icing", icing), m.icing);

            var rnd = new System.Random(7);
            var groups = new[] { new List<(Mesh, Matrix4x4)>(), new List<(Mesh, Matrix4x4)>(), new List<(Mesh, Matrix4x4)>() };
            for (int i = 0; i < 30; i++)
            {
                float s = i % 2 == 0 ? 1f : -1f;
                float phi = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float psi = ((float)rnd.NextDouble() * 1.8f - 0.9f) + (s > 0 ? 0f : Mathf.PI);
                float rad = b * 1.12f, x = Mathf.Cos(psi) * rad, r = rm + Mathf.Sin(psi) * rad;
                var pos = new Vector3(x, Mathf.Cos(phi) * r, Mathf.Sin(phi) * r);
                var rot = Quaternion.LookRotation(pos - new Vector3(0, Mathf.Cos(phi) * rm, Mathf.Sin(phi) * rm)) * Quaternion.Euler(0, 0, (float)rnd.NextDouble() * 360f);
                groups[i % 3].Add((Ellipsoid(new Vector3(0.035f, 0.011f, 0.011f), 6, 8), TRS(pos, rot)));
            }
            Child(dir, root, name, "SprinkleW", Combine(name + "_SprW", groups[0]), m.white);
            Child(dir, root, name, "SprinkleY", Combine(name + "_SprY", groups[1]), m.yellow);
            Child(dir, root, name, "SprinkleB", Combine(name + "_SprB", groups[2]), m.blue);
        }

        // ---------------- kayıt ----------------

        static GameObject Child(string dir, GameObject root, string wheel, string part, Mesh mesh, Material mat)
        {
            AssetDatabase.CreateAsset(mesh, $"{dir}/Meshes/{wheel}_{part}.asset");
            var g = new GameObject(part);
            g.transform.SetParent(root.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        static GameObject Save(string dir, GameObject root, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(root);
            return p;
        }
    }
}
