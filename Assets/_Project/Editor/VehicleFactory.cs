using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Chibi araç fabrikası. Gövde, süper-elips kesitlerin yumuşakça birleştirilmesiyle (loft) üretilir:
    /// kısa, tombul, "kafası büyük" oranlar; keskin köşe yok. Boyama için gövdenin UV'si tekildir.
    /// Eksen: ileri +Z, yukarı +Y. Yeni araçlar aynı fonksiyonun farklı ölçüleriyle üretilir.
    /// </summary>
    public static class VehicleFactory
    {
        public class Mats
        {
            public Material body, roof, glass, seat, dark, pink, white;
            // Yüz: gerçek 3D şekiller (parlak göz kubbesi, iris, göz bebeği, ışıltı)
            public Material faceWhite, faceDark, iris, sparkle, blush, starYellow, heartPink;
            // Süspansiyon: krom çubuk + jant rengini alan yay
            public Material chrome, spring;
            // İç mekân: gösterge düğmeleri
            public Material red, yellow, blue;
        }

        /// <summary>Bir chibi aracın ölçüleri. Tüm araçlar bu tarifin farklı sayılarıyla oluşur.</summary>
        public class Spec
        {
            public string name = "Car_Spor";
            // Chibi oranlar: kısa, tombul, yüksek; tekerlekler gövdenin altına sokulmuş
            public float length = 1.85f, width = 1.46f, clearance = 0.16f;
            public float topRear = 0.74f, topFront = 0.64f;    // gövde üst çizgisi (kaput önde biraz alçak)
            public float endRound = 3.4f;                      // uç yuvarlaklığı (büyük = daha dolgun uçlar)
            public float widthKeep = 0.3f;                     // genişliğin uçlara kadar korunması (küçük = daha dolgun)
            public float bodyExp = 3.4f;                       // kesit köşeliliği (2 = elips, 4 = yumuşak kutu)
            public float cabinZ0 = -0.56f, cabinZ1 = 0.34f, cabinHalfW = 0.57f;
            public float cabinBottom = 0.5f, cabinTop = 1.28f, cabinTopFront = 1.16f;
            public float wheelR = 0.38f, wheelX = 0.64f, wheelZ = 0.56f;
            public float eyeY = 0.37f, eyeSep = 0.27f, eyeSize = 0.3f;
        }

        public static GameObject Build(string dir, Spec sp, Mats m)
        {
            var root = new GameObject(sp.name);
            var ch = root.AddComponent<CarChassis>();
            var bodyRoot = new GameObject("Body");
            bodyRoot.transform.SetParent(root.transform, false);
            bodyRoot.transform.localPosition = new Vector3(0, sp.clearance, 0);

            // ---------- Gövde ----------
            var secs = new List<Section>();
            const int N = 46;
            for (int i = 0; i < N; i++)
            {
                float t = CosSpace(i, N);
                float s = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * t - 1f), sp.endRound)), 1f / sp.endRound);
                float top = Mathf.Lerp(sp.topRear, sp.topFront, Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.6f));
                float bottom = 0.12f * (1f - s);
                secs.Add(new Section
                {
                    z = Mathf.Lerp(-sp.length / 2f, sp.length / 2f, t),
                    halfW = sp.width / 2f * Mathf.Pow(s, sp.widthKeep),
                    center = (top + bottom) / 2f,
                    halfH = (top - bottom) / 2f * Mathf.Pow(s, 0.9f)
                });
            }
            var bodyLoft = Loft(secs, 56, sp.bodyExp);
            var bodyMesh = ToMesh(sp.name + "_Body", bodyLoft.v, bodyLoft.n, bodyLoft.uv, bodyLoft.tris);
            AssetDatabase.CreateAsset(bodyMesh, $"{dir}/Meshes/{sp.name}_Body.asset");
            var bodyGo = new GameObject("BodyMesh");
            bodyGo.transform.SetParent(bodyRoot.transform, false);
            bodyGo.AddComponent<MeshFilter>().sharedMesh = bodyMesh;
            var bodyRend = bodyGo.AddComponent<MeshRenderer>(); bodyRend.sharedMaterial = m.body;
            bodyGo.AddComponent<MeshCollider>().sharedMesh = bodyMesh;
            bodyGo.AddComponent<PaintableSurface>();
            bodyGo.layer = LayerMask.NameToLayer("CarBody");

            // ---------- Kabin: kemer (gövde rengi) + cam + tavan ----------
            var csecs = new List<Section>();
            const int NC = 34;
            for (int i = 0; i < NC; i++)
            {
                float t = CosSpace(i, NC);
                float s = Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(Mathf.Abs(2f * t - 1f), 2.2f)), 1f / 2.2f);
                float top = Mathf.Lerp(sp.cabinTop, sp.cabinTopFront, Mathf.SmoothStep(0f, 1f, (t - 0.5f) / 0.5f));
                csecs.Add(new Section
                {
                    z = Mathf.Lerp(sp.cabinZ0, sp.cabinZ1, t),
                    halfW = sp.cabinHalfW * Mathf.Pow(s, 0.5f),
                    center = (top + sp.cabinBottom) / 2f,
                    halfH = (top - sp.cabinBottom) / 2f * Mathf.Pow(s, 0.7f)
                });
            }
            var cab = Loft(csecs, 48, 2.8f);
            // Kesit açısına göre: alt = kemer (gövde rengi), geri kalanı cam baloncuk
            var groups = SplitByAngle(cab, 2, sinTh => sinTh < -0.2f ? 0 : 1);
            MeshChild(dir, bodyRoot, sp.name, "CabinBelt", ToMesh("Belt", cab.v, cab.n, null, groups[0]), m.body, TintChannel.Body);
            MeshChild(dir, bodyRoot, sp.name, "CabinGlass", ToMesh("Glass", cab.v, cab.n, null, groups[1]), m.glass, null);
            // Tavan: baloncuğun üstüne oturan yumuşak bir kapak (şapka gibi); camın temiz kalması için ayrı parça
            float capTop = (sp.cabinTop + sp.cabinTopFront) * 0.5f;
            var cap = MeshChild(dir, bodyRoot, sp.name, "CabinRoof",
                Ellipsoid(new Vector3(sp.cabinHalfW * 0.9f, 0.075f, (sp.cabinZ1 - sp.cabinZ0) * 0.4f), 18, 28), m.roof, TintChannel.Roof);
            cap.transform.localPosition = new Vector3(0f, capTop - 0.015f, (sp.cabinZ0 + sp.cabinZ1) * 0.5f - 0.04f);
            cap.transform.localRotation = Quaternion.Euler(Mathf.Atan2(sp.cabinTop - sp.cabinTopFront, sp.cabinZ1 - sp.cabinZ0) * Mathf.Rad2Deg, 0f, 0f);

            // ---------- Yüz: 3D anime gözler, gülümseme, yanak pembeliği ----------
            // Her parça gövde yüzeyine oturtulur ve yüzeyin eğimine göre döndürülür (yerel +Z = dışarı).
            float frontAtEye = FrontZ(secs, sp.eyeY, 0f, sp.bodyExp);
            var face = new GameObject("Face");
            face.transform.SetParent(bodyRoot.transform, false);
            face.transform.localPosition = new Vector3(0, sp.eyeY, frontAtEye);
            void OnSurface(Transform t, float x, float y, float size, float lift)
            {
                float z = FrontZ(secs, y, x, sp.bodyExp);
                var n = SurfaceNormal(secs, x, y, sp.bodyExp);
                t.position = bodyRoot.transform.TransformPoint(new Vector3(x, y, z) + n * lift);
                t.rotation = bodyRoot.transform.rotation * Quaternion.LookRotation(n, Vector3.up);
                t.localScale = Vector3.one * size;
            }
            var meshCache = new Dictionary<string, Mesh>();
            Mesh Ell(float rx, float ry, float rz)
            {
                string key = $"{rx:0.###}_{ry:0.###}_{rz:0.###}";
                if (meshCache.TryGetValue(key, out var mm)) return mm;
                mm = Ellipsoid(new Vector3(rx, ry, rz), 14, 22);
                AssetDatabase.CreateAsset(mm, $"{dir}/Meshes/{sp.name}_Face_{meshCache.Count}.asset");
                meshCache[key] = mm;
                return mm;
            }
            GameObject Grp(GameObject parent, string n2)
            {
                var g = new GameObject(n2);
                g.transform.SetParent(parent.transform, false);
                return g;
            }
            void Piece(GameObject parent, string n2, Mesh mesh, Material mat, Vector3 pos, float rotZ = 0f)
            {
                var g = new GameObject(n2);
                g.transform.SetParent(parent.transform, false);
                g.transform.localPosition = pos;
                g.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
                g.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            void Arc(GameObject parent, float from, float to, float rx, float ry, Vector3 c, float r, int count)
            {
                for (int k = 0; k < count; k++)
                {
                    float a = Mathf.Lerp(from, to, count == 1 ? 0.5f : (float)k / (count - 1)) * Mathf.Deg2Rad;
                    Piece(parent, "Dot", Ell(r, r, r * 0.8f), m.faceDark, c + new Vector3(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry, 0));
                }
            }

            foreach (var (eyeName, x) in new[] { ("EyeL", -sp.eyeSep), ("EyeR", sp.eyeSep) })
            {
                var e = new GameObject(eyeName);
                e.transform.SetParent(face.transform, false);
                OnSurface(e.transform, x, sp.eyeY, sp.eyeSize, -0.012f);
                // Boya modunda göze dokunulabilsin (iris rengi)
                e.layer = LayerMask.NameToLayer("CarZone");
                var eyeCol = e.AddComponent<SphereCollider>(); eyeCol.isTrigger = true; eyeCol.radius = 0.55f; eyeCol.center = new Vector3(0, 0, 0.12f);
                e.AddComponent<EyeTapZone>();

                var open = Grp(e, "Open");
                Piece(open, "White", Ell(0.42f, 0.5f, 0.2f), m.faceWhite, Vector3.zero);
                // Normal iris: renklendirilebilir iris + göz bebeği + iki ışıltı
                var irisN = Grp(open, "Iris_normal");
                Piece(irisN, "Iris", Ell(0.29f, 0.35f, 0.06f), m.iris, new Vector3(0, -0.04f, 0.15f));
                Piece(irisN, "Pupil", Ell(0.15f, 0.18f, 0.04f), m.faceDark, new Vector3(0, -0.07f, 0.19f));
                Piece(irisN, "Sparkle", Ell(0.1f, 0.1f, 0.03f), m.sparkle, new Vector3(-0.1f, 0.1f, 0.215f));
                Piece(irisN, "Sparkle2", Ell(0.045f, 0.045f, 0.02f), m.sparkle, new Vector3(0.1f, -0.16f, 0.21f));
                // Yıldız gözler
                var irisS = Grp(open, "Iris_yildiz");
                Piece(irisS, "Back", Ell(0.3f, 0.36f, 0.05f), m.faceDark, new Vector3(0, -0.03f, 0.15f));
                for (int k = 0; k < 5; k++)
                {
                    float a = 90f + k * 72f;
                    Piece(irisS, "Ray", Ell(0.07f, 0.17f, 0.03f), m.starYellow,
                          new Vector3(Mathf.Cos(a * Mathf.Deg2Rad) * 0.11f, Mathf.Sin(a * Mathf.Deg2Rad) * 0.11f - 0.03f, 0.19f), a - 90f);
                }
                Piece(irisS, "Core", Ell(0.09f, 0.09f, 0.03f), m.starYellow, new Vector3(0, -0.03f, 0.2f));
                Piece(irisS, "Sparkle", Ell(0.05f, 0.05f, 0.02f), m.sparkle, new Vector3(-0.12f, 0.14f, 0.21f));
                irisS.SetActive(false);
                // Kalp gözler
                var irisH = Grp(open, "Iris_kalp");
                Piece(irisH, "LobeL", Ell(0.13f, 0.21f, 0.05f), m.heartPink, new Vector3(-0.08f, -0.02f, 0.17f), 35f);
                Piece(irisH, "LobeR", Ell(0.13f, 0.21f, 0.05f), m.heartPink, new Vector3(0.08f, -0.02f, 0.17f), -35f);
                Piece(irisH, "Sparkle", Ell(0.045f, 0.045f, 0.02f), m.sparkle, new Vector3(-0.1f, 0.06f, 0.21f));
                irisH.SetActive(false);
                // Mutlu (^ ^) ve uykulu (‿) kapalı gözler
                var happy = Grp(e, "Happy");
                Arc(happy, 20f, 160f, 0.26f, 0.22f, new Vector3(0, -0.1f, 0.1f), 0.07f, 9);
                happy.SetActive(false);
                var sleepy = Grp(e, "Sleepy");
                Arc(sleepy, 200f, 340f, 0.27f, 0.1f, new Vector3(0, 0.04f, 0.1f), 0.065f, 9);
                sleepy.SetActive(false);
            }

            // Gülümseme
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(face.transform, false);
            float mouthY = sp.eyeY - sp.eyeSize * 0.66f;
            OnSurface(mouth.transform, 0f, mouthY, sp.eyeSize, -0.005f);
            Arc(mouth, 205f, 335f, 0.38f, 0.22f, new Vector3(0, 0.17f, 0.03f), 0.068f, 13);

            // Yanak pembelikleri
            foreach (float sx in new[] { -1f, 1f })
            {
                var cheek = new GameObject(sx < 0 ? "BlushL" : "BlushR");
                cheek.transform.SetParent(face.transform, false);
                OnSurface(cheek.transform, sx * (sp.eyeSep + sp.eyeSize * 0.6f), sp.eyeY - sp.eyeSize * 0.5f, sp.eyeSize, -0.01f);
                Piece(cheek, "Blush", Ell(0.34f, 0.2f, 0.08f), m.blush, Vector3.zero);
            }

            // ---------- İç mekân ----------
            var interior = new GameObject("Interior");
            interior.transform.SetParent(bodyRoot.transform, false);
            float cabMid = (sp.cabinZ0 + sp.cabinZ1) / 2f, floorY = sp.topRear - 0.04f;
            var inner = new List<(Mesh, Matrix4x4)>();
            foreach (float sx in new[] { -1f, 1f })
            {
                inner.Add((Ellipsoid(new Vector3(0.15f, 0.06f, 0.15f)), At(new Vector3(sx * 0.24f, floorY + 0.04f, cabMid - 0.08f))));
                inner.Add((Ellipsoid(new Vector3(0.15f, 0.17f, 0.05f)), At(new Vector3(sx * 0.24f, floorY + 0.19f, cabMid - 0.24f))));
            }
            MeshChild(dir, interior, sp.name, "Seats", Combine(sp.name + "_Seats", inner), m.seat, null);
            var dash = new List<(Mesh, Matrix4x4)> { (Ellipsoid(new Vector3(0.44f, 0.07f, 0.1f)), At(new Vector3(0, floorY + 0.12f, sp.cabinZ1 - 0.2f))) };
            MeshChild(dir, interior, sp.name, "Dashboard", Combine(sp.name + "_Dash", dash), m.dark, null);
            int zoneLayer = LayerMask.NameToLayer("CarZone");

            // Direksiyon: ayrı parça; dokununca korna çalar ve sağa sola döner
            var wheelRing = Revolve("Steer", Circle(new Vector2(0, 0.075f), 0.016f, 10), true, 32);
            var steer = MeshChild(dir, interior, sp.name, "SteeringWheel", wheelRing, m.dark, null);
            steer.transform.localPosition = new Vector3(-0.24f, floorY + 0.24f, sp.cabinZ1 - 0.34f);
            steer.transform.localRotation = Quaternion.Euler(0, 90, 0) * Quaternion.Euler(0, 0, -25);
            MeshChild(dir, steer, sp.name, "SteeringHub", Ellipsoid(new Vector3(0.014f, 0.034f, 0.034f), 10, 14), m.pink, null);
            steer.layer = zoneLayer;
            var steerCol = steer.AddComponent<SphereCollider>(); steerCol.isTrigger = true; steerCol.radius = 0.11f;
            steer.AddComponent<InteriorTapTarget>().kind = InteriorKind.Steering;

            // Gösterge düğmeleri: kırmızı, sarı, mavi; dokununca nota çalar ve rengini söyler
            var btnMesh = Ellipsoid(new Vector3(0.036f, 0.022f, 0.036f), 10, 14);
            AssetDatabase.CreateAsset(btnMesh, $"{dir}/Meshes/{sp.name}_DashButton.asset");
            var dashBtns = new[] { (m.red, "color_red", 523f), (m.yellow, "color_yellow", 659f), (m.blue, "color_blue", 784f) };
            for (int bi = 0; bi < dashBtns.Length; bi++)
            {
                float bx = 0.06f + bi * 0.1f, bz = -0.04f;
                float by = floorY + 0.12f + 0.07f * Mathf.Sqrt(Mathf.Max(0f, 1f - (bx / 0.44f) * (bx / 0.44f) - (bz / 0.1f) * (bz / 0.1f)));
                var b = Empty("DashButton_" + bi, interior.transform, new Vector3(bx, by, sp.cabinZ1 - 0.2f + bz));
                b.AddComponent<MeshFilter>().sharedMesh = btnMesh;
                b.AddComponent<MeshRenderer>().sharedMaterial = dashBtns[bi].Item1;
                b.layer = zoneLayer;
                var bc = b.AddComponent<SphereCollider>(); bc.isTrigger = true; bc.radius = 0.05f;
                var bt = b.AddComponent<InteriorTapTarget>();
                bt.kind = InteriorKind.DashButton; bt.wordKey = dashBtns[bi].Item2; bt.noteHz = dashBtns[bi].Item3;
            }

            // Sürücü gözü: biraz geride ve ortaya yakın, hafif sağa dönük; direksiyon, düğmeler ve yandaki arkadaş birlikte görünür
            var driver = Empty("DriverView", interior.transform, new Vector3(-0.12f, floorY + 0.36f, cabMid - 0.25f));
            driver.transform.localRotation = Quaternion.Euler(0f, 15f, 0f);
            var passenger = Empty("PassengerSeat", interior.transform, new Vector3(0.24f, floorY + 0.06f, cabMid + 0.02f));
            passenger.layer = zoneLayer;
            var pc = passenger.AddComponent<SphereCollider>(); pc.isTrigger = true; pc.center = new Vector3(0f, 0.25f, 0f); pc.radius = 0.17f;
            passenger.AddComponent<InteriorTapTarget>().kind = InteriorKind.Buddy;
            var toyA = Empty("ToyAnchor", interior.transform, new Vector3(0, sp.cabinTopFront - 0.12f, sp.cabinZ1 - 0.28f));
            toyA.layer = zoneLayer;
            var tc = toyA.AddComponent<SphereCollider>(); tc.isTrigger = true; tc.center = new Vector3(0f, -0.12f, 0f); tc.radius = 0.08f;
            toyA.AddComponent<InteriorTapTarget>().kind = InteriorKind.Toy;

            // ---------- Bağlantı noktaları ----------
            float frontZ = FrontZ(secs, 0.2f, 0f, sp.bodyExp), rearZ = -FrontZ(Mirror(secs), 0.45f, 0f, sp.bodyExp);
            // Ön yuva yüzün ortasında: şekil çerçeveleri yüzü çevreler
            float faceMidY = sp.eyeY - sp.eyeSize * 0.22f;
            Socket(bodyRoot, "Socket_Front", PartSlot.Front, new Vector3(0, faceMidY, FrontZ(secs, faceMidY, 0f, sp.bodyExp)));
            Socket(bodyRoot, "Socket_Top", PartSlot.Top, new Vector3(0, (sp.cabinTop + sp.cabinTopFront) * 0.5f + 0.04f, cabMid - 0.05f));
            Socket(bodyRoot, "Socket_Back", PartSlot.Back, new Vector3(0, sp.topRear - 0.05f, rearZ + 0.1f));
            Socket(bodyRoot, "Socket_Side", PartSlot.Side, new Vector3(0, sp.topRear * 0.72f, -0.05f));
            Socket(bodyRoot, "Socket_Under", PartSlot.Under, new Vector3(0, 0.02f, 0));
            Socket(bodyRoot, "Socket_Exhaust", PartSlot.Exhaust, new Vector3(0.32f, 0.14f, rearZ + 0.02f));

            // ---------- Dokunma bölgeleri ----------
            var zones = Empty("Zones", bodyRoot.transform, Vector3.zero);
            float cabLow = sp.topRear - 0.02f;
            Zone(zones.transform, "Zone_Top", PartSlot.Top, new Vector3(0, (cabLow + sp.cabinTop) / 2f, cabMid), new Vector3(sp.cabinHalfW * 2.1f, sp.cabinTop - cabLow + 0.1f, sp.cabinZ1 - sp.cabinZ0 + 0.1f));
            Zone(zones.transform, "Zone_Front", PartSlot.Front, new Vector3(0, sp.topFront / 2f, frontZ - 0.25f), new Vector3(sp.width + 0.05f, sp.topFront + 0.05f, 0.55f));
            Zone(zones.transform, "Zone_Back", PartSlot.Back, new Vector3(0, sp.topRear / 2f, rearZ + 0.25f), new Vector3(sp.width + 0.05f, sp.topRear + 0.05f, 0.55f));
            Zone(zones.transform, "Zone_Side", PartSlot.Side, new Vector3(0, sp.topRear / 2f, cabMid), new Vector3(sp.width + 0.1f, sp.topRear, sp.length * 0.45f));

            // ---------- Tekerlek yuvaları (gövdenin dışında: yükseklik ayarından etkilenmez) ----------
            var anchors = new List<Transform>();
            foreach (var p in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            {
                var a = Empty((p.y > 0 ? "Wheel_F" : "Wheel_R") + (p.x < 0 ? "L" : "R"), root.transform, new Vector3(p.x * sp.wheelX, sp.wheelR, p.y * sp.wheelZ));
                anchors.Add(a.transform);
                Zone(a.transform, "Zone_Wheel", PartSlot.Wheels, Vector3.zero, new Vector3(0.4f, sp.wheelR * 2.1f, sp.wheelR * 2.1f));
            }

            // ---------- Süspansiyon: yaylı amortisörler + akslar ----------
            var rodMesh = Revolve(sp.name + "_StrutRod", new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 0.034f), new Vector2(1f, 0.034f), new Vector2(1f, 0f) }, false, 16, true);
            var coils = new List<(Mesh, Matrix4x4)>();
            foreach (float h in new[] { 0.18f, 0.38f, 0.58f, 0.78f })
                coils.Add((Revolve("Coil", Circle(new Vector2(h, 0.072f), 0.022f, 10), true, 24, true), Matrix4x4.identity));
            var coilMesh = Combine(sp.name + "_StrutCoil", coils);
            AssetDatabase.CreateAsset(rodMesh, $"{dir}/Meshes/{sp.name}_StrutRod.asset");
            AssetDatabase.CreateAsset(coilMesh, $"{dir}/Meshes/{sp.name}_StrutCoil.asset");
            var struts = new List<Transform>();
            foreach (var wa in anchors)
            {
                var st = Empty("Strut", wa, new Vector3(-Mathf.Sign(wa.localPosition.x) * 0.17f, 0f, 0f));
                var rod = Empty("Rod", st.transform, Vector3.zero);
                rod.AddComponent<MeshFilter>().sharedMesh = rodMesh; rod.AddComponent<MeshRenderer>().sharedMaterial = m.chrome;
                var coil = Empty("Coil", st.transform, Vector3.zero);
                coil.AddComponent<MeshFilter>().sharedMesh = coilMesh; coil.AddComponent<MeshRenderer>().sharedMaterial = m.spring;
                coil.AddComponent<TintTarget>().channel = TintChannel.Rim;
                struts.Add(st.transform);
            }
            var axleMesh = Revolve(sp.name + "_Axle", new List<Vector2> { new Vector2(-sp.wheelX, 0f), new Vector2(-sp.wheelX, 0.032f), new Vector2(sp.wheelX, 0.032f), new Vector2(sp.wheelX, 0f) }, false, 16);
            AssetDatabase.CreateAsset(axleMesh, $"{dir}/Meshes/{sp.name}_Axle.asset");
            var axles = new List<Transform>();
            foreach (float az in new[] { sp.wheelZ, -sp.wheelZ })
            {
                var ax = Empty("Axle", root.transform, new Vector3(0f, sp.wheelR, az));
                ax.AddComponent<MeshFilter>().sharedMesh = axleMesh; ax.AddComponent<MeshRenderer>().sharedMaterial = m.chrome;
                axles.Add(ax.transform);
            }

            ch.bodyRoot = bodyRoot.transform; ch.bodyRenderer = bodyRend; ch.wheelAnchors = anchors.ToArray(); ch.wheelRadius = sp.wheelR;
            ch.struts = struts.ToArray(); ch.axles = axles.ToArray();
            ch.faceAnchor = face.transform; ch.driverView = driver.transform; ch.passengerSeat = passenger.transform; ch.toyAnchor = toyA.transform;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/Prefabs/{sp.name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ---------------- yardımcılar ----------------

        /// <summary>Verilen yükseklik ve yan uzaklıkta gövdenin ön yüzeyinin Z konumu.</summary>
        static float FrontZ(List<Section> secs, float y, float x, float exp)
        {
            // Önden arkaya: (x, y) noktasını içine alan ilk kesit = yüzeyin o noktadaki Z'si
            for (int i = secs.Count - 1; i >= 0; i--)
            {
                var s = secs[i];
                if (s.halfH <= 1e-4f || s.halfW <= 1e-4f) continue;
                float u = Mathf.Abs(x) / s.halfW, v = Mathf.Abs(y - s.center) / s.halfH;
                if (u > 1f || v > 1f) continue;
                if (Mathf.Pow(u, exp) + Mathf.Pow(v, exp) <= 1f)
                {
                    if (i == secs.Count - 1) return s.z;
                    // İki kesit arasında yumuşak geçiş
                    var p = secs[i + 1];
                    float up = p.halfW > 1e-4f ? Mathf.Abs(x) / p.halfW : 9f, vp = p.halfH > 1e-4f ? Mathf.Abs(y - p.center) / p.halfH : 9f;
                    float fIn = Mathf.Pow(u, exp) + Mathf.Pow(v, exp), fOut = Mathf.Pow(Mathf.Min(up, 3f), exp) + Mathf.Pow(Mathf.Min(vp, 3f), exp);
                    float t = Mathf.Clamp01((1f - fIn) / Mathf.Max(1e-4f, fOut - fIn));
                    return Mathf.Lerp(s.z, p.z, t);
                }
            }
            return secs[secs.Count / 2].z;
        }

        static Vector3 SurfaceNormal(List<Section> secs, float x, float y, float exp)
        {
            const float d = 0.04f;
            float dzdx = (FrontZ(secs, y, x + d, exp) - FrontZ(secs, y, x - d, exp)) / (2f * d);
            float dzdy = (FrontZ(secs, y + d, x, exp) - FrontZ(secs, y - d, x, exp)) / (2f * d);
            var n = new Vector3(-dzdx, -dzdy, 1f).normalized;
            // Aşırı eğimi sınırla (görsel okunaklı kalsın)
            return Vector3.Slerp(Vector3.forward, n, 0.8f).normalized;
        }

        static List<Section> Mirror(List<Section> secs)
        {
            var l = new List<Section>();
            // Ters sırada ve z'si ters: listenin sonu artık aracın arkası olur
            for (int i = secs.Count - 1; i >= 0; i--) { var s = secs[i]; s.z = -s.z; l.Add(s); }
            return l;
        }

        static GameObject MeshChild(string dir, GameObject parent, string car, string name, Mesh mesh, Material mat, TintChannel? tint)
        {
            AssetDatabase.CreateAsset(mesh, $"{dir}/Meshes/{car}_{name}.asset");
            var g = new GameObject(name);
            g.transform.SetParent(parent.transform, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (tint.HasValue) g.AddComponent<TintTarget>().channel = tint.Value;
            return g;
        }

        static GameObject Empty(string name, Transform parent, Vector3 pos)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            return g;
        }

        static void Socket(GameObject body, string name, PartSlot slot, Vector3 pos) =>
            Empty(name, body.transform, pos).AddComponent<CarSocket>().slot = slot;

        static void Zone(Transform parent, string name, PartSlot slot, Vector3 pos, Vector3 size)
        {
            var z = Empty(name, parent, pos);
            z.layer = LayerMask.NameToLayer("CarZone");
            var bc = z.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
            z.AddComponent<TapZone>().slot = slot;
        }
    }
}
