using System;

namespace HayalGaraji
{
    /// <summary>
    /// Abonelik durumu. Şimdilik elle ayarlanıyor; Unity IAP entegrasyonu
    /// (ebeveyn alanında) satın alma doğrulandığında Set(true) çağıracak.
    /// </summary>
    public static class Entitlements
    {
        public static bool IsPremium { get; private set; }
        public static event Action Changed;

        public static void Set(bool premium)
        {
            if (IsPremium == premium) return;
            IsPremium = premium;
            Changed?.Invoke();
        }
    }
}
