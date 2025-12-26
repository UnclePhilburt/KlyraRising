using UnityEngine;
using UnityEditor;
using System.IO;

public class BossSetup : EditorWindow
{
    private GameObject _characterModel;
    private RuntimeAnimatorController _animatorController;
    private string _bossName = "Boss";

    [MenuItem("Klyra/Create Boss")]
    public static void ShowWindow()
    {
        GetWindow<BossSetup>("Boss Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Create Boss Character", EditorStyles.boldLabel);
        GUILayout.Space(10);

        _bossName = EditorGUILayout.TextField("Boss Name", _bossName);
        _characterModel = (GameObject)EditorGUILayout.ObjectField("Character Model", _characterModel, typeof(GameObject), false);
        _animatorController = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller", _animatorController, typeof(RuntimeAnimatorController), false);

        GUILayout.Space(10);

        // Help text
        EditorGUILayout.HelpBox(
            "1. Drag a Synty character prefab (like SM_Chr_Samurai_Male_01)\n" +
            "2. Optionally assign an animator controller\n" +
            "3. Click Create Boss\n\n" +
            "This creates:\n" +
            "- Boss with Enemy component\n" +
            "- BossIntroTrigger with death sequence\n" +
            "- Respawn point",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Create Boss", GUILayout.Height(40)))
        {
            CreateBoss();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Find Samurai Characters", GUILayout.Height(25)))
        {
            FindCharacters();
        }
    }

    void FindCharacters()
    {
        string[] guids = AssetDatabase.FindAssets("SM_Chr_Samurai t:prefab");
        Debug.Log($"Found {guids.Length} samurai characters:");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"  - {Path.GetFileNameWithoutExtension(path)}: {path}");
        }
    }

    void CreateBoss()
    {
        if (_characterModel == null)
        {
            // Try to find a default samurai
            string[] guids = AssetDatabase.FindAssets("SM_Chr_Samurai_Male_01 t:prefab");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Debug.Log($"Using default character: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a character model!", "OK");
                return;
            }
        }

        // Create parent container
        GameObject bossRoot = new GameObject(_bossName);
        Undo.RegisterCreatedObjectUndo(bossRoot, "Create Boss");

        // Spawn character model
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(_characterModel);
        model.name = "Model";
        model.transform.SetParent(bossRoot.transform);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Add CharacterController
        CharacterController cc = bossRoot.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);

        // Add Enemy component (boss is just a strong enemy)
        Enemy enemy = bossRoot.AddComponent<Enemy>();

        // Set high health via SerializedObject
        SerializedObject so = new SerializedObject(enemy);
        so.FindProperty("_maxHealth").floatValue = 500f;
        so.ApplyModifiedProperties();

        // Setup animator
        Animator animator = model.GetComponent<Animator>();
        if (animator == null)
            animator = model.GetComponentInChildren<Animator>();

        if (animator != null && _animatorController != null)
        {
            animator.runtimeAnimatorController = _animatorController;
        }

        // Create BossIntroTrigger
        GameObject introTrigger = new GameObject("BossIntroTrigger");
        introTrigger.transform.SetParent(bossRoot.transform);
        introTrigger.transform.localPosition = new Vector3(0, 0, -5f); // In front of boss

        BossIntro bossIntro = introTrigger.AddComponent<BossIntro>();

        // Set BossIntro references via SerializedObject
        SerializedObject introSo = new SerializedObject(bossIntro);
        introSo.FindProperty("_bossAnimator").objectReferenceValue = animator;
        introSo.FindProperty("_bossTransform").objectReferenceValue = bossRoot.transform;
        introSo.ApplyModifiedProperties();

        // Create respawn point
        GameObject respawnPoint = new GameObject("RespawnPoint");
        respawnPoint.transform.SetParent(bossRoot.transform);
        respawnPoint.transform.localPosition = new Vector3(0, 0, -10f); // Behind trigger

        // Assign respawn point
        introSo.FindProperty("_respawnPoint").objectReferenceValue = respawnPoint.transform;
        introSo.ApplyModifiedProperties();

        // Add sword to boss
        AttachSwordToBoss(model);

        // Select the boss
        Selection.activeGameObject = bossRoot;

        Debug.Log($"[BossSetup] Created boss: {_bossName}");
        Debug.Log("[BossSetup] Don't forget to:");
        Debug.Log("  1. Position the boss in your scene");
        Debug.Log("  2. Move the BossIntroTrigger to where players will approach");
        Debug.Log("  3. Move the RespawnPoint to where players should respawn");
        Debug.Log("  4. Add attack animations to the boss's Animator with 'Attack' trigger");
    }

    void AttachSwordToBoss(GameObject model)
    {
        // Find hand bone
        Transform handBone = FindBoneRecursive(model.transform, "Hand_R") ??
                             FindBoneRecursive(model.transform, "hand_r") ??
                             FindBoneRecursive(model.transform, "RightHand");

        if (handBone == null)
        {
            Debug.LogWarning("[BossSetup] Could not find hand bone for sword");
            return;
        }

        // Find a katana prefab
        string[] swordGuids = AssetDatabase.FindAssets("SM_Wep_Katana t:prefab");
        if (swordGuids.Length == 0)
        {
            Debug.LogWarning("[BossSetup] No katana prefab found");
            return;
        }

        string swordPath = AssetDatabase.GUIDToAssetPath(swordGuids[0]);
        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(swordPath);

        GameObject sword = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
        sword.name = "BossSword";
        sword.transform.SetParent(handBone);
        sword.transform.localPosition = new Vector3(0.02f, 0.08f, -0.02f);
        sword.transform.localRotation = Quaternion.Euler(-10f, 0f, -90f);
        sword.transform.localScale = Vector3.one;

        Debug.Log("[BossSetup] Attached sword to boss");
    }

    Transform FindBoneRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(name.ToLower()))
                return child;
            Transform found = FindBoneRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
