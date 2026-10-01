using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// "Oyuncak stüdyosu" sahnesi: pastel gökyüzü kubbesi, ufukta yumuşak tepeler, süzülen bulutlar,
    /// ampullü döner platform, dokununca gece/gündüz değiştiren güneş-ay, uçuşan yapraklar,
    /// gece ateşböcekleri ve arabayı takip eden gölge.
    /// </summary>
    public static class StudioBuilder
    {
        public class Setup
        {
            public string dir;
            public Transform carRoot;
            public Light light;
            public Sprite petal, softDot, shadow;
            public Material shadowMat;
        }

        public static StageController Build(Setup inp)
        {
            string dir = inp.dir;
            var stage = new GameObject("Stage");
            var ctrl = stage.AddComponent<StageController>();

            // Gökyüzü
            var skyMat = new Material(Shader.Find("HayalGaraji/Sky"));
            AssetDatabase.CreateAsset(skyMat, $"{dir}/Materials/Sky.mat");
            var sky = MeshObj(dir, stage.transform, "SkyDome", Ellipsoid(Vector3.one * 90f, 24, 36), skyMat, Vector3.zero);
            sky.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Platform: yuvarlak kenarlı pasta dilimi gibi
            var platMat = Toon(dir, "Platform", new Color32(228, 248, 251, 255), 0.25f);
            platMat.SetFloat("_OutlinePx", 1.6f);
            var platProf = new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0f, 3.2f), new Vector2(-0.02f, 3.32f), new Vector2(-0.08f, 3.4f),
                new Vector2(-0.18f, 3.42f), new Vector2(-0.28f, 3.36f), new Vector2(-0.34f, 3.2f), new Vector2(-0.36f, 0f)
            };
            MeshObj(dir, stage.transform, "Platform", Revolve("Platform", platProf, false, 96, true), platMat, Vector3.zero);

            // Platform süsleri: iç halkalar ve kenar şeridi
            var ringMat = Toon(dir, "PlatformRing", new Color32(120, 214, 236, 255), 0.2f);
            var rings = new List<(Mesh, Matrix4x4)>();
            foreach (float r in new[] { 2.0f, 2.7f })
                rings.Add((Revolve("R", Circle(new Vector2(0.0f, r), 0.035f, 10), true, 96, true), Matrix4x4.identity));
            rings.Add((Revolve("Edge", Circle(new Vector2(-0.14f, 3.44f), 0.07f, 12), true, 96, true), Matrix4x4.identity));
            MeshObj(dir, stage.transform, "PlatformRings", Combine("PlatformRings", rings), ringMat, Vector3.zero);

            // Ampuller
            var bulbMesh = Ellipsoid(Vector3.one * 0.07f, 8, 12);
            AssetDatabase.CreateAsset(bulbMesh, $"{dir}/Meshes/Bulb.asset");
            var bulbMat = Toon(dir, "Bulb", Color.white, 0f);
            var bulbs = new List<Renderer>();
            for (int i = 0; i < 28; i++)
            {
                float a = i / 28f * Mathf.PI * 2f;
                var b = new GameObject("Bulb");
                b.transform.SetParent(stage.transform, false);
                b.transform.localPosition = new Vector3(Mathf.Cos(a) * 3.47f, -0.1f, Mathf.Sin(a) * 3.47f);
                b.AddComponent<MeshFilter>().sharedMesh = bulbMesh;
                var br = b.AddComponent<MeshRenderer>(); br.sharedMaterial = bulbMat;
                bulbs.Add(br);
            }

            // Yer: platformdan ufka uzanan masal çayırı (gökyüzüyle net ayrışır)
            var groundMat = new Material(Shader.Find("HayalGaraji/Ground"));
            AssetDatabase.CreateAsset(groundMat, $"{dir}/Materials/Ground.mat");
            MeshObj(dir, stage.transform, "Meadow", Disc(new[] { 0f, 3f, 5f, 8f, 12f, 18f, 26f, 38f, 55f, 80f, 115f, 160f }, 128), groundMat, new Vector3(0, -0.3f, 0));

            // Çayırda minik çiçekler ve yuvarlak çalılar (platformun dışında, kameranın yolunu kapatmadan)
            var rnd = new System.Random(3);
            var petalsPink = new List<(Mesh, Matrix4x4)>(); var petalsWhite = new List<(Mesh, Matrix4x4)>(); var centers = new List<(Mesh, Matrix4x4)>();
            for (int i = 0; i < 44; i++)
            {
                float a = (float)rnd.NextDouble() * Mathf.PI * 2f, d = 4.4f + (float)rnd.NextDouble() * 12f;
                var c = new Vector3(Mathf.Cos(a) * d, -0.24f, Mathf.Sin(a) * d);
                var list = i % 3 == 0 ? petalsWhite : petalsPink;
                float s = 0.8f + (float)rnd.NextDouble() * 0.5f, rot = (float)rnd.NextDouble() * 360f;
                for (int k = 0; k < 5; k++)
                {
                    float pa = (rot + k * 72f) * Mathf.Deg2Rad;
                    list.Add((Ellipsoid(new Vector3(0.07f * s, 0.025f * s, 0.045f * s), 8, 10),
                              TRS(c + new Vector3(Mathf.Cos(pa), 0, Mathf.Sin(pa)) * 0.065f * s, Quaternion.Euler(0, -pa * Mathf.Rad2Deg, 0))));
                }
                centers.Add((Ellipsoid(Vector3.one * 0.035f * s, 8, 10), At(c + Vector3.up * 0.015f)));
            }
            var flowerPink = Toon(dir, "FlowerPink", new Color32(255, 150, 200, 255), 0.2f); flowerPink.SetFloat("_OutlinePx", 1.2f);
            var flowerWhite = Toon(dir, "FlowerWhite", Color.white, 0.2f); flowerWhite.SetFloat("_OutlinePx", 1.2f);
            var flowerCenter = Toon(dir, "FlowerCenter", new Color32(255, 214, 60, 255), 0.2f); flowerCenter.SetFloat("_OutlinePx", 0f);
            MeshObj(dir, stage.transform, "FlowersPink", Combine("FlowersPink", petalsPink), flowerPink, Vector3.zero);
            MeshObj(dir, stage.transform, "FlowersWhite", Combine("FlowersWhite", petalsWhite), flowerWhite, Vector3.zero);
            MeshObj(dir, stage.transform, "FlowerCenters", Combine("FlowerCenters", centers), flowerCenter, Vector3.zero);

            var bushes = new List<(Mesh, Matrix4x4)>();
            for (int i = 0; i < 12; i++)
            {
                float a = (i + (float)rnd.NextDouble() * 0.6f) / 12f * Mathf.PI * 2f, d = 8f + (float)rnd.NextDouble() * 9f;
                var c = new Vector3(Mathf.Cos(a) * d, -0.3f, Mathf.Sin(a) * d);
                float s = 0.55f + (float)rnd.NextDouble() * 0.4f;
                bushes.Add((Ellipsoid(new Vector3(0.55f, 0.45f, 0.55f) * s, 12, 18), At(c + new Vector3(0, 0.25f, 0) * s)));
                bushes.Add((Ellipsoid(new Vector3(0.4f, 0.35f, 0.4f) * s, 12, 18), At(c + new Vector3(0.45f, 0.18f, 0.1f) * s)));
                bushes.Add((Ellipsoid(new Vector3(0.38f, 0.32f, 0.38f) * s, 12, 18), At(c + new Vector3(-0.4f, 0.16f, -0.12f) * s)));
            }
            var bushMat = Toon(dir, "Bush", new Color32(96, 190, 120, 255), 0.15f);
            MeshObj(dir, stage.transform, "Bushes", Combine("Bushes", bushes), bushMat, Vector3.zero);

            // Ufuk: önde çayırdan koyu yeşil tepeler, arkada maviye çalan lila tepeler (derinlik)
            var hillFront = Toon(dir, "HillFront", new Color32(110, 196, 140, 255), 0f);
            var hillBack = Toon(dir, "HillBack", new Color32(168, 176, 240, 255), 0f);
            foreach (var hm in new[] { hillFront, hillBack }) { hm.SetFloat("_OutlinePx", 0f); hm.SetFloat("_RimStrength", 0.5f); hm.SetFloat("_RimPower", 2.2f); }
            var hillsBackGo = MeshObj(dir, stage.transform, "HillsBack", HillRing(82f, 30f, -4f, 11f, 5, 1.7f), hillBack, Vector3.zero);
            var hillsFrontGo = MeshObj(dir, stage.transform, "HillsFront", HillRing(58f, 24f, -4f, 7.5f, 7, 0.4f), hillFront, Vector3.zero);

            // Bulutlar (yavaşça döner)
            var cloudRoot = new GameObject("Clouds");
            cloudRoot.transform.SetParent(stage.transform, false);
            var cloudMat = Toon(dir, "Cloud", Color.white, 0f);
            cloudMat.SetFloat("_OutlinePx", 1.4f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f, d = 30f + (float)rnd.NextDouble() * 8f, y = 9f + (float)rnd.NextDouble() * 5f;
                var puffs = new List<(Mesh, Matrix4x4)>();
                int n = 3 + rnd.Next(3);
                for (int k = 0; k < n; k++)
                {
                    float r = 1.3f + (float)rnd.NextDouble() * 0.9f;
                    puffs.Add((Ellipsoid(new Vector3(r, r * 0.8f, r)), At(new Vector3((k - n / 2f) * 1.3f, (float)rnd.NextDouble() * 0.5f, (float)rnd.NextDouble() * 0.6f))));
                }
                var c = MeshObj(dir, cloudRoot.transform, "Cloud", Combine("Cloud", puffs), cloudMat, new Vector3(Mathf.Cos(a) * d, y, Mathf.Sin(a) * d));
                c.transform.LookAt(new Vector3(0, y, 0));
            }

            // Güneş / ay: dokununca gece-gündüz
            var celMat = Toon(dir, "Celestial", new Color32(255, 210, 63, 255), 0.3f);
            var cel = MeshObj(dir, stage.transform, "SunMoon", Ellipsoid(Vector3.one * 2.2f, 16, 24), celMat, new Vector3(10f, 9f, 24f));
            cel.AddComponent<SphereCollider>().radius = 2.6f;
            var tap = cel.AddComponent<StageTapTarget>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(tap.onTap, new UnityEngine.Events.UnityAction(ctrl.ToggleNight));

            // Yapraklar (gündüz) ve ateşböcekleri (gece)
            var petalMat = SpriteMat(dir, "Petal", inp.petal);
            var petals = Particles(stage.transform, "Petals", petalMat, new Vector3(0, 7, 0), new Vector3(16, 1, 16), 7f, 12f, 0.09f, 0.16f, 0.03f, true);
            var fly = Particles(stage.transform, "Fireflies", SpriteMat(dir, "Firefly", inp.softDot), new Vector3(0, 1.5f, 0), new Vector3(10, 2.5f, 10), 6f, 5f, 0.06f, 0.12f, -0.01f, false);
            var flyMain = fly.main; flyMain.startColor = new Color(1f, 0.95f, 0.55f);

            // Gece gökyüzü: gezegenler (gece "pıt" diye belirir) ve kayan yıldızlar
            var night = new GameObject("NightSky");
            night.transform.SetParent(stage.transform, false);
            GameObject Planet(string name, float angleDeg, float dist, float height, float radius, Color col)
            {
                float a = angleDeg * Mathf.Deg2Rad;
                var pm = Toon(dir, "Planet_" + name, col, 0.35f);
                pm.SetColor("_Emission", col * 0.35f); pm.SetFloat("_OutlinePx", 1.4f);
                var p = MeshObj(dir, night.transform, name, Ellipsoid(Vector3.one * radius, 18, 28), pm, new Vector3(Mathf.Cos(a) * dist, height, Mathf.Sin(a) * dist));
                p.transform.LookAt(new Vector3(0, height * 0.3f, 0)); // yüzü sahneye dönük
                return p;
            }
            Material Mat(string n, Color c, float emi) { var m = Toon(dir, n, c, 0.2f); m.SetColor("_Emission", c * emi); m.SetFloat("_OutlinePx", 1.2f); return m; }
            void Add(GameObject parent, string n, Mesh mesh, Material mat, Vector3 local, Quaternion rot)
            {
                var g = MeshObj(dir, parent.transform, n, mesh, mat, local);
                g.transform.localRotation = rot;
            }

            // 1) Halkalı, uykulu yüzlü lila gezegen
            var sleepy = Planet("Sleepy", 35f, 40f, 6f, 3.2f, new Color32(190, 168, 255, 255));
            var planetRingMat = Mat("PlanetRing", new Color32(255, 160, 214, 255), 0.4f);
            Add(sleepy, "Ring", Revolve("Ring", Circle(new Vector2(0, 5.0f), 0.32f, 12), true, 96), planetRingMat, Vector3.zero, Quaternion.Euler(18f, 0f, -12f) * Quaternion.Euler(0, 0, 90));
            var faceDark = Mat("PlanetFace", new Color32(70, 50, 120, 255), 0f);
            var faceBlush = Mat("PlanetBlush", new Color32(255, 150, 200, 255), 0.3f);
            foreach (float sx in new[] { -1f, 1f })
            {
                for (int k = 0; k < 5; k++)
                {
                    float t = Mathf.Lerp(200f, 340f, k / 4f) * Mathf.Deg2Rad;
                    Add(sleepy, "Eye", Ellipsoid(Vector3.one * 0.16f, 8, 10), faceDark, new Vector3(sx * 1.05f + Mathf.Cos(t) * 0.5f, 0.55f + Mathf.Sin(t) * 0.22f, 3.05f), Quaternion.identity);
                }
                Add(sleepy, "Blush", Ellipsoid(new Vector3(0.55f, 0.3f, 0.15f), 10, 14), faceBlush, new Vector3(sx * 1.6f, -0.15f, 2.85f), Quaternion.identity);
            }
            for (int k = 0; k < 5; k++)
            {
                float t = Mathf.Lerp(210f, 330f, k / 4f) * Mathf.Deg2Rad;
                Add(sleepy, "Smile", Ellipsoid(Vector3.one * 0.13f, 8, 10), faceDark, new Vector3(Mathf.Cos(t) * 0.45f, -0.35f + Mathf.Sin(t) * 0.22f, 3.12f), Quaternion.identity);
            }

            // 2) Benekli nane şekeri gezegen
            var candy = Planet("Candy", 150f, 38f, 5f, 2.3f, new Color32(150, 232, 200, 255));
            var spotMat = Mat("PlanetSpot", new Color32(255, 130, 190, 255), 0.25f);
            var spots = new[] { new Vector3(0.6f, 0.9f, 1.95f), new Vector3(-1.1f, 0.2f, 1.9f), new Vector3(0.3f, -1.0f, 1.95f), new Vector3(1.5f, -0.2f, 1.6f), new Vector3(-0.4f, 1.6f, 1.45f) };
            foreach (var sp in spots)
                Add(candy, "Spot", Ellipsoid(new Vector3(0.45f, 0.45f, 0.12f), 10, 14), spotMat, sp, Quaternion.LookRotation(sp.normalized));

            // 3) Halkalı küçük turuncu gezegen
            var orange = Planet("Orange", 255f, 38f, 5.2f, 1.8f, new Color32(255, 176, 90, 255));
            var ring2 = Mat("PlanetRing2", new Color32(255, 225, 110, 255), 0.4f);
            Add(orange, "Ring", Revolve("Ring2", Circle(new Vector2(0, 2.4f), 0.18f, 10), true, 72), ring2, Vector3.zero, Quaternion.Euler(-24f, 0f, 20f) * Quaternion.Euler(0, 0, 90));

            // 4) Minik mavi gezegen
            Planet("Tiny", 320f, 34f, 4.2f, 1.1f, new Color32(120, 190, 255, 255));

            // Kayan yıldızlar
            var shooting = Particles(stage.transform, "ShootingStars", SpriteMat(dir, "ShootingStar", inp.softDot), new Vector3(0, 20f, 0), new Vector3(70, 1, 70), 0.35f, 1.4f, 0.35f, 0.5f, 0f, false);
            shooting.transform.localRotation = Quaternion.Euler(28f, 35f, 0f);
            var sMain = shooting.main; sMain.startSpeed = 24f;
            var sNoise = shooting.noise; sNoise.enabled = false;
            var sRot = shooting.rotationOverLifetime; sRot.enabled = false;
            var sRend = shooting.GetComponent<ParticleSystemRenderer>();
            sRend.renderMode = ParticleSystemRenderMode.Stretch; sRend.lengthScale = 10f; sRend.velocityScale = 0.05f;
            for (int i = 0; i < night.transform.childCount; i++) night.transform.GetChild(i).localScale = Vector3.zero;
            night.SetActive(false);

            // Gölge
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(shadow.GetComponent<Collider>());
            shadow.name = "CarShadow";
            shadow.transform.SetParent(stage.transform, false);
            shadow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shadow.transform.localScale = new Vector3(2.3f, 3.0f, 1f);
            shadow.GetComponent<MeshRenderer>().sharedMaterial = inp.shadowMat;

            // Işık
            inp.light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            inp.light.shadows = LightShadows.None;

            ctrl.skyMaterial = skyMat; ctrl.groundMaterial = groundMat; ctrl.sun = inp.light; ctrl.celestial = cel.GetComponent<MeshRenderer>();
            ctrl.clouds = cloudRoot.transform; ctrl.bulbs = bulbs.ToArray();
            ctrl.bulbColors = new[] { new Color(1f, 0.82f, 0.25f), new Color(1f, 0.48f, 0.85f), new Color(0.42f, 0.83f, 1f) };
            ctrl.petals = petals; ctrl.fireflies = fly; ctrl.carRoot = inp.carRoot; ctrl.shadow = shadow.transform;
            ctrl.nightSky = night.transform; ctrl.shootingStars = shooting;
            // Gece sisi: ön tepeler koyu mavi-yeşile, arka tepeler ufuk moruna doğru solar
            ctrl.hills = new Renderer[] { hillsFrontGo.GetComponent<MeshRenderer>(), hillsBackGo.GetComponent<MeshRenderer>() };
            ctrl.hillDay = new[] { hillFront.GetColor("_BaseColor"), hillBack.GetColor("_BaseColor") }; // bizim shader "_Color" değil "_BaseColor" kullanır
            ctrl.hillNight = new[] { new Color(0.22f, 0.3f, 0.44f), new Color(0.34f, 0.27f, 0.55f) };
            return ctrl;
        }

        // ---------------- yardımcılar ----------------

        /// <summary>Yukarı bakan düz, halkalı disk (çayır).</summary>
        static Mesh Disc(float[] radii, int segs)
        {
            var v = new List<Vector3> { Vector3.zero };
            for (int r = 1; r < radii.Length; r++)
                for (int j = 0; j < segs; j++)
                {
                    float a = 2f * Mathf.PI * j / segs;
                    v.Add(new Vector3(Mathf.Cos(a) * radii[r], 0f, Mathf.Sin(a) * radii[r]));
                }
            var tris = new List<int>();
            for (int j = 0; j < segs; j++) { tris.Add(0); tris.Add(1 + (j + 1) % segs); tris.Add(1 + j); }
            for (int r = 0; r < radii.Length - 2; r++)
                for (int j = 0; j < segs; j++)
                {
                    int j2 = (j + 1) % segs, a = 1 + r * segs + j, b = 1 + r * segs + j2, c = a + segs, d = b + segs;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            var va = v.ToArray();
            // Yukarı baksın
            var n0 = Vector3.Cross(va[tris[1]] - va[tris[0]], va[tris[2]] - va[tris[0]]);
            if (n0.y < 0) for (int k = 0; k < tris.Count; k += 3) { int t = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = t; }
            var normals = new Vector3[va.Length];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.up;
            return ToMesh("Meadow", va, normals, null, tris);
        }

        /// <summary>
        /// Merkez etrafında kesintisiz tepe halkası. Yükseklik, farklı frekanslı yumuşak dalgaların
        /// toplamıyla değişir: tekrar etmeyen ama sakin bir ufuk çizgisi.
        /// </summary>
        static Mesh HillRing(float radius, float width, float baseY, float height, int waves, float phase)
        {
            const int S = 160, U = 12;
            var v = new Vector3[S * (U + 1)];
            for (int j = 0; j < S; j++)
            {
                float phi = 2f * Mathf.PI * j / S;
                float h = 0.62f + 0.24f * Mathf.Sin(phi * waves + phase) + 0.14f * Mathf.Sin(phi * (waves * 2 + 1) + phase * 2.3f);
                for (int u = 0; u <= U; u++)
                {
                    float t = (float)u / U;
                    float hump = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * t)), 0.8f); // sin(π) float'ta eksi çıkabilir → NaN olmasın
                    float r = radius + t * width;
                    v[j * (U + 1) + u] = new Vector3(Mathf.Cos(phi) * r, baseY + hump * height * h, Mathf.Sin(phi) * r);
                }
            }
            var tris = new List<int>();
            for (int j = 0; j < S; j++)
            {
                int j2 = (j + 1) % S;
                for (int u = 0; u < U; u++)
                {
                    int a = j * (U + 1) + u, b = a + 1, c = j2 * (U + 1) + u, d = c + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(b); tris.Add(d); tris.Add(c);
                }
            }
            // Yüzey merkeze ve yukarı baksın (kamera halkanın içinde)
            int ta = U / 4;
            var nrm = Vector3.Cross(v[ta + 1] - v[ta], v[(U + 1) + ta] - v[ta]);
            var want = -new Vector3(v[ta].x, 0, v[ta].z).normalized + Vector3.up;
            if (Vector3.Dot(nrm, want) < 0)
                for (int k = 0; k < tris.Count; k += 3) { int t = tris[k + 1]; tris[k + 1] = tris[k + 2]; tris[k + 2] = t; }
            return ToMesh("HillRing", v, ComputeNormals(v, tris), null, tris);
        }

        public static Material Toon(string dir, string name, Color c, float gloss)
        {
            var m = new Material(Shader.Find("HayalGaraji/Toon"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_GlossStrength", gloss);
            AssetDatabase.CreateAsset(m, $"{dir}/Materials/{name}.mat");
            return m;
        }

        static Material SpriteMat(string dir, string name, Sprite s)
        {
            var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = s.texture };
            AssetDatabase.CreateAsset(m, $"{dir}/Materials/{name}.mat");
            return m;
        }

        static int meshCounter;

        static GameObject MeshObj(string dir, Transform parent, string name, Mesh mesh, Material mat, Vector3 pos)
        {
            AssetDatabase.CreateAsset(mesh, $"{dir}/Meshes/Stage_{name}_{meshCounter++}.asset");
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        static ParticleSystem Particles(Transform parent, string name, Material mat, Vector3 pos, Vector3 box,
                                        float rate, float life, float minSize, float maxSize, float gravity, bool play)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = play; main.startLifetime = life; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = gravity; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.35f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }
    }
}
