using UnityEngine;

namespace HayalGaraji
{
    /// <summary>Takılabilir her şey: tekerlek, kedi kulağı, roket, şoför hayvan, korna…</summary>
    [CreateAssetMenu(menuName = "Hayal Garajı/Parça", fileName = "Part_")]
    public class PartDefinition : ScriptableObject
    {
        public string id;
        public PartSlot slot;
        [Tooltip("Korna gibi sadece sesi olan parçalarda boş bırakılabilir.")]
        public GameObject prefab;
        [Tooltip("Tepsideki sabit ikon. Editör menüsündeki Küçük Resim Üretici doldurur.")]
        public Sprite thumbnail;
        [Tooltip("vocabulary.csv içindeki anahtar, ör. animal_cat")]
        public string wordKey;
        public bool premium;
        [Tooltip("Bu parçanın takılabileceği araç aileleri")]
        public FamilyMask families = FamilyMask.All;
        [Tooltip("Korna sesi, egzoz sesi vb.")]
        public AudioClip sound;
    }
}
