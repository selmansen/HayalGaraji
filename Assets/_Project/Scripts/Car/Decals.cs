using UnityEngine;
using UnityEngine.Rendering;

namespace HayalGaraji
{
    /// <summary>Gövdeye yapışık görsel katmanları (göz, çıkartma) üretmek için yardımcı.</summary>
    public static class Decals
    {
        static Mesh quad;
        static MaterialPropertyBlock mpb;
        static readonly int MainTex = Shader.PropertyToID("_MainTex");

        /// <summary>1x1 birimlik, ön yüzü yerel -Z'ye bakan kare. LookRotation(-normal) ile yüzeye yapıştırılır.</summary>
        public static MeshRenderer Create(Transform parent, string name, Material mat, int order)
        {
            if (!quad) quad = BuildQuad();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = quad;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        /// <summary>Kendi kare mesh'imiz: yerleşik kaynak araması bazı sürümlerde sessizce boş dönüyor.</summary>
        static Mesh BuildQuad()
        {
            var m = new Mesh { name = "DecalQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateBounds();
            return m;
        }

        public static void Set(Renderer r, Texture tex, Color color)
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            if (tex) mpb.SetTexture(MainTex, tex);
            mpb.SetColor(ShaderIds.BaseColor, color);
            r.SetPropertyBlock(mpb);
        }
    }
}
