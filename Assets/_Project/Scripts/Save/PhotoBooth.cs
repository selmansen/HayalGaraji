using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace HayalGaraji
{
    /// <summary>
    /// 📸 Sadece 3D sahneyi (arayüz olmadan) 4:3 fotoğraf olarak çeker ve koleksiyona ekler.
    /// Flaş ve "fotoğraf garaja uçuyor" animasyonu onPhotoTaken olayına bağlanır.
    /// </summary>
    public class PhotoBooth : MonoBehaviour
    {
        [SerializeField] Camera cam;
        [SerializeField] CarBuilder builder;
        [SerializeField] CarReactions reactions;
        [SerializeField] AudioClip shutter;
        [SerializeField] int width = 1024, height = 768;
        public UnityEvent<Texture2D> onPhotoTaken;

        bool busy;

        public void TakePhoto()
        {
            if (builder.Config == null || builder.Paint == null) { Debug.LogWarning("PhotoBooth: Henüz araba yüklenmedi."); return; }
            if (!busy) StartCoroutine(Capture());
        }

        IEnumerator Capture()
        {
            busy = true;
            AudioService.I?.PlaySfx(shutter ? shutter : SynthSounds.Shutter, 0f);
            yield return new WaitForEndOfFrame();

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
            var prevTarget = cam.targetTexture;
            float prevAspect = cam.aspect;
            cam.targetTexture = rt;
            cam.aspect = (float)width / height;
            cam.Render();
            cam.targetTexture = prevTarget;
            cam.aspect = prevAspect;
            cam.ResetAspect();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var photo = new Texture2D(width, height, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            photo.Apply();
            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);

            SaveService.AddToGarage(builder.Config.Clone(), builder.Paint.EncodePng(), photo.EncodeToJPG(85));
            reactions.Jump(2.2f);
            VocabularyService.I?.Say("pofu_photo");
            onPhotoTaken?.Invoke(photo); // UI küçük resmi gösterip sonra yok etmeli
            busy = false;
        }
    }
}
