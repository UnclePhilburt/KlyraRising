using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviourPun, IPunObservable
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _speedChangeRate = 10f;

    [Header("Jump & Gravity")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _gravity = -40f;
    [SerializeField] private float _groundedGravity = -5f;

    [Header("Ground Check")]
    [SerializeField] private float _groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask _groundLayers = -1;

    [Header("Network")]
    [SerializeField] private float _networkSmoothTime = 0.1f;

    [Header("Head Look")]
    [SerializeField] private float _headLookSpeed = 5f;
    [SerializeField] private float _maxHeadAngle = 60f;

    // Components
    private CharacterController _controller;
    private Animator _animator;
    private ThirdPersonCamera _cameraController;
    private Transform _cameraTransform;
    private Transform _headBone;
    private LockOnSystem _lockOnSystem;
    private CharacterSwitcher _characterSwitcher;

    // State
    private Vector3 _velocity;
    private Vector2 _inputMove;
    private float _currentSpeed;
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _jumpRequested;

    // Network interpolation (for remote players)
    private Vector3 _networkPosition;
    private Quaternion _networkRotation;

    // Cached at startup - true if local player or single-player mode
    private bool _isLocalPlayer = true;
    private float _networkSpeed;
    private int _networkGait;
    private bool _networkGrounded;
    private bool _networkJumping;
    private Vector3 _smoothVelocity;

    // Animator hashes (matching Synty's AC_Polygon_Masculine)
    private readonly int _speedHash = Animator.StringToHash("MoveSpeed");
    private readonly int _gaitHash = Animator.StringToHash("CurrentGait");
    private readonly int _groundedHash = Animator.StringToHash("IsGrounded");
    private readonly int _jumpHash = Animator.StringToHash("IsJumping");
    private readonly int _strafeXHash = Animator.StringToHash("StrafeDirectionX");
    private readonly int _strafeZHash = Animator.StringToHash("StrafeDirectionZ");
    private readonly int _isStrafingHash = Animator.StringToHash("IsStrafing");
    private readonly int _inputHeldHash = Animator.StringToHash("MovementInputHeld");
    private readonly int _isStoppedHash = Animator.StringToHash("IsStopped");

    public bool IsGrounded => _isGrounded;
    public bool IsSprinting => _isSprinting;
    public float CurrentSpeed => _currentSpeed;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();

        // Check if we're local player (or in single-player mode without PhotonView)
        var pv = GetComponent<Photon.Pun.PhotonView>();
        _isLocalPlayer = (pv == null || pv.IsMine);

        // Subscribe to character switching early (before any Start methods)
        _characterSwitcher = GetComponent<CharacterSwitcher>();
        if (_characterSwitcher != null)
        {
            _characterSwitcher.OnCharacterSwitched += OnCharacterSwitched;
        }
    }

    void Start()
    {
        FindAnimator();

        if (_isLocalPlayer)
        {
            SetupCamera();
            SetupLockOn();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Disable CharacterController for remote players (we'll move them via interpolation)
            _controller.enabled = false;
        }

        // Initialize network position
        _networkPosition = transform.position;
        _networkRotation = transform.rotation;
    }

    void OnDestroy()
    {
        if (_characterSwitcher != null)
        {
            _characterSwitcher.OnCharacterSwitched -= OnCharacterSwitched;
        }
    }

    void OnCharacterSwitched(Animator newAnimator)
    {
        _animator = newAnimator;

        // Re-find head bone on new model
        if (_animator != null)
        {
            _headBone = FindBoneRecursive(_animator.transform, "Head");
        }

        Debug.Log($"[Controller] Character switched, new animator: {(_animator != null ? _animator.gameObject.name : "null")}");
    }

    void FindAnimator()
    {
        // Find animator on child character model
        foreach (Transform child in transform)
        {
            if (child.gameObject.activeInHierarchy && child.name.StartsWith("SM_Chr_"))
            {
                _animator = child.GetComponent<Animator>();
                if (_animator != null)
                {
                    Debug.Log($"[Controller] Found animator on {child.name}");
                    return;
                }
            }
        }

        // Fallback
        _animator = GetComponentInChildren<Animator>();
        if (_animator != null)
            Debug.Log($"[Controller] Using animator: {_animator.gameObject.name}");
        else
            Debug.LogWarning("[Controller] No animator found!");

        // Find head bone
        if (_animator != null)
        {
            _headBone = FindBoneRecursive(_animator.transform, "Head");
            if (_headBone != null)
                Debug.Log($"[Controller] Found head bone: {_headBone.name}");
        }
    }

    Transform FindBoneRecursive(Transform parent, string boneName)
    {
        if (parent.name.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return parent;

        foreach (Transform child in parent)
        {
            Transform found = FindBoneRecursive(child, boneName);
            if (found != null) return found;
        }
        return null;
    }

    void SetupCamera()
    {
        // Check for existing camera controller
        _cameraController = FindFirstObjectByType<ThirdPersonCamera>();

        if (_cameraController == null)
        {
            // Create camera rig
            GameObject cameraRig = new GameObject("CameraRig");
            _cameraController = cameraRig.AddComponent<ThirdPersonCamera>();

            // Create camera
            GameObject camObj = new GameObject("Main Camera");
            camObj.transform.SetParent(cameraRig.transform);
            camObj.tag = "MainCamera";

            Camera cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            camObj.AddComponent<AudioListener>();

            _cameraController.Initialize(transform, cam);
        }
        else
        {
            _cameraController.Initialize(transform, Camera.main);
        }

        _cameraTransform = _cameraController.transform;
    }

    void SetupLockOn()
    {
        _lockOnSystem = GetComponent<LockOnSystem>();
        if (_lockOnSystem == null)
        {
            _lockOnSystem = gameObject.AddComponent<LockOnSystem>();
        }
    }

    void Update()
    {
        if (_isLocalPlayer)
        {
            // Local player - full control
            HandleInput();
            GroundCheck();
            ApplyGravity();
            Move();
            UpdateAnimator();
        }
        else
        {
            // Remote player - interpolate to network position
            UpdateRemotePlayer();
        }
    }

    void LateUpdate()
    {
        // Rotate head to look toward camera direction (runs after animations)
        if (_isLocalPlayer && _headBone != null && _cameraTransform != null)
        {
            // Get camera forward direction
            Vector3 lookDir = _cameraTransform.forward;

            // Calculate angle between body forward and look direction
            Vector3 bodyForward = transform.forward;
            float angleY = Vector3.SignedAngle(bodyForward, lookDir, Vector3.up);

            // Clamp the angle
            angleY = Mathf.Clamp(angleY, -_maxHeadAngle, _maxHeadAngle);

            // Calculate pitch from camera
            float angleX = -_cameraTransform.eulerAngles.x;
            if (angleX < -180f) angleX += 360f;
            if (angleX > 180f) angleX -= 360f;
            angleX = Mathf.Clamp(angleX, -_maxHeadAngle * 0.5f, _maxHeadAngle * 0.5f);

            // Apply rotation to head bone (relative to its current animation rotation)
            Quaternion targetRot = _headBone.rotation * Quaternion.Euler(angleX, angleY, 0);
            _headBone.rotation = Quaternion.Slerp(_headBone.rotation, targetRot, _headLookSpeed * Time.deltaTime);
        }
    }

    void HandleInput()
    {
        // Movement input
        if (Keyboard.current != null)
        {
            Vector2 input = Vector2.zero;
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
            _inputMove = input.normalized;

            // Sprint
            _isSprinting = Keyboard.current.leftShiftKey.isPressed && _inputMove.y > 0;

            // Jump
            if (Keyboard.current.spaceKey.wasPressedThisFrame && _isGrounded)
                _jumpRequested = true;
        }
    }

    void GroundCheck()
    {
        // Raycast is more reliable than CharacterController.isGrounded
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        _isGrounded = Physics.Raycast(origin, Vector3.down, _groundCheckRadius + 0.1f, _groundLayers);
    }

    void ApplyGravity()
    {
        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = _groundedGravity;
        }
        else
        {
            // Always apply gravity when in air, accelerating downward
            _velocity.y += _gravity * Time.deltaTime;
            // Clamp to terminal velocity
            _velocity.y = Mathf.Max(_velocity.y, -50f);
        }

        // Jump
        if (_jumpRequested && _isGrounded)
        {
            _velocity.y = _jumpForce;
            _jumpRequested = false;
        }
    }

    void Move()
    {
        if (_cameraTransform == null) return;

        // Calculate movement direction relative to camera
        Vector3 camForward = _cameraTransform.forward;
        Vector3 camRight = _cameraTransform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * _inputMove.y + camRight * _inputMove.x);

        // Calculate target speed
        float targetSpeed = 0f;
        if (moveDir.magnitude > 0.1f)
        {
            if (_isSprinting)
                targetSpeed = _sprintSpeed;
            else
                targetSpeed = _runSpeed;
        }

        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, _speedChangeRate * Time.deltaTime);

        // Apply horizontal movement
        Vector3 horizontalVelocity = moveDir.normalized * _currentSpeed;
        _velocity.x = horizontalVelocity.x;
        _velocity.z = horizontalVelocity.z;

        // Rotate to face lock-on target or movement direction
        if (_lockOnSystem != null && _lockOnSystem.IsLockedOn)
        {
            Vector3 dirToTarget = _lockOnSystem.CurrentTarget.position - transform.position;
            dirToTarget.y = 0;
            if (dirToTarget.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
            }
        }
        else if (moveDir.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _rotationSpeed * Time.deltaTime);
        }

        // Apply movement
        _controller.Move(_velocity * Time.deltaTime);
    }

    void UpdateAnimator()
    {
        // Re-find animator if it's missing or destroyed
        if (_animator == null || _animator.gameObject == null)
        {
            FindAnimator();
            if (_animator == null) return;
        }

        bool hasInput = _inputMove.magnitude > 0.1f;
        bool isStopped = !hasInput && _currentSpeed < 0.5f;

        _animator.SetFloat(_speedHash, _currentSpeed);
        _animator.SetBool(_groundedHash, _isGrounded);
        _animator.SetBool(_jumpHash, !_isGrounded && _velocity.y > 0);
        _animator.SetFloat(_isStrafingHash, 0f); // Not strafing for now
        _animator.SetBool(_inputHeldHash, hasInput);
        _animator.SetBool(_isStoppedHash, isStopped);

        // CurrentGait: 0=Idle, 1=Walk, 2=Run, 3=Sprint
        int gait = 0;
        if (_currentSpeed > 0.1f)
        {
            float walkThreshold = (_walkSpeed + _runSpeed) / 2f;
            float sprintThreshold = (_runSpeed + _sprintSpeed) / 2f;

            if (_currentSpeed < walkThreshold)
                gait = 1; // Walk
            else if (_currentSpeed < sprintThreshold)
                gait = 2; // Run
            else
                gait = 3; // Sprint
        }
        _animator.SetInteger(_gaitHash, gait);

        // Strafe direction (relative to character facing)
        Vector3 localVel = transform.InverseTransformDirection(new Vector3(_velocity.x, 0, _velocity.z));
        _animator.SetFloat(_strafeXHash, localVel.x / Mathf.Max(_currentSpeed, 0.1f));
        _animator.SetFloat(_strafeZHash, localVel.z / Mathf.Max(_currentSpeed, 0.1f));
    }

    void UpdateRemotePlayer()
    {
        // Smoothly interpolate position and rotation
        transform.position = Vector3.SmoothDamp(transform.position, _networkPosition, ref _smoothVelocity, _networkSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, _networkRotation, Time.deltaTime / _networkSmoothTime);

        // Update animator with network values
        if (_animator != null)
        {
            _animator.SetFloat(_speedHash, _networkSpeed);
            _animator.SetInteger(_gaitHash, _networkGait);
            _animator.SetBool(_groundedHash, _networkGrounded);
            _animator.SetBool(_jumpHash, _networkJumping);
            _animator.SetBool(_inputHeldHash, _networkSpeed > 0.1f);
            _animator.SetBool(_isStoppedHash, _networkSpeed < 0.1f);
        }
    }

    // Called by Photon to sync data
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Local player sends data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(_currentSpeed);
            stream.SendNext(GetCurrentGait());
            stream.SendNext(_isGrounded);
            stream.SendNext(!_isGrounded && _velocity.y > 0);
        }
        else
        {
            // Remote player receives data
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();
            _networkSpeed = (float)stream.ReceiveNext();
            _networkGait = (int)stream.ReceiveNext();
            _networkGrounded = (bool)stream.ReceiveNext();
            _networkJumping = (bool)stream.ReceiveNext();

            // Lag compensation - predict position based on time since message sent
            float lag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _networkPosition += _smoothVelocity * lag;
        }
    }

    private int GetCurrentGait()
    {
        if (_currentSpeed < 0.1f) return 0;
        float walkThreshold = (_walkSpeed + _runSpeed) / 2f;
        float sprintThreshold = (_runSpeed + _sprintSpeed) / 2f;
        if (_currentSpeed < walkThreshold) return 1;
        if (_currentSpeed < sprintThreshold) return 2;
        return 3;
    }
}
