using System.Collections.Generic;
using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Kodla üretilen sesler (prototipteki gibi): motor, egzoz, korna, dokunma, kıkırdama, fotoğraf, sürpriz.
    /// Ses dosyası atanmamışsa sistemler bunları kullanır; gerçek ses dosyaları geldikçe onların yerine geçer.
    /// Her ses ilk kullanımda bir kez üretilip saklanır.
    /// </summary>
    public static class SynthSounds
    {
        const int SR = 44100;
        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        static System.Random rnd = new System.Random(11);

        // ---------------- kullanıma hazır sesler ----------------
        public static AudioClip Pop => Get("pop", () => Chirp(0.12f, 520f, 880f, Wave.Sine, 0.5f));
        public static AudioClip Boing => Get("boing", () => Mix(Chirp(0.18f, 180f, 640f, Wave.Sine, 0.6f), Delay(Chirp(0.25f, 640f, 260f, Wave.Sine, 0.45f), 0.16f)));
        public static AudioClip Thud => Get("thud", () => Chirp(0.18f, 130f, 55f, Wave.Sine, 0.7f));
        public static AudioClip Giggle => Get("giggle", () =>
        {
            float[] mix = null;
            for (int i = 0; i < 5; i++) mix = Mix(mix, Delay(Chirp(0.07f, 920f - i * 60f, 760f - i * 60f, Wave.Triangle, 0.35f), i * 0.09f));
            return mix;
        });
        public static AudioClip Shutter => Get("shutter", () => Mix(Noise(0.09f, 3000f, 0.5f, true), Delay(Chirp(0.05f, 1500f, 900f, Wave.Square, 0.12f), 0.07f)));
        public static AudioClip Stick => Get("stick", () => Mix(Chirp(0.08f, 300f, 120f, Wave.Square, 0.2f), Noise(0.06f, 1200f, 0.3f, false)));
        public static AudioClip Sparkle => Get("sparkle", () => Arp(new[] { 1318f, 1568f, 1760f, 2093f }, 0.07f, 0.18f, Wave.Triangle, 0.25f));
        public static AudioClip Drumroll => Get("drumroll", () =>
        {
            float[] mix = null;
            for (int i = 0; i < 14; i++) mix = Mix(mix, Delay(Noise(0.05f, 1800f, 0.25f + i * 0.02f, false), i * 0.06f));
            return mix;
        });
        public static AudioClip Fanfare => Get("fanfare", () => Trumpet(new[] { 523f, 659f, 784f, 1047f }, new[] { 0.1f, 0.1f, 0.1f, 0.45f }));
        public static AudioClip SprayLoop => Get("spray", () => Noise(1f, 4200f, 0.18f, false, loop: true));
        public static AudioClip WashLoop => Get("wash", () =>
        {
            float[] mix = Noise(1f, 900f, 0.1f, false, loop: true);
            for (int i = 0; i < 8; i++) { float f = 380f + (float)rnd.NextDouble() * 400f; mix = Mix(mix, Delay(Chirp(0.07f, f, f * 1.8f, Wave.Sine, 0.25f), i * 0.12f)); }
            return mix;
        });

        /// <summary>Tek nota (gösterge düğmeleri için).</summary>
        public static AudioClip Note(float hz) => Get("note_" + Mathf.RoundToInt(hz), () => Mix(Chirp(0.35f, hz, hz * 1.002f, Wave.Triangle, 0.35f), Chirp(0.25f, hz * 2f, hz * 2.004f, Wave.Sine, 0.08f)));

        /// <summary>Egzoz türüne göre motor sesi.</summary>
        public static AudioClip ForExhaust(string id)
        {
            switch (id)
            {
                case "double": return Get("ex_double", () => Engine(46f, 125f, 15f, Wave.Saw, 12f, 1000f, 0.55f));
                case "stacks": return Get("ex_stacks", () => Engine(34f, 95f, 11f, Wave.Square, 9f, 520f, 0.6f));
                case "trumpet": return Get("ex_trumpet", () => Trumpet(new[] { 392f, 523f, 659f, 784f }, new[] { 0.14f, 0.14f, 0.14f, 0.5f }));
                case "bubble": return Get("ex_bubble", () =>
                {
                    float[] mix = null;
                    for (int i = 0; i < 11; i++) { float f = 300f + (float)rnd.NextDouble() * 400f; mix = Mix(mix, Delay(Chirp(0.09f, f, f * 1.9f, Wave.Sine, 0.4f), i * 0.12f + (float)rnd.NextDouble() * 0.04f)); }
                    return mix;
                });
                case "rainbow": return Get("ex_rainbow", () => Mix(Arp(new[] { 523f, 659f, 784f, 1047f, 784f, 659f, 523f, 1047f }, 0.12f, 0.2f, Wave.Triangle, 0.3f), Noise(1.2f, 300f, 0.1f, false)));
                case "confetti": return Get("ex_confetti", () => Mix(Noise(0.18f, 900f, 0.8f, false), Delay(Arp(new[] { 1047f, 1319f, 1568f, 2093f }, 0.06f, 0.2f, Wave.Triangle, 0.3f), 0.12f)));
                case "electric": return Electric;
                default: return Get("ex_single", () => Engine(55f, 150f, 22f, Wave.Saw, 6f, 800f, 0.5f));
            }
        }

        public static AudioClip Electric => Get("electric", () =>
        {
            int n = (int)(1.4f * SR); var d = new float[n]; float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, k = t / 1.4f;
                float f = k < 0.55f ? Mathf.Lerp(260f, 1100f, k / 0.55f) : Mathf.Lerp(1100f, 420f, (k - 0.55f) / 0.45f);
                f += Mathf.Sin(t * 30f) * 8f;
                ph += f / SR; d[i] = Mathf.Sin(ph * 2f * Mathf.PI) * 0.22f * Env(t, 1.4f, 0.06f, 0.35f);
            }
            return d;
        });

        /// <summary>Korna sesi (hayvan kornaları).</summary>
        public static AudioClip ForHorn(string id)
        {
            if (id != null && id.Contains("duck")) return Get("horn_duck", () =>
            {
                float[] mix = null;
                for (int q = 0; q < 2; q++)
                {
                    int n = (int)(0.16f * SR); var d = new float[n]; float ph = 0f;
                    for (int i = 0; i < n; i++)
                    {
                        float t = (float)i / SR, f = Mathf.Lerp(470f, 380f, t / 0.16f);
                        ph += f / SR;
                        float sq = Mathf.Repeat(ph, 1f) < 0.3f ? 1f : -1f;             // genizden "vak"
                        d[i] = sq * (0.5f + 0.5f * Mathf.Sin(t * 2f * Mathf.PI * 32f)) * 0.22f * Env(t, 0.16f, 0.01f, 0.06f);
                    }
                    mix = Mix(mix, Delay(LowPass(d, 1800f), q * 0.2f));
                }
                return mix;
            });
            if (id != null && id.Contains("cow")) return Get("horn_cow", () =>
            {
                int n = (int)(1.0f * SR); var d = new float[n]; float ph = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR, f = Mathf.Lerp(150f, 112f, t) + Mathf.Sin(t * 2f * Mathf.PI * 5f) * 4f;
                    ph += f / SR;
                    d[i] = (Mathf.Repeat(ph, 1f) * 2f - 1f) * 0.35f * Env(t, 1.0f, 0.12f, 0.3f);   // "möö"
                }
                return LowPass(d, 700f);
            });
            return Get("horn_default", () => Mix(Tone(0.25f, 392f, Wave.Square, 0.18f), Delay(Tone(0.28f, 494f, Wave.Square, 0.16f), 0.3f)));
        }

        // ---------------- üreticiler ----------------
        enum Wave { Sine, Square, Saw, Triangle }

        static float Osc(Wave w, float ph)
        {
            float p = Mathf.Repeat(ph, 1f);
            switch (w)
            {
                case Wave.Square: return p < 0.5f ? 1f : -1f;
                case Wave.Saw: return p * 2f - 1f;
                case Wave.Triangle: return 1f - 4f * Mathf.Abs(p - 0.5f);
                default: return Mathf.Sin(p * 2f * Mathf.PI);
            }
        }

        static float Env(float t, float dur, float attack, float release)
        {
            if (t < attack) return t / attack;
            if (t > dur - release) return Mathf.Clamp01((dur - t) / release);
            return 1f;
        }

        static float[] Chirp(float dur, float f0, float f1, Wave w, float vol)
        {
            int n = (int)(dur * SR); var d = new float[n]; float ph = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR, k = t / dur;
                float f = f0 * Mathf.Pow(f1 / f0, k);
                ph += f / SR; d[i] = Osc(w, ph) * vol * Mathf.Pow(1f - k, 1.5f) * Mathf.Clamp01(t / 0.01f);
            }
            return d;
        }

        static float[] Tone(float dur, float f, Wave w, float vol)
        {
            int n = (int)(dur * SR); var d = new float[n];
            for (int i = 0; i < n; i++) { float t = (float)i / SR; d[i] = Osc(w, f * t) * vol * Env(t, dur, 0.01f, 0.05f); }
            return d;
        }

        static float[] Arp(float[] notes, float step, float noteDur, Wave w, float vol)
        {
            float[] mix = null;
            for (int i = 0; i < notes.Length; i++) mix = Mix(mix, Delay(Chirp(noteDur, notes[i], notes[i] * 1.001f, w, vol), i * step));
            return mix;
        }

        static float[] Trumpet(float[] notes, float[] lens)
        {
            float[] mix = null; float at = 0f;
            for (int k = 0; k < notes.Length; k++)
            {
                int n = (int)(lens[k] * SR); var d = new float[n]; float ph = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = (float)i / SR;
                    ph += (notes[k] + Mathf.Sin(t * 2f * Mathf.PI * 6f) * 7f) / SR;
                    d[i] = Osc(Wave.Square, ph) * 0.2f * Env(t, lens[k], 0.02f, 0.06f);
                }
                mix = Mix(mix, Delay(LowPass(d, 2400f), at));
                at += lens[k] * 0.9f;
            }
            return mix;
        }

        /// <summary>Motor: devir yükselip iner, gövde "gurul gurul" titrer.</summary>
        static float[] Engine(float baseF, float peakF, float lfo, Wave w, float detune, float cutoff, float vol)
        {
            const float dur = 1.6f;
            int n = (int)(dur * SR); var d = new float[n]; float p1 = 0f, p2 = 0f, pl = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                float f = t < 0.45f ? Mathf.Lerp(baseF, peakF, t / 0.45f)
                        : t < 0.8f ? Mathf.Lerp(peakF, peakF * 0.8f, (t - 0.45f) / 0.35f)
                        : Mathf.Lerp(peakF * 0.8f, baseF, (t - 0.8f) / 0.8f);
                float rpm = (f - baseF) / (peakF - baseF);
                p1 += f / SR; p2 += f * (1f + detune * 0.0006f) / SR; pl += lfo * (1f + rpm * 1.6f) / SR;
                float amp = 0.6f + 0.4f * (Mathf.Repeat(pl, 1f) < 0.5f ? 1f : -1f);
                d[i] = (Osc(w, p1) + Osc(w, p2)) * 0.5f * amp * vol * Env(t, dur, 0.05f, 0.35f);
            }
            return Mix(LowPass(d, cutoff), Noise(0.15f, 400f, 0.5f, false));
        }

        static float[] Noise(float dur, float freq, float vol, bool highPass, bool loop = false)
        {
            int n = (int)(dur * SR); var d = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SR;
                d[i] = ((float)rnd.NextDouble() * 2f - 1f) * vol * (loop ? 1f : Mathf.Pow(1f - t / dur, 2f));
            }
            var lp = LowPass(d, freq);
            if (highPass) for (int i = 0; i < n; i++) lp[i] = d[i] - lp[i];
            return lp;
        }

        static float[] LowPass(float[] x, float cutoff)
        {
            var y = new float[x.Length];
            float a = 1f - Mathf.Exp(-2f * Mathf.PI * cutoff / SR), s = 0f;
            for (int i = 0; i < x.Length; i++) { s += a * (x[i] - s); y[i] = s; }
            return y;
        }

        static float[] Delay(float[] x, float seconds)
        {
            int off = (int)(seconds * SR);
            var y = new float[x.Length + off];
            System.Array.Copy(x, 0, y, off, x.Length);
            return y;
        }

        static float[] Mix(float[] a, float[] b)
        {
            if (a == null) return b;
            if (b == null) return a;
            var y = new float[Mathf.Max(a.Length, b.Length)];
            for (int i = 0; i < a.Length; i++) y[i] += a[i];
            for (int i = 0; i < b.Length; i++) y[i] += b[i];
            return y;
        }

        static AudioClip Get(string key, System.Func<float[]> make)
        {
            if (cache.TryGetValue(key, out var c) && c) return c;
            var data = make();
            float peak = 0f; foreach (var v in data) peak = Mathf.Max(peak, Mathf.Abs(v));
            if (peak > 0.95f) for (int i = 0; i < data.Length; i++) data[i] *= 0.95f / peak;
            c = AudioClip.Create(key, Mathf.Max(1, data.Length), 1, SR, false);
            c.SetData(data, 0);
            cache[key] = c;
            return c;
        }
    }
}
