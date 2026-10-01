using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Parçaların takıldığı boş nokta (Ön, Tavan, Arka, Yan, Alt, Egzoz).</summary>
    public class CarSocket : MonoBehaviour
    {
        public PartSlot slot;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.65f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.06f);
            Gizmos.DrawRay(transform.position, transform.up * 0.15f);
        }
    }
}
