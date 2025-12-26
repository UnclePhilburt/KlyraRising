using UnityEngine;
using System.Collections.Generic;

public class SwordHitbox : MonoBehaviour
{
    [SerializeField] private float _damage = 25f;
    [SerializeField] private float _hitRadius = 2.5f;

    [Header("Slash Effects")]
    [SerializeField] private GameObject _lightSlashPrefab;      // FX_SwordSlash_01
    [SerializeField] private GameObject _heavySlashPrefab;      // FX_Slash_Large_01
    [SerializeField] private GameObject _stabPrefab;            // FX_SwordStab_01
    [SerializeField] private GameObject _comboFinisherPrefab;   // FX_Slash_01

    [Header("Hit Effects")]
    [SerializeField] private GameObject _hitEffectPrefab;       // FX_BloodSplat_01
    [SerializeField] private GameObject _hitSparksPrefab;       // FX_Sparks_01

    [Header("Effect Settings")]
    [SerializeField] private Vector3 _slashOffset = new Vector3(0f, 1.2f, 1f);
    [SerializeField] private float _slashScale = 1f;

    private WeaponController _weaponController;
    private HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();
    private HashSet<TrainingDummy> _hitDummies = new HashSet<TrainingDummy>();
    private bool _isActive = false;
    private Collider[] _hitBuffer = new Collider[20];
    private bool _slashSpawned = false;

    // Cache collider -> component lookups to avoid GetComponentInParent every frame
    private static Dictionary<int, Enemy> _colliderToEnemy = new Dictionary<int, Enemy>();
    private static Dictionary<int, TrainingDummy> _colliderToDummy = new Dictionary<int, TrainingDummy>();

    void Start()
    {
        FindWeaponController();
    }

    void FindWeaponController()
    {
        Transform current = transform;
        while (current != null)
        {
            _weaponController = current.GetComponent<WeaponController>();
            if (_weaponController != null) return;
            current = current.parent;
        }
        _weaponController = FindFirstObjectByType<WeaponController>();
    }

    void Update()
    {
        if (_weaponController == null)
        {
            FindWeaponController();
            return;
        }

        bool shouldBeActive = _weaponController.IsAttacking;

        if (shouldBeActive && !_isActive)
        {
            _isActive = true;
            _hitEnemies.Clear();
            _hitDummies.Clear();
            _slashSpawned = false;
        }
        else if (!shouldBeActive && _isActive)
        {
            _isActive = false;
            _slashSpawned = false;
        }

        // Spawn slash effect once per attack
        if (_isActive && !_slashSpawned)
        {
            SpawnSlashEffect();
            _slashSpawned = true;
        }

        if (_isActive)
        {
            CheckHits();
        }
    }

    void CheckHits()
    {
        Vector3 playerPos = _weaponController.transform.position;
        Vector3 playerForward = _weaponController.transform.forward;
        Vector3 hitPos = playerPos + Vector3.up * 1f + playerForward * 1.5f;

        int hitCount = Physics.OverlapSphereNonAlloc(hitPos, _hitRadius, _hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = _hitBuffer[i];
            int colId = col.GetInstanceID();

            // Check for enemies (cached lookup)
            Enemy enemy = GetCachedEnemy(col, colId);
            if (enemy != null && !_hitEnemies.Contains(enemy))
            {
                enemy.TakeDamage(_damage);
                _hitEnemies.Add(enemy);
                SpawnHitEffect(enemy.transform.position + Vector3.up * 1f);
            }

            // Check for training dummies (cached lookup)
            TrainingDummy dummy = GetCachedDummy(col, colId);
            if (dummy != null && !_hitDummies.Contains(dummy))
            {
                dummy.TakeDamage(_damage);
                _hitDummies.Add(dummy);
                SpawnHitEffect(dummy.transform.position + Vector3.up * 1f);
            }
        }
    }

    Enemy GetCachedEnemy(Collider col, int colId)
    {
        if (_colliderToEnemy.TryGetValue(colId, out Enemy cached))
        {
            // Validate cached reference is still valid
            if (cached != null) return cached;
            _colliderToEnemy.Remove(colId);
        }

        // First time seeing this collider - do the expensive lookup once
        Enemy enemy = col.GetComponentInParent<Enemy>();
        if (enemy != null)
            _colliderToEnemy[colId] = enemy;
        return enemy;
    }

    TrainingDummy GetCachedDummy(Collider col, int colId)
    {
        if (_colliderToDummy.TryGetValue(colId, out TrainingDummy cached))
        {
            if (cached != null) return cached;
            _colliderToDummy.Remove(colId);
        }

        TrainingDummy dummy = col.GetComponentInParent<TrainingDummy>();
        if (dummy != null)
            _colliderToDummy[colId] = dummy;
        return dummy;
    }

    void SpawnSlashEffect()
    {
        Transform playerTransform = _weaponController.transform;
        Vector3 spawnPos = playerTransform.position +
                           playerTransform.forward * _slashOffset.z +
                           playerTransform.up * _slashOffset.y +
                           playerTransform.right * _slashOffset.x;

        // Pick effect based on attack type
        GameObject prefab = GetSlashPrefabForAttack();
        if (prefab == null) return;

        GameObject effect = Instantiate(prefab, spawnPos, playerTransform.rotation);
        effect.transform.localScale = Vector3.one * _slashScale;
        Destroy(effect, 2f);
    }

    GameObject GetSlashPrefabForAttack()
    {
        // Check attack type from WeaponController using reflection or public method
        var attackType = GetCurrentAttackType();

        switch (attackType)
        {
            case "HeavyFlourish":
            case "HeavyCombo":
                return _heavySlashPrefab != null ? _heavySlashPrefab : _lightSlashPrefab;

            case "HeavyStab":
            case "Fencing":
                return _stabPrefab != null ? _stabPrefab : _lightSlashPrefab;

            case "Leaping":
                return _comboFinisherPrefab != null ? _comboFinisherPrefab : _heavySlashPrefab;

            case "LightCombo":
            default:
                return _lightSlashPrefab;
        }
    }

    string GetCurrentAttackType()
    {
        // Use reflection to get the current attack type
        var field = typeof(WeaponController).GetField("_currentAttack",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            var value = field.GetValue(_weaponController);
            return value.ToString();
        }
        return "LightCombo";
    }

    void SpawnHitEffect(Vector3 position)
    {
        // Spawn blood effect
        if (_hitEffectPrefab != null)
        {
            GameObject blood = Instantiate(_hitEffectPrefab, position, Quaternion.identity);
            Destroy(blood, 2f);
        }

        // Also spawn sparks
        if (_hitSparksPrefab != null)
        {
            GameObject sparks = Instantiate(_hitSparksPrefab, position, Quaternion.identity);
            Destroy(sparks, 2f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 swordTip = transform.position + transform.up * 0.8f;
        Gizmos.DrawWireSphere(swordTip, _hitRadius);
    }
}
