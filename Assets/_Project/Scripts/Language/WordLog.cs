using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Ebeveyn raporu için hangi kelimenin kaç kez duyulduğunu tutar.
    /// Sadece cihazda saklanır, hiçbir yere gönderilmez.
    /// </summary>
    public static class WordLog
    {
        [Serializable] class Entry { public string key; public int count; }
        [Serializable] class Data { public List<Entry> words = new List<Entry>(); }

        static Data data;
        static float lastSave;
        static string FilePath => Path.Combine(Application.persistentDataPath, "wordlog.json");

        public static void Record(string key)
        {
            // Pofu'nun cümleleri ve arayüz sesleri kelime raporuna girmez
            if (key.StartsWith("pofu_") || key.StartsWith("ui_") || key.StartsWith("praise_") || key.StartsWith("prompt_") || key.StartsWith("cat_")) return;
            Load();
            var e = data.words.Find(w => w.key == key);
            if (e == null) data.words.Add(e = new Entry { key = key });
            e.count++;
            if (Time.realtimeSinceStartup - lastSave > 10f) Save();
        }

        public static IReadOnlyList<(string key, int count)> All()
        {
            Load();
            var list = new List<(string, int)>();
            foreach (var w in data.words) list.Add((w.key, w.count));
            list.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return list;
        }

        public static void Save()
        {
            if (data == null) return;
            lastSave = Time.realtimeSinceStartup;
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(data)); }
            catch (Exception ex) { Debug.LogWarning("WordLog kaydedilemedi: " + ex.Message); }
        }

        static void Load()
        {
            if (data != null) return;
            try { data = File.Exists(FilePath) ? JsonUtility.FromJson<Data>(File.ReadAllText(FilePath)) : new Data(); }
            catch { data = new Data(); }
            if (data == null) data = new Data();
        }
    }
}
