using HorrorPrototype.Interaction;
using UnityEngine;

namespace HorrorPrototype.Events
{
    // HallwayExitDoorBootstrap prepara la puerta del fondo del pasillo al cargar la escena:
    // la deja como limite fisico sin accion ni texto de interaccion.
    public static class HallwayExitDoorBootstrap
    {
        private const string HallwayExitName = "HallwayExitDoor_Final";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConfigureHallwayExitDoor()
        {
            GameObject hallwayExit = GameObject.Find(HallwayExitName);
            if (hallwayExit == null)
            {
                return;
            }

            ConfigureCollider(hallwayExit);
            RemoveInteraction(hallwayExit);
        }

        private static void ConfigureCollider(GameObject hallwayExit)
        {
            // La puerta final si bloquea el limite del pasillo; no debe haber un plano previo.
            Collider hallwayCollider = hallwayExit.GetComponent<Collider>();
            if (hallwayCollider != null)
            {
                hallwayCollider.isTrigger = false;
            }
        }

        private static void RemoveInteraction(GameObject hallwayExit)
        {
            // La puerta final no ejecuta ninguna mecanica; solo marca el final del pasillo.
            InteractableObject interactable = hallwayExit.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                Object.Destroy(interactable);
            }
        }
    }
}
