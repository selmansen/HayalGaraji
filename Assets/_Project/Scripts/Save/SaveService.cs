using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Her şey cihazda saklanır (hesap yok, sunucu yok):
    ///   garage/current/      → üzerinde çalışılan araba (otomatik kayıt)
    ///   garage/&lt;id&gt;/  → koleksiyondaki arabalar (config.json, paint.png, photo.jpg)
    /// </summary>
    public static class SaveService
    {
        [Serializable] class Meta { public long time; }

        public class Entry
        {
            public string id;
            public long time;
            public string photoPath;
        }

        static string Root => Path.Combine(Application.persistentDataPath, "garage");
        static string Dir(string id) => Path.Combine(Root, id);

        public static void SaveCurrent(CarConfig cfg, byte[] paintPng) => Write("current", cfg, paintPng, null);
        public static bool LoadCurrent(out CarConfig cfg, out byte[] paintPng) => Read("current", out cfg, out paintPng);

        public static string AddToGarage(CarConfig cfg, byte[] paintPng, byte[] photoJpg)
        {
            string id = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            Write(id, cfg, paintPng, photoJpg);
            return id;
        }

        public static bool LoadFromGarage(string id, out CarConfig cfg, out byte[] paintPng) => Read(id, out cfg, out paintPng);

        public static List<Entry> ListGarage()
        {
            var list = new List<Entry>();
            if (!Directory.Exists(Root)) return list;
            foreach (var dir in Directory.GetDirectories(Root))
            {
                string id = Path.GetFileName(dir);
                if (id == "current") continue;
                long time = 0;
                try { time = JsonUtility.FromJson<Meta>(File.ReadAllText(Path.Combine(dir, "meta.json"))).time; } catch { }
                list.Add(new Entry { id = id, time = time, photoPath = Path.Combine(dir, "photo.jpg") });
            }
            list.Sort((a, b) => b.time.CompareTo(a.time));
            return list;
        }

        public static void Delete(string id)
        {
            if (id == "current") return;
            try { if (Directory.Exists(Dir(id))) Directory.Delete(Dir(id), true); }
            catch (Exception ex) { Debug.LogWarning("Silinemedi: " + ex.Message); }
        }

        static void Write(string id, CarConfig cfg, byte[] paint, byte[] photo)
        {
            try
            {
                var d = Dir(id);
                Directory.CreateDirectory(d);
                File.WriteAllText(Path.Combine(d, "config.json"), JsonUtility.ToJson(cfg));
                File.WriteAllText(Path.Combine(d, "meta.json"), JsonUtility.ToJson(new Meta { time = DateTime.UtcNow.Ticks }));
                if (paint != null) File.WriteAllBytes(Path.Combine(d, "paint.png"), paint);
                if (photo != null) File.WriteAllBytes(Path.Combine(d, "photo.jpg"), photo);
            }
            catch (Exception ex) { Debug.LogWarning("Kaydedilemedi: " + ex.Message); }
        }

        static bool Read(string id, out CarConfig cfg, out byte[] paint)
        {
            cfg = null; paint = null;
            try
            {
                var d = Dir(id);
                var cfgPath = Path.Combine(d, "config.json");
                if (!File.Exists(cfgPath)) return false;
                cfg = JsonUtility.FromJson<CarConfig>(File.ReadAllText(cfgPath));
                var paintPath = Path.Combine(d, "paint.png");
                if (File.Exists(paintPath)) paint = File.ReadAllBytes(paintPath);
                return cfg != null;
            }
            catch (Exception ex) { Debug.LogWarning("Okunamadı: " + ex.Message); return false; }
        }
    }
}
