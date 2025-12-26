using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SoulData
{
    public string soulName;
    public string description;
    public Sprite icon;
    public GameObject characterPrefab;
    public bool unlockedByDefault;
}

public class SoulCollection : MonoBehaviour
{
    public static SoulCollection Instance { get; private set; }

    [Header("Available Souls")]
    [SerializeField] private List<SoulData> _allSouls = new List<SoulData>();

    [Header("Current Soul")]
    [SerializeField] private int _currentSoulIndex = 0;

    private HashSet<string> _unlockedSouls = new HashSet<string>();

    public List<SoulData> AllSouls => _allSouls;
    public SoulData CurrentSoul => _allSouls.Count > 0 ? _allSouls[_currentSoulIndex] : null;
    public int CurrentSoulIndex => _currentSoulIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadUnlockedSouls();
    }

    void LoadUnlockedSouls()
    {
        _unlockedSouls.Clear();

        // Load from PlayerPrefs
        string saved = PlayerPrefs.GetString("UnlockedSouls", "");
        if (!string.IsNullOrEmpty(saved))
        {
            string[] names = saved.Split(',');
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name))
                    _unlockedSouls.Add(name);
            }
        }

        // Add default unlocks
        foreach (var soul in _allSouls)
        {
            if (soul.unlockedByDefault)
            {
                _unlockedSouls.Add(soul.soulName);
            }
        }

        // Load current soul
        _currentSoulIndex = PlayerPrefs.GetInt("CurrentSoulIndex", 0);
        if (_currentSoulIndex >= _allSouls.Count) _currentSoulIndex = 0;

        Debug.Log($"[SoulCollection] Loaded {_unlockedSouls.Count} unlocked souls");
    }

    void SaveUnlockedSouls()
    {
        string saved = string.Join(",", _unlockedSouls);
        PlayerPrefs.SetString("UnlockedSouls", saved);
        PlayerPrefs.SetInt("CurrentSoulIndex", _currentSoulIndex);
        PlayerPrefs.Save();
    }

    public bool IsSoulUnlocked(string soulName)
    {
        return _unlockedSouls.Contains(soulName);
    }

    public bool IsSoulUnlocked(int index)
    {
        if (index < 0 || index >= _allSouls.Count) return false;
        return IsSoulUnlocked(_allSouls[index].soulName);
    }

    public void UnlockSoul(string soulName)
    {
        if (_unlockedSouls.Add(soulName))
        {
            Debug.Log($"[SoulCollection] SOUL ABSORBED: {soulName}!");
            SaveUnlockedSouls();

            // TODO: Play unlock effect/sound
        }
    }

    public bool SelectSoul(int index)
    {
        if (index < 0 || index >= _allSouls.Count) return false;

        if (!IsSoulUnlocked(index))
        {
            Debug.Log($"[SoulCollection] Soul '{_allSouls[index].soulName}' is locked!");
            return false;
        }

        _currentSoulIndex = index;
        SaveUnlockedSouls();
        Debug.Log($"[SoulCollection] Selected: {_allSouls[index].soulName}");
        return true;
    }

    public int GetUnlockedCount()
    {
        int count = 0;
        foreach (var soul in _allSouls)
        {
            if (IsSoulUnlocked(soul.soulName)) count++;
        }
        return count;
    }

    [ContextMenu("Unlock All Souls (Debug)")]
    public void DebugUnlockAll()
    {
        foreach (var soul in _allSouls)
        {
            UnlockSoul(soul.soulName);
        }
    }

    [ContextMenu("Reset All Souls (Debug)")]
    public void DebugResetAll()
    {
        _unlockedSouls.Clear();
        PlayerPrefs.DeleteKey("UnlockedSouls");
        PlayerPrefs.DeleteKey("CurrentSoulIndex");
        LoadUnlockedSouls();
    }
}
