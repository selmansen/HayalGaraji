using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Parmakla boyama. Boya, gövdenin UV'sine göre ayrı bir dokuya (_PaintTex) yazılır;
    /// shader bu dokuyu ana rengin üstüne karıştırır. Yıkama aynı dokudan boyayı siler.
    /// Not: Başlangıç için CPU tabanlı ve basit. Gerekirse sadece bu sınıf GPU'ya taşınır,
    /// dışarıdaki API aynı kalır.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class PaintableSurface : MonoBehaviour
    {
        [SerializeField] int resolution = 512;

        Texture2D tex;
        Color32[] px;
        bool dirty;
        float hue;

        public void Init(Material mat)
        {
            if (tex == null)
            {
                tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "PaintTex"
                };
                px = new Color32[resolution * resolution];
            }
            Clear();
            mat.SetTexture(ShaderIds.PaintTex, tex);
        }

        public void Clear()
        {
            System.Array.Clear(px, 0, px.Length);
            dirty = true;
        }

        /// <summary>Sprey: bir ana nokta + etrafında küçük zerrecikler.</summary>
        public void Spray(Vector2 uv, Color color, BrushStyle style)
        {
            float r = style == BrushStyle.Thin ? 0.018f : 0.03f; // çocuk parmağı için varsayılan geniş fırça
            Color c = color;
            if (style == BrushStyle.Rainbow)
            {
                hue = (hue + 0.012f) % 1f;
                c = Color.HSVToRGB(hue, 0.85f, 1f);
            }
            Dab(uv, c, r, 0.55f, 1f);
            int specks = style == BrushStyle.Glitter ? 6 : 3;
            for (int i = 0; i < specks; i++)
            {
                var off = Random.insideUnitCircle * r * 1.8f;
                var sc = style == BrushStyle.Glitter
                    ? (Random.value < 0.5f ? Color.white : new Color(1f, 0.95f, 0.6f))
                    : c;
                Dab(uv + off, sc, r * Random.Range(0.15f, 0.35f), 0.3f, 1f);
            }
        }

        /// <summary>Sünger: boyayı yavaşça siler. Silinecek bir şey kaldıysa true döner.</summary>
        public bool Wash(Vector2 uv, float radius = 0.06f, float strength = 0.35f)
        {
            int cx = (int)(uv.x * resolution), cy = (int)(uv.y * resolution);
            int r = Mathf.Max(1, (int)(radius * resolution));
            bool any = false;
            for (int y = -r; y <= r; y++)
            {
                int py = cy + y; if (py < 0 || py >= resolution) continue;
                for (int x = -r; x <= r; x++)
                {
                    int pxx = cx + x; if (pxx < 0 || pxx >= resolution) continue;
                    float d = Mathf.Sqrt(x * x + y * y) / r; if (d > 1f) continue;
                    int i = py * resolution + pxx;
                    var p = px[i]; if (p.a == 0) continue;
                    any = true;
                    p.a = (byte)Mathf.Max(0, p.a - 255f * strength * (1f - d));
                    px[i] = p;
                }
            }
            dirty |= any;
            return any;
        }

        void Dab(Vector2 uv, Color color, float radius, float hardness, float opacity)
        {
            int cx = (int)(uv.x * resolution), cy = (int)(uv.y * resolution);
            int r = Mathf.Max(1, (int)(radius * resolution));
            Color32 c = color;
            for (int y = -r; y <= r; y++)
            {
                int py = cy + y; if (py < 0 || py >= resolution) continue;
                for (int x = -r; x <= r; x++)
                {
                    int pxx = cx + x; if (pxx < 0 || pxx >= resolution) continue;
                    float d = Mathf.Sqrt(x * x + y * y) / r; if (d > 1f) continue;
                    float a = (d < hardness ? 1f : 1f - (d - hardness) / (1f - hardness)) * opacity;
                    int i = py * resolution + pxx;
                    var p = px[i];
                    // Boş piksele ilk boya: rengi doğrudan al, kenarda siyah hale oluşmasın
                    if (p.a == 0) p = new Color32(c.r, c.g, c.b, 0);
                    px[i] = Color32.Lerp(p, c, a);
                }
            }
            dirty = true;
        }

        void LateUpdate()
        {
            if (!dirty || tex == null) return;
            tex.SetPixels32(px);
            tex.Apply(false);
            dirty = false;
        }

        public byte[] EncodePng()
        {
            tex.SetPixels32(px);
            tex.Apply(false);
            return tex.EncodeToPNG();
        }

        public void LoadPng(byte[] data)
        {
            if (data == null || data.Length == 0) return;
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (t.LoadImage(data) && t.width == resolution && t.height == resolution)
            {
                px = t.GetPixels32();
                dirty = true;
            }
            Destroy(t);
        }

        void OnDestroy() { if (tex) Destroy(tex); }
    }
}
