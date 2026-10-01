using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Arabanın dokunulabilir bölgesi. Çocuk tavana dokununca tavan parçaları açılır.
    /// Trigger collider ile birlikte, "CarZone" katmanında kullanılır.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TapZone : MonoBehaviour
    {
        public PartSlot slot;
        void Reset() { GetComponent<Collider>().isTrigger = true; }
    }
}
