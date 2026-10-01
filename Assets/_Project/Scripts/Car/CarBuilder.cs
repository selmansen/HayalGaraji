using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Arabayı CarConfig'e göre kurar ve her değişikliği uygular.
    /// Diğer tüm sistemler arabayı bu sınıf üzerinden değiştirir.
    /// </summary>
    public class CarBuilder : MonoBehaviour
    {
        [SerializeField] PartCatalog catalog;
        [Tooltip("Aracın instantiate edildiği kök. CarReactions bunu zıplatır.")]
        [SerializeField] Transform carRoot;
        [SerializeField] float heightLerpSpeed = 5f;

        public PartCatalog Catalog => catalog;
        public CarConfig Config { get; private set; }
        public CarChassis Chassis { get; private set; }
        public CarDefinition Definition { get; private set; }
        public PaintableSurface Paint { get; private set; }
        public Transform CarRoot => carRoot;
        public VehicleFamily Family => Definition ? Definition.family : VehicleFamily.Land;
        public bool Hop { get; set; }

        /// <summary>Araç (şasi) baştan kurulduğunda.</summary>
        public event Action Rebuilt;
        /// <summary>Herhangi bir şey değiştiğinde (otomatik kayıt bunu dinler).</summary>
        public event Action Changed;

        readonly Dictionary<PartSlot, GameObject> spawned = new Dictionary<PartSlot, GameObject>();
        readonly List<Transform> wheels = new List<Transform>();
        Material bodyMat;
        float bodyBaseY, currentOffset, wheelSpinTime, wheelAngularSpeed;

        static readonly PartSlot[] AttachSlots =
            { PartSlot.Front, PartSlot.Top, PartSlot.Back, PartSlot.Side, PartSlot.Under, PartSlot.Exhaust, PartSlot.Buddy, PartSlot.Toy };

        // ---------- kurulum ----------

        public void LoadCar(string carId)
        {
            if (!catalog) { Debug.LogError("CarBuilder: 'Catalog' alanı boş. Systems nesnesindeki CarBuilder'a Catalog asset'ini atayın."); return; }
            var def = catalog.GetCar(carId);
            if (!def) { Debug.LogError($"Araç bulunamadı: {carId}"); return; }
            var cfg = def.defaults != null ? def.defaults.Clone() : new CarConfig();
            cfg.carId = carId;
            cfg.stickers.Clear();
            Build(cfg, true);
        }

        /// <summary>Konfigürasyonu uygular. Aynı araçsa şasi korunur (boya ve çıkartmalar kalır).</summary>
        public void Build(CarConfig cfg, bool clearPaint)
        {
            if (!catalog) { Debug.LogError("CarBuilder: 'Catalog' alanı boş."); return; }
            var def = catalog.GetCar(cfg.carId);
            if (!def) { Debug.LogError($"Araç bulunamadı: {cfg.carId}"); return; }
            Config = cfg;

            bool newChassis = Chassis == null || Definition != def;
            if (newChassis)
            {
                if (Chassis) Destroy(Chassis.gameObject);
                spawned.Clear();
                wheels.Clear();
                Definition = def;
                Chassis = Instantiate(def.chassisPrefab, carRoot, false).GetComponent<CarChassis>();
                bodyBaseY = Chassis.bodyRoot.localPosition.y;
                bodyMat = Chassis.bodyRenderer.material; // araca özel kopya (boya dokusu için)
                Paint = Chassis.bodyRenderer.GetComponent<PaintableSurface>();
                if (!Paint) Paint = Chassis.bodyRenderer.gameObject.AddComponent<PaintableSurface>();
                Paint.Init(bodyMat);
                currentOffset = TargetBodyY() - bodyBaseY;
            }
            else if (clearPaint) Paint.Clear();

            foreach (var slot in AttachSlots) ApplyPart(slot, cfg.GetPart(slot));
            BuildWheels();
            ApplyColors();
            if (newChassis) Rebuilt?.Invoke();
            Changed?.Invoke();
        }

        // ---------- değişiklikler ----------

        public void SetPart(PartSlot slot, string partId)
        {
            Config.SetPart(slot, partId);
            if (slot == PartSlot.Wheels) BuildWheels(); else ApplyPart(slot, partId);
            ApplyColors();
            Changed?.Invoke();
        }

        public void SetColor(TintChannel channel, Color c)
        {
            string hex = CarConfig.Hex(c);
            switch (channel)
            {
                case TintChannel.Body: Config.bodyColor = hex; if (Config.finish == Finish.Rainbow) Config.finish = Finish.Solid; break;
                case TintChannel.Roof: Config.roofColor = hex; break;
                case TintChannel.Rim: Config.rimColor = hex; break;
            }
            ApplyColors();
            Changed?.Invoke();
        }

        public void SetFinish(Finish f) { Config.finish = f; ApplyColors(); Changed?.Invoke(); }
        public void SetHeight(RideHeight h) { Config.height = h; Changed?.Invoke(); }
        public void SetWheelSize(WheelSize s) { Config.wheelSize = s; BuildWheels(); ApplyColors(); Changed?.Invoke(); }

        /// <summary>Motor çalışınca tekerlekleri bir süre döndürür.</summary>
        public void SpinWheels(float seconds, float degPerSec = 900f)
        {
            wheelSpinTime = seconds;
            wheelAngularSpeed = degPerSec;
        }

        public void NotifyChanged() => Changed?.Invoke();

        // ---------- iç işler ----------

        void ApplyPart(PartSlot slot, string partId)
        {
            if (spawned.TryGetValue(slot, out var old) && old) Destroy(old);
            spawned.Remove(slot);

            var part = catalog.GetPart(partId);
            if (part == null || part.prefab == null) return; // ör. korna: sadece ses

            Transform parent;
            switch (slot)
            {
                case PartSlot.Buddy: parent = Chassis.passengerSeat; break;
                case PartSlot.Toy: parent = Chassis.toyAnchor; break;
                default:
                    var socket = Chassis.GetSocket(slot);
                    parent = socket ? socket.transform : null;
                    break;
            }
            if (!parent) { Debug.LogWarning($"{Definition.id} aracında {slot} noktası yok."); return; }
            spawned[slot] = Instantiate(part.prefab, parent, false);
        }

        void BuildWheels()
        {
            foreach (var w in wheels) if (w) Destroy(w.gameObject);
            wheels.Clear();
            var part = catalog.GetPart(Config.GetPart(PartSlot.Wheels));
            if (part == null || part.prefab == null)
            {
                // Eski kayıttaki tekerlek artık yoksa: aracın varsayılanı, o da yoksa ilk uygun tekerlek
                part = catalog.GetPart(Definition.defaults != null ? Definition.defaults.GetPart(PartSlot.Wheels) : null)
                       ?? catalog.PartsFor(PartSlot.Wheels, Family).FirstOrDefault();
                if (part == null || part.prefab == null) return;
                Config.SetPart(PartSlot.Wheels, part.id);
            }

            float s = WheelScale();
            foreach (var anchor in Chassis.wheelAnchors)
            {
                var w = Instantiate(part.prefab, anchor, false).transform;
                w.localScale = Vector3.one * s;
                // Büyük tekerlek yere bassın diye merkezi yukarı kaldır
                w.localPosition = new Vector3(0f, Chassis.wheelRadius * (s - 1f), 0f);
                wheels.Add(w);
            }
        }

        void ApplyColors()
        {
            var body = CarConfig.Col(Config.bodyColor);
            var roof = CarConfig.Col(Config.roofColor);
            var rim = CarConfig.Col(Config.rimColor);
            var neon = Config.finish == Finish.Neon ? body * 0.45f : Color.black;

            bodyMat.SetColor(ShaderIds.BaseColor, body);
            bodyMat.SetColor(ShaderIds.SecondColor, roof);
            bodyMat.SetFloat(ShaderIds.Finish, (float)Config.finish);
            bodyMat.SetColor(ShaderIds.Emission, neon);

            foreach (var t in carRoot.GetComponentsInChildren<TintTarget>(true))
            {
                switch (t.channel)
                {
                    case TintChannel.Body: t.Apply(body, neon); break;
                    case TintChannel.Roof: t.Apply(roof, Color.black); break;
                    case TintChannel.Rim: t.Apply(rim, Color.black); break;
                }
            }
        }

        float WheelScale()
        {
            switch (Config.wheelSize)
            {
                case WheelSize.Small: return 0.85f;
                case WheelSize.Big: return 1.28f;
                default: return 1f;
            }
        }

        float HeightOffset()
        {
            switch (Config.height)
            {
                case RideHeight.Low: return -0.1f;
                case RideHeight.High: return 0.3f;
                case RideHeight.Giant: return 0.65f;
                default: return 0f;
            }
        }

        float TargetBodyY() => bodyBaseY + Chassis.wheelRadius * (WheelScale() - 1f) + HeightOffset();

        void Update()
        {
            if (!Chassis) return;

            // Yükseklik (yumuşak geçiş + zıplama modu)
            float target = TargetBodyY() - bodyBaseY;
            if (Hop) target += Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.28f;
            currentOffset = Mathf.Lerp(currentOffset, target, Time.deltaTime * (Hop ? 18f : heightLerpSpeed));
            var p = Chassis.bodyRoot.localPosition;
            p.y = Mathf.Max(0.03f, bodyBaseY + currentOffset);
            Chassis.bodyRoot.localPosition = p;
            Chassis.bodyRoot.localRotation = Hop ? Quaternion.Euler(Mathf.Sin(Time.time * 5f) * 4f, 0f, 0f) : Quaternion.identity;

            // Süspansiyon: akslar tekerlek merkezinde, yaylar tekerlek merkezinden gövdenin altına uzanır
            float ws = WheelScale();
            float wheelCenterY = Chassis.wheelRadius * ws;
            float bodyBottomY = Chassis.bodyRoot.localPosition.y + 0.12f;
            float strutLen = bodyBottomY - wheelCenterY;
            if (Chassis.struts != null)
                foreach (var st in Chassis.struts)
                {
                    if (!st) continue;
                    bool show = strutLen > 0.02f;
                    if (st.gameObject.activeSelf != show) st.gameObject.SetActive(show);
                    if (!show) continue;
                    var lp = st.localPosition; lp.y = Chassis.wheelRadius * (ws - 1f); st.localPosition = lp;
                    st.localScale = new Vector3(1f, strutLen, 1f);
                }
            if (Chassis.axles != null)
                foreach (var ax in Chassis.axles)
                {
                    if (!ax) continue;
                    var lp = ax.localPosition; lp.y = wheelCenterY; ax.localPosition = lp;
                }

            // Tekerlek dönüşü (dünya ekseninde; sol/sağ yön farkı olmaz)
            if (wheelSpinTime > 0f)
            {
                wheelSpinTime -= Time.deltaTime;
                float ang = wheelAngularSpeed * Time.deltaTime;
                foreach (var w in wheels) if (w) w.Rotate(Chassis.transform.right, ang, Space.World);
            }
        }
    }
}
