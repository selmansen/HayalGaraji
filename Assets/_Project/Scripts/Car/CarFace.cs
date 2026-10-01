using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Pofu'nun 3D anime yüzü. Gözler aracın prefab'ında hazır şekillerdir:
    ///   EyeL / EyeR
    ///     Open   → White + Iris_normal / Iris_yildiz / Iris_kalp …
    ///     Happy  → mutlu kapalı göz (^ ^)
    ///     Sleepy → uykulu göz (‿)
    /// Bu bileşen stili seçer, iris rengini boyar, göz kırptırır ve ifadeleri değiştirir.
    /// </summary>
    public class CarFace : MonoBehaviour
    {
        [SerializeField] CarBuilder builder;
        [SerializeField] Vector2 blinkInterval = new Vector2(2f, 5f);

        class Eye { public Transform root, open, happy, sleepy; }

        Eye left, right;
        float happyUntil, nextBlink, blinkT;
        bool sleepy;
        static MaterialPropertyBlock mpb;

        void OnEnable() { builder.Rebuilt += Rebuild; }
        void OnDisable() { builder.Rebuilt -= Rebuild; }

        void Rebuild()
        {
            var anchor = builder.Chassis.faceAnchor;
            left = anchor ? Find(anchor.Find("EyeL")) : null;
            right = anchor ? Find(anchor.Find("EyeR")) : null;
            Apply();
        }

        static Eye Find(Transform root)
        {
            if (!root) return null;
            return new Eye { root = root, open = root.Find("Open"), happy = root.Find("Happy"), sleepy = root.Find("Sleepy") };
        }

        public void SetStyle(string id) { builder.Config.eyeStyle = id; Apply(); builder.NotifyChanged(); }
        public void SetEyeColor(Color c) { builder.Config.eyeColor = CarConfig.Hex(c); Apply(); builder.NotifyChanged(); }
        public void PlayHappy(float seconds) { happyUntil = Time.time + seconds; Apply(); }
        public void SetSleepy(bool value) { sleepy = value; Apply(); }

        void Apply()
        {
            string id = builder.Config != null ? builder.Config.eyeStyle : "normal";
            bool happy = Time.time < happyUntil;
            var irisColor = CarConfig.Col(builder.Config != null ? builder.Config.eyeColor : "#4AA8FF");
            if (mpb == null) mpb = new MaterialPropertyBlock();

            foreach (var e in new[] { left, right })
            {
                if (e == null || e.open == null) continue;
                bool closed = sleepy || happy;
                e.open.gameObject.SetActive(!closed);
                if (e.happy) e.happy.gameObject.SetActive(happy && !sleepy);
                if (e.sleepy) e.sleepy.gameObject.SetActive(sleepy);

                bool found = e.open.Find("Iris_" + id) != null;
                foreach (Transform c in e.open)
                    if (c.name.StartsWith("Iris_"))
                        c.gameObject.SetActive(c.name == (found ? "Iris_" + id : "Iris_normal"));

                var iris = e.open.Find("Iris_normal/Iris");
                if (iris && iris.TryGetComponent<Renderer>(out var r))
                {
                    r.GetPropertyBlock(mpb);
                    mpb.SetColor(ShaderIds.BaseColor, irisColor);
                    r.SetPropertyBlock(mpb);
                }
            }
        }

        void Update()
        {
            if (left == null && right == null) return;
            if (happyUntil > 0f && Time.time >= happyUntil) { happyUntil = 0f; Apply(); }

            nextBlink -= Time.deltaTime;
            if (nextBlink <= 0f) { blinkT = 0.14f; nextBlink = Random.Range(blinkInterval.x, blinkInterval.y); }
            if (blinkT > 0f) blinkT -= Time.deltaTime;
            float sy = blinkT > 0f && !sleepy ? 0.12f : 1f;
            foreach (var e in new[] { left, right })
            {
                if (e == null) continue;
                var s = e.root.localScale;
                s.y = Mathf.Lerp(s.y, sy * Mathf.Abs(s.x), 0.6f);
                e.root.localScale = s;
            }
        }
    }
}
