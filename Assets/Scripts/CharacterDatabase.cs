using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CharacterDatabase", menuName = "Klyra/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterData> characters = new List<CharacterData>();

    public CharacterData GetCharacter(string characterId)
    {
        return characters.Find(c => c.characterId == characterId);
    }

    public CharacterData GetDefaultCharacter()
    {
        return characters.Find(c => c.unlockedByDefault);
    }

    public List<CharacterData> GetAllUnlockedByDefault()
    {
        return characters.FindAll(c => c.unlockedByDefault);
    }
}
