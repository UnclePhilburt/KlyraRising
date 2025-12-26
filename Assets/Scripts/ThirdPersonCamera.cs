using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private float _distance = 6f;
    [SerializeField] private float _minDistance = 4f;
    [SerializeField] private float _maxDistance = 6f;
    [SerializeField] private float _zoomSpeed = 3f;
    [SerializeField] private float _height = 1.5f;
    [SerializeField] private float _shoulderOffset = 0.7f;
    [SerializeField] private float _sensitivity = 2f;
    [SerializeField] private float _minPitch = -30f;
    [SerializeField] private float _maxPitch = 60f;
    [SerializeField] private float _lockOnRotationSpeed = 5f;

    [Header("Collision")]
    [SerializeField] private float _collisionRadius = 0.2f;
    [SerializeField] private LayerMask _collisionLayers = -1;
    [SerializeField] private float _collisionBuffer = 0.1f;

    [Header("Smoothing")]
    [SerializeField] private float _followSmoothTime = 0.2f;
    [SerializeField] private float _deadZoneRadius = 0.5f;
    [SerializeField] private float _transitionSmoothTime = 0.3f;

    private Camera _camera;
    private float _yaw;
    private float _pitch = 10f;
    private Transform _lockOnTarget;
    private Vector3 _currentPivot;
    private Vector3 _pivotVelocity;

    // Smooth transitions
    private Vector3 _currentPosition;
    private Vector3 _positionVelocity;
    private Vector3 _currentLookPoint;
    private Vector3 _lookPointVelocity;

    public void Initialize(Transform target, Camera camera)
    {
        _target = target;
        _camera = camera;
        _yaw = 0f;
        _currentPivot = target.position + Vector3.up * _height;
        _currentPosition = transform.position;
        _currentLookPoint = _currentPivot;
    }

    public void SetLockOnTarget(Transform target)
    {
        _lockOnTarget = target;
    }

    public void SetTarget(Transform target)
    {
        _target = target;
        _currentPivot = target.position + Vector3.up * _height;
    }

    void LateUpdate()
    {
        if (_target == null || _camera == null) return;

        // Scroll wheel zoom (always available)
        if (Mouse.current != null)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll != 0)
            {
                _distance -= scroll * _zoomSpeed * 0.01f;
                _distance = Mathf.Clamp(_distance, _minDistance, _maxDistance);
            }
        }

        Vector3 targetPosition;
        Vector3 targetLookPoint;

        if (_lockOnTarget != null)
        {
            CalculateLockOnCamera(out targetPosition, out targetLookPoint);
        }
        else
        {
            CalculateFreeCamera(out targetPosition, out targetLookPoint);
        }

        // Smooth transition for position and look point
        _currentPosition = Vector3.SmoothDamp(_currentPosition, targetPosition, ref _positionVelocity, _transitionSmoothTime);
        _currentLookPoint = Vector3.SmoothDamp(_currentLookPoint, targetLookPoint, ref _lookPointVelocity, _transitionSmoothTime);

        transform.position = _currentPosition;
        transform.LookAt(_currentLookPoint);
    }

    void CalculateFreeCamera(out Vector3 position, out Vector3 lookPoint)
    {
        // Mouse input
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * _sensitivity * 0.1f;
            _pitch -= delta.y * _sensitivity * 0.1f;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        // Target pivot point
        Vector3 targetPivot = _target.position + Vector3.up * _height;

        // Dead zone - only move pivot if player is outside dead zone
        Vector3 pivotToTarget = targetPivot - _currentPivot;
        pivotToTarget.y = 0; // Only horizontal dead zone

        if (pivotToTarget.magnitude > _deadZoneRadius)
        {
            // Move pivot to keep player at edge of dead zone
            Vector3 deadZoneEdge = _currentPivot + pivotToTarget.normalized * _deadZoneRadius;
            deadZoneEdge.y = targetPivot.y;
            targetPivot = _target.position + (targetPivot - deadZoneEdge);
            targetPivot.y = _target.position.y + _height;
        }
        else
        {
            // Player inside dead zone - keep current pivot but match height
            targetPivot = _currentPivot;
            targetPivot.y = _target.position.y + _height;
        }

        // Smooth follow
        _currentPivot = Vector3.SmoothDamp(_currentPivot, targetPivot, ref _pivotVelocity, _followSmoothTime);

        // Camera orbits around pivot
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredPosition = _currentPivot - rotation * Vector3.forward * _distance;

        // Offset to the right (relative to camera direction)
        Vector3 right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;
        desiredPosition += right * _shoulderOffset;

        // Apply collision
        position = HandleCollision(_currentPivot, desiredPosition);
        lookPoint = _currentPivot;
    }

    void CalculateLockOnCamera(out Vector3 position, out Vector3 lookPoint)
    {
        // Calculate direction from player to target
        Vector3 dirToTarget = _lockOnTarget.position - _target.position;
        dirToTarget.y = 0;

        // Target yaw angle
        float targetYaw = Quaternion.LookRotation(dirToTarget).eulerAngles.y;

        // Smoothly rotate camera to face target
        _yaw = Mathf.LerpAngle(_yaw, targetYaw, _lockOnRotationSpeed * Time.deltaTime);

        // Allow vertical adjustment with mouse
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _pitch -= delta.y * _sensitivity * 0.05f;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        // Position camera behind player, facing target
        Vector3 pivot = _target.position + Vector3.up * _height;
        _currentPivot = pivot; // Update pivot for when we exit lock-on

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0);
        Vector3 desiredPosition = pivot - rotation * Vector3.forward * _distance;

        // Offset to the right
        Vector3 right = Quaternion.Euler(0, _yaw, 0) * Vector3.right;
        desiredPosition += right * _shoulderOffset;

        // Apply collision
        position = HandleCollision(pivot, desiredPosition);

        // Look at point between player and target
        lookPoint = (_target.position + _lockOnTarget.position) * 0.5f + Vector3.up * _height;
    }

    Vector3 HandleCollision(Vector3 pivot, Vector3 desiredPosition)
    {
        // Ignore player layer
        int layerMask = _collisionLayers & ~(1 << _target.gameObject.layer);

        Vector3 direction = desiredPosition - pivot;
        float distance = direction.magnitude;

        if (Physics.SphereCast(pivot, _collisionRadius, direction.normalized, out RaycastHit hit, distance, layerMask))
        {
            // Move camera to hit point with buffer
            float adjustedDistance = hit.distance - _collisionBuffer;
            adjustedDistance = Mathf.Max(adjustedDistance, 0.5f); // Minimum distance
            return pivot + direction.normalized * adjustedDistance;
        }

        return desiredPosition;
    }

    public Vector3 GetForward()
    {
        return Quaternion.Euler(0, _yaw, 0) * Vector3.forward;
    }

    public Vector3 GetRight()
    {
        return Quaternion.Euler(0, _yaw, 0) * Vector3.right;
    }

    public bool IsLockedOn()
    {
        return _lockOnTarget != null;
    }
}
