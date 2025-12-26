using UnityEngine;
using UnityEditor;

public class EnemyCharacterSetup : EditorWindow
{
    [MenuItem("Klyra/Setup Enemy Character IDs")]
    public static void SetupAllEnemies()
    {
        int updated = 0;

        // Find all Enemy components in the scene
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        foreach (Enemy enemy in enemies)
        {
            if (SetupEnemy(enemy))
            {
                updated++;
            }
        }

        // Also check prefabs in selection
        foreach (GameObject obj in Selection.gameObjects)
        {
            Enemy enemy = obj.GetComponent<Enemy>();
            if (enemy != null && SetupEnemy(enemy))
            {
                updated++;
            }
        }

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Enemy Character Setup",
            $"Updated {updated} enemies with character model IDs.", "OK");
    }

    [MenuItem("Klyra/Setup Selected Enemy Character ID")]
    public static void SetupSelectedEnemy()
    {
        if (Selection.activeGameObject == null)
        {
            EditorUtility.DisplayDialog("No Selection", "Please select an enemy GameObject.", "OK");
            return;
        }

        Enemy enemy = Selection.activeGameObject.GetComponent<Enemy>();
        if (enemy == null)
        {
            EditorUtility.DisplayDialog("No Enemy", "Selected object doesn't have an Enemy component.", "OK");
            return;
        }

        if (SetupEnemy(enemy))
        {
            EditorUtility.DisplayDialog("Success",
                $"Set character model ID on {enemy.gameObject.name}", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Failed",
                "Couldn't find a valid SM_Chr_ model on this enemy.", "OK");
        }
    }

    static bool SetupEnemy(Enemy enemy)
    {
        string modelId = FindCharacterModel(enemy.gameObject);

        if (string.IsNullOrEmpty(modelId))
        {
            Debug.LogWarning($"[EnemySetup] No character model found for {enemy.gameObject.name}");
            return false;
        }

        // Use SerializedObject to set the private field
        SerializedObject so = new SerializedObject(enemy);
        SerializedProperty prop = so.FindProperty("_characterModelId");

        if (prop != null)
        {
            prop.stringValue = modelId;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(enemy);
            Debug.Log($"[EnemySetup] {enemy.gameObject.name} -> {modelId}");
            return true;
        }

        return false;
    }

    static string FindCharacterModel(GameObject enemyObj)
    {
        // Find ALL SM_Chr_ models (not attachments)
        System.Collections.Generic.List<Transform> candidates = new System.Collections.Generic.List<Transform>();

        FindAllCharacterModels(enemyObj.transform, candidates);

        Debug.Log($"[EnemySetup] {enemyObj.name} has {candidates.Count} character model candidates:");
        foreach (var c in candidates)
        {
            Debug.Log($"  - {c.name}");
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0].name;
        }

        // Multiple models found - pick the best one
        // Priority: one with most active SkinnedMeshRenderers
        Transform best = null;
        int bestScore = -1;

        foreach (var candidate in candidates)
        {
            int score = 0;

            // Count active skinned mesh renderers
            var renderers = candidate.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var r in renderers)
            {
                if (r.enabled && r.gameObject.activeInHierarchy)
                {
                    score += 10;
                }
            }

            // Bonus if it has an Animator
            if (candidate.GetComponent<Animator>() != null)
            {
                score += 5;
            }

            // Penalty for generic names (likely placeholder)
            if (candidate.name.Contains("Generic"))
            {
                score -= 3;
            }

            Debug.Log($"  {candidate.name} score: {score}");

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best?.name;
    }

    static void FindAllCharacterModels(Transform parent, System.Collections.Generic.List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            // Check if this is a character model (not attachment)
            if (child.name.StartsWith("SM_Chr_") && !child.name.Contains("Attach"))
            {
                results.Add(child);
            }

            // Keep searching children
            FindAllCharacterModels(child, results);
        }
    }
}
