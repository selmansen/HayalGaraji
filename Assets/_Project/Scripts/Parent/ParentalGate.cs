using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>
    /// Ebeveyn kapısı: ayarlar, satın alma, silme ve ek süre bunun arkasındadır.
    /// Çarpma sorusu evrenseldir (dil gerektirmez) ve okul öncesi çocuğun çözemeyeceği düzeydedir.
    /// </summary>
    public class ParentalGate : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text question;
        [SerializeField] Button[] answerButtons;
        [SerializeField] TMP_Text[] answerLabels;

        Action onPassed;

        public void Open(Action passed)
        {
            onPassed = passed;
            int a = UnityEngine.Random.Range(4, 10), b = UnityEngine.Random.Range(4, 10), correct = a * b;
            var options = new List<int> { correct };
            while (options.Count < answerButtons.Length)
            {
                int wrong = correct + UnityEngine.Random.Range(-9, 10);
                if (wrong > 0 && !options.Contains(wrong)) options.Add(wrong);
            }
            for (int i = options.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); (options[i], options[j]) = (options[j], options[i]); }

            question.text = $"{a} × {b} = ?";
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int value = options[i];
                answerLabels[i].text = value.ToString();
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => Answer(value == correct));
            }
            panel.SetActive(true);
        }

        public void Close() => panel.SetActive(false);

        void Answer(bool ok)
        {
            panel.SetActive(false);
            if (ok) onPassed?.Invoke();
        }
    }
}
