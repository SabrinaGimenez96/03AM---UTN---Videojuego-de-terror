using UnityEngine;

namespace HorrorPrototype.Events
{
    // TemporaryDestroy elimina automaticamente objetos de susto, marcadores o efectos temporales
    // para que la escena no acumule clones durante pruebas largas.
    public class TemporaryDestroy : MonoBehaviour
    {
        // Tiempo de vida del objeto instanciado antes de destruirse.
        [Min(0.1f)] public float lifetime = 2f;

        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
    }
}
