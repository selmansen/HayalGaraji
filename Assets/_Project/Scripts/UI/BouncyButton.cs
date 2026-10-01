using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace HayalGaraji
{
    /// <summary>
    /// Çocuk düğmesi: basınca ezilir, bırakınca zıplar, "pop" sesi çıkarır ve adını söyler.
    /// Yazı yok; anlam ikon + ses ile verilir.
    /// </summary>
    public class BouncyButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public string wordKey;
        public UnityEvent onClick = new UnityEvent();
        [SerializeField] float pressedScale = 0.88f;

        Vector3 baseScale;
        Coroutine anim;

        void Awake() { baseScale = transform.localScale; }

        public void OnPointerDown(PointerEventData e) => AnimateTo(pressedScale, 0.06f);
        public void OnPointerUp(PointerEventData e) => AnimateTo(1f, 0.1f);

        public void OnPointerClick(PointerEventData e)
        {
            AudioService.I?.Pop();
            VocabularyService.I?.Say(wordKey);
            onClick.Invoke();
            // Tıklama düğmeyi kapatmış olabilir (ör. albüm kartı dokununca albüm kapanır): o zaman animasyon yok
            if (!isActiveAndEnabled) return;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(Boing());
        }

        void AnimateTo(float s, float dur)
        {
            if (!isActiveAndEnabled) return;
            if (anim != null) StopCoroutine(anim);
            anim = StartCoroutine(ScaleTo(s, dur));
        }

        IEnumerator ScaleTo(float target, float dur)
        {
            var from = transform.localScale; var to = baseScale * target;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / dur)
            {
                transform.localScale = Vector3.Lerp(from, to, t);
                yield return null;
            }
            transform.localScale = to;
        }

        IEnumerator Boing()
        {
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
            {
                float s = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.12f * (1f - t);
                transform.localScale = baseScale * s;
                yield return null;
            }
            transform.localScale = baseScale;
        }

        void OnDisable() { transform.localScale = baseScale; }
    }
}
