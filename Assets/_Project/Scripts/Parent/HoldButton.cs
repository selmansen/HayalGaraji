using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>Köşedeki 🔒: kazara açılmasın diye basılı tutmak gerekir, dolan halka gösterir.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] float holdSeconds = 2f;
        [SerializeField] Image fill;
        public UnityEvent onHeld;

        float t;
        bool holding;

        public void OnPointerDown(PointerEventData e) { holding = true; t = 0f; }
        public void OnPointerUp(PointerEventData e) => Cancel();
        public void OnPointerExit(PointerEventData e) => Cancel();

        void Cancel() { holding = false; t = 0f; if (fill) fill.fillAmount = 0f; }

        void Update()
        {
            if (!holding) return;
            t += Time.unscaledDeltaTime;
            if (fill) fill.fillAmount = t / holdSeconds;
            if (t >= holdSeconds) { Cancel(); onHeld?.Invoke(); }
        }
    }
}
