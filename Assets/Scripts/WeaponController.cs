using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

public class WeaponController : MonoBehaviourPun, IPunObservable
{
    [Header("State")]
    [SerializeField] private bool _isSwordEquipped = false;

    [Header("Attack Sounds")]
    [SerializeField] private AudioClip[] _lightSwingSounds;
    [SerializeField] private AudioClip[] _heavySwingSounds;
    [SerializeField] private AudioClip[] _drawSounds;
    [SerializeField] private AudioClip[] _sheatheSounds;
    [SerializeField] private AudioSource _audioSource;

    [Header("Combat Timing")]
    [SerializeField] private float _comboWindow = 0.8f;
    [SerializeField] private float _lightAttackDuration = 0.4f;
    [SerializeField] private float _heavyAttackDuration = 0.6f;
    [SerializeField] private float _chargeThreshold = 0.3f;
    [SerializeField] private float _parryWindow = 0.3f;
    [SerializeField] private float _drawDuration = 0.8f;
    [SerializeField] private float _sheatheDuration = 0.8f;
    [SerializeField] private float _swordSwapTime = 0.4f;
    [SerializeField] private float _blockInputBuffer = 0.15f; // Time to wait for second button

    private Animator _animator;
    private ThirdPersonController _controller;
    private CharacterSwitcher _characterSwitcher;
    private int _combatLayerIndex = -1;

    // Sword GameObjects
    private GameObject _swordHand;
    private GameObject _swordSheathed;

    // Animator hashes
    private readonly int _isArmedHash = Animator.StringToHash("IsArmed");
    private readonly int _attackTypeHash = Animator.StringToHash("AttackType");
    private readonly int _comboStepHash = Animator.StringToHash("ComboStep");
    private readonly int _isBlockingHash = Animator.StringToHash("IsBlocking");
    private readonly int _attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int _drawTriggerHash = Animator.StringToHash("Draw");
    private readonly int _sheatheTriggerHash = Animator.StringToHash("Sheathe");

    // Attack types for animator
    public enum AttackType
    {
        None = 0,
        LightCombo = 1,
        HeavyFlourish = 2,
        HeavyStab = 3,
        HeavyCombo = 4,
        Fencing = 5,
        Leaping = 6,
        Parry = 7
    }

    public enum WeaponState
    {
        Sheathed,
        Drawing,
        Equipped,
        Sheathing
    }

    // Combat state
    private WeaponState _weaponState = WeaponState.Sheathed;
    private AttackType _currentAttack = AttackType.None;
    private int _lightComboStep = 0;
    private int _heavyComboStep = 0;
    private float _lastAttackTime = -10f;
    private float _attackEndTime = 0f;
    private bool _isAttacking = false;
    private bool _isBlocking = false;
    private bool _wasBlocking = false;
    private float _blockStartTime = 0f;
    private float _heavyPressTime = 0f;
    private bool _heavyHeld = false;
    private bool _heavyCharged = false;
    private int _lastHeavyType = 0;
    private bool _comboFinisherReady = false;

    // Draw/Sheathe timing
    private float _drawSheatheStartTime = 0f;
    private bool _swordSwapped = false;

    // Block input buffer
    private float _leftPressTime = -10f;
    private float _rightPressTime = -10f;
    private bool _pendingLeftAttack = false;
    private bool _pendingRightAttack = false;

    // Network state
    private int _networkWeaponState = 0;
    private bool _networkIsBlocking = false;
    private bool _networkIsArmed = false;
    private bool _isLocalPlayer = true;

    public bool IsSwordEquipped => _isSwordEquipped;
    public bool IsAttacking => _isAttacking;
    public bool IsBlocking => _isBlocking;
    public WeaponState CurrentWeaponState => _weaponState;

    // Helper to safely call RPCs (no-op in single player)
    private Photon.Pun.PhotonView _cachedPhotonView;
    private bool _hasPhotonView;
    private void SafeRPC(string methodName, RpcTarget target, params object[] parameters)
    {
        if (_hasPhotonView && _cachedPhotonView != null)
        {
            _cachedPhotonView.RPC(methodName, target, parameters);
        }
    }

    void Awake()
    {
        // Check if we're local player (or in single-player mode without PhotonView)
        _cachedPhotonView = GetComponent<Photon.Pun.PhotonView>();
        _hasPhotonView = (_cachedPhotonView != null);
        _isLocalPlayer = !_hasPhotonView || _cachedPhotonView.IsMine;

        // Subscribe to character switching early
        _characterSwitcher = GetComponent<CharacterSwitcher>();
        if (_characterSwitcher != null)
        {
            _characterSwitcher.OnCharacterSwitched += OnCharacterSwitched;
        }
    }

