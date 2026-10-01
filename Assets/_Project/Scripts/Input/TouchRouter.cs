using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HayalGaraji
{
    /// <summary>
    /// Tüm dokunmaları tek yerden yönetir.
    ///   Bakma modu: sürükle = etrafında dön, dokun = Pofu tepki verir / bölge seçilir.
    ///   Boya modu: arabaya dokun = o parçayı boya, arabanın üstünde sürükle = sprey.
    ///   Yıkama modu: arabanın üstünde sürükle = sünger.
    ///   Çıkartma modu: arabaya dokun = çıkartma yapıştır.
    ///   İki parmak = yakınlaştır. Arayüz üzerindeki dokunuşlar yok sayılır. Editörde fare dokunma sayılır.
    /// </summary>
    public class TouchRouter : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] OrbitCameraRig rig;
        [SerializeField] CarBuilder builder;
        [SerializeField] CarReactions reactions;
        [SerializeField] StickerPlacer stickers;
        [Tooltip("Boyanabilir gövdenin katmanı (MeshCollider)")]
        [SerializeField] LayerMask bodyMask;
        [Tooltip("TapZone / EyeTapZone trigger'larının katmanı")]
        [SerializeField] LayerMask zoneMask;
        [SerializeField] float tapMaxTime = 0.35f, tapMaxMove = 20f, dragStart = 12f;
        [SerializeField] ParticleSystem foamFx;
        [SerializeField] AudioClip sprayLoop, washLoop;

        public ToolMode Tool { get; set; } = ToolMode.Look;
        public Color SprayColor { get; set; } = new Color(1f, 0.31f, 0.48f);
        public BrushStyle Brush { get; set; } = BrushStyle.Thick;
        public StickerDefinition CurrentSticker { get; set; }

        /// <summary>Bakma modunda arabanın bir bölgesine dokunuldu (tavan, ön, arka, tekerlek…).</summary>
        public event Action<PartSlot> ZoneTapped;
        /// <summary>Boya modunda arabanın bir parçasına dokunuldu.</summary>
        public event Action<PaintPart> PaintTapped;

        Vector2 startPos;
        float startTime;
        bool overUI, brushCandidate, brushing, pinching, ignoreUntilRelease;
        RaycastHit firstHit;
        float pinchStartDist, pinchStartZoom;
        PointerEventData ped;
        static readonly List<RaycastResult> uiHits = new List<RaycastResult>();

        bool BrushTool => Tool == ToolMode.Paint || Tool == ToolMode.Wash;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
#if UNITY_EDITOR
            TouchSimulation.Enable();
