using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HorrorPrototype.Core;

namespace HorrorPrototype.Visuals
{
    [RequireComponent(typeof(Volume))]
    public class SanityVisualEffects : MonoBehaviour
    {
        private Volume volume;
        private Vignette vignette;
        private ColorAdjustments colorAdjustments;

        [Header("Vignette Settings (Fear)")]
        [Tooltip("Max intensity of the vignette when fear is at 10.")]
        public float maxVignetteIntensity = 0.6f;

        [Header("Color Settings (Sanity)")]
        [Tooltip("Max desaturation when sanity is at 0.")]
        public float minSaturation = -100f;

        private void Start()
        {
            volume = GetComponent<Volume>();

            // Extraemos los overrides del volumen si existen
            if (volume.profile.TryGet(out Vignette v))
            {
                vignette = v;
            }
            
            if (volume.profile.TryGet(out ColorAdjustments c))
            {
                colorAdjustments = c;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;

            // Mapeamos el Miedo (0 a 10) a la intensidad del Vignette
            if (vignette != null)
            {
                float targetIntensity = Mathf.Lerp(0f, maxVignetteIntensity, GameManager.Instance.miedo / 10f);
                // Usamos Lerp para que el cambio no sea brusco, sino que transicione suavemente
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, Time.deltaTime * 2f);
            }

            // Mapeamos la Cordura (5 a 0) a la saturación
            if (colorAdjustments != null)
            {
                // Cordura 5 = Saturacion 0 (Normal). Cordura 0 = Saturacion -100 (Blanco y negro)
                float targetSaturation = Mathf.Lerp(minSaturation, 0f, GameManager.Instance.cordura / 5f);
                colorAdjustments.saturation.value = Mathf.Lerp(colorAdjustments.saturation.value, targetSaturation, Time.deltaTime * 2f);
            }
        }
    }
}
