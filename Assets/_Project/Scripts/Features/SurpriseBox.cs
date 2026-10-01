using System.Collections;
using System.Linq;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// 🎁 Sürpriz kutusu: davul sesi, kutu açılır, araba baştan aşağı değişir.
    /// Her zaman ücretsiz ve sınırsız; asla satın almaya veya "nadir ödüle" bağlanmaz.
    /// Araç tipi korunur (çocuk onu kendisi seçti).
    /// </summary>
    public class SurpriseBox : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] StickerPlacer stickers;
        [SerializeField] CarReactions reactions;
        [SerializeField] Animator boxAnimator;
        [SerializeField] AudioClip drumroll, tada;
        [SerializeField] ParticleSystem confetti;
        [SerializeField] float revealDelay = 0.9f;
        [SerializeField, Range(0f, 1f)] float partChance = 0.55f;

        bool busy;

        public void Open()
        {
            if (!busy && builder.Config != null) StartCoroutine(Run());
        }

        IEnumerator Run()
        {
            busy = true;
            if (boxAnimator) boxAnimator.SetTrigger("Open");
            AudioService.I?.PlaySfx(drumroll ? drumroll : SynthSounds.Drumroll, 0f);
            yield return new WaitForSeconds(revealDelay);

            builder.Build(Randomize(builder.Config), false);
            stickers.Restore();
            reactions.Jump(3.5f);
            if (confetti) confetti.Play();
            AudioService.I?.PlaySfx(tada ? tada : SynthSounds.Fanfare, 0f);
            VocabularyService.I?.Say("pofu_surprise");
            yield return new WaitForSeconds(0.4f);
            busy = false;
        }

        CarConfig Randomize(CarConfig src)
        {
            var c = src.Clone();
            var cat = builder.Catalog;
            if (cat.colors.Count > 0)
            {
                c.bodyColor = CarConfig.Hex(Pick(cat.colors).color);
                c.roofColor = CarConfig.Hex(Pick(cat.colors).color);
                c.rimColor = CarConfig.Hex(Pick(cat.colors).color);
                c.eyeColor = CarConfig.Hex(Pick(cat.colors).color);
            }
            float f = Random.value;
            c.finish = f < 0.55f ? Finish.Solid : f < 0.7f ? Finish.Rainbow : f < 0.85f ? Finish.TwoTone : Finish.Neon;
            if (cat.eyeStyles.Count > 0) c.eyeStyle = Pick(cat.eyeStyles).id;
            c.wheelSize = (WheelSize)Random.Range(0, 3);
            c.height = Random.value < 0.5f ? RideHeight.Normal : (RideHeight)Random.Range(0, 4);

            SetRandom(c, PartSlot.Wheels, 1f);
            SetRandom(c, PartSlot.Buddy, 1f);
            SetRandom(c, PartSlot.Horn, 1f);
            SetRandom(c, PartSlot.Exhaust, 0.8f);
            foreach (var s in new[] { PartSlot.Front, PartSlot.Top, PartSlot.Back, PartSlot.Side, PartSlot.Under })
                SetRandom(c, s, partChance);
            return c;
        }

        void SetRandom(CarConfig c, PartSlot slot, float chance)
        {
            var options = builder.Catalog.PartsFor(slot, builder.Family).ToList();
            c.SetPart(slot, options.Count > 0 && Random.value < chance ? options[Random.Range(0, options.Count)].id : null);
        }

        static T Pick<T>(System.Collections.Generic.IList<T> list) => list[Random.Range(0, list.Count)];
    }
}
