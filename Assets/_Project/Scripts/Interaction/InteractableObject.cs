using UnityEngine;

namespace HorrorPrototype.Interaction
{
    public enum ActionType
    {
        // Ignorar/regresar a cama: accion defensiva o de reposicion.
        Ignore,
        // Celular: reduce miedo, puede recuperarse o devolverse al velador.
        Phone,
        // Lampara: ilumina, puede parpadear con miedo alto y modifica avistamientos.
        Lamp,
        // Puerta: via de escape de ultimo recurso con resultado segun estado mental.
        Door,
        // Television: se enciende con estatica, puede asustar.
        TV,
        // Espejo: al interactuar genera un pensamiento narrativo de duda.
        Mirror,
        // Puerta del baño al final del pasillo
        BathroomDoor
    }

    // InteractableObject etiqueta cualquier objeto o collider auxiliar con una accion de gameplay.
    // PlayerInteractor lee este componente para mostrar texto y ejecutar la regla correcta.
    public class InteractableObject : MonoBehaviour
    {
        // El nombre aparece en HUD cuando el jugador apunta al collider del objeto.
        public string displayName = "Interactuar";
        public ActionType actionType;
        // Permite que un collider grande active un objeto visual distinto, por ejemplo mesa -> lampara.
        public Transform feedbackTarget;
    }
}
