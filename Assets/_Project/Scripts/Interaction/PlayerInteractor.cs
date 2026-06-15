using HorrorPrototype.Core;
using HorrorPrototype.Events;
using HorrorPrototype.Player;
using HorrorPrototype.UI;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorPrototype.Interaction
{
    // PlayerInteractor detecta objetos interactivos con un Raycast desde el centro de la camara,
    // muestra el texto contextual y ejecuta la accion con barra espaciadora o boton UI.
    public class PlayerInteractor : MonoBehaviour
    {
        // Distancia maxima del Raycast; se aumento para alcanzar veladores desde la cama.
        public Camera playerCamera;
        public float maxDistance = 3f;
        public PlayerActionFeedback actionFeedback;

        private InteractableObject currentInteractable;

        private void Update()
        {
            UpdateTarget();

            if (CanUseCurrentAction() && WasInteractPressed())
            {
                InteractCurrent();
            }
        }

        public void InteractCurrent()
        {
            // Si no se mira ningun objeto pero el jugador esta en cama y mira a la izquierda, la barra lo levanta.
            if (currentInteractable == null && actionFeedback != null && actionFeedback.IsInBed)
            {
                FirstPersonController fpc = actionFeedback.GetComponent<FirstPersonController>();
                if (fpc != null && fpc.IsLookingLeft())
                {
                    actionFeedback.StandUpFromBed();
                    UIManager.Instance?.HideContextAction();
                }
                return;
            }

            if (currentInteractable != null)
            {
                InteractableObject interacted = currentInteractable;

                if (ShouldReturnPhone(interacted))
                {
                    // Si el celular ya esta en mano, interactuar con el celular lo devuelve al velador.
                    actionFeedback.ReturnPhoneToNightstand();
                    UIManager.Instance?.HideContextAction();
                    return;
                }

                if (ShouldStandUpFromBed(interacted))
                {
                    FirstPersonController fpc = actionFeedback.GetComponent<FirstPersonController>();
                    if (fpc != null && fpc.IsLookingLeft())
                    {
                        actionFeedback.StandUpFromBed();
                        UIManager.Instance?.HideContextAction();
                    }
                    return;
                }

                if (IsBedInteraction(interacted))
                {
                    // Interactuar con la cama estando de pie aplica "Ignore" y vuelve a postura acostada.
                    GameManager.Instance?.ApplyAction(ActionType.Ignore);
                    actionFeedback.ReturnToBed();

                    UIManager.Instance?.HideContextAction();
                    return;
                }

                GameManager.Instance?.ApplyAction(interacted.actionType);
                actionFeedback?.ApplyFeedback(interacted);
            }
        }

        private void UpdateTarget()
        {
            // El Raycast sale del centro de pantalla para que mouse y boton contextual coincidan.
            currentInteractable = null;

            if (playerCamera == null)
            {
                UIManager.Instance?.ShowInteractionText(string.Empty);
                UIManager.Instance?.HideContextAction();
                return;
            }

            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
            {
                currentInteractable = hit.collider.GetComponentInParent<InteractableObject>();
            }

            if (currentInteractable != null)
            {
                string displayName = GetDisplayName(currentInteractable);
                if (string.IsNullOrEmpty(displayName))
                {
                    UIManager.Instance?.ShowInteractionText(string.Empty);
                    UIManager.Instance?.HideContextAction();
                }
                else
                {
                    UIManager.Instance?.ShowInteractionText("[Espacio] " + displayName);
                    UIManager.Instance?.ShowContextAction(displayName, currentInteractable.actionType, this);
                }
            }
            else
            {
                if (actionFeedback != null && actionFeedback.IsInBed)
                {
                    FirstPersonController fpc = actionFeedback.GetComponent<FirstPersonController>();
                    if (fpc != null && fpc.IsLookingLeft())
                    {
                        UIManager.Instance?.ShowInteractionText("[Espacio] Levantarse");
                        UIManager.Instance?.ShowContextAction("Levantarse", ActionType.Ignore, this);
                    }
                    else
                    {
                        UIManager.Instance?.ShowInteractionText(string.Empty);
                        UIManager.Instance?.HideContextAction();
                    }
                }
                else
                {
                    UIManager.Instance?.ShowInteractionText(string.Empty);
                    UIManager.Instance?.HideContextAction();
                }
            }
        }

        private string GetDisplayName(InteractableObject interactable)
        {
            // Cambia los textos segun estado: levantarse, regresar, tomar/dejar, encender/apagar.
            if (IsBedInteraction(interactable))
            {
                return actionFeedback.IsInBed ? "Levantarse" : "Regresar a cama";
            }

            if (ShouldStandUpFromBed(interactable))
            {
                FirstPersonController fpc = actionFeedback.GetComponent<FirstPersonController>();
                if (fpc != null && fpc.IsLookingLeft())
                {
                    return "Levantarse";
                }
                return string.Empty;
            }

            if (interactable.actionType == ActionType.Phone)
            {
                return ShouldReturnPhone(interactable) ? "Dejar el celular" : "Tomar el celular";
            }

            if (interactable.actionType == ActionType.Lamp && actionFeedback != null)
            {
                return actionFeedback.LampIsOn ? "Apagar lámpara" : "Encender lámpara";
            }

            if (interactable.actionType == ActionType.TV && actionFeedback != null)
            {
                return actionFeedback.TvIsOn ? "Apagar TV" : "Encender TV";
            }

            if (interactable.actionType == ActionType.Door)
            {
                return DoorEscapeController.Instance != null && DoorEscapeController.Instance.IsOpen ? "Cerrar puerta" : "Abrir puerta";
            }

            return interactable.displayName;
        }

        private bool IsBedInteraction(InteractableObject interactable)
        {
            // El interactuable Ignore representa la cama dentro de esta escena.
            return actionFeedback != null && interactable.actionType == ActionType.Ignore;
        }

        private bool ShouldStandUpFromBed(InteractableObject interactable)
        {
            // Evita ejecutar puerta/cama mientras el jugador aun esta acostado.
            return actionFeedback != null && actionFeedback.IsInBed && (IsBedInteraction(interactable) || interactable.actionType == ActionType.Door);
        }

        private bool ShouldReturnPhone(InteractableObject interactable)
        {
            // Reutiliza el mismo objeto/collider del celular para tomarlo y devolverlo.
            return actionFeedback != null && actionFeedback.PhoneIsHeld && interactable.actionType == ActionType.Phone;
        }

        private bool CanUseCurrentAction()
        {
            // Permite interactuar si hay objetivo o si la cama habilita la accion de levantarse.
            return currentInteractable != null || (actionFeedback != null && actionFeedback.IsInBed);
        }

        private static bool WasInteractPressed()
        {
            // Lee barra espaciadora tanto con Input System como con Input Manager clasico.
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }
    }
}
