using UnityEngine;

public class SoulSystemSetup : MonoBehaviour
{
    [Header("Auto-creates soul system if not present")]
    [SerializeField] private bool _createTestSouls = true;

    [Header("Character Prefabs (assign these!)")]
    [SerializeField] private GameObject _peasantPrefab;
    [SerializeField] private GameObject _tenguPrefab;

    void Awake()
    {
        // Create SoulCollection if needed
        if (SoulCollection.Instance == null)
        {
            GameObject collectionObj = new GameObject("SoulCollection");
            SoulCollection collection = collectionObj.AddComponent<SoulCollection>();
            DontDestroyOnLoad(collectionObj);
        }

        // Create RadialMenu if needed
        if (SoulRadialMenu.Instance == null)
        {
            GameObject menuObj = new GameObject("SoulRadialMenu");
            SoulRadialMenu menu = menuObj.AddComponent<SoulRadialMenu>();
            DontDestroyOnLoad(menuObj);

            if (_createTestSouls)
            {
                SetupTestSouls(menu, SoulCollection.Instance);
            }
        }

        // Create CharacterSwapper if needed
        if (CharacterSwapper.Instance == null)
        {
            GameObject swapperObj = new GameObject("CharacterSwapper");
            swapperObj.AddComponent<CharacterSwapper>();
            DontDestroyOnLoad(swapperObj);
        }
    }

    void SetupTestSouls(SoulRadialMenu menu, SoulCollection collection)
    {
        // Starting souls - Peasant and Tengu
        SoulData peasant = new SoulData
        {
            soulName = "Peasant",
            description = "Where your journey began",
            unlockedByDefault = true,
            characterPrefab = _peasantPrefab
        };

        SoulData tengu = new SoulData
        {
            soulName = "Tengu",
            description = "Swift demon warrior",
            unlockedByDefault = true,
            characterPrefab = _tenguPrefab
        };

        // Add to collection
        collection.AllSouls.Add(peasant);
        collection.AllSouls.Add(tengu);

        // Unlock both
        collection.UnlockSoul("Peasant");
        collection.UnlockSoul("Tengu");

        // Equip to radial menu - 2 filled, 3 empty
        menu.EquipSoul(peasant, 0);
        menu.EquipSoul(tengu, 1);
        // Slots 2, 3, 4 remain empty (null)

        Debug.Log("[SoulSystem] Souls ready! Hold Q to open radial menu.");
        if (_peasantPrefab == null || _tenguPrefab == null)
        {
            Debug.LogWarning("[SoulSystem] Assign character prefabs in SoulSystemSetup to enable swapping!");
        }
        else
        {
            Debug.Log("[SoulSystem] Character swapping enabled. Select a soul to transform!");
        }
    }
}
