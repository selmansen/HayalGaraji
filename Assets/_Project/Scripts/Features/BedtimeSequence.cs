using System.Collections;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Uyku vakti sahnesi: gözler uykulu olur, esneme sesi, "iyi geceler" ve garaj kapısı kapanır.
    /// Kavga yok: oyun "bitmedi", Pofu uyudu. Yarın yine buluşacaklar.
    /// </summary>
    public class BedtimeSequence : MonoBehaviour
    {
        [SerializeField] CarFace face;
        [SerializeField] TouchRouter touch;
        [SerializeField] GameObject hud;
        [SerializeField] Animator garageDoor;
        [SerializeField] GameObject sleepOverlay;
        [SerializeField] AudioClip yawn;

        public void Begin() => StartCoroutine(Run());

        IEnumerator Run()
        {
            touch.enabled = false;
            if (hud) hud.SetActive(false);
            face.SetSleepy(true);
            AudioService.I?.PlaySfx(yawn, 0f);
            VocabularyService.I?.Say("pofu_yawn");
            yield return new WaitForSeconds(2f);
            VocabularyService.I?.Say("pofu_seeyou");
            if (garageDoor) garageDoor.SetTrigger("Close");
            yield return new WaitForSeconds(1.2f);
            if (sleepOverlay) sleepOverlay.SetActive(true);
        }

        /// <summary>Ebeveyn ek süre verdiğinde.</summary>
        public void WakeUp()
        {
            StopAllCoroutines();
            if (sleepOverlay) sleepOverlay.SetActive(false);
            if (garageDoor) garageDoor.SetTrigger("Open");
            if (hud) hud.SetActive(true);
            face.SetSleepy(false);
            touch.enabled = true;
        }
    }
}
