using UnityEngine;

[CreateAssetMenu(fileName = "CharacterData", menuName = "Klyra/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Character Info")]
    public string characterId; // Must match prefab name (e.g., "SM_Chr_Samurai_Male_01")
    public string displayName;
    public GameObject prefab;

    [Header("Unlock Settings")]
    public bool unlockedByDefault = false;

    [Header("Display")]
    public Sprite icon;
    [TextArea] public string description;
}
