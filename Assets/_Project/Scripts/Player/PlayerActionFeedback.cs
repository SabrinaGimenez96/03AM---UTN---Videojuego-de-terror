using HorrorPrototype.Core;
using HorrorPrototype.Interaction;
using System.Collections;
using UnityEngine;
using HorrorPrototype.UI;

namespace HorrorPrototype.Player
{
    [RequireComponent(typeof(FirstPersonController))]
    [RequireComponent(typeof(CharacterController))]
    // PlayerActionFeedback convierte acciones de gameplay en feedback visible:
    // toma/deja el celular, prende/apaga lampara, parpadea luz y cambia postura cama/de pie.
    public class PlayerActionFeedback : MonoBehaviour
    {
        private static readonly Color PhoneScreenLitColor = new Color(2.4f, 2.4f, 2.4f, 1f);
        private static readonly Color PhoneScreenOffColor = Color.black;

        // Configuracion de postura. Define donde queda el jugador/camara en cama y al levantarse.
        public Transform cameraRoot;
        public bool startsInBed = true;
        public Vector3 bedPosition = new Vector3(0f, 0.45f, -2.25f);
        public Vector3 bedEulerAngles = Vector3.zero;
        public Vector3 bedCameraLocalPosition = new Vector3(0f, 0.42f, 0f);
        public Vector3 standPosition = new Vector3(0f, 0.05f, -0.35f);
        public Vector3 standEulerAngles = Vector3.zero;
        public Vector3 standCameraLocalPosition = new Vector3(0f, 1.72f, 0f);

        private FirstPersonController controller;
        private CharacterController characterController;
        private Transform heldPhone;
        private Transform phoneOriginalParent;
        private Vector3 phoneOriginalPosition;
        private Quaternion phoneOriginalRotation;
        private Vector3 phoneOriginalScale;
        private Collider[] heldPhoneColliders;
        private Coroutine lampFlickerRoutine;
        private Light flickeringLampLight;
        private float lampFlickerDeadline = -1f;
        private bool phoneIsHeld;
        private bool lampIsOn;
        private bool isInBed;
        private bool tvIsOn;

        public bool PhoneIsHeld => phoneIsHeld;
        public bool IsInBed => isInBed;
        public bool LampIsOn => lampIsOn;
        public bool TvIsOn => tvIsOn;

        private void Awake()
        {
            EnsureReferences();
            if (startsInBed)
            {
                ReturnToBed();
            }
            else
            {
                StandUpFromBed();
            }
        }

        private void Update()
        {
            // Seguro contra parpadeos atascados: si la corrutina no termina, restaura la lampara.
            if (lampFlickerDeadline < 0f || Time.realtimeSinceStartup < lampFlickerDeadline)
            {
                return;
            }

            if (lampFlickerRoutine != null)
            {
                StopCoroutine(lampFlickerRoutine);
                lampFlickerRoutine = null;
            }

            if (flickeringLampLight != null)
            {
                flickeringLampLight.enabled = lampIsOn;
            }

            flickeringLampLight = null;
            lampFlickerDeadline = -1f;
        }

        public void ApplyFeedback(InteractableObject interactable)
        {
            // Enruta el efecto visual correcto segun el tipo de objeto interactuado.
            EnsureReferences();
            if (interactable == null)
            {
                return;
            }

            switch (interactable.actionType)
            {
                case ActionType.Phone:
                    TogglePhoneView(GetFeedbackTarget(interactable));
                    break;
                case ActionType.Lamp:
                    ToggleLamp(interactable);
                    break;
                case ActionType.TV:
                    ToggleTV(GetFeedbackTarget(interactable));
                    break;
                case ActionType.Mirror:
                    TriggerMirrorNarrative();
                    break;
                case ActionType.Door:
                    if (isInBed)
                    {
                        StandUpFromBed();
                    }
                    break;
            }
        }

        private static Transform GetFeedbackTarget(InteractableObject interactable)
        {
            return interactable.feedbackTarget != null ? interactable.feedbackTarget : interactable.transform;
        }

