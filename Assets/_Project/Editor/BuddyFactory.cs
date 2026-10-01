using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Chibi şoför arkadaşlar: kedi, köpek, tavşan, ayı, panda, tilki, sincap, kurbağa, penguen, fare.
    /// Hepsi aynı tombul kalıptan (büyük kafa, küçük gövde, parlak gözler, pembe yanaklar) çıkar;
    /// kulak, renk ve küçük ayrıntılarla ayrışır. Yolcu koltuğunda oturur, öne (+Z) bakar.
    /// </summary>
    public static class BuddyFactory
    {
        enum Ears { None, Cat, Fox, Dog, Rabbit, Round, Mouse }

        class Spec
        {
            public string id, word;
            public Color fur, belly, ear, inner;
            public Ears ears;
            public bool panda, frog, penguin, squirrelTail, foxTail;
        }

        static Mesh sphere, cone;
        static string meshDir;

        public static List<(string id, GameObject prefab, string word)> BuildAll(string dir, Func<string, Color, Material> toon)
        {
            meshDir = $"{dir}/Meshes";
            sphere = Save(Ellipsoid(Vector3.one, 16, 24), "Buddy_Sphere");
            cone = Save(Revolve("Buddy_Cone", new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 0f) }, false, 20, true), "Buddy_Cone");

            var dark = toon("BuddyDark", new Color(0.16f, 0.14f, 0.22f));
            var white = toon("BuddyWhite", Color.white);
            var blush = toon("BuddyBlush", new Color(1f, 0.6f, 0.72f));
            var cream = new Color(1f, 0.94f, 0.84f);
            var pink = new Color(1f, 0.68f, 0.78f);

            var specs = new[]
            {
                new Spec { id = "buddy_cat", word = "animal_cat", fur = new Color(1f, 0.72f, 0.45f), belly = cream, ear = new Color(1f, 0.72f, 0.45f), inner = pink, ears = Ears.Cat },
                new Spec { id = "buddy_dog", word = "animal_dog", fur = new Color(0.88f, 0.68f, 0.48f), belly = cream, ear = new Color(0.55f, 0.38f, 0.26f), inner = pink, ears = Ears.Dog },
                new Spec { id = "buddy_rabbit", word = "animal_rabbit", fur = new Color(0.97f, 0.96f, 0.99f), belly = Color.white, ear = new Color(0.97f, 0.96f, 0.99f), inner = pink, ears = Ears.Rabbit },
                new Spec { id = "buddy_bear", word = "animal_bear", fur = new Color(0.66f, 0.46f, 0.32f), belly = new Color(0.9f, 0.75f, 0.58f), ear = new Color(0.66f, 0.46f, 0.32f), inner = new Color(0.9f, 0.75f, 0.58f), ears = Ears.Round },
                new Spec { id = "buddy_panda", word = "animal_panda", fur = new Color(0.98f, 0.98f, 0.98f), belly = Color.white, ear = new Color(0.2f, 0.19f, 0.25f), inner = new Color(0.2f, 0.19f, 0.25f), ears = Ears.Round, panda = true },
                new Spec { id = "buddy_fox", word = "animal_fox", fur = new Color(1f, 0.56f, 0.26f), belly = Color.white, ear = new Color(1f, 0.56f, 0.26f), inner = Color.white, ears = Ears.Fox, foxTail = true },
                new Spec { id = "buddy_squirrel", word = "animal_squirrel", fur = new Color(0.8f, 0.52f, 0.32f), belly = cream, ear = new Color(0.8f, 0.52f, 0.32f), inner = pink, ears = Ears.Cat, squirrelTail = true },
                new Spec { id = "buddy_frog", word = "animal_frog", fur = new Color(0.52f, 0.84f, 0.44f), belly = new Color(0.88f, 0.96f, 0.64f), ear = Color.white, inner = Color.white, ears = Ears.None, frog = true },
                new Spec { id = "buddy_penguin", word = "animal_penguin", fur = new Color(0.24f, 0.27f, 0.4f), belly = Color.white, ear = new Color(1f, 0.72f, 0.22f), inner = Color.white, ears = Ears.None, penguin = true },
                new Spec { id = "buddy_mouse", word = "animal_mouse", fur = new Color(0.76f, 0.76f, 0.82f), belly = new Color(0.93f, 0.93f, 0.96f), ear = new Color(0.76f, 0.76f, 0.82f), inner = pink, ears = Ears.Mouse },
            };

            var list = new List<(string, GameObject, string)>();
            foreach (var s in specs)
            {
                var fur = toon("Fur_" + s.id, s.fur);
                var belly = toon("Belly_" + s.id, s.belly);
                var ear = toon("Ear_" + s.id, s.ear);
                var inner = toon("Inner_" + s.id, s.inner);
                var root = new GameObject("Buddy_" + s.id);
                root.AddComponent<BuddyAnimator>();

                // Gövde ve kafa
                P(root, sphere, s.panda ? dark : fur, new Vector3(0, 0.11f, 0), new Vector3(0.12f, 0.13f, 0.11f));
                P(root, sphere, belly, new Vector3(0, 0.1f, 0.075f), new Vector3(0.075f, 0.085f, 0.04f));
                if (s.frog) P(root, sphere, fur, new Vector3(0, 0.29f, 0), new Vector3(0.18f, 0.125f, 0.14f));
                else P(root, sphere, fur, new Vector3(0, 0.31f, 0), new Vector3(0.16f, 0.145f, 0.135f));

                // Yüz
                if (s.penguin)
                {
                    P(root, sphere, white, new Vector3(0, 0.3f, 0.07f), new Vector3(0.125f, 0.105f, 0.075f));
                    P(root, cone, ear, new Vector3(0, 0.285f, 0.135f), new Vector3(0.026f, 0.05f, 0.022f), Quaternion.Euler(90, 0, 0));
                }
                else if (!s.frog)
                {
                    P(root, sphere, belly, new Vector3(0, 0.272f, 0.112f), new Vector3(0.068f, 0.048f, 0.04f));
                    P(root, sphere, dark, new Vector3(0, 0.29f, 0.15f), new Vector3(0.022f, 0.016f, 0.014f));
                }
                if (s.frog)
                {
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        P(root, sphere, fur, new Vector3(sx * 0.075f, 0.385f, 0.04f), new Vector3(0.055f, 0.05f, 0.05f));
                        P(root, sphere, white, new Vector3(sx * 0.075f, 0.395f, 0.075f), new Vector3(0.038f, 0.038f, 0.025f));
                        P(root, sphere, dark, new Vector3(sx * 0.075f, 0.395f, 0.098f), new Vector3(0.019f, 0.024f, 0.01f));
                        P(root, sphere, white, new Vector3(sx * 0.075f - 0.006f, 0.404f, 0.107f), new Vector3(0.006f, 0.006f, 0.004f));
                        P(root, sphere, blush, new Vector3(sx * 0.12f, 0.27f, 0.1f), new Vector3(0.026f, 0.014f, 0.008f));
                    }
                    P(root, sphere, dark, new Vector3(0, 0.255f, 0.13f), new Vector3(0.05f, 0.008f, 0.008f)); // gülümseme
                }
                else
                {
                    foreach (float sx in new[] { -1f, 1f })
                    {
                        if (s.panda) P(root, sphere, dark, new Vector3(sx * 0.058f, 0.326f, 0.117f), new Vector3(0.036f, 0.046f, 0.016f), Quaternion.Euler(0, 0, sx * 25f));
                        P(root, sphere, s.panda ? white : dark, new Vector3(sx * 0.058f, 0.33f, 0.124f), new Vector3(0.021f, 0.029f, 0.013f));
                        if (s.panda) P(root, sphere, dark, new Vector3(sx * 0.058f, 0.328f, 0.133f), new Vector3(0.012f, 0.017f, 0.008f));
                        P(root, sphere, white, new Vector3(sx * 0.058f - 0.006f, 0.343f, 0.136f), new Vector3(0.007f, 0.007f, 0.004f));
                        P(root, sphere, blush, new Vector3(sx * 0.1f, 0.283f, 0.11f), new Vector3(0.026f, 0.014f, 0.008f));
                    }
                }

                // Kulaklar
                foreach (float sx in new[] { -1f, 1f })
                {
                    switch (s.ears)
                    {
                        case Ears.Cat:
                            P(root, cone, ear, new Vector3(sx * 0.085f, 0.4f, 0), new Vector3(0.055f, 0.085f, 0.032f), Quaternion.Euler(0, 0, -sx * 18f));
                            P(root, cone, inner, new Vector3(sx * 0.086f, 0.405f, 0.014f), new Vector3(0.032f, 0.055f, 0.02f), Quaternion.Euler(0, 0, -sx * 18f));
                            break;
                        case Ears.Fox:
                            P(root, cone, ear, new Vector3(sx * 0.09f, 0.4f, 0), new Vector3(0.068f, 0.12f, 0.035f), Quaternion.Euler(0, 0, -sx * 20f));
                            P(root, cone, inner, new Vector3(sx * 0.091f, 0.405f, 0.016f), new Vector3(0.04f, 0.075f, 0.02f), Quaternion.Euler(0, 0, -sx * 20f));
                            break;
                        case Ears.Dog:
                            P(root, sphere, ear, new Vector3(sx * 0.15f, 0.3f, 0), new Vector3(0.045f, 0.095f, 0.06f), Quaternion.Euler(0, 0, sx * 18f));
                            break;
                        case Ears.Rabbit:
                            P(root, sphere, ear, new Vector3(sx * 0.055f, 0.5f, 0), new Vector3(0.036f, 0.12f, 0.028f), Quaternion.Euler(0, 0, -sx * 8f));
                            P(root, sphere, inner, new Vector3(sx * 0.056f, 0.5f, 0.02f), new Vector3(0.018f, 0.085f, 0.012f), Quaternion.Euler(0, 0, -sx * 8f));
                            break;
                        case Ears.Round:
                            P(root, sphere, ear, new Vector3(sx * 0.115f, 0.42f, 0), new Vector3(0.05f, 0.05f, 0.035f));
                            if (!s.panda) P(root, sphere, inner, new Vector3(sx * 0.115f, 0.42f, 0.022f), new Vector3(0.026f, 0.026f, 0.016f));
                            break;
                        case Ears.Mouse:
                            P(root, sphere, ear, new Vector3(sx * 0.13f, 0.42f, 0), new Vector3(0.075f, 0.075f, 0.025f));
                            P(root, sphere, inner, new Vector3(sx * 0.13f, 0.42f, 0.014f), new Vector3(0.05f, 0.05f, 0.014f));
                            break;
                    }
                }

                // Kuyruklar (arkadan ve yandan görünür)
                if (s.squirrelTail)
                {
                    P(root, sphere, fur, new Vector3(0, 0.2f, -0.15f), new Vector3(0.075f, 0.14f, 0.07f), Quaternion.Euler(-20, 0, 0));
                    P(root, sphere, belly, new Vector3(0, 0.36f, -0.19f), new Vector3(0.065f, 0.08f, 0.065f));
                }
                if (s.foxTail)
                {
                    P(root, sphere, fur, new Vector3(0.05f, 0.1f, -0.15f), new Vector3(0.06f, 0.06f, 0.12f), Quaternion.Euler(0, 30, 0));
                    P(root, sphere, belly, new Vector3(0.1f, 0.1f, -0.24f), new Vector3(0.04f, 0.04f, 0.05f), Quaternion.Euler(0, 30, 0));
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/Prefabs/Buddy_{s.id}.prefab");
                UnityEngine.Object.DestroyImmediate(root);
                list.Add((s.id, prefab, s.word));
            }
            return list;
        }

        static void P(GameObject root, Mesh mesh, Material mat, Vector3 pos, Vector3 scale) => P(root, mesh, mat, pos, scale, Quaternion.identity);

        static void P(GameObject root, Mesh mesh, Material mat, Vector3 pos, Vector3 scale, Quaternion rot)
        {
            var g = new GameObject(mesh.name);
            g.transform.SetParent(root.transform, false);
            g.transform.localPosition = pos; g.transform.localRotation = rot; g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Mesh Save(Mesh m, string name)
        {
            m.name = name;
            AssetDatabase.CreateAsset(m, $"{meshDir}/{name}.asset");
            return m;
        }
    }
}
