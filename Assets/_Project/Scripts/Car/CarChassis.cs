using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Her araç prefab'ının kökünde durur ve parçaların nereye takılacağını tarif eder.
    /// Eksen kuralı: ileri +Z, yukarı +Y, tekerlek aksı X. Kök, zemin seviyesindedir (y = 0).
    /// </summary>
    public class CarChassis : MonoBehaviour
    {
        [Header("Gövde")]
        [Tooltip("Yükseklik ayarıyla aşağı-yukarı hareket eden kısım. Tekerlekler bunun DIŞINDA olmalı.")]
        public Transform bodyRoot;
        [Tooltip("Boyanabilir ana gövde. Üzerinde MeshCollider ve PaintableSurface olmalı; mesh'te tekil UV açılımı gerekir.")]
        public Renderer bodyRenderer;

        [Header("Tekerlekler")]
        [Tooltip("Tekerlek merkezleri; kökün doğrudan çocukları olmalı.")]
        public Transform[] wheelAnchors;
        public float wheelRadius = 0.3f;
        [Tooltip("Yaylı amortisörler (tekerlek yuvalarının çocukları, taban noktası altta). Gövde yükseldikçe uzarlar.")]
        public Transform[] struts;
        [Tooltip("Sağ-sol tekerlekleri bağlayan akslar (kökün çocukları).")]
        public Transform[] axles;

        [Header("Bağlantı noktaları")]
        [Tooltip("İçinde EyeL ve EyeR adlı iki çocuk olmalı.")]
        public Transform faceAnchor;
        [Tooltip("İçeri girince kameranın oturduğu nokta (ileri +Z bakar).")]
        public Transform driverView;
        public Transform passengerSeat;
        public Transform toyAnchor;

        public CarSocket GetSocket(PartSlot slot)
        {
            foreach (var s in GetComponentsInChildren<CarSocket>(true))
                if (s.slot == slot) return s;
            return null;
        }
    }
}
