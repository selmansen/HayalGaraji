using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace HayalGaraji
{
    /// <summary>
    /// Uygulama dili cihaz diliyle açılır (Localization ayarlarındaki seçici sırası:
    /// PlayerPrefs → Sistem dili → İngilizce). Ebeveyn değiştirirse PlayerPrefs seçicisi hatırlar.
    /// Öğrenilen dil ayrıca burada tutulur.
    /// </summary>
    public static class LanguagePrefs
    {
        const string LearnKey = "hg.learnLocale";
        const string ModeKey = "hg.learnMode";

        public static LearnMode Mode
        {
            get => (LearnMode)PlayerPrefs.GetInt(ModeKey, (int)LearnMode.Both);
            set { PlayerPrefs.SetInt(ModeKey, (int)value); PlayerPrefs.Save(); }
        }

        public static Locale AppLocale
        {
            get => LocalizationSettings.SelectedLocale;
            set => LocalizationSettings.SelectedLocale = value;
        }

        /// <summary>Kayıt yoksa: ana dil İngilizce ise Türkçe, değilse İngilizce öğretilir.</summary>
        public static Locale LearningLocale
        {
            get
            {
                var locales = LocalizationSettings.AvailableLocales;
                string code = PlayerPrefs.GetString(LearnKey, "");
                if (!string.IsNullOrEmpty(code))
                {
                    var saved = locales.GetLocale(code);
                    if (saved != null) return saved;
                }
                var native = LocalizationSettings.SelectedLocale;
                bool nativeIsEnglish = native != null && native.Identifier.Code.StartsWith("en");
                return locales.GetLocale(nativeIsEnglish ? "tr" : "en");
            }
            set
            {
                PlayerPrefs.SetString(LearnKey, value != null ? value.Identifier.Code : "");
                PlayerPrefs.Save();
            }
        }
    }
}
