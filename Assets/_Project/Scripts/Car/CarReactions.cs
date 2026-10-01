using System;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Pofu'nun canlı tepkileri: dokununca zıplar, kıkırdar, kalp saçar.</summary>
    public class CarReactions : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] CarFace face;
        [SerializeField] ParticleSystem heartsFx;
        [SerializeField] AudioClip[] giggles;
        [SerializeField] AudioClip landThud;
        [SerializeField] float gravity = 9.8f;

        [Tooltip("Yere inince ezilip esneme miktarı")]
        [SerializeField] float squashAmount = 1f;

        public event Action Jumped;
        float y, v, sq, sqV;

        public void Jump(float power = 3.2f)
        {
            v = y <= 0.001f ? power : Mathf.Max(v, power * 0.6f);
            sq = -0.1f * squashAmount; sqV = 0f; // kalkarken hafif uzama
            Jumped?.Invoke();
        }

        /// <summary>Çocuk arabaya dokundu.</summary>
        public void Tapped(Vector3 worldPoint)
        {
            Jump();
            face.PlayHappy(1.3f);
            AudioService.I?.PlaySfx(giggles != null && giggles.Length > 0 ? giggles[UnityEngine.Random.Range(0, giggles.Length)] : SynthSounds.Giggle, 0.08f);
            if (heartsFx)
            {
                heartsFx.transform.position = worldPoint + Vector3.up * 0.3f;
                heartsFx.Play();
            }
        }

        /// <summary>Yıkarken arada bir gıdıklanma.</summary>
        public void Tickle()
        {
            if (UnityEngine.Random.value > 0.04f) return;
            face.PlayHappy(0.8f);
            Jump(1.4f);
            VocabularyService.I?.Say("pofu_giggle");
            AudioService.I?.PlaySfx(giggles != null && giggles.Length > 0 ? giggles[UnityEngine.Random.Range(0, giggles.Length)] : SynthSounds.Giggle, 0.12f);
        }

        void Update()
        {
            // Ezilme-esneme yayı (squash & stretch)
            if (Mathf.Abs(sq) > 0.0005f || Mathf.Abs(sqV) > 0.0005f)
            {
                sqV += (-140f * sq - 11f * sqV) * Time.deltaTime;
                sq += sqV * Time.deltaTime;
                builder.CarRoot.localScale = new Vector3(1f + sq * 0.6f, 1f - sq, 1f + sq * 0.6f);
            }
            else if (builder.CarRoot.localScale != Vector3.one) builder.CarRoot.localScale = Vector3.one;

            if (y <= 0f && Mathf.Approximately(v, 0f)) return;
            v -= gravity * Time.deltaTime;
            y += v * Time.deltaTime;
            if (y <= 0f)
            {
                y = 0f;
                if (v < -1.5f)
                {
                    sq = Mathf.Min(0.22f, -v * 0.045f) * squashAmount; sqV = 0f; // yere inince ezil
                    if (v < -4f) AudioService.I?.PlaySfx(landThud ? landThud : SynthSounds.Thud, 0.05f);
                    v = -v * 0.35f;
                }
                else v = 0f;
            }
            builder.CarRoot.localPosition = new Vector3(0f, y, 0f);
        }
    }
}
