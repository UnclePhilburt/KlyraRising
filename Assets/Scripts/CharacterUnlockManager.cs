using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CharacterUnlockManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterDatabase _characterDatabase;
    [SerializeField] private CharacterSwitcher _characterSwitcher;

    [Header("Settings")]
    [SerializeField] private KeyCode _cycleKey = KeyCode.C;
    [SerializeField] private bool _showUnlockPopup = true;

    [Header("Switch Effects")]
    [SerializeField] private GameObject _smokeBombPrefab;
    [SerializeField] private float _smokeScale = 3f;
    [SerializeField] private float _smokeDuration = 3f;
    [SerializeField] private int _smokeLayerCount = 4;  // Stack multiple for thickness

    private HashSet<string> _unlockedCharacters = new HashSet<string>();
    private List<string> _unlockedList = new List<string>(); // For cycling
    private int _currentIndex = 0;

    private const string SAVE_KEY = "UnlockedCharacters";

    public static CharacterUnlockManager Instance { get; private set; }

    public event System.Action<CharacterData> OnCharacterUnlocked;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);  // Only destroy the component, not the whole GameObject!
            return;
        }
        Instance = this;

        LoadUnlocks();
    }

    void Start()
    {
        // Find references if not set
        if (_characterSwitcher == null)
        {
            _characterSwitcher = FindFirstObjectByType<CharacterSwitcher>();
        }

        // Unlock default characters
        CharacterData defaultCharacter = null;
        if (_characterDatabase != null)
        {
            foreach (var charData in _characterDatabase.characters)
            {
                if (charData.unlockedByDefault)
                {
                    UnlockCharacter(charData.characterId, silent: true);
                    if (defaultCharacter == null)
                    {
                        defaultCharacter = charData;
                    }
                }
            }
        }

        UpdateUnlockedList();

        // Switch to the default character at start
        if (defaultCharacter != null && _characterSwitcher != null)
        {
            _characterSwitcher.SwitchToCharacter(defaultCharacter);
            _currentIndex = 0;
        }
    }

    void Update()
    {
        // Cycle through unlocked characters
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Debug.Log("[CharacterUnlockManager] C key pressed!");
            CycleCharacter();
        }
    }

    public void UnlockCharacter(string characterId, bool silent = false)
    {
        if (_unlockedCharacters.Contains(characterId)) return;

        _unlockedCharacters.Add(characterId);
        UpdateUnlockedList();
        SaveUnlocks();

        if (!silent)
        {
            CharacterData charData = _characterDatabase?.GetCharacter(characterId);
            if (charData != null)
            {
                Debug.Log($"[Unlock] Character unlocked: {charData.displayName}");
                OnCharacterUnlocked?.Invoke(charData);

                if (_showUnlockPopup)
                {
                    ShowUnlockPopup(charData);
                }
            }
        }
    }

    public bool IsUnlocked(string characterId)
    {
        return _unlockedCharacters.Contains(characterId);
    }

    public List<CharacterData> GetUnlockedCharacters()
    {
        List<CharacterData> result = new List<CharacterData>();
        if (_characterDatabase == null) return result;

        foreach (string id in _unlockedCharacters)
        {
            CharacterData data = _characterDatabase.GetCharacter(id);
            if (data != null) result.Add(data);
        }
        return result;
    }

    public void CycleCharacter()
    {
        Debug.Log($"[CharacterUnlockManager] CycleCharacter called. Unlocked count: {_unlockedList.Count}, Switcher: {_characterSwitcher != null}");

        if (_unlockedList.Count <= 1)
        {
            Debug.Log("[CharacterUnlockManager] Only 1 or fewer characters unlocked, skipping");
            return;
        }
        if (_characterSwitcher == null)
        {
            Debug.Log("[CharacterUnlockManager] No character switcher!");
            return;
        }

        _currentIndex = (_currentIndex + 1) % _unlockedList.Count;
        string nextId = _unlockedList[_currentIndex];

        Debug.Log($"[CharacterUnlockManager] Starting switch coroutine to: {nextId}");
        StartCoroutine(SwitchWithWeaponHandling(nextId));
    }

    public void CycleCharacterReverse()
    {
        if (_unlockedList.Count <= 1) return;
        if (_characterSwitcher == null) return;

        _currentIndex = (_currentIndex - 1 + _unlockedList.Count) % _unlockedList.Count;
        string nextId = _unlockedList[_currentIndex];

        StartCoroutine(SwitchWithWeaponHandling(nextId));
    }

    System.Collections.IEnumerator SwitchWithWeaponHandling(string characterId)
    {
        // Get weapon controller
        WeaponController weapon = _characterSwitcher.GetComponent<WeaponController>();
        bool wasEquipped = weapon != null && weapon.IsSwordEquipped;

        // Force sheathe before switch
        if (wasEquipped && weapon != null)
        {
            weapon.ForceSheatheForSwitch();
            yield return null; // Wait a frame
        }

        // Spawn smoke bomb effect at player position
        SpawnSwitchSmoke();

        // Small delay for smoke to appear before switch
        yield return new WaitForSeconds(0.1f);

        // Do the switch
        _characterSwitcher.SwitchCharacter(characterId);

        // Re-equip after switch
        if (wasEquipped && weapon != null)
        {
            weapon.ForceDrawAfterSwitch();
        }
    }

    void SpawnSwitchSmoke()
    {
        if (_smokeBombPrefab == null)
        {
            Debug.LogWarning("[CharacterUnlockManager] No smoke bomb prefab assigned!");
            return;
        }
        if (_characterSwitcher == null) return;

        Vector3 basePos = _characterSwitcher.transform.position;
        Debug.Log($"[CharacterUnlockManager] Spawning {_smokeLayerCount} smoke layers at {basePos}");

        // Spawn multiple smoke layers for a thick, opaque cloud
        for (int i = 0; i < _smokeLayerCount; i++)
        {
            // Offset each layer slightly in different directions
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                i * 0.4f + 1f,  // Stack vertically, start at waist height
                Random.Range(-0.5f, 0.5f)
            );

            Vector3 spawnPos = basePos + offset;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject smoke = Instantiate(_smokeBombPrefab, spawnPos, rotation);
            smoke.transform.localScale = Vector3.one * _smokeScale;

            // Force particle system to play
            ParticleSystem ps = smoke.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }
            // Also check children
            foreach (ParticleSystem childPs in smoke.GetComponentsInChildren<ParticleSystem>())
            {
                childPs.Play();
            }

            // Fade out and destroy
            StartCoroutine(FadeOutSmoke(smoke, _smokeDuration));
        }
    }

    System.Collections.IEnumerator FadeOutSmoke(GameObject smoke, float duration)
    {
        float fadeStartTime = duration * 0.6f;  // Start fading at 60% of duration
        float fadeDuration = duration * 0.4f;   // Fade over remaining 40%

        yield return new WaitForSeconds(fadeStartTime);

        // Get all particle systems
        ParticleSystem[] systems = smoke.GetComponentsInChildren<ParticleSystem>();

        // Store original start colors
        Color[] originalColors = new Color[systems.Length];
        for (int i = 0; i < systems.Length; i++)
        {
            var main = systems[i].main;
            originalColors[i] = main.startColor.color;
        }

        // Fade out over time
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / fadeDuration);

            for (int i = 0; i < systems.Length; i++)
            {
                var main = systems[i].main;
                Color c = originalColors[i];
                c.a = originalColors[i].a * alpha;
                main.startColor = c;
            }

            yield return null;
        }

        Destroy(smoke);
    }

    void UpdateUnlockedList()
    {
        _unlockedList = new List<string>(_unlockedCharacters);

        // Update current index to match current character
        if (_characterSwitcher != null && !string.IsNullOrEmpty(_characterSwitcher.CurrentCharacterId))
        {
            int idx = _unlockedList.IndexOf(_characterSwitcher.CurrentCharacterId);
            if (idx >= 0) _currentIndex = idx;
        }
    }

    void ShowUnlockPopup(CharacterData charData)
    {
        // TODO: Show UI popup
        // For now just log
        Debug.Log($"<color=yellow>NEW CHARACTER UNLOCKED: {charData.displayName}</color>");
    }

    void SaveUnlocks()
    {
        string data = string.Join(",", _unlockedCharacters);
        PlayerPrefs.SetString(SAVE_KEY, data);
        PlayerPrefs.Save();
    }

    void LoadUnlocks()
    {
        string data = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(data))
        {
            string[] ids = data.Split(',');
            foreach (string id in ids)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    _unlockedCharacters.Add(id);
                }
            }
        }
    }

    // Call this to reset all unlocks (for testing)
    public void ResetUnlocks()
    {
        _unlockedCharacters.Clear();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();

        // Re-unlock defaults
        if (_characterDatabase != null)
        {
            foreach (var charData in _characterDatabase.characters)
            {
                if (charData.unlockedByDefault)
                {
                    UnlockCharacter(charData.characterId, silent: true);
                }
            }
        }

        UpdateUnlockedList();
        Debug.Log("[Unlock] All unlocks reset!");
    }
}
