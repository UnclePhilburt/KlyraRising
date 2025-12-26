using UnityEngine;
using UnityEngine.InputSystem;

public class LockOnSystem : MonoBehaviour
{
    [Header("Lock On Settings")]
    [SerializeField] private float _lockOnRange = 20f;
    [SerializeField] private float _lockOnAngle = 60f;
    [SerializeField] private KeyCode _lockOnKey = KeyCode.Tab;

    [Header("Indicator")]
    [SerializeField] private Color _indicatorColor = Color.red;
    [SerializeField] private float _indicatorSize = 0.3f;
    [SerializeField] private float _indicatorHeight = 2.2f;

    private Transform _currentTarget;
    private GameObject _indicator;
    private ThirdPersonCamera _camera;
    private Transform _player;

    public Transform CurrentTarget => _currentTarget;
    public bool IsLockedOn => _currentTarget != null;

    void Start()
    {
        _camera = FindFirstObjectByType<ThirdPersonCamera>();
        _player = transform;
        CreateIndicator();
    }

    void CreateIndicator()
    {
        _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _indicator.name = "LockOnIndicator";
        _indicator.transform.localScale = Vector3.one * _indicatorSize;

        // Remove collider
        Destroy(_indicator.GetComponent<Collider>());

        // Set material
        var renderer = _indicator.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.material.color = _indicatorColor;

        _indicator.SetActive(false);
    }

    void Update()
    {
        // Toggle lock-on with middle mouse button
        if (Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (_currentTarget != null)
            {
                Unlock();
            }
            else
            {
                TryLockOn();
            }
        }

        // Update indicator position
        if (_currentTarget != null)
        {
            // Check if target still exists and in range
            if (_currentTarget == null || Vector3.Distance(_player.position, _currentTarget.position) > _lockOnRange * 1.5f)
            {
                Unlock();
                return;
            }

            _indicator.transform.position = _currentTarget.position + Vector3.up * _indicatorHeight;

            // Make indicator face camera
            if (Camera.main != null)
            {
                _indicator.transform.LookAt(Camera.main.transform);
            }
        }
    }

    void TryLockOn()
    {
        Transform bestTarget = null;
        float bestScore = float.MaxValue;

        // Check enemies from registry (no FindObjectsByType!)
        var enemies = TargetRegistry.Instance.Enemies;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null) continue;
            float score = GetTargetScore(enemies[i].transform);
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = enemies[i].transform;
            }
        }

        // Check training dummies from registry
        var dummies = TargetRegistry.Instance.Dummies;
        for (int i = 0; i < dummies.Count; i++)
        {
            if (dummies[i] == null) continue;
            float score = GetTargetScore(dummies[i].transform);
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = dummies[i].transform;
            }
        }

        if (bestTarget != null)
        {
            LockOn(bestTarget);
        }
    }

    float GetTargetScore(Transform target)
    {
        Vector3 dirToTarget = target.position - _player.position;
        float distance = dirToTarget.magnitude;

        // Check range
        if (distance > _lockOnRange) return float.MaxValue;

        // Check angle
        float angle = Vector3.Angle(_player.forward, dirToTarget);
        if (angle > _lockOnAngle) return float.MaxValue;

        // Score based on distance and angle (prefer closer and more centered)
        return distance + angle * 0.5f;
    }

    void LockOn(Transform target)
    {
        _currentTarget = target;
        _indicator.SetActive(true);

        if (_camera != null)
        {
            _camera.SetLockOnTarget(target);
        }

        Debug.Log($"Locked on to: {target.name}");
    }

    void Unlock()
    {
        _currentTarget = null;
        _indicator.SetActive(false);

        if (_camera != null)
        {
            _camera.SetLockOnTarget(null);
        }

        Debug.Log("Lock-on released");
    }

    void OnDestroy()
    {
        if (_indicator != null)
        {
            Destroy(_indicator);
        }
    }
}
