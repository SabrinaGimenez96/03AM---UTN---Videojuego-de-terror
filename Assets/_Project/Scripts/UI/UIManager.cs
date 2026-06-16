using HorrorPrototype.Core;
using HorrorPrototype.Interaction;
using UnityEngine;
using UnityEngine.UI;

namespace HorrorPrototype.UI
{
    public enum EndResult
    {
        // El jugador llega al amanecer en buen estado.
        Victoria,
        // El jugador sobrevive, pero con miedo/cordura deteriorados.
        Neutral,
        // El miedo o la perdida de cordura terminan la partida.
        Derrota
    }

    // UIManager controla toda la interfaz: stats, reloj, texto de interaccion,
    // mensaje narrativo, boton contextual y pantalla final.
    public class UIManager : MonoBehaviour
    {
        private static UIManager instance;

        public static UIManager Instance
        {
            get
            {
                // El HUD tambien debe responder si el editor conserva la escena al entrar a Play.
                if (instance == null)
                {
                    instance = FindAnyObjectByType<UIManager>();
                }

                return instance;
            }
            private set => instance = value;
        }

        [Header("HUD")]
        // Textos del HUD principal que se actualizan desde GameManager, NightManager e interaccion.
        public Text statsText;
        public Text interactionText;
        public Text messageText;
        public Text timeText;
        public Text controlsText;
        public Text actionHintText;
        public GameObject contextActionPanel;
        public Text contextActionText;

        [Header("End Screen")]
        // Panel mostrado al ganar, perder o llegar a final neutral.
        public GameObject endPanel;
        public Text endTitleText;
        public Text endReasonText;
        [Header("End Screen Images (Optional)")]
        public Image endBackgroundImage;
        public Sprite victorySprite;
        public Sprite defeatSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (endPanel != null)
            {
                endPanel.SetActive(false);
            }
        }

        private void Start()
        {
            if (controlsText != null)
            {
                Invoke(nameof(FadeOutControlsText), 8f);
                Invoke(nameof(HideControlsTextDelayed), 10f);
            }
        }

        private void FadeOutControlsText()
        {
            if (controlsText != null)
            {
                controlsText.CrossFadeAlpha(0f, 2f, false);
            }
        }

        private void HideControlsTextDelayed()
        {
            if (controlsText != null)
            {
                controlsText.gameObject.SetActive(false);
            }
        }

        public void UpdateStats()
        {
            // Ocultamos los numeros de energia, miedo y cordura para mayor inmersión.
            // Solo mostramos el estado mental actual con colores dinámicos.
            if (statsText == null || GameManager.Instance == null)
            {
                return;
            }

            GameManager game = GameManager.Instance;
            string stateColor = "#FFFFFF"; // Blanco por defecto (Calma)
            
            if (game.currentState == MentalState.Inestable)
                stateColor = "#FFA500"; // Naranja
            else if (game.currentState == MentalState.Crisis)
                stateColor = "#FF0000"; // Rojo
                
            statsText.text = $"<color=white>Estado Mental:</color> <b><color={stateColor}>{game.currentState}</color></b>";
        }

        // El texto de objeto apuntado se alimenta desde el Raycast del jugador.
        public void ShowInteractionText(string text)
        {
            if (interactionText != null)
            {
                interactionText.text = text;
            }
        }

        public void ShowContextAction(string displayName, ActionType actionType, PlayerInteractor interactor)
        {
            // El panel contextual reutiliza el mismo boton para todos los interactuables.
            currentContextAction = actionType;
            currentInteractor = interactor;

            if (contextActionPanel != null)
            {
                contextActionPanel.SetActive(false); // Oculto permanentemente a favor del texto central
            }

            if (contextActionText != null)
            {
                contextActionText.text = displayName;
            }
        }

        public void HideContextAction()
        {
            currentInteractor = null;

            if (contextActionPanel != null)
            {
                contextActionPanel.SetActive(false);
            }
        }

        public void ShowMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }
        }

        public void ShowTime(string simulatedTime)
        {
            if (timeText != null)
            {
                timeText.text = simulatedTime;
            }
        }

        // La pantalla final distingue victoria, final neutral y derrota.
        public void ShowEndScreen(EndResult result, string reason)
        {
            if (endPanel != null)
            {
                endPanel.SetActive(true);
            }

            switch (result)
            {
                case EndResult.Victoria:
                    if (endTitleText != null) endTitleText.text = "VICTORIA";
                    if (endBackgroundImage != null && victorySprite != null) endBackgroundImage.sprite = victorySprite;
                    break;
                case EndResult.Neutral:
                    if (endTitleText != null) endTitleText.text = "FINAL NEUTRAL";
                    if (endBackgroundImage != null && victorySprite != null) endBackgroundImage.sprite = victorySprite;
                    break;
                default:
                    if (endTitleText != null) endTitleText.text = "DERROTA";
                    if (endBackgroundImage != null && defeatSprite != null) endBackgroundImage.sprite = defeatSprite;
                    break;
            }

            if (endReasonText != null)
            {
                endReasonText.text = reason;
            }
        }

        public void OnIgnoreAction()
        {
            // Metodos llamados por botones antiguos/directos de UI.
            ApplyAction(ActionType.Ignore);
        }

        public void OnPhoneAction()
        {
            ApplyAction(ActionType.Phone);
        }

        public void OnLampAction()
        {
            ApplyAction(ActionType.Lamp);
        }

        public void OnDoorAction()
        {
            ApplyAction(ActionType.Door);
        }

        public void OnContextAction()
        {
            // Boton contextual: si hay interactor, ejecuta exactamente lo mismo que la barra espaciadora.
            if (currentInteractor != null)
            {
                currentInteractor.InteractCurrent();
                return;
            }

            ApplyAction(currentContextAction);
        }

        private static void ApplyAction(ActionType action)
        {
            // Fallback para botones que aplican una accion directa sin objeto apuntado.
            GameManager.Instance?.ApplyAction(action);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            // Asegúrate de que la escena del menú se llame "Menu" o cambia este texto.
            UnityEngine.SceneManagement.SceneManager.LoadScene("Menu"); 
        }

        private ActionType currentContextAction;
        private PlayerInteractor currentInteractor;
    }
}
