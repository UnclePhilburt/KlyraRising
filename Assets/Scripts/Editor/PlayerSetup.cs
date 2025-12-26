using UnityEngine;
using UnityEditor;
using Photon.Pun;
using System.IO;

public class PlayerSetup : EditorWindow
{
    [MenuItem("Klyra/Print Sword Transforms")]
    public static void PrintSwordTransforms()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Select the Player first!", "OK");
            return;
        }

        Transform swordHand = FindInHierarchy(selected.transform, "SwordHand");
        Transform swordSheathed = FindInHierarchy(selected.transform, "SwordSheathed");

        string msg = "";

        if (swordHand != null)
        {
            msg += $"SwordHand (parent: {swordHand.parent.name}):\n";
            msg += $"  Position: {swordHand.localPosition.x}f, {swordHand.localPosition.y}f, {swordHand.localPosition.z}f\n";
            msg += $"  Rotation: {swordHand.localEulerAngles.x}f, {swordHand.localEulerAngles.y}f, {swordHand.localEulerAngles.z}f\n";
            msg += $"  Scale: {swordHand.localScale.x}f, {swordHand.localScale.y}f, {swordHand.localScale.z}f\n\n";
        }
        else
        {
            msg += "SwordHand: NOT FOUND\n\n";
        }

        if (swordSheathed != null)
        {
            msg += $"SwordSheathed (parent: {swordSheathed.parent.name}):\n";
            msg += $"  Position: {swordSheathed.localPosition.x}f, {swordSheathed.localPosition.y}f, {swordSheathed.localPosition.z}f\n";
            msg += $"  Rotation: {swordSheathed.localEulerAngles.x}f, {swordSheathed.localEulerAngles.y}f, {swordSheathed.localEulerAngles.z}f\n";
            msg += $"  Scale: {swordSheathed.localScale.x}f, {swordSheathed.localScale.y}f, {swordSheathed.localScale.z}f\n";
        }
        else
        {
            msg += "SwordSheathed: NOT FOUND\n";
        }

        Debug.Log("[PlayerSetup] Sword Transforms:\n" + msg);
        EditorUtility.DisplayDialog("Sword Transforms", msg, "OK");
    }

    static Transform FindInHierarchy(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindInHierarchy(child, name);
            if (found != null) return found;
        }
        return null;
    }

    [MenuItem("Klyra/Attach Sword to Character")]
    public static void AttachSword()
    {
        // Get selected object
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select the Player or character in the hierarchy first!", "OK");
            return;
        }

        // Find the character (SM_Chr_*)
        Transform character = null;
        if (selected.name.StartsWith("SM_Chr_"))
        {
            character = selected.transform;
        }
        else
        {
            foreach (Transform child in selected.transform)
            {
                if (child.name.StartsWith("SM_Chr_"))
                {
                    character = child;
                    break;
                }
            }
        }

        if (character == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find character (SM_Chr_*) in selection!", "OK");
            return;
        }

        // Find bones
        Transform rightHand = FindBoneRecursive(character, "Hand_R");
        if (rightHand == null) rightHand = FindBoneRecursive(character, "hand_r");
        if (rightHand == null) rightHand = FindBoneRecursive(character, "RightHand");

        if (rightHand == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find right hand bone!", "OK");
            return;
        }

        Debug.Log($"[PlayerSetup] Found rightHand bone: {rightHand.name} (full path: {GetBonePath(rightHand)})");

        // Find sword prefab
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
            EditorUtility.DisplayDialog("Error", "Could not find SM_Wep_Sword_01 prefab!", "OK");
            return;
        }

        // Find left thigh bone for sheathed sword (so it wobbles with walking)
        Transform thighBone = FindBoneRecursive(character, "UpperLeg_L");
        if (thighBone == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find UpperLeg_L bone!", "OK");
            return;
        }

        // Remove existing swords (search entire hierarchy)
        Transform playerRoot = character.parent != null ? character.parent : character;
        RemoveExistingSwordRecursive(playerRoot, "SwordHand");
        RemoveExistingSwordRecursive(playerRoot, "SwordSheathed");

        // Create HAND sword (hidden by default)
        GameObject swordHand = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
        swordHand.transform.SetParent(rightHand);
        swordHand.name = "SwordHand";
        swordHand.transform.localPosition = new Vector3(0.07f, 0.02f, 0f);
        swordHand.transform.localEulerAngles = new Vector3(356.1f, 85.90999f, 265.3f);
        swordHand.transform.localScale = Vector3.one;
        swordHand.SetActive(false); // Hidden until drawn

        // Create SHEATHED sword (visible by default - on left thigh)
        GameObject swordSheathed = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
        swordSheathed.name = "SwordSheathed";
        // Set world scale first, then parent preserving it
        swordSheathed.transform.localScale = Vector3.one;
        swordSheathed.transform.SetParent(thighBone, true);
        // Apply user's exact local values
        swordSheathed.transform.localPosition = new Vector3(-0.1f, -0.09f, 0.2f);
        swordSheathed.transform.localEulerAngles = new Vector3(339.8f, 59.15001f, 262f);
        swordSheathed.SetActive(true);

        EditorUtility.DisplayDialog("Swords Attached",
            "Two swords created:\n\n" +
            "1. SwordSheathed (left hip) - visible when unequipped\n" +
            "2. SwordHand (right hand) - visible when equipped\n\n" +
            "Press 1 to draw/sheathe.\n\n" +
            "Adjust positions in the Inspector as needed.",
            "OK");
    }

    static void RemoveExistingSword(Transform parent, string swordName)
    {
        Transform existing = parent.Find(swordName);
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }
    }

    static void RemoveExistingSwordRecursive(Transform parent, string swordName)
    {
        // Check this transform
        if (parent.name == swordName)
        {
            Object.DestroyImmediate(parent.gameObject);
            return;
        }
        // Check children (iterate backwards since we may destroy)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            RemoveExistingSwordRecursive(parent.GetChild(i), swordName);
        }
    }

    static Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name.ToLower().Contains(boneName.ToLower()))
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null)
                return found;
        }
        return null;
    }

    static string GetBonePath(Transform bone)
    {
        string path = bone.name;
        Transform current = bone.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    [MenuItem("Klyra/Setup Player")]
    public static void Setup()
    {
        // Create player root
        GameObject player = new GameObject("Player");

        // Add CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.radius = 0.3f;

        // Add controller scripts
        player.AddComponent<ThirdPersonController>();
        player.AddComponent<WeaponController>();

        // Find animator controller
        RuntimeAnimatorController animController = null;
        string[] guids = AssetDatabase.FindAssets("AC_Polygon_Masculine t:AnimatorController");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path);
        }

        // Find and add starting character (generic peasant - nobody to hero!)
        string[] charGuids = AssetDatabase.FindAssets("SM_Chr_Generic_Male_01 t:Prefab");
        GameObject addedChar = null;

        foreach (string guid in charGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("PolygonSamurai"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    addedChar = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    addedChar.transform.SetParent(player.transform);
                    addedChar.transform.localPosition = Vector3.zero;
                    addedChar.transform.localRotation = Quaternion.identity;

                    // Setup animator
                    Animator anim = addedChar.GetComponent<Animator>();
                    if (anim == null) anim = addedChar.AddComponent<Animator>();
                    if (animController != null)
                    {
                        anim.runtimeAnimatorController = animController;
                        anim.applyRootMotion = false;
                    }

                    Debug.Log($"[Setup] Added character: {addedChar.name}");
                    break;
                }
            }
        }

        // Position in scene
        player.transform.position = new Vector3(0, 1, 0);

        // Select in hierarchy
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog("Player Created",
            $"Player setup complete!\n\n" +
            $"- Added CharacterController\n" +
            $"- Added ThirdPersonController\n" +
            $"- Added WeaponController\n" +
            $"- Added {(addedChar != null ? addedChar.name : "no character")}\n\n" +
            "Run 'Klyra > Setup Combat Animations' next.",
            "OK");
    }

    [MenuItem("Klyra/Create Network Player Prefab")]
    public static void CreateNetworkPrefab()
    {
        // Get selected player object
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select the Player in the hierarchy first!", "OK");
            return;
        }

        // Verify it has required components
        if (selected.GetComponent<ThirdPersonController>() == null)
        {
            EditorUtility.DisplayDialog("Error", "Selected object must have ThirdPersonController!", "OK");
            return;
        }

        // Add PhotonView if not present
        PhotonView photonView = selected.GetComponent<PhotonView>();
        if (photonView == null)
        {
            photonView = selected.AddComponent<PhotonView>();
        }

        // Configure PhotonView
        photonView.OwnershipTransfer = OwnershipOption.Takeover;
        photonView.Synchronization = ViewSynchronization.UnreliableOnChange;

        // Add observed components
        var observedList = new System.Collections.Generic.List<Component>();

        var controller = selected.GetComponent<ThirdPersonController>();
        if (controller != null) observedList.Add(controller);

        var weapon = selected.GetComponent<WeaponController>();
        if (weapon != null) observedList.Add(weapon);

        photonView.ObservedComponents = observedList;

        // Create Resources folder if it doesn't exist
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Save as prefab
        string prefabPath = resourcesPath + "/NetworkPlayer.prefab";

        // Remove existing prefab if it exists
        if (File.Exists(prefabPath))
        {
            AssetDatabase.DeleteAsset(prefabPath);
        }

        // Create the prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(selected, prefabPath);

        if (prefab != null)
        {
            EditorUtility.DisplayDialog("Network Prefab Created",
                "NetworkPlayer prefab created!\n\n" +
                "Location: Assets/Resources/NetworkPlayer.prefab\n\n" +
                "Components added:\n" +
                "- PhotonView\n" +
                "- ThirdPersonController (observed)\n" +
                "- WeaponController (observed)\n\n" +
                "The prefab is ready for PhotonNetwork.Instantiate()",
                "OK");

            // Select the prefab
            Selection.activeObject = prefab;
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Failed to create prefab!", "OK");
        }
    }

    [MenuItem("Klyra/Swap Character Model")]
    public static void SwapCharacterModel()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Select the Player first!", "OK");
            return;
        }

        // Find current character
        Transform oldChar = null;
        foreach (Transform child in selected.transform)
        {
            if (child.name.StartsWith("SM_Chr_"))
            {
                oldChar = child;
                break;
            }
        }

        if (oldChar == null)
        {
            EditorUtility.DisplayDialog("Error", "No SM_Chr_ character found!", "OK");
            return;
        }

        // Find the new character prefab
        string[] charGuids = AssetDatabase.FindAssets("SM_Chr_Generic_Male_01 t:Prefab");
        GameObject newCharPrefab = null;

        foreach (string guid in charGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("PolygonSamurai"))
            {
                newCharPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                break;
            }
        }

        if (newCharPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find SM_Chr_Generic_Male_01!", "OK");
            return;
        }

        // Find animator controller from old character
        Animator oldAnim = oldChar.GetComponent<Animator>();
        RuntimeAnimatorController animController = oldAnim != null ? oldAnim.runtimeAnimatorController : null;

        // Find swords before destroying old character
        Transform swordHand = FindInHierarchy(oldChar, "SwordHand");
        Transform swordSheathed = FindInHierarchy(oldChar, "SwordSheathed");

        // Store sword local transforms
        Vector3 swordHandPos = Vector3.zero, swordHandRot = Vector3.zero;
        Vector3 swordSheathedPos = Vector3.zero, swordSheathedRot = Vector3.zero;
        string swordHandParentBone = "", swordSheathedParentBone = "";

        if (swordHand != null)
        {
            swordHandPos = swordHand.localPosition;
            swordHandRot = swordHand.localEulerAngles;
            swordHandParentBone = swordHand.parent.name;
        }
        if (swordSheathed != null)
        {
            swordSheathedPos = swordSheathed.localPosition;
            swordSheathedRot = swordSheathed.localEulerAngles;
            swordSheathedParentBone = swordSheathed.parent.name;
        }

        // Instantiate new character
        GameObject newChar = (GameObject)PrefabUtility.InstantiatePrefab(newCharPrefab);
        newChar.transform.SetParent(selected.transform);
        newChar.transform.localPosition = Vector3.zero;
        newChar.transform.localRotation = Quaternion.identity;

        // Setup animator
        Animator newAnim = newChar.GetComponent<Animator>();
        if (newAnim == null) newAnim = newChar.AddComponent<Animator>();
        if (animController != null)
        {
            newAnim.runtimeAnimatorController = animController;
            newAnim.applyRootMotion = false;
        }

        // Reattach swords to new character bones
        if (swordHand != null && !string.IsNullOrEmpty(swordHandParentBone))
        {
            Transform newBone = FindBoneRecursive(newChar.transform, swordHandParentBone);
            if (newBone != null)
            {
                swordHand.SetParent(newBone);
                swordHand.localPosition = swordHandPos;
                swordHand.localEulerAngles = swordHandRot;
                swordHand.localScale = Vector3.one;
            }
        }

        if (swordSheathed != null && !string.IsNullOrEmpty(swordSheathedParentBone))
        {
            Transform newBone = FindBoneRecursive(newChar.transform, swordSheathedParentBone);
            if (newBone != null)
            {
                swordSheathed.SetParent(newBone);
                swordSheathed.localPosition = swordSheathedPos;
                swordSheathed.localEulerAngles = swordSheathedRot;
                swordSheathed.localScale = Vector3.one;
            }
        }

        // Destroy old character
        Object.DestroyImmediate(oldChar.gameObject);

        EditorUtility.DisplayDialog("Character Swapped",
            $"Swapped to: {newChar.name}\n\n" +
            "Swords transferred to new character.\n\n" +
            "Now run: Klyra > Create Network Player Prefab",
            "OK");
    }
}
