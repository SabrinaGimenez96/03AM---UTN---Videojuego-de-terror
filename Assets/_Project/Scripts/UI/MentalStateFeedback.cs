using HorrorPrototype.Core;
using HorrorPrototype.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorPrototype.UI
{
    // MentalStateFeedback comunica el estado mental sin postprocesado:
    // aumenta un overlay pulsante y sacude la camara al entrar en crisis.
    public class MentalStateFeedback : MonoBehaviour
    {
        // overlay es una imagen full-screen transparente que se tiñe segun el peligro.
        public Image overlay;
        public CameraShake cameraShake;
        public float unstableBaseAlpha = 0.06f;
        public float crisisBaseAlpha = 0.16f;

        private MentalState currentState = MentalState.Calma;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                currentState = GameManager.Instance.currentState;
                GameManager.Instance.MentalStateChanged += OnMentalStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.MentalStateChanged -= OnMentalStateChanged;
            }
        }

        private void Update()
        {
            // Calcula un pulso visual mas lento en inestable y mas agresivo en crisis.
            if (overlay == null)
            {
                return;
            }

            float pulse = Mathf.Abs(Mathf.Sin(Time.time * (currentState == MentalState.Crisis ? 8f : 3f)));
            float alpha = 0f;

            if (currentState == MentalState.Inestable)
            {
                alpha = unstableBaseAlpha + pulse * 0.035f;
            }
            else if (currentState == MentalState.Crisis)
            {
                alpha = crisisBaseAlpha + pulse * 0.12f;
            }

            Color color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }

        private void OnMentalStateChanged(MentalState state)
        {
            // Al pasar a crisis, agrega una sacudida para reforzar el cambio de estado.
            currentState = state;

            if (state == MentalState.Crisis)
            {
                cameraShake?.Shake(0.8f, 0.09f);
            }
        }
    }
}
