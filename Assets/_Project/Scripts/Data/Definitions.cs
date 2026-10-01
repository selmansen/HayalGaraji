using System;
using UnityEngine;

namespace HayalGaraji
{
    [Serializable] public class PaletteColor { public Color color = Color.white; public string wordKey; }

    [Serializable]
    public class StickerDefinition
    {
        public string id;
        public Sprite sprite;
        public string wordKey;
        public bool premium;
        [Tooltip("Kelebek gibi kıpırdayan çıkartmalar")]
        public bool animated;
    }

    /// <summary>Anime göz seti. Göz akı + renklendirilebilir iris + parıltı katmanlarından oluşur.</summary>
    [Serializable]
    public class EyeStyle
    {
        public string id;
        public Sprite white;
        public Sprite iris;
        public Sprite highlight;
        [Tooltip("Mutlu kapalı göz (^ ^)")] public Sprite happy;
        [Tooltip("Uykulu göz (ekran süresi bitince)")] public Sprite sleepy;
        public string wordKey;
        public bool tintIris = true;
        public Sprite thumbnail;
    }
}
