using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>
    /// 📚 Albüm: çekilen fotoğraflar polaroid gibi dizilir; birine dokununca o araba geri gelir.
    /// Fotoğraf çekilince flaş patlar ve fotoğraf albüm düğmesine uçar.
    /// Silme ileride ebeveyn alanına eklenecek (çocuk yanlışlıkla silemesin).
    /// </summary>
    public class AlbumView : MonoBehaviour
    {
        [SerializeField] GameManager game;
        [SerializeField] GameObject root;
        [SerializeField] RectTransform grid;
        [SerializeField] GameObject emptyHint;
        [SerializeField] Sprite cardSprite;
        [SerializeField] RectTransform albumButton;
        [SerializeField] Image flash;
        [SerializeField] RectTransform flyLayer;

        readonly List<Texture2D> loaded = new List<Texture2D>();

        public void Open()
        {
            root.SetActive(true);
            Build();
            VocabularyService.I?.Say("ui_album");
        }

        public void Close()
        {
            root.SetActive(false);
            Clear();
        }

        void Build()
        {
            Clear();
            int n = 0;
            foreach (var e in SaveService.ListGarage())
            {
                if (!File.Exists(e.photoPath)) continue;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(File.ReadAllBytes(e.photoPath))) { Destroy(tex); continue; }
                loaded.Add(tex);
                MakeCard(e.id, tex, n++);
            }
            if (emptyHint) emptyHint.SetActive(n == 0);
        }

        void MakeCard(string id, Texture2D tex, int index)
        {
            var card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(grid, false);
            var bg = card.AddComponent<Image>();
            bg.sprite = cardSprite; bg.type = Image.Type.Sliced; bg.color = Color.white;
            card.transform.localRotation = Quaternion.Euler(0, 0, (index % 5 - 2) * 1.5f);

            var photo = new GameObject("Photo", typeof(RectTransform));
            photo.transform.SetParent(card.transform, false);
            var prt = (RectTransform)photo.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(14, 44); prt.offsetMax = new Vector2(-14, -14);
            var raw = photo.AddComponent<RawImage>();
            raw.texture = tex; raw.raycastTarget = false;

            var btn = card.AddComponent<BouncyButton>();
            btn.onClick.AddListener(() => { game.LoadFromGarage(id); Close(); });
        }

        void Clear()
        {
            for (int i = grid.childCount - 1; i >= 0; i--) Destroy(grid.GetChild(i).gameObject);
            foreach (var t in loaded) if (t) Destroy(t);
            loaded.Clear();
        }

        /// <summary>PhotoBooth.onPhotoTaken'a bağlanır.</summary>
        public void OnPhotoTaken(Texture2D photo) => StartCoroutine(FlashAndFly(photo));

        IEnumerator FlashAndFly(Texture2D photo)
        {
            if (flash)
            {
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.4f)
                {
                    flash.color = new Color(1, 1, 1, 1f - t);
                    yield return null;
                }
                flash.color = new Color(1, 1, 1, 0);
            }
            if (!flyLayer || !albumButton) { if (photo) Destroy(photo); yield break; }

            // Polaroid: beyaz çerçeve + fotoğraf
            var frame = new GameObject("FlyingPhoto", typeof(RectTransform));
            frame.transform.SetParent(flyLayer, false);
            var frt = (RectTransform)frame.transform;
            frt.sizeDelta = new Vector2(440, 370);
            var bg = frame.AddComponent<Image>(); bg.sprite = cardSprite; bg.type = Image.Type.Sliced; bg.raycastTarget = false;
            var pic = new GameObject("Pic", typeof(RectTransform));
            pic.transform.SetParent(frame.transform, false);
            var prt = (RectTransform)pic.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(18, 56); prt.offsetMax = new Vector2(-18, -18);
            var raw = pic.AddComponent<RawImage>(); raw.texture = photo; raw.raycastTarget = false;

            Vector3 from = flyLayer.TransformPoint(Vector3.zero), to = albumButton.position;
            frt.position = from;
            yield return new WaitForSecondsRealtime(0.35f);
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                float e = t * t * (3f - 2f * t);
                frt.position = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 120f;
                frt.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, e);
                frt.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-4f, 18f, e));
                yield return null;
            }
            Destroy(frame);
            if (photo) Destroy(photo);

            // Albüm düğmesi "yuttum" diye zıplar
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                albumButton.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.22f);
                yield return null;
            }
            albumButton.localScale = Vector3.one;
        }
    }
}
