using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;

public class SoulRadialMenu : MonoBehaviour
{
    public static SoulRadialMenu Instance { get; private set; }

    [Header("Menu Settings")]
    [SerializeField] private float _menuRadius = 150f;
    [SerializeField] private float _slotSize = 80f;
    [SerializeField] private float _deadZone = 30f;
    [SerializeField] private Color _normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color _selectedColor = new Color(0.8f, 0.6f, 1f, 0.9f);
    [SerializeField] private Color _emptyColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);

    [Header("Equipped Souls (max 5)")]
    [SerializeField] private List<SoulData> _equippedSouls = new List<SoulData>();

    [Header("References")]
    [SerializeField] private SoulCollection _soulCollection;

    // UI
    private Canvas _canvas;
    private GameObject _menuRoot;
    private List<GameObject> _slotObjects = new List<GameObject>();
    private List<Image> _slotImages = new List<Image>();
    private List<Image> _slotIcons = new List<Image>();
    private List<TextMeshProUGUI> _slotLabels = new List<TextMeshProUGUI>();
    private TextMeshProUGUI _centerLabel;
    private Image _centerImage;

    private bool _isOpen = false;
    private int _selectedIndex = -1;
    private int _previousSelected = -1;

    public bool IsOpen => _isOpen;
    public List<SoulData> EquippedSouls => _equippedSouls;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (_soulCollection == null)
            _soulCollection = FindFirstObjectByType<SoulCollection>();

        CreateMenuUI();
        CloseMenu();
    }

    void Update()
    {
        // Check for open input using new Input System
        bool shouldOpen = false;

        // Keyboard: Q key
        if (Keyboard.current != null)
        {
            shouldOpen = Keyboard.current.qKey.isPressed;
        }

        // Gamepad: Left Bumper (LB)
        if (!shouldOpen && Gamepad.current != null)
        {
            shouldOpen = Gamepad.current.leftShoulder.isPressed;
        }

        if (shouldOpen && !_isOpen)
        {
            OpenMenu();
        }
        else if (!shouldOpen && _isOpen)
        {
            CloseMenu();
        }

        if (_isOpen)
        {
            UpdateSelection();
        }
    }

    void CreateMenuUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("RadialMenuCanvas");
        canvasObj.transform.SetParent(transform);
        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Menu root (centered)
        _menuRoot = new GameObject("MenuRoot");
        _menuRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform rootRect = _menuRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;

        // Center circle
        GameObject centerObj = new GameObject("Center");
        centerObj.transform.SetParent(_menuRoot.transform, false);
        _centerImage = centerObj.AddComponent<Image>();
        _centerImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        RectTransform centerRect = centerObj.GetComponent<RectTransform>();
        centerRect.sizeDelta = new Vector2(_deadZone * 2, _deadZone * 2);

        // Center label
        GameObject centerLabelObj = new GameObject("CenterLabel");
        centerLabelObj.transform.SetParent(centerObj.transform, false);
        _centerLabel = centerLabelObj.AddComponent<TextMeshProUGUI>();
        _centerLabel.text = "SOULS";
        _centerLabel.fontSize = 18;
        _centerLabel.alignment = TextAlignmentOptions.Center;
        _centerLabel.color = Color.white;
        RectTransform labelRect = _centerLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Create 5 slots around the circle
        CreateSlots(5);
    }

    void CreateSlots(int count)
    {
        _slotObjects.Clear();
        _slotImages.Clear();
        _slotIcons.Clear();
        _slotLabels.Clear();

        for (int i = 0; i < count; i++)
        {
            // Calculate position around circle (start at top, go clockwise)
            float angle = (90f - (i * (360f / count))) * Mathf.Deg2Rad;
            Vector2 pos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _menuRadius;

            // Slot background
            GameObject slotObj = new GameObject($"Slot_{i}");
            slotObj.transform.SetParent(_menuRoot.transform, false);
            Image slotImg = slotObj.AddComponent<Image>();
            slotImg.color = _normalColor;
            RectTransform slotRect = slotObj.GetComponent<RectTransform>();
            slotRect.anchoredPosition = pos;
            slotRect.sizeDelta = new Vector2(_slotSize, _slotSize);

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(slotObj.transform, false);
            Image iconImg = iconObj.AddComponent<Image>();
            iconImg.color = Color.white;
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10, 25);
            iconRect.offsetMax = new Vector2(-10, -10);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 12;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            RectTransform labelRectT = label.GetComponent<RectTransform>();
            labelRectT.anchorMin = new Vector2(0, 0);
            labelRectT.anchorMax = new Vector2(1, 0);
            labelRectT.pivot = new Vector2(0.5f, 0);
            labelRectT.anchoredPosition = new Vector2(0, 5);
            labelRectT.sizeDelta = new Vector2(0, 20);

            // Number indicator
            GameObject numObj = new GameObject("Number");
            numObj.transform.SetParent(slotObj.transform, false);
            TextMeshProUGUI numLabel = numObj.AddComponent<TextMeshProUGUI>();
            numLabel.text = (i + 1).ToString();
            numLabel.fontSize = 14;
            numLabel.alignment = TextAlignmentOptions.TopRight;
            numLabel.color = new Color(1, 1, 1, 0.5f);
            RectTransform numRect = numLabel.GetComponent<RectTransform>();
            numRect.anchorMin = Vector2.zero;
            numRect.anchorMax = Vector2.one;
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = new Vector2(-5, -2);

            _slotObjects.Add(slotObj);
            _slotImages.Add(slotImg);
            _slotIcons.Add(iconImg);
            _slotLabels.Add(label);
        }
    }

    void OpenMenu()
    {
        _isOpen = true;
        _menuRoot.SetActive(true);
        _selectedIndex = -1;

        RefreshSlots();

        // Unlock cursor for selection
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseMenu()
    {
        // Select soul if one was highlighted
        if (_isOpen && _selectedIndex >= 0 && _selectedIndex < _equippedSouls.Count)
        {
            SelectSoul(_selectedIndex);
        }

        _isOpen = false;
        _menuRoot.SetActive(false);
        _selectedIndex = -1;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void RefreshSlots()
    {
        for (int i = 0; i < _slotObjects.Count; i++)
        {
            if (i < _equippedSouls.Count && _equippedSouls[i] != null)
            {
                // Filled slot
                SoulData soul = _equippedSouls[i];
                _slotLabels[i].text = soul.soulName;

                if (soul.icon != null)
                {
                    _slotIcons[i].sprite = soul.icon;
                    _slotIcons[i].enabled = true;
                }
                else
                {
                    _slotIcons[i].enabled = false;
                }

                _slotImages[i].color = _normalColor;
            }
            else
            {
                // Empty slot
                _slotLabels[i].text = "- - -";
                _slotIcons[i].enabled = false;
                _slotImages[i].color = _emptyColor;
            }
        }
    }

    void UpdateSelection()
    {
        // Get input direction (mouse or controller)
        Vector2 input = Vector2.zero;

        // Mouse input - position relative to screen center
        Vector2 mousePos = Vector2.zero;
        if (Mouse.current != null)
        {
            mousePos = Mouse.current.position.ReadValue();
        }
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mouseDir = mousePos - screenCenter;

        // Controller input - left stick
        Vector2 stickDir = Vector2.zero;
        if (Gamepad.current != null)
        {
            stickDir = Gamepad.current.leftStick.ReadValue();
        }

        // Use whichever has more input
        input = stickDir.magnitude > 0.3f ? stickDir : mouseDir;

        // Check if outside dead zone
        if (input.magnitude < _deadZone)
        {
            _selectedIndex = -1;
        }
        else
        {
            // Find which slot we're pointing at
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            // Convert to slot index (0 is at top/90 degrees)
            float slotAngle = 360f / _slotObjects.Count;
            float adjusted = (90f - angle + slotAngle / 2f + 360f) % 360f;
            _selectedIndex = Mathf.FloorToInt(adjusted / slotAngle) % _slotObjects.Count;
        }

        // Update visuals
        if (_selectedIndex != _previousSelected)
        {
            for (int i = 0; i < _slotImages.Count; i++)
            {
                bool hasSoul = i < _equippedSouls.Count && _equippedSouls[i] != null;

                if (i == _selectedIndex && hasSoul)
                {
                    _slotImages[i].color = _selectedColor;
                    _slotObjects[i].transform.localScale = Vector3.one * 1.15f;
                    _centerLabel.text = _equippedSouls[i].soulName.ToUpper();
                }
                else
                {
                    _slotImages[i].color = hasSoul ? _normalColor : _emptyColor;
                    _slotObjects[i].transform.localScale = Vector3.one;
                }
            }

            // Show empty slot message or default
            if (_selectedIndex >= 0 && (_selectedIndex >= _equippedSouls.Count || _equippedSouls[_selectedIndex] == null))
            {
                _centerLabel.text = "EMPTY";
            }
            else if (_selectedIndex < 0)
            {
                _centerLabel.text = "SOULS";
            }

            _previousSelected = _selectedIndex;
        }
    }

    void SelectSoul(int index)
    {
        if (index < 0 || index >= _equippedSouls.Count) return;

        SoulData soul = _equippedSouls[index];
        if (soul == null) return;

        if (_soulCollection != null && !_soulCollection.IsSoulUnlocked(soul.soulName))
        {
            Debug.Log($"[RadialMenu] Soul '{soul.soulName}' is locked!");
            return;
        }

        Debug.Log($"[RadialMenu] Selected: {soul.soulName}");

        // TODO: Actually swap the character
        // For now, just notify
        OnSoulSelected?.Invoke(soul);
    }

    public void EquipSoul(SoulData soul, int slot)
    {
        while (_equippedSouls.Count <= slot)
            _equippedSouls.Add(null);

        _equippedSouls[slot] = soul;
    }

    // Event for when a soul is selected
    public System.Action<SoulData> OnSoulSelected;
}
