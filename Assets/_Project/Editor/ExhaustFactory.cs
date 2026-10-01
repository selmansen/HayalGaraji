using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Eğlenceli egzozlar: tek, çift, dev bacalar, trompet (nota uçar), baloncuk, gökkuşağı, konfeti.
    /// Hepsi egzoz yuvasına (arka, sağ alt) takılır; yuvanın ayna simetriği x = -0.64'tür.
    /// Ses kodla üretilir (ExhaustEmitter.soundId → SynthSounds).
    /// </summary>
    public static class ExhaustFactory
    {
        public class Mats
        {
            public Material chrome, gold, pink, dark, red, yellow, blue, cyan;
            public Material smokeFx, bubbleFx, noteFx, confettiFx, puffFx;
        }

        const float MirrorX = -0.64f;

        public static List<(string id, GameObject prefab, string word)> BuildAll(string dir, Mats m)
        {
            var list = new List<(string, GameObject, string)>();

            // Tek egzoz
            var single = new GameObject("Exhaust_Single");
            var fx1 = Pipe(dir, single.transform, "Single", Vector3.zero, 0.05f, 0.24f, m.chrome, m.dark);
            list.Add(("exhaust_single", Save(dir, single, "single", Smoke(single.transform, fx1, m.smokeFx, new Color(0.9f, 0.9f, 0.95f), 0.9f, 0.5f)), "part_exhaust"));

            // Çift egzoz
            var dbl = new GameObject("Exhaust_Double");
            var d1 = Pipe(dir, dbl.transform, "DoubleA", Vector3.zero, 0.055f, 0.24f, m.chrome, m.dark);
            var d2 = Pipe(dir, dbl.transform, "DoubleB", new Vector3(MirrorX, 0, 0), 0.055f, 0.24f, m.chrome, m.dark);
            list.Add(("exhaust_double", Save(dir, dbl, "double",
                Smoke(dbl.transform, d1, m.smokeFx, new Color(0.9f, 0.9f, 0.95f), 1f, 0.5f),
                Smoke(dbl.transform, d2, m.smokeFx, new Color(0.9f, 0.9f, 0.95f), 1f, 0.5f)), "number_2"));

            // Dev bacalar (kamyon gibi, yukarı dumanlı)
            var stacks = new GameObject("Exhaust_Stacks");
            var fxs = new List<ParticleSystem>();
            foreach (float x in new[] { 0f, MirrorX })
            {
                var g = new GameObject("Stack");
                g.transform.SetParent(stacks.transform, false);
                g.transform.localPosition = new Vector3(x, 0.45f, 0.26f);
                var rod = Revolve("Stack", new List<Vector2> { new Vector2(0, 0), new Vector2(0, 0.055f), new Vector2(0.7f, 0.055f), new Vector2(0.72f, 0.075f), new Vector2(0.78f, 0.075f), new Vector2(0.78f, 0) }, false, 24, true);
                Child(dir, g, "Stack_" + (x == 0f ? "A" : "B"), rod, m.chrome, Vector3.zero, Quaternion.identity);
                var top = new GameObject("Top"); top.transform.SetParent(g.transform, false); top.transform.localPosition = new Vector3(0, 0.8f, 0);
                fxs.Add(Particles(top.transform, "Smoke", m.smokeFx, Quaternion.Euler(-90, 0, 0), true, 22, 0, 1.2f, new Vector2(1.2f, 1.8f), new Vector2(0.18f, 0.26f), -0.05f,
                    new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.74f, 0.82f)), 0.9f));
            }
            list.Add(("exhaust_stacks", Save(dir, stacks, "stacks", fxs.ToArray()), "size_giant"));

            // Trompet: nota uçurur
            var trumpet = new GameObject("Exhaust_Trumpet");
            var bell = Revolve("Bell", new List<Vector2> { new Vector2(0.14f, 0f), new Vector2(0.14f, 0.035f), new Vector2(0f, 0.04f), new Vector2(-0.08f, 0.06f), new Vector2(-0.13f, 0.1f), new Vector2(-0.15f, 0.13f), new Vector2(-0.15f, 0f) }, false, 28);
            Child(dir, trumpet, "Trumpet", bell, m.gold, new Vector3(0, 0.02f, -0.1f), Quaternion.Euler(0, -90, 0));
            var tMouth = new GameObject("Mouth"); tMouth.transform.SetParent(trumpet.transform, false); tMouth.transform.localPosition = new Vector3(0, 0.05f, -0.28f);
            var notes = Particles(tMouth.transform, "Notes", m.noteFx, Quaternion.Euler(-25, 180, 0), true, 7, 0, 1.4f, new Vector2(0.8f, 1.3f), new Vector2(0.14f, 0.2f), -0.2f,
                new ParticleSystem.MinMaxGradient(new Color(0.55f, 0.35f, 0.95f), new Color(1f, 0.35f, 0.65f)), 1f);
            list.Add(("exhaust_trumpet", Save(dir, trumpet, "trumpet", notes), "music_trumpet"));

            // Baloncuk
            var bubble = new GameObject("Exhaust_Bubble");
            var bfx = Pipe(dir, bubble.transform, "Bubble", Vector3.zero, 0.055f, 0.22f, m.pink, m.dark);
            var ring = Revolve("Ring", Circle(new Vector2(0, 0.075f), 0.02f, 10), true, 28);
            Child(dir, bubble, "BubbleRing", ring, m.cyan, new Vector3(0, 0, -0.2f), Quaternion.Euler(0, 90, 0));
            var bubbles = Particles(bfx, "Bubbles", m.bubbleFx, Quaternion.Euler(-15, 180, 0), true, 14, 0, 2.2f, new Vector2(0.5f, 1f), new Vector2(0.08f, 0.2f), -0.25f,
                new ParticleSystem.MinMaxGradient(Color.white), 0.95f);
            list.Add(("exhaust_bubble", Save(dir, bubble, "bubble", bubbles), "weather_bubbles"));

            // Gökkuşağı
            var rainbow = new GameObject("Exhaust_Rainbow");
            var rMats = new[] { m.red, m.yellow, m.blue };
            for (int i = 0; i < 3; i++)
            {
                var seg = Revolve("Seg" + i, new List<Vector2> { new Vector2(-0.04f, 0), new Vector2(-0.04f, 0.056f), new Vector2(0.04f, 0.056f), new Vector2(0.04f, 0) }, false, 24);
                Child(dir, rainbow, "Seg" + i, seg, rMats[i], new Vector3(0, 0, 0.02f - i * 0.08f), Quaternion.Euler(0, 90, 0));
            }
            var rTip = new GameObject("Tip"); rTip.transform.SetParent(rainbow.transform, false); rTip.transform.localPosition = new Vector3(0, 0, -0.24f);
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.35f, 0.45f), 0f), new GradientColorKey(new Color(1f, 0.82f, 0.25f), 0.33f),
                                 new GradientColorKey(new Color(0.45f, 0.85f, 0.45f), 0.55f), new GradientColorKey(new Color(0.35f, 0.65f, 1f), 0.8f),
                                 new GradientColorKey(new Color(0.7f, 0.45f, 1f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            var puffs = Particles(rTip.transform, "Puffs", m.puffFx, Quaternion.Euler(-10, 180, 0), true, 24, 0, 1f, new Vector2(1f, 1.5f), new Vector2(0.14f, 0.22f), -0.05f,
                new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor }, 0.9f);
            list.Add(("exhaust_rainbow", Save(dir, rainbow, "rainbow", puffs), "shape_rainbow"));

            // Konfeti: parti borusu, motor çalışınca "pat!" diye konfeti patlatır
            var confetti = new GameObject("Exhaust_Confetti");
            var cone = Revolve("Popper", new List<Vector2> { new Vector2(0.16f, 0f), new Vector2(0.16f, 0.025f), new Vector2(-0.14f, 0.11f), new Vector2(-0.16f, 0.12f), new Vector2(-0.16f, 0f) }, false, 28);
            var popRot = Quaternion.Euler(30f, 0f, 0f) * Quaternion.Euler(0, -90, 0);
            var coneGo = Child(dir, confetti, "Popper", cone, m.gold, new Vector3(-0.32f, 0.08f, -0.08f), popRot);
            var stripe = Revolve("Stripe", Circle(new Vector2(0.02f, 0.066f), 0.018f, 8), true, 28);
            Child(dir, coneGo, "Stripe", stripe, m.pink, Vector3.zero, Quaternion.identity);
            var mouth = new GameObject("Mouth"); mouth.transform.SetParent(coneGo.transform, false); mouth.transform.localPosition = new Vector3(-0.17f, 0, 0);
            var conf = Particles(mouth.transform, "Confetti", m.confettiFx, Quaternion.Euler(0, -90, 0), false, 0, 55, 2.2f, new Vector2(2.5f, 4.5f), new Vector2(0.05f, 0.09f), 0.9f,
                new ParticleSystem.MinMaxGradient(grad) { mode = ParticleSystemGradientMode.RandomColor }, 1f);
            var cshape = conf.shape; cshape.angle = 28f;
            var crot = conf.rotationOverLifetime; crot.enabled = true; crot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            list.Add(("exhaust_confetti", Save(dir, confetti, "confetti", conf), "fx_confetti"));

            return list;
        }

        // ---------------- yardımcılar ----------------

        /// <summary>Arkaya bakan boru (+ koyu ağız). Efektin çıkacağı noktayı döndürür.</summary>
        static Transform Pipe(string dir, Transform parent, string name, Vector3 pos, float r, float len, Material mat, Material dark)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var prof = new List<Vector2> { new Vector2(len * 0.5f, 0f), new Vector2(len * 0.5f, r), new Vector2(-len * 0.5f + 0.02f, r), new Vector2(-len * 0.5f, r * 1.25f), new Vector2(-len * 0.5f - 0.015f, r * 1.25f), new Vector2(-len * 0.5f - 0.015f, 0f) };
            // Euler(0,-90,0): profilin -X ucu (ağız) arkaya (-Z) bakar
            Child(dir, g, name + "_Pipe", Revolve(name, prof, false, 24), mat, new Vector3(0, 0, -len * 0.5f + 0.06f), Quaternion.Euler(0, -90, 0));
            Child(dir, g, name + "_Hole", Ellipsoid(new Vector3(r * 0.8f, r * 0.8f, 0.01f), 8, 16), dark, new Vector3(0, 0, -len + 0.05f), Quaternion.identity);
            var tip = new GameObject("Tip"); tip.transform.SetParent(g.transform, false); tip.transform.localPosition = new Vector3(0, 0, -len + 0.03f);
            return tip.transform;
        }

        static ParticleSystem Smoke(Transform root, Transform tip, Material mat, Color c, float speed, float size) =>
            Particles(tip, "Smoke", mat, Quaternion.Euler(-12, 180, 0), true, 28, 0, 0.9f, new Vector2(speed, speed * 1.5f), new Vector2(size * 0.3f, size * 0.45f), -0.08f,
                new ParticleSystem.MinMaxGradient(c), 0.85f);

        static ParticleSystem Particles(Transform parent, string name, Material mat, Quaternion rot, bool loop, float rate, int burst,
                                        float life, Vector2 speed, Vector2 size, float gravity, ParticleSystem.MinMaxGradient color, float alpha)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = rot;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop; main.playOnAwake = false; main.duration = loop ? 1f : 0.5f;
            main.startLifetime = life; main.startSpeed = new ParticleSystem.MinMaxCurve(speed.x, speed.y);
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = color; main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 16f; sh.radius = 0.03f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(alpha, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.3f));
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        static GameObject Child(string dir, GameObject parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot)
        {
            AssetDatabase.CreateAsset(mesh, $"{dir}/Meshes/Exhaust_{name}.asset");
            var g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos; g.transform.localRotation = rot;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        static GameObject Child(string dir, Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot) =>
            Child(dir, parent.gameObject, name, mesh, mat, pos, rot);

        static GameObject Save(string dir, GameObject root, string soundId, params ParticleSystem[] fx)
        {
            var em = root.AddComponent<ExhaustEmitter>();
            em.soundId = soundId;
            em.effects = fx;
            var p = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/Prefabs/{root.name}.prefab");
            Object.DestroyImmediate(root);
            return p;
        }
    }
}
