using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Bu renderer'ın hangi renkle boyanacağını söyler (gövde, tavan, jant).
    /// Kedi kulağının dışı "Body", kanatçık "Roof", jant "Rim" gibi.
    /// Ana boyanabilir gövdede kullanılmaz; o kendi materyal kopyasını kullanır.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class TintTarget : MonoBehaviour
    {
        public TintChannel channel;
        Renderer rend;
        static MaterialPropertyBlock mpb;

        public void Apply(Color color, Color emission)
        {
            if (!rend) rend = GetComponent<Renderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor(ShaderIds.BaseColor, color);
            mpb.SetColor(ShaderIds.Emission, emission);
            rend.SetPropertyBlock(mpb);
        }
    }
}
