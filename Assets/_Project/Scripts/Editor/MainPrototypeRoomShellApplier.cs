using HorrorPrototype.Core;
using HorrorPrototype.Events;
using HorrorPrototype.Interaction;
using HorrorPrototype.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace HorrorPrototype.EditorTools
{
    // MainPrototypeRoomShellApplier migra MainPrototype al cuarto importado desde Blender:
    // reemplaza el greybox, crea la puerta funcional, instala props y aplica la ambientacion oscura.
    public static class MainPrototypeRoomShellApplier
    {
        private const string ScenePath = "Assets/_Project/Scenes/MainPrototype.unity";
        private const string ModelPath = "Assets/_Project/Models/Integration/Room_Shell.fbx";
        private const string ShadowPrefabPath = "Assets/_Project/Prefabs/PF_ShadowFigure.prefab";
        private const string MaterialFolder = "Assets/_Project/Materials/IntegrationLook";
        private const string GeneratedMeshFolder = "Assets/_Project/Models/Integration/Generated";
        private const string GeneratedTextureFolder = "Assets/_Project/Art/Textures/Generated";
        private const string ClockTexturePath = "Assets/_Project/Art/Textures/Props/T_ClockFace_PinkFloyd_Inner.png";
        private const string FrameTexturePath = "Assets/_Project/Art/Textures/Props/T_FramePicture_Horse_Crop.png";
        private const string BedModelPath = "Assets/_Project/Models/GameplayProps/Bed.fbx";
        private const string NightstandRightModelPath = "Assets/_Project/Models/GameplayProps/BedLeft.fbx";
        private const string NightstandLeftModelPath = "Assets/_Project/Models/GameplayProps/BedRight.fbx";
        private const string LampModelPath = "Assets/_Project/Models/GameplayProps/Lamp.fbx";
        private const string PhoneModelPath = "Assets/_Project/Models/GameplayProps/Phone.fbx";

        [MenuItem("Tools/Horror Prototype/Apply Room Shell To Main Prototype")]
        public static void Apply()
        {
            // Flujo principal del menu: abre MainPrototype y reconstruye el cuarto sin tocar scripts de runtime.
            EditorSceneManager.OpenScene(ScenePath);
            EnsureMaterialFolder();
            RemovePreviousVisualExperiment();
            RemoveGreyboxRoom();

            GameObject root = new GameObject("Main_RoomShellRoot");
            GameObject imported = InstantiateRoom(root.transform);
            AddStaticColliders(imported);

            GameObject gameplayDoor = BuildGameplayDoor(root.transform);
            DoorEscapeController doorEscape = ConfigureDoorController(gameplayDoor.transform, root.transform);
            BuildGameplayInteractables(root.transform);
            ConfigureManagers(gameplayDoor.transform, doorEscape);
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureLook();
            ApplyPropTextures(imported.transform);

            Selection.activeGameObject = gameplayDoor;
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Main Prototype RoomShell] MainPrototype ahora usa Room_Shell con puerta funcional y ambientacion.");
        }

        private static void RemovePreviousVisualExperiment()
        {
            // Borra luces, niebla y props generados por pruebas visuales anteriores.
            DestroyIfExists("MainLook_ImportedProps");
            DestroyIfExists("MainLook_MoonCold_WindowFill");
            DestroyIfExists("MainLook_DoorWarmSpill");
            DestroyIfExists("MainLook_LowFloorColdBounce");
            DestroyIfExists("Look_HallwayWarmSconce_A");
            DestroyIfExists("Look_HallwayWarmSconce_B");
            DestroyIfExists("Door_UnderGapLight_Main");
            DestroyIfExists("HallwayFogVeil_Main");
        }

        private static void RemoveGreyboxRoom()
        {
            // Elimina el cuarto base antes de instanciar el Room_Shell definitivo.
            DestroyIfExists("Greybox_Room");
            DestroyIfExists("Main_RoomShellRoot");
            DestroyIfExists("Directional Light");
        }

        private static void DestroyIfExists(string objectName)
        {
            // Elimina un objeto generado si existe en la escena activa.
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static GameObject InstantiateRoom(Transform parent)
        {
            // Instancia el FBX Room_Shell en el origen y lo agrupa bajo Main_RoomShellRoot.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (prefab == null)
            {
                Debug.LogError("[Main Prototype RoomShell] No se encontro el FBX en " + ModelPath);
                return new GameObject("Missing_Room_Shell");
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.name = "Imported_Room_Shell";
            instance.transform.SetParent(parent);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void AddStaticColliders(GameObject imported)
        {
            // Agrega MeshColliders a arquitectura estatica y oculta la puerta original del FBX.
            foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
            {
                if (IsDoorPart(renderer.transform))
                {
                    renderer.gameObject.SetActive(false);
                    continue;
                }

                MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
                if (meshFilter == null || meshFilter.sharedMesh == null || renderer.GetComponent<Collider>() != null)
                {
                    continue;
                }

                MeshCollider collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = meshFilter.sharedMesh;
            }
        }

        private static GameObject BuildGameplayDoor(Transform parent)
        {
            // Crea una puerta procedural alineada al hueco del modelo, con pivote real de apertura.
            Vector3 doorCenter = new Vector3(-2.54f, 1.05f, -5.685f);
            Vector3 doorSize = new Vector3(0.96f, 2.06f, 0.055f);
            Vector3 hingePosition = new Vector3(doorCenter.x - doorSize.x * 0.5f, doorCenter.y, doorCenter.z);

            GameObject pivot = new GameObject("Gameplay_DoorPivot");
            pivot.transform.SetParent(parent);
            pivot.transform.position = hingePosition;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Gameplay_Door";
            visual.transform.SetParent(pivot.transform);
            visual.transform.position = doorCenter;
            visual.transform.rotation = Quaternion.identity;
            visual.transform.localScale = doorSize;
            ApplyMaterial(visual, GetLightDoorWoodMaterial());
            AddDoorHandles(visual.transform, "Gameplay_Door");

            InteractableObject interactable = pivot.AddComponent<InteractableObject>();
            interactable.displayName = "Abrir puerta";
            interactable.actionType = ActionType.Door;
            interactable.feedbackTarget = pivot.transform;

            BoxCollider interactionCollider = pivot.AddComponent<BoxCollider>();
            interactionCollider.center = pivot.transform.InverseTransformPoint(doorCenter);
            interactionCollider.size = new Vector3(1.15f, 2.2f, 0.28f);
            interactionCollider.isTrigger = true;

            GameObject interactionZone = new GameObject("Gameplay_DoorInteractionZone");
            interactionZone.transform.SetParent(parent);
            interactionZone.transform.position = doorCenter;

            InteractableObject zoneInteractable = interactionZone.AddComponent<InteractableObject>();
            zoneInteractable.displayName = "Abrir puerta";
            zoneInteractable.actionType = ActionType.Door;
            zoneInteractable.feedbackTarget = pivot.transform;

            BoxCollider zoneCollider = interactionZone.AddComponent<BoxCollider>();
            zoneCollider.center = Vector3.zero;
            zoneCollider.size = new Vector3(1.35f, 2.25f, 1.1f);
            zoneCollider.isTrigger = true;

            BuildDoorFrame(parent, doorCenter, doorSize, "Gameplay_Door");
            BuildUnderDoorLight(parent, doorCenter, doorSize);
            return pivot;
        }

        private static void BuildUnderDoorLight(Transform parent, Vector3 doorCenter, Vector3 doorSize)
        {
            // Agrega una franja de luz bajo la puerta sin iluminar directamente todo el cuarto.
            GameObject slit = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slit.name = "Door_UnderGapLight_Main";
            slit.transform.SetParent(parent);
            slit.transform.position = new Vector3(doorCenter.x, 0.035f, doorCenter.z - 0.035f);
            slit.transform.localScale = new Vector3(doorSize.x * 0.84f, 0.01f, 0.022f);
            ApplyMaterial(slit, GetEmissiveUnlitMaterial("ML_Door_UnderGap_WarmLight", new Color(1f, 0.48f, 0.12f), 0.85f));
            Collider collider = slit.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            GameObject spill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spill.name = "Door_UnderGapSpill_Main";
            spill.transform.SetParent(parent);
            spill.transform.position = new Vector3(doorCenter.x, 0.012f, doorCenter.z + 0.055f);
            spill.transform.localScale = new Vector3(doorSize.x * 0.68f, 0.004f, 0.095f);
            ApplyMaterial(spill, GetEmissiveUnlitMaterial("ML_Door_UnderGap_FloorSpill", new Color(0.55f, 0.22f, 0.045f), 0.55f));
            Collider spillCollider = spill.GetComponent<Collider>();
            if (spillCollider != null)
            {
                Object.DestroyImmediate(spillCollider);
            }
        }

        private static void ConfigureHallwayExitDoor(GameObject hallwayExitDoor)
        {
            // Mantiene solo la puerta final del pasillo: bloquea el limite sin accion ni texto.
            BoxCollider collider = hallwayExitDoor.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = hallwayExitDoor.AddComponent<BoxCollider>();
            }

            collider.isTrigger = false;
            collider.center = Vector3.zero;
            collider.size = Vector3.one;

            InteractableObject interactable = hallwayExitDoor.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                Object.DestroyImmediate(interactable);
            }
        }

        private static void BuildDoorFrame(Transform parent, Vector3 doorCenter, Vector3 doorSize, string namePrefix)
        {
            // Construye el marco claro alrededor de la puerta funcional.
            Material frame = GetLightDoorWoodMaterial();
            float sideHeight = doorSize.y + 0.16f;
            float sideThickness = 0.08f;
            float topHeight = 0.1f;
            float frameDepth = 0.12f;
            float halfWidth = doorSize.x * 0.5f + sideThickness * 0.5f;

            CreateFramePiece(namePrefix + "Frame_Left", parent, new Vector3(doorCenter.x - halfWidth, doorCenter.y, doorCenter.z), new Vector3(sideThickness, sideHeight, frameDepth), frame);
            CreateFramePiece(namePrefix + "Frame_Right", parent, new Vector3(doorCenter.x + halfWidth, doorCenter.y, doorCenter.z), new Vector3(sideThickness, sideHeight, frameDepth), frame);
            CreateFramePiece(namePrefix + "Frame_Top", parent, new Vector3(doorCenter.x, doorCenter.y + doorSize.y * 0.5f + topHeight * 0.5f, doorCenter.z), new Vector3(doorSize.x + sideThickness * 2f, topHeight, frameDepth), frame);
        }

        private static void CreateFramePiece(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            // Crea cada pieza rectangular del marco de puerta.
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(parent);
            piece.transform.position = position;
            piece.transform.localScale = scale;
            ApplyMaterial(piece, material);
        }

        private static void AddDoorHandles(Transform door, string namePrefix)
        {
            // Coloca manijas de bronce en ambos lados de la puerta.
            Material handle = GetBronzeMetalMaterial();
            CreateHandle(namePrefix + "Handle_Front", door, new Vector3(0.28f, 0f, -0.62f), handle);
            CreateHandle(namePrefix + "Handle_Back", door, new Vector3(0.28f, 0f, 0.62f), handle);
        }

        private static void CreateHandle(string name, Transform parent, Vector3 localPosition, Material material)
        {
            // Crea una manija esferica simple con material metalico.
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            handle.name = name;
            handle.transform.SetParent(parent);
            handle.transform.localPosition = localPosition;
            handle.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            ApplyMaterial(handle, material);
        }

        private static DoorEscapeController ConfigureDoorController(Transform doorPivot, Transform parent)
        {
            // Conecta la puerta al controlador de escape y prepara luz, fondo y niebla del pasillo.
            DoorEscapeController controller = Object.FindAnyObjectByType<DoorEscapeController>();
            if (controller == null)
            {
                GameObject controllerObject = new GameObject("DoorEscape_MainController");
                controllerObject.transform.SetParent(parent);
                controller = controllerObject.AddComponent<DoorEscapeController>();
            }

            controller.door = doorPivot;
            controller.openYaw = -92f;
            controller.openOffset = Vector3.zero;
            controller.revealSeconds = 2.5f;

            DestroyIfPresent("HallwayEscapeLight_Main");
            DestroyIfPresent("HallwayRevealBack_Main");
            DestroyIfPresent("HallwayFogVeil_Main");
            controller.hallwayLight = null;
            controller.hallwayBackRenderer = null;
            controller.hallwayFogRenderer = null;

            BuildStaticHallwayExitDoor(parent);

            GameObject point = GetOrCreate("HallwayVisualPoint_Main", parent);
            point.transform.position = new Vector3(-2.45f, 1.05f, -7.6f);
            controller.hallwayVisualPoint = point.transform;
            controller.shadowFigurePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShadowPrefabPath);
            return controller;
        }

        private static void BuildStaticHallwayExitDoor(Transform parent)
        {
            // Replica la puerta del dormitorio en el hueco real del fondo del pasillo, sin interaccion.
            Vector3 doorCenter = new Vector3(-2.514f, 1.05f, -9.633f);
            Vector3 doorSize = new Vector3(0.96f, 2.06f, 0.055f);

            GameObject hallwayExitDoor = GetOrCreatePrimitive("HallwayExitDoor_Final", parent);
            hallwayExitDoor.transform.position = doorCenter;
            hallwayExitDoor.transform.rotation = Quaternion.identity;
            hallwayExitDoor.transform.localScale = doorSize;
            ApplyMaterial(hallwayExitDoor, GetLightDoorWoodMaterial());
            ConfigureHallwayExitDoor(hallwayExitDoor);
            AddDoorHandles(hallwayExitDoor.transform, "HallwayExitDoor");

            BuildDoorFrame(parent, doorCenter, doorSize, "HallwayExitDoor");
        }

        private static void ConfigureManagers(Transform doorPivot, DoorEscapeController doorEscape)
        {
            // Actualiza el manager paranormal con nuevos puntos de sombra, puerta, celular y probabilidades.
            HorrorEventManager horror = Object.FindAnyObjectByType<HorrorEventManager>();
            if (horror != null)
            {
                horror.hallwayVisualPoint = doorEscape.hallwayVisualPoint;
                horror.doorTarget = doorPivot;
                horror.shadowFigurePrefab = doorEscape.shadowFigurePrefab;
                horror.lampLight = FindLampLight();
                horror.minDelay = 8f;
                horror.maxDelay = 15f;
                horror.soundEventChance = 0.18f;
                horror.shadowEventChance = 0.42f;
                horror.lightFlickerEventChance = 0.12f;
                horror.doorKnockEventChance = 0.16f;
                horror.phoneGlitchEventChance = 0.12f;
                horror.shadowSpawnPoints = BuildShadowSpawnPoints();
                GameObject phone = GameObject.Find("Celular");
                if (phone != null)
                {
                    horror.phoneTarget = phone.transform;
                    horror.phoneAudioSource = phone.GetComponent<AudioSource>();
                }
            }
        }

        private static Transform[] BuildShadowSpawnPoints()
        {
            // Crea puntos validos dentro del cuarto para apariciones visibles del fantasma.
            Vector3[] positions =
            {
                new Vector3(-4.28f, 0.82f, -4.68f),
                new Vector3(-0.72f, 0.82f, -4.68f),
                new Vector3(-2.45f, 0.82f, -5.42f)
            };

            Transform[] points = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = GetOrCreate("ShadowSpawn_Room_" + (i + 1), null);
                point.transform.position = positions[i];
                Vector3 lookTarget = new Vector3(-2.45f, 0.95f, -0.85f);
                point.transform.rotation = Quaternion.LookRotation((lookTarget - positions[i]).normalized, Vector3.up);
                points[i] = point.transform;
            }

            return points;
        }

        private static void BuildGameplayInteractables(Transform parent)
        {
            // Sustituye objetos base por cama, veladores, celular y lampara importados o fallbacks.
            Material bedMaterial = GetMaterial("ML_Main_Bed_DarkFabric", new Color(0.05f, 0.045f, 0.052f), 0.18f);
            Material woodMaterial = GetMaterial("ML_Main_Nightstand", new Color(0.095f, 0.071f, 0.052f), 0.22f);
            Material lampMaterial = GetMaterial("ML_Main_LampWarmBase", new Color(0.31f, 0.24f, 0.16f), 0.28f);
            Material phoneMaterial = GetMaterial("ML_Main_PhoneBlack", new Color(0.01f, 0.012f, 0.018f), 0.12f);

            GameObject bed = InstantiatePropModel(BedModelPath, "Bed_Ignore", parent);
            if (bed == null)
            {
                bed = CreateCube("Bed_Ignore", parent, new Vector3(-2.45f, 0.22f, -1.25f), new Vector3(1.35f, 0.35f, 1.8f), bedMaterial);
            }
            else
            {
                MoveBoundsCenter(bed, new Vector3(-2.45f, 0.51f, -1.17f));
            }

            ApplyBedMaterials(bed);
            AddBoundsCollider(bed, true, new Vector3(0.08f, 0.05f, 0.08f));
            AddInteractable(bed, "Regresar a cama", ActionType.Ignore, bed.transform);

            Bounds bedBounds = GetRendererBounds(bed);
            GameObject rightNightstand = InstantiatePropModel(NightstandRightModelPath, "Velador_Derecho", parent);
            if (rightNightstand == null)
            {
                rightNightstand = CreateCube("Velador_Derecho", parent, new Vector3(-0.8f, 0.45f, -0.24f), new Vector3(0.56f, 0.5f, 0.56f), woodMaterial);
            }

            GameObject leftNightstand = InstantiatePropModel(NightstandLeftModelPath, "Velador_Izquierdo", parent);
            if (leftNightstand == null)
            {
                leftNightstand = CreateCube("Velador_Izquierdo", parent, new Vector3(-4.1f, 0.45f, -0.24f), new Vector3(0.56f, 0.5f, 0.56f), woodMaterial);
            }

            Bounds rightBounds = GetRendererBounds(rightNightstand);
            Bounds leftBounds = GetRendererBounds(leftNightstand);
            float standGap = 0.05f;
            float nightstandZ = bedBounds.max.z - 0.21f;
            MoveBoundsCenter(rightNightstand, new Vector3(bedBounds.max.x + standGap + rightBounds.extents.x, rightBounds.center.y, nightstandZ));
            MoveBoundsCenter(leftNightstand, new Vector3(bedBounds.min.x - standGap - leftBounds.extents.x, leftBounds.center.y, nightstandZ));
            rightBounds = GetRendererBounds(rightNightstand);
            leftBounds = GetRendererBounds(leftNightstand);
            ApplyNightstandMaterials(rightNightstand);
            ApplyNightstandMaterials(leftNightstand);

            GameObject lamp = InstantiatePropModel(LampModelPath, "Lampara", parent);
            if (lamp == null)
            {
                lamp = CreateCube("Lampara", parent, rightBounds.center + new Vector3(0f, 0.55f, 0f), new Vector3(0.22f, 0.55f, 0.22f), lampMaterial);
            }
            else
            {
                Bounds lampBounds = GetRendererBounds(lamp);
                MoveBoundsCenter(lamp, new Vector3(rightBounds.center.x, rightBounds.max.y + lampBounds.extents.y + 0.02f, rightBounds.center.z));
            }

            ApplyLampMaterials(lamp);
            BoxCollider lampInteraction = AddBoundsCollider(lamp, true, new Vector3(0.62f, 0.28f, 0.62f));
            lampInteraction.center += new Vector3(0f, -0.04f, 0f);
            Light lampLight = lamp.AddComponent<Light>();
            lampLight.type = LightType.Point;
            lampLight.enabled = false;
            lampLight.color = new Color(1f, 0.68f, 0.34f);
            lampLight.range = 4.2f;
            lampLight.intensity = 1.35f;
            AddInteractable(lamp, "Encender lampara", ActionType.Lamp, lamp.transform);

            GameObject phone = InstantiatePropModel(PhoneModelPath, "Celular", parent);
            if (phone == null)
            {
                phone = CreateCube("Celular", parent, leftBounds.center + new Vector3(0f, 0.29f, 0f), new Vector3(0.16f, 0.03f, 0.32f), phoneMaterial);
            }
            else
            {
                Bounds phoneBounds = GetRendererBounds(phone);
                MoveBoundsCenter(phone, new Vector3(leftBounds.center.x, leftBounds.max.y + phoneBounds.extents.y + 0.025f, leftBounds.center.z));
            }

            ApplyPhoneMaterials(phone);
            AddBoundsCollider(phone, true, new Vector3(0.14f, 0.08f, 0.14f));
            if (phone.GetComponent<AudioSource>() == null)
            {
                phone.AddComponent<AudioSource>();
            }

            AddInteractable(phone, "Tomar celular", ActionType.Phone, phone.transform);
            AddBoundsCollider(leftNightstand, true, new Vector3(0.16f, 0.08f, 0.16f));
            AddInteractable(leftNightstand, "Dejar celular", ActionType.Phone, phone.transform);
        }

        private static GameObject InstantiatePropModel(string assetPath, string name, Transform parent)
        {
            // Instancia el modelo FBX del prop; si no existe, devuelve null para usar cubo fallback.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning("[Main Prototype RoomShell] No se encontro el modelo de gameplay: " + assetPath);
                return null;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.name = name;
            instance.transform.SetParent(parent, true);
            return instance;
        }

        private static Bounds GetRendererBounds(GameObject target)
        {
            // Calcula limites visuales combinados para ubicar modelos aunque tengan pivotes raros.
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(target.transform.position, Vector3.one * 0.1f);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void MoveBoundsCenter(GameObject target, Vector3 desiredCenter)
        {
            // Mueve un objeto usando su centro visual, no el pivote importado.
            Bounds bounds = GetRendererBounds(target);
            target.transform.position += desiredCenter - bounds.center;
        }

        private static BoxCollider AddBoundsCollider(GameObject target, bool isTrigger, Vector3 padding)
        {
            // Genera un collider envolvente para interaccion o seleccion con margen configurable.
            Bounds bounds = GetRendererBounds(target);
            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = target.AddComponent<BoxCollider>();
            }

            collider.center = target.transform.InverseTransformPoint(bounds.center);
            collider.size = bounds.size + padding;
            collider.isTrigger = isTrigger;
            return collider;
        }

        private static void ApplyBedMaterials(GameObject bed)
        {
            // Aplica telas oscuras, sabanas, colcha y almohadas con materiales no emisivos.
            Material headboard = GetTexturedMaterial("ML_Bed_DarkPaddedHeadboard", GetOrCreateFabricTexture("T_Bed_DarkHeadboard", new Color(0.035f, 0.043f, 0.048f), new Color(0.12f, 0.14f, 0.15f), 22f), new Vector2(2f, 2f), 0.38f, 0f);
            Material sheet = GetTexturedMaterial("ML_Bed_LightGraySheet", GetOrCreateFabricTexture("T_Bed_LightGraySheet", new Color(0.45f, 0.51f, 0.52f), new Color(0.78f, 0.82f, 0.8f), 34f), new Vector2(3f, 3f), 0.28f, 0f);
            Material quilt = GetTexturedMaterial("ML_Bed_DarkGrayQuilt", GetOrCreateFabricTexture("T_Bed_DarkGrayQuilt", new Color(0.18f, 0.24f, 0.25f), new Color(0.43f, 0.49f, 0.48f), 28f), new Vector2(3.5f, 3.5f), 0.32f, 0f);
            Material pillow = GetTexturedMaterial("ML_Bed_PillowGrayFabric", GetOrCreateFabricTexture("T_Bed_PillowGrayFabric", new Color(0.54f, 0.59f, 0.59f), new Color(0.84f, 0.87f, 0.85f), 42f), new Vector2(2f, 2f), 0.26f, 0f);
            MakeTextureReadable(headboard, new Color(0.06f, 0.07f, 0.075f));
            MakeTextureReadable(sheet, new Color(0.36f, 0.39f, 0.38f));
            MakeTextureReadable(quilt, new Color(0.20f, 0.25f, 0.25f));
            MakeTextureReadable(pillow, new Color(0.40f, 0.43f, 0.41f));
            Material metal = GetMaterial("ML_Bed_SubtleDarkMetal", new Color(0.06f, 0.065f, 0.067f), 0.45f);
            if (metal.HasProperty("_Metallic")) metal.SetFloat("_Metallic", 0.55f);

            foreach (Renderer renderer in bed.GetComponentsInChildren<Renderer>(true))
            {
                string lower = renderer.gameObject.name.ToLowerInvariant();
                Material selected = sheet;
                if (lower.Contains("wall_fabric")) selected = headboard;
                else if (lower.Contains("cylinder")) selected = metal;
                else if (lower.Contains("021") || lower.Contains("016") || lower.Contains("015")) selected = quilt;
                else if (lower.Contains("017") || lower.Contains("018") || lower.Contains("019") || lower.Contains("020")) selected = pillow;
                AssignMaterial(renderer, selected);
            }
        }

        private static void ApplyNightstandMaterials(GameObject nightstand)
        {
            // Aplica material de velador con gavetas tipo madera y cuerpo claro atenuado.
            Material material = GetTexturedMaterial("ML_Nightstand_WhiteWoodDrawer", GetOrCreateNightstandTexture(), new Vector2(1f, 1f), 0.34f, 0f);
            MakeTextureReadable(material, new Color(0.48f, 0.46f, 0.42f));
            foreach (Renderer renderer in nightstand.GetComponentsInChildren<Renderer>(true))
            {
                AssignMaterial(renderer, material);
            }
        }

        private static void ApplyLampMaterials(GameObject lamp)
        {
            // Aplica base de madera, pantalla de tela, metal y bombillo sin luz emisiva permanente.
            Material body = GetTexturedMaterial("ML_Lamp_WarmWoodBody", GetOrCreateWoodTexture("T_Lamp_WarmWoodBody", new Color(0.32f, 0.17f, 0.065f), new Color(0.66f, 0.39f, 0.16f), new Color(0.11f, 0.055f, 0.024f), 22f), new Vector2(1.2f, 2.5f), 0.36f, 0f);
            Material shade = GetTexturedMaterial("ML_Lamp_WarmFabricShade", GetOrCreateFabricTexture("T_Lamp_WarmFabricShade", new Color(0.73f, 0.67f, 0.57f), new Color(1f, 0.93f, 0.82f), 48f), new Vector2(2f, 1f), 0.22f, 0f);
            MakeTextureReadable(body, new Color(0.34f, 0.20f, 0.09f));
            MakeTextureReadable(shade, new Color(0.48f, 0.43f, 0.36f));
            Material metal = GetBronzeMetalMaterial();
            Material bulb = GetMaterial("ML_Lamp_Bulb_WarmGlow", new Color(1f, 0.78f, 0.42f), 0.3f);
            SetMaterialEmission(bulb, Color.black);
            Material wire = GetMaterial("ML_Lamp_DarkWire", new Color(0.015f, 0.012f, 0.01f), 0.25f);

            foreach (Renderer renderer in lamp.GetComponentsInChildren<Renderer>(true))
            {
                string lower = renderer.gameObject.name.ToLowerInvariant();
                if (lower.Contains("cover")) AssignMaterial(renderer, shade);
                else if (lower.Contains("bulb")) AssignMaterial(renderer, bulb);
                else if (lower.Contains("wire")) AssignMaterial(renderer, wire);
                else AssignRendererMaterials(renderer, new[] { body, metal });
            }
        }

        private static void ApplyPhoneMaterials(GameObject phone)
        {
            // Aplica pantalla con textura/emision controlada, cuerpo oscuro y detalles metalicos.
            Material screen = GetTexturedMaterial("ML_PhoneScreen_EmissiveStandby", GetOrCreatePhoneScreenTexture(), new Vector2(1f, 1f), 0.78f, 0f);
            SetMaterialEmission(screen, Color.black);
            Material back = GetMaterial("ML_Phone_BackDarkGlass", new Color(0.02f, 0.018f, 0.023f), 0.72f);
            Material caseMat = GetMaterial("ML_Phone_BlackCase", new Color(0.006f, 0.006f, 0.008f), 0.45f);
            Material detail = GetMaterial("ML_Phone_DimMetalDetail", new Color(0.09f, 0.088f, 0.09f), 0.48f);
            if (back.HasProperty("_Metallic")) back.SetFloat("_Metallic", 0.15f);
            if (caseMat.HasProperty("_Metallic")) caseMat.SetFloat("_Metallic", 0.1f);
            if (detail.HasProperty("_Metallic")) detail.SetFloat("_Metallic", 0.35f);

            foreach (Renderer renderer in phone.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.sharedMaterials.Length >= 6)
                {
                    AssignRendererMaterials(renderer, new[] { screen, back, caseMat, caseMat, detail, detail });
                }
                else
                {
                    AssignMaterial(renderer, screen);
                }
            }
        }

        private static void AssignMaterial(Renderer renderer, Material material)
        {
            // Reemplaza todos los slots del renderer por un unico material.
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        private static void AssignRendererMaterials(Renderer renderer, Material[] sourceMaterials)
        {
            // Asigna un arreglo de materiales respetando la cantidad de slots del mesh.
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = sourceMaterials[Mathf.Min(i, sourceMaterials.Length - 1)];
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            // Crea un prop procedural simple cuando no hay modelo importado disponible.
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;
            ApplyMaterial(cube, material);
            return cube;
        }

        private static void AddInteractable(GameObject target, string displayName, ActionType actionType, Transform feedbackTarget)
        {
            // Registra el objeto como interactuable para que el Raycast del jugador lo use.
            InteractableObject interactable = target.AddComponent<InteractableObject>();
            interactable.displayName = displayName;
            interactable.actionType = actionType;
            interactable.feedbackTarget = feedbackTarget;
        }

        private static void ConfigurePlayer()
        {
            // Ajusta posicion de inicio, alturas de camara y distancia de interaccion del jugador.
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                return;
            }

            player.transform.SetPositionAndRotation(new Vector3(-2.45f, 0.05f, -0.58f), Quaternion.Euler(0f, 180f, 0f));
            PlayerActionFeedback feedback = player.GetComponent<PlayerActionFeedback>();
            if (feedback != null)
            {
                feedback.bedPosition = new Vector3(-2.45f, 0.05f, -0.58f);
                feedback.standPosition = new Vector3(-2.45f, 0.05f, -2.05f);
                feedback.bedEulerAngles = new Vector3(0f, 180f, 0f);
                feedback.standEulerAngles = new Vector3(0f, 180f, 0f);
                feedback.bedCameraLocalPosition = new Vector3(0f, 0.96f, 0f);
                feedback.standCameraLocalPosition = new Vector3(0f, 1.72f, 0f);
            }

            FirstPersonController firstPerson = player.GetComponent<FirstPersonController>();
            if (firstPerson != null)
            {
                firstPerson.EnableBedLook();
            }

            PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
            Camera camera = player.GetComponentInChildren<Camera>(true);
            if (interactor != null)
            {
                interactor.playerCamera = camera;
                interactor.maxDistance = 6f;
            }
        }

        private static void ConfigureCamera()
        {
            // Configura la camara FPS para una lectura nocturna con fondo oscuro.
            GameObject cameraObject = GameObject.Find("FirstPersonCamera");
            Camera camera = cameraObject != null ? cameraObject.GetComponent<Camera>() : Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.transform.localPosition = new Vector3(0f, 0.96f, 0f);
            camera.transform.localRotation = Quaternion.identity;
            camera.backgroundColor = new Color(0.005f, 0.007f, 0.011f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 64f;
        }

        private static void ConfigureLook()
        {
            // Aplica materiales de pared/techo/piso y luces muy tenues para generar penumbra.
            Material wall = GetFlatColorMaterial("ML_Wall_ColdBlueGray", new Color(0.045f, 0.054f, 0.064f));
            Material floor = GetFloorWoodMaterial();
            Material ceiling = wall;
            Material baseboard = GetBaseboardWoodMaterial();
            Material clock = GetMaterial("ML_Clock_DimIvory", new Color(0.62f, 0.66f, 0.65f), 0.38f);
            Material frame = GetMaterial("ML_Frame_DarkTrim", new Color(0.035f, 0.038f, 0.04f), 0.35f);
            Material door = GetLightDoorWoodMaterial();
            Material handle = GetBronzeMetalMaterial();

            GameObject imported = GameObject.Find("Imported_Room_Shell");
            if (imported != null)
            {
                foreach (Renderer renderer in imported.GetComponentsInChildren<Renderer>(true))
                {
                    string objectName = renderer.gameObject.name.ToLowerInvariant();
                    Material selected = wall;
                    if (objectName.Contains("floor")) selected = floor;
                    else if (objectName.Contains("ceiling")) selected = ceiling;
                    else if (objectName.Contains("baseboard")) selected = baseboard;
                    else if (objectName.Contains("clock") || objectName.Contains("face") || objectName.Contains("border") || objectName.Contains("hands")) selected = clock;
                    else if (objectName.Contains("doorframe")) selected = door;
                    else if (objectName.Contains("frame") || objectName.Contains("picture") || objectName.Contains("glass")) selected = frame;
                    else if (objectName.Contains("handle")) selected = handle;
                    else if (objectName.Contains("door")) selected = door;

                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = selected;
                    }

                    renderer.sharedMaterials = materials;
                }

                AssignWoodProjectedUvs(imported.transform);
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.0004f, 0.0006f, 0.0010f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.01f, 0.013f, 0.018f);
            RenderSettings.fogDensity = 0.072f;

            EnsurePointLight("Look_MoonCold_WindowFill", new Vector3(-4.75f, 2.25f, -2.65f), new Color(0.035f, 0.055f, 0.095f), 0.025f, 3.2f);
            EnsurePointLight("Look_RoomLowColdFloorBounce", new Vector3(-2.45f, 0.45f, -1.55f), new Color(0.012f, 0.014f, 0.018f), 0.006f, 2.1f);
            EnsurePointLight("Look_FloorWall_GrazingContrast", new Vector3(-3.50f, 0.20f, -3.15f), new Color(0.012f, 0.014f, 0.018f), 0.004f, 2.0f);
            EnsurePointLight("Look_FloorWood_ReadLight", new Vector3(-2.45f, 0.35f, -0.95f), new Color(0.010f, 0.012f, 0.016f), 0.003f, 1.8f);
            EnsurePointLight("Look_DoorWood_LowRim", new Vector3(-2.70f, 0.65f, -5.12f), new Color(0.015f, 0.016f, 0.020f), 0.006f, 1.8f);
        }

        private static void AssignWoodProjectedUvs(Transform importedRoot)
        {
            // Genera UVs proyectadas en piso y zocalos para que la madera se lea de forma uniforme.
            EnsureGeneratedMeshFolder();
            foreach (MeshFilter filter in importedRoot.GetComponentsInChildren<MeshFilter>(true))
            {
                string lower = filter.gameObject.name.ToLowerInvariant();
                if (!lower.Contains("floor") && !lower.Contains("baseboard"))
                {
                    continue;
                }

                if (filter.sharedMesh == null)
                {
                    continue;
                }

                string meshPath = GeneratedMeshFolder + "/WoodUV_" + filter.gameObject.name.Replace(".", "_") + ".asset";
                Mesh source = filter.sharedMesh;
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null)
                {
                    mesh = new Mesh { name = "WoodUV_" + filter.gameObject.name.Replace(".", "_") };
                    AssetDatabase.CreateAsset(mesh, meshPath);
                }

                mesh.Clear();
                mesh.vertices = source.vertices;
                mesh.normals = source.normals;
                mesh.tangents = source.tangents;
                mesh.colors = source.colors;
                mesh.subMeshCount = source.subMeshCount;
                for (int i = 0; i < source.subMeshCount; i++)
                {
                    mesh.SetTriangles(source.GetTriangles(i), i);
                }

                Bounds bounds = source.bounds;
                Vector3 size = bounds.size;
                Vector2[] uvs = new Vector2[source.vertexCount];
                for (int i = 0; i < source.vertexCount; i++)
                {
                    Vector3 vertex = source.vertices[i];
                    float u;
                    float v;
                    if (lower.Contains("floor"))
                    {
                        u = size.x > 0.001f ? (vertex.x - bounds.min.x) / size.x : 0f;
                        v = size.z > 0.001f ? (vertex.z - bounds.min.z) / size.z : 0f;
                    }
                    else
                    {
                        u = size.x > 0.001f ? (vertex.x - bounds.min.x) / size.x : (vertex.z - bounds.min.z) / Mathf.Max(size.z, 0.001f);
                        v = size.y > 0.001f ? (vertex.y - bounds.min.y) / size.y : 0f;
                    }

                    uvs[i] = new Vector2(u, v);
                }

                mesh.uv = uvs;
                mesh.RecalculateBounds();
                if (mesh.normals == null || mesh.normals.Length == 0)
                {
                    mesh.RecalculateNormals();
                }

                EditorUtility.SetDirty(mesh);
                filter.sharedMesh = mesh;
                EditorUtility.SetDirty(filter);
            }
        }

        private static void ApplyPropTextures(Transform importedRoot)
        {
            // Aplica texturas del reloj y cuadro directamente sobre sus materiales/mallas.
            Texture2D clockTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ClockTexturePath);
            Texture2D frameTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FrameTexturePath);
            if (clockTexture == null || frameTexture == null)
            {
                Debug.LogWarning("[Main Prototype RoomShell] No se cargaron texturas de reloj/cuadro. Clock=" + (clockTexture != null) + " Frame=" + (frameTexture != null));
                return;
            }

            Material clockFace = CreatePropMaterial("ML_ClockFace_PinkFloydPreview", Color.white, clockTexture, 0.45f);
            Material picture = CreatePropMaterial("ML_FramePicture_HorsePreview", Color.white, frameTexture, 0.35f);
            Material clockBorder = CreatePropMaterial("ML_ClockBorder_AgedBrass", new Color(0.58f, 0.43f, 0.19f), null, 0.45f);
            Material clockHands = CreatePropMaterial("ML_ClockHands_DimWhite", new Color(0.82f, 0.9f, 0.92f), null, 0.3f);
            Material frame = CreatePropMaterial("ML_Frame_DarkCharcoal", new Color(0.035f, 0.038f, 0.04f), null, 0.35f);
            Material glass = CreatePropMaterial("ML_FrameGlass_FaintReflection", new Color(0.58f, 0.66f, 0.72f, 0.45f), null, 0.75f);

            AssignByName(importedRoot, "Face", clockFace);
            AssignClockFaceMesh(importedRoot, "Face", "MainClockFace_ProjectUV");
            AssignByName(importedRoot, "Border", clockBorder);
            AssignByName(importedRoot, "Hands", clockHands);
            AssignByName(importedRoot, "Picture", picture);
            AssignByName(importedRoot, "Frame", frame);
            AssignByName(importedRoot, "Glass", glass);
            RemoveLegacyOverlay("Clock_TextureOverlay");
            RemoveLegacyOverlay("Frame_TextureOverlay");
            DisableRenderer(importedRoot, "Glass");
        }

        private static void AssignByName(Transform root, string objectName, Material material)
        {
            // Reemplaza materiales en piezas del FBX encontradas por nombre exacto.
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != objectName)
                {
                    continue;
                }

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer == null)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = material;
                }

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void AssignClockFaceMesh(Transform root, string objectName, string generatedMeshName)
        {
            // Crea una cara con UVs circulares para que la textura del reloj no se deforme.
            EnsureGeneratedMeshFolder();
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != objectName)
                {
                    continue;
                }

                MeshFilter filter = child.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                string path = GeneratedMeshFolder + "/" + generatedMeshName + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null)
                {
                    Mesh source = filter.sharedMesh;
                    Bounds bounds = source.bounds;
                    mesh = new Mesh { name = generatedMeshName };
                    Vector3[] vertices = new Vector3[9];
                    Vector2[] uvs = new Vector2[9];
                    int[] triangles = new int[24];
                    float radiusX = bounds.extents.x * 0.74f;
                    float radiusZ = bounds.extents.z * 0.74f;
                    float y = bounds.max.y + 0.002f;

                    vertices[0] = new Vector3(bounds.center.x, y, bounds.center.z);
                    uvs[0] = new Vector2(0.5f, 0.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = (Mathf.PI * 2f * i / 8f) + Mathf.PI / 8f;
                        float x = bounds.center.x + Mathf.Cos(angle) * radiusX;
                        float z = bounds.center.z + Mathf.Sin(angle) * radiusZ;
                        float u = (x - (bounds.center.x - radiusX)) / (radiusX * 2f);
                        float v = (z - (bounds.center.z - radiusZ)) / (radiusZ * 2f);
                        vertices[i + 1] = new Vector3(x, y, z);
                        uvs[i + 1] = new Vector2(1f - u, 1f - v);
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        int next = i == 7 ? 1 : i + 2;
                        triangles[i * 3] = 0;
                        triangles[i * 3 + 1] = next;
                        triangles[i * 3 + 2] = i + 1;
                    }

                    mesh.vertices = vertices;
                    mesh.uv = uvs;
                    mesh.triangles = triangles;
                    mesh.RecalculateBounds();
                    mesh.RecalculateNormals();
                    AssetDatabase.CreateAsset(mesh, path);
                }

                filter.sharedMesh = mesh;
                EditorUtility.SetDirty(filter);
            }
        }

        private static void DisableRenderer(Transform root, string objectName)
        {
            // Oculta renderers concretos del FBX cuando interfieren con la textura final.
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != objectName)
                {
                    continue;
                }

                Renderer renderer = child.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                }
            }
        }

        private static Material CreatePropMaterial(string name, Color color, Texture2D texture, float smoothness)
        {
            // Crea material de reloj/cuadro con textura opcional y parametros de superficie.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = FindShader(texture != null);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.shader = shader;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (texture != null && material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindShader(bool texturedSurface)
        {
            // Texturas de referencia usan shader Unlit; superficies comunes usan Lit/Standard.
            Shader shader = null;
            if (texturedSurface)
            {
                shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Texture");
            }

            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return shader;
        }

        private static void EnsureGeneratedMeshFolder()
        {
            // Garantiza carpeta para mallas de UV generadas por el applier.
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Models"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Models");
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Models/Integration"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Models", "Integration");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedMeshFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Models/Integration", "Generated");
            }
        }

        private static void RemoveLegacyOverlay(string objectName)
        {
            // Elimina planos overlay antiguos para evitar imagenes pegadas sobre props.
            GameObject existing = GameObject.Find(objectName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }
        }

        private static void EnsurePointLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            // Crea/actualiza luces puntuales extremadamente tenues para conservar la noche.
            GameObject lightObject = GameObject.Find(name);
            if (lightObject == null)
            {
                lightObject = new GameObject(name);
            }

            lightObject.transform.position = position;
            Light light = lightObject.GetComponent<Light>();
            if (light == null)
            {
                light = lightObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
        }

        private static GameObject GetOrCreate(string name, Transform parent)
        {
            // Obtiene un objeto por nombre o lo crea para puntos y referencias auxiliares.
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
            }

            obj.transform.SetParent(parent);
            return obj;
        }

        private static GameObject GetOrCreatePrimitive(string name, Transform parent)
        {
            // Obtiene un cubo existente o crea uno para piezas visuales.
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name = name;
            }

            obj.transform.SetParent(parent);
            return obj;
        }

        private static void DestroyIfPresent(string name)
        {
            // Elimina auxiliares antiguos que podian reaparecer al preparar la escena.
            GameObject obj = GameObject.Find(name);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        private static Light FindLampLight()
        {
            // Busca la luz real instalada en el objeto Lampara.
            GameObject lamp = GameObject.Find("Lampara");
            return lamp != null ? lamp.GetComponent<Light>() : null;
        }

        private static bool IsDoorPart(Transform transform)
        {
            // Identifica puerta/manijas importadas para reemplazarlas por la puerta jugable.
            string lower = transform.name.ToLowerInvariant();
            return lower.Contains("door") || lower.Contains("handle");
        }

        private static Material GetFloorWoodMaterial()
        {
            // Material plano muy oscuro usado en piso para distinguirlo de paredes claras.
            return GetFlatColorMaterial("ML_Wood_DarkFloorAndBaseboards", new Color(0.001f, 0.0015f, 0.0025f));
        }

        private static Material GetBaseboardWoodMaterial()
        {
            // Material plano casi negro para zocalos/baseboards.
            return GetFlatColorMaterial("ML_Wood_DarkBaseboards", new Color(0.0002f, 0.0004f, 0.0008f));
        }

        private static Material GetLightDoorWoodMaterial()
        {
            // Material de puerta/marcos, mas claro que piso y zocalos pero aun nocturno.
            return GetFlatColorMaterial("ML_Wood_LightDoorsAndFrames", new Color(0.002f, 0.0028f, 0.0045f));
        }

        private static Material GetFlatColorMaterial(string name, Color color)
        {
            // Crea material Unlit sin textura para controlar contraste por color puro.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            if (shader != null)
            {
                material.shader = shader;
            }

            material.color = color;
            material.mainTexture = null;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
            if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetMaterialEmission(Material material, Color emission)
        {
            // Ajusta emision de materiales como pantalla del celular o bombillo.
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            EditorUtility.SetDirty(material);
        }

        private static void MakeTextureReadable(Material material, Color tint)
        {
            // Mantiene la textura pero oscurece su tinte para que respete la baja luminosidad.
            if (material == null)
            {
                return;
            }

            Texture texture = material.mainTexture;
            Vector2 tiling = material.mainTextureScale;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (lit != null)
            {
                material.shader = lit;
            }

            material.color = tint;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.SetColor("_EmissionColor", Color.black);
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
        }

        private static Material GetEmissiveUnlitMaterial(string name, Color color, float intensity)
        {
            // Crea materiales emisivos para pequenas pistas visuales, como luz bajo puerta.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                material.shader = unlit;
            }

            material.color = color * intensity;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color * intensity);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * intensity);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetTransparentUnlitMaterial(string name, Color color)
        {
            // Crea material transparente para velos visuales o niebla.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit != null)
            {
                material.shader = unlit;
            }

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetFogVeilMaterial()
        {
            // Material texturizado de niebla para ocultar lo que hay tras la puerta.
            Texture2D texture = GetOrCreateFogTexture();
            Material material = GetTexturedMaterial("ML_Hallway_FogVeil", texture, new Vector2(1f, 1f), 0.1f, 0f);
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (unlit != null)
            {
                material.shader = unlit;
            }

            material.color = new Color(0.055f, 0.052f, 0.055f, 1f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetBronzeMetalMaterial()
        {
            // Material metalico tipo bronce usado en manijas.
            Material material = GetMaterial("ML_Metal_AgedBronzeHandles", new Color(0.63f, 0.39f, 0.16f), 0.58f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.82f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.54f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material GetTexturedMaterial(string name, Texture2D texture, Vector2 tiling, float smoothness, float metallic)
        {
            // Crea materiales Lit con textura procedural/importada, tiling y valores PBR basicos.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = Color.white;
            material.mainTexture = texture;
            material.mainTextureScale = tiling;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
                material.SetTextureScale("_BaseMap", tiling);
            }

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D GetOrCreateWoodTexture(string name, Color low, Color high, Color line, float grainFrequency)
        {
            // Genera una textura procedural de madera con tablas, variacion y lineas oscuras.
            EnsureGeneratedTextureFolder();
            string path = GeneratedTextureFolder + "/" + name + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, true) { name = name };

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float u = x / (float)texture.width;
                    float v = y / (float)texture.height;
                    float plankCount = 5f;
                    float plankCoord = v * plankCount;
                    float plankIndex = Mathf.Floor(plankCoord);
                    float plankLocal = plankCoord - plankIndex;
                    float plankVariation = Mathf.PerlinNoise(plankIndex * 0.37f + 3.1f, 0.17f);
                    float knots = Mathf.PerlinNoise(u * 5.4f + plankVariation * 1.7f, v * 2.2f);
                    float wave = Mathf.Abs(Mathf.Sin((u * grainFrequency + knots * 2.2f + plankVariation * 1.4f) * Mathf.PI));
                    float fineGrain = Mathf.PerlinNoise(u * 64f + plankVariation * 8f, v * 18f);
                    float longBands = Mathf.Clamp01(wave * 0.5f + fineGrain * 0.32f + plankVariation * 0.18f);
                    float seam = Mathf.SmoothStep(0.965f, 1f, Mathf.Abs(plankLocal - 0.5f) * 2f);
                    float darkLine = Mathf.SmoothStep(0.76f, 0.98f, longBands);
                    Color baseColor = Color.Lerp(low, high, Mathf.Clamp01(longBands * 0.76f + plankVariation * 0.24f));
                    Color finalColor = Color.Lerp(baseColor, line, darkLine * 0.32f + seam * 0.65f);
                    texture.SetPixel(x, y, finalColor);
                }
            }

            texture.Apply(true);
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            Texture2D imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            return imported;
        }

        private static Texture2D GetOrCreateFabricTexture(string name, Color low, Color high, float weaveFrequency)
        {
            // Genera una textura procedural de tela con patron de tejido y ruido.
            EnsureGeneratedTextureFolder();
            string path = GeneratedTextureFolder + "/" + name + ".png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, true) { name = name };

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float u = x / (float)texture.width;
                    float v = y / (float)texture.height;
                    float weaveA = Mathf.Abs(Mathf.Sin(u * weaveFrequency * Mathf.PI));
                    float weaveB = Mathf.Abs(Mathf.Sin(v * weaveFrequency * 0.83f * Mathf.PI));
                    float noise = Mathf.PerlinNoise(u * 42f, v * 42f);
                    float value = Mathf.Clamp01(weaveA * 0.24f + weaveB * 0.24f + noise * 0.52f);
                    texture.SetPixel(x, y, Color.Lerp(low, high, value));
                }
            }

            return SaveGeneratedPng(texture, path);
        }

        private static Texture2D GetOrCreateNightstandTexture()
        {
            // Genera textura compuesta para velador: cuerpo claro, gavetas madera y tiradores.
            EnsureGeneratedTextureFolder();
            string path = GeneratedTextureFolder + "/T_Nightstand_WhiteWoodDrawer.png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, true) { name = "T_Nightstand_WhiteWoodDrawer" };
            Color white = new Color(0.86f, 0.85f, 0.81f);
            Color shadow = new Color(0.58f, 0.56f, 0.52f);
            Color woodLow = new Color(0.48f, 0.28f, 0.12f);
            Color woodHigh = new Color(0.86f, 0.58f, 0.3f);
            Color handle = new Color(0.92f, 0.9f, 0.84f);

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float u = x / (float)texture.width;
                    float v = y / (float)texture.height;
                    bool drawerArea = u > 0.13f && u < 0.87f && v > 0.18f && v < 0.82f;
                    Color color = Color.Lerp(shadow, white, Mathf.SmoothStep(0f, 0.2f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v))));

                    if (drawerArea)
                    {
                        float grain = Mathf.PerlinNoise(u * 18f, v * 42f);
                        float band = Mathf.Abs(Mathf.Sin((v * 18f + grain * 2f) * Mathf.PI));
                        color = Color.Lerp(woodLow, woodHigh, Mathf.Clamp01(grain * 0.45f + band * 0.55f));
                    }

                    if (drawerArea && Mathf.Abs(v - 0.5f) < 0.008f)
                    {
                        color = new Color(0.22f, 0.13f, 0.065f);
                    }

                    bool upperHandle = u > 0.34f && u < 0.66f && v > 0.61f && v < 0.66f;
                    bool lowerHandle = u > 0.34f && u < 0.66f && v > 0.31f && v < 0.36f;
                    if (upperHandle || lowerHandle)
                    {
                        color = handle;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            return SaveGeneratedPng(texture, path);
        }

        private static Texture2D GetOrCreatePhoneScreenTexture()
        {
            // Genera una pantalla blanca; la emision se activa al tomar el celular.
            EnsureGeneratedTextureFolder();
            string path = GeneratedTextureFolder + "/T_PhoneScreen_WhiteOn.png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, true) { name = "T_PhoneScreen_WhiteOn" };

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float u = x / (float)texture.width;
                    float v = y / (float)texture.height;
                    float edgeShade = Mathf.SmoothStep(0.82f, 0.98f, Mathf.Max(Mathf.Abs(u - 0.5f), Mathf.Abs(v - 0.5f)) * 2f);
                    float subtleNoise = Mathf.PerlinNoise(u * 8f, v * 8f) * 0.035f;
                    Color color = Color.Lerp(new Color(1f, 1f, 1f), new Color(0.72f, 0.74f, 0.76f), edgeShade);
                    color += new Color(subtleNoise, subtleNoise, subtleNoise, 0f);

                    if (v > 0.74f && v < 0.8f && u > 0.31f && u < 0.69f)
                    {
                        color = Color.white;
                    }

                    if (v > 0.12f && v < 0.14f && u > 0.38f && u < 0.62f)
                    {
                        color = Color.white;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            return SaveGeneratedPng(texture, path);
        }

        private static Texture2D GetOrCreateFogTexture()
        {
            // Genera textura de niebla irregular para el pasillo tras la puerta.
            EnsureGeneratedTextureFolder();
            string path = GeneratedTextureFolder + "/T_Hallway_FogVeil.png";
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, true) { name = "T_Hallway_FogVeil" };

            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float u = x / (float)texture.width;
                    float v = y / (float)texture.height;
                    float center = 1f - Mathf.Clamp01(Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.56f)) * 1.55f);
                    float smokeA = Mathf.PerlinNoise(u * 5f + 2.7f, v * 6f + 1.3f);
                    float smokeB = Mathf.PerlinNoise(u * 15f + smokeA, v * 12f);
                    float mist = Mathf.Clamp01(center * 0.45f + smokeA * 0.35f + smokeB * 0.2f);
                    Color dark = new Color(0.018f, 0.017f, 0.019f);
                    Color light = new Color(0.22f, 0.2f, 0.17f);
                    texture.SetPixel(x, y, Color.Lerp(dark, light, mist));
                }
            }

            return SaveGeneratedPng(texture, path);
        }

        private static Texture2D SaveGeneratedPng(Texture2D texture, string path)
        {
            // Guarda la textura procedural como PNG importable por Unity.
            texture.Apply(true);
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material GetMaterial(string name, Color color, float smoothness)
        {
            // Crea material Lit simple con color, smoothness y metalicidad cero.
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyMaterial(GameObject target, Material material)
        {
            // Asigna material a un objeto procedural si tiene Renderer.
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void EnsureMaterialFolder()
        {
            // Garantiza carpeta de materiales del look integrado.
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Materials"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Materials", "IntegrationLook");
            }
        }

        private static void EnsureGeneratedTextureFolder()
        {
            // Garantiza carpeta para texturas procedurales generadas por editor.
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Art");
            }

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Textures"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Textures");
            }

            if (!AssetDatabase.IsValidFolder(GeneratedTextureFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art/Textures", "Generated");
            }
        }
    }
}
