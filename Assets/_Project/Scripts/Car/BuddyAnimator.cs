using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Şoför arkadaş canlı dursun: hafif nefes alır, etrafa bakınır; dokununca zıplar.</summary>
    public class BuddyAnimator : MonoBehaviour
    {
        float hop, seed;
        Vector3 basePos;
        Quaternion baseRot;

        void Awake()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            seed = Random.value * 10f;
        }

        public void Hop() => hop = 1f;

        void Update()
        {
            float t = Time.time + seed;
            hop = Mathf.MoveTowards(hop, 0f, Time.deltaTime * 2.2f);
            float jump = hop > 0f ? Mathf.Sin((1f - hop) * Mathf.PI) * 0.08f : 0f;
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 2.2f) * 0.006f + jump);
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.7f) * 12f, Mathf.Sin(t * 1.3f) * 4f + (hop > 0f ? Mathf.Sin(hop * 20f) * 8f * hop : 0f));
        }
    }
}
