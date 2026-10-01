using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static HayalGaraji.EditorTools.ChibiMesh;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Ön süsler: Pofu'nun yüzünün etrafında şekil çerçeveleri (daire, kare, dikdörtgen, üçgen, kalp, yıldız).
    /// Dokununca şeklin adı söylenir: hem süs hem şekil öğretimi. Çerçeve jant rengini alır.
    /// Ön yuva yüzün ortasındadır; çerçeve biraz öndedir ve iki kısa direkle gövdeye bağlanır.
    /// Ölçüler yüzü (gözler ve ağız) kapatmayacak şekilde, aşağı doğru açık tutulmuştur.
    /// </summary>
    public static class FrameFactory
    {
        public static List<(string id, GameObject prefab, string word)> BuildAll(string dir, Material tube, Material chrome)
        {
            var list = new List<(string, GameObject, string)>();
            void Add(string id, string word, List<Vector2> outline) => list.Add((id, Make(dir, id, outline, tube, chrome), word));

            var circle = new List<Vector2>();
            for (int i = 0; i < 96; i++) { float a = 2f * Mathf.PI * i / 96; circle.Add(new Vector2(Mathf.Cos(a) * 0.5f, Mathf.Sin(a) * 0.46f)); }
            Add("frame_circle", "shape_circle", circle);
            Add("frame_square", "shape_square", RoundedOutline(new[] { new Vector2(-0.46f, -0.4f), new Vector2(0.46f, -0.4f), new Vector2(0.46f, 0.44f), new Vector2(-0.46f, 0.44f) }, 0.1f));
            Add("frame_rectangle", "shape_rectangle", RoundedOutline(new[] { new Vector2(-0.62f, -0.36f), new Vector2(0.62f, -0.36f), new Vector2(0.62f, 0.32f), new Vector2(-0.62f, 0.32f) }, 0.1f));
            Add("frame_triangle", "shape_triangle", RoundedOutline(new[] { new Vector2(-0.74f, -0.38f), new Vector2(0.74f, -0.38f), new Vector2(0f, 0.66f) }, 0.12f, 10, 10));
            var heart = new List<Vector2>();
            for (int i = 0; i < 120; i++)
            {
                float t = 2f * Mathf.PI * i / 120;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f), y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                heart.Add(new Vector2(x, y) * 0.036f + new Vector2(0f, 0.04f));
            }
            Add("frame_heart", "shape_heart", heart);
            var star = new List<Vector2>();
            for (int i = 0; i < 10; i++) { float a = Mathf.PI / 2f + i * Mathf.PI / 5f; float r = i % 2 == 0 ? 0.72f : 0.4f; star.Add(new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r - 0.02f)); }
            Add("frame_star", "shape_star", RoundedOutline(star, 0.06f, 6, 5));
            return list;
        }

        static GameObject Make(string dir, string id, List<Vector2> outline, Material tube, Material chrome)
        {
            var root = new GameObject(id);
            var frame = new GameObject("Frame");
            frame.transform.SetParent(root.transform, false);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.09f);
            var mesh = TubeLoop(id, outline, 0.034f);
            AssetDatabase.CreateAsset(mesh, $"{dir}/Meshes/Front_{id}.asset");
            frame.AddComponent<MeshFilter>().sharedMesh = mesh;
            frame.AddComponent<MeshRenderer>().sharedMaterial = tube;
            frame.AddComponent<TintTarget>().channel = TintChannel.Rim;

            // Gövdeye bağlayan iki kısa direk: şeklin sol ve sağ alt noktalarından
            Vector2 left = outline[0], right = outline[0];
            foreach (var p in outline)
            {
                if (p.x < -0.12f && (left.x >= -0.12f || p.y < left.y)) left = p;
                if (p.x > 0.12f && (right.x <= 0.12f || p.y < right.y)) right = p;
            }
            var postMesh = Ellipsoid(new Vector3(0.026f, 0.026f, 0.09f), 8, 12);
            AssetDatabase.CreateAsset(postMesh, $"{dir}/Meshes/Front_{id}_Post.asset");
            foreach (var p in new[] { left, right })
            {
                var post = new GameObject("Post");
                post.transform.SetParent(root.transform, false);
                post.transform.localPosition = new Vector3(p.x, p.y, 0.02f);
                post.AddComponent<MeshFilter>().sharedMesh = postMesh;
                post.AddComponent<MeshRenderer>().sharedMaterial = chrome;
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{dir}/Prefabs/Front_{id}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
