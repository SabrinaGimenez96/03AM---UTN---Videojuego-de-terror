using UnityEngine;

namespace HorrorPrototype.Events
{
    // Este script hace que el modelo avance constantemente hacia adelante.
    // Como el HorrorEventManager ya hace que el espectro mire hacia el jugador al aparecer,
    // esto hara que el zombie se arrastre directo hacia la camara.
    public class CreepyCrawler : MonoBehaviour
    {
        [Header("Configuracion de Movimiento")]
        [Tooltip("Velocidad a la que se arrastra el espectro (metros por segundo).")]
        public float crawlSpeed = 0.15f;

        private void Update()
        {
            // Mueve el objeto hacia el frente (su propio frente local)
            transform.Translate(Vector3.forward * crawlSpeed * Time.deltaTime, Space.Self);
        }
    }
}
