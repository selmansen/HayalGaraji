using System;
using System.Collections.Generic;
using UnityEngine;

namespace HayalGaraji
{
    [Serializable] public class SlotPart { public PartSlot slot; public string partId; }

    [Serializable]
    public class StickerData
    {
        public string stickerId;
        public Vector3 localPos;
        public Vector3 localNormal;
        public float size;
        public float roll;
    }

    /// <summary>
    /// Bir arabanın tüm hali. JSON olarak kaydedilir; boya dokusu ayrı PNG dosyasında durur.
    /// JsonUtility sözlük desteklemediği için parçalar liste olarak tutulur.
    /// </summary>
    [Serializable]
    public class CarConfig
    {
        public string carId;
        public string bodyColor = "#FF4F7B";
        public string roofColor = "#2B2B3A";
        public string rimColor = "#FFD23F";
        public string eyeColor = "#4AA8FF";
        public Finish finish = Finish.Solid;
        public WheelSize wheelSize = WheelSize.Normal;
        public RideHeight height = RideHeight.Normal;
        public string eyeStyle = "normal";
        public List<SlotPart> parts = new List<SlotPart>();
        public List<StickerData> stickers = new List<StickerData>();

        public string GetPart(PartSlot s)
        {
            var p = parts.Find(x => x.slot == s);
            return p != null ? p.partId : null;
        }

        public void SetPart(PartSlot s, string id)
        {
            parts.RemoveAll(x => x.slot == s);
            if (!string.IsNullOrEmpty(id)) parts.Add(new SlotPart { slot = s, partId = id });
        }

        public CarConfig Clone() => JsonUtility.FromJson<CarConfig>(JsonUtility.ToJson(this));

        public static Color Col(string hex) =>
            ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }
}
