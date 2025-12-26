using UnityEngine;

public enum BehaviorType
{
    Idle,
    Patrol,
    Chase,
    Wander
}

public class EnemyBehavior : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private BehaviorType _behaviorType = BehaviorType.Idle;

    [Header("Idle Settings")]
    [SerializeField] private bool _facePlayerWhenNear = false;
    [SerializeField] private float _facePlayerDistance = 10f;

    [Header("Patrol Settings")]
    [SerializeField] private Transform[] _patrolPoints;
    [SerializeField] private float _patrolSpeed = 2f;
    [SerializeField] private float _patrolWaitTime = 2f;

    [Header("Chase Settings")]
    [SerializeField] private float _chaseSpeed = 4f;
    [SerializeField] private float _detectionRange = 15f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackCooldown = 2f;
    [SerializeField] private float _attackDamage = 10f;
    [SerializeField] private float _chaseTimeout = 5f;

    [Header("Wander Settings")]
    [SerializeField] private float _wanderRadius = 10f;
    [SerializeField] private float _wanderSpeed = 1.5f;
    [SerializeField] private float _wanderWaitTime = 3f;

    private Enemy _enemy;
    private Transform _player;
    private CharacterController _controller;
    private EnemyCombat _combat;

    // State
    private int _currentPatrolPoint = 0;
    private float _waitTimer = 0f;
    private bool _isWaiting = false;
    private float _attackTimer = 0f;
    private bool _isChasing = false;
    private float _chaseTimer = 0f;
    private Vector3 _startPosition;
    private Vector3 _wanderTarget;
    private float _currentSpeed;

    public BehaviorType CurrentBehavior => _behaviorType;
    public float CurrentSpeed => _currentSpeed;

    public void Initialize(Enemy enemy)
    {
        _enemy = enemy;
        _controller = enemy.GetComponent<CharacterController>();
        _combat = enemy.GetComponent<EnemyCombat>();

        // Add EnemyCombat if Chase behavior and not present
        if (_combat == null && _behaviorType == BehaviorType.Chase)
        {
            _combat = enemy.gameObject.AddComponent<EnemyCombat>();
            _combat.Initialize();
        }

        _startPosition = transform.position;

        // Pick initial wander target
        PickNewWanderTarget();

        // Find player
        var player = FindFirstObjectByType<ThirdPersonController>();
        if (player != null)
        {
            _player = player.transform;
        }
    }

    public void UpdateBehavior()
    {
        switch (_behaviorType)
        {
            case BehaviorType.Idle:
                UpdateIdle();
                break;
            case BehaviorType.Patrol:
                UpdatePatrol();
                break;
            case BehaviorType.Chase:
                UpdateChase();
                break;
            case BehaviorType.Wander:
                UpdateWander();
                break;
        }
    }

    #region Idle
    void UpdateIdle()
    {
        _currentSpeed = 0f;
        if (_facePlayerWhenNear && DistanceToPlayer() < _facePlayerDistance)
        {
            FacePlayer();
        }
    }
    #endregion

    #region Patrol
    void UpdatePatrol()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0)
        {
            _currentSpeed = 0f;
            return;
        }

        if (_isWaiting)
        {
            _currentSpeed = 0f;
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0)
            {
                _isWaiting = false;
                _currentPatrolPoint = (_currentPatrolPoint + 1) % _patrolPoints.Length;
            }
            return;
        }

        Transform target = _patrolPoints[_currentPatrolPoint];
        if (target == null) return;

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance < 0.5f)
        {
            _isWaiting = true;
            _waitTimer = _patrolWaitTime;
        }
        else
        {
            MoveToward(target.position, _patrolSpeed);
        }
    }
    #endregion

    #region Chase
    void UpdateChase()
    {
        float distance = DistanceToPlayer();

        if (_attackTimer > 0)
            _attackTimer -= Time.deltaTime;

        // Start chasing if player in range
        if (distance < _detectionRange)
        {
            if (!_isChasing)
            {
                _isChasing = true;
                // Draw sword when first spotting player
                if (_combat != null && !_combat.IsArmed && !_combat.IsDrawing)
                {
                    _combat.DrawSword();
                }
            }
            _chaseTimer = _chaseTimeout;
        }

        if (_isChasing)
        {
            _chaseTimer -= Time.deltaTime;

            // Lost player - stop chasing and sheathe
            if (_chaseTimer <= 0 || distance > _detectionRange * 1.5f)
            {
                _isChasing = false;
                _currentSpeed = 0f;
                // Sheathe sword when losing player
                if (_combat != null && _combat.IsArmed)
                {
                    _combat.SheatheSword();
                }
                return;
            }

            // Wait for sword to be drawn before attacking
            if (_combat != null && !_combat.IsArmed)
            {
                _currentSpeed = 0f;
                FacePlayer();
                return;
            }

            // Don't act while staggered or telegraphing
            if (_combat != null && !_combat.CanAct)
            {
                _currentSpeed = 0f;
                return;
            }

            if (distance < _attackRange)
            {
                _currentSpeed = 0f;
                FacePlayer();
                if (_attackTimer <= 0 && _combat != null && _combat.CanAct)
                {
                    Attack();
                }
            }
            else
            {
                MoveToward(_player.position, _chaseSpeed);
            }
        }
        else
        {
            _currentSpeed = 0f;
        }
    }

    void Attack()
    {
        _attackTimer = _attackCooldown;

        // Trigger attack animation (with telegraph)
        if (_combat != null)
        {
            _combat.Attack();

            // Damage is dealt after telegraph, so schedule it
            StartCoroutine(DealDamageAfterTelegraph());
        }
    }

    System.Collections.IEnumerator DealDamageAfterTelegraph()
    {
        float telegraphTime = _combat != null ? _combat.TelegraphDuration : 0.3f;
        yield return new WaitForSeconds(telegraphTime);

        if (_combat != null && _combat.IsStaggered) yield break;

        if (_player != null)
        {
            PlayerHealth playerHealth = _player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                Vector3 attackDir = (_player.position - transform.position).normalized;
                PlayerHealth.DamageResult result = playerHealth.TakeDamage(_attackDamage, attackDir, null);

                if (result == PlayerHealth.DamageResult.Parried)
                {
                    StartCoroutine(StaggerAfterAttack());
                }
            }
        }
    }

    System.Collections.IEnumerator StaggerAfterAttack()
    {
        // Wait for attack animation to finish
        float attackDuration = 0.5f; // Default
        if (_combat != null)
        {
            // Wait until attack is done
            while (_combat.IsAttacking)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(attackDuration);
        }

        // Now stagger
        if (_combat != null)
        {
            _combat.Stagger();
        }
    }
    #endregion

    #region Wander
    void UpdateWander()
    {
        if (_isWaiting)
        {
            _currentSpeed = 0f;
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0)
            {
                _isWaiting = false;
                PickNewWanderTarget();
            }
            return;
        }

        float distance = Vector3.Distance(transform.position, _wanderTarget);

        if (distance < 0.5f)
        {
            _isWaiting = true;
            _waitTimer = _wanderWaitTime;
        }
        else
        {
            MoveToward(_wanderTarget, _wanderSpeed);
        }
    }

    void PickNewWanderTarget()
    {
        // Pick a random point at least 2 units away
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(2f, _wanderRadius);
            Vector3 newTarget = _startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            if (Vector3.Distance(transform.position, newTarget) > 1f)
            {
                _wanderTarget = newTarget;
                return;
            }
        }

        // Fallback - just pick any point
        Vector2 fallback = Random.insideUnitCircle.normalized * _wanderRadius * 0.5f;
        _wanderTarget = _startPosition + new Vector3(fallback.x, 0, fallback.y);
    }
    #endregion

    #region Helpers
    void MoveToward(Vector3 target, float speed)
    {
        if (_controller == null) return;

        Vector3 direction = (target - transform.position);
        direction.y = 0;

        if (direction.magnitude > 0.1f)
        {
            direction.Normalize();

            // Combine horizontal movement with downward force to stay grounded
            Vector3 move = direction * speed * Time.deltaTime;
            move.y = -5f * Time.deltaTime; // Constant downward force

            _controller.Move(move);
            _currentSpeed = speed;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                10f * Time.deltaTime
            );
        }
        else
        {
            _currentSpeed = 0f;
        }
    }

    float DistanceToPlayer()
    {
        if (_player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, _player.position);
    }

    void FacePlayer()
    {
        if (_player == null) return;

        Vector3 dir = _player.position - transform.position;
        dir.y = 0;
        if (dir.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                5f * Time.deltaTime
            );
        }
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        switch (_behaviorType)
        {
            case BehaviorType.Patrol:
                DrawPatrolGizmos();
                break;
            case BehaviorType.Chase:
                DrawChaseGizmos();
                break;
            case BehaviorType.Wander:
                DrawWanderGizmos();
                break;
        }
    }

    void DrawPatrolGizmos()
    {
        if (_patrolPoints == null || _patrolPoints.Length == 0) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _patrolPoints.Length; i++)
        {
            if (_patrolPoints[i] != null)
            {
                Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.3f);
                int next = (i + 1) % _patrolPoints.Length;
                if (_patrolPoints[next] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[next].position);
                }
            }
        }
    }

    void DrawChaseGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }

    void DrawWanderGizmos()
    {
        Vector3 center = Application.isPlaying ? _startPosition : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(center, _wanderRadius);
    }
}
