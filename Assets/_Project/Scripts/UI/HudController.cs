using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HayalGaraji
{
    /// <summary>
    /// Arayüzün beyni. Temel ilke: her kategori tek tür seçim içerir; tepside hep aynı tip öğeler olur.
    /// Ayarlar (yükseklik, tekerlek boyu, içeri gir) tepsiye karışmaz; arabanın yanındaki ayar kulesindedir.
    ///   Boya      : renkler + gökkuşağı, simli, sünger → arabaya dokun = o parçayı boya, sürükle = sprey
    ///   Çıkartma  : çıkartmalar + sünger (hepsini temizle)
    ///   Tekerlek  : jantlar            | kule: ⬆⬇ yükseklik, ➕➖ tekerlek boyu, 🦘 zıpla
    ///   Yüz       : göz stilleri + yüz çerçeveleri (şekiller; dokun = tak, tekrar dokun = çıkar)
    ///   Süsler    : aksesuarlar (dokun = tak, tekrar dokun = çıkar)
    ///   Egzoz     : egzozlar (seçince motor çalışır)
    ///   Yüz       : göz stilleri (göz rengi Boya'dan: renk seç, göze dokun)
    ///   Arkadaş   : şoför hayvanlar    | kule: 🚪 içeri gir / dışarı çık
    ///   Sesler    : hayvan kornaları (sağdaki direksiyon seçili kornayı, 💨 seçili egzozu çalar)
    /// </summary>
    public class HudController : MonoBehaviour
    {
        [Header("Sistemler")]
        [SerializeField] CarBuilder builder;
        [SerializeField] CarFace face;
        [SerializeField] CarReactions reactions;
        [SerializeField] TouchRouter touch;
        [SerializeField] StickerPlacer stickers;
        [SerializeField] EngineController engine;
        [SerializeField] OrbitCameraRig rig;
        [SerializeField] ItemTray tray;

        [Header("Kategori rayı (Category sırasıyla)")]
        [SerializeField] Image[] categoryImages;
        [SerializeField] Color accent = new Color(0.22f, 0.78f, 0.9f);

        [Header("Ayar kuleleri")]
        [SerializeField] GameObject wheelPod;
        [Tooltip("Alttan üste: Alçak, Normal, Yüksek, Dev")]
        [SerializeField] Image[] heightDots;
        [Tooltip("Alttan üste: Küçük, Normal, Büyük")]
        [SerializeField] Image[] sizeDots;
        [SerializeField] Image hopBg;
        [SerializeField] GameObject buddyPod;
        [SerializeField] Image doorIcon;

        [Header("Sabit ikonlar")]
        [SerializeField] Sprite iconRainbow, iconGlitter, iconSponge, iconDoorIn, iconDoorOut;

        Category current = Category.Paint;
        Color paintColor = new Color(1f, 0.31f, 0.48f);
        PaintSpecial special;
        StickerDefinition sticker;
        readonly Dictionary<PartSlot, int> slotIndex = new Dictionary<PartSlot, int>();

        static readonly PartSlot[] AccessorySlots = { PartSlot.Top, PartSlot.Back, PartSlot.Side, PartSlot.Under, PartSlot.Toy };
        static readonly RideHeight[] Heights = { RideHeight.Low, RideHeight.Normal, RideHeight.High, RideHeight.Giant };
        static readonly WheelSize[] Sizes = { WheelSize.Small, WheelSize.Normal, WheelSize.Big };

        void OnEnable()
        {
            touch.ZoneTapped += OnZone;
            touch.PaintTapped += OnPaintTap;
            Entitlements.Changed += Refresh;
            builder.Rebuilt += Refresh;
        }

        void OnDestroy() { if (UiTheme.I) UiTheme.I.Changed -= OnTheme; }

        Color ButtonColor => UiTheme.I ? UiTheme.I.ButtonColor : Color.white;
        Color DotOff => UiTheme.I ? UiTheme.I.DotOff : new Color(1f, 1f, 1f, 0.95f);

        void OnTheme()
        {
            HighlightRail();
            UpdatePods();
            tray.BubbleColor = ButtonColor;
            tray.Recolor();
        }

        void OnDisable()
        {
            touch.ZoneTapped -= OnZone;
            touch.PaintTapped -= OnPaintTap;
            Entitlements.Changed -= Refresh;
            builder.Rebuilt -= Refresh;
        }

        void Start()
        {
            if (UiTheme.I) UiTheme.I.Changed += OnTheme;
            var cat = builder.Catalog;
            if (cat.colors.Count > 0) paintColor = cat.colors[0].color;
            ApplyTool();
            HighlightRail();
            Refresh();
        }

        // ---------------- kategori ----------------

        /// <summary>Soldaki kategori düğmelerine bağlanır (Category sırası).</summary>
        public void ShowCategory(int category) => ShowCategory((Category)category);

        public void ShowCategory(Category c)
        {
            current = c;
            // İçeriden sadece dışarıyı değiştiren kategorilerde çıkılır; arkadaş, egzoz ve korna içeride de seçilebilir
            if (rig.Interior && c != Category.Buddy && c != Category.Exhaust && c != Category.Sound) rig.SetInterior(false);
            ApplyTool();
            HighlightRail();
            Refresh();
        }

        void ApplyTool()
        {
            switch (current)
            {
                case Category.Paint:
                    touch.Tool = special == PaintSpecial.Sponge ? ToolMode.Wash : ToolMode.Paint;
                    touch.SprayColor = paintColor;
                    touch.Brush = special == PaintSpecial.Rainbow ? BrushStyle.Rainbow
                                : special == PaintSpecial.Glitter ? BrushStyle.Glitter : BrushStyle.Thick;
                    break;
                case Category.Stickers:
                    touch.Tool = ToolMode.Sticker;
                    touch.CurrentSticker = sticker;
                    break;
                default:
                    touch.Tool = ToolMode.Look;
                    break;
            }
        }

        void HighlightRail()
        {
            if (categoryImages == null) return;
            for (int i = 0; i < categoryImages.Length; i++)
                if (categoryImages[i]) categoryImages[i].color = i == (int)current ? accent : ButtonColor;
        }

        // ---------------- tepsi ----------------

        void Refresh()
        {
            if (builder.Config == null) return;
            var list = new List<TrayEntry>();
            var cat = builder.Catalog;
            var cfg = builder.Config;
            var fam = builder.Family;

            switch (current)
            {
                case Category.Paint:
                    foreach (var pc in cat.colors)
                    {
                        var col = pc.color;
                        list.Add(new TrayEntry
                        {
                            swatch = col, wordKey = pc.wordKey,
                            selected = special == PaintSpecial.None && CarConfig.Hex(col) == CarConfig.Hex(paintColor),
                            onPick = () => { paintColor = col; special = PaintSpecial.None; ApplyTool(); Refresh(); }
                        });
                    }
                    AddSpecial(list, iconRainbow, PaintSpecial.Rainbow, "shape_rainbow");
                    AddSpecial(list, iconGlitter, PaintSpecial.Glitter, "finish_glitter");
                    AddSpecial(list, iconSponge, PaintSpecial.Sponge, "action_wash");
                    break;

                case Category.Stickers:
                    StickerDefinition first = null;
                    foreach (var s in cat.Stickers())
                    {
                        if (first == null) first = s;
                        var def = s;
                        list.Add(new TrayEntry
                        {
                            icon = s.sprite, wordKey = s.wordKey, selected = sticker == def,
                            onPick = () => { sticker = def; ApplyTool(); Refresh(); }
                        });
                    }
                    if (sticker == null && first != null) { sticker = first; ApplyTool(); list[0] = Selected(list[0]); }
                    list.Add(new TrayEntry { icon = iconSponge, wordKey = "action_wash", onPick = () => stickers.ClearAll() });
                    break;

                case Category.Wheels:
                    foreach (var p in cat.PartsFor(PartSlot.Wheels, fam))
                    {
                        var id = p.id;
                        list.Add(new TrayEntry
                        {
                            icon = p.thumbnail, wordKey = p.wordKey, selected = cfg.GetPart(PartSlot.Wheels) == id,
                            onPick = () => { builder.SetPart(PartSlot.Wheels, id); reactions.Jump(1.5f); Refresh(); }
                        });
                    }
                    break;

                case Category.Accessories:
                    slotIndex.Clear();
                    foreach (var slot in AccessorySlots)
                        foreach (var p in cat.PartsFor(slot, fam))
                        {
                            if (!slotIndex.ContainsKey(slot)) slotIndex[slot] = list.Count;
                            var id = p.id; var sl = slot;
                            bool on = cfg.GetPart(sl) == id;
                            list.Add(new TrayEntry
                            {
                                icon = p.thumbnail, wordKey = p.wordKey, selected = on,
                                onPick = () =>
                                {
                                    bool wasOn = builder.Config.GetPart(sl) == id;
                                    builder.SetPart(sl, wasOn ? null : id);
                                    reactions.Jump(1.2f);
                                    Refresh();
                                }
                            });
                        }
                    break;

                case Category.Face:
                    foreach (var es in cat.eyeStyles)
                    {
                        var id = es.id;
                        list.Add(new TrayEntry
                        {
                            icon = es.thumbnail, wordKey = es.wordKey, selected = cfg.eyeStyle == id,
                            onPick = () => { face.SetStyle(id); face.PlayHappy(0.6f); Refresh(); }
                        });
                    }
                    // Yüz çerçeveleri (şekiller): dokun = tak, tekrar dokun = çıkar
                    slotIndex.Clear();
                    foreach (var p in cat.PartsFor(PartSlot.Front, fam))
                    {
                        if (!slotIndex.ContainsKey(PartSlot.Front)) slotIndex[PartSlot.Front] = list.Count;
                        var id = p.id;
                        list.Add(new TrayEntry
                        {
                            icon = p.thumbnail, wordKey = p.wordKey, selected = cfg.GetPart(PartSlot.Front) == id,
                            onPick = () =>
                            {
                                bool wasOn = builder.Config.GetPart(PartSlot.Front) == id;
                                builder.SetPart(PartSlot.Front, wasOn ? null : id);
                                if (!wasOn) face.PlayHappy(0.6f);
                                Refresh();
                            }
                        });
                    }
                    break;

                case Category.Exhaust:
                    foreach (var p in cat.PartsFor(PartSlot.Exhaust, fam))
                    {
                        var id = p.id;
                        list.Add(new TrayEntry
                        {
                            icon = p.thumbnail, wordKey = p.wordKey, selected = cfg.GetPart(PartSlot.Exhaust) == id,
                            onPick = () => { builder.SetPart(PartSlot.Exhaust, id); engine.Rev(); Refresh(); }
                        });
                    }
                    break;

                case Category.Buddy:
                    foreach (var p in cat.PartsFor(PartSlot.Buddy, fam))
                    {
                        var id = p.id;
                        list.Add(new TrayEntry
                        {
                            icon = p.thumbnail, wordKey = p.wordKey, selected = cfg.GetPart(PartSlot.Buddy) == id,
                            onPick = () => { builder.SetPart(PartSlot.Buddy, id); reactions.Jump(1f); Refresh(); }
                        });
                    }
                    break;

                case Category.Sound:
                    foreach (var p in cat.PartsFor(PartSlot.Horn, fam))
                    {
                        var id = p.id;
                        list.Add(new TrayEntry
                        {
                            icon = p.thumbnail, wordKey = null, selected = cfg.GetPart(PartSlot.Horn) == id,
                            onPick = () => { builder.SetPart(PartSlot.Horn, id); engine.Honk(); Refresh(); }
                        });
                    }
                    break;
            }
            tray.Show(list);
            UpdatePods();
        }

        static TrayEntry Selected(TrayEntry e) { e.selected = true; return e; }

        void AddSpecial(List<TrayEntry> list, Sprite icon, PaintSpecial sp, string word)
        {
            list.Add(new TrayEntry
            {
                icon = icon, wordKey = word, selected = special == sp,
                onPick = () => { special = sp; ApplyTool(); Refresh(); }
            });
        }

        // ---------------- arabaya dokunma ----------------

        void OnZone(PartSlot slot)
        {
            if (slot == PartSlot.Wheels) { if (current != Category.Wheels) ShowCategory(Category.Wheels); return; }
            // Önüne (yüzüne) dokununca Yüz, egzoza dokununca Egzoz, diğer bölgeler Süsler
            var target = slot == PartSlot.Front ? Category.Face : slot == PartSlot.Exhaust ? Category.Exhaust : Category.Accessories;
            if (current != target) ShowCategory(target);
            if (slotIndex.TryGetValue(slot, out int idx)) tray.ScrollTo(idx);
        }

        void OnPaintTap(PaintPart part)
        {
            if (special == PaintSpecial.Sponge) return;
            switch (part)
            {
                case PaintPart.Eyes:
                    face.SetEyeColor(paintColor);
                    VocabularyService.I?.Say("part_eyes");
                    break;
                case PaintPart.Roof:
                    builder.SetColor(TintChannel.Roof, paintColor);
                    VocabularyService.I?.Say("part_roof");
                    break;
                case PaintPart.Rim:
                    builder.SetColor(TintChannel.Rim, paintColor);
                    VocabularyService.I?.Say("part_rim");
                    break;
                default:
                    if (special == PaintSpecial.Rainbow) builder.SetFinish(Finish.Rainbow);
                    else
                    {
                        builder.SetColor(TintChannel.Body, paintColor);
                        builder.SetFinish(special == PaintSpecial.Glitter ? Finish.Neon : Finish.Solid);
                    }
                    VocabularyService.I?.Say("part_body");
                    break;
            }
            reactions.Jump(1f);
            face.PlayHappy(0.6f);
        }

        // ---------------- ayar kuleleri ----------------

        public void HeightUp() => StepHeight(1);
        public void HeightDown() => StepHeight(-1);
        public void SizeUp() => StepSize(1);
        public void SizeDown() => StepSize(-1);

        void StepHeight(int d)
        {
            int cur = System.Array.IndexOf(Heights, builder.Config.height);
            int next = Mathf.Clamp(cur + d, 0, Heights.Length - 1);
            if (next == cur) { reactions.Jump(0.5f); return; } // sınırda: küçük bir "olmaz" zıplaması
            builder.SetHeight(Heights[next]);
            VocabularyService.I?.Say(d > 0 ? "opp_high" : "opp_low");
            UpdatePods();
        }

        void StepSize(int d)
        {
            int cur = System.Array.IndexOf(Sizes, builder.Config.wheelSize);
            int next = Mathf.Clamp(cur + d, 0, Sizes.Length - 1);
            if (next == cur) { reactions.Jump(0.5f); return; }
            builder.SetWheelSize(Sizes[next]);
            reactions.Jump(1.2f);
            VocabularyService.I?.Say(d > 0 ? "size_big" : "size_small");
            UpdatePods();
        }

        public void ToggleHop()
        {
            builder.Hop = !builder.Hop;
            if (builder.Hop) VocabularyService.I?.Say("action_jump");
            UpdatePods();
        }

        public void ToggleInside()
        {
            rig.SetInterior(!rig.Interior);
            VocabularyService.I?.Say(rig.Interior ? "dir_inside" : "dir_outside");
            if (rig.Interior) StartCoroutine(WelcomeInside());
            UpdatePods();
        }

        /// <summary>İçeri girince kamera yerine oturduktan sonra arkadaş "hoş geldin" diye zıplar ve kıkırdar.</summary>
        System.Collections.IEnumerator WelcomeInside()
        {
            yield return new WaitForSeconds(0.75f);
            if (!rig.Interior || !builder.Chassis || !builder.Chassis.passengerSeat) yield break;
            var b = builder.Chassis.passengerSeat.GetComponentInChildren<BuddyAnimator>();
            if (b) b.Hop();
            AudioService.I?.PlaySfx(SynthSounds.Giggle, 0.1f);
            yield return new WaitForSeconds(0.5f);
            if (rig.Interior) VocabularyService.I?.Say("pofu_welcome_back");
        }

        void UpdatePods()
        {
            if (wheelPod) wheelPod.SetActive(current == Category.Wheels);
            if (buddyPod) buddyPod.SetActive(current == Category.Buddy);
            if (builder.Config == null) return;
            Meter(heightDots, System.Array.IndexOf(Heights, builder.Config.height));
            Meter(sizeDots, System.Array.IndexOf(Sizes, builder.Config.wheelSize));
            if (hopBg) hopBg.color = builder.Hop ? accent : ButtonColor;
            if (doorIcon) doorIcon.sprite = rig.Interior ? iconDoorOut : iconDoorIn;
        }

        void Meter(Image[] dots, int level)
        {
            if (dots == null) return;
            for (int i = 0; i < dots.Length; i++) if (dots[i]) dots[i].color = i <= level ? accent : DotOff;
        }
    }
}
