using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace HayalGaraji
{
    /// <summary>
    /// Kelime makinesi. Say("color_red") → "Kırmızı!" ... "Red!"
    /// Sesler "WordAudio" asset tablosundan, dile göre gelir (yapay zekâ ile üretilip gömülür).
    /// </summary>
    public class VocabularyService : MonoBehaviour
    {
        public static VocabularyService I { get; private set; }

        [SerializeField] string audioTable = "WordAudio";
        [SerializeField] float gapBetweenLanguages = 0.25f;

        Coroutine running;

        void Awake() { I = this; }

        public void Say(string wordKey)
        {
            if (string.IsNullOrEmpty(wordKey)) return;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(SayRoutine(wordKey));
            WordLog.Record(wordKey);
        }

        IEnumerator SayRoutine(string key)
        {
            yield return LocalizationSettings.InitializationOperation;
            var native = LanguagePrefs.AppLocale;
            var learn = LanguagePrefs.LearningLocale;
            var mode = LanguagePrefs.Mode;
            bool differ = learn != null && native != null && learn.Identifier.Code != native.Identifier.Code;

            if (mode != LearnMode.LearningOnly || !differ)
                yield return Play(key, native);

            if (differ && mode != LearnMode.NativeOnly)
            {
                if (mode == LearnMode.Both) yield return new WaitForSeconds(gapBetweenLanguages);
                yield return Play(key, learn);
            }
            running = null;
        }

        IEnumerator Play(string key, Locale locale)
        {
            var handle = LocalizationSettings.AssetDatabase.GetLocalizedAssetAsync<AudioClip>(audioTable, key, locale);
            yield return handle;
            var clip = handle.Result;
            if (!clip) yield break;
            AudioService.I?.PlayVoice(clip);
            yield return new WaitForSeconds(clip.length);
        }
    }
}
