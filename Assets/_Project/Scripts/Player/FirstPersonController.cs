using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorPrototype.Player
{
    [RequireComponent(typeof(CharacterController))]
    // FirstPersonController controla al jugador en primera persona: movimiento WASD,
    // gravedad simple, bloqueo de cursor y limites de mirada horizontal/vertical.
    public class FirstPersonController : MonoBehaviour
    {
        [Header("Movement")]
        // allowMovement se desactiva cuando el jugador esta acostado en cama.
        public bool allowMovement = true;
        public float moveSpeed = 3.2f;
        public float gravity = -18f;

        [Header("Look")]
        // cameraRoot es la camara hija: recibe el pitch vertical mientras el jugador rota en yaw.
        public Transform cameraRoot;
        public float mouseSensitivity = 0.24f;
        public float minPitch = -35f;
        public float maxPitch = 35f;
        public float yawLimit = 70f;

        private CharacterController controller;
        private float pitch;
        private float yaw;
        private float baseYaw;
        private float verticalVelocity;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            baseYaw = transform.eulerAngles.y;
            LockCursor();
        }

        private void Update()
        {
            // Tab libera/bloquea cursor para pruebas; Escape lo libera siempre.
            if (WasToggleCursorPressed())
            {
                ToggleCursor();
            }

            if (WasEscapePressed())
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Look();
            }

            if (allowMovement)
            {
                Move();
            }
        }

        private void Move()
        {
            // CharacterController aplica colisiones sin necesitar un Rigidbody en el jugador.
            Vector2 input = GetMoveInput();
            Vector3 move = transform.right * input.x + transform.forward * input.y;

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -1f;
            }

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;
            controller.Move(moveSpeed * Time.deltaTime * new Vector3(move.x, 0f, move.z) + Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void Look()
        {
            // La rotacion horizontal vive en el jugador y la vertical en la camara hija.
            Vector2 mouseDelta = GetMouseDelta() * mouseSensitivity;
            yaw = Mathf.Clamp(yaw + mouseDelta.x, -yawLimit, yawLimit);
            transform.rotation = Quaternion.Euler(0f, baseYaw + yaw, 0f);

            pitch = Mathf.Clamp(pitch - mouseDelta.y, minPitch, maxPitch);
            if (cameraRoot != null)
            {
                cameraRoot.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void EnableStandingMovement()
        {
            // Modo de pie: habilita caminar y permite mirar casi 180 grados alrededor.
            allowMovement = true;
            minPitch = -75f;
            maxPitch = 75f;
            yawLimit = 180f;
            yaw = 0f;
            pitch = 0f;
            baseYaw = transform.eulerAngles.y;
        }

        public void EnableBedLook()
        {
            // Modo cama: bloquea movimiento fisico, pero deja mirar alrededor desde la almohada.
            allowMovement = false;
            minPitch = -45f;
            maxPitch = 48f;
            yawLimit = 180f;
            yaw = 0f;
            pitch = 0f;
            baseYaw = transform.eulerAngles.y;
        }

        public void DisableLookAndMovement()
        {
            // Bloquea por completo el movimiento y fija la camara (usado en secuencias de muerte).
            allowMovement = false;
            yawLimit = 0f;
            minPitch = pitch;
            maxPitch = pitch;
        }

        public void LockCameraForward()
        {
            // Fija la camara en la direccion actual, pero permite seguir caminando.
            yawLimit = 0f;
            baseYaw = transform.eulerAngles.y;
            yaw = 0f;
            minPitch = pitch;
            maxPitch = pitch;
        }

        public bool IsLookingLeft()
        {
            // Si el yaw es negativo, esta mirando a la izquierda de su punto central (la puerta).
            return yaw < -15f;
        }

        private static void ToggleCursor()
        {
            bool shouldLock = Cursor.lockState != CursorLockMode.Locked;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !shouldLock;
        }

        private static Vector2 GetMoveInput()
        {
            // Lee teclado tanto con el Input System nuevo como con el Input Manager legado.
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) input.x -= 1f;
                if (Keyboard.current.dKey.isPressed) input.x += 1f;
                if (Keyboard.current.sKey.isPressed) input.y -= 1f;
                if (Keyboard.current.wKey.isPressed) input.y += 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            input.x += Input.GetAxisRaw("Horizontal");
            input.y += Input.GetAxisRaw("Vertical");
#endif

            return Vector2.ClampMagnitude(input, 1f);
        }

        private static Vector2 GetMouseDelta()
        {
            // Unifica lectura de mouse para que el controlador compile con ambos sistemas de input.
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue();
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#else
            return Vector2.zero;
#endif
        }

        private static bool WasEscapePressed()
        {
            // Detecta Escape sin depender de una configuracion especifica del proyecto.
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Escape);
#else
            return false;
#endif
        }

        private static bool WasToggleCursorPressed()
        {
            // Detecta Tab para alternar cursor durante pruebas en editor/build.
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.Tab);
#else
            return false;
#endif
        }

    }
}
