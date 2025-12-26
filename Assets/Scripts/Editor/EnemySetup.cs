#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class EnemySetup : EditorWindow
{
    [MenuItem("Klyra/Attach Sword to Enemy")]
    public static void AttachSwordToEnemy()
    {
        GameObject enemy = Selection.activeGameObject;
        if (enemy == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select an enemy in the hierarchy!", "OK");
            return;
        }

        // Check if it has Enemy component
        if (enemy.GetComponent<Enemy>() == null)
        {
            EditorUtility.DisplayDialog("Error", "Selected object doesn't have an Enemy component!", "OK");
            return;
        }

        // Find the animator
        Animator animator = enemy.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("Error", "No Animator found on enemy!", "OK");
            return;
        }

        // Find hand bone (same as player: Hand_R)
        Transform rightHand = FindBoneContaining(animator.transform, "Hand_R");
        if (rightHand == null)
            rightHand = FindBoneContaining(animator.transform, "hand_r");
        if (rightHand == null)
            rightHand = FindBoneContaining(animator.transform, "RightHand");

        // Find left thigh bone for sheath (same as player: UpperLeg_L)
        Transform thighBone = FindBoneContaining(animator.transform, "UpperLeg_L");
        if (thighBone == null)
            thighBone = FindBoneContaining(animator.transform, "upperleg_l");
        if (thighBone == null)
            thighBone = FindBoneContaining(animator.transform, "LeftUpLeg");

        if (rightHand == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Hand_R bone!", "OK");
            return;
        }

        // Find sword prefab (same as player: SM_Wep_Sword_01)
        string[] swordGuids = AssetDatabase.FindAssets("SM_Wep_Sword_01 t:Prefab");
        GameObject swordPrefab = null;

        foreach (string guid in swordGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("PolygonSamurai") && path.Contains("Weapons"))
            {
                swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                break;
            }
        }

        if (swordPrefab == null)
        {
            // Fallback to any sword
            swordGuids = AssetDatabase.FindAssets("SM_Wep_Sword t:Prefab");
            if (swordGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(swordGuids[0]);
                swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            }
        }

        // Remove existing swords
        RemoveExistingSwordRecursive(enemy.transform, "SwordHand");
        RemoveExistingSwordRecursive(enemy.transform, "SwordSheathed");

        // Create SwordHand - EXACT same values as player
        GameObject swordHand = null;
        if (swordPrefab != null)
        {
            swordHand = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
            swordHand.transform.SetParent(rightHand);
            swordHand.name = "SwordHand";
            swordHand.transform.localPosition = new Vector3(0.07f, 0.02f, 0f);
            swordHand.transform.localEulerAngles = new Vector3(356.1f, 85.90999f, 265.3f);
            swordHand.transform.localScale = Vector3.one;
        }

        // Create SwordSheathed on left thigh - EXACT same values as player
        GameObject swordSheathed = null;
        if (thighBone != null && swordPrefab != null)
        {
            swordSheathed = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
            swordSheathed.name = "SwordSheathed";
            swordSheathed.transform.localScale = Vector3.one;
            swordSheathed.transform.SetParent(thighBone, true);
            swordSheathed.transform.localPosition = new Vector3(-0.1f, -0.09f, 0.2f);
            swordSheathed.transform.localEulerAngles = new Vector3(339.8f, 59.15001f, 262f);
            swordSheathed.SetActive(false); // Hidden when sword is drawn
        }

        // Add EnemyCombat component if not present
        if (enemy.GetComponent<EnemyCombat>() == null)
        {
            enemy.AddComponent<EnemyCombat>();
        }

        EditorUtility.SetDirty(enemy);

        Debug.Log($"[EnemySetup] Attached sword to {enemy.name}");
        EditorUtility.DisplayDialog("Success",
            $"Sword attached to {enemy.name}!\n\n" +
            $"SwordHand: {rightHand.name}\n" +
            $"SwordSheathed: {(thighBone != null ? thighBone.name : "N/A")}\n\n" +
            "Using same positions as player.", "OK");
    }

    static Transform FindBoneContaining(Transform root, string namePart)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.Contains(namePart))
                return child;
        }
        return null;
    }

    static GameObject FindChildByName(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child.gameObject;
        }
        return null;
    }

    static void RemoveExistingSwordRecursive(Transform parent, string swordName)
    {
        if (parent.name == swordName)
        {
            Object.DestroyImmediate(parent.gameObject);
            return;
        }
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            RemoveExistingSwordRecursive(parent.GetChild(i), swordName);
        }
    }

    [MenuItem("Tools/Add Sword Hitbox to Player")]
    public static void AddSwordHitbox()
    {
        // Search for sword by various names
        string[] swordNames = { "SwordHand", "Sword", "Wep_Sword", "Katana", "Weapon" };
        GameObject sword = null;

        // First check selection
        if (Selection.activeGameObject != null)
        {
            // If user selected a sword directly
            if (Selection.activeGameObject.name.ToLower().Contains("sword") ||
                Selection.activeGameObject.name.ToLower().Contains("wep") ||
                Selection.activeGameObject.name.ToLower().Contains("katana"))
            {
                sword = Selection.activeGameObject;
            }
            else
            {
                // Search in selection's children
                foreach (string name in swordNames)
                {
                    sword = FindInChildrenPartial(Selection.activeGameObject.transform, name);
                    if (sword != null) break;
                }
            }
        }

        // Search entire scene
        if (sword == null)
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                string lowerName = t.name.ToLower();
                if ((lowerName.Contains("sword") || lowerName.Contains("wep_") || lowerName.Contains("katana"))
                    && t.GetComponent<MeshRenderer>() != null)
                {
                    // Check if it's parented to a hand bone
                    if (t.parent != null && t.parent.name.ToLower().Contains("hand"))
                    {
                        sword = t.gameObject;
                        break;
                    }
                }
            }
        }

        // Last resort - find any sword mesh
        if (sword == null)
        {
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name.ToLower().Contains("sword") && t.GetComponent<MeshRenderer>() != null)
                {
                    sword = t.gameObject;
                    Debug.Log($"Found sword: {t.name} (parent: {t.parent?.name})");
                    break;
                }
            }
        }

        if (sword == null)
        {
            Debug.LogError("Could not find sword! Select the sword object in the hierarchy and try again.");
            // List all objects with 'sword' in name for debugging
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                if (t.name.ToLower().Contains("sword") || t.name.ToLower().Contains("wep"))
                {
                    Debug.Log($"  Found potential sword: {t.name} (parent: {t.parent?.name})");
                }
            }
            return;
        }

        // Add SwordHitbox component
        SwordHitbox hitbox = sword.GetComponent<SwordHitbox>();
        if (hitbox == null)
        {
            hitbox = sword.AddComponent<SwordHitbox>();
            Debug.Log($"Added SwordHitbox to {sword.name}");
        }
        else
        {
            Debug.Log("SwordHitbox already exists!");
        }

        Selection.activeGameObject = sword;
        EditorGUIUtility.PingObject(sword);
    }

    static GameObject FindInChildrenPartial(Transform parent, string partialName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(partialName.ToLower()))
                return child.gameObject;
            GameObject found = FindInChildrenPartial(child, partialName);
            if (found != null)
                return found;
        }
        return null;
    }

    static GameObject FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;
            GameObject found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    [MenuItem("Tools/Create Enemy Prefab")]
    public static void CreateEnemyPrefab()
    {
        // Find the Tengu model
        string tenguPath = "Assets/Synty/PolygonSamuraiEmpire/Prefabs/Characters/SM_Chr_Tengu_Male_01.prefab";
        GameObject tenguPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(tenguPath);

        if (tenguPrefab == null)
        {
            Debug.LogError("Could not find Tengu prefab at: " + tenguPath);
            return;
        }

        // Find the animator controller
        string animatorPath = "Assets/Synty/AnimationBaseLocomotion/Animations/Polygon/AC_Polygon_Masculine.controller";
        RuntimeAnimatorController animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animatorPath);

        if (animController == null)
        {
            Debug.LogError("Could not find animator controller at: " + animatorPath);
            return;
        }

        // Create instance
        GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(tenguPrefab);
        enemy.name = "Enemy_Tengu";

        // Add Enemy component
        Enemy enemyComponent = enemy.AddComponent<Enemy>();

        // Set animator controller via serialized property
        SerializedObject so = new SerializedObject(enemyComponent);
        so.FindProperty("_animatorController").objectReferenceValue = animController;
        so.ApplyModifiedProperties();

        // Add/configure Animator on the model
        Animator animator = enemy.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            var skinnedMesh = enemy.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                animator = skinnedMesh.gameObject.AddComponent<Animator>();
            }
        }

        if (animator != null)
        {
            animator.runtimeAnimatorController = animController;
        }

        // Add CharacterController for physics/gravity
        CharacterController controller = enemy.GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = enemy.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, 0.9f, 0);
            controller.radius = 0.3f;
        }

        // Create Prefabs folder if needed
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // Save as prefab
        string prefabPath = "Assets/Prefabs/Enemy_Tengu.prefab";
        PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);

        // Clean up scene instance
        DestroyImmediate(enemy);

        // Select the new prefab
        GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        Selection.activeObject = newPrefab;
        EditorGUIUtility.PingObject(newPrefab);

        Debug.Log("Enemy prefab created at: " + prefabPath);
    }
}
#endif
