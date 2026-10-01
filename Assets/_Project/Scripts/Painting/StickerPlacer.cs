using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Çıkartmaları gövdeye "pat" diye yapıştırır ve kaydedilebilir tutar.</summary>
    public class StickerPlacer : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] Material decalMaterial;
        [SerializeField] float smallSize = 0.22f, bigSize = 0.3f;
        [SerializeField] AudioClip stickSound;

        public bool Big { get; set; }
        readonly List<GameObject> spawned = new List<GameObject>();

        void OnEnable() { builder.Rebuilt += OnRebuilt; }
        void OnDisable() { builder.Rebuilt -= OnRebuilt; }

        void OnRebuilt() { spawned.Clear(); Restore(); } // yeni şasi: eski çıkartmalar şasiyle birlikte silindi

        public void Place(StickerDefinition def, RaycastHit hit)
        {
            var body = builder.Chassis.bodyRoot;
            var d = new StickerData
            {
                stickerId = def.id,
                localPos = body.InverseTransformPoint(hit.point),
                localNormal = body.InverseTransformDirection(hit.normal).normalized,
                size = Big ? bigSize : smallSize,
                roll = Random.Range(-25f, 25f)
            };
            builder.Config.stickers.Add(d);
            Spawn(d, true);
            AudioService.I?.PlaySfx(stickSound ? stickSound : SynthSounds.Stick, 0.1f);
            VocabularyService.I?.Say(def.wordKey);
            builder.NotifyChanged();
        }

        public void Restore()
        {
            foreach (var go in spawned) if (go) Destroy(go);
            spawned.Clear();
            if (builder.Config == null) return;
            foreach (var d in builder.Config.stickers) Spawn(d, false);
        }

        public void ClearAll()
        {
            foreach (var go in spawned) if (go) Destroy(go);
            spawned.Clear();
            builder.Config.stickers.Clear();
            builder.NotifyChanged();
        }

        void Spawn(StickerData d, bool pop)
        {
            var def = builder.Catalog.GetSticker(d.stickerId);
            if (def == null || def.sprite == null) { Debug.LogWarning($"Çıkartma bulunamadı ya da görseli yok: {d.stickerId}"); return; }
            var r = Decals.Create(builder.Chassis.bodyRoot, "Sticker_" + d.stickerId, decalMaterial, 5);
            Decals.Set(r, def.sprite.texture, Color.white);
            var go = r.gameObject;
            go.transform.localPosition = d.localPos + d.localNormal * 0.006f;
            go.transform.localRotation = Quaternion.LookRotation(-d.localNormal) * Quaternion.Euler(0f, 0f, d.roll);
            float scale = d.size;
            spawned.Add(go);
            if (def.animated) go.AddComponent<StickerWiggle>().targetScale = scale; // kendi ölçeğini yönetir
            else if (pop) StartCoroutine(Pop(go.transform, scale));
            else go.transform.localScale = Vector3.one * scale;
        }

        static IEnumerator Pop(Transform t, float target)
        {
            for (float k = 0f; k < 1f; k += Time.deltaTime * 4f)
            {
                if (!t) yield break;
                float bounce = 1f + Mathf.Sin(k * Mathf.PI) * 0.35f * (1f - k);
                t.localScale = Vector3.one * target * Mathf.Min(1f, k * 1.4f) * bounce;
                yield return null;
            }
            if (t) t.localScale = Vector3.one * target;
        }
    }
}