        private void TogglePhoneView(Transform phone)
        {
            EnsureReferences();
            // El telefono se parenta a la camara para simular una vista en mano sin animacion.
            if (cameraRoot == null || phone == null)
            {
                return;
            }

            if (!phoneIsHeld)
            {
                heldPhone = phone;
                phoneOriginalParent = phone.parent;
                phoneOriginalPosition = phone.position;
                phoneOriginalRotation = phone.rotation;
                phoneOriginalScale = phone.localScale;

                phone.SetParent(cameraRoot);
                phone.localPosition = new Vector3(0f, -0.18f, 0.58f);
                phone.localRotation = Quaternion.Euler(-72f, 0f, 0f);
                phone.localScale = phoneOriginalScale * 1.75f;
                SetHeldPhoneColliders(false);
                SetPhoneScreenLit(phone, true);
                phoneIsHeld = true;
            }
            else
            {
                ReturnPhoneToNightstand();
            }
        }

        public void ReturnPhoneToNightstand()
        {
            // Devuelve el celular exactamente al padre, posicion, rotacion y escala originales.
            EnsureReferences();
            if (!phoneIsHeld || heldPhone == null)
            {
                return;
            }

            heldPhone.SetParent(phoneOriginalParent);
            heldPhone.position = phoneOriginalPosition;
            heldPhone.rotation = phoneOriginalRotation;
            heldPhone.localScale = phoneOriginalScale;
            SetPhoneScreenLit(heldPhone, false);
            SetHeldPhoneColliders(true);
            heldPhone = null;
            heldPhoneColliders = null;
            phoneIsHeld = false;
        }

        private static void SetPhoneScreenLit(Transform phone, bool lit)
        {
            // Enciende/apaga la pantalla del celular con blanco emisivo visible en editor y build.
            if (phone == null)
            {
                return;
            }

            foreach (Renderer renderer in phone.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.materials;
                bool changed = false;

                foreach (Material material in materials)
                {
                    if (material == null || !material.name.Contains("PhoneScreen"))
                    {
                        continue;
                    }

                    Color screenColor = lit ? PhoneScreenLitColor : PhoneScreenOffColor;
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", screenColor);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.SetColor("_Color", screenColor);
                    }

                    if (lit)
                    {
                        if (material.HasProperty("_BaseMap"))
                        {
                            material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                        }

                        if (material.HasProperty("_MainTex"))
                        {
                            material.SetTexture("_MainTex", Texture2D.whiteTexture);
                        }
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        if (lit)
                        {
                            material.EnableKeyword("_EMISSION");
                        }
                        else
                        {
                            material.DisableKeyword("_EMISSION");
                        }

                        material.SetColor("_EmissionColor", screenColor);
                    }

                    changed = true;
                }

                if (changed)
                {
                    renderer.materials = materials;
                    renderer.UpdateGIMaterials();
                }
            }
        }

        private void SetHeldPhoneColliders(bool enabled)
        {
            // Desactiva colliders del celular en mano para que no bloquee raycasts ni fisicas.
            if (heldPhone == null)
            {
                return;
            }

            if (heldPhoneColliders == null)
            {
                heldPhoneColliders = heldPhone.GetComponentsInChildren<Collider>();
            }

            foreach (Collider phoneCollider in heldPhoneColliders)
            {
                phoneCollider.enabled = enabled;
            }
        }

        private void ToggleLamp(InteractableObject lampInteractable)
        {
            // El velador puede activarse por su collider directo o via mesa usando feedbackTarget.
            Transform target = GetFeedbackTarget(lampInteractable);
            Light lampLight = target.GetComponentInChildren<Light>();
            if (lampLight == null)
            {
                lampLight = target.GetComponent<Light>();
            }

            lampIsOn = !lampIsOn;
            if (lampLight != null)
            {
                lampLight.enabled = lampIsOn;
            }

            ParticleSystem lampParticles = target.GetComponentInChildren<ParticleSystem>();
            if (lampParticles != null)
            {
                if (lampIsOn) lampParticles.Play();
                else lampParticles.Stop();
            }

            GameManager.Instance?.SetLampState(lampIsOn);

            if (lampIsOn && GameManager.Instance != null && GameManager.Instance.miedo >= 5)
            {
                // Con miedo alto, encender la lampara provoca parpadeo y aumenta tension.
                if (lampFlickerRoutine != null)
                {
                    StopCoroutine(lampFlickerRoutine);
                }

                flickeringLampLight = lampLight;
                lampFlickerDeadline = Time.realtimeSinceStartup + 1.5f;
                lampFlickerRoutine = StartCoroutine(FlickerLamp(lampLight));
                if (lampLight != null)
                {
                    lampLight.enabled = lampIsOn;
                }
            }
            else if (!lampIsOn && lampFlickerRoutine != null)
            {
                StopCoroutine(lampFlickerRoutine);
                lampFlickerRoutine = null;
                flickeringLampLight = null;
                lampFlickerDeadline = -1f;
            }
        }

