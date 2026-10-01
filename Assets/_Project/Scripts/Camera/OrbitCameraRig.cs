using UnityEngine;

namespace HayalGaraji
{
    /// <summary>
    /// Arabanın etrafında 360° dönen kamera. Boşta kalınca vitrin gibi yavaşça döner.
    /// İçeri girince sürücü koltuğuna yumuşak geçiş yapar.
    /// </summary>
    public class OrbitCameraRig : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] CarBuilder builder;
        [Tooltip("Açılışta kamera önden-sağdan bakar: çocuk ilk olarak Pofu'nun yüzünü görür")]
        [SerializeField] float yaw = 215f, pitch = 12f, distance = 5f;
        [Tooltip("Arabanın ekranda biraz yukarıda durması için bakış noktası bu kadar aşağı kaydırılır (alttaki tepsi arabayı kapatmasın).")]
        [SerializeField] float framingOffsetY = 0f; // Pofu ekranın tam ortasında, sol ve sağ menülerle aynı hizada
        [SerializeField] float minDistance = 2.5f, maxDistance = 10f, minPitch = 3f, maxPitch = 70f;
        [SerializeField] float orbitSpeed = 0.25f, inertia = 0.92f;
        [SerializeField] float idleDelay = 5f, idleSpinDegPerSec = 6f;
        [SerializeField] float interiorExtraFov = 20f;

        public float Distance => distance;
        public bool Interior { get; private set; }

        float vYaw, vPitch, idle, k, lookYaw, lookPitch, baseFov;
        int lastDragFrame = -1;

        float baseNear;
        static readonly int InteriorId = Shader.PropertyToID("_HG_Interior");

        void Awake() { baseFov = cam.fieldOfView; baseNear = cam.nearClipPlane; Shader.SetGlobalFloat(InteriorId, 0f); }
        void OnEnable() { builder.Rebuilt += OnRebuilt; }
        void OnDisable() { builder.Rebuilt -= OnRebuilt; }
        void OnRebuilt() { distance = Mathf.Clamp(builder.Definition.cameraDistance, minDistance, maxDistance); }

        public void SetDistance(float d) { distance = Mathf.Clamp(d, minDistance, maxDistance); idle = 0f; }

        public void Orbit(Vector2 screenDelta)
        {
            vYaw = screenDelta.x * orbitSpeed;
            vPitch = -screenDelta.y * orbitSpeed * 0.7f;
            yaw += vYaw; pitch += vPitch;
            idle = 0f; lastDragFrame = Time.frameCount;
        }

        public void Look(Vector2 screenDelta)
        {
            lookYaw += screenDelta.x * 0.2f;
            lookPitch = Mathf.Clamp(lookPitch - screenDelta.y * 0.15f, -50f, 50f);
            idle = 0f;
        }

        public void SetInterior(bool inside)
        {
            Interior = inside;
            lookYaw = 0f; lookPitch = -12f; // direksiyon ve gösterge düğmeleri görünsün
        }

        void LateUpdate()
        {
            var ch = builder.Chassis;
            if (!ch) return;
            float dt = Time.deltaTime;
            idle += dt;

            if (Time.frameCount != lastDragFrame)
            {
                yaw += vYaw; pitch += vPitch;
                vYaw *= inertia; vPitch *= inertia * 0.95f;
                if (idle > idleDelay && !Interior) yaw += idleSpinDegPerSec * dt;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            k = Mathf.MoveTowards(k, Interior ? 1f : 0f, dt * 1.6f);
            float e = Mathf.SmoothStep(0f, 1f, k);

            var target = ch.transform.position + Vector3.up * (ch.bodyRoot.localPosition.y + builder.Definition.cameraTargetHeight + framingOffsetY);
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var outPos = target - rot * Vector3.forward * distance;
            var outRot = Quaternion.LookRotation(target - outPos, Vector3.up);

            Vector3 pos = outPos; Quaternion r = outRot;
            if (e > 0f && ch.driverView)
            {
                var inRot = ch.driverView.rotation * Quaternion.Euler(lookPitch, lookYaw, 0f);
                pos = Vector3.Lerp(outPos, ch.driverView.position, e);
                r = Quaternion.Slerp(outRot, inRot, e);
            }
            cam.transform.SetPositionAndRotation(pos, r);
            cam.fieldOfView = baseFov + interiorExtraFov * e;
            // İçeride yakındaki direksiyon kesilmesin; cam ve çizgi kabukları kapanır
            cam.nearClipPlane = Mathf.Lerp(baseNear, 0.02f, e);
            Shader.SetGlobalFloat(InteriorId, e > 0.6f ? 1f : 0f);
        }
    }
}
