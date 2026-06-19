using HorrorPrototype.Interaction;
using HorrorPrototype.Events;
using HorrorPrototype.Player;
using HorrorPrototype.UI;
using UnityEngine;

namespace HorrorPrototype.Core
{
    public enum MentalState
    {
        // Estado seguro: el jugador conserva energia, miedo bajo y cordura alta.
        Calma,
        // Estado intermedio: las acciones empiezan a tener probabilidad de fallo.
        Inestable,
        // Estado critico: la mayoria de acciones empeoran la partida y la puerta derrota.
        Crisis
    }

    // GameManager es el centro de reglas del prototipo: guarda energia, miedo, cordura,
    // calcula el estado mental, resuelve acciones del jugador y dispara victoria/derrota.
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                // El fallback cubre modos de Play con recarga de escena/dominio reducida.
                if (instance == null)
                {
                    instance = FindAnyObjectByType<GameManager>();
                }

                return instance;
            }
            private set => instance = value;
        }

        // Este evento permite que la UI y el feedback visual reaccionen al estado mental.
        public event System.Action<MentalState> MentalStateChanged;

        [Header("Stats")]
        // Valores principales de supervivencia. Se ajustan con acciones y eventos paranormales.
        public int energia = 10;
        public int miedo = 0;
        public int cordura = 5;
        public MentalState currentState = MentalState.Calma;

        [Header("Estadisticas y Bateria")]
        public float lampBattery = 100f;
        public float lampDrainRate = 4.0f;
        [Header("Estadisticas Ocultas")]
        public int vecesPuertaAbierta = 0;
        public int sustosEvitados = 0;
        public int vecesLuzEncendida = 0;
        private int phoneUses = 0;

        [Header("Evento Final")]
        public bool isFinalEventActive = false;
        private int doorEscapeClicks = 0;
        private float finalEventFearTimer = 0f;
        private bool lampBroken = false;
        private bool isHallwayDeathSequenceActive = false;

        [Header("Feedback")]
        public CameraShake cameraShake;

        public bool GameEnded { get; private set; }

        // Condiciones usadas por NightManager para definir el tipo de final al amanecer.
        public bool IsStableEnoughForVictory => miedo <= 5 && cordura >= 3;
        public bool IsBarelySurviving => miedo <= 8 && cordura >= 1;
        // Estado logico de la lampara. Lo usan los eventos para saber si el fantasma se revela completo.
        public bool LampIsOn { get; private set; }

        private void Update()
        {
            if (GameEnded)
            {
                return;
            }

            if (isFinalEventActive)
            {
                finalEventFearTimer += Time.deltaTime;
                if (finalEventFearTimer >= 1.5f)
                {
                    miedo = Mathf.Min(10, miedo + 1);
                    finalEventFearTimer = 0f;
                    UIManager.Instance?.UpdateStats();
                    cameraShake?.Shake(0.1f, 0.02f);
                }
            }

            if (LampIsOn)
            {
                lampBattery -= lampDrainRate * Time.deltaTime;
                if (lampBattery <= 0f && !lampBroken)
                {
                    BreakLamp();
                }
            }

            if (!isHallwayDeathSequenceActive)
            {
                if (miedo >= 10 || cordura <= 0)
                {
                    LoseGame(miedo >= 10 ? "El panico paralizo tu corazon." : "Tu mente se quebro definitivamente.");
                }
            }
        }

        private void BreakLamp()
        {
            lampBroken = true;
            SetLampState(false);
            if (HorrorEventManager.Instance != null && HorrorEventManager.Instance.lampLight != null)
            {
                HorrorEventManager.Instance.lampLight.enabled = false;
            }
            ApplyStress(4, -2, "¡El foco de la lampara estallo por sobrecalentamiento!");
            AudioManager.Instance?.PlayScareStinger();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // La partida siempre inicia con valores validos y una lectura clara del HUD.
            ClampStats();
            UpdateMentalState();
            UIManager.Instance?.UpdateStats();
            UIManager.Instance?.ShowMessage("03:00 AM. Otra vez ese ruido. Siento que no estoy sola en la habitación...");
        }

        public void TriggerFinalEvent()
        {
            if (isFinalEventActive || GameEnded) return;

            isFinalEventActive = true;
            
            if (!lampBroken)
            {
                BreakLamp();
            }
            else
            {
                AudioManager.Instance?.PlayScareStinger();
            }

            UIManager.Instance?.ShowMessage("<color=red>¡VIENE POR TI! ¡ESCAPA POR LA PUERTA!</color>");
        }

        public void ApplyAction(ActionType action)
        {
            // Entrada comun para botones de UI, barra espaciadora e interactuables del cuarto.
            if (GameEnded)
            {
                return;
            }

            if (action == ActionType.Door && !(DoorEscapeController.Instance != null && DoorEscapeController.Instance.IsOpen))
            {
                if (energia <= 0)
                {
                    UIManager.Instance?.ShowMessage("Estas demasiado agotado para levantarte y llegar a la puerta.");
                    cameraShake?.Shake(0.2f, 0.05f);
                    return;
                }

                if (isFinalEventActive)
                {
                    doorEscapeClicks++;
                    if (doorEscapeClicks >= 3)
                    {
                        UIManager.Instance?.ShowMessage("<color=green>¡LOGRASTE ESCAPAR!</color>");
                        WinGame();
                    }
                    else
                    {
                        UIManager.Instance?.ShowMessage($"<color=red>¡FUERZA LA PUERTA! ({doorEscapeClicks}/3)</color>");
                        cameraShake?.Shake(0.4f, 0.15f);
                        AudioManager.Instance?.PlayKnock();
                    }
                    return;
                }

                vecesPuertaAbierta++;
            }
            
            if (action == ActionType.Lamp && lampBroken)
            {
                UIManager.Instance?.ShowMessage("La lampara esta quemada. No volvera a encender.");
                return;
            }
            
            if (action == ActionType.Lamp && !LampIsOn)
            {
                vecesLuzEncendida++;
            }

            string message = ResolveAction(action);
            
            if (action == ActionType.Ignore && (message.Contains("ayuda") || message.Contains("quieto") || message.Contains("al menos") || message.Contains("respiras")))
            {
                sustosEvitados++;
            }
            ClampStats();
            UpdateMentalState();
            UIManager.Instance?.UpdateStats();
            UIManager.Instance?.ShowMessage(message);
            CheckDefeat();
        }

        // Los eventos paranormales usan deltas para graduar intensidad sin repetir reglas.
        public void ApplyParanormalEvent(string message, int fearDelta, int sanityDelta)
        {
            if (GameEnded)
            {
                return;
            }

            miedo += fearDelta;
            cordura += sanityDelta;

            ClampStats();
            UpdateMentalState();
            UIManager.Instance?.UpdateStats();
            UIManager.Instance?.ShowMessage(message);
            CheckDefeat();
        }

        public string CalculateRank()
        {
            int score = (cordura * 100) - (miedo * 50) + (energia * 20);
            if (score >= 400) return "S";
            if (score >= 250) return "A";
            if (score >= 100) return "B";
            if (score >= 0) return "C";
            return "D";
        }

        public string GetStatsReport()
        {
            return $"Sustos Evitados: {sustosEvitados}\nVeces Puerta Abierta: {vecesPuertaAbierta}\nVeces Luz Encendida: {vecesLuzEncendida}";
        }

        public void WinGame()
        {
            if (GameEnded)
            {
                return;
            }

            GameEnded = true;
            StartCoroutine(WinSequence());
        }

        private System.Collections.IEnumerator WinSequence()
        {
            // Bloqueamos la camara parando el tiempo, pero la corrutina seguira en tiempo real
            UnlockCursor();
            Time.timeScale = 0f;
            
            // Frenamos cualquier temblor residual de la camara
            cameraShake?.StopShake();

            // Iniciamos el fade out de todos los sonidos de tension
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.FadeOutAllAudio(3.5f);
            }

            // Mostramos el texto narrativo final
            UIManager.Instance?.ShowMessage("06:00 AM. La noche por fin ha terminado... El sol empieza a salir.");

            // Iluminamos el cuarto de a poco (Amanecer) usando el tiempo real
            float fadeTime = 0f;
            float fadeDuration = 7.0f; // 7 segundos de amanecer
            Color initialAmbient = RenderSettings.ambientLight;
            Color morningAmbient = new Color(0.45f, 0.5f, 0.6f); // Gris azulado de madrugada

            while (fadeTime < fadeDuration)
            {
                fadeTime += Time.unscaledDeltaTime;
                RenderSettings.ambientLight = Color.Lerp(initialAmbient, morningAmbient, fadeTime / fadeDuration);
                yield return null;
            }

            // Mostramos la pantalla final oficial
            UIManager.Instance?.ShowEndScreen(EndResult.Victoria, "Lograste sobrevivir hasta el amanecer.");
        }

        // El final neutral mantiene la partida entregable sin tratar la inestabilidad como exito pleno.
        public void NeutralEndGame()
        {
            if (GameEnded)
            {
                return;
            }

            GameEnded = true;
            UIManager.Instance?.ShowEndScreen(EndResult.Neutral, "Sobreviviste, pero algo quedo contigo...");
            UnlockCursor();
            Time.timeScale = 0f;
        }

        // Este metodo concentra tension disparada por triggers y acciones externas.
        public void ApplyStress(int fearDelta, int sanityDelta, string message)
        {
            if (GameEnded)
            {
                return;
            }

            miedo += fearDelta;
            cordura += sanityDelta;
            ClampStats();
            UpdateMentalState();
            UIManager.Instance?.UpdateStats();

            if (!string.IsNullOrWhiteSpace(message))
            {
                UIManager.Instance?.ShowMessage(message);
            }

            CheckDefeat();
        }

        public void LoseFromHallwayCrisis()
        {
            LoseGame("Entraste al pasillo cuando tu mente ya estaba en crisis.");
        }

        private string ResolveAction(ActionType action)
        {
            // Control de abuso del celular
            if (action == ActionType.Phone)
            {
                phoneUses++;
                if (phoneUses > 3)
                {
                    // Si el jugador abusa del celular para calmarse, recibe un susto severo
                    miedo = Mathf.Min(10, miedo + 3);
                    cordura = Mathf.Max(0, cordura - 1);
                    AudioManager.Instance?.PlayScareStinger();
                    cameraShake?.Shake(0.3f, 0.05f);
                    return "Abres la galería... pero hay una foto tuya... durmiendo... tomada desde el techo.";
                }
            }

            if (action == ActionType.BathroomDoor)
            {
                if (Random.value < 0.5f)
                {
                    return "Que extraño, no puede abrirse, es como si tuviera el seguro puesto...";
                }
                else
                {
                    AudioManager.Instance?.PlayKnock();
                    miedo = Mathf.Min(10, miedo + 2);
                    cameraShake?.Shake(0.15f, 0.05f);
                    return "¡POOM POOM! ¿Hay alguien ahí adentro...?";
                }
            }

            // Selecciona una tabla de reglas distinta segun el estado mental actual.
            switch (currentState)
            {
                case MentalState.Calma:
                    return ResolveCalmAction(action);
                case MentalState.Crisis:
                    return ResolveCrisisAction(action);
                default:
                    return ResolveUnstableAction(action);
            }
        }

        private string ResolveCalmAction(ActionType action)
        {
            // En calma las acciones casi siempre ayudan; la puerta penaliza porque es innecesaria.
            switch (action)
            {
                case ActionType.Ignore:
                    energia += 1;
                    miedo -= 1;
                    cordura += 1;
                    return "Respiras profundo y te obligas a quedarte quieto.";
                case ActionType.Phone:
                    miedo -= 2;
                    cordura += 1;
                    return "La luz del celular y una foto familiar te devuelven al presente.";
                case ActionType.Lamp:
                    return ResolveCalmLampAction();
                case ActionType.Door:
                    if (DoorEscapeController.Instance != null && DoorEscapeController.Instance.IsOpen)
                    {
                        DoorEscapeController.Instance.CloseDoor();
                        return "Cierras la puerta. El pasillo queda del otro lado.";
                    }

                    energia -= 1;
                    cordura -= 1;
                    DoorEscapeController.Instance?.ShowOutcome(DoorEscapeOutcome.CalmWarning);
                    return "Te acercas a la puerta. El pasillo parece demasiado largo.";
                default:
                    return string.Empty;
            }
        }

        private string ResolveUnstableAction(ActionType action)
        {
            // En inestable las acciones tienen riesgo. La puerta puede salvar o empeorar la noche.
            switch (action)
            {
                case ActionType.Ignore:
                    if (Random.value < 0.75f)
                    {
                        energia += 1;
                        miedo -= 1;
                        cordura += 1;
                        return "Quedarte en cama ayuda mas de lo esperado.";
                    }

                    miedo += 1;
                    return "Intentas ignorarlo, pero el silencio pesa demasiado.";
                case ActionType.Phone:
                    if (Random.value < 0.75f)
                    {
                        miedo -= 2;
                        cordura += 1;
                        return "El celular te devuelve una sensacion de normalidad.";
                    }

                    miedo += 1;
                    return "La pantalla parpadea con una notificacion imposible.";
                case ActionType.Lamp:
                    if (!LampIsOn && miedo >= 5)
                    {
                        miedo += 2;
                        cordura -= 1;
                        return "Intentas encender la lampara, pero empieza a parpadear y la sombra se agranda.";
                    }

                    if (LampIsOn)
                    {
                        miedo -= 4;
                        cordura += 1;
                        return "Apagas la lampara. Sin verla directamente, la figura pierde fuerza.";
                    }

                    if (Random.value < 0.7f)
                    {
                        miedo -= 2;
                        cordura += 1;
                        return "La luz estabiliza un poco la habitacion.";
                    }

                    miedo += 1;
                    return "El foco titila y una sombra cruza la pared.";
                case ActionType.Door:
                    return ResolveUnstableDoorAction();
                default:
                    return string.Empty;
            }
        }

        private string ResolveCrisisAction(ActionType action)
        {
            // En crisis se empuja al jugador hacia derrota; abrir la puerta termina la partida.
            switch (action)
            {
                case ActionType.Ignore:
                    if (Random.value < 0.3f)
                    {
                        return "Te paralizas. Al menos nada empeora durante unos segundos.";
                    }

                    miedo += 2;
                    cordura -= 1;
                    return "Cerrar los ojos solo hace que los sonidos se acerquen.";
                case ActionType.Phone:
                    if (Random.value < 0.1f)
                    {
                        miedo -= 1;
                        cordura += 1;
                        return "Una foto familiar aparece en pantalla y te ancla.";
                    }

                    miedo += 2;
                    cordura -= 1;
                    return "El celular muestra tu habitacion desde un angulo que no existe.";
                case ActionType.Lamp:
                    if (!LampIsOn && miedo >= 5)
                    {
                        miedo += 3;
                        cordura -= 1;
                        return "La lampara parpadea en plena crisis y revela algo demasiado cerca.";
                    }

                    if (LampIsOn)
                    {
                        miedo -= 4;
                        cordura += 1;
                        return "Apagar la lampara corta la imagen antes de que termine de formarse.";
                    }

                    if (Random.value < 0.2f)
                    {
                        miedo -= 2;
                        cordura += 1;
                        return "La luz prende de golpe y recuperas control.";
                    }

                    miedo += 3;
                    cordura -= 1;
                    return "El velador revela una silueta que no deberia estar ahi.";
                case ActionType.Door:
                    if (DoorEscapeController.Instance != null && DoorEscapeController.Instance.IsOpen)
                    {
                        DoorEscapeController.Instance.CloseDoor();
                        return "Cierras la puerta desesperadamente.";
                    }

                    StartHallwayDeathSequence();
                    return "La oscuridad del pasillo te atrapa...";
                default:
                    return string.Empty;
            }
        }

        private void StartHallwayDeathSequence()
        {
            isHallwayDeathSequenceActive = true;
            FirstPersonController fpc = FindAnyObjectByType<FirstPersonController>();
            if (fpc != null) fpc.LockCameraForward();

            DoorEscapeController.Instance?.ShowDeathSequence();
            UIManager.Instance?.ShowMessage("<color=red>No hay escapatoria...</color>");
        }

        public void DrainStatsForCinematic()
        {
            energia = Mathf.Max(0, energia - 1);
            miedo = Mathf.Min(10, miedo + 1);
            cordura = Mathf.Max(0, cordura - 1);
            ClampStats();
            UIManager.Instance?.UpdateStats();
        }

        public void EndHallwayDeathSequence()
        {
            isHallwayDeathSequenceActive = false;
            LoseGame("La oscuridad del pasillo consumió tu mente.");
        }

        private string ResolveUnstableDoorAction()
        {
            // La puerta como via de escape de ultima instancia: 55% alivio, 45% pasillo oscuro.
            if (DoorEscapeController.Instance != null && DoorEscapeController.Instance.IsOpen)
            {
                DoorEscapeController.Instance.CloseDoor();
                return "Cierras la puerta. El pasillo queda del otro lado.";
            }

            energia -= 1;

            if (Random.value < 0.55f)
            {
                miedo -= 3;
                cordura += 2;
                DoorEscapeController.Instance?.ShowOutcome(DoorEscapeOutcome.SafeHallway);
                return "Abres la puerta. La luz del pasillo corta los sonidos y vuelves a respirar.";
            }

            miedo += 3;
            cordura -= 2;
            DoorEscapeController.Instance?.ShowOutcome(DoorEscapeOutcome.DarkHallway);
            HorrorEventManager.Instance?.TriggerHallwayDoorEvent();
            return "Abres la puerta. El pasillo esta negro y los sonidos golpean mas fuerte.";
        }

        // Eliminado CloseDoorIfAlreadyOpen, resuelto inline.

        private string ResolveCalmLampAction()
        {
            // Con miedo bajo, encender o apagar la lampara reduce tension sin activar parpadeo.
            if (LampIsOn)
            {
                miedo -= 2;
                return "Apagas la lampara y el cuarto recupera un silencio manejable.";
            }

            miedo -= 2;
            return "El velador ilumina las esquinas mas cercanas.";
        }

        public void SetLampState(bool isOn)
        {
            LampIsOn = isOn;
            
            // Si la luz se enciende, destruimos automáticamente cualquier monstruo/araña que esté en la escena
            if (isOn)
            {
                var arañas = FindObjectsByType<Enemy.ShadowAIController>(FindObjectsInactive.Exclude);
                foreach (var araña in arañas)
                {
                    Destroy(araña.gameObject);
                }
            }
        }

        public void ApplyLampPanic()
        {
            // Castigo especial cuando la lampara se usa con miedo alto.
            ApplyStress(2, -1, "La lampara parpadea y la sombra del cuarto se rompe en pedazos.");
        }

        public void ApplyLampOffRelief()
        {
            // Apagar la lampara permite bajar el miedo porque se deja de mirar directamente al fantasma.
            if (miedo < 5)
            {
                return;
            }

            ApplyStress(-2, 1, "Apagar la lampara oculta la figura y el miedo empieza a bajar mas rapido.");
        }

        private void CheckDefeat()
        {
            if (isHallwayDeathSequenceActive) return;

            // Cada modificacion de stats revisa si miedo o cordura ya cruzaron el limite.
            if (miedo >= 10)
            {
                LoseGame("El miedo llega al limite. No puedes seguir.");
            }
            else if (cordura <= 0)
            {
                LoseGame("Tu cordura se rompe antes del amanecer.");
            }
        }

        public void LoseGame(string reason)
        {
            if (GameEnded)
            {
                return;
            }

            GameEnded = true;
            UIManager.Instance?.ShowEndScreen(EndResult.Derrota, reason);
            UnlockCursor();
            Time.timeScale = 0f;
        }

        private void UpdateMentalState()
        {
            // Convierte energia/miedo/cordura en Calma, Inestable o Crisis para cambiar reglas.
            MentalState previousState = currentState;

            if (energia >= 5 && miedo <= 3 && cordura >= 4)
            {
                currentState = MentalState.Calma;
            }
            else if (energia < 5 && miedo > 3 && cordura < 4)
            {
                currentState = MentalState.Crisis;
            }
            else
            {
                currentState = MentalState.Inestable;
            }

            if (previousState != currentState)
            {
                MentalStateChanged?.Invoke(currentState);

                if (currentState == MentalState.Crisis)
                {
                    UIManager.Instance?.ShowMessage("Tu respiracion se rompe. La habitacion ya no se siente real.");
                    cameraShake?.Shake(0.65f, 0.08f);
                    AudioManager.Instance?.PlayScareStinger();
                }
            }
        }

        private void ClampStats()
        {
            // Evita valores fuera de rango para que UI, condiciones y probabilidades sean estables.
            energia = Mathf.Clamp(energia, 0, 10);
            miedo = Mathf.Clamp(miedo, 0, 10);
            cordura = Mathf.Clamp(cordura, 0, 5);
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
