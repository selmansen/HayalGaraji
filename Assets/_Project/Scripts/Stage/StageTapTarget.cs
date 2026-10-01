using UnityEngine;
using UnityEngine.Events;

namespace HayalGaraji
{
    /// <summary>Sahnedeki dokunulabilir süsler (güneş/ay gibi). Collider ile birlikte kullanılır.</summary>
    public class StageTapTarget : MonoBehaviour
    {
        public UnityEvent onTap = new UnityEvent();

        public void Tap()
        {
            AudioService.I?.Pop();
            onTap.Invoke();
        }
    }
}
