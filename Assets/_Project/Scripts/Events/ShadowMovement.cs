using UnityEngine;

namespace HorrorPrototype.Events
{
    // Este script le da a la sombra un leve movimiento espeluznante
    // deslizarse lentamente hacia una direccion y flotar ligeramente.
    public class ShadowMovement : MonoBehaviour
    {
        [Header("Configuracion de Movimiento")]
        [Tooltip("Velocidad a la que se desliza la sombra.")]
        public float driftSpeed = 0.5f;
        
        [Tooltip("Si es verdadero, la sombra mirara siempre hacia el jugador mientras se mueve.")]
        public bool facePlayer = true;

        [Header("Efecto de Flotacion")]
        [Tooltip("Velocidad de la flotacion arriba/abajo.")]
        public float bobSpeed = 1.5f;
        
        [Tooltip("Que tanto sube y baja.")]
        public float bobAmount = 0.05f;

        private Vector3 driftDirection;
        private Vector3 startPosition;
        private Transform playerCamera;
        private float randomOffset;

        private void Start()
        {
            startPosition = transform.position;
            
            // Elegimos una direccion lateral levemente aleatoria para que cada aparicion se sienta distinta
            float randomX = Random.Range(-1f, 1f);
            float randomZ = Random.Range(-0.2f, 0.5f); // Un poco hacia adelante o neutral
            driftDirection = transform.TransformDirection(new Vector3(randomX, 0f, randomZ).normalized);

            // Offset aleatorio para que la flotacion no sea sincronizada si hay varios
            randomOffset = Random.Range(0f, 10f);

            if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
        }

        private void Update()
        {
            // Movimiento lateral suave (Deslizamiento)
            transform.position += driftDirection * driftSpeed * Time.deltaTime;

            // Movimiento de flotacion (Bobbing) usando Seno
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed + randomOffset) * bobAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Rotar lentamente hacia el jugador para dar la sensacion de que te observa
            if (facePlayer && playerCamera != null)
            {
                // Solo rotar en el eje Y (para no inclinarse hacia arriba o abajo)
                Vector3 lookPos = playerCamera.position - transform.position;
                lookPos.y = 0;
                
                if (lookPos != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookPos);
                    // Rotacion suave hacia el jugador
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
                }
            }
        }
    }
}