    void OnDestroy()
    {
        if (_characterSwitcher != null)
        {
            _characterSwitcher.OnCharacterSwitched -= OnCharacterSwitched;
        }
    }

    // Called BEFORE character switch to force sheathe
    public void ForceSheatheForSwitch()
    {
        CancelInvoke(nameof(SwapToHandSword));
        CancelInvoke(nameof(SwapToSheathedSword));
        CancelInvoke(nameof(FinishDraw));
        CancelInvoke(nameof(FinishSheathe));

        _weaponState = WeaponState.Sheathed;
        _isSwordEquipped = false;
        _isAttacking = false;
        _isBlocking = false;
        _currentAttack = AttackType.None;

        if (_swordHand != null) _swordHand.SetActive(false);
        if (_swordSheathed != null) _swordSheathed.SetActive(true);

        if (_animator != null)
        {
            _animator.SetBool(_isArmedHash, false);
            _animator.SetBool(_isBlockingHash, false);
            if (_combatLayerIndex >= 0)
            {
                _animator.SetLayerWeight(_combatLayerIndex, 0f);
            }
        }
    }

    void OnCharacterSwitched(Animator newAnimator)
    {
        _animator = newAnimator;
        _combatLayerIndex = -1;

        // Search from the NEW MODEL for swords
        if (_animator != null)
        {
            _swordHand = FindInChildren(_animator.transform, "SwordHand");
            _swordSheathed = FindInChildren(_animator.transform, "SwordSheathed");
        }

        // Force sheathed state on new model
        _weaponState = WeaponState.Sheathed;
        _isSwordEquipped = false;
        if (_swordHand != null) _swordHand.SetActive(false);
        if (_swordSheathed != null) _swordSheathed.SetActive(true);

        // Setup animator after a frame
        StartCoroutine(SetupAnimatorAfterSwitch());
    }

    System.Collections.IEnumerator SetupAnimatorAfterSwitch()
    {
        yield return null;

        if (_animator == null) yield break;

        // Find combat layer
        _combatLayerIndex = -1;
        for (int i = 0; i < _animator.layerCount; i++)
        {
            if (_animator.GetLayerName(i) == "Combat")
            {
                _combatLayerIndex = i;
                break;
            }
        }

        // Set to sheathed state
        _animator.SetBool(_isArmedHash, false);
        _animator.SetBool(_isBlockingHash, false);
        if (_combatLayerIndex >= 0)
        {
            _animator.SetLayerWeight(_combatLayerIndex, 0f);
        }

        Debug.Log($"[Weapon] Animator ready, combatLayer: {_combatLayerIndex}");
    }

    // Called AFTER character switch to re-equip
    public void ForceDrawAfterSwitch()
    {
        if (_animator == null || _swordHand == null) return;

        StartCoroutine(DrawAfterDelay());
    }

    System.Collections.IEnumerator DrawAfterDelay()
    {
        // Wait for animator to be ready
        yield return null;
        yield return null;

        // Now draw the sword properly
        StartDraw();
    }

    void Start()
    {
        FindComponents();
        FindSwords();
        UpdateSwordVisibility();
        SetupAudio();
    }

