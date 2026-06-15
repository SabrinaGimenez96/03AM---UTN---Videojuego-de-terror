using UnityEngine;
using UnityEditor;
using HorrorPrototype.Events;

namespace HorrorPrototype.Editor
{
    public class ZombiePreparer : MonoBehaviour
    {
        [MenuItem("Horror Game/Arreglar Zombie Automaticamente")]
        public static void FixZombie()
        {
            string originalPrefabPath = "Assets/Tensori/SkinlessZombie/Prefabs/skinless zombie.prefab";
            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(originalPrefabPath);

            if (originalPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "No se encontró el prefab original del zombie en: " + originalPrefabPath, "OK");
                return;
            }

            // Crear el material negro de sombra para URP
            string matPath = "Assets/_Project/Materials/Material_Sombra.mat";
            Material blackMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (blackMat == null)
            {
                System.IO.Directory.CreateDirectory("Assets/_Project/Materials");
                blackMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                blackMat.SetColor("_BaseColor", Color.black);
                AssetDatabase.CreateAsset(blackMat, matPath);
            }

            // Instanciar el prefab para modificarlo
            GameObject zombieInstance = (GameObject)PrefabUtility.InstantiatePrefab(originalPrefab);
            zombieInstance.name = "ZombieSombra_Listo";

            // Limpiar componentes indeseados (físicas, navegación, etc)
            var components = zombieInstance.GetComponentsInChildren<Component>(true);
            foreach (var comp in components)
            {
                if (comp is Collider || comp is Rigidbody || comp is UnityEngine.AI.NavMeshAgent)
                {
                    DestroyImmediate(comp);
                }
            }

            // Asegurarnos de que tenga CreepyCrawler y AudioSource
            if (zombieInstance.GetComponent<CreepyCrawler>() == null)
            {
                zombieInstance.AddComponent<CreepyCrawler>();
            }
            if (zombieInstance.GetComponent<AudioSource>() == null)
            {
                zombieInstance.AddComponent<AudioSource>();
            }

            // Asignar el Animator Controller para que no este tieso
            Animator animator = zombieInstance.GetComponent<Animator>();
            if (animator != null)
            {
                string controllerPath = "Assets/Tensori/SkinlessZombie/Art/Animations/Animator.controller";
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                if (controller != null)
                {
                    animator.runtimeAnimatorController = controller;
                }
            }

            // Pintarlo de negro puro
            var renderers = zombieInstance.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                Material[] mats = new Material[rend.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++)
                {
                    mats[i] = blackMat;
                }
                rend.sharedMaterials = mats;
            }

            // Guardar como nuestro propio prefab
            string newPrefabPath = "Assets/_Project/Prefabs/ZombieSombra_Listo.prefab";
            System.IO.Directory.CreateDirectory("Assets/_Project/Prefabs");
            GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(zombieInstance, newPrefabPath);
            DestroyImmediate(zombieInstance);

            // Intentar conectarlo automáticamente al HorrorEventManager de la escena
            HorrorEventManager manager = FindAnyObjectByType<HorrorEventManager>();
            if (manager != null)
            {
                // Usamos SerializedObject para modificar el campo privado/publico sin problemas
                SerializedObject so = new SerializedObject(manager);
                SerializedProperty prop = so.FindProperty("shadowFigurePrefab");
                if (prop != null)
                {
                    prop.objectReferenceValue = newPrefab;
                    so.ApplyModifiedProperties();
                    EditorUtility.DisplayDialog("¡Éxito!", "Zombie arreglado, convertido en sombra negra y conectado al juego automáticamente.", "Genial");
                }
                else
                {
                    EditorUtility.DisplayDialog("Éxito a medias", "Zombie arreglado y guardado en Prefabs. Pero tendrás que asignarlo a mano en el HorrorEventManager.", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("¡Éxito!", "Zombie arreglado y guardado en Prefabs. Ve al objeto Managers y asígnalo manualmente.", "OK");
            }
        }
    }
}
