using System.Collections;
using UnityEngine;

namespace HorrorPrototype.Events
{
    public enum DoorEscapeOutcome
    {
        // Calma: usar la puerta fue innecesario y solo muestra una advertencia.
        CalmWarning,
        // Inestable positivo: se ve luz y bajan miedo/recupera cordura.
        SafeHallway,
        // Inestable negativo: se ve oscuridad/figura y empeoran miedo/cordura.
        DarkHallway,
        // Crisis: la puerta confirma la derrota.
        Crisis
    }

    // DoorEscapeController maneja la puerta como recurso de ultimo momento:
    // abre de forma persistente y deja el pasillo transitable.
    public class DoorEscapeController : MonoBehaviour
    {
        private static DoorEscapeController instance;

        public static DoorEscapeController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindAnyObjectByType<DoorEscapeController>();
                }

                return instance;
            }
            private set => instance = value;
        }

        [Header("Door")]
        // Transform de pivote de la puerta. openYaw rota ese pivote durante el reveal.
        public Transform door;
        public float openYaw = -92f;
        public Vector3 openOffset = new Vector3(0.42f, 0f, 0.18f);
        public float revealSeconds = 2.5f;

        [Header("Hallway Reveal")]
        // Referencias heredadas de versiones anteriores; se limpian para evitar planos/luz bloqueando el pasillo.
        public Light hallwayLight;
        public Renderer hallwayBackRenderer;
        public Renderer hallwayFogRenderer;
        public Color safeLightColor = new Color(1f, 0.86f, 0.48f);
        public Color dangerLightColor = new Color(0.32f, 0.02f, 0.02f);
        public GameObject shadowFigurePrefab;
        public Transform hallwayVisualPoint;

        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private bool hasClosedPose;
        private GameObject activeReveal;
        private Coroutine closeRoutine;
        private float closeAtRealtime = -1f;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CaptureClosedPose();
            HideHallwayReveal();
        }

        public void ShowOutcome(DoorEscapeOutcome outcome)
        {
            // Prepara el estado cerrado, abre la puerta y limpia cualquier plano o luz temporal del pasillo.
            CaptureClosedPose();
            StopCloseRoutine();
            CancelInvoke(nameof(CloseDoor));
            closeAtRealtime = -1f;
            OpenDoor();
            HideHallwayReveal();

            switch (outcome)
            {
                case DoorEscapeOutcome.DarkHallway:
                    SpawnShadowReveal(2.6f);
                    break;
                case DoorEscapeOutcome.Crisis:
                    SpawnShadowReveal(4f);
                    break;
            }
        }

        public void HideHallwayReveal()
        {
            // Apaga luz, niebla, fondo artificial y figura para que no bloqueen el pasillo.
            if (hallwayLight != null)
            {
                hallwayLight.enabled = false;
            }

            if (hallwayBackRenderer != null)
            {
                hallwayBackRenderer.enabled = false;
            }

            SetHallwayFog(false, Color.clear);
            ClearActiveReveal();
        }

        public void CloseDoor()
        {
            // Restaura posicion/rotacion originales y limpia cualquier elemento temporal del reveal.
            CancelInvoke(nameof(CloseDoor));
            StopCloseRoutine();
            closeAtRealtime = -1f;

            if (door != null && hasClosedPose)
            {
                door.position = closedPosition;
                door.rotation = closedRotation;
                Physics.SyncTransforms();
            }

            IsOpen = false;
            HideHallwayReveal();
        }

        private void Update()
        {
            if (closeAtRealtime < 0f || Time.realtimeSinceStartup < closeAtRealtime)
            {
                return;
            }

            CloseDoor();
        }

        private void CaptureClosedPose()
        {
            // Guarda la pose original una sola vez para cerrar exactamente donde estaba.
            if (hasClosedPose || door == null)
            {
                return;
            }

            closedPosition = door.position;
            closedRotation = door.rotation;
            hasClosedPose = true;
        }

        private void OpenDoor()
        {
            // Aplica desplazamiento y rotacion de apertura al pivote configurado.
            if (door == null)
            {
                return;
            }

            door.position = closedPosition + openOffset;
            door.rotation = closedRotation * Quaternion.Euler(0f, openYaw, 0f);
            Physics.SyncTransforms();
            IsOpen = true;
        }

        private void SetHallwayFog(bool visible, Color color)
        {
            // Muestra una lamina de niebla que oculta el pasillo excepto siluetas y luz fuerte.
            if (hallwayFogRenderer == null)
            {
                return;
            }

            hallwayFogRenderer.enabled = visible;
            if (hallwayFogRenderer.material != null)
            {
                hallwayFogRenderer.material.color = color;
                if (hallwayFogRenderer.material.HasProperty("_BaseColor"))
                {
                    hallwayFogRenderer.material.SetColor("_BaseColor", color);
                }
            }
        }

        private void SpawnShadowReveal(float lifetime)
        {
            // Instancia la figura al otro lado de la puerta durante el reveal oscuro/crisis.
            if (shadowFigurePrefab == null || hallwayVisualPoint == null)
            {
                return;
            }

            activeReveal = Instantiate(shadowFigurePrefab, hallwayVisualPoint.position, hallwayVisualPoint.rotation);
            Destroy(activeReveal, lifetime);
        }

        private void ClearActiveReveal()
        {
            if (activeReveal != null)
            {
                Destroy(activeReveal);
                activeReveal = null;
            }
        }

        private IEnumerator CloseAfterReveal()
        {
            // Cierre temporizado independiente del tiempo normal de juego.
            yield return new WaitForSecondsRealtime(revealSeconds);
            closeRoutine = null;
            CloseDoor();
        }

        private void StopCloseRoutine()
        {
            if (closeRoutine == null)
            {
                return;
            }

            StopCoroutine(closeRoutine);
            closeRoutine = null;
        }
    }
}