    void SetupAudio()
    {
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.spatialBlend = 1f;
                _audioSource.playOnAwake = false;
            }
        }
    }

    void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0 || _audioSource == null) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip != null) _audioSource.PlayOneShot(clip);
    }

    void FindComponents()
    {
        _controller = GetComponent<ThirdPersonController>();

        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy && child.name.StartsWith("SM_Chr_"))
            {
                _animator = child.GetComponent<Animator>();
                if (_animator != null)
                {
                    for (int i = 0; i < _animator.layerCount; i++)
                    {
                        if (_animator.GetLayerName(i) == "Combat")
                        {
                            _combatLayerIndex = i;
                            break;
                        }
                    }
                    return;
                }
            }
        }
        _animator = GetComponentInChildren<Animator>();
    }

    void FindSwords()
    {
        _swordHand = FindInChildren(transform, "SwordHand");
        _swordSheathed = FindInChildren(transform, "SwordSheathed");

        if (_swordHand == null)
            Debug.LogWarning("[Weapon] SwordHand not found! Run Klyra > Attach Sword to Character");
        if (_swordSheathed == null)
            Debug.LogWarning("[Weapon] SwordSheathed not found! Run Klyra > Attach Sword to Character");
    }

    GameObject FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;
            GameObject found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    void Update()
    {
        if (_isLocalPlayer)
        {
            // Local player - handle input
            HandleInput();
            UpdateDrawSheathe();
            UpdateCombatState();
            UpdateAnimator();
        }
        else
        {
            // Remote player - apply network state
            UpdateRemoteState();
        }
    }

    void HandleInput()
    {
        if (Keyboard.current == null) return;

        // Toggle sword with 1
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TryToggleSword();
        }

        // Only allow combat when fully equipped
        if (_weaponState != WeaponState.Equipped || Mouse.current == null) return;

        bool leftDown = Mouse.current.leftButton.isPressed;
        bool rightDown = Mouse.current.rightButton.isPressed;
        bool leftPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool rightReleased = Mouse.current.rightButton.wasReleasedThisFrame;

        // === BLOCKING (Right mouse button) ===
        if (rightDown && !_isAttacking)
        {
            if (!_isBlocking)
            {
                StartBlock();
            }
        }
        else if (_isBlocking && !rightDown)
        {
            StopBlock();
        }

        // === PARRY on block release ===
        if (_wasBlocking && !_isBlocking && rightReleased)
        {
            float blockDuration = Time.time - _blockStartTime;
            if (blockDuration < _parryWindow && CanAttack())
            {
                ExecuteParry();
                _wasBlocking = false;
                return;
            }
            _wasBlocking = false;
        }

        if (_isBlocking) return;

        // === LIGHT ATTACK (Left Click) ===
        if (leftPressed && !rightDown)
        {
            if (CanAttack())
            {
                ExecuteLightAttack();
            }
        }
    }

    void TryToggleSword()
    {
        if (_weaponState == WeaponState.Sheathed)
        {
            StartDraw();
            SafeRPC("RPC_DrawSword", RpcTarget.Others);
        }
        else if (_weaponState == WeaponState.Equipped)
        {
            StartSheathe();
            SafeRPC("RPC_SheatheSword", RpcTarget.Others);
        }
    }

    void StartDraw()
    {
        _weaponState = WeaponState.Drawing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;
        _isSwordEquipped = true;

        if (_animator != null)
        {
            if (_combatLayerIndex >= 0)
                _animator.SetLayerWeight(_combatLayerIndex, 1f);
            _animator.SetTrigger(_drawTriggerHash);
        }

        PlayRandomSound(_drawSounds);
    }

    void StartSheathe()
    {
        _weaponState = WeaponState.Sheathing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;
        ResetCombatState();

        if (_animator != null)
        {
            _animator.SetTrigger(_sheatheTriggerHash);
        }

        PlayRandomSound(_sheatheSounds);
    }

    void UpdateDrawSheathe()
    {
        float elapsed = Time.time - _drawSheatheStartTime;

        if (_weaponState == WeaponState.Drawing)
        {
            if (!_swordSwapped && elapsed >= _swordSwapTime)
            {
                _swordSwapped = true;
                if (_swordSheathed != null) _swordSheathed.SetActive(false);
                if (_swordHand != null) _swordHand.SetActive(true);
            }

            if (elapsed >= _drawDuration)
            {
                _weaponState = WeaponState.Equipped;
                if (_animator != null)
                    _animator.SetBool(_isArmedHash, true);
            }
        }
        else if (_weaponState == WeaponState.Sheathing)
        {
            if (!_swordSwapped && elapsed >= _swordSwapTime)
            {
                _swordSwapped = true;
                if (_swordHand != null) _swordHand.SetActive(false);
                if (_swordSheathed != null) _swordSheathed.SetActive(true);
            }

            if (elapsed >= _sheatheDuration)
            {
                _weaponState = WeaponState.Sheathed;
                _isSwordEquipped = false;
                if (_animator != null)
                {
                    _animator.SetBool(_isArmedHash, false);
                    if (_combatLayerIndex >= 0)
                        _animator.SetLayerWeight(_combatLayerIndex, 0f);
                }
            }
        }
    }

    void UpdateSwordVisibility()
    {
        if (_swordHand != null)
            _swordHand.SetActive(_weaponState == WeaponState.Equipped);
        if (_swordSheathed != null)
            _swordSheathed.SetActive(_weaponState == WeaponState.Sheathed);
    }

    bool CanAttack()
    {
        if (_weaponState != WeaponState.Equipped) return false;
        if (_isBlocking) return false;
        if (_isAttacking && Time.time < _attackEndTime - 0.15f) return false;
        return true;
    }

    void StartBlock()
    {
        _isBlocking = true;
        _wasBlocking = true;
        _blockStartTime = Time.time;
        _lightComboStep = 0;
        _heavyComboStep = 0;
        _comboFinisherReady = false;
    }

    void StopBlock()
    {
        _isBlocking = false;
    }

    void ExecuteParry()
    {
        _currentAttack = AttackType.Parry;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _heavyAttackDuration;
        _lightComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)AttackType.Parry);
        _animator.SetInteger(_comboStepHash, 1);
        _animator.SetTrigger(_attackTriggerHash);

        // Notify other players
        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.Parry, 1);
    }

    void ExecuteLightAttack()
    {
        bool isGrounded = _controller != null ? _controller.IsGrounded : true;
        bool isSprinting = _controller != null ? _controller.IsSprinting : false;

        if (!isGrounded)
        {
            ExecuteLeapingAttack();
            return;
        }

        if (isSprinting)
        {
            ExecuteFencingAttack();
            return;
        }

        float timeSince = Time.time - _lastAttackTime;
        if (timeSince < _comboWindow && _currentAttack == AttackType.LightCombo && _lightComboStep < 3)
        {
            _lightComboStep++;
        }
        else
        {
            _lightComboStep = 1;
        }

        _currentAttack = AttackType.LightCombo;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _lightAttackDuration;
        _comboFinisherReady = (_lightComboStep == 3);

        _animator.SetInteger(_attackTypeHash, (int)AttackType.LightCombo);
        _animator.SetInteger(_comboStepHash, _lightComboStep);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_lightSwingSounds);

        // Notify other players
        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.LightCombo, _lightComboStep);
    }

    void ExecuteLeapingAttack()
    {
        _currentAttack = AttackType.Leaping;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _heavyAttackDuration;
        _lightComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)AttackType.Leaping);
        _animator.SetInteger(_comboStepHash, 1);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_heavySwingSounds);

        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.Leaping, 1);
    }

    void ExecuteFencingAttack()
    {
        _currentAttack = AttackType.Fencing;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _lightAttackDuration;
        _lightComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)AttackType.Fencing);
        _animator.SetInteger(_comboStepHash, 1);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_lightSwingSounds);

        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.Fencing, 1);
    }

    void ExecuteHeavyTap()
    {
        _lastHeavyType = 1 - _lastHeavyType;
        _currentAttack = _lastHeavyType == 0 ? AttackType.HeavyFlourish : AttackType.HeavyStab;

        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _heavyAttackDuration;
        _lightComboStep = 0;
        _heavyComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)_currentAttack);
        _animator.SetInteger(_comboStepHash, 1);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_heavySwingSounds);

        SafeRPC("RPC_Attack", RpcTarget.Others, (int)_currentAttack, 1);
    }

    void ExecuteHeavyCombo()
    {
        float timeSince = Time.time - _lastAttackTime;
        if (timeSince < _comboWindow && _currentAttack == AttackType.HeavyCombo && _heavyComboStep < 3)
        {
            _heavyComboStep++;
        }
        else
        {
            _heavyComboStep = 1;
        }

        _currentAttack = AttackType.HeavyCombo;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _heavyAttackDuration;
        _lightComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)AttackType.HeavyCombo);
        _animator.SetInteger(_comboStepHash, _heavyComboStep);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_heavySwingSounds);

        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.HeavyCombo, _heavyComboStep);
    }

    void ExecuteFinisher()
    {
        _currentAttack = AttackType.HeavyFlourish;
        _isAttacking = true;
        _lastAttackTime = Time.time;
        _attackEndTime = Time.time + _heavyAttackDuration + 0.2f;
        _lightComboStep = 0;
        _heavyComboStep = 0;
        _comboFinisherReady = false;

        _animator.SetInteger(_attackTypeHash, (int)AttackType.HeavyFlourish);
        _animator.SetInteger(_comboStepHash, 1);
        _animator.SetTrigger(_attackTriggerHash);

        PlayRandomSound(_heavySwingSounds);

        SafeRPC("RPC_Attack", RpcTarget.Others, (int)AttackType.HeavyFlourish, 1);
    }

    void ResetCombatState()
    {
        _isAttacking = false;
        _isBlocking = false;
        _wasBlocking = false;
        _lightComboStep = 0;
        _heavyComboStep = 0;
        _comboFinisherReady = false;
        _heavyHeld = false;
        _heavyCharged = false;
        _currentAttack = AttackType.None;
    }

    void UpdateCombatState()
    {
        if (_isAttacking && Time.time >= _attackEndTime)
        {
            _isAttacking = false;
            _currentAttack = AttackType.None;
        }

        if (!_isAttacking && Time.time - _lastAttackTime > _comboWindow)
        {
            if (_lightComboStep > 0 || _heavyComboStep > 0)
            {
                _lightComboStep = 0;
                _heavyComboStep = 0;
                _comboFinisherReady = false;
            }
        }
    }

    void UpdateAnimator()
    {
        // Re-find animator if missing or destroyed
        if (_animator == null || _animator.gameObject == null)
        {
            FindComponents();
            if (_animator == null) return;
        }

        _animator.SetBool(_isBlockingHash, _isBlocking);

        if (_combatLayerIndex >= 0 && _weaponState == WeaponState.Equipped)
        {
            float current = _animator.GetLayerWeight(_combatLayerIndex);
            _animator.SetLayerWeight(_combatLayerIndex, Mathf.Lerp(current, 1f, 10f * Time.deltaTime));
        }
    }

    void UpdateRemoteState()
    {
        // Update animator from network state
        if (_animator != null)
        {
            _animator.SetBool(_isBlockingHash, _networkIsBlocking);
            _animator.SetBool(_isArmedHash, _networkIsArmed);

            if (_combatLayerIndex >= 0)
            {
                float targetWeight = _networkIsArmed ? 1f : 0f;
                float current = _animator.GetLayerWeight(_combatLayerIndex);
                _animator.SetLayerWeight(_combatLayerIndex, Mathf.Lerp(current, targetWeight, 10f * Time.deltaTime));
            }
        }

        // Update sword visibility
        WeaponState netState = (WeaponState)_networkWeaponState;
        if (_swordHand != null)
            _swordHand.SetActive(netState == WeaponState.Equipped || netState == WeaponState.Drawing);
        if (_swordSheathed != null)
            _swordSheathed.SetActive(netState == WeaponState.Sheathed || netState == WeaponState.Sheathing);
    }

    // === PHOTON NETWORK SYNC ===

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext((int)_weaponState);
            stream.SendNext(_isBlocking);
            stream.SendNext(_weaponState == WeaponState.Equipped);
        }
        else
        {
            _networkWeaponState = (int)stream.ReceiveNext();
            _networkIsBlocking = (bool)stream.ReceiveNext();
            _networkIsArmed = (bool)stream.ReceiveNext();
        }
    }

    // === PHOTON RPCs ===

    [PunRPC]
    void RPC_Attack(int attackType, int comboStep)
    {
        if (_animator == null) return;

        _animator.SetInteger(_attackTypeHash, attackType);
        _animator.SetInteger(_comboStepHash, comboStep);
        _animator.SetTrigger(_attackTriggerHash);
    }

    [PunRPC]
    void RPC_DrawSword()
    {
        _weaponState = WeaponState.Drawing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;

        if (_animator != null)
        {
            if (_combatLayerIndex >= 0)
                _animator.SetLayerWeight(_combatLayerIndex, 1f);
            _animator.SetTrigger(_drawTriggerHash);
        }

        // Schedule sword visibility swap
        Invoke(nameof(SwapToHandSword), _swordSwapTime);
        Invoke(nameof(FinishDraw), _drawDuration);
    }

    [PunRPC]
    void RPC_SheatheSword()
    {
        _weaponState = WeaponState.Sheathing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;

        if (_animator != null)
        {
            _animator.SetTrigger(_sheatheTriggerHash);
        }

        Invoke(nameof(SwapToSheathedSword), _swordSwapTime);
        Invoke(nameof(FinishSheathe), _sheatheDuration);
    }

    void SwapToHandSword()
    {
        if (_swordSheathed != null) _swordSheathed.SetActive(false);
        if (_swordHand != null) _swordHand.SetActive(true);
    }

    void SwapToSheathedSword()
    {
        if (_swordHand != null) _swordHand.SetActive(false);
        if (_swordSheathed != null) _swordSheathed.SetActive(true);
    }

    void FinishDraw()
    {
        _weaponState = WeaponState.Equipped;
        if (_animator != null)
            _animator.SetBool(_isArmedHash, true);
    }

    void FinishSheathe()
    {
        _weaponState = WeaponState.Sheathed;
        if (_animator != null)
        {
            _animator.SetBool(_isArmedHash, false);
            if (_combatLayerIndex >= 0)
                _animator.SetLayerWeight(_combatLayerIndex, 0f);
        }
    }

    public void EquipSword() { if (_weaponState == WeaponState.Sheathed) TryToggleSword(); }
    public void UnequipSword() { if (_weaponState == WeaponState.Equipped) TryToggleSword(); }
}
