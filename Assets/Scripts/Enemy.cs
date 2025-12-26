using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Character Unlock")]
    [SerializeField] private string _characterModelId; // e.g. "SM_Chr_Samurai_Male_01"


    [Header("Animation")]
    [SerializeField] private RuntimeAnimatorController _animatorController;

    [Header("Physics")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask _groundMask = -1;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] _footstepClips;

    [Header("Performance LOD")]
    [SerializeField] private float _nearDistance = 15f;    // Full update rate
    [SerializeField] private float _mediumDistance = 30f;  // Half update rate
    [SerializeField] private float _farDistance = 50f;     // Quarter update rate

    private float _currentHealth;
    private Animator _animator;
    private CharacterController _controller;
    private Vector3 _velocity;
    private bool _isGrounded;
    private EnemyBehavior _behavior;

    // Performance: cached player ref and frame skipping
    private static Transform _cachedPlayer;
    private int _frameCounter;
    private int _updateInterval = 1;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0;

    // Animator hashes
    private readonly int _speedHash = Animator.StringToHash("MoveSpeed");
    private readonly int _gaitHash = Animator.StringToHash("CurrentGait");
    private readonly int _groundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _jumpHash = Animator.StringToHash("IsJumping");
    private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
    private readonly int _inputHeldHash = Animator.StringToHash("MovementInputHeld");
    private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");

    void OnEnable()
    {
        TargetRegistry.Instance.RegisterEnemy(this);
    }

    void OnDisable()
    {
        if (TargetRegistry.Instance != null)
            TargetRegistry.Instance.UnregisterEnemy(this);
    }

    void Start()
    {
        // Check if there's another Enemy component on a parent - if so, we're a duplicate
        Transform parent = transform.parent;
        while (parent != null)
        {
            if (parent.GetComponent<Enemy>() != null)
            {
                Debug.Log($"[Enemy] Destroying duplicate Enemy on {gameObject.name} (parent {parent.name} already has one)");
                Destroy(this);
                return;
            }
            parent = parent.parent;
        }

        // Also remove any Enemy components on children (we're the root)
        Enemy[] childEnemies = GetComponentsInChildren<Enemy>();
        foreach (Enemy e in childEnemies)
        {
            if (e != this)
            {
                Debug.Log($"[Enemy] Removing duplicate Enemy from child {e.gameObject.name}");
                Destroy(e);
            }
        }

        _currentHealth = _maxHealth;
        SetupController();
        SetupAnimator();
        SetupBoneColliders();
        SetupBehavior();
        SetupFootsteps();

        Debug.Log($"[Enemy] Initialized: {gameObject.name}");
    }

    void SetupBehavior()
    {
        _behavior = GetComponent<EnemyBehavior>();
        if (_behavior != null)
        {
            _behavior.Initialize(this);
            Debug.Log($"[Enemy] Using behavior: {_behavior.CurrentBehavior}");
        }
    }

    void SetupFootsteps()
    {
        FootstepPlayer footsteps = GetComponent<FootstepPlayer>();
        if (footsteps == null)
        {
            footsteps = gameObject.AddComponent<FootstepPlayer>();
        }

        if (_footstepClips != null && _footstepClips.Length > 0)
        {
            footsteps.SetFootstepClips(_footstepClips);
        }

        footsteps.Initialize();
    }

    void SetupController()
    {
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
        {
            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = 1.8f;
            _controller.center = new Vector3(0, 0.9f, 0);
            _controller.radius = 0.3f;
        }
    }

    void SetupAnimator()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                _animator = skinnedMesh.gameObject.AddComponent<Animator>();
            }
        }

        if (_animator != null && _animatorController != null)
        {
            _animator.runtimeAnimatorController = _animatorController;
        }

        UpdateAnimator();
    }

    void SetupBoneColliders()
    {
        if (_animator == null)
        {
            Debug.LogWarning("[Enemy] No animator - adding simple collider");
            // Just add a big trigger collider on the enemy itself
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.isTrigger = true;
            col.height = 1.8f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 0.9f, 0);
            return;
        }

        int collidersAdded = 0;

        // Add colliders to key bones
        string[] boneNames = { "Spine", "Spine1", "Spine2", "Head", "LeftArm", "RightArm", "LeftForeArm", "RightForeArm", "LeftUpLeg", "RightUpLeg", "LeftLeg", "RightLeg" };

        foreach (string boneName in boneNames)
        {
            Transform bone = FindBoneRecursive(_animator.transform, boneName);
            if (bone != null)
            {
                CapsuleCollider col = bone.GetComponent<CapsuleCollider>();
                if (col == null)
                {
                    col = bone.gameObject.AddComponent<CapsuleCollider>();
                    col.isTrigger = true;

                    if (boneName.Contains("Spine") || boneName == "Head")
                    {
                        col.radius = 0.15f;
                        col.height = 0.3f;
                    }
                    else
                    {
                        col.radius = 0.08f;
                        col.height = 0.3f;
                        col.direction = 0;
                    }
                    collidersAdded++;
                }
            }
        }

        Debug.Log($"[Enemy] Added {collidersAdded} bone colliders");

        // Also add a main body collider as backup
        if (GetComponent<CapsuleCollider>() == null)
        {
            CapsuleCollider bodyCol = gameObject.AddComponent<CapsuleCollider>();
            bodyCol.isTrigger = true;
            bodyCol.height = 1.8f;
            bodyCol.radius = 0.4f;
            bodyCol.center = new Vector3(0, 0.9f, 0);
            Debug.Log("[Enemy] Added backup body collider");
        }
    }

    Transform FindBoneRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name))
                return child;

            Transform found = FindBoneRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    void Update()
    {
        if (!IsAlive) return;

        // Distance-based LOD: skip frames for far enemies
        _frameCounter++;
        UpdateLOD();

        if (_frameCounter % _updateInterval != 0) return;

        // Run behavior
        if (_behavior != null)
        {
            _behavior.UpdateBehavior();
        }

        ApplyGravity();
        UpdateAnimator();
    }

    void UpdateLOD()
    {
        // Cache player reference
        if (_cachedPlayer == null)
        {
            var player = FindFirstObjectByType<ThirdPersonController>();
            if (player != null) _cachedPlayer = player.transform;
        }

        if (_cachedPlayer == null)
        {
            _updateInterval = 1;
            return;
        }

        float distSqr = (transform.position - _cachedPlayer.position).sqrMagnitude;

        if (distSqr < _nearDistance * _nearDistance)
            _updateInterval = 1;  // Every frame
        else if (distSqr < _mediumDistance * _mediumDistance)
            _updateInterval = 2;  // Every other frame
        else if (distSqr < _farDistance * _farDistance)
            _updateInterval = 4;  // Every 4th frame
        else
            _updateInterval = 8;  // Very far: every 8th frame
    }

    void ApplyGravity()
    {
        // Raycast ground check - more reliable than CharacterController.isGrounded
        float checkDistance = 0.3f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        _isGrounded = Physics.Raycast(origin, Vector3.down, checkDistance + 0.1f, _groundMask);

        // Always apply some downward force to keep grounded
        if (_isGrounded)
        {
            _velocity.y = -2f;
        }
        else
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        // Apply gravity movement
        _controller.Move(new Vector3(0, _velocity.y * Time.deltaTime, 0));
    }

    void UpdateAnimator()
    {
        if (_animator == null) return;

        // Get speed from behavior
        float speed = 0f;
        if (_behavior != null)
        {
            speed = _behavior.CurrentSpeed;
        }

        _animator.SetFloat(_speedHash, speed);
        _animator.SetBool(_groundedHash, true); // Always grounded - enemies don't jump
        _animator.SetBool(_jumpHash, false);
        _animator.SetFloat(_isStrafingHash, 0f);
        _animator.SetBool(_inputHeldHash, speed > 0.1f);
        _animator.SetBool(_isStoppedHash, speed < 0.1f);
        _animator.SetInteger(_gaitHash, speed > 3f ? 1 : 0); // 0 = walk, 1 = run
    }

    public void TakeDamage(float damage)
    {
        _currentHealth -= damage;

        // Flash red
        StartCoroutine(FlashRed());

        // Show damage popup
        Vector3 popupPos = transform.position + Vector3.up * 1.5f + Random.insideUnitSphere * 0.3f;
        DamagePopup.Create(popupPos, damage);

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashRed()
    {
        // Get all renderers
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        // Store original colors and set to red
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

        // Restore original colors
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
        // Unlock this enemy's character model for the player
        if (CharacterUnlockManager.Instance != null)
        {
            // Find the character model (SM_Chr_*)
            string characterId = GetCharacterModelName();
            if (!string.IsNullOrEmpty(characterId))
            {
                CharacterUnlockManager.Instance.UnlockCharacter(characterId);
            }
        }

        Destroy(gameObject);
    }

    string GetCharacterModelName()
    {
        // Use the specified character model ID if set
        if (!string.IsNullOrEmpty(_characterModelId))
        {
            return _characterModelId;
        }

        // Fallback: try to find it from animator
        if (_animator != null)
        {
            Transform t = _animator.transform;
            while (t != null && t != transform)
            {
                if (t.name.StartsWith("SM_Chr_") && !t.name.Contains("Attach"))
                {
                    return t.name;
                }
                t = t.parent;
            }
        }

        return null;
    }
}
