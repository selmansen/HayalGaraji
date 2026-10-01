using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Açılış: yatay ekranı sabitler, kaldığı arabayı yükler (yoksa ilk ücretsiz aracı),
    /// değişiklikleri birkaç saniye sonra otomatik kaydeder.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] StickerPlacer stickers;
        [SerializeField] string defaultCarId = "spor";
        [SerializeField] float autosaveDelay = 3f;

        float saveAt = -1f;

        void Awake()
        {
            Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            if (!builder.Catalog) { Debug.LogError("GameManager: CarBuilder'ın Catalog alanı boş."); return; }
            if (SaveService.LoadCurrent(out var cfg, out var paint) && builder.Catalog.GetCar(cfg.carId))
            {
                builder.Build(cfg, true);
                builder.Paint.LoadPng(paint);
                stickers.Restore();
            }
            else builder.LoadCar(defaultCarId);

            // Pofu çocuğu karşılar. İlk açılışta kendini tanıtır, sonra sadece "tekrar hoş geldin" der.
            // Not: Pofu asla suçluluk duygusu yaratan cümleler kurmaz ("gitme", "beni bıraktın" vb.).
            bool first = PlayerPrefs.GetInt("hg.metPofu", 0) == 0;
            PlayerPrefs.SetInt("hg.metPofu", 1);
            StartCoroutine(Greet(first ? "pofu_hello" : "pofu_welcome_back"));

            builder.Changed += () => saveAt = Time.unscaledTime + autosaveDelay;
        }

        System.Collections.IEnumerator Greet(string key)
        {
            yield return new WaitForSeconds(1f);
            VocabularyService.I?.Say(key);
        }

        /// <summary>Araç seçme ekranından çağrılır.</summary>
        public void SelectCar(string carId)
        {
            builder.LoadCar(carId);
            stickers.Restore();
        }

        /// <summary>Garajdan (koleksiyondan) bir araba açılır.</summary>
        public void LoadFromGarage(string id)
        {
            if (!SaveService.LoadFromGarage(id, out var cfg, out var paint)) return;
            builder.Build(cfg, true);
            builder.Paint.LoadPng(paint);
            stickers.Restore();
        }

        void Update()
        {
            if (saveAt > 0f && Time.unscaledTime >= saveAt) SaveNow();
        }

        void SaveNow()
        {
            saveAt = -1f;
            if (builder.Config == null) return;
            SaveService.SaveCurrent(builder.Config, builder.Paint.EncodePng());
            WordLog.Save();
        }

        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }
        void OnApplicationQuit() => SaveNow();
    }
}
