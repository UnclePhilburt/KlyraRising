using UnityEngine;
using UnityEditor;
using System.IO;

public class SoulSystemFullSetup : MonoBehaviour
{
    [MenuItem("Klyra/Full Soul System Setup")]
    public static void FullSetup()
    {
        Debug.Log("[SoulSystem] Starting full setup...");

        // 1. Create prefab folder
        string prefabFolder = "Assets/Prefabs/Characters";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        // 2. Find the base NetworkPlayer prefab to duplicate
        GameObject networkPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/NetworkPlayer.prefab");
        if (networkPlayerPrefab == null)
        {
            Debug.LogError("[SoulSystem] Could not find NetworkPlayer.prefab in Resources! Please create it first.");
            return;
        }

        // 3. Find the character models
        GameObject genericMaleModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Synty/PolygonSamuraiEmpire/Prefabs/Characters/SM_Chr_Generic_Male_01.prefab");
        GameObject tenguModel = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Synty/PolygonSamuraiEmpire/Prefabs/Characters/SM_Chr_Tengu_Male_01.prefab");

        if (genericMaleModel == null)
            Debug.LogWarning("[SoulSystem] Could not find SM_Chr_Generic_Male_01.prefab");
        if (tenguModel == null)
            Debug.LogWarning("[SoulSystem] Could not find SM_Chr_Tengu_Male_01.prefab");

        // 4. Create Peasant prefab (duplicate NetworkPlayer, swap model)
        GameObject peasantPrefab = CreateCharacterFromBase(networkPlayerPrefab, "Peasant", genericMaleModel, prefabFolder);

        // 5. Create Tengu prefab
        GameObject tenguPrefab = CreateCharacterFromBase(networkPlayerPrefab, "Tengu", tenguModel, prefabFolder);

        // 6. Create SoulSystem in scene if it doesn't exist
        SoulSystemSetup existingSetup = FindFirstObjectByType<SoulSystemSetup>();
        if (existingSetup != null)
        {
            Debug.Log("[SoulSystem] SoulSystemSetup already exists, updating prefab references...");
            UpdatePrefabReferences(existingSetup, peasantPrefab, tenguPrefab);
            Selection.activeGameObject = existingSetup.gameObject;
        }
        else
        {
            GameObject soulSystemObj = new GameObject("SoulSystem");
            SoulSystemSetup setup = soulSystemObj.AddComponent<SoulSystemSetup>();
            UpdatePrefabReferences(setup, peasantPrefab, tenguPrefab);
            Selection.activeGameObject = soulSystemObj;
            Debug.Log("[SoulSystem] Created SoulSystem object in scene");
        }

        Debug.Log("===========================================");
        Debug.Log("[SoulSystem] SETUP COMPLETE!");
        Debug.Log("===========================================");
        Debug.Log("Prefabs created at: Assets/Prefabs/Characters/");
        Debug.Log("Hold Q in-game to open the radial menu");
        Debug.Log("Select a soul to swap characters!");
        Debug.Log("===========================================");
    }

    static GameObject CreateCharacterFromBase(GameObject basePrefab, string characterName, GameObject modelPrefab, string prefabFolder)
    {
        string prefabPath = $"{prefabFolder}/{characterName}.prefab";

        // Check if prefab already exists - delete it to recreate
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (existingPrefab != null)
        {
            Debug.Log($"[SoulSystem] Deleting existing {characterName} prefab to recreate...");
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // Load the animator controller
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller");

        if (animController == null)
        {
            Debug.LogWarning("[SoulSystem] Could not find AC_Polygon_Masculine.controller - animations may not work!");
        }

        // Create root object from scratch (copying NetworkPlayer structure)
        GameObject root = new GameObject(characterName);
        root.tag = "Player";

        // Add CharacterController (same settings as NetworkPlayer)
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.3f;
        cc.skinWidth = 0.08f;
        cc.minMoveDistance = 0.001f;

        // Add ThirdPersonController
        root.AddComponent<ThirdPersonController>();

        // Add WeaponController
        root.AddComponent<WeaponController>();

        // Add PlayerHealth
        root.AddComponent<PlayerHealth>();

        // Add the model as a child
        if (modelPrefab != null)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            model.transform.SetParent(root.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            // Set up animator on the model
            Animator modelAnimator = model.GetComponent<Animator>();
            if (modelAnimator == null)
            {
                modelAnimator = model.AddComponent<Animator>();
            }

            modelAnimator.runtimeAnimatorController = animController;
            modelAnimator.applyRootMotion = false;
            Debug.Log($"[SoulSystem] Set animator controller: {animController?.name}, avatar: {modelAnimator.avatar?.name}");
        }

        // Create camera target point
        GameObject camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(root.transform);
        camTarget.transform.localPosition = new Vector3(0, 1.5f, 0);

        // Save as prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[SoulSystem] Created {characterName} prefab with all components");
        return prefab;
    }

    static void UpdatePrefabReferences(SoulSystemSetup setup, GameObject peasant, GameObject tengu)
    {
        SerializedObject so = new SerializedObject(setup);
        so.FindProperty("_peasantPrefab").objectReferenceValue = peasant;
        so.FindProperty("_tenguPrefab").objectReferenceValue = tengu;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(setup);
    }

    [MenuItem("Klyra/Clear Soul System")]
    public static void ClearSoulSystem()
    {
        // Clear PlayerPrefs
        PlayerPrefs.DeleteKey("UnlockedSouls");
        PlayerPrefs.DeleteKey("CurrentSoulIndex");
        PlayerPrefs.Save();

        // Remove from scene
        SoulSystemSetup setup = FindFirstObjectByType<SoulSystemSetup>();
        if (setup != null)
        {
            DestroyImmediate(setup.gameObject);
            Debug.Log("[SoulSystem] Removed SoulSystem from scene");
        }

        // Delete generated prefabs
        if (File.Exists("Assets/Prefabs/Characters/Peasant.prefab"))
        {
            AssetDatabase.DeleteAsset("Assets/Prefabs/Characters/Peasant.prefab");
        }
        if (File.Exists("Assets/Prefabs/Characters/Tengu.prefab"))
        {
            AssetDatabase.DeleteAsset("Assets/Prefabs/Characters/Tengu.prefab");
        }

        AssetDatabase.Refresh();
        Debug.Log("[SoulSystem] Cleared all soul save data and prefabs");
    }
}
