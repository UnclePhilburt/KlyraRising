using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("Sword")]
    [SerializeField] private GameObject _swordHand;
    [SerializeField] private GameObject _swordSheathed;

    [Header("Timing")]
    [SerializeField] private float _drawDuration = 0.8f;
    [SerializeField] private float _sheatheDuration = 0.8f;
    [SerializeField] private float _swordSwapTime = 0.4f;
    [SerializeField] private float _attackDuration = 0.5f;

    [Header("Attack Settings")]
    [SerializeField] private float _attackDamage = 15f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _telegraphDuration = 0.75f;

    [Header("Telegraph Indicator")]
    [SerializeField] private Color _telegraphLineColor = Color.red;
    [SerializeField] private float _lineLength = 0.5f;
    [SerializeField] private float _lineWidth = 0.15f;
    [SerializeField] private float _headOffset = 2.0f;
    [SerializeField] private int _lineCount = 6;
    [SerializeField] private float _lineSpacing = 0.25f;

    [Header("Stagger")]
    [SerializeField] private float _staggerDuration = 4f;

    private Animator _animator;
    private int _combatLayerIndex = -1;

    // State
    public enum WeaponState { Sheathed, Drawing, Equipped, Sheathing }
    private WeaponState _weaponState = WeaponState.Sheathed;
    private bool _isAttacking = false;
    private bool _isTelegraphing = false;
    private bool _isStaggered = false;
    private float _attackEndTime = 0f;
    private float _drawSheatheStartTime = 0f;
    private bool _swordSwapped = false;
    private float _telegraphStartTime = 0f;
    private float _staggerEndTime = 0f;
    private float _nextStaggerAnimTime = 0f;
    private float _staggerAnimInterval = 0.8f; // How often to replay the flinch

    // Telegraph indicator
    private GameObject _telegraphIndicator;
    private LineRenderer[] _lineRenderers;

    // Animator hashes
    private readonly int _isArmedHash = Animator.StringToHash("IsArmed");
    private readonly int _attackTypeHash = Animator.StringToHash("AttackType");
    private readonly int _comboStepHash = Animator.StringToHash("ComboStep");
    private readonly int _attackTriggerHash = Animator.StringToHash("Attack");
    private readonly int _drawTriggerHash = Animator.StringToHash("Draw");
    private readonly int _sheatheTriggerHash = Animator.StringToHash("Sheathe");

    public bool IsAttacking => _isAttacking;
    public bool IsTelegraphing => _isTelegraphing;
    public bool IsStaggered => _isStaggered;
    public bool IsArmed => _weaponState == WeaponState.Equipped;
    public bool IsDrawing => _weaponState == WeaponState.Drawing;
    public bool CanAct => !_isStaggered && !_isTelegraphing && !_isAttacking;
    public WeaponState CurrentState => _weaponState;
    public float AttackRange => _attackRange;
    public float AttackDamage => _attackDamage;
    public float TelegraphDuration => _telegraphDuration;

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        FindComponents();
        FindSwords();
        CreateTelegraphIndicator();

        // Start sheathed
        if (_swordHand != null) _swordHand.SetActive(false);
        if (_swordSheathed != null) _swordSheathed.SetActive(true);
        _weaponState = WeaponState.Sheathed;
    }

    void CreateTelegraphIndicator()
    {
        // Create parent object for lines
        _telegraphIndicator = new GameObject("TelegraphIndicator");
        _telegraphIndicator.transform.SetParent(transform);
        _telegraphIndicator.transform.localPosition = Vector3.up * _headOffset;

        _lineRenderers = new LineRenderer[_lineCount];

        // Find a working shader
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");

        for (int i = 0; i < _lineCount; i++)
        {
            GameObject lineObj = new GameObject($"AlertLine_{i}");
            lineObj.transform.SetParent(_telegraphIndicator.transform);
            lineObj.transform.localPosition = Vector3.zero;
            lineObj.transform.localRotation = Quaternion.identity;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = _lineWidth;
            lr.endWidth = _lineWidth * 0.5f;
            lr.useWorldSpace = false;

            // Create material
            Material mat = new Material(shader);
            mat.color = _telegraphLineColor;
            lr.material = mat;
            lr.startColor = _telegraphLineColor;
            lr.endColor = _telegraphLineColor;

            // Position lines around head (alternating left/right)
            float side = (i % 2 == 0) ? 1f : -1f;
            float verticalOffset = (i / 2) * _lineSpacing - (_lineCount / 4f) * _lineSpacing;

            float xStart = side * 0.5f;
            float xEnd = side * (0.5f + _lineLength);

            lr.SetPosition(0, new Vector3(xStart, verticalOffset, 0));
            lr.SetPosition(1, new Vector3(xEnd, verticalOffset, 0));

            // Make sure it renders
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.allowOcclusionWhenDynamic = false;

            _lineRenderers[i] = lr;
        }

        _telegraphIndicator.SetActive(false);
    }

    void FindComponents()
    {
        _animator = GetComponentInChildren<Animator>();

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
        }
    }

    void FindSwords()
    {
        if (_swordHand == null)
            _swordHand = FindInChildren(transform, "SwordHand");
        if (_swordSheathed == null)
            _swordSheathed = FindInChildren(transform, "SwordSheathed");
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
        UpdateDrawSheathe();
        UpdateTelegraph();
        UpdateStagger();

        if (_isAttacking && Time.time >= _attackEndTime)
        {
            _isAttacking = false;
        }

        // Update telegraph indicator position and make it face camera
        if (_telegraphIndicator != null && _telegraphIndicator.activeSelf)
        {
            // Keep above head
            _telegraphIndicator.transform.position = transform.position + Vector3.up * _headOffset;

            // Face camera
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = cam.transform.position - _telegraphIndicator.transform.position;
                lookDir.y = 0; // Keep upright
                if (lookDir.magnitude > 0.1f)
                {
                    _telegraphIndicator.transform.rotation = Quaternion.LookRotation(-lookDir);
                }
            }
        }
    }

    void UpdateTelegraph()
    {
        if (!_isTelegraphing) return;

        // Cancel telegraph if staggered
        if (_isStaggered)
        {
            _isTelegraphing = false;
            HideTelegraphIndicator();
            return;
        }

        float elapsed = Time.time - _telegraphStartTime;

        // Animate the lines (pulse effect)
        if (_lineRenderers != null)
        {
            float pulse = Mathf.PingPong(elapsed * 6f, 1f);
            float scale = 1f + pulse * 0.4f;

            for (int i = 0; i < _lineRenderers.Length; i++)
            {
                if (_lineRenderers[i] != null)
                {
                    float side = (i % 2 == 0) ? 1f : -1f;
                    float verticalOffset = (i / 2) * _lineSpacing - (_lineCount / 4f) * _lineSpacing;

                    float xStart = side * 0.4f * scale;
                    float xEnd = side * (0.4f + _lineLength) * scale;

                    _lineRenderers[i].SetPosition(0, new Vector3(xStart, verticalOffset, 0));
                    _lineRenderers[i].SetPosition(1, new Vector3(xEnd, verticalOffset, 0));

                    // Update width for pulse
                    _lineRenderers[i].startWidth = _lineWidth * (0.8f + pulse * 0.4f);
                    _lineRenderers[i].endWidth = _lineWidth * 0.5f * (0.8f + pulse * 0.4f);
                }
            }
        }

        // Telegraph done - execute attack
        if (elapsed >= _telegraphDuration)
        {
            _isTelegraphing = false;
            HideTelegraphIndicator();
            ExecuteAttack();
        }
    }

    void ShowTelegraphIndicator()
    {
        if (_telegraphIndicator != null)
        {
            _telegraphIndicator.transform.position = transform.position + Vector3.up * _headOffset;
            _telegraphIndicator.SetActive(true);
            Debug.Log($"[Telegraph] Showing indicator at {_telegraphIndicator.transform.position}");
        }
    }

    void HideTelegraphIndicator()
    {
        if (_telegraphIndicator != null)
        {
            _telegraphIndicator.SetActive(false);
        }
    }

    void UpdateStagger()
    {
        if (!_isStaggered) return;

        // Keep replaying the flinch animation while staggered
        if (Time.time >= _nextStaggerAnimTime)
        {
            _nextStaggerAnimTime = Time.time + _staggerAnimInterval;
            if (_animator != null)
            {
                // Force replay the animation from the start
                _animator.CrossFadeInFixedTime("Stagger", 0.05f, 0, 0f);
            }
        }

        if (Time.time >= _staggerEndTime)
        {
            _isStaggered = false;
            Debug.Log($"[Stagger] Enemy recovered from stagger at {Time.time}");
        }
    }

    void UpdateDrawSheathe()
    {
        float elapsed = Time.time - _drawSheatheStartTime;

        if (_weaponState == WeaponState.Drawing)
        {
            // Swap sword visibility mid-animation
            if (!_swordSwapped && elapsed >= _swordSwapTime)
            {
                _swordSwapped = true;
                if (_swordSheathed != null) _swordSheathed.SetActive(false);
                if (_swordHand != null) _swordHand.SetActive(true);
            }

            // Finish drawing
            if (elapsed >= _drawDuration)
            {
                _weaponState = WeaponState.Equipped;
                if (_animator != null)
                    _animator.SetBool(_isArmedHash, true);
            }
        }
        else if (_weaponState == WeaponState.Sheathing)
        {
            // Swap sword visibility mid-animation
            if (!_swordSwapped && elapsed >= _swordSwapTime)
            {
                _swordSwapped = true;
                if (_swordHand != null) _swordHand.SetActive(false);
                if (_swordSheathed != null) _swordSheathed.SetActive(true);
            }

            // Finish sheathing
            if (elapsed >= _sheatheDuration)
            {
                _weaponState = WeaponState.Sheathed;
                if (_animator != null)
                {
                    _animator.SetBool(_isArmedHash, false);
                    if (_combatLayerIndex >= 0)
                        _animator.SetLayerWeight(_combatLayerIndex, 0f);
                }
            }
        }
    }

    public void DrawSword()
    {
        if (_weaponState != WeaponState.Sheathed) return;

        _weaponState = WeaponState.Drawing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;

        if (_animator != null)
        {
            if (_combatLayerIndex >= 0)
                _animator.SetLayerWeight(_combatLayerIndex, 1f);
            _animator.SetTrigger(_drawTriggerHash);
        }
    }

    public void SheatheSword()
    {
        if (_weaponState != WeaponState.Equipped) return;

        _weaponState = WeaponState.Sheathing;
        _drawSheatheStartTime = Time.time;
        _swordSwapped = false;

        if (_animator != null)
        {
            _animator.SetTrigger(_sheatheTriggerHash);
        }
    }

    public void Attack()
    {
        if (_weaponState != WeaponState.Equipped) return;
        if (_isAttacking || _isTelegraphing || _isStaggered) return;

        // Start telegraph (show indicator before attack)
        _isTelegraphing = true;
        _telegraphStartTime = Time.time;
        ShowTelegraphIndicator();
    }

    void ExecuteAttack()
    {
        if (_isStaggered) return;

        _isAttacking = true;
        _attackEndTime = Time.time + _attackDuration;

        if (_animator != null)
        {
            int attackStep = Random.Range(1, 4);
            _animator.SetInteger(_attackTypeHash, 1);
            _animator.SetInteger(_comboStepHash, attackStep);
            _animator.SetTrigger(_attackTriggerHash);
        }
    }

    public void HeavyAttack()
    {
        if (_weaponState != WeaponState.Equipped || _isAttacking || _isTelegraphing || _isStaggered) return;

        _isAttacking = true;
        _attackEndTime = Time.time + _attackDuration * 1.5f;

        if (_animator != null)
        {
            _animator.SetInteger(_attackTypeHash, 2);
            _animator.SetInteger(_comboStepHash, 1);
            _animator.SetTrigger(_attackTriggerHash);
        }
    }

    public void Stagger()
    {
        _isTelegraphing = false;
        _isAttacking = false;
        _isStaggered = true;
        _staggerEndTime = Time.time + _staggerDuration;
        _nextStaggerAnimTime = Time.time + _staggerAnimInterval; // First replay after interval

        Debug.Log($"[Stagger] Enemy staggered for {_staggerDuration}s until {_staggerEndTime}");

        HideTelegraphIndicator();

        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>();
        }
        if (_animator != null)
        {
            // Force play the stagger animation from the start
            _animator.CrossFadeInFixedTime("Stagger", 0.05f, 0, 0f);
        }
    }

    public void CancelTelegraph()
    {
        if (_isTelegraphing)
        {
            _isTelegraphing = false;
            HideTelegraphIndicator();
        }
    }

    void OnDestroy()
    {
        // Clean up dynamically created materials
        if (_lineRenderers != null)
        {
            for (int i = 0; i < _lineRenderers.Length; i++)
            {
                if (_lineRenderers[i] != null && _lineRenderers[i].material != null)
                {
                    Destroy(_lineRenderers[i].material);
                }
            }
        }
    }
}
