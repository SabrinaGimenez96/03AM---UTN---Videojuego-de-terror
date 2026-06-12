using System.Collections;
using UnityEngine;

namespace HorrorPrototype.Player
{
    // CameraShake mueve levemente la camara en su espacio local durante sustos o crisis,
    // y luego la devuelve a su posicion base para no desalinear la vista del jugador.
    public class CameraShake : MonoBehaviour
    {
        private Coroutine shakeRoutine;
        private Vector3 baseLocalPosition;

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
        }

        public void SetBaseLocalPosition(Vector3 localPosition)
        {
            // Actualiza la posicion estable cuando el jugador cambia entre cama y estar de pie.
            baseLocalPosition = localPosition;

            if (shakeRoutine == null)
            {
                transform.localPosition = baseLocalPosition;
            }
        }

        public void Shake(float duration, float intensity)
        {
            // Reinicia el temblor si llega otro susto antes de terminar el anterior.
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
            }

            shakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            // Genera offsets aleatorios pequenos cada frame y restaura al finalizar.
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector2 offset = Random.insideUnitCircle * intensity;
                transform.localPosition = baseLocalPosition + new Vector3(offset.x, offset.y, 0f);
                yield return null;
            }

            transform.localPosition = baseLocalPosition;
            shakeRoutine = null;
        }
    }
}
