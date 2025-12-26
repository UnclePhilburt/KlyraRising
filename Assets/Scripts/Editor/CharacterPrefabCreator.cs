using UnityEngine;
using UnityEditor;
using System.IO;

public class CharacterPrefabCreator : MonoBehaviour
{
    [MenuItem("Klyra/Create Character Prefab/Peasant (Generic Male)")]
    public static void CreatePeasantPrefab()
    {
        CreateCharacterPrefab("Peasant", "A humble peasant. Where your journey began.");
    }

    [MenuItem("Klyra/Create Character Prefab/Tengu")]
    public static void CreateTenguPrefab()
    {
        CreateCharacterPrefab("Tengu", "Swift demon warrior.");
    }

    [MenuItem("Klyra/Create Character Prefab/Custom...")]
    public static void CreateCustomPrefab()
    {
        string name = "NewCharacter";
        CreateCharacterPrefab(name, "A warrior soul.");
    }

    static void CreateCharacterPrefab(string characterName, string description)
    {
        // Create root object
        GameObject root = new GameObject(characterName);

        // Add CharacterController
        CharacterController cc = root.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.9f, 0);

        // Add ThirdPersonController
        root.AddComponent<ThirdPersonController>();

        // Add PlayerHealth
        root.AddComponent<PlayerHealth>();

        // Add WeaponController
        root.AddComponent<WeaponController>();

        // Add Animator (will need controller assigned)
        Animator anim = root.AddComponent<Animator>();

        // Create model placeholder
        GameObject modelPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        modelPlaceholder.name = "Model";
        modelPlaceholder.transform.SetParent(root.transform);
        modelPlaceholder.transform.localPosition = new Vector3(0, 0.9f, 0);
        modelPlaceholder.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

        // Remove collider from placeholder (CharacterController handles collision)
        Object.DestroyImmediate(modelPlaceholder.GetComponent<Collider>());

        // Give it a color based on name
        Renderer rend = modelPlaceholder.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            if (characterName == "Peasant")
                mat.color = new Color(0.6f, 0.5f, 0.4f); // Brown/tan
            else if (characterName == "Tengu")
                mat.color = new Color(0.8f, 0.2f, 0.2f); // Red
            else
                mat.color = new Color(0.5f, 0.5f, 0.5f); // Gray
            rend.material = mat;
        }

        // Create camera target point
        GameObject camTarget = new GameObject("CameraTarget");
        camTarget.transform.SetParent(root.transform);
        camTarget.transform.localPosition = new Vector3(0, 1.5f, 0);

        // Set tag
        root.tag = "Player";

        // Create Prefabs folder if needed
        string prefabFolder = "Assets/Prefabs/Characters";
        if (!Directory.Exists(prefabFolder))
        {
            Directory.CreateDirectory(prefabFolder);
            AssetDatabase.Refresh();
        }

        // Save as prefab
        string prefabPath = $"{prefabFolder}/{characterName}.prefab";

        // Check if prefab already exists
        if (File.Exists(prefabPath))
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                $"{characterName} prefab already exists. Overwrite?", "Yes", "No"))
            {
                Object.DestroyImmediate(root);
                return;
            }
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // Select the new prefab
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        Debug.Log($"[CharacterPrefab] Created {characterName} prefab at {prefabPath}");
        Debug.Log($"[CharacterPrefab] Replace the 'Model' child with your actual character model");
        Debug.Log($"[CharacterPrefab] Assign an Animator Controller to the Animator component");
    }
}
