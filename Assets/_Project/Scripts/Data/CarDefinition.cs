using UnityEngine;

namespace HayalGaraji
{
    [CreateAssetMenu(menuName = "Hayal Garajı/Araç", fileName = "Car_")]
    public class CarDefinition : ScriptableObject
    {
        public string id;
        [Tooltip("Kökünde CarChassis bileşeni olan prefab.")]
        public GameObject chassisPrefab;
        public Sprite thumbnail;
        public string wordKey;
        public bool premium;
        [Tooltip("Kara / Hava / Deniz. Askeri ve yarış araçları da Kara ailesindendir.")]
        public VehicleFamily family = VehicleFamily.Land;
        public float cameraDistance = 5f;
        public float cameraTargetHeight = 0.6f;
        [Tooltip("Araç ilk seçildiğinde gelen hali.")]
        public CarConfig defaults = new CarConfig();
    }
}
