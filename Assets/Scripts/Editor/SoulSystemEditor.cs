using UnityEngine;
using UnityEditor;

public class SoulSystemEditor : MonoBehaviour
{
    [MenuItem("Klyra/Setup Soul System")]
    public static void SetupSoulSystem()
    {
        // Check if already exists
        SoulSystemSetup existing = FindFirstObjectByType<SoulSystemSetup>();
        if (existing != null)
        {
            Debug.Log("[SoulSystem] Already exists in scene! Select it to configure.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Create the setup object
        GameObject setupObj = new GameObject("SoulSystem");
        SoulSystemSetup setup = setupObj.AddComponent<SoulSystemSetup>();

        // Try to find character prefabs in project
        string[] peasantGuids = AssetDatabase.FindAssets("Peasant t:Prefab");
        string[] tenguGuids = AssetDatabase.FindAssets("Tengu t:Prefab");

        SerializedObject so = new SerializedObject(setup);

        if (peasantGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(peasantGuids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            so.FindProperty("_peasantPrefab").objectReferenceValue = prefab;
            Debug.Log($"[SoulSystem] Found Peasant prefab: {path}");
        }
        else
        {
            Debug.LogWarning("[SoulSystem] No Peasant prefab found. Drag it into the Peasant Prefab slot.");
        }

        if (tenguGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(tenguGuids[0]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            so.FindProperty("_tenguPrefab").objectReferenceValue = prefab;
            Debug.Log($"[SoulSystem] Found Tengu prefab: {path}");
        }
        else
        {
            Debug.LogWarning("[SoulSystem] No Tengu prefab found. Drag it into the Tengu Prefab slot.");
        }

        so.ApplyModifiedProperties();

        Selection.activeGameObject = setupObj;

        Debug.Log("[SoulSystem] Setup complete!");
        Debug.Log("1. Assign character prefabs if not auto-detected");
        Debug.Log("2. Hold Q in-game to open radial menu");
        Debug.Log("3. Select a soul to swap characters");
    }

    [MenuItem("Klyra/Clear Soul Save Data")]
    public static void ClearSoulData()
    {
        PlayerPrefs.DeleteKey("UnlockedSouls");
        PlayerPrefs.DeleteKey("CurrentSoulIndex");
        PlayerPrefs.Save();
        Debug.Log("[SoulSystem] Cleared all saved soul data.");
    }
}
