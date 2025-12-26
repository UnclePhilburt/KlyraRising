using UnityEngine;
using UnityEditor;

public class SwordEffectsSetup : EditorWindow
{
    [MenuItem("Klyra/Setup Sword Effects")]
    public static void SetupSwordEffects()
    {
        // Find all effect prefabs
        GameObject lightSlash = FindPrefab("FX_SwordSlash_01");
        GameObject heavySlash = FindPrefab("FX_Slash_Large_01");
        GameObject stab = FindPrefab("FX_SwordStab_01");
        GameObject finisher = FindPrefab("FX_Slash_01");
        GameObject blood = FindPrefab("FX_BloodSplat_01");
        GameObject sparks = FindPrefab("FX_Sparks_01");

        if (lightSlash == null && heavySlash == null)
        {
            EditorUtility.DisplayDialog("Effects Not Found",
                "Could not find sword effect prefabs.\n\n" +
                "Make sure PolygonParticleFX is imported.", "OK");
            return;
        }

        // Find all SwordHitbox components in scene
        SwordHitbox[] hitboxes = FindObjectsByType<SwordHitbox>(FindObjectsSortMode.None);
        int updated = 0;

        foreach (SwordHitbox hitbox in hitboxes)
        {
            UpdateHitbox(hitbox, lightSlash, heavySlash, stab, finisher, blood, sparks);
            updated++;
        }

        // Also check prefabs in selection
        foreach (GameObject obj in Selection.gameObjects)
        {
            SwordHitbox hitbox = obj.GetComponentInChildren<SwordHitbox>();
            if (hitbox != null)
            {
                UpdateHitbox(hitbox, lightSlash, heavySlash, stab, finisher, blood, sparks);
                updated++;
            }
        }

        AssetDatabase.SaveAssets();

        string message = $"Updated {updated} SwordHitbox component(s).\n\n";
        message += "Assigned Effects:\n";
        if (lightSlash != null) message += $"  Light Slash: {lightSlash.name}\n";
        if (heavySlash != null) message += $"  Heavy Slash: {heavySlash.name}\n";
        if (stab != null) message += $"  Stab: {stab.name}\n";
        if (finisher != null) message += $"  Finisher: {finisher.name}\n";
        if (blood != null) message += $"  Blood: {blood.name}\n";
        if (sparks != null) message += $"  Sparks: {sparks.name}\n";

        message += "\nDon't forget to Apply changes to your prefab!";

        EditorUtility.DisplayDialog("Sword Effects Setup", message, "OK");
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

    static void UpdateHitbox(SwordHitbox hitbox, GameObject lightSlash, GameObject heavySlash,
                              GameObject stab, GameObject finisher, GameObject blood, GameObject sparks)
    {
        SerializedObject so = new SerializedObject(hitbox);

        SetProperty(so, "_lightSlashPrefab", lightSlash);
        SetProperty(so, "_heavySlashPrefab", heavySlash);
        SetProperty(so, "_stabPrefab", stab);
        SetProperty(so, "_comboFinisherPrefab", finisher);
        SetProperty(so, "_hitEffectPrefab", blood);
        SetProperty(so, "_hitSparksPrefab", sparks);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hitbox);
    }

    static void SetProperty(SerializedObject so, string propName, GameObject prefab)
    {
        if (prefab == null) return;

        SerializedProperty prop = so.FindProperty(propName);
        if (prop != null)
        {
            prop.objectReferenceValue = prefab;
        }
    }
}