#endif
        }

        void OnDisable()
        {
            EndBrush();
#if UNITY_EDITOR
            TouchSimulation.Disable();
#endif
            EnhancedTouchSupport.Disable();
        }

        void Update()
        {
            var touches = ETouch.activeTouches;

            if (touches.Count >= 2)
            {
                float d = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
                if (!pinching)
                {
                    pinching = true; ignoreUntilRelease = true; EndBrush(); brushCandidate = false;
                    pinchStartDist = Mathf.Max(20f, d); pinchStartZoom = rig.Distance;
                }
                else rig.SetDistance(pinchStartZoom * pinchStartDist / Mathf.Max(20f, d));
                return;
            }
            pinching = false;

            if (touches.Count == 0) { ignoreUntilRelease = false; ScrollZoom(); return; }

            var t = touches[0];
            switch (t.phase)
            {
                case TouchPhase.Began:
                    startPos = t.screenPosition; startTime = Time.time;
                    overUI = IsOverUI(t.screenPosition);
                    brushCandidate = false;
                    if (overUI || ignoreUntilRelease) break;
                    // Boya/yıkama: parmak arabanın üstünde başladıysa sürükleme boyar (dönmez)
                    if (BrushTool && !rig.Interior && RaycastBody(t.screenPosition, out firstHit)) brushCandidate = true;
                    break;

                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    if (overUI || ignoreUntilRelease) break;
                    if (brushCandidate)
                    {
                        if (!brushing && (t.screenPosition - startPos).magnitude > dragStart)
                        {
                            brushing = true;
                            AudioService.I?.SetLoop(Tool == ToolMode.Wash ? (washLoop ? washLoop : SynthSounds.WashLoop) : (sprayLoop ? sprayLoop : SynthSounds.SprayLoop));
                            Brushing(firstHit);
                        }
                        if (brushing && RaycastBody(t.screenPosition, out var h)) Brushing(h);
                    }
                    else if (t.phase == TouchPhase.Moved)
                    {
                        if (rig.Interior) rig.Look(t.delta); else rig.Orbit(t.delta);
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    bool isTap = !overUI && !brushing && !ignoreUntilRelease
                                 && Time.time - startTime < tapMaxTime
                                 && (t.screenPosition - startPos).magnitude < tapMaxMove;
                    EndBrush();
                    brushCandidate = false;
                    if (isTap) HandleTap(t.screenPosition);
                    ignoreUntilRelease = false;
                    break;
            }
        }

        void Brushing(RaycastHit hit)
        {
            var paint = builder.Paint;
            if (!paint) return;
            if (Tool == ToolMode.Paint) paint.Spray(hit.textureCoord, SprayColor, Brush);
            else
            {
                if (paint.Wash(hit.textureCoord)) reactions.Tickle();
                if (foamFx) { foamFx.transform.position = hit.point + hit.normal * 0.05f; foamFx.Emit(2); }
            }
        }

        void EndBrush()
        {
            if (!brushing) return;
            brushing = false;
            AudioService.I?.SetLoop(null);
            builder.NotifyChanged();
        }

        void HandleTap(Vector2 pos)
        {
            var ray = cam.ScreenPointToRay(pos);
            if (rig.Interior)
            {
                // İçeride: direksiyon, gösterge düğmeleri, arkadaş, oyuncak
                int n = Physics.RaycastNonAlloc(ray, hitBuf, 10f, zoneMask, QueryTriggerInteraction.Collide);
                InteriorTapTarget best = null; float bestD = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    var it = hitBuf[i].collider.GetComponentInParent<InteriorTapTarget>();
                    if (it && hitBuf[i].distance < bestD) { best = it; bestD = hitBuf[i].distance; }
                }
                if (best) best.Tap(); else reactions.Jump(0.6f);
                return;
            }
            int carMask = bodyMask | zoneMask;

            if (Tool == ToolMode.Sticker)
            {
                if (CurrentSticker == null) Debug.LogWarning("Çıkartma: seçili çıkartma yok.");
                else if (RaycastBody(pos, out var bodyHit)) { stickers.Place(CurrentSticker, bodyHit); return; }
                else Debug.Log("Çıkartma: dokunuş arabanın gövdesine denk gelmedi.");
            }

            if (Tool == ToolMode.Paint && Physics.Raycast(ray, out var paintHit, 50f, carMask, QueryTriggerInteraction.Collide))
            {
                var part = PaintPart.Body;
                if (paintHit.collider.GetComponentInParent<EyeTapZone>()) part = PaintPart.Eyes;
                else
                {
                    var z = paintHit.collider.GetComponentInParent<TapZone>();
                    if (z) part = z.slot == PartSlot.Top ? PaintPart.Roof : z.slot == PartSlot.Wheels ? PaintPart.Rim : PaintPart.Body;
                }
                PaintTapped?.Invoke(part);
                return;
            }

            if (Physics.Raycast(ray, out var any, 50f, carMask, QueryTriggerInteraction.Collide))
            {
                if (Tool == ToolMode.Look)
                {
                    var zone = any.collider.GetComponentInParent<TapZone>();
                    if (zone) ZoneTapped?.Invoke(zone.slot);
                }
                reactions.Tapped(any.point);
                return;
            }

            // Sahne süsleri (güneş/ay gibi)
            if (Physics.Raycast(ray, out var stageHit, 300f, ~0, QueryTriggerInteraction.Collide))
            {
                var target = stageHit.collider.GetComponentInParent<StageTapTarget>();
                if (target) target.Tap();
            }
        }

        static readonly RaycastHit[] hitBuf = new RaycastHit[24];

        /// <summary>
        /// Boyanabilir gövdeyi bulur: katman ayarına güvenmez, ışının değdiği tüm katı nesneler arasından
        /// PaintableSurface taşıyan en yakın MeshCollider'ı seçer (önündeki süsler dokunuşu engellemez).
        /// </summary>
        bool RaycastBody(Vector2 pos, out RaycastHit hit)
        {
            hit = default;
            int n = Physics.RaycastNonAlloc(cam.ScreenPointToRay(pos), hitBuf, 50f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue; bool found = false;
            for (int i = 0; i < n; i++)
            {
                var h = hitBuf[i];
                if (!(h.collider is MeshCollider) || !h.collider.GetComponentInParent<PaintableSurface>()) continue;
                if (h.distance < best) { best = h.distance; hit = h; found = true; }
            }
            return found;
        }

        bool IsOverUI(Vector2 pos)
        {
            if (EventSystem.current == null) return false;
            if (ped == null) ped = new PointerEventData(EventSystem.current);
            ped.position = pos;
            uiHits.Clear();
            EventSystem.current.RaycastAll(ped, uiHits);
            return uiHits.Count > 0;
        }

        void ScrollZoom()
        {
            var m = Mouse.current;
            if (m == null) return;
            float s = m.scroll.ReadValue().y;
            if (Mathf.Abs(s) > 0.01f) rig.SetDistance(rig.Distance * (1f - s * 0.001f));
        }
    }
}
