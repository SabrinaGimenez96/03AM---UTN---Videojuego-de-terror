using UnityEngine;
using UnityEngine.AI;
using HorrorPrototype.Player;
using HorrorPrototype.Core;

namespace HorrorPrototype.Enemy
{
    // Controlador de Inteligencia Artificial usando NavMesh.
    // Hace que la entidad busque activamente al jugador en la escena y camine hacia él.
    [RequireComponent(typeof(NavMeshAgent))]
    public class ShadowAIController : MonoBehaviour
    {
        private NavMeshAgent agent;
        private Transform targetPlayer;
        private Animator animator;
        
        [Header("Configuración de Sustos")]
        [Tooltip("Velocidad de movimiento del espectro (más bajo es más tétrico)")]
        public float walkSpeed = 0.7f;
        [Tooltip("Distancia a la que la IA atrapa al jugador y lo asusta")]
        public float scareDistance = 1.8f;
        
        private bool hasScared = false;

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            
            // Configuramos la velocidad del agente para que camine lentamente
            agent.speed = walkSpeed;
            agent.stoppingDistance = 0.5f;
            
            // Buscamos dinámicamente al jugador (esté acostado o caminando)
            FirstPersonController player = FindAnyObjectByType<FirstPersonController>();
            if (player != null)
            {
                targetPlayer = player.transform;
            }
        }

        private void Update()
        {
            if (targetPlayer == null || hasScared) return;

            // Asegurarse de que el agente esté colocado en el NavMesh antes de moverlo
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(targetPlayer.position);
                
                if (animator != null)
                {
                    // Le decimos al Animator que estamos caminando si la velocidad es mayor a casi cero
                    animator.SetBool("isWalking", agent.velocity.sqrMagnitude > 0.05f);
                }
            }

            // Calculamos la distancia real entre la sombra y el jugador
            float distance = Vector3.Distance(transform.position, targetPlayer.position);
            
            // Si la sombra logra tocar al jugador antes de desaparecer...
            if (distance <= scareDistance)
            {
                TriggerJumpScare();
            }
        }

        private void TriggerJumpScare()
        {
            hasScared = true;
            
            if (GameManager.Instance != null)
            {
                // Penalización severa por dejar que la sombra te alcance
                GameManager.Instance.ApplyParanormalEvent("¡¿Q-Qué es eso?!", 3, -1);
            }
            
            AudioManager.Instance?.PlayScareStinger();
            
            // Detenemos al agente para que no siga empujando
            if (agent.isOnNavMesh) agent.isStopped = true;
            
            // Si hay animación de ataque, la ejecutamos y retrasamos la destrucción
            if (animator != null)
            {
                animator.SetTrigger("Attack");
                Destroy(gameObject, 1.2f); // Tiempo para ver la animación antes de que desaparezca
            }
            else
            {
                // Destruimos la sombra al instante si no hay animador
                Destroy(gameObject);
            }
        }
    }
}
