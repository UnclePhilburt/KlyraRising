using UnityEngine;

public class CharacterSwapper : MonoBehaviour
{
    public static CharacterSwapper Instance { get; private set; }

    [Header("Current Character")]
    [SerializeField] private GameObject _currentCharacter;

    [Header("Swap Settings")]
    [SerializeField] private float _swapEffectDuration = 0.3f;
    [SerializeField] private GameObject _swapEffectPrefab;

    private bool _isSwapping = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Find player if not assigned
        FindCurrentCharacter();

        // Subscribe to radial menu
        SubscribeToRadialMenu();
    }

    void Update()
    {
        // Keep trying to subscribe if not yet done
        if (SoulRadialMenu.Instance != null && SoulRadialMenu.Instance.OnSoulSelected == null)
        {
            SubscribeToRadialMenu();
        }

        // Keep track of current character
        if (_currentCharacter == null)
        {
            FindCurrentCharacter();
        }
    }

    void FindCurrentCharacter()
    {
        if (_currentCharacter == null)
        {
            // Try to find by ThirdPersonController
            var player = FindFirstObjectByType<ThirdPersonController>();
            if (player != null)
            {
                _currentCharacter = player.gameObject;
                Debug.Log($"[CharacterSwapper] Found current character: {_currentCharacter.name}");
                return;
            }

            // Fallback: find by Player tag
            var playerByTag = GameObject.FindGameObjectWithTag("Player");
            if (playerByTag != null)
            {
                _currentCharacter = playerByTag;
                Debug.Log($"[CharacterSwapper] Found current character by tag: {_currentCharacter.name}");
            }
        }
    }

    void SubscribeToRadialMenu()
    {
        if (SoulRadialMenu.Instance != null)
        {
            SoulRadialMenu.Instance.OnSoulSelected -= OnSoulSelected; // Prevent double sub
            SoulRadialMenu.Instance.OnSoulSelected += OnSoulSelected;
        }
    }

    void OnDestroy()
    {
        if (SoulRadialMenu.Instance != null)
        {
            SoulRadialMenu.Instance.OnSoulSelected -= OnSoulSelected;
        }
    }

    void OnSoulSelected(SoulData soul)
    {
        if (soul == null || soul.characterPrefab == null)
        {
            Debug.LogWarning($"[CharacterSwapper] Soul '{soul?.soulName}' has no character prefab assigned!");
            return;
        }

        SwapToCharacter(soul);
    }

    public void SwapToCharacter(SoulData soul)
    {
        if (_isSwapping) return;
        if (soul.characterPrefab == null) return;

        StartCoroutine(DoSwap(soul));
    }

    System.Collections.IEnumerator DoSwap(SoulData soul)
    {
        _isSwapping = true;

        // Make sure we have current character reference - try multiple methods
        if (_currentCharacter == null)
        {
            // Direct search for ThirdPersonController
            var tpc = FindFirstObjectByType<ThirdPersonController>();
            if (tpc != null)
            {
                _currentCharacter = tpc.gameObject;
            }
            else
            {
                // Try by tag
                _currentCharacter = GameObject.FindGameObjectWithTag("Player");
            }
        }

        // Get current position/rotation
        Vector3 position;
        Quaternion rotation;

        if (_currentCharacter != null)
        {
            position = _currentCharacter.transform.position;
            rotation = _currentCharacter.transform.rotation;

            // Sanity check - if position seems invalid (fell through ground), use camera
            if (position.y < -5f)
            {
                Debug.LogWarning($"[CharacterSwapper] Character at invalid Y position ({position.y}), using camera position");
                var cam = Camera.main;
                if (cam != null)
                {
                    position = cam.transform.position + cam.transform.forward * 3f;
                    // Raycast to find ground
                    if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                    {
                        position.y = hit.point.y;
                    }
                }
            }
        }
        else
        {
            // Last resort: use camera position
            var cam = Camera.main;
            if (cam != null)
            {
                position = cam.transform.position + cam.transform.forward * 3f;
                // Raycast to find ground
                if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                {
                    position.y = hit.point.y;
                }
            }
            else
            {
                position = Vector3.zero;
            }
            rotation = Quaternion.identity;
            Debug.LogWarning("[CharacterSwapper] Could not find current character! Using fallback position.");
        }

        Debug.Log($"[CharacterSwapper] Swapping from pos: {position}, current char: {(_currentCharacter != null ? _currentCharacter.name : "NULL")}");

        // Get current health/stamina percentage to transfer
        float healthPercent = 1f;
        float staminaPercent = 1f;
        if (_currentCharacter != null)
        {
            var health = _currentCharacter.GetComponent<PlayerHealth>();
            if (health != null)
            {
                healthPercent = health.HealthPercent;
                staminaPercent = health.StaminaPercent;
            }
        }

        // Spawn swap effect
        if (_swapEffectPrefab != null)
        {
            GameObject effect = Instantiate(_swapEffectPrefab, position + Vector3.up, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Brief pause for effect
        yield return new WaitForSecondsRealtime(_swapEffectDuration * 0.5f);

        // Disable old character
        if (_currentCharacter != null)
        {
            _currentCharacter.SetActive(false);
        }

        // Spawn new character - disable CharacterController first to allow positioning
        GameObject newChar = Instantiate(soul.characterPrefab);
        newChar.name = $"Player_{soul.soulName}";

        // Disable CharacterController before moving (it can interfere with position)
        var cc = newChar.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        newChar.transform.position = position;
        newChar.transform.rotation = rotation;

        // Re-enable CharacterController
        if (cc != null) cc.enabled = true;

        Debug.Log($"[CharacterSwapper] Spawned {newChar.name} at pos: {newChar.transform.position}");

        // Transfer health/stamina percentage
        var newHealth = newChar.GetComponent<PlayerHealth>();
        if (newHealth != null)
        {
            newHealth.SetHealthPercent(healthPercent);
            newHealth.SetStaminaPercent(staminaPercent);
        }

        // Make sure it has player tag
        newChar.tag = "Player";

        // Setup camera to follow new character
        SetupCameraFollow(newChar);

        // Destroy old character
        if (_currentCharacter != null)
        {
            Destroy(_currentCharacter);
        }

        _currentCharacter = newChar;

        Debug.Log($"[CharacterSwapper] Swapped to: {soul.soulName}");

        yield return new WaitForSecondsRealtime(_swapEffectDuration * 0.5f);

        _isSwapping = false;
    }

    public void SetCurrentCharacter(GameObject character)
    {
        _currentCharacter = character;
    }

    void SetupCameraFollow(GameObject newChar)
    {
        // Get the camera follow target - look for a child named "CameraTarget" or use transform
        Transform followTarget = newChar.transform.Find("CameraTarget");
        if (followTarget == null)
        {
            followTarget = newChar.transform;
        }

        // Find all Cinemachine virtual cameras using reflection (avoids hard dependency)
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var component in allComponents)
        {
            var type = component.GetType();
            if (type.Name.Contains("CinemachineVirtualCamera") ||
                type.Name.Contains("CinemachineCamera"))
            {
                // Try to set Follow property
                var followProp = type.GetProperty("Follow");
                if (followProp != null)
                {
                    followProp.SetValue(component, followTarget);
                    Debug.Log($"[CharacterSwapper] Updated camera follow target");
                }
            }
        }

        // Also update ThirdPersonCamera if present
        var tpCam = FindFirstObjectByType<ThirdPersonCamera>();
        if (tpCam != null)
        {
            tpCam.SetTarget(newChar.transform);
        }
    }
}
