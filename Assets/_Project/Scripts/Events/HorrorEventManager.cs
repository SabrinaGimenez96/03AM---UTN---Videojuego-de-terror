using System.Collections;
using HorrorPrototype.Core;
using HorrorPrototype.Player;
using HorrorPrototype.UI;
using UnityEngine;

namespace HorrorPrototype.Events
{
    public enum HorrorEventType
    {
        // Sonido/susurro cercano que sube miedo.
        SoundEvent,
        // Aparicion visual de sombra o fantasma.
        ShadowEvent,
        // Parpadeo de lampara sin crear esfera visible.
        LightFlickerEvent,
        // Golpes desde la puerta.
        DoorKnockEvent,
        // Vibracion o fallo del celular.
        PhoneGlitchEvent,
        // Television que se enciende sola
        TVScaresEvent
    }

    // HorrorEventManager orquesta los sustos de la noche: eventos por temporizador,
    // eventos por acumulacion de mirada y efectos puntuales de puerta/celular/lampara.
    public class HorrorEventManager : MonoBehaviour
    {
        private static HorrorEventManager instance;

        public static HorrorEventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<HorrorEventManager>();
                }

                return instance;
            }
            private set => instance = value;
        }

        [Header("Ritmo de eventos")]
        // Controlan cada cuanto aparece un evento automatico.
        public float initialGracePeriod = 15f;
        public float minDelay = 14f;
        public float maxDelay = 24f;
        public Camera playerCamera;
        public float lookEventAngle = 70f;
        public float lookEventCooldown = 10f;

        [Header("Probabilidades")]
        // Pesos relativos. No necesitan sumar 1; PickWeightedEvent normaliza por total.
        [Range(0f, 1f)] public float soundEventChance = 0.22f;
        [Range(0f, 1f)] public float shadowEventChance = 0.24f;
        [Range(0f, 1f)] public float lightFlickerEventChance = 0.18f;
        [Range(0f, 1f)] public float doorKnockEventChance = 0.2f;
        [Range(0f, 1f)] public float phoneGlitchEventChance = 0.16f;
        [Range(0f, 1f)] public float tvScaresEventChance = 0.15f;

        [Header("Prefabs y puntos de escena")]
        // Referencias visuales, puntos de aparicion y emisores de audio espacial.
        public GameObject shadowFigurePrefab;
        public Transform[] shadowSpawnPoints;
        public Transform hallwayVisualPoint;
        public Transform doorTarget;
        public Transform phoneTarget;
        public Transform tvTarget;
        public AudioSource doorAudioSource;
        public AudioSource phoneAudioSource;
        public Light lampLight;
        public CameraShake cameraShake;

        private float nextEventTime;
        private float accumulatedLookAngle;
        private float nextLookEventTime;
        private Quaternion lastCameraRotation;
        private Coroutine flickerRoutine;
        private Coroutine phoneGlitchRoutine;

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
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (playerCamera != null)
            {
                lastCameraRotation = playerCamera.transform.rotation;
            }

            ScheduleNextEvent();
            // Retraso inicial para dar un respiro al jugador antes de que empiecen los eventos
            nextEventTime += initialGracePeriod;
            nextLookEventTime = Time.time + initialGracePeriod;
        }

        private void Update()
        {
            // Detiene sustos cuando no hay GameManager o la partida ya termino.
            if (GameManager.Instance == null || GameManager.Instance.GameEnded)
            {
                return;
            }

            if (Time.time >= nextEventTime)
            {
                TriggerRandomEvent();
                ScheduleNextEvent();
            }

            UpdateLookTriggeredEvent();
        }

        // La mirada acumulada fuerza microeventos para que observar tambien tenga tension.
        private void UpdateLookTriggeredEvent()
        {
            if (playerCamera == null)
            {
                return;
            }

            float angle = Quaternion.Angle(lastCameraRotation, playerCamera.transform.rotation);
            lastCameraRotation = playerCamera.transform.rotation;

            if (angle < 0.01f)
            {
                return;
            }

            accumulatedLookAngle += angle;
            if (accumulatedLookAngle >= lookEventAngle && Time.time >= nextLookEventTime)
            {
                accumulatedLookAngle = 0f;
                nextLookEventTime = Time.time + lookEventCooldown;
                TriggerRandomEvent();
                ScheduleNextEvent();
            }
        }

        public void TriggerHallwayDoorEvent()
        {
            // La puerta inestable muestra una figura rapida sin sumar doble castigo estadistico.
            SpawnAt(shadowFigurePrefab, hallwayVisualPoint, 2.4f);
            UIManager.Instance?.ShowMessage("Algo se desliza fuera de tu vista en el pasillo.");
            cameraShake?.Shake(0.35f, 0.045f);
        }

        private void TriggerRandomEvent()
        {
            // Elige un evento por peso y llama el metodo que aplica stats, audio y visual.
            HorrorEventType eventType = PickWeightedEvent();

            switch (eventType)
            {
                case HorrorEventType.SoundEvent:
                    TriggerSoundEvent();
                    break;
                case HorrorEventType.ShadowEvent:
                    TriggerShadowEvent();
                    break;
                case HorrorEventType.LightFlickerEvent:
                    TriggerLightFlickerEvent();
                    break;
                case HorrorEventType.DoorKnockEvent:
                    TriggerDoorKnockEvent();
                    break;
                case HorrorEventType.PhoneGlitchEvent:
                    TriggerPhoneGlitchEvent();
                    break;
                default:
                    TriggerTVScaresEvent();
                    break;
            }
        }

        private HorrorEventType PickWeightedEvent()
        {
            // Sorteo ponderado: cada chance ocupa un tramo del total acumulado.
            float total = soundEventChance + shadowEventChance + lightFlickerEventChance + doorKnockEventChance + phoneGlitchEventChance + tvScaresEventChance;
            float roll = Random.Range(0f, Mathf.Max(total, 0.01f));

            if ((roll -= soundEventChance) <= 0f) return HorrorEventType.SoundEvent;
            if ((roll -= shadowEventChance) <= 0f) return HorrorEventType.ShadowEvent;
            if ((roll -= lightFlickerEventChance) <= 0f) return HorrorEventType.LightFlickerEvent;
            if ((roll -= doorKnockEventChance) <= 0f) return HorrorEventType.DoorKnockEvent;
            if ((roll -= phoneGlitchEventChance) <= 0f) return HorrorEventType.PhoneGlitchEvent;
            return HorrorEventType.TVScaresEvent;
        }

        private void TriggerSoundEvent()
        {
            // Susurro: pequeno aumento de miedo y posible sombra temporal.
            GameManager.Instance.ApplyParanormalEvent("Algo respira cerca de la puerta.", 1, 0);
            GameObject ghost = SpawnAt(shadowFigurePrefab, PickShadowPoint(), 2.5f);
            AudioSource ghostAudioSource = ghost != null ? ghost.GetComponent<AudioSource>() : null;
            AudioManager.Instance?.PlayWhisperAt(ghostAudioSource);
        }

        private void TriggerShadowEvent()
        {
            // Si la lampara esta encendida, ver al fantasma completo castiga mas fuerte.
            bool lampIsOn = GameManager.Instance != null && GameManager.Instance.LampIsOn;
            string message = lampIsOn
                ? "La lampara revela una figura completa en la esquina."
                : "Una sombra cruza por la esquina de la habitacion.";
            int fearDelta = lampIsOn ? 3 : 1;
            int sanityDelta = lampIsOn ? -1 : 0;

            GameManager.Instance.ApplyParanormalEvent(message, fearDelta, sanityDelta);
            SpawnAt(shadowFigurePrefab, PickShadowPoint(), 2.5f);
            cameraShake?.Shake(0.42f, 0.055f);
            AudioManager.Instance?.PlayScareStinger();
        }

        private void TriggerLightFlickerEvent()
        {
            // Parpadeo ambiental: sube miedo, pero ya no instancia la esfera visible.
            GameManager.Instance.ApplyParanormalEvent("La luz parpadea sin razon.", 1, 0);

            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
            }

            flickerRoutine = StartCoroutine(FlickerLamp());
        }

        private void TriggerDoorKnockEvent()
        {
            // Golpes de puerta: afecta miedo/cordura y usa audio/temblor sin crear marcadores visibles.
            GameManager.Instance.ApplyParanormalEvent("Escuchas tres golpes desde el pasillo.", 2, -1);
            cameraShake?.Shake(0.5f, 0.075f);
            AudioManager.Instance?.PlayKnockAt(doorAudioSource);
        }

        private void TriggerPhoneGlitchEvent()
        {
            // Celular: castigo mixto y vibracion del telefono sin instanciar cubos placeholder.
            GameManager.Instance.ApplyParanormalEvent("El celular vibra, pero no tiene notificaciones.", 1, -1);

            if (phoneGlitchRoutine != null)
            {
                StopCoroutine(phoneGlitchRoutine);
            }

            phoneGlitchRoutine = StartCoroutine(VibratePhone());
            AudioManager.Instance?.PlayPhoneGlitchAt(phoneAudioSource);
        }

        private void TriggerTVScaresEvent()
        {
            GameManager.Instance.ApplyParanormalEvent("La televisión se encendió sola.", 2, -1);
            if (tvTarget != null)
            {
                Light tvLight = tvTarget.GetComponentInChildren<Light>();
                if (tvLight != null) tvLight.enabled = true;
                
                AudioSource tvAudio = tvTarget.GetComponentInChildren<AudioSource>();
                if (tvAudio != null && !tvAudio.isPlaying) tvAudio.Play();
            }
            cameraShake?.Shake(0.6f, 0.08f);
        }

        private IEnumerator VibratePhone()
        {
            // Mueve y rota levemente el celular por tiempo real para sugerir vibracion sin mostrar geometria extra.
            if (phoneTarget == null)
            {
                phoneGlitchRoutine = null;
                yield break;
            }

            Vector3 baseLocalPosition = phoneTarget.localPosition;
            Quaternion baseLocalRotation = phoneTarget.localRotation;
            float endTime = Time.realtimeSinceStartup + 0.9f;

            while (Time.realtimeSinceStartup < endTime)
            {
                float offsetX = Random.Range(-0.012f, 0.012f);
                float offsetZ = Random.Range(-0.012f, 0.012f);
                float roll = Random.Range(-1.8f, 1.8f);
                phoneTarget.localPosition = baseLocalPosition + new Vector3(offsetX, 0f, offsetZ);
                phoneTarget.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, roll);
                yield return new WaitForSecondsRealtime(0.035f);
            }

            phoneTarget.localPosition = baseLocalPosition;
            phoneTarget.localRotation = baseLocalRotation;
            phoneGlitchRoutine = null;
        }

        private IEnumerator FlickerLamp()
        {
            // Respeta el estado original de la lampara al terminar el parpadeo paranormal.
            if (lampLight == null)
            {
                yield break;
            }

            bool originalState = lampLight.enabled;
            for (int i = 0; i < 5; i++)
            {
                lampLight.enabled = !lampLight.enabled;
                yield return new WaitForSecondsRealtime(Random.Range(0.06f, 0.18f));
            }

            lampLight.enabled = originalState;
            flickerRoutine = null;
        }

        private Transform PickShadowPoint()
        {
            // Escoge un punto valido dentro del cuarto; si no hay, cae al punto de pasillo.
            if (shadowSpawnPoints == null || shadowSpawnPoints.Length == 0)
            {
                return hallwayVisualPoint;
            }

            return shadowSpawnPoints[Random.Range(0, shadowSpawnPoints.Length)];
        }

        private static GameObject SpawnAt(GameObject prefab, Transform point, float fallbackLifetime)
        {
            // Instancia prefabs temporales y los destruye aunque no traigan TemporaryDestroy.
            if (prefab == null || point == null)
            {
                return null;
            }

            GameObject spawned = Instantiate(prefab, point.position, point.rotation);
            TemporaryDestroy destroyer = spawned.GetComponent<TemporaryDestroy>();

            if (destroyer == null)
            {
                Destroy(spawned, fallbackLifetime);
            }

            return spawned;
        }

        private void ScheduleNextEvent()
        {
            // Programa el siguiente susto con un intervalo aleatorio dentro del rango configurado.
            nextEventTime = Time.time + Random.Range(minDelay, maxDelay);
        }
    }
}
