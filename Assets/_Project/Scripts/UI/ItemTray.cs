using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>Tepsideki tek bir seçenek.</summary>
    public struct TrayEntry
    {
        public Sprite icon;
        public Color? swatch;   // doluysa ikon yerine renk topu gösterilir
        public string wordKey;
        public bool selected;
        public Action onPick;
    }

    /// <summary>
    /// Alttaki seçenek tepsisi. İçerik kadar genişler ve ekranda ortalanır; sığmazsa
    /// kaydırılabilir olur ve kenarlarda yumuşak solma görünür.
    /// Öğe yapısı: TrayItem (balon + BouncyButton) → SelectedBg (turkuaz balon), Icon.
    /// </summary>
    public class ItemTray : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] ScrollRect scroll;
        [SerializeField] RectTransform content;
        [SerializeField] BouncyButton itemPrefab;
        [SerializeField] Sprite swatchSprite;
        [SerializeField] GameObject fadeLeft, fadeRight;
        [Tooltip("HorizontalLayoutGroup ile aynı olmalı")]
        [SerializeField] float itemSize = 116f, spacing = 14f, padding = 20f;
        [Tooltip("Sol ray ve sağ düğmeler için her iki yanda bırakılan boşluk")]
        [SerializeField] float sideReserve = 230f;
        [SerializeField] float selectedIconScale = 1.12f;

        /// <summary>Öğe balonlarının rengi (gündüz beyaz, gece koyu). UiTheme ile değişir.</summary>
        public Color BubbleColor { get; set; } = Color.white;

        readonly List<BouncyButton> pool = new List<BouncyButton>();
        float contentWidth;
        int count;

        void Awake() { scroll.onValueChanged.AddListener(_ => UpdateFades()); }

        public void Show(IList<TrayEntry> entries)
        {
            while (pool.Count < entries.Count) pool.Add(Instantiate(itemPrefab, content));
            for (int i = 0; i < pool.Count; i++)
            {
                var b = pool[i];
                bool active = i < entries.Count;
                b.gameObject.SetActive(active);
                if (!active) continue;

                var e = entries[i];
                var bubble = b.GetComponent<Image>();
                if (bubble) bubble.color = BubbleColor;
                var icon = b.transform.Find("Icon").GetComponent<Image>();
                if (e.swatch.HasValue) { icon.sprite = swatchSprite; icon.color = e.swatch.Value; }
                else { icon.sprite = e.icon; icon.color = Color.white; }
                icon.preserveAspect = true;
                icon.rectTransform.localScale = Vector3.one * (e.selected ? selectedIconScale : 1f);
                var sel = b.transform.Find("SelectedBg");
                if (sel) sel.gameObject.SetActive(e.selected);

                b.wordKey = e.wordKey;
                b.onClick.RemoveAllListeners();
                var pick = e.onPick;
                b.onClick.AddListener(() => pick?.Invoke());
            }
            bool changedCount = count != entries.Count;
            count = entries.Count;
            Layout(changedCount);
        }

        void Layout(bool resetScroll)
        {
            contentWidth = count * itemSize + Mathf.Max(0, count - 1) * spacing + 2f * padding;
            var parent = panel.parent as RectTransform;
            float maxW = parent != null ? parent.rect.width - 2f * sideReserve : 1400f;
            if (maxW <= 0f) maxW = 1400f;
            bool overflow = contentWidth > maxW + 0.5f;
            float width = contentWidth;
            if (overflow)
            {
                // Sığmıyorsa genişlik, son görünen öğe tam YARIM kalacak şekilde seçilir: "devamı var" der
                float step = itemSize + spacing;
                int k = Mathf.Max(1, Mathf.FloorToInt((maxW - padding - itemSize * 0.5f) / step));
                width = padding + k * step + itemSize * 0.5f;
            }
            panel.sizeDelta = new Vector2(Mathf.Round(width), panel.sizeDelta.y);
            scroll.horizontal = overflow;
            if (resetScroll || !overflow)
            {
                scroll.StopMovement();
                content.anchoredPosition = new Vector2(0f, content.anchoredPosition.y);
            }
            UpdateFades();
        }

        /// <summary>Kenar solmaları: sadece o yönde kaydırılacak içerik varken görünür.</summary>
        void UpdateFades()
        {
            bool overflow = scroll.horizontal;
            float x = -content.anchoredPosition.x;
            float max = Mathf.Max(0f, contentWidth - panel.rect.width);
            if (fadeLeft) fadeLeft.SetActive(overflow && x > 2f);
            if (fadeRight) fadeRight.SetActive(overflow && x < max - 2f);
        }

        /// <summary>Tema değişince mevcut öğelerin balon rengini günceller.</summary>
        public void Recolor()
        {
            foreach (var b in pool)
            {
                if (!b || !b.gameObject.activeSelf) continue;
                var img = b.GetComponent<Image>();
                if (img) img.color = BubbleColor;
            }
        }

        /// <summary>Kaydırılabilir tepside verilen öğeyi görünür yapar.</summary>
        public void ScrollTo(int index)
        {
            if (!scroll.horizontal || index < 0) return;
            float viewW = panel.rect.width;
            float x = padding + index * (itemSize + spacing) - itemSize * 0.5f;
            float max = Mathf.Max(0f, contentWidth - viewW);
            scroll.StopMovement();
            content.anchoredPosition = new Vector2(-Mathf.Clamp(x, 0f, max), content.anchoredPosition.y);
        }
    }
}
