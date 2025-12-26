using UnityEngine;

public class FootstepPlayer : MonoBehaviour
{
    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] _footstepClips;

    [Header("Settings")]
    [SerializeField] private float _volume = 0.5f;
    [SerializeField] private float _pitchMin = 0.9f;
    [SerializeField] private float _pitchMax = 1.1f;
    [SerializeField] private float _minSpeedToPlay = 0.5f;

    [Header("Foot Detection")]
    [SerializeField] private float _footHeightThreshold = 0.15f;
    [SerializeField] private float _minTimeBetweenSteps = 0.2f;
    [SerializeField] private bool _useTimerFallback = true;
    [SerializeField] private float _walkStepInterval = 0.5f;
    [SerializeField] private float _runStepInterval = 0.35f;

    [Header("3D Audio")]
    [SerializeField] private bool _use3DAudio = true;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 20f;

    private AudioSource _audioSource;
    private CharacterController _controller;
    private CharacterSwitcher _characterSwitcher;
    private Animator _animator;
    private Transform _leftFoot;
    private Transform _rightFoot;

    private int _lastClipIndex = -1;
    private float _lastStepTime;
    private bool _leftFootWasDown;
    private bool _rightFootWasDown;
    private float _groundY;
    private bool _initialized = false;
    private float _stepTimer = 0f;

    void Awake()
    {
        // Subscribe to character switching early
        _characterSwitcher = GetComponent<CharacterSwitcher>();
        if (_characterSwitcher == null)
            _characterSwitcher = GetComponentInParent<CharacterSwitcher>();

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

    void OnCharacterSwitched(Animator newAnimator)
    {
        _animator = newAnimator;
        _leftFoot = null;
        _rightFoot = null;

        // Re-find foot bones on new model
        if (_animator != null)
        {
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            if (_leftFoot == null || _rightFoot == null)
            {
                FindFootBonesByName(_animator.transform);
            }
        }

        Debug.Log($"[Footsteps] Character switched, leftFoot={(_leftFoot != null)}, rightFoot={(_rightFoot != null)}");
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (_initialized) return;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.volume = _volume;

        if (_use3DAudio)
        {
            _audioSource.spatialBlend = 1f; // 3D spatial sound
            _audioSource.minDistance = _minDistance;
            _audioSource.maxDistance = _maxDistance;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        else
        {
            _audioSource.spatialBlend = 0f; // 2D sound
        }

        // Find CharacterController
        _controller = GetComponent<CharacterController>();
        if (_controller == null)
            _controller = GetComponentInParent<CharacterController>();

        // Find animator and foot bones
        FindFootBones();

        _initialized = true;

        // Debug info
        Debug.Log($"[Footsteps] Initialized on {gameObject.name}: clips={(_footstepClips != null ? _footstepClips.Length : 0)}, leftFoot={(_leftFoot != null)}, rightFoot={(_rightFoot != null)}, 3D={_use3DAudio}");
    }

    void FindFootBones()
    {
        // Search in multiple places for animator
        _animator = GetComponent<Animator>();
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        if (_animator == null)
            _animator = GetComponentInParent<Animator>();

        if (_animator == null)
        {
            // Search parent hierarchy
            Transform parent = transform.parent;
            while (parent != null && _animator == null)
            {
                _animator = parent.GetComponentInChildren<Animator>();
                parent = parent.parent;
            }
        }

        if (_animator != null)
        {
            // Try to get foot bones from animator (humanoid rig)
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);

            // Fallback: search by name
            if (_leftFoot == null || _rightFoot == null)
            {
                FindFootBonesByName(_animator.transform);
            }
        }
    }

    void FindFootBonesByName(Transform root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            string name = child.name.ToLower();
            if (_leftFoot == null && (name.Contains("left") && name.Contains("foot") || name.Contains("l_foot") || name.Contains("foot_l")))
            {
                _leftFoot = child;
            }
            if (_rightFoot == null && (name.Contains("right") && name.Contains("foot") || name.Contains("r_foot") || name.Contains("foot_r")))
            {
                _rightFoot = child;
            }
        }
    }

    void Update()
    {
        if (!_initialized) return;
        if (_footstepClips == null || _footstepClips.Length == 0) return;
        if (_controller == null) return;

        // Check if grounded (use raycast as backup since CharacterController.isGrounded can be unreliable)
        bool isGrounded = _controller.isGrounded || CheckGroundedRaycast();
        if (!isGrounded)
        {
            _stepTimer = 0f;
            return;
        }

        // Get current speed
        Vector3 velocity = _controller.velocity;
        velocity.y = 0;
        float speed = velocity.magnitude;

        if (speed < _minSpeedToPlay)
        {
            _stepTimer = 0f;
            return;
        }

        // Use foot bone detection if available
        if (_leftFoot != null || _rightFoot != null)
        {
            _groundY = transform.position.y;
            CheckFoot(_leftFoot, ref _leftFootWasDown);
            CheckFoot(_rightFoot, ref _rightFootWasDown);
        }
        // Fallback to timer-based footsteps
        else if (_useTimerFallback)
        {
            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                PlayFootstep();
                bool isRunning = speed > 3f;
                _stepTimer = isRunning ? _runStepInterval : _walkStepInterval;
            }
        }
    }

    bool CheckGroundedRaycast()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, 0.3f);
    }

    void CheckFoot(Transform foot, ref bool wasDown)
    {
        if (foot == null) return;

        float footHeight = foot.position.y - _groundY;
        bool isDown = footHeight < _footHeightThreshold;

        // Play sound when foot comes down (was up, now down)
        if (isDown && !wasDown)
        {
            if (Time.time - _lastStepTime >= _minTimeBetweenSteps)
            {
                PlayFootstep();
                _lastStepTime = Time.time;
            }
        }

        wasDown = isDown;
    }

    void PlayFootstep()
    {
        if (_footstepClips == null || _footstepClips.Length == 0) return;

        int clipIndex;
        if (_footstepClips.Length > 1)
        {
            do
            {
                clipIndex = Random.Range(0, _footstepClips.Length);
            } while (clipIndex == _lastClipIndex);
        }
        else
        {
            clipIndex = 0;
        }

        _lastClipIndex = clipIndex;
        AudioClip clip = _footstepClips[clipIndex];

        if (clip != null)
        {
            _audioSource.pitch = Random.Range(_pitchMin, _pitchMax);
            _audioSource.PlayOneShot(clip, _volume);
        }
    }

    // Set clips at runtime (useful for enemies)
    public void SetFootstepClips(AudioClip[] clips)
    {
        _footstepClips = clips;
    }

    // Legacy method - can still be called from animation events if preferred
    public void Footstep()
    {
        if (_controller != null && !_controller.isGrounded) return;
        PlayFootstep();
    }
}
