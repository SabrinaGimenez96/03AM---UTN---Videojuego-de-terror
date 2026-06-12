using HorrorPrototype.UI;
using UnityEngine;

namespace HorrorPrototype.Core
{
    // NightManager simula el paso de la noche: convierte segundos reales en reloj de 03:00 a 06:00
    // y decide el final cuando llega el amanecer.
    public class NightManager : MonoBehaviour
    {
        [Header("Duracion")]
        [Min(10f)] public float nightDurationSeconds = 180f;

        private const int StartMinutes = 3 * 60;
        private const int EndMinutes = 6 * 60;
        private float elapsedSeconds;
        private bool nightResolved;

        private void Start()
        {
            UIManager.Instance?.ShowTime(FormatClock(StartMinutes));
        }

        private void Update()
        {
            // Mientras la partida siga activa, avanza el reloj mostrado en el HUD.
            if (nightResolved || GameManager.Instance == null || GameManager.Instance.GameEnded)
            {
                return;
            }

            elapsedSeconds += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedSeconds / nightDurationSeconds);
            int currentMinutes = Mathf.RoundToInt(Mathf.Lerp(StartMinutes, EndMinutes, progress));
            UIManager.Instance?.ShowTime(FormatClock(currentMinutes));

            // A las 05:45 AM comienza la secuencia de escape
            if (currentMinutes >= 345 && !GameManager.Instance.isFinalEventActive)
            {
                GameManager.Instance.TriggerFinalEvent();
            }

            if (progress >= 1f)
            {
                ResolveNightEnd();
            }
        }

        private void ResolveNightEnd()
        {
            // Al terminar la noche se evalua si el jugador gana, sobrevive apenas o pierde.
            nightResolved = true;

            // Si llegamos a las 6:00 AM y el evento final estaba activo significa que NO escapamos por la puerta a tiempo
            if (GameManager.Instance != null && GameManager.Instance.isFinalEventActive)
            {
                GameManager.Instance.LoseGame("Se acabo el tiempo. Las sombras te atraparon antes de que pudieras escapar.");
                return;
            }

            if (GameManager.Instance.currentState == MentalState.Crisis)
            {
                GameManager.Instance.LoseGame("El amanecer llega, pero la crisis te consume antes de verlo.");
                return;
            }

            if (GameManager.Instance.IsStableEnoughForVictory)
            {
                GameManager.Instance.WinGame();
            }
            else if (GameManager.Instance.IsBarelySurviving)
            {
                GameManager.Instance.NeutralEndGame();
            }
            else
            {
                GameManager.Instance.LoseGame("Llegas al amanecer demasiado quebrado para escapar de la noche.");
            }
        }

        private static string FormatClock(int totalMinutes)
        {
            // Convierte minutos simulados en el formato visible del HUD.
            int hour = Mathf.Clamp(totalMinutes / 60, 3, 6);
            int minute = totalMinutes % 60;
            return $"{hour:00}:{minute:00} AM";
        }
    }
}
