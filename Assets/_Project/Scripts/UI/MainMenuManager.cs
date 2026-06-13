using UnityEngine;
using UnityEngine.SceneManagement;

namespace HorrorPrototype.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        // Este método se asignará al botón de "Jugar" o "Start"
        public void PlayGame()
        {
            // Restablecemos el tiempo por si el jugador viene de la pantalla de Game Over
            Time.timeScale = 1f;
            
            // Cargar la escena principal del juego. Asegúrate de que tu escena se llame exactamente así.
            SceneManager.LoadScene("MainPrototype"); 
        }

        // Este método se asignará al botón de "Salir" o "Quit"
        public void QuitGame()
        {
            Debug.Log("Cerrando el juego...");
            Application.Quit();
            
            // Esta línea extra es solo para que el botón funcione también mientras pruebas dentro de Unity
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
