using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>
    /// Oyuncak stüdyosu: gökyüzü, gece/gündüz, süzülen bulutlar, göz kırpan ampuller,
    /// uçuşan yapraklar, arabayı takip eden yumuşak gölge.
    /// </summary>
    public class StageController : MonoBehaviour
    {
        [Header("Bağlantılar")]
        public Material skyMaterial;
        [Tooltip("Çayır: gökyüzü gibi gece/gündüz geçişi yapar")]
        public Material groundMaterial;
        [Tooltip("Gece/gündüz düğmesinin ikonu: gündüz ay, gece güneş gösterir")]
        public Image toggleIcon;
        public Sprite dayIcon, nightIcon;
        public Light sun;
        public Renderer celestial;
        public Transform clouds;
        public Renderer[] bulbs;
        public Color[] bulbColors;
        public ParticleSystem petals;
        public ParticleSystem fireflies;
        [Tooltip("Gece beliren gezegenler")]
        public Transform nightSky;
        public ParticleSystem shootingStars;
        public Transform carRoot;
        public Transform shadow;
        [Tooltip("Ufuk tepeleri: gece hem kararır hem de gökyüzü rengine doğru sisle solar")]
        public Renderer[] hills;
        public Color[] hillDay, hillNight;

        [Header("Gündüz / Gece")]
        public Color sunDay = new Color(1f, 0.96f, 0.9f), sunNight = new Color(0.62f, 0.66f, 1f);
        public float sunIntensityDay = 1f, sunIntensityNight = 0.45f;
        public Color ambientDay = new Color(0.72f, 0.66f, 0.84f), ambientNight = new Color(0.3f, 0.28f, 0.52f);
        public Color celestialDay = new Color(1f, 0.82f, 0.25f), celestialNight = new Color(1f, 0.97f, 0.82f);

        public bool IsNight { get; private set; }
        /// <summary>0 = gündüz, 1 = gece (geçiş sırasında aradaki değerler). Arayüz teması bunu izler.</summary>
        public float NightAmount => Mathf.Clamp01(k);

        float k = -1f, target;
        Vector3 shadowBase;
        static MaterialPropertyBlock mpb;
        static readonly int NightId = Shader.PropertyToID("_Night");

        void Start()
        {
            if (shadow) shadowBase = shadow.localScale;
            Apply(0f);
        }

        public void ToggleNight() => SetNight(!IsNight);

        public void SetNight(bool night)
        {
            IsNight = night;
            target = night ? 1f : 0f;
            if (toggleIcon) toggleIcon.sprite = night ? dayIcon : nightIcon;
            VocabularyService.I?.Say(night ? "opp_night" : "opp_day");
            if (petals) { var e = petals.emission; e.enabled = !night; }
            if (fireflies) { if (night) fireflies.Play(); else fireflies.Stop(); }
            if (shootingStars) { if (night) shootingStars.Play(); else shootingStars.Stop(); }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (!Mathf.Approximately(k, target)) Apply(Mathf.MoveTowards(Mathf.Max(0f, k), target, dt * 1.2f));
            if (clouds) clouds.Rotate(0f, 1.2f * dt, 0f);

            // Gezegenler gece "pıt" diye büyüyerek belirir, gündüz küçülüp kaybolur
            if (nightSky)
            {
                float e = Mathf.Clamp01((k - 0.3f) / 0.7f);
                float pop = e <= 0f ? 0f : e * (1f + 0.25f * Mathf.Sin(e * Mathf.PI));
                bool show = pop > 0.001f;
                if (nightSky.gameObject.activeSelf != show) nightSky.gameObject.SetActive(show);
                if (show)
                    for (int i = 0; i < nightSky.childCount; i++)
                    {
                        // Her gezegen kendi yerinde büyür (merkezden uçup gelmez) ve hafifçe süzülür
                        var p = nightSky.GetChild(i);
                        p.localScale = Vector3.one * pop;
                        p.localPosition += Vector3.up * Mathf.Sin(Time.time * 0.6f + i * 1.3f) * 0.004f;
                    }
            }

            if (mpb == null) mpb = new MaterialPropertyBlock();
            if (bulbs != null)
                for (int i = 0; i < bulbs.Length; i++)
                {
                    if (!bulbs[i]) continue;
                    var c = bulbColors != null && bulbColors.Length > 0 ? bulbColors[i % bulbColors.Length] : Color.white;
                    float tw = 0.35f + 0.35f * Mathf.Sin(Time.time * 3f + i * 1.7f) + k * 0.6f;
                    bulbs[i].GetPropertyBlock(mpb);
                    mpb.SetColor(ShaderIds.BaseColor, c);
                    mpb.SetColor(ShaderIds.Emission, c * tw);
                    bulbs[i].SetPropertyBlock(mpb);
                }

            if (shadow && carRoot)
            {
                float h = Mathf.Max(0f, carRoot.localPosition.y);
                shadow.position = new Vector3(carRoot.position.x, 0.012f, carRoot.position.z);
                shadow.localScale = shadowBase * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01(h * 0.6f));
            }
        }

        void Apply(float value)
        {
            k = value;
            if (skyMaterial) skyMaterial.SetFloat(NightId, k);
            if (groundMaterial) groundMaterial.SetFloat(NightId, k);
            if (sun)
            {
                sun.color = Color.Lerp(sunDay, sunNight, k);
                sun.intensity = Mathf.Lerp(sunIntensityDay, sunIntensityNight, k);
            }
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(Color.Lerp(ambientDay, ambientNight, k));
            RenderSettings.ambientProbe = sh;
            if (hills != null && hillDay != null && hillNight != null)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                for (int i = 0; i < hills.Length && i < hillDay.Length && i < hillNight.Length; i++)
                {
                    if (!hills[i]) continue;
                    hills[i].GetPropertyBlock(mpb);
                    mpb.SetColor(ShaderIds.BaseColor, Color.Lerp(hillDay[i], hillNight[i], k));
                    hills[i].SetPropertyBlock(mpb);
                }
            }
            if (celestial)
            {
                if (mpb == null) mpb = new MaterialPropertyBlock();
                var c = Color.Lerp(celestialDay, celestialNight, k);
                celestial.GetPropertyBlock(mpb);
                mpb.SetColor(ShaderIds.BaseColor, c);
                mpb.SetColor(ShaderIds.Emission, c * 0.6f);
                celestial.SetPropertyBlock(mpb);
            }
        }

        void OnDisable()
        {
            if (skyMaterial) skyMaterial.SetFloat(NightId, 0f);
            if (groundMaterial) groundMaterial.SetFloat(NightId, 0f);
        }
    }
}
