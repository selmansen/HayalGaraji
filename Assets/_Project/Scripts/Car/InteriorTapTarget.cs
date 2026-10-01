using System.Collections;
using UnityEngine;

namespace HayalGaraji
{
    public enum InteriorKind { Steering, DashButton, Buddy, Toy }

    /// <summary>
    /// Arabanın içinde dokunulabilen şeyler:
    ///   Direksiyon → korna çalar, direksiyon sağa sola döner.
    ///   Gösterge düğmeleri → nota çalar, renginin adını söyler (kırmızı, sarı, mavi).
    ///   Arkadaş → zıplar, adını söyler.
    ///   Oyuncak → sallanır.
    /// </summary>
    public class InteriorTapTarget : MonoBehaviour
    {
        public InteriorKind kind;
        public string wordKey;
        public float noteHz = 523f;

        Quaternion baseRot;
        Vector3 baseScale;
        Coroutine anim;
        static EngineController engine;
        static CarBuilder builder;

        void Awake() { baseRot = transform.localRotation; baseScale = transform.localScale; }

        public void Tap()
        {
            switch (kind)
            {
                case InteriorKind.Steering:
                    if (!engine) engine = FindFirstObjectByType<EngineController>();
                    if (engine) engine.Honk();
                    Run(Wiggle());
                    break;
                case InteriorKind.DashButton:
                    AudioService.I?.PlaySfx(SynthSounds.Note(noteHz), 0f);
                    if (!string.IsNullOrEmpty(wordKey)) VocabularyService.I?.Say(wordKey);
                    Run(Press());
                    break;
                case InteriorKind.Buddy:
                    var b = GetComponentInChildren<BuddyAnimator>();
                    if (b) b.Hop();
                    AudioService.I?.PlaySfx(SynthSounds.Giggle, 0.1f);
                    if (!builder) builder = FindFirstObjectByType<CarBuilder>();
                    var part = builder && builder.Config != null ? builder.Catalog.GetPart(builder.Config.GetPart(PartSlot.Buddy)) : null;
                    if (part) VocabularyService.I?.Say(part.wordKey);
                    break;
                case InteriorKind.Toy:
                    AudioService.I?.PlaySfx(SynthSounds.Boing, 0.1f);
                    Run(Swing());
                    break;
            }
        }

        void Run(IEnumerator r)
        {
            if (anim != null) StopCoroutine(anim);
            transform.localRotation = baseRot; transform.localScale = baseScale;
            anim = StartCoroutine(r);
        }

        IEnumerator Wiggle()
        {
            for (float t = 0f; t < 0.8f; t += Time.deltaTime)
            {
                transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 18f) * 35f * (1f - t / 0.8f), 0f, 0f);
                yield return null;
            }
            transform.localRotation = baseRot;
        }

        IEnumerator Press()
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                float k = t / 0.3f;
                transform.localScale = new Vector3(baseScale.x * (1f + 0.25f * Mathf.Sin(k * Mathf.PI)), baseScale.y * (1f - 0.45f * Mathf.Sin(k * Mathf.PI)), baseScale.z * (1f + 0.25f * Mathf.Sin(k * Mathf.PI)));
                yield return null;
            }
            transform.localScale = baseScale;
        }

        IEnumerator Swing()
        {
            for (float t = 0f; t < 2f; t += Time.deltaTime)
            {
                float d = 1f - t / 2f;
                transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * 9f) * 28f * d, 0f, Mathf.Sin(t * 7f) * 18f * d);
                yield return null;
            }
            transform.localRotation = baseRot;
        }
    }
}
