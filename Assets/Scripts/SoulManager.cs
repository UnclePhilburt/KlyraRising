using UnityEngine;
using TMPro;

public class SoulManager : MonoBehaviour
{
    public static SoulManager Instance { get; private set; }

    [Header("Souls")]
    [SerializeField] private int _startingSouls = 0;

    [Header("UI (auto-created if not assigned)")]
    [SerializeField] private TextMeshProUGUI _soulText;

    private int _souls;
    public int Souls => _souls;

    // Passive upgrade stats
    public float BonusDamagePercent { get; set; } = 0f;
    public float BonusHealthPercent { get; set; } = 0f;
    public float LifeStealPercent { get; set; } = 0f;
    public float ThornsPercent { get; set; } = 0f;
    public float MoveSpeedPercent { get; set; } = 0f;
    public float AttackSpeedPercent { get; set; } = 0f;
    public float CritChancePercent { get; set; } = 0f;
    public float HealOnKill { get; set; } = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved souls
        _souls = PlayerPrefs.GetInt("Souls", _startingSouls);

        // Load saved upgrades
        LoadUpgrades();
    }

    void Start()
    {
        CreateUI();
        UpdateUI();
    }

    void CreateUI()
    {
        if (_soulText != null) return;

        // Create canvas
        GameObject canvasObj = new GameObject("SoulCanvas");
        canvasObj.transform.SetParent(transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Create text
        GameObject textObj = new GameObject("SoulText");
        textObj.transform.SetParent(canvasObj.transform, false);
        _soulText = textObj.AddComponent<TextMeshProUGUI>();
        _soulText.fontSize = 36;
        _soulText.color = new Color(0.8f, 0.6f, 1f); // Purple soul color
        _soulText.alignment = TextAlignmentOptions.TopRight;
        _soulText.fontStyle = FontStyles.Bold;

        RectTransform rect = _soulText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-20, -20);
        rect.sizeDelta = new Vector2(300, 50);
    }

    void UpdateUI()
    {
        if (_soulText != null)
        {
            _soulText.text = $"Souls: {_souls}";
        }
    }

    public void AddSouls(int amount)
    {
        _souls += amount;
        PlayerPrefs.SetInt("Souls", _souls);
        UpdateUI();
        Debug.Log($"[SoulManager] +{amount} souls (Total: {_souls})");
    }

    public bool SpendSouls(int amount)
    {
        if (_souls >= amount)
        {
            _souls -= amount;
            PlayerPrefs.SetInt("Souls", _souls);
            UpdateUI();
            return true;
        }
        Debug.Log($"[SoulManager] Not enough souls! Need {amount}, have {_souls}");
        return false;
    }

    public void SaveUpgrades()
    {
        PlayerPrefs.SetFloat("Upgrade_BonusDamage", BonusDamagePercent);
        PlayerPrefs.SetFloat("Upgrade_BonusHealth", BonusHealthPercent);
        PlayerPrefs.SetFloat("Upgrade_LifeSteal", LifeStealPercent);
        PlayerPrefs.SetFloat("Upgrade_Thorns", ThornsPercent);
        PlayerPrefs.SetFloat("Upgrade_MoveSpeed", MoveSpeedPercent);
        PlayerPrefs.SetFloat("Upgrade_AttackSpeed", AttackSpeedPercent);
        PlayerPrefs.SetFloat("Upgrade_CritChance", CritChancePercent);
        PlayerPrefs.SetFloat("Upgrade_HealOnKill", HealOnKill);
        PlayerPrefs.Save();
    }

    void LoadUpgrades()
    {
        BonusDamagePercent = PlayerPrefs.GetFloat("Upgrade_BonusDamage", 0f);
        BonusHealthPercent = PlayerPrefs.GetFloat("Upgrade_BonusHealth", 0f);
        LifeStealPercent = PlayerPrefs.GetFloat("Upgrade_LifeSteal", 0f);
        ThornsPercent = PlayerPrefs.GetFloat("Upgrade_Thorns", 0f);
        MoveSpeedPercent = PlayerPrefs.GetFloat("Upgrade_MoveSpeed", 0f);
        AttackSpeedPercent = PlayerPrefs.GetFloat("Upgrade_AttackSpeed", 0f);
        CritChancePercent = PlayerPrefs.GetFloat("Upgrade_CritChance", 0f);
        HealOnKill = PlayerPrefs.GetFloat("Upgrade_HealOnKill", 0f);
    }

    [ContextMenu("Add 100 Souls (Debug)")]
    public void DebugAddSouls()
    {
        AddSouls(100);
    }

    [ContextMenu("Reset All (Debug)")]
    public void DebugReset()
    {
        PlayerPrefs.DeleteAll();
        _souls = 0;
        BonusDamagePercent = 0;
        BonusHealthPercent = 0;
        LifeStealPercent = 0;
        ThornsPercent = 0;
        MoveSpeedPercent = 0;
        AttackSpeedPercent = 0;
        CritChancePercent = 0;
        HealOnKill = 0;
        UpdateUI();
    }
}
