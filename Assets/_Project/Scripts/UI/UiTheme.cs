using System;
using UnityEngine;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>
    /// Arayüzün gündüz / gece teması: gece olunca paneller ve düğmeler koyu temaya geçer
    /// (web sitelerindeki açık/koyu mod gibi). Geçiş sahneyle aynı anda, yumuşakça olur.
    /// </summary>
    public class UiTheme : MonoBehaviour
    {
        public static UiTheme I { get; private set; }

        [SerializeField] StageController stage;
        [Tooltip("Yarı saydam paneller (ray, tepsi, kuleler, albüm)")]
        [SerializeField] Image[] panels;
        [Tooltip("Balon düğmeler (sağ sütun, kule düğmeleri, albüm kapat). Ray düğmeleri HudController'dadır.")]
        [SerializeField] Image[] buttons;
        [Tooltip("Tepsi kenar solmaları")]
        [SerializeField] Image[] fades;

        public Color panelDay = new Color(0.88f, 0.97f, 1f, 0.78f), panelNight = new Color(0.17f, 0.15f, 0.34f, 0.84f);
        public Color buttonDay = Color.white, buttonNight = new Color(0.34f, 0.31f, 0.58f);
        public Color dotOffDay = new Color(1f, 1f, 1f, 0.95f), dotOffNight = new Color(0.5f, 0.47f, 0.74f);

        public Color ButtonColor { get; private set; } = Color.white;
        public Color DotOff { get; private set; } = Color.white;
        public event Action Changed;

        float last = -1f;

        void Awake() { I = this; Apply(0f); }

        void Update()
        {
            float k = stage ? stage.NightAmount : 0f;
            if (!Mathf.Approximately(k, last)) Apply(k);
        }

        void Apply(float k)
        {
            last = k;
            var pc = Color.Lerp(panelDay, panelNight, k);
            if (panels != null) foreach (var p in panels) if (p) p.color = pc;
            if (fades != null) foreach (var f in fades) if (f) f.color = new Color(pc.r, pc.g, pc.b, 0.95f);
            ButtonColor = Color.Lerp(buttonDay, buttonNight, k);
            DotOff = Color.Lerp(dotOffDay, dotOffNight, k);
            if (buttons != null) foreach (var b in buttons) if (b) b.color = ButtonColor;
            Changed?.Invoke();
        }
    }
}
