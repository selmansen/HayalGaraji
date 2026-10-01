using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Hareketli çıkartma: kelebek kanat çırpar gibi kıpırdar.</summary>
    public class StickerWiggle : MonoBehaviour
    {
        [HideInInspector] public float targetScale = 1f;
        float phase;

        void Start() { phase = Random.value * 10f; }

        void Update()
        {
            float t = Time.time * 6f + phase;
            transform.localScale = new Vector3(targetScale * (0.8f + 0.2f * Mathf.Abs(Mathf.Sin(t))), targetScale, targetScale);
        }
    }
}
