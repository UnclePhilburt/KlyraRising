using UnityEngine;
using UnityEditor;

public class CharacterSwitchEffectsSetup : EditorWindow
{
    [MenuItem("Klyra/Setup Character Switch Effects")]
    public static void SetupSwitchEffects()
    {
        // Find smoke prefabs - prefer white large smoke
        GameObject smokePrefab = FindPrefab("FX_Smoke_White_Large_01");
        if (smokePrefab == null)
            smokePrefab = FindPrefab("FX_Smoke_Black_Large_01");
        if (smokePrefab == null)
            smokePrefab = FindPrefab("FX_Grenade_Smoke_01");

        if (smokePrefab == null)
        {
            EditorUtility.DisplayDialog("Effects Not Found",
                "Could not find smoke effect prefabs.\n\n" +
                "Make sure PolygonParticleFX is imported.", "OK");
            return;
        }

        // Find CharacterUnlockManager in scene
        CharacterUnlockManager manager = FindFirstObjectByType<CharacterUnlockManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog("Manager Not Found",
                "Could not find CharacterUnlockManager in the scene.", "OK");
            return;
        }

        // Update the manager
        SerializedObject so = new SerializedObject(manager);

        SerializedProperty smokeProp = so.FindProperty("_smokeBombPrefab");
        if (smokeProp != null)
        {
            smokeProp.objectReferenceValue = smokePrefab;
        }

        // Set good defaults for thick smoke cloud
        SerializedProperty scaleProp = so.FindProperty("_smokeScale");
        if (scaleProp != null)
        {
            scaleProp.floatValue = 3f; // Large enough to cover player
        }

        SerializedProperty durationProp = so.FindProperty("_smokeDuration");
        if (durationProp != null)
        {
            durationProp.floatValue = 3f;
        }

        SerializedProperty layerProp = so.FindProperty("_smokeLayerCount");
        if (layerProp != null)
        {
            layerProp.intValue = 4; // Multiple layers for thickness
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(manager);

        string message = "Character Switch Effects Setup Complete!\n\n";
        message += $"Smoke Effect: {smokePrefab.name}\n";
        message += "Scale: 3\n";
        message += "Layers: 4 (stacked for thickness)\n";
        message += "Duration: 3 seconds\n\n";
        message += "Adjust values in Inspector if needed.";

        EditorUtility.DisplayDialog("Setup Complete", message, "OK");
    }

    static GameObject FindPrefab(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:Prefab");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        return null;
    }
}
