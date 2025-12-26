using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class CharacterSetup : EditorWindow
{
    [MenuItem("Klyra/Setup Character Database")]
    public static void SetupCharacterDatabase()
    {
        // Create folder for character data
        string dataFolder = "Assets/Data/Characters";
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(dataFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Characters");
        }

        // Define character mappings (prefab name -> display name, is default unlock)
        var characterMappings = new Dictionary<string, (string displayName, bool defaultUnlock)>
        {
            { "SM_Chr_Generic_Male_01", ("Peasant", true) }, // Starting character
            { "SM_Chr_Generic_Female_01", ("Villager", false) },
            { "SM_Chr_Soldier_Male_01", ("Ashigaru", false) },
            { "SM_Chr_Samurai_Male_01", ("Samurai Initiate", false) },
            { "SM_Chr_Samurai_Male_02", ("Samurai Warrior", false) },
            { "SM_Chr_Samurai_Male_03", ("Samurai Master", false) },
            { "SM_Chr_Samurai_Female_01", ("Onna-bugeisha", false) },
            { "SM_Chr_Shinobi_01", ("Shinobi", false) },
            { "SM_Chr_Kunoichi_01", ("Kunoichi", false) },
            { "SM_Chr_Tengu_Male_01", ("Tengu", false) },
            { "SM_Chr_Nobility_Male_01", ("Noble", false) },
            { "SM_Chr_Noble_Female_01", ("Noblewoman", false) },
            { "SM_Chr_Geisha_01", ("Geisha", false) },
        };

        List<CharacterData> createdData = new List<CharacterData>();

        // Find all character prefabs
        string[] guids = AssetDatabase.FindAssets("SM_Chr_ t:Prefab", new[] { "Assets/Synty/PolygonSamuraiEmpire/Prefabs/Characters" });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null) continue;

            string prefabName = prefab.name;

            // Skip attachment prefabs
            if (prefabName.Contains("Attach")) continue;

            // Check if CharacterData already exists
            string dataPath = $"{dataFolder}/{prefabName}.asset";
            CharacterData existingData = AssetDatabase.LoadAssetAtPath<CharacterData>(dataPath);

            if (existingData != null)
            {
                // Update existing
                existingData.prefab = prefab;
                if (characterMappings.TryGetValue(prefabName, out var mapping))
                {
                    existingData.displayName = mapping.displayName;
                    existingData.unlockedByDefault = mapping.defaultUnlock;
                }
                EditorUtility.SetDirty(existingData);
                createdData.Add(existingData);
                continue;
            }

            // Create new CharacterData
            CharacterData charData = ScriptableObject.CreateInstance<CharacterData>();
            charData.characterId = prefabName;
            charData.prefab = prefab;

            if (characterMappings.TryGetValue(prefabName, out var map))
            {
                charData.displayName = map.displayName;
                charData.unlockedByDefault = map.defaultUnlock;
            }
            else
            {
                charData.displayName = prefabName.Replace("SM_Chr_", "").Replace("_", " ");
                charData.unlockedByDefault = false;
            }

            AssetDatabase.CreateAsset(charData, dataPath);
            createdData.Add(charData);

            Debug.Log($"[CharacterSetup] Created: {charData.displayName}");
        }

        // Create or update CharacterDatabase
        string dbPath = "Assets/Data/CharacterDatabase.asset";
        CharacterDatabase database = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(dbPath);

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(database, dbPath);
        }

        database.characters = createdData;
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the database
        Selection.activeObject = database;

        EditorUtility.DisplayDialog("Character Database Setup",
            $"Created/Updated {createdData.Count} character entries.\n\n" +
            "Database saved to: Assets/Data/CharacterDatabase.asset\n\n" +
            "Next steps:\n" +
            "1. Add CharacterSwitcher to your Player\n" +
            "2. Add CharacterUnlockManager to your scene\n" +
            "3. Assign the CharacterDatabase to both\n" +
            "4. Set enemy types on Enemy components\n" +
            "5. Press C to cycle characters!",
            "OK");
    }

    [MenuItem("Klyra/Reset Character Unlocks")]
    public static void ResetUnlocks()
    {
        PlayerPrefs.DeleteKey("UnlockedCharacters");
        PlayerPrefs.Save();
        EditorUtility.DisplayDialog("Unlocks Reset", "All character unlocks have been reset.", "OK");
    }
}