        private void ToggleTV(Transform target)
        {
            tvIsOn = !tvIsOn;

            Light tvLight = target.GetComponentInChildren<Light>(true);
            if (tvLight != null)
            {
                tvLight.gameObject.SetActive(true);
                tvLight.enabled = tvIsOn;
            }

            AudioSource tvAudio = target.GetComponentInChildren<AudioSource>(true);
            if (tvAudio != null)
            {
                if (tvIsOn) tvAudio.Play();
                else tvAudio.Stop();
            }

            if (tvIsOn && GameManager.Instance != null && GameManager.Instance.miedo >= 5)
            {
                GameManager.Instance.ApplyParanormalEvent("La estática te perfora los oídos.", 1, -1);
            }
        }

        private void TriggerMirrorNarrative()
        {
            if (GameManager.Instance == null) return;

            if (GameManager.Instance.miedo >= 6)
            {
                string[] scaryThoughts = new string[] { "¿Quién está detrás de mí?", "Ese no es mi reflejo...", "Mis ojos se ven... vacíos.", "Siento que me miran desde adentro." };
                string thought = scaryThoughts[Random.Range(0, scaryThoughts.Length)];
                AudioManager.Instance?.PlayWhisperAt(null);
                GameManager.Instance.ApplyParanormalEvent(thought, 2, -1);
            }
            else
            {
                string[] thoughts = new string[] { "Solo soy yo...", "Tengo unas ojeras terribles.", "No quiero mirarme mucho tiempo." };
                string thought = thoughts[Random.Range(0, thoughts.Length)];
                UIManager.Instance?.ShowMessage(thought);
            }
        }

        private IEnumerator FlickerLamp(Light lampLight)
        {
            // Alterna la luz varias veces con tiempo real para que funcione aunque se pause Time.timeScale.
            if (lampLight == null)
            {
                yield break;
            }

            for (int i = 0; i < 8; i++)
            {
                lampLight.enabled = !lampLight.enabled;
                yield return new WaitForSecondsRealtime(Random.Range(0.05f, 0.16f));
            }

            lampLight.enabled = lampIsOn;
            lampFlickerRoutine = null;
            flickeringLampLight = null;
            lampFlickerDeadline = -1f;
        }

        public void StandUpFromBed()
        {
            EnsureReferences();
            // Al reposicionar se apaga el CharacterController para no pelear con colisiones.
            if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            {
                return;
            }

            characterController.enabled = false;
            transform.position = standPosition;
            transform.rotation = Quaternion.Euler(standEulerAngles);
            characterController.height = 1.9f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.enabled = true;

            if (cameraRoot != null)
            {
                cameraRoot.localPosition = standCameraLocalPosition;
                cameraRoot.localRotation = Quaternion.identity;
                cameraRoot.GetComponent<CameraShake>()?.SetBaseLocalPosition(standCameraLocalPosition);
            }

            controller.EnableStandingMovement();
            isInBed = false;
        }

        public void ReturnToBed()
        {
            // Regresa a la postura acostada sin soltar el celular si el jugador lo tenia en mano.
            EnsureReferences();
            if (GameManager.Instance != null && GameManager.Instance.GameEnded)
            {
                return;
            }

            characterController.enabled = false;
            transform.position = bedPosition;
            transform.rotation = Quaternion.Euler(bedEulerAngles);
            characterController.height = 0.8f;
            characterController.center = new Vector3(0f, 0.35f, 0f);
            characterController.enabled = true;

            if (cameraRoot != null)
            {
                cameraRoot.localPosition = bedCameraLocalPosition;
                cameraRoot.localRotation = Quaternion.identity;
                cameraRoot.GetComponent<CameraShake>()?.SetBaseLocalPosition(bedCameraLocalPosition);
            }

            controller.EnableBedLook();
            isInBed = true;
        }

        private void EnsureReferences()
        {
            // Busca referencias locales en caso de que el builder o inspector no las haya asignado.
            if (controller == null)
            {
                controller = GetComponent<FirstPersonController>();
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (cameraRoot == null)
            {
                Camera camera = GetComponentInChildren<Camera>(true);
                if (camera != null)
                {
                    cameraRoot = camera.transform;
                }
            }
        }
    }
}
