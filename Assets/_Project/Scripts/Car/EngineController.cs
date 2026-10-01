using System.Collections;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>🔊 düğmesi: motoru çalıştırır, egzoza göre ses ve efekt verir.</summary>
    public class EngineController : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] CarReactions reactions;
        [Tooltip("Egzoz takılı değilse (elektrikli) çalan ses")]
        [SerializeField] AudioClip electricSound;
        [SerializeField] float revSeconds = 1.7f;

        float busy;

        public void Rev()
        {
            if (busy > 0f || builder.Chassis == null) return;
            busy = revSeconds;
            var emitters = builder.Chassis.GetComponentsInChildren<ExhaustEmitter>();
            AudioClip clip;
            if (emitters.Length > 0) clip = emitters[0].revSound ? emitters[0].revSound : SynthSounds.ForExhaust(emitters[0].soundId);
            else clip = electricSound ? electricSound : SynthSounds.Electric;
            AudioService.I?.PlaySfx(clip, 0f);
            foreach (var e in emitters) e.Burst(revSeconds);
            builder.SpinWheels(revSeconds);
            reactions.Jump(0.8f);
        }

        public void Honk()
        {
            var horn = builder.Catalog.GetPart(builder.Config.GetPart(PartSlot.Horn));
            if (horn == null) return;
            AudioService.I?.PlaySfx(horn.sound ? horn.sound : SynthSounds.ForHorn(horn.id), 0f);
            VocabularyService.I?.Say(horn.wordKey);
            reactions.Jump(1.2f);
        }

        void Update() { if (busy > 0f) busy -= Time.deltaTime; }
    }
}
