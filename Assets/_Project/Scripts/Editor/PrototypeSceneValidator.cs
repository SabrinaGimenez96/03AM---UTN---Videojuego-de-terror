using HorrorPrototype.Core;
using HorrorPrototype.Events;
using HorrorPrototype.Interaction;
using HorrorPrototype.Player;
using HorrorPrototype.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HorrorPrototype.EditorTools
{
    // PrototypeSceneValidator revisa la escena activa antes de probar o entregar:
    // managers obligatorios, jugador, UI, interactuables, referencias y Build Settings.
    public static class PrototypeSceneValidator
    {
        [MenuItem("Tools/Horror Prototype/Validate Scene")]
        public static void ValidateActiveScene()
        {
            // Ejecuta todas las validaciones y reporta un resumen unico en consola.
            int errors = 0;
            int warnings = 0;

            ValidateSceneSetup(ref errors, ref warnings);
            ValidateManagers(ref errors, ref warnings);
            ValidatePlayer(ref errors, ref warnings);
            ValidateUI(ref errors, ref warnings);
            ValidateInteractables(ref errors, ref warnings);

            if (errors == 0 && warnings == 0)
            {
                Debug.Log("[Horror Prototype Validator] Escena validada sin problemas.");
            }
            else
            {
                Debug.Log($"[Horror Prototype Validator] Validacion terminada: {errors} error(es), {warnings} advertencia(s). Revisa la consola.");
            }
        }

        private static void ValidateSceneSetup(ref int errors, ref int warnings)
        {
            // Confirma que la escena cargada sea valida, sea MainPrototype y este en la build.
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                LogError("No hay una escena activa cargada.", ref errors);
                return;
            }

            if (activeScene.name != "MainPrototype")
            {
                LogWarning($"La escena activa es '{activeScene.name}'. El prototipo principal esperado es 'MainPrototype'.", ref warnings);
            }

            bool sceneInBuild = false;
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled && buildScene.path == activeScene.path)
                {
                    sceneInBuild = true;
                    break;
                }
            }

            if (!sceneInBuild)
            {
                LogError($"La escena activa no esta habilitada en Build Settings: {activeScene.path}", ref errors);
            }
        }

        private static void ValidateManagers(ref int errors, ref int warnings)
        {
            // Verifica que existan los managers principales y que sus referencias criticas esten asignadas.
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            AudioManager audioManager = Object.FindAnyObjectByType<AudioManager>();
            NightManager nightManager = Object.FindAnyObjectByType<NightManager>();
            HorrorEventManager eventManager = Object.FindAnyObjectByType<HorrorEventManager>();
            DoorEscapeController doorEscape = Object.FindAnyObjectByType<DoorEscapeController>();
            RestartManager restartManager = Object.FindAnyObjectByType<RestartManager>();

            Require(gameManager, "Falta GameManager en escena.", ref errors);
            Require(audioManager, "Falta AudioManager en escena.", ref errors);
            Require(nightManager, "Falta NightManager en escena.", ref errors);
            Require(eventManager, "Falta HorrorEventManager en escena.", ref errors);
            Require(doorEscape, "Falta DoorEscapeController en escena.", ref errors);
            RequireWarning(restartManager, "Falta RestartManager en escena.", ref warnings);

            if (gameManager != null)
            {
                RequireWarning(gameManager.cameraShake, "GameManager no tiene CameraShake asignado.", ref warnings);
            }

            if (audioManager != null)
            {
                RequireWarning(audioManager.ambientLoop, "AudioManager no tiene AmbientLoop asignado.", ref warnings);
                RequireWarning(audioManager.whisper, "AudioManager no tiene Whisper asignado.", ref warnings);
                RequireWarning(audioManager.knock, "AudioManager no tiene Knock asignado.", ref warnings);
                RequireWarning(audioManager.phoneGlitch, "AudioManager no tiene PhoneGlitch asignado.", ref warnings);
                RequireWarning(audioManager.scareStinger, "AudioManager no tiene ScareStinger asignado.", ref warnings);
                RequireWarning(audioManager.ambientSource, "AudioManager no tiene ambientSource asignado.", ref warnings);
                RequireWarning(audioManager.oneShotSource, "AudioManager no tiene oneShotSource asignado.", ref warnings);
            }

            if (eventManager != null)
            {
                Require(eventManager.playerCamera, "HorrorEventManager no tiene playerCamera asignada.", ref errors);
                Require(eventManager.shadowFigurePrefab, "HorrorEventManager no tiene shadowFigurePrefab asignado.", ref errors);
                RequireWarning(eventManager.hallwayVisualPoint, "HorrorEventManager no tiene hallwayVisualPoint asignado.", ref warnings);
                RequireWarning(eventManager.doorTarget, "HorrorEventManager no tiene doorTarget asignado.", ref warnings);
                RequireWarning(eventManager.phoneTarget, "HorrorEventManager no tiene phoneTarget asignado.", ref warnings);
                RequireWarning(eventManager.lampLight, "HorrorEventManager no tiene lampLight asignada.", ref warnings);
                RequireWarning(eventManager.cameraShake, "HorrorEventManager no tiene CameraShake asignado.", ref warnings);

                if (eventManager.shadowSpawnPoints == null || eventManager.shadowSpawnPoints.Length == 0)
                {
                    LogWarning("HorrorEventManager no tiene puntos de aparicion para sombras.", ref warnings);
                }
            }

            if (doorEscape != null)
            {
                RequireWarning(doorEscape.door, "DoorEscapeController no tiene puerta asignada.", ref warnings);
                RequireWarning(doorEscape.shadowFigurePrefab, "DoorEscapeController no tiene prefab de sombra asignado.", ref warnings);
                RequireWarning(doorEscape.hallwayVisualPoint, "DoorEscapeController no tiene punto visual de pasillo asignado.", ref warnings);
            }
        }

        private static void ValidatePlayer(ref int errors, ref int warnings)
        {
            // Revisa controlador, feedback de acciones, interactor y camara principal del jugador.
            FirstPersonController controller = Object.FindAnyObjectByType<FirstPersonController>();
            PlayerActionFeedback feedback = Object.FindAnyObjectByType<PlayerActionFeedback>();
            PlayerInteractor interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            Camera mainCamera = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();

            Require(controller, "Falta FirstPersonController en escena.", ref errors);
            Require(feedback, "Falta PlayerActionFeedback en escena.", ref errors);
            Require(interactor, "Falta PlayerInteractor en escena.", ref errors);
            Require(mainCamera, "Falta una Camera en escena.", ref errors);

            if (controller != null)
            {
                Require(controller.cameraRoot, "FirstPersonController no tiene cameraRoot asignado.", ref errors);
            }

            if (feedback != null)
            {
                Require(feedback.cameraRoot, "PlayerActionFeedback no tiene cameraRoot asignado.", ref errors);
            }

            if (interactor != null)
            {
                Require(interactor.playerCamera, "PlayerInteractor no tiene playerCamera asignada.", ref errors);
                Require(interactor.actionFeedback, "PlayerInteractor no tiene actionFeedback asignado.", ref errors);

                if (interactor.maxDistance <= 0f)
                {
                    LogError("PlayerInteractor.maxDistance debe ser mayor que cero.", ref errors);
                }
            }
        }

        private static void ValidateUI(ref int errors, ref int warnings)
        {
            // Comprueba que el HUD, boton contextual y pantalla final tengan los textos necesarios.
            UIManager ui = Object.FindAnyObjectByType<UIManager>();
            Require(ui, "Falta UIManager en escena.", ref errors);
            if (ui == null)
            {
                return;
            }

            Require(ui.statsText, "UIManager no tiene statsText asignado.", ref errors);
            RequireWarning(ui.interactionText, "UIManager no tiene interactionText asignado.", ref warnings);
            Require(ui.messageText, "UIManager no tiene messageText asignado.", ref errors);
            Require(ui.timeText, "UIManager no tiene timeText asignado.", ref errors);
            RequireWarning(ui.contextActionPanel, "UIManager no tiene contextActionPanel asignado.", ref warnings);
            RequireWarning(ui.contextActionText, "UIManager no tiene contextActionText asignado.", ref warnings);
            Require(ui.endPanel, "UIManager no tiene endPanel asignado.", ref errors);
            Require(ui.endTitleText, "UIManager no tiene endTitleText asignado.", ref errors);
            Require(ui.endReasonText, "UIManager no tiene endReasonText asignado.", ref errors);

            if (ui.contextActionPanel != null)
            {
                Button button = ui.contextActionPanel.GetComponent<Button>();
                RequireWarning(button, "ContextAction existe, pero no tiene componente Button.", ref warnings);

                if (button != null && button.onClick.GetPersistentEventCount() == 0)
                {
                    LogWarning("ContextAction tiene Button, pero no tiene evento persistente asignado.", ref warnings);
                }
            }
        }

        private static void ValidateInteractables(ref int errors, ref int warnings)
        {
            // Recorre todos los objetos interactuables y confirma que haya cama, celular, lampara y puerta.
            InteractableObject[] interactables = Object.FindObjectsByType<InteractableObject>(FindObjectsInactive.Include);
            if (interactables.Length == 0)
            {
                LogError("No hay InteractableObject en la escena.", ref errors);
                return;
            }

            bool hasBed = false;
            bool hasPhone = false;
            bool hasLamp = false;
            bool hasDoor = false;

            foreach (InteractableObject interactable in interactables)
            {
                Collider collider = interactable.GetComponentInChildren<Collider>();
                if (collider == null)
                {
                    LogWarning($"'{interactable.name}' es interactuable, pero no tiene Collider en sus hijos.", ref warnings);
                }

                hasBed |= interactable.actionType == ActionType.Ignore;
                hasPhone |= interactable.actionType == ActionType.Phone;
                hasLamp |= interactable.actionType == ActionType.Lamp;
                hasDoor |= interactable.actionType == ActionType.Door;
            }

            if (!hasBed) LogWarning("No hay interactuable de cama/ignorar.", ref warnings);
            if (!hasPhone) LogWarning("No hay interactuable de celular.", ref warnings);
            if (!hasLamp) LogWarning("No hay interactuable de lampara.", ref warnings);
            if (!hasDoor) LogWarning("No hay interactuable de puerta.", ref warnings);
        }

        private static void Require(Object value, string message, ref int errors)
        {
            // Helper para dependencias obligatorias que deben detener entrega si faltan.
            if (value == null)
            {
                LogError(message, ref errors);
            }
        }

        private static void RequireWarning(Object value, string message, ref int warnings)
        {
            // Helper para dependencias deseables que no bloquean la escena pero conviene revisar.
            if (value == null)
            {
                LogWarning(message, ref warnings);
            }
        }

        private static void LogError(string message, ref int errors)
        {
            errors++;
            Debug.LogError($"[Horror Prototype Validator] {message}");
        }

        private static void LogWarning(string message, ref int warnings)
        {
            warnings++;
            Debug.LogWarning($"[Horror Prototype Validator] {message}");
        }
    }
}
