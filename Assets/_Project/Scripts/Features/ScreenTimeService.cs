using System;
using UnityEngine;
using UnityEngine.Events;

namespace HayalGaraji
{
    /// <summary>
    /// Ebeveynin belirlediği günlük süre dolunca "uyku vakti" başlar:
    /// araba esner, garaja park eder, kapı kapanır. Ek süre sadece ebeveyn kapısından verilir.
    /// Sayaç sadece uygulama açıkken ilerler ve her gün sıfırlanır.
    /// </summary>
    public class ScreenTimeService : MonoBehaviour
    {
        const string LimitKey = "hg.limitMinutes", UsedKey = "hg.usedSeconds", DayKey = "hg.day";

        public UnityEvent onBedtime;

        /// <summary>0 = sınırsız</summary>
        public static int LimitMinutes
        {
            get => PlayerPrefs.GetInt(LimitKey, 0);
            set { PlayerPrefs.SetInt(LimitKey, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public bool IsBedtime => LimitMinutes > 0 && used >= LimitMinutes * 60f;

        float used, saveTimer;
        bool fired;

        void Start() { RollDay(); used = PlayerPrefs.GetFloat(UsedKey, 0f); fired = IsBedtime; if (fired) onBedtime?.Invoke(); }

        void Update()
        {
            if (LimitMinutes <= 0 || fired) return;
            used += Time.unscaledDeltaTime;
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer > 15f) { saveTimer = 0f; Persist(); RollDay(); }
            if (IsBedtime) { fired = true; Persist(); onBedtime?.Invoke(); }
        }

        /// <summary>Ebeveyn kapısından sonra çağrılır.</summary>
        public void GrantExtraMinutes(int minutes)
        {
            used = Mathf.Max(0f, used - minutes * 60f);
            fired = false;
            Persist();
        }

        void RollDay()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            if (PlayerPrefs.GetString(DayKey, "") == today) return;
            PlayerPrefs.SetString(DayKey, today);
            PlayerPrefs.SetFloat(UsedKey, 0f);
            used = 0f; fired = false;
        }

        void Persist() { PlayerPrefs.SetFloat(UsedKey, used); PlayerPrefs.Save(); }

        void OnApplicationPause(bool paused)
        {
            if (paused) Persist();
            else { RollDay(); used = PlayerPrefs.GetFloat(UsedKey, 0f); }
        }
    }
}
