namespace HayalGaraji
{
    /// <summary>Bir parçanın araca takıldığı yer.</summary>
    /// <summary>Araç ailesi: sahneyi, boşta hareketini ve takılabilecek parçaları belirler.</summary>
    public enum VehicleFamily { Land = 0, Air = 1, Sea = 2 }

    /// <summary>Bir parçanın takılabileceği aileler.</summary>
    [System.Flags] public enum FamilyMask { Land = 1, Air = 2, Sea = 4, All = 7 }

    public enum PartSlot { Wheels, Front, Top, Back, Side, Under, Exhaust, Horn, Buddy, Toy }

    /// <summary>Gövde kaplaması. Değerler shader'daki _Finish ile birebir eşleşir.</summary>
    public enum Finish { Solid = 0, Rainbow = 1, TwoTone = 2, Neon = 3 }

    public enum WheelSize { Small, Normal, Big }
    public enum RideHeight { Low, Normal, High, Giant }

    /// <summary>Sol kenardaki sabit kategori düğmeleri (sıra = raydaki sıra).</summary>
    /// <summary>Sol menüdeki kategoriler; değer = menüdeki sıra.</summary>
    public enum Category { Paint = 0, Stickers = 1, Wheels = 2, Face = 3, Accessories = 4, Exhaust = 5, Buddy = 6, Sound = 7 }

    /// <summary>
    /// Parmağın araba üzerinde ne yaptığı.
    /// Paint: dokun = dokunulan parçayı boya, sürükle = sprey. Wash: sürükle = yıka.
    /// </summary>
    public enum ToolMode { Look, Paint, Sticker, Wash }

    /// <summary>Boya modunda dokunulan parça.</summary>
    public enum PaintPart { Body, Roof, Rim, Eyes }

    /// <summary>Boya tepsisindeki özel seçimler.</summary>
    public enum PaintSpecial { None, Rainbow, Glitter, Sponge }

    public enum TintChannel { Body, Roof, Rim }
    public enum BrushStyle { Thin, Thick, Rainbow, Glitter }
    public enum LearnMode { NativeOnly, Both, LearningOnly }

    public static class ShaderIds
    {
        public static readonly int BaseColor = UnityEngine.Shader.PropertyToID("_BaseColor");
        public static readonly int SecondColor = UnityEngine.Shader.PropertyToID("_SecondColor");
        public static readonly int Finish = UnityEngine.Shader.PropertyToID("_Finish");
        public static readonly int Emission = UnityEngine.Shader.PropertyToID("_Emission");
        public static readonly int PaintTex = UnityEngine.Shader.PropertyToID("_PaintTex");
    }
}
