using UnityEngine;

namespace HorrorPrototype.Core
{
    // AudioManager centraliza todos los sonidos del prototipo: ambiente en loop,
    // golpes, susurros, glitch del celular y stingers de susto.
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<AudioManager>();
                }

                return instance;
            }
            private set => instance = value;
        }

        [Header("Clips placeholder")]
        // Clips asignables desde el inspector. Si falta alguno, el metodo simplemente no reproduce nada.
        public AudioClip ambientLoop;
        public AudioClip whisper;
        public AudioClip knock;
        public AudioClip phoneGlitch;
        public AudioClip scareStinger;
        public AudioClip heartbeat;

        [Header("Fuentes")]
        // Una fuente mantiene el ambiente continuo y la otra reproduce sonidos puntuales.
        public AudioSource ambientSource;
        public AudioSource oneShotSource;
        public AudioSource heartbeatSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ConfigureSources();
        }

        private void Start()
        {
            PlayAmbientLoop();
            if (heartbeatSource != null && heartbeat != null)
            {
                heartbeatSource.clip = heartbeat;
                heartbeatSource.Play();
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || heartbeatSource == null) return;
            
            float miedo = GameManager.Instance.miedo; // 0 to 10
            
            // Si el miedo es muy bajo, casi no se escucha
            float targetVolume = Mathf.Lerp(0f, 1f, miedo / 10f);
            // El latido se acelera (tono) con el miedo
            float targetPitch = Mathf.Lerp(0.8f, 1.5f, miedo / 10f);
            
            // Lerp mas rapido para que responda enseguida a los sustos
            heartbeatSource.volume = Mathf.Lerp(heartbeatSource.volume, targetVolume, Time.deltaTime * 5f);
            heartbeatSource.pitch = Mathf.Lerp(heartbeatSource.pitch, targetPitch, Time.deltaTime * 2f);

            // Fuerza la reproduccion si por alguna razon el audio se detuvo o no arranco
            if (!heartbeatSource.isPlaying && heartbeatSource.clip != null)
            {
                heartbeatSource.Play();
            }
        }

        public void PlayWhisper()
        {
            // Reproduce susurros globales cuando no se necesita posicion espacial.
            PlayOneShot(whisper);
        }

        public void PlayWhisperAt(AudioSource emitter)
        {
            // Reproduce susurros desde un objeto especifico para ubicar el susto en el espacio.
            PlayAt(emitter, whisper);
        }

        public void PlayKnock()
        {
            PlayOneShot(knock);
        }

        public void PlayKnockAt(AudioSource emitter)
        {
            PlayAt(emitter, knock);
        }

        public void PlayPhoneGlitch()
        {
            PlayOneShot(phoneGlitch);
        }

        public void PlayPhoneGlitchAt(AudioSource emitter)
        {
            PlayAt(emitter, phoneGlitch);
        }

        public void PlayScareStinger()
        {
            PlayOneShot(scareStinger);
        }

        public void FadeOutAllAudio(float duration)
        {
            if (ambientSource != null) StartCoroutine(FadeOutRoutine(ambientSource, duration));
            if (heartbeatSource != null) StartCoroutine(FadeOutRoutine(heartbeatSource, duration));
            if (oneShotSource != null) StartCoroutine(FadeOutRoutine(oneShotSource, duration));
        }

        private System.Collections.IEnumerator FadeOutRoutine(AudioSource source, float duration)
        {
            if (source == null || !source.isPlaying) yield break;

            float startVolume = source.volume;
            float time = 0;

            while (time < duration)
            {
                // Usamos unscaledDeltaTime por si el juego está en pausa (Time.timeScale = 0)
                time += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, time / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
        }

        private void ConfigureSources()
        {
            // Autocompleta AudioSources para que el manager funcione aunque el prefab este incompleto.
            if (ambientSource == null)
            {
                ambientSource = GetComponent<AudioSource>();
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
            }

            if (heartbeatSource == null)
            {
                heartbeatSource = gameObject.AddComponent<AudioSource>();
            }

            ambientSource.playOnAwake = false;
            ambientSource.loop = true;
            oneShotSource.playOnAwake = false;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.loop = true;
        }

        private void PlayAmbientLoop()
        {
            // Inicia la cama sonora nocturna, sin fallar si aun no hay clip final.
            if (ambientSource == null || ambientLoop == null)
            {
                return;
            }

            ambientSource.clip = ambientLoop;
            ambientSource.Play();
        }

        private void PlayOneShot(AudioClip clip)
        {
            // Salida segura para clips puntuales no espaciales.
            if (oneShotSource != null && clip != null)
            {
                oneShotSource.PlayOneShot(clip);
            }
        }

        private static void PlayAt(AudioSource emitter, AudioClip clip)
        {
            // Salida segura para sonidos que deben venir de la puerta, celular o sombra.
            if (emitter != null && clip != null)
            {
                emitter.PlayOneShot(clip);
            }
        }
    }
}
