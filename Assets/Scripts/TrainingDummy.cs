using UnityEngine;

public class TrainingDummy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private bool _regenerateHealth = true;
    [SerializeField] private float _regenDelay = 2f;
    [SerializeField] private float _regenRate = 50f;

    [Header("Visual Feedback")]
    [SerializeField] private bool _showDamageNumbers = true;
    [SerializeField] private bool _flashOnHit = true;
    [SerializeField] private Color _hitColor = Color.red;
    [SerializeField] private bool _shakeOnHit = true;
    [SerializeField] private float _shakeIntensity = 0.1f;
    [SerializeField] private float _shakeDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip _hitSound;

    private float _currentHealth;
    private float _lastHitTime;
    private AudioSource _audioSource;
    private Renderer[] _renderers;
    private Color[][] _originalColors;
    private Vector3 _originalPosition;
    private bool _isShaking = false;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;

    void OnEnable()
    {
        TargetRegistry.Instance.RegisterDummy(this);
    }

    void OnDisable()
    {
        if (TargetRegistry.Instance != null)
            TargetRegistry.Instance.UnregisterDummy(this);
    }

    void Start()
    {
        _currentHealth = _maxHealth;
        _originalPosition = transform.position;
        _renderers = GetComponentsInChildren<Renderer>();
        CacheOriginalColors();
        SetupCollider();
        SetupAudio();

        Debug.Log($"[TrainingDummy] Initialized: {gameObject.name}");
    }

    void SetupCollider()
    {
        // Add a collider if none exists
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1f, 0);
            Debug.Log("[TrainingDummy] Added default collider");
        }
        else if (!col.isTrigger)
        {
            // Make sure it's a trigger so sword can detect it
            col.isTrigger = true;
        }
    }

    void SetupAudio()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && _hitSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }
    }

    void CacheOriginalColors()
    {
        _originalColors = new Color[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material[] mats = _renderers[i].materials;
            _originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    _originalColors[i][j] = mats[j].color;
                }
            }
        }
    }

    void Update()
    {
        // Regenerate health after delay
        if (_regenerateHealth && _currentHealth < _maxHealth)
        {
            if (Time.time - _lastHitTime > _regenDelay)
            {
                _currentHealth += _regenRate * Time.deltaTime;
                _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
            }
        }
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _lastHitTime = Time.time;

        // Clamp to zero (dummy doesn't die)
        _currentHealth = Mathf.Max(_currentHealth, 0f);

        // Visual feedback
        if (_flashOnHit)
        {
            StartCoroutine(FlashColor());
        }

        if (_shakeOnHit && !_isShaking)
        {
            StartCoroutine(Shake());
        }

        // Damage popup
        if (_showDamageNumbers)
        {
            Vector3 popupPos = transform.position + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
            DamagePopup.Create(popupPos, damage);
        }

        // Sound
        if (_audioSource != null && _hitSound != null)
        {
            _audioSource.PlayOneShot(_hitSound);
        }

        Debug.Log($"[TrainingDummy] Hit for {damage} damage! Health: {_currentHealth}/{_maxHealth}");
    }

    System.Collections.IEnumerator FlashColor()
    {
        // Set to hit color
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material[] mats = _renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = _hitColor;
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        // Restore original colors
        for (int i = 0; i < _renderers.Length; i++)
        {
            Material[] mats = _renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = _originalColors[i][j];
                }
            }
        }
    }

    System.Collections.IEnumerator Shake()
    {
        _isShaking = true;
        _originalPosition = transform.position;  // Update in case dummy moved
        float elapsed = 0f;

        while (elapsed < _shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * _shakeIntensity;
            float z = Random.Range(-1f, 1f) * _shakeIntensity;

            transform.position = _originalPosition + new Vector3(x, 0, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Return to original position
        transform.position = _originalPosition;
        _isShaking = false;
    }

    // Reset dummy to full health
    public void ResetDummy()
    {
        _currentHealth = _maxHealth;
    }
}
