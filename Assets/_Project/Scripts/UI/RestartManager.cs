using UnityEngine;
using UnityEngine.SceneManagement;
using HorrorPrototype.Core;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorrorPrototype.UI
{
    // RestartManager reinicia la escena actual cuando la partida ya termino,
    // reutilizando barra espaciadora para hacer pruebas rapidas del prototipo.
    public class RestartManager : MonoBehaviour
    {
        private void Update()
        {
            // El usuario solicitó remover el reinicio con barra espaciadora, ahora solo se reinicia mediante los botones de la UI.
        }

        public void RestartCurrentScene()
        {
            // Restablece el tiempo por si acaso.
            Time.timeScale = 1f;
            // Recarga la escena activa sin cambiar Build Settings ni referencias.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static bool WasRestartPressed()
        {
            // Lee barra espaciadora con Input System o Input Manager clasico.
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
