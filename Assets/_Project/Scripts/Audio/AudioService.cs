using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Üç kanal: seslendirme (kelimeler), efektler, döngü (sprey/yıkama).</summary>
    public class AudioService : MonoBehaviour
    {
        public static AudioService I { get; private set; }

        [SerializeField] AudioSource voice, sfx, loop;
        [SerializeField] AudioClip pop;

        void Awake() { I = this; }

        public void PlayVoice(AudioClip clip)
        {
            if (!clip) return;
            voice.Stop();
            voice.clip = clip;
            voice.Play();
        }

        public void PlaySfx(AudioClip clip, float pitchVariance = 0.05f)
        {
            if (!clip) return;
            sfx.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            sfx.PlayOneShot(clip);
        }

        public void Pop() => PlaySfx(pop ? pop : SynthSounds.Pop, 0.1f);

        public void SetLoop(AudioClip clip)
        {
            if (!clip) { loop.Stop(); return; }
            if (loop.clip == clip && loop.isPlaying) return;
            loop.clip = clip; loop.loop = true; loop.Play();
        }
    }
}
