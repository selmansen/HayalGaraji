using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Menü: Hayal Garajı → Test Sahnesini Kur
    /// Basit şekillerden bir test arabası, test parçaları, renk paleti, göz ve çıkartma görselleri,
    /// yer tutucu ikonlar ve TÜM bağlantıları yapılmış Main sahnesini tek tıkla kurar.
    /// Kalıcı olan: sahne düzeni, arayüz, sistem bağlantıları.
    /// Sonradan değişecek olan: Generated klasöründeki test modelleri ve yer tutucu ikonlar.
    /// </summary>
    public static class TestSceneBuilder
    {
        const string Gen = "Assets/_Project/Generated";

        /// <summary>Çıkartmalar: Fluent emoji dosyası (Art/Icons/&lt;key&gt;.png), kelime, hareketli mi.</summary>
        static readonly (string id, string key, string word, bool animated)[] StickerList =
        {
            ("star", "sticker_star", "shape_star", true), ("heart", "sticker_heart", "shape_heart", false),
            ("rainbow", "sticker_rainbow", "shape_rainbow", false), ("unicorn", "sticker_unicorn", "animal_unicorn", false),
            ("dino", "sticker_dino", "animal_dinosaur", false), ("cat", "sticker_cat", "animal_cat", false),
            ("rocket", "sticker_rocket", "part_rocket", false), ("blossom", "sticker_blossom", "shape_flower", false),
            ("donut", "sticker_donut", "food_donut", false), ("strawberry", "sticker_strawberry", "food_strawberry", false),
            ("butterfly", "sticker_butterfly", "animal_butterfly", true), ("zap", "sticker_zap", "shape_lightning", false),
            ("panda", "sticker_panda", "animal_panda", false), ("ghost", "sticker_ghost", "shape_ghost", false),
            ("icecream", "sticker_icecream", "food_icecream", false), ("crown", "sticker_crown", "part_crown", false),
        };
        const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        static Sprite uiSprite;
        static readonly Color Ink = new Color32(43, 35, 80, 255);

        [MenuItem("Hayal Garajı/Sahneyi Kur")]
        public static void Build()
        {
            if (LayerMask.NameToLayer("CarBody") < 0 || LayerMask.NameToLayer("CarZone") < 0)
            {
                EditorUtility.DisplayDialog("Hayal Garajı", "Önce CarBody ve CarZone katmanlarını oluşturun (README 2.4).", "Tamam");
                return;
            }
            if (Shader.Find("HayalGaraji/Toon") == null)
            {
                EditorUtility.DisplayDialog("Hayal Garajı", "HayalGaraji/Toon shader'ı bulunamadı. Console'daki shader hatalarını kontrol edin.", "Tamam");
                return;
            }
            if (!EditorUtility.DisplayDialog("Hayal Garajı",
                "Oyuncak stüdyosu ve Pofu kurulacak. 'Generated' klasörü ve 'Main' sahnesi varsa baştan oluşturulur.\n\nDevam edilsin mi?", "Kur", "Vazgeç"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            failures = 0;

            try
            {
                EditorUtility.DisplayProgressBar("Hayal Garajı", "Klasörler hazırlanıyor…", 0.05f);
                // Sahne EN BAŞTA açılır: sonradan açılırsa Unity yeni üretilen dosyaları bellekten boşaltabilir
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                PrepareFolders();
                uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

                EditorUtility.DisplayProgressBar("Hayal Garajı", "Görseller çiziliyor…", 0.15f);
                var art = MakeArt();
                bubbleSprite = art.bubble;

                EditorUtility.DisplayProgressBar("Hayal Garajı", "Malzemeler ve parçalar…", 0.35f);
                var catalog = MakeContent(art);

                EditorUtility.DisplayProgressBar("Hayal Garajı", "Sahne kuruluyor…", 0.6f);
                MakeScene(scene, catalog, art);

                EditorUtility.DisplayProgressBar("Hayal Garajı", "Parça ikonları üretiliyor…", 0.85f);
                try { ThumbnailBaker.BakeAll(); EditorSceneManager.SaveOpenScenes(); }
                catch (System.Exception ex) { Debug.LogWarning("İkon üretimi atlandı: " + ex.Message); }

                AssetDatabase.SaveAssets();
                if (failures > 0)
                {
                    EditorUtility.DisplayDialog("Hayal Garajı", $"Sahne kuruldu ama {failures} bağlantı yapılamadı.\nConsole'daki kırmızı satırları gönderin.", "Tamam");
                    return;
                }
                Debug.Log("Hayal Garajı: Test sahnesi hazır. Play'e basın!");
                EditorUtility.DisplayDialog("Hayal Garajı", "Test sahnesi hazır!\n\nÜstteki ▶ Play düğmesine basın.", "Harika");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        // ================= klasörler =================

        static void PrepareFolders()
        {
            if (AssetDatabase.IsValidFolder(Gen)) AssetDatabase.DeleteAsset(Gen);
            foreach (var f in new[] { Gen, Gen + "/Materials", Gen + "/Prefabs", Gen + "/Data", Gen + "/Sprites", Gen + "/Meshes", "Assets/_Project/Scenes" })
                Directory.CreateDirectory(f);
            AssetDatabase.Refresh();
        }

        // ================= görseller =================

        class Art
        {
            public Sprite bubble, panel, fade, fxBubble, fxNote, fxSquare;
            public Sprite swatch, ring, eyeWhite, iris, highlight, happy, sleepy, star, heart, dot, softDot, mouth, blush, petal, shadow;
            public readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        }

        delegate Color Painter(float u, float v);

        static Art MakeArt()
        {
            var a = new Art();
            Color W = Color.white, C0 = new Color(0, 0, 0, 0);
            // Balon düğme: üstte parlak, altta hafif gölgeli beyaz daire (renklendirilebilir)
            a.bubble = MakeSprite("ui_bubble", 192, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 0.96f) return C0;
                float shade = Mathf.Lerp(0.86f, 1f, Mathf.Clamp01((v + 0.9f) / 1.3f));
                if (d > 0.86f && v < 0) shade *= 0.9f;                       // alt kenar gölgesi
                bool hi = (u + 0.28f) * (u + 0.28f) / 0.16f + (v - 0.5f) * (v - 0.5f) / 0.04f < 1f; // üst parlaklık
                var col = new Color(shade, shade, shade, 1f);
                return hi ? Color.Lerp(col, Color.white, 0.8f) : col;
            });
            // Panel: köşeleri çok yuvarlak, 9 parçalı esneyen zemin
            a.panel = MakeSprite("ui_panel", 128, (u, v) =>
            {
                float r = 0.75f, x = Mathf.Max(Mathf.Abs(u) - (1f - r), 0f), y = Mathf.Max(Mathf.Abs(v) - (1f - r), 0f);
                return Mathf.Sqrt(x * x + y * y) <= r * 0.98f ? W : C0;
            }, new Vector4(52, 52, 52, 52));
            a.swatch = MakeSprite("swatch", 128, (u, v) => Circle(u, v, 0, 0, 0.92f) ? W : C0);
            a.ring = MakeSprite("ring", 128, (u, v) => { float d = Mathf.Sqrt(u * u + v * v); return d < 0.97f && d > 0.8f ? W : C0; });
            a.eyeWhite = MakeSprite("eye_white", 128, (u, v) =>
            {
                float e = (u / 0.8f) * (u / 0.8f) + (v / 0.95f) * (v / 0.95f);
                return e <= 0.8f ? W : e <= 1f ? Ink : C0;
            });
            a.iris = MakeSprite("eye_iris", 128, (u, v) =>
                Circle(u, v, 0, -0.08f, 0.27f) ? new Color(0.11f, 0.09f, 0.2f) : Circle(u, v, 0, -0.05f, 0.56f) ? W : C0);
            a.highlight = MakeSprite("eye_highlight", 128, (u, v) =>
                Circle(u, v, -0.22f, 0.28f, 0.16f) || Circle(u, v, 0.22f, -0.25f, 0.07f) ? W : C0);
            a.happy = MakeSprite("eye_happy", 128, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + (v + 0.25f) * (v + 0.25f));
                return v > -0.25f && d > 0.42f && d < 0.6f ? Ink : C0;
            });
            a.sleepy = MakeSprite("eye_sleepy", 128, (u, v) => Mathf.Abs(v) < 0.07f && Mathf.Abs(u) < 0.6f ? Ink : C0);
            a.star = MakeSprite("shape_star", 128, (u, v) => InStar(u, v, 0.95f, 0.42f) ? W : C0);
            a.heart = MakeSprite("shape_heart", 128, (u, v) => InHeart(u, v) ? W : C0);
            a.dot = MakeSprite("shape_dot", 64, (u, v) => Circle(u, v, 0, 0, 0.9f) ? W : C0);
            a.softDot = MakeSprite("soft_dot", 64, (u, v) => { float d = Mathf.Sqrt(u * u + v * v); return new Color(1, 1, 1, Mathf.Clamp01(1 - d)); });
            // Açık ağızlı anime gülümseme + dil
            a.mouth = MakeSprite("face_mouth", 128, (u, v) =>
            {
                bool inMouth = v < 0.25f && (u / 0.62f) * (u / 0.62f) + ((v - 0.25f) / 0.62f) * ((v - 0.25f) / 0.62f) <= 1f;
                bool tongue = (u / 0.3f) * (u / 0.3f) + ((v + 0.2f) / 0.15f) * ((v + 0.2f) / 0.15f) <= 1f;
                return inMouth ? (tongue ? (Color)new Color32(255, 120, 150, 255) : Ink) : C0;
            });
            a.blush = MakeSprite("face_blush", 64, (u, v) =>
            {
                float e = (u / 0.95f) * (u / 0.95f) + (v / 0.55f) * (v / 0.55f);
                return new Color(1f, 0.55f, 0.72f, Mathf.Clamp01((1f - e) * 1.6f));
            });
            a.petal = MakeSprite("petal", 64, (u, v) =>
            {
                float e = (u / 0.55f) * (u / 0.55f) + (v / 0.92f) * (v / 0.92f);
                bool notch = v > 0.7f && Mathf.Abs(u) < (v - 0.7f) * 0.6f;
                return e <= 1f && !notch ? (Color)new Color32(255, 196, 222, 255) : C0;
            });
            a.shadow = MakeSprite("shadow", 64, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                return new Color(0.16f, 0.12f, 0.3f, Mathf.Pow(Mathf.Clamp01(1f - d), 1.4f) * 0.5f);
            });

            // Yer tutucu ikonlar: renkli daire + beyaz şekil. Gerçek ikon setiyle değiştirilecek.
            void Icon(string name, Color bg, System.Func<float, float, bool> shape) =>
                a.icons[name] = MakeSprite("icon_" + name, 128, (u, v) => !Circle(u, v, 0, 0, 0.95f) ? C0 : shape(u, v) ? W : bg);
            Color pink = new Color32(255, 95, 162, 255), orange = new Color32(255, 157, 46, 255), yellow = new Color32(255, 196, 40, 255),
                  gray = new Color32(120, 124, 150, 255), blue = new Color32(74, 168, 255, 255), green = new Color32(80, 190, 110, 255),
                  purple = new Color32(150, 100, 255, 255), teal = new Color32(40, 190, 170, 255), red = new Color32(240, 70, 90, 255);
            System.Func<float, float, bool> circle = (u, v) => Circle(u, v, 0, 0, 0.42f);
            System.Func<float, float, bool> ringS = (u, v) => { float d = Mathf.Sqrt(u * u + v * v); return d < 0.5f && d > 0.3f; };
            System.Func<float, float, bool> star = (u, v) => InStar(u, v, 0.6f, 0.26f);
            System.Func<float, float, bool> heart = (u, v) => InHeart(u / 0.6f, v / 0.6f);
            System.Func<float, float, bool> square = (u, v) => Mathf.Abs(u) < 0.36f && Mathf.Abs(v) < 0.36f;
            System.Func<float, float, bool> play = (u, v) => u > -0.3f && Mathf.Abs(v) < (0.4f - u) * 0.6f && u < 0.45f;
            System.Func<float, float, bool> up = (u, v) => v > -0.3f && Mathf.Abs(u) < (0.4f - v) * 0.8f && v < 0.4f;
            System.Func<float, float, bool> down = (u, v) => up(u, -v);
            System.Func<float, float, bool> dots = (u, v) => Circle(u, v, -0.25f, 0.05f, 0.14f) || Circle(u, v, 0.25f, 0.05f, 0.14f);
            System.Func<float, float, bool> cross = (u, v) => (Mathf.Abs(u - v) < 0.12f || Mathf.Abs(u + v) < 0.12f) && Mathf.Abs(u) < 0.4f;
            System.Func<float, float, bool> small = (u, v) => Circle(u, v, 0, 0, 0.2f);
            System.Func<float, float, bool> big = (u, v) => Circle(u, v, 0, 0, 0.55f);

            Icon("cat_paint", pink, circle); Icon("cat_brush", orange, heart); Icon("cat_stickers", yellow, star);
            Icon("cat_wheels", gray, ringS); Icon("cat_face", blue, dots); Icon("cat_buddy", green, heart); Icon("cat_sound", purple, play);
            Icon("surprise", red, star); Icon("photo", teal, square); Icon("vroom", purple, play);
            Icon("cat_exhaust", gray, circle); Icon("engine_key", yellow, play);
            Icon("none", gray, cross); Icon("target_body", pink, square); Icon("target_roof", pink, up); Icon("target_rim", pink, ringS);
            Icon("finish_solid", blue, circle); Icon("finish_rainbow", orange, ringS); Icon("finish_twotone", teal, square); Icon("finish_neon", yellow, star);
            Icon("small", gray, small); Icon("big", gray, big); Icon("sponge", blue, dots); Icon("glitter", yellow, star); Icon("rainbow", orange, ringS);
            Icon("low", green, down); Icon("high", green, up); Icon("hop", green, heart); Icon("inside", teal, square); Icon("horn", purple, play);
            Icon("night", new Color32(90, 80, 200, 255), (u, v) => Circle(u, v, 0, 0, 0.42f) && !Circle(u, v, 0.18f, 0.12f, 0.36f));
            Icon("day", yellow, circle); Icon("brush_thin", orange, small); Icon("brush_thick", orange, big);
            Icon("medium", gray, circle); Icon("height_normal", green, circle); Icon("giant", green, up); Icon("outside", teal, square);
            Color accentC = new Color32(56, 200, 230, 255);
            System.Func<float, float, bool> plusS = (u, v) => (Mathf.Abs(u) < 0.1f && Mathf.Abs(v) < 0.4f) || (Mathf.Abs(v) < 0.1f && Mathf.Abs(u) < 0.4f);
            System.Func<float, float, bool> minusS = (u, v) => Mathf.Abs(v) < 0.1f && Mathf.Abs(u) < 0.4f;
            Icon("cat_accessories", pink, heart); Icon("album", teal, square);
            Icon("arrow_up", accentC, up); Icon("arrow_down", accentC, down); Icon("plus", accentC, plusS); Icon("minus", accentC, minusS);
            Icon("face_normal", blue, dots); Icon("face_star", yellow, star); Icon("face_heart", pink, heart);
            Icon("part_horn_cow", purple, play); Icon("part_horn_duck", yellow, play); Icon("horn_button", purple, play);
            int si = 0;
            foreach (var st in StickerList)
            {
                var shapes = new[] { star, heart, circle };
                var cols = new[] { yellow, pink, blue, green, orange, purple };
                Icon(st.key, cols[si % cols.Length], shapes[si % shapes.Length]);
                si++;
            }
            // Egzoz efektleri: baloncuk (halka + parıltı), nota, konfeti karesi
            a.fxBubble = MakeSprite("fx_bubble", 64, (u, v) =>
            {
                float d = Mathf.Sqrt(u * u + v * v);
                if (d > 0.95f) return C0;
                if (Circle(u, v, -0.32f, 0.35f, 0.16f)) return W;
                float rim = Mathf.Clamp01((d - 0.7f) / 0.25f);
                return new Color(0.85f, 0.97f, 1f, 0.15f + rim * 0.75f);
            });
            a.fxNote = MakeSprite("fx_note", 64, (u, v) =>
            {
                bool head = ((u + 0.2f) / 0.34f) * ((u + 0.2f) / 0.34f) + ((v + 0.5f) / 0.24f) * ((v + 0.5f) / 0.24f) <= 1f;
                bool stem = u > 0.08f && u < 0.2f && v > -0.5f && v < 0.8f;
                bool flag = u > 0.08f && v > 0.35f && v < 0.8f && u < 0.2f + (0.8f - v) * 0.9f && u < 0.6f;
                return head || stem || flag ? W : C0;
            });
            a.fxSquare = MakeSprite("fx_square", 32, (u, v) => Mathf.Abs(u) < 0.8f && Mathf.Abs(v) < 0.5f ? W : C0);
            // Tepsi kenarı solması (soldan sağa saydamlaşır)
            a.fade = MakeSprite("ui_fade", 64, (u, v) => new Color(1f, 1f, 1f, Mathf.Clamp01((0.9f - u) / 1.8f)));

            // Gerçek ikonlar (Fluent Emoji 3D) varsa yer tutucuların yerine geçer: Assets/_Project/Art/Icons/<anahtar>.png
            const string IconDir = "Assets/_Project/Art/Icons";
            int real = 0;
            foreach (var key in new List<string>(a.icons.Keys))
            {
                string path = $"{IconDir}/{key}.png";
                if (!File.Exists(path)) continue;
                AssetDatabase.ImportAsset(path);
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                // Net ikonlar: mipmap + üç doğrusal filtre (küçültmede kırçıllanmaz), sıkıştırma yok
                if (imp.textureType != TextureImporterType.Sprite || !imp.mipmapEnabled || imp.filterMode != FilterMode.Trilinear
                    || imp.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.alphaIsTransparency = true;
                    imp.mipmapEnabled = true;
                    imp.filterMode = FilterMode.Trilinear;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
                var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sp) { a.icons[key] = sp; real++; }
            }
            Debug.Log(real > 0 ? $"Hayal Garajı: {real} gerçek ikon kullanıldı." : "Hayal Garajı: Gerçek ikon bulunamadı, yer tutucular kullanılıyor.");
            return a;
        }

        static bool Circle(float u, float v, float cx, float cy, float r) => (u - cx) * (u - cx) + (v - cy) * (v - cy) <= r * r;

        static bool InStar(float u, float v, float ro, float ri)
        {
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2 + i * Mathf.PI / 5; float r = i % 2 == 0 ? ro : ri; pts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r); }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > v) != (pts[j].y > v) && u < (pts[j].x - pts[i].x) * (v - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) inside = !inside;
            return inside;
        }

        static bool InHeart(float u, float v)
        {
            float x = u * 1.3f, y = v * 1.3f + 0.15f;
            float a = x * x + y * y - 1f;
            return a * a * a - x * x * y * y * y <= 0f;
        }

        static Sprite MakeSprite(string name, int size, Painter paint, Vector4 border = default)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var px = new Color[size * size];
            const int ss = 3; // kenar yumuşatma için alt örnekleme
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Color acc = new Color(0, 0, 0, 0);
                    for (int sy = 0; sy < ss; sy++)
                        for (int sx = 0; sx < ss; sx++)
                        {
                            float u = (x + (sx + 0.5f) / ss) / size * 2f - 1f;
                            float v = (y + (sy + 0.5f) / ss) / size * 2f - 1f;
                            var c = paint(u, v);
                            acc += new Color(c.r * c.a, c.g * c.a, c.b * c.a, c.a);
                        }
                    acc /= ss * ss;
                    px[y * size + x] = acc.a > 0.001f ? new Color(acc.r / acc.a, acc.g / acc.a, acc.b / acc.a, acc.a) : new Color(0, 0, 0, 0);
                }
            tex.SetPixels(px);
            var path = $"{Gen}/Sprites/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.spritePixelsPerUnit = size;
            if (border != default) imp.spriteBorder = border;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================= malzeme, model, parça =================

        static Material Toon(string name, Color c)
        {
            var m = new Material(Shader.Find("HayalGaraji/Toon"));
            m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, $"{Gen}/Materials/{name}.mat");
            return m;
        }

        static Material DecalMat(string name, Sprite s, Color c)
        {
            var m = new Material(Shader.Find("HayalGaraji/Decal"));
            m.SetTexture("_MainTex", s.texture);
            m.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(m, $"{Gen}/Materials/{name}.mat");
            return m;
        }

        static Material SpriteMat(string name, Sprite s)
        {
            var m = new Material(Shader.Find("Sprites/Default")) { mainTexture = s.texture };
            AssetDatabase.CreateAsset(m, $"{Gen}/Materials/{name}.mat");
            return m;
        }

        static GameObject Prim(PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, Vector3 euler = default)
        {
            var g = GameObject.CreatePrimitive(t);
            Object.DestroyImmediate(g.GetComponent<Collider>());
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            g.transform.localScale = scale;
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            return g;
        }

        static GameObject Empty(string name, Transform parent, Vector3 pos, Vector3 euler = default)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localEulerAngles = euler;
            return g;
        }

        static void Tint(GameObject g, TintChannel ch) => g.AddComponent<TintTarget>().channel = ch;

        static GameObject SavePrefab(GameObject root, string name)
        {
            var p = PrefabUtility.SaveAsPrefabAsset(root, $"{Gen}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(root);
            return p;
        }

        static void AddMesh(GameObject parent, Mesh mesh, Material mat, Vector3 pos, Quaternion rot)
        {
            var g = new GameObject(mesh.name);
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = pos; g.transform.localRotation = rot;
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static PartDefinition Part(string id, PartSlot slot, GameObject prefab, string word)
        {
            var d = ScriptableObject.CreateInstance<PartDefinition>();
            d.id = id; d.slot = slot; d.prefab = prefab; d.wordKey = word;
            AssetDatabase.CreateAsset(d, $"{Gen}/Data/Part_{id}.asset");
            return d;
        }

        /// <summary>Her yüzü UV'de ayrı bir kareye açılmış kutu: boyama her yüzde ayrı çalışır.</summary>
        static Mesh AtlasBox(Vector3 size, string name)
        {
            var h = size * 0.5f;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            for (int f = 0; f < 6; f++)
            {
                var n = normals[f];
                var up = (f == 2 || f == 3) ? Vector3.forward : Vector3.up;
                var right = Vector3.Cross(up, -n);
                Vector3 c = Vector3.Scale(n, h), u = Vector3.Scale(right, h), v = Vector3.Scale(up, h);
                int b = verts.Count;
                verts.Add(c - u - v); verts.Add(c - u + v); verts.Add(c + u + v); verts.Add(c + u - v);
                int col = f % 3, row = f / 3;
                float x0 = col / 3f + 0.01f, x1 = (col + 1) / 3f - 0.01f, y0 = row / 2f + 0.01f, y1 = (row + 1) / 2f - 0.01f;
                uvs.Add(new Vector2(x0, y0)); uvs.Add(new Vector2(x0, y1)); uvs.Add(new Vector2(x1, y1)); uvs.Add(new Vector2(x1, y0));
                tris.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
            }
            var m = new Mesh { name = name };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            AssetDatabase.CreateAsset(m, $"{Gen}/Meshes/{name}.asset");
            return m;
        }

        static ParticleSystem Particles(string name, Transform parent, Material mat, bool loop, float rate, int burst,
                                        float life, float speed, float size, float gravity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop; main.playOnAwake = false; main.startLifetime = life; main.startSpeed = speed;
            main.startSize = size; main.gravityModifier = gravity; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 18f; shape.radius = 0.03f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        static PartCatalog MakeContent(Art art)
        {
            // Malzemeler
            var body = Toon("Body", new Color32(255, 79, 123, 255));
            var roof = Toon("Roof", new Color32(43, 43, 58, 255));
            // Cam: yarı saydam (şoför arkadaş dışarıdan görünsün)
            var glass = new Material(Shader.Find("HayalGaraji/Glass"));
            AssetDatabase.CreateAsset(glass, $"{Gen}/Materials/Glass.mat");
            var seat = Toon("Seat", new Color32(255, 154, 213, 255));
            var dough = Toon("Dough", new Color32(230, 169, 94, 255));
            var icing = Toon("Icing", new Color32(255, 143, 199, 255));
            var tire = Toon("Tire", new Color32(58, 52, 82, 255)); tire.SetFloat("_GlossStrength", 0f); tire.SetFloat("_RimStrength", 0.35f);
            var rim = Toon("Rim", new Color32(255, 210, 63, 255));
            var chrome = Toon("Chrome", new Color32(223, 230, 245, 255));
            var white = Toon("White", Color.white);
            var dark = Toon("Dark", new Color32(29, 24, 52, 255));
            var pinkM = Toon("Pink", new Color32(255, 158, 196, 255));
            var red = Toon("Red", new Color32(255, 51, 85, 255));
            var blue = Toon("Blue", new Color32(61, 123, 255, 255));
            var yellow = Toon("Yellow", new Color32(255, 210, 63, 255));
            var orange = Toon("Orange", new Color32(255, 179, 92, 255));
            var smokeMat = SpriteMat("Smoke", art.softDot);
            var gold = Toon("Gold", new Color32(255, 200, 70, 255));
            var cyan = Toon("Cyan", new Color32(90, 214, 240, 255));
            var heartMat = SpriteMat("Hearts", art.heart);

            // ---------- Pofu: chibi spor araba ----------
            Material FaceMat(string n, Color c, float gloss, float outline, float emission = 0f)
            {
                var fm = Toon(n, c);
                fm.SetFloat("_GlossStrength", gloss); fm.SetFloat("_OutlinePx", outline);
                if (emission > 0f) fm.SetColor("_Emission", c * emission);
                return fm;
            }
            var vmats = new VehicleFactory.Mats { body = body, roof = roof, glass = glass, seat = seat, dark = dark, pink = pinkM, white = white,
                faceWhite = FaceMat("FaceWhite", Color.white, 0.7f, 1.8f),
                faceDark = FaceMat("FaceDark", new Color32(29, 24, 52, 255), 0.5f, 0f),
                iris = FaceMat("FaceIris", new Color32(74, 168, 255, 255), 0.6f, 0f),
                sparkle = FaceMat("FaceSparkle", Color.white, 0f, 0f, 0.9f),
                blush = FaceMat("FaceBlush", new Color32(255, 150, 190, 255), 0f, 0f, 0.15f),
                starYellow = FaceMat("FaceStar", new Color32(255, 214, 60, 255), 0.5f, 0f, 0.2f),
                heartPink = FaceMat("FaceHeart", new Color32(255, 70, 130, 255), 0.5f, 0f, 0.15f),
                chrome = chrome, spring = rim, red = red, yellow = yellow, blue = blue };
            var spec = new VehicleFactory.Spec();
            var chassisPrefab = VehicleFactory.Build(Gen, spec, vmats);

            // ---------- Parçalar ----------
            var parts = new List<PartDefinition>();
            // Tekerlekler: chibi balon lastikler ve karakterli jantlar
            var wm = new WheelFactory.Mats { tire = tire, rim = rim, white = white, dark = dark, pink = pinkM, yellow = yellow, blue = blue, dough = dough, icing = icing, chrome = chrome };
            foreach (var (id, prefab, word) in WheelFactory.BuildAll(Gen, wm, spec.wheelR, 0.32f, 0.15f))
                parts.Add(Part(id, PartSlot.Wheels, prefab, word));

            // Ön: yüzü çevreleyen şekil çerçeveleri (dokununca şeklin adını söyler)
            var frameMat = Toon("FrameTube", Color.white);
            foreach (var (id, prefab, word) in FrameFactory.BuildAll(Gen, frameMat, chrome))
                parts.Add(Part(id, PartSlot.Front, prefab, word));

            // Sağ menüdeki korna düğmesinin ikonu: oyunun kendi stilinde 3D direksiyon
            {
                var steer = new GameObject("Steer");
                var ringMesh = ChibiMesh.Revolve("SteerRing", ChibiMesh.Circle(new Vector2(0f, 0.42f), 0.07f, 14), true, 48);
                AddMesh(steer, ringMesh, Toon("SteerRing", new Color32(94, 76, 168, 255)), Vector3.zero, Quaternion.Euler(0, 90, 0));
                for (int k = 0; k < 3; k++)
                {
                    float ang = 90f + k * 120f;
                    var spoke = ChibiMesh.Ellipsoid(new Vector3(0.05f, 0.2f, 0.035f), 10, 14);
                    AddMesh(steer, spoke, chrome, Quaternion.Euler(0, 0, ang - 90f) * new Vector3(0f, 0.21f, 0f), Quaternion.Euler(0, 0, ang - 90f));
                }
                AddMesh(steer, ChibiMesh.Ellipsoid(new Vector3(0.16f, 0.16f, 0.07f), 16, 22), pinkM, new Vector3(0, 0, 0.02f), Quaternion.identity);
                AddMesh(steer, ChibiMesh.Ellipsoid(new Vector3(0.05f, 0.035f, 0.02f), 8, 12), white, new Vector3(-0.05f, 0.06f, 0.085f), Quaternion.identity);
                var icon = ThumbnailBaker.RenderIcon(steer, new Vector3(0.12f, 0.18f, 1f), $"{Gen}/Sprites/horn_button.png", Color.white, true);
                if (icon) art.icons["horn_button"] = icon;
                Object.DestroyImmediate(steer);
            }

            // Tavan
            var ears = new GameObject("Top_Ears");
            foreach (var sx in new[] { -0.35f, 0.35f })
            {
                Tint(Prim(PrimitiveType.Cube, ears.transform, new Vector3(sx, 0.1f, 0), new Vector3(0.22f, 0.22f, 0.08f), body, new Vector3(0, 0, 45)), TintChannel.Body);
                Prim(PrimitiveType.Cube, ears.transform, new Vector3(sx, 0.09f, 0.03f), new Vector3(0.12f, 0.12f, 0.05f), pinkM, new Vector3(0, 0, 45));
            }
            parts.Add(Part("top_ears", PartSlot.Top, SavePrefab(ears, "Top_Ears"), "part_ears"));

            var siren = new GameObject("Top_Siren");
            Prim(PrimitiveType.Cube, siren.transform, new Vector3(-0.13f, 0.06f, 0), new Vector3(0.24f, 0.12f, 0.16f), red);
            Prim(PrimitiveType.Cube, siren.transform, new Vector3(0.13f, 0.06f, 0), new Vector3(0.24f, 0.12f, 0.16f), blue);
            parts.Add(Part("top_siren", PartSlot.Top, SavePrefab(siren, "Top_Siren"), "part_siren"));

            // Arka
            var spoiler = new GameObject("Back_Spoiler");
            Tint(Prim(PrimitiveType.Cube, spoiler.transform, new Vector3(0, 0.25f, 0), new Vector3(1.3f, 0.05f, 0.3f), roof), TintChannel.Roof);
            foreach (var sx in new[] { -0.4f, 0.4f }) Tint(Prim(PrimitiveType.Cube, spoiler.transform, new Vector3(sx, 0.12f, 0), new Vector3(0.06f, 0.24f, 0.08f), roof), TintChannel.Roof);
            parts.Add(Part("back_spoiler", PartSlot.Back, SavePrefab(spoiler, "Back_Spoiler"), "part_spoiler"));

            var balloons = new GameObject("Back_Balloons");
            var bCols = new[] { pinkM, yellow, blue };
            for (int i = 0; i < 3; i++)
            {
                float x = (i - 1) * 0.22f, top = 0.9f + (i % 2) * 0.15f;
                Prim(PrimitiveType.Cylinder, balloons.transform, new Vector3(x, top / 2f, 0), new Vector3(0.01f, top / 2f, 0.01f), dark);
                Prim(PrimitiveType.Sphere, balloons.transform, new Vector3(x, top + 0.12f, 0), new Vector3(0.26f, 0.3f, 0.26f), bCols[i]);
            }
            parts.Add(Part("back_balloons", PartSlot.Back, SavePrefab(balloons, "Back_Balloons"), "part_balloon"));

            // Yan
            var wings = new GameObject("Side_Wings");
            foreach (var sx in new[] { -1f, 1f })
                Prim(PrimitiveType.Sphere, wings.transform, new Vector3(sx * 0.9f, 0.15f, 0), new Vector3(0.6f, 0.06f, 0.35f), white, new Vector3(0, 0, sx * 25f));
            parts.Add(Part("side_wings", PartSlot.Side, SavePrefab(wings, "Side_Wings"), "part_wings"));

            // Egzozlar: tek, çift, dev bacalar, trompet, baloncuk, gökkuşağı, konfeti
            var exm = new ExhaustFactory.Mats { chrome = chrome, gold = gold, pink = pinkM, dark = dark, red = red, yellow = yellow, blue = blue, cyan = cyan,
                smokeFx = smokeMat, bubbleFx = SpriteMat("FxBubble", art.fxBubble), noteFx = SpriteMat("FxNote", art.fxNote),
                confettiFx = SpriteMat("FxConfetti", art.fxSquare), puffFx = SpriteMat("FxPuff", art.softDot) };
            foreach (var (id, prefab, word) in ExhaustFactory.BuildAll(Gen, exm))
                parts.Add(Part(id, PartSlot.Exhaust, prefab, word));

            // Şoför arkadaş: kedi
            // Şoför arkadaşlar: kedi, köpek, tavşan, ayı, panda, tilki, sincap, kurbağa, penguen, fare
            foreach (var (id, prefab, word) in BuddyFactory.BuildAll(Gen, (n, c) => Toon(n, c)))
                parts.Add(Part(id, PartSlot.Buddy, prefab, word));

            // Oyuncak: zar
            var dice = new GameObject("Toy_Dice");
            Prim(PrimitiveType.Cylinder, dice.transform, new Vector3(0, -0.05f, 0), new Vector3(0.006f, 0.05f, 0.006f), white);
            Prim(PrimitiveType.Cube, dice.transform, new Vector3(0, -0.13f, 0), Vector3.one * 0.06f, pinkM, new Vector3(20, 30, 10));
            parts.Add(Part("toy_dice", PartSlot.Toy, SavePrefab(dice, "Toy_Dice"), "shape_square"));

            // Korna: sadece ses (ses dosyaları gelince atanacak)
            parts.Add(Part("horn_cow", PartSlot.Horn, null, "animal_cow"));
            parts.Add(Part("horn_duck", PartSlot.Horn, null, "animal_duck"));

            // Yer tutucu ikon: gerçek ikonlar üretilene kadar
            // Sesli parçalar (korna) modelsizdir: ikonları emoji setinden (part_<id>); diğerleri sonra modelden çekilir
            foreach (var p in parts)
            {
                p.thumbnail = art.icons.TryGetValue("part_" + p.id, out var ic) ? ic : art.icons["cat_wheels"];
                EditorUtility.SetDirty(p);
            }

            // ---------- Araç tanımı ----------
            var car = ScriptableObject.CreateInstance<CarDefinition>();
            car.id = "spor"; car.chassisPrefab = chassisPrefab; car.wordKey = "vehicle_car"; car.cameraDistance = 4.7f; car.cameraTargetHeight = 0.6f;
            car.defaults = new CarConfig { carId = "spor", bodyColor = "#FF4F7B", roofColor = "#2B2B3A", rimColor = "#FFD23F" };
            car.defaults.SetPart(PartSlot.Wheels, "wheel_spokes");
            car.defaults.SetPart(PartSlot.Exhaust, "exhaust_double");
            car.defaults.SetPart(PartSlot.Buddy, "buddy_cat");
            car.defaults.SetPart(PartSlot.Toy, "toy_dice");
            car.defaults.SetPart(PartSlot.Horn, "horn_cow");
            AssetDatabase.CreateAsset(car, $"{Gen}/Data/Car_Test.asset");

            // ---------- Katalog ----------
            var cat2 = ScriptableObject.CreateInstance<PartCatalog>();
            cat2.cars.Add(car);
            cat2.parts.AddRange(parts);
            var pal = new (string hex, string key)[] {
                ("#FF4F7B","color_red"),("#FF9D2E","color_orange"),("#FFD23F","color_yellow"),("#7BDC5A","color_green"),
                ("#39C4A8","color_turquoise"),("#4AA8FF","color_blue"),("#6C7BFF","color_navy"),("#B06CFF","color_purple"),
                ("#FF7AD9","color_pink"),("#FFFFFF","color_white"),("#9AA0B5","color_gray"),("#2B2B3A","color_black"),
                ("#8B5A3C","color_brown"),("#FFB3C7","color_lightpink") };
            foreach (var (hex, key) in pal) cat2.colors.Add(new PaletteColor { color = CarConfig.Col(hex), wordKey = key });
            foreach (var st in StickerList)
                cat2.stickers.Add(new StickerDefinition { id = st.id, sprite = art.icons[st.key], wordKey = st.word, animated = st.animated });
            cat2.eyeStyles.Add(new EyeStyle { id = "normal", white = art.eyeWhite, iris = art.iris, highlight = art.highlight, happy = art.happy, sleepy = art.sleepy, wordKey = "eyes_normal", thumbnail = art.icons["face_normal"] });
            cat2.eyeStyles.Add(new EyeStyle { id = "yildiz", white = art.eyeWhite, iris = art.star, highlight = art.highlight, happy = art.happy, sleepy = art.sleepy, wordKey = "eyes_star", thumbnail = art.icons["face_star"] });
            cat2.eyeStyles.Add(new EyeStyle { id = "kalp", white = art.eyeWhite, iris = art.heart, highlight = art.highlight, happy = art.happy, sleepy = art.sleepy, wordKey = "eyes_heart", thumbnail = art.icons["face_heart"] });
            AssetDatabase.CreateAsset(cat2, $"{Gen}/Data/Catalog.asset");

            // Kalp efekti malzemesini sahnede kullanmak için sakla
            heartMatCache = heartMat;
            return cat2;
        }

        static Material heartMatCache;

        // ================= sahne =================

        static void MakeScene(UnityEngine.SceneManagement.Scene scene, PartCatalog catalog, Art art)
        {
            // Güvence: katalog dosyasını diskten yeniden yükle
            var fresh = AssetDatabase.LoadAssetAtPath<PartCatalog>($"{Gen}/Data/Catalog.asset");
            if (fresh) catalog = fresh;
            var cam = Camera.main;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(222, 214, 255, 255);
            cam.fieldOfView = 42f;
            cam.farClipPlane = 250f;
            var light = Object.FindFirstObjectByType<Light>();
            var carRoot = new GameObject("CarRoot");

            // Oyuncak stüdyosu
            var shadowMat = SpriteMat("Shadow", art.shadow);
            var stage = StudioBuilder.Build(new StudioBuilder.Setup { dir = Gen, carRoot = carRoot.transform, light = light, petal = art.petal, softDot = art.softDot, shadow = art.shadow, shadowMat = shadowMat });

            // Sistemler
            var sys = new GameObject("Systems");
            var builder = sys.AddComponent<CarBuilder>();
            var face = sys.AddComponent<CarFace>();
            var reactions = sys.AddComponent<CarReactions>();
            var engine = sys.AddComponent<EngineController>();
            var stickers = sys.AddComponent<StickerPlacer>();
            var surprise = sys.AddComponent<SurpriseBox>();
            var photo = sys.AddComponent<PhotoBooth>();
            var touch = sys.AddComponent<TouchRouter>();
            var rig = sys.AddComponent<OrbitCameraRig>();
            var vocab = sys.AddComponent<VocabularyService>();
            var gm = sys.AddComponent<GameManager>();

            var audioGo = new GameObject("Audio"); audioGo.transform.SetParent(sys.transform, false);
            var audio = audioGo.AddComponent<AudioService>();
            AudioSource Src(string n) { var g = new GameObject(n); g.transform.SetParent(audioGo.transform, false); var s = g.AddComponent<AudioSource>(); s.playOnAwake = false; return s; }

            var hearts = Particles("HeartsFx", sys.transform, heartMatCache, false, 0, 8, 1f, 2f, 0.2f, -0.3f);
            var confetti = Particles("ConfettiFx", sys.transform, SpriteMat("Confetti", art.star), false, 0, 30, 1.5f, 4f, 0.12f, 0.6f);
            confetti.transform.position = new Vector3(0, 1.5f, 0);
            confetti.transform.eulerAngles = new Vector3(-90, 0, 0);

            Set(builder, "catalog", catalog); Set(builder, "carRoot", carRoot.transform);
            var decalMat = new Material(Shader.Find("HayalGaraji/Decal"));
            AssetDatabase.CreateAsset(decalMat, $"{Gen}/Materials/Decal.mat");
            Set(face, "builder", builder);
            Set(reactions, "builder", builder); Set(reactions, "face", face); Set(reactions, "heartsFx", hearts);
            Set(engine, "builder", builder); Set(engine, "reactions", reactions);
            Set(stickers, "builder", builder); Set(stickers, "decalMaterial", decalMat);
            Set(surprise, "builder", builder); Set(surprise, "stickers", stickers); Set(surprise, "reactions", reactions); Set(surprise, "confetti", confetti);
            Set(photo, "cam", cam); Set(photo, "builder", builder); Set(photo, "reactions", reactions);
            Set(touch, "cam", cam); Set(touch, "rig", rig); Set(touch, "builder", builder); Set(touch, "reactions", reactions); Set(touch, "stickers", stickers);
            SetMask(touch, "bodyMask", LayerMask.GetMask("CarBody")); SetMask(touch, "zoneMask", LayerMask.GetMask("CarZone"));
            Set(rig, "cam", cam); Set(rig, "builder", builder);
            Set(audio, "voice", Src("Voice")); Set(audio, "sfx", Src("Sfx")); Set(audio, "loop", Src("Loop"));
            Set(gm, "builder", builder); Set(gm, "stickers", stickers);

            // ================= Arayüz =================
            // Ölçü tablosu (1920x1080 referans, yüksekliğe göre ölçeklenir): ray düğmesi 104, tepsi öğesi 116,
            // boşluklar 12/14, iç boşluk 14/20, kenar payı 24. Ana renk turkuaz, ikinci renk pembe.
            Color accent = new Color32(56, 200, 230, 255);
            Color panelTint = new Color(0.88f, 0.97f, 1f, 0.78f);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // yatay tablette düğmeler hep aynı boyda kalsın
            var canvasRt = (RectTransform)canvasGo.transform;

            var hudRt = UI("HUD", canvasGo.transform); Stretch(hudRt, 0);
            var hud = hudRt.gameObject.AddComponent<HudController>();

            // ---- Sol: kategori rayı (7 kategori) ----
            const float railBtn = 104f, railGap = 10f, railPad = 14f, edge = 24f;
            var rail = UI("CategoryRail", hudRt);
            rail.anchorMin = rail.anchorMax = new Vector2(0, 0.5f); rail.pivot = new Vector2(0, 0.5f);
            rail.anchoredPosition = new Vector2(edge, 0f);
            rail.sizeDelta = new Vector2(railBtn + 2 * railPad, 8 * railBtn + 7 * railGap + 2 * railPad); // 8 kategori: tablette sığan üst sınır
            var railPanel = Img(rail, art.panel, panelTint); railPanel.type = Image.Type.Sliced;
            var vl = rail.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset((int)railPad, (int)railPad, (int)railPad, (int)railPad);
            vl.spacing = railGap; vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = vl.childControlHeight = true; vl.childForceExpandWidth = vl.childForceExpandHeight = false;
            // Sıra = Category enum değerleri
            var cats = new (string icon, string word)[] { ("cat_paint", "cat_paint"), ("cat_stickers", "cat_stickers"), ("cat_wheels", "cat_wheels"),
                ("cat_face", "cat_face"), ("cat_accessories", "cat_accessories"), ("cat_exhaust", "cat_exhaust"), ("cat_buddy", "cat_buddy"), ("cat_sound", "cat_sound") };
            var catImages = new List<Object>();
            for (int i = 0; i < cats.Length; i++)
            {
                var b = KidButton(rail, "Cat_" + i, art.icons[cats[i].icon], Color.white, railBtn, cats[i].word);
                UnityEventTools.AddIntPersistentListener(b.onClick, new UnityAction<int>(hud.ShowCategory), i);
                catImages.Add(b.GetComponent<Image>());
            }

            // ---- Sağ: eylem düğmeleri (soldakiyle aynı panel, aynı boy, dikey ortalı) ----
            var themeButtons = new List<Image>();
            var right = UI("RightButtons", hudRt);
            right.anchorMin = right.anchorMax = new Vector2(1, 0.5f); right.pivot = new Vector2(1, 0.5f);
            right.anchoredPosition = new Vector2(-edge, 0f);
            right.sizeDelta = new Vector2(railBtn + 2 * railPad, 7 * railBtn + 6 * railGap + 2 * railPad);
            var rightPanel = Img(right, art.panel, panelTint); rightPanel.type = Image.Type.Sliced;
            var rl = right.gameObject.AddComponent<VerticalLayoutGroup>();
            rl.padding = new RectOffset((int)railPad, (int)railPad, (int)railPad, (int)railPad);
            rl.spacing = railGap; rl.childAlignment = TextAnchor.UpperCenter;
            rl.childControlWidth = rl.childControlHeight = true; rl.childForceExpandWidth = rl.childForceExpandHeight = false;
            BouncyButton RightBtn(string n, string icon, string word)
            {
                var b = KidButton(right, n, art.icons[icon], Color.white, railBtn, word);
                themeButtons.Add(b.GetComponent<Image>());
                return b;
            }
            UnityEventTools.AddPersistentListener(RightBtn("Surprise", "surprise", "ui_surprise").onClick, new UnityAction(surprise.Open));
            UnityEventTools.AddPersistentListener(RightBtn("Photo", "photo", "ui_photo").onClick, new UnityAction(photo.TakePhoto));
            var albumBtn = RightBtn("Album", "album", "ui_album");
            UnityEventTools.AddPersistentListener(RightBtn("Vroom", "engine_key", "action_drive").onClick, new UnityAction(engine.Rev));   // seçili egzozun sesi
            UnityEventTools.AddPersistentListener(RightBtn("Horn", "horn_button", null).onClick, new UnityAction(engine.Honk));       // seçili kornanın sesi
            var doorBtn = RightBtn("Door", "inside", null);                                                                            // arabanın içine gir / çık
            UnityEventTools.AddPersistentListener(doorBtn.onClick, new UnityAction(hud.ToggleInside));
            var dayNightBtn = RightBtn("DayNight", "night", null);
            UnityEventTools.AddPersistentListener(dayNightBtn.onClick, new UnityAction(stage.ToggleNight));
            stage.toggleIcon = dayNightBtn.transform.Find("Icon").GetComponent<Image>();
            stage.dayIcon = art.icons["day"]; stage.nightIcon = art.icons["night"];

            // ---- Ayar kuleleri: tepsinin hemen üstünde, ikinci satırda, yatay ve ortalı ----
            var themePanels = new List<Image> { railPanel, rightPanel };
            RectTransform Pod(Transform parent, string name, bool fit)
            {
                var p = UI(name, parent);
                var pi = Img(p, art.panel, panelTint); pi.type = Image.Type.Sliced; themePanels.Add(pi);
                var l = p.gameObject.AddComponent<HorizontalLayoutGroup>();
                l.padding = new RectOffset(14, 14, 12, 12); l.spacing = 10; l.childAlignment = TextAnchor.MiddleCenter;
                l.childControlWidth = l.childControlHeight = true; l.childForceExpandWidth = l.childForceExpandHeight = false;
                if (fit) { var f = p.gameObject.AddComponent<ContentSizeFitter>(); f.horizontalFit = f.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
                return p;
            }
            Image Dot(Transform parent)
            {
                var d = UI("Dot", parent);
                var im = Img(d, art.dot, Color.white); im.raycastTarget = false;
                var le = d.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 22;
                return im;
            }
            const float podBtn = 76f, podRowY = edge + 116f + 2 * 20f + 14f;
            var wheelPod = UI("WheelPod", hudRt);
            wheelPod.anchorMin = wheelPod.anchorMax = new Vector2(0.5f, 0); wheelPod.pivot = new Vector2(0.5f, 0);
            wheelPod.anchoredPosition = new Vector2(0, podRowY);
            var wl = wheelPod.gameObject.AddComponent<HorizontalLayoutGroup>();
            wl.spacing = 16; wl.childAlignment = TextAnchor.MiddleCenter;
            wl.childControlWidth = wl.childControlHeight = true; wl.childForceExpandWidth = wl.childForceExpandHeight = false;
            var wf = wheelPod.gameObject.AddComponent<ContentSizeFitter>(); wf.horizontalFit = wf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BouncyButton PodBtn(Transform pod, string n, string icon, string word)
            {
                var b = KidButton(pod, n, art.icons[icon], Color.white, podBtn, word);
                themeButtons.Add(b.GetComponent<Image>());
                return b;
            }
            // Yükseklik: [⬇] ●●●● [⬆]  (noktalar soldan sağa dolar)
            var hPod = Pod(wheelPod, "HeightPod", false);
            UnityEventTools.AddPersistentListener(PodBtn(hPod, "Down", "arrow_down", null).onClick, new UnityAction(hud.HeightDown));
            var heightDots = new Object[4];
            for (int lv = 0; lv < 4; lv++) heightDots[lv] = Dot(hPod);
            UnityEventTools.AddPersistentListener(PodBtn(hPod, "Up", "arrow_up", null).onClick, new UnityAction(hud.HeightUp));
            // Tekerlek boyu: [➖] ●●● [➕]
            var sPod = Pod(wheelPod, "SizePod", false);
            UnityEventTools.AddPersistentListener(PodBtn(sPod, "Minus", "minus", null).onClick, new UnityAction(hud.SizeDown));
            var sizeDots = new Object[3];
            for (int lv = 0; lv < 3; lv++) sizeDots[lv] = Dot(sPod);
            UnityEventTools.AddPersistentListener(PodBtn(sPod, "Plus", "plus", null).onClick, new UnityAction(hud.SizeUp));

            var jPod = Pod(wheelPod, "HopPod", false);
            var hopBtn = KidButton(jPod, "Hop", art.icons["hop"], Color.white, podBtn, "action_jump");
            UnityEventTools.AddPersistentListener(hopBtn.onClick, new UnityAction(hud.ToggleHop));


            // ---- Alt: seçenek tepsisi (içerik kadar genişler, ortalanır) ----
            const float item = 116f, gap = 14f, pad = 20f;
            var trayRt = UI("ItemTray", hudRt);
            trayRt.anchorMin = trayRt.anchorMax = new Vector2(0.5f, 0); trayRt.pivot = new Vector2(0.5f, 0);
            trayRt.anchoredPosition = new Vector2(0, edge); trayRt.sizeDelta = new Vector2(800, item + 2 * pad);
            var trayPanel = Img(trayRt, art.panel, panelTint); trayPanel.type = Image.Type.Sliced; themePanels.Add(trayPanel);
            var scroll = trayRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false; scroll.movementType = ScrollRect.MovementType.Elastic;
            var viewport = UI("Viewport", trayRt); Stretch(viewport, 0);
            Img(viewport, art.panel, Color.white).type = Image.Type.Sliced;       // yuvarlak maske şekli
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = UI("Content", viewport);
            content.anchorMin = new Vector2(0, 0); content.anchorMax = new Vector2(0, 1); content.pivot = new Vector2(0, 0.5f);
            content.offsetMin = Vector2.zero; content.offsetMax = Vector2.zero;
            var hl = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad); hl.spacing = gap; hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlWidth = hl.childControlHeight = true; hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport; scroll.content = content;
            GameObject Fade(string name, bool rightSide)
            {
                var f = UI(name, viewport);
                f.anchorMin = new Vector2(rightSide ? 1 : 0, 0); f.anchorMax = new Vector2(rightSide ? 1 : 0, 1);
                f.pivot = new Vector2(0.5f, 0.5f); f.sizeDelta = new Vector2(64, 0);
                f.anchoredPosition = new Vector2(rightSide ? -32 : 32, 0);
                if (rightSide) f.localScale = new Vector3(-1, 1, 1);
                var im = Img(f, art.fade, new Color(panelTint.r, panelTint.g, panelTint.b, 0.95f)); im.raycastTarget = false;
                f.gameObject.SetActive(false);
                return f.gameObject;
            }
            var fadeL = Fade("FadeLeft", false);
            var fadeR = Fade("FadeRight", true);
            var tray = trayRt.gameObject.AddComponent<ItemTray>();
            Set(tray, "panel", trayRt); Set(tray, "scroll", scroll); Set(tray, "content", content);
            Set(tray, "itemPrefab", MakeTrayItemPrefab(art, accent)); Set(tray, "swatchSprite", art.swatch);
            Set(tray, "fadeLeft", fadeL); Set(tray, "fadeRight", fadeR);

            // ---- HUD bağlantıları ----
            Set(hud, "builder", builder); Set(hud, "face", face); Set(hud, "reactions", reactions); Set(hud, "touch", touch);
            Set(hud, "stickers", stickers); Set(hud, "engine", engine); Set(hud, "rig", rig); Set(hud, "tray", tray);
            SetArray(hud, "categoryImages", catImages);
            Set(hud, "wheelPod", wheelPod.gameObject);
            SetArray(hud, "heightDots", new List<Object>(heightDots)); SetArray(hud, "sizeDots", new List<Object>(sizeDots));
            Set(hud, "hopBg", hopBtn.GetComponent<Image>());
            Set(hud, "doorIcon", doorBtn.transform.Find("Icon").GetComponent<Image>());
            Set(hud, "iconRainbow", art.icons["rainbow"]); Set(hud, "iconGlitter", art.icons["glitter"]); Set(hud, "iconSponge", art.icons["sponge"]);
            Set(hud, "iconDoorIn", art.icons["inside"]); Set(hud, "iconDoorOut", art.icons["outside"]);
            wheelPod.gameObject.SetActive(false);

            // ---- Albüm (tam ekran, en üstte) ----
            var album = canvasGo.AddComponent<AlbumView>();
            var albumRoot = UI("Album", canvasGo.transform); Stretch(albumRoot, 0);
            Img(albumRoot, null, new Color(0.03f, 0.27f, 0.36f, 0.6f));
            var albumPanel = UI("Panel", albumRoot); Stretch(albumPanel, 56);
            var albumPanelImg = Img(albumPanel, art.panel, new Color(0.94f, 0.99f, 1f, 0.97f)); albumPanelImg.type = Image.Type.Sliced; themePanels.Add(albumPanelImg);
            var hdr = UI("Header", albumPanel);
            hdr.anchorMin = hdr.anchorMax = new Vector2(0, 1); hdr.pivot = new Vector2(0, 1);
            hdr.anchoredPosition = new Vector2(40, -28); hdr.sizeDelta = new Vector2(112, 112);
            Img(hdr, art.icons["album"], Color.white).preserveAspect = true;
            var close = KidButton(albumPanel, "Close", art.icons["none"], Color.white, railBtn, null);
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(1, 1);
            crt.anchoredPosition = new Vector2(-32, -32); crt.sizeDelta = new Vector2(railBtn, railBtn);
            UnityEventTools.AddPersistentListener(close.onClick, new UnityAction(album.Close));
            themeButtons.Add(close.GetComponent<Image>());
            var gscroll = UI("Scroll", albumPanel);
            gscroll.anchorMin = Vector2.zero; gscroll.anchorMax = Vector2.one;
            gscroll.offsetMin = new Vector2(40, 40); gscroll.offsetMax = new Vector2(-40, -164);
            Img(gscroll, null, new Color(1, 1, 1, 0));
            gscroll.gameObject.AddComponent<RectMask2D>();
            var gsr = gscroll.gameObject.AddComponent<ScrollRect>(); gsr.horizontal = false; gsr.vertical = true;
            var grid = UI("Grid", gscroll);
            grid.anchorMin = new Vector2(0, 1); grid.anchorMax = new Vector2(1, 1); grid.pivot = new Vector2(0.5f, 1);
            grid.offsetMin = Vector2.zero; grid.offsetMax = Vector2.zero;
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(320, 276); gl.spacing = new Vector2(36, 36); gl.childAlignment = TextAnchor.UpperCenter;
            gl.padding = new RectOffset(12, 12, 12, 12);
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            gsr.viewport = gscroll; gsr.content = grid;
            var empty = UI("Empty", albumPanel);
            empty.anchorMin = empty.anchorMax = new Vector2(0.5f, 0.5f); empty.sizeDelta = new Vector2(300, 300);
            var emptyImg = Img(empty, art.icons["photo"], new Color(1, 1, 1, 0.55f)); emptyImg.preserveAspect = true; emptyImg.raycastTarget = false;
            var flash = UI("Flash", canvasGo.transform); Stretch(flash, 0);
            var flashImg = Img(flash, null, new Color(1, 1, 1, 0)); flashImg.raycastTarget = false;
            Set(album, "game", gm); Set(album, "root", albumRoot.gameObject); Set(album, "grid", grid);
            Set(album, "emptyHint", empty.gameObject); Set(album, "cardSprite", art.panel);
            Set(album, "albumButton", albumBtn.transform); Set(album, "flash", flashImg); Set(album, "flyLayer", canvasRt);
            UnityEventTools.AddPersistentListener(albumBtn.onClick, new UnityAction(album.Open));
            UnityEventTools.AddPersistentListener(photo.onPhotoTaken, new UnityAction<Texture2D>(album.OnPhotoTaken));
            albumRoot.gameObject.SetActive(false);

            // ---- Gündüz / gece arayüz teması ----
            var theme = canvasGo.AddComponent<UiTheme>();
            Set(theme, "stage", stage);
            SetArray(theme, "panels", themePanels.ConvertAll(x => (Object)x));
            SetArray(theme, "buttons", themeButtons.ConvertAll(x => (Object)x));
            SetArray(theme, "fades", new List<Object> { fadeL.GetComponent<Image>(), fadeR.GetComponent<Image>() });

            EditorSceneManager.SaveScene(scene, ScenePath);
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            list.RemoveAll(s => s.path == ScenePath);
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static BouncyButton MakeTrayItemPrefab(Art art, Color accent)
        {
            // Beyaz balon → seçiliyse turkuaz balon arkada + ikon biraz büyür
            var rt = UI("TrayItem", null);
            Img(rt, art.bubble, Color.white);
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 116;
            rt.gameObject.AddComponent<BouncyButton>();
            var sel = UI("SelectedBg", rt); Stretch(sel, 0);
            var si = Img(sel, art.bubble, accent); si.raycastTarget = false;
            sel.gameObject.SetActive(false);
            var icon = UI("Icon", rt); Stretch(icon, 22);
            var ii = Img(icon, null, Color.white); ii.preserveAspect = true; ii.raycastTarget = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(rt.gameObject, $"{Gen}/Prefabs/TrayItem.prefab");
            Object.DestroyImmediate(rt.gameObject);
            return prefab.GetComponent<BouncyButton>();
        }

        // ================= arayüz yardımcıları =================

        static RectTransform UI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent) go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform r, float pad)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(pad, pad); r.offsetMax = new Vector2(-pad, -pad);
        }

        static Image Img(RectTransform rt, Sprite s, Color c)
        {
            var i = rt.gameObject.AddComponent<Image>();
            i.sprite = s; i.color = c;
            if (s != null && s == uiSprite) i.type = Image.Type.Sliced;
            return i;
        }

        static Sprite bubbleSprite;

        static BouncyButton KidButton(Transform parent, string name, Sprite icon, Color bg, float size, string word)
        {
            var rt = UI(name, parent);
            Img(rt, bubbleSprite ? bubbleSprite : uiSprite, bg);
            var le = rt.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = size;
            var b = rt.gameObject.AddComponent<BouncyButton>(); b.wordKey = word;
            var ic = UI("Icon", rt); Stretch(ic, size * 0.17f);
            var im = Img(ic, icon, Color.white); im.preserveAspect = true; im.raycastTarget = false;
            return b;
        }

        // ================= bağlantı yardımcıları =================

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{target.GetType().Name}.{field} alanı bulunamadı"); return; }
            if (value == null) { Debug.LogError($"{target.GetType().Name}.{field}: atanacak değer boş."); failures++; return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            if (p.objectReferenceValue == null) { Debug.LogError($"{target.GetType().Name}.{field} bağlanamadı."); failures++; }
        }

        static int failures;

        static void SetArray(Object target, string field, List<Object> values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{target.GetType().Name}.{field} alanı bulunamadı"); failures++; return; }
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetMask(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{target.GetType().Name}.{field} alanı bulunamadı"); return; }
            p.intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
