using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth;

    [Header("Block")]
    [SerializeField] private float _blockDamageReduction = 0.9f; // 90% damage reduction when blocking
    [SerializeField] private float _blockStaminaCost = 10f;

    [Header("Parry")]
    [SerializeField] private float _parryWindow = 0.4f; // Time window after starting block to parry
    [SerializeField] private float _parryStaminaReward = 20f;

    [Header("Stamina")]
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _currentStamina;
    [SerializeField] private float _staminaRegen = 20f;
    [SerializeField] private float _staminaRegenDelay = 1f;

    private WeaponController _weaponController;
    private float _lastDamageTime;
    private float _blockStartTime;
    private bool _wasBlocking = false;
    private bool _isDead = false;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _maxStamina;
    public float HealthPercent => _currentHealth / _maxHealth;
    public float StaminaPercent => _currentStamina / _maxStamina;
    public bool IsDead => _isDead;
    public bool IsBlocking => _weaponController != null && _weaponController.IsBlocking;

    void Start()
    {
        _currentHealth = _maxHealth;
        _currentStamina = _maxStamina;
        _weaponController = GetComponent<WeaponController>();
    }

    void Update()
    {
        // Track block start time for parry window
        bool isBlocking = IsBlocking;
        if (isBlocking && !_wasBlocking)
        {
            _blockStartTime = Time.time;
        }
        _wasBlocking = isBlocking;

        // Regenerate stamina after delay
        if (Time.time - _lastDamageTime > _staminaRegenDelay)
        {
            _currentStamina = Mathf.Min(_currentStamina + _staminaRegen * Time.deltaTime, _maxStamina);
        }
    }

    bool IsInParryWindow()
    {
        return IsBlocking && (Time.time - _blockStartTime) <= _parryWindow;
    }

    public enum DamageResult { Hit, Blocked, Parried }

    public DamageResult TakeDamage(float damage, Vector3 attackDirection = default, EnemyCombat attacker = null)
    {
        if (_isDead) return DamageResult.Hit;

        float actualDamage = damage;
        DamageResult result = DamageResult.Hit;

        if (IsInParryWindow())
        {
            float angle = attackDirection != default ? Vector3.Angle(transform.forward, -attackDirection) : 0f;
            if (angle < 90f)
            {
                result = DamageResult.Parried;
                actualDamage = 0f;
                _currentStamina = Mathf.Min(_currentStamina + _parryStaminaReward, _maxStamina);
                StartCoroutine(ParryFlash());
                return result;
            }
        }

        // Check if blocking (but not parry)
        if (IsBlocking && _currentStamina > 0)
        {
            // Check if facing the attack (within 90 degrees)
            float angle = attackDirection != default ? Vector3.Angle(transform.forward, -attackDirection) : 0f;
            if (angle < 90f || attackDirection == default)
            {
                // Successful block
                result = DamageResult.Blocked;
                actualDamage = damage * (1f - _blockDamageReduction);
                _currentStamina -= _blockStaminaCost;

                if (_currentStamina < 0)
                {
                    _currentStamina = 0;
                    actualDamage = damage * 0.5f;
                    result = DamageResult.Hit;
                }
            }
        }

        _currentHealth -= actualDamage;
        _lastDamageTime = Time.time;

        if (result == DamageResult.Hit && actualDamage > 0)
        {
            StartCoroutine(FlashRed());
        }

        // Show damage popup (only if damage was dealt)
        if (actualDamage > 0)
        {
            Vector3 popupPos = transform.position + Vector3.up * 2f + Random.insideUnitSphere * 0.3f;
            DamagePopup.Create(popupPos, actualDamage);
        }

        if (_currentHealth <= 0)
        {
            Die();
        }

        return result;
    }

    System.Collections.IEnumerator ParryFlash()
    {
        // Flash white/gold for successful parry
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color parryColor = new Color(1f, 0.9f, 0.3f); // Gold

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = parryColor;
                }
            }
        }

        yield return new WaitForSeconds(0.15f);

        // Restore (let the material system handle it or flash again)
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = Color.white;
                }
            }
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[][] originalColors = new Color[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    originalColors[i][j] = mats[j].color;
                    mats[j].color = Color.red;
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (mats[j].HasProperty("_Color"))
                {
                    mats[j].color = originalColors[i][j];
                }
            }
        }
    }

    void Die()
    {
        _isDead = true;
        // TODO: Handle death (respawn, game over, etc.)
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
    }

    public void RestoreStamina(float amount)
    {
        _currentStamina = Mathf.Min(_currentStamina + amount, _maxStamina);
    }

    public void SetHealthPercent(float percent)
    {
        _currentHealth = _maxHealth * Mathf.Clamp01(percent);
    }

    public void SetStaminaPercent(float percent)
    {
        _currentStamina = _maxStamina * Mathf.Clamp01(percent);
    }
}
