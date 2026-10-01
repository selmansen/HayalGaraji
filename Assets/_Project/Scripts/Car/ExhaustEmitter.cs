using System.Collections;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Egzoz parçasının prefab'ında durur: sesi ve duman/baloncuk efektleri.</summary>
    public class ExhaustEmitter : MonoBehaviour
    {
        public AudioClip revSound;
        [Tooltip("Ses dosyası yoksa kodla üretilecek ses: single, double, stacks, trumpet, bubble, rainbow, confetti, electric")]
        public string soundId = "single";
        public ParticleSystem[] effects;

        public void Burst(float seconds)
        {
            StopAllCoroutines();
            StartCoroutine(Run(seconds));
        }

        IEnumerator Run(float seconds)
        {
            foreach (var p in effects) if (p) p.Play();
            yield return new WaitForSeconds(seconds);
            foreach (var p in effects) if (p) p.Stop();
        }
    }
}
