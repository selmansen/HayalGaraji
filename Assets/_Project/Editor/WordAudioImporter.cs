using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization.Tables;

namespace HayalGaraji.EditorTools
{
    /// <summary>
    /// Menü: Hayal Garajı → Kelime Seslerini Tabloya Aktar
    /// Assets/_Project/Audio/Words/{dil}/{anahtar}.wav|mp3|ogg dosyalarını bulur ve
    /// "WordAudio" asset tablosuna dil dil yerleştirir. Yeni dil eklemek = klasör eklemek.
    /// </summary>
    public static class WordAudioImporter
    {
        const string Root = "Assets/_Project/Audio/Words";
        const string TableName = "WordAudio";

        [MenuItem("Hayal Garajı/Kelime Seslerini Tabloya Aktar")]
        public static void Import()
        {
            var collection = LocalizationEditorSettings.GetAssetTableCollection(TableName);
            if (collection == null)
            {
                EditorUtility.DisplayDialog("Hayal Garajı", $"Önce '{TableName}' adlı bir Asset Table Collection oluşturun (README, adım 5).", "Tamam");
                return;
            }
            if (!Directory.Exists(Root)) { Debug.LogWarning($"{Root} bulunamadı."); return; }

            int count = 0;
            foreach (var localeDir in Directory.GetDirectories(Root))
            {
                string code = Path.GetFileName(localeDir);
                var table = collection.GetTable(code) as AssetTable;
                if (table == null) { Debug.LogWarning($"'{code}' dili tabloda yok, atlandı."); continue; }

                foreach (var file in Directory.GetFiles(localeDir))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".wav" && ext != ".mp3" && ext != ".ogg") continue;
                    string key = Path.GetFileNameWithoutExtension(file);
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file.Replace('\\', '/'));
                    if (clip == null) continue;
                    if (!collection.SharedData.Contains(key)) collection.SharedData.AddKey(key);
                    collection.AddAssetToTable(table, key, clip);
                    count++;
                }
            }
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();
            Debug.Log($"Hayal Garajı: {count} ses dosyası '{TableName}' tablosuna aktarıldı.");
        }
    }
}
