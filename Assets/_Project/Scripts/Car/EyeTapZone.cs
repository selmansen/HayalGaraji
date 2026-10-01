using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Göze dokunulabilen alan (trigger collider, CarZone katmanı).
    /// Boya modunda göze dokunmak iris rengini değiştirir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EyeTapZone : MonoBehaviour
    {
        void Reset() { GetComponent<Collider>().isTrigger = true; }
    }
}
