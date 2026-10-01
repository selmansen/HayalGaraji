using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Menü: Hayal Garajı → Parça İkonlarını Üret
    /// Her parçayı açık sahnede, kendi katmanında ve kendi kamerasıyla çeker: oyundaki çizgi film
    /// görünümüyle birebir aynı, yuvarlak kırpılmış ikonlar. Aynı açı, ışık ve fon; çocuk bir kez
    /// öğrendiği ikonu hep aynı görür. RenderIcon, oyuna özel ikonlar (ör. direksiyon) için de kullanılır.
    /// </summary>
    public static class ThumbnailBaker
    {
        const string OutDir = "Assets/_Project/Art/Thumbnails";
        const int Size = 256;
        const int Layer = 31; // sadece ikon çekimi için

        [MenuItem("Hayal Garajı/Parça İkonlarını Üret")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutDir);
            int done = 0;
            foreach (var g in AssetDatabase.FindAssets("t:PartDefinition"))
            {
                var part = AssetDatabase.LoadAssetAtPath<PartDefinition>(AssetDatabase.GUIDToAssetPath(g));
                if (part == null || part.prefab == null) continue;
                // Tekerlek: jant yüzü; şoför ve ön çerçeve: önden; diğerleri: çeyrek açı
                Vector3 dir = part.slot == PartSlot.Wheels ? new Vector3(0.95f, 0.2f, 0.3f)
                            : part.slot == PartSlot.Buddy || part.slot == PartSlot.Front ? new Vector3(0.3f, 0.25f, 1f)
                            : new Vector3(0.65f, 0.5f, 0.8f);
                var sprite = RenderIcon(part.prefab, dir, $"{OutDir}/{part.id}.png", Color.white, true);
                if (sprite) { part.thumbnail = sprite; EditorUtility.SetDirty(part); done++; }
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"Hayal Garajı: {done} ikon üretildi → {OutDir}");
        }

        /// <summary>Bir nesneyi verilen yönden çekip yuvarlak (isteğe bağlı) ikon olarak kaydeder.</summary>
        public static Sprite RenderIcon(GameObject source, Vector3 viewDir, string path, Color bg, bool circleCrop)
        {
            var camGo = new GameObject("~ThumbCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.enabled = false; cam.cullingMask = 1 << Layer;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = bg; cam.fieldOfView = 24f;
            var lightGo = new GameObject("~ThumbLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 0.9f; light.cullingMask = 1 << Layer;
            light.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
            var rt = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            GameObject go = null;
            try
            {
                go = Object.Instantiate(source);
                go.transform.position = new Vector3(0f, -500f, 0f);
                foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = Layer;
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) ps.gameObject.SetActive(false);

                var b = Bounds(go);
                float radius = Mathf.Max(0.02f, b.extents.magnitude);
                float dist = radius / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
                cam.transform.position = b.center + viewDir.normalized * dist;
                cam.transform.LookAt(b.center);
                cam.nearClipPlane = Mathf.Max(0.01f, dist - radius * 2f);
                cam.farClipPlane = dist + radius * 2f;
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                RenderTexture.active = prev;
                if (circleCrop)
                {
                    var px = tex.GetPixels32();
                    float c = (Size - 1) * 0.5f, rad = Size * 0.47f;
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                            var p = px[y * Size + x];
                            p.a = (byte)(255f * Mathf.Clamp01(rad - d));
                            px[y * Size + x] = p;
                        }
                    tex.SetPixels32(px);
                }
                tex.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllBytes(path, tex.EncodeToPNG());
            }
            finally
            {
                if (go) Object.DestroyImmediate(go);
                cam.targetTexture = null;
                Object.DestroyImmediate(tex);
                rt.Release(); Object.DestroyImmediate(rt);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(lightGo);
            }
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.filterMode = FilterMode.Trilinear;
            imp.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (!sprite) Debug.LogWarning($"İkon görsel olarak yüklenemedi: {path}");
            return sprite;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
