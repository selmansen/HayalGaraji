using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Oyundaki tüm içeriğin tek listesi. Abonelik yoksa premium içerik
    /// buradan hiç dönmez; çocuk kilitli bir şey görmez.
    /// </summary>
    [CreateAssetMenu(menuName = "Hayal Garajı/Katalog", fileName = "Catalog")]
    public class PartCatalog : ScriptableObject
    {
        public List<CarDefinition> cars = new List<CarDefinition>();
        public List<PartDefinition> parts = new List<PartDefinition>();
        public List<PaletteColor> colors = new List<PaletteColor>();
        public List<StickerDefinition> stickers = new List<StickerDefinition>();
        public List<EyeStyle> eyeStyles = new List<EyeStyle>();

        public CarDefinition GetCar(string id) => cars.Find(c => c && c.id == id);
        public PartDefinition GetPart(string id) => string.IsNullOrEmpty(id) ? null : parts.Find(p => p && p.id == id);
        public StickerDefinition GetSticker(string id) => stickers.Find(s => s.id == id);
        public EyeStyle GetEyes(string id) => eyeStyles.Find(e => e.id == id) ?? (eyeStyles.Count > 0 ? eyeStyles[0] : null);

        public IEnumerable<CarDefinition> Cars() =>
            cars.Where(c => c && (Entitlements.IsPremium || !c.premium));

        public IEnumerable<PartDefinition> PartsFor(PartSlot slot) =>
            parts.Where(p => p && p.slot == slot && (Entitlements.IsPremium || !p.premium));

        /// <summary>Sadece bu araç ailesine uyan parçalar (tekneye kar küreği önerilmez).</summary>
        public IEnumerable<PartDefinition> PartsFor(PartSlot slot, VehicleFamily family)
        {
            var bit = (FamilyMask)(1 << (int)family);
            return PartsFor(slot).Where(p => (p.families & bit) != 0);
        }

        public IEnumerable<StickerDefinition> Stickers() =>
            stickers.Where(s => Entitlements.IsPremium || !s.premium);
    }
}
