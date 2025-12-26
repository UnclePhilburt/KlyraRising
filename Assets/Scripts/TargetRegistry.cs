using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central registry for all targetable objects. Eliminates expensive FindObjectsByType calls.
/// Enemies and TrainingDummies register themselves on enable/disable.
/// </summary>
public class TargetRegistry : MonoBehaviour
{
    private static TargetRegistry _instance;
    private static bool _applicationQuitting = false;

    public static TargetRegistry Instance
    {
        get
        {
            if (_applicationQuitting)
                return null;

            if (_instance == null)
            {
                var go = new GameObject("TargetRegistry");
                _instance = go.AddComponent<TargetRegistry>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    void OnApplicationQuit()
    {
        _applicationQuitting = true;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _applicationQuitting = true;
    }

    // Reset static flag when scripts reload (entering play mode again)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _applicationQuitting = false;
        _instance = null;
    }

    private HashSet<Enemy> _enemies = new HashSet<Enemy>();
    private HashSet<TrainingDummy> _dummies = new HashSet<TrainingDummy>();

    // Cached lists to avoid allocations when iterating
    private List<Enemy> _enemyList = new List<Enemy>();
    private List<TrainingDummy> _dummyList = new List<TrainingDummy>();
    private bool _enemyListDirty = true;
    private bool _dummyListDirty = true;

    public IReadOnlyList<Enemy> Enemies
    {
        get
        {
            if (_enemyListDirty)
            {
                _enemyList.Clear();
                _enemyList.AddRange(_enemies);
                _enemyListDirty = false;
            }
            return _enemyList;
        }
    }

    public IReadOnlyList<TrainingDummy> Dummies
    {
        get
        {
            if (_dummyListDirty)
            {
                _dummyList.Clear();
                _dummyList.AddRange(_dummies);
                _dummyListDirty = false;
            }
            return _dummyList;
        }
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (_enemies.Add(enemy))
            _enemyListDirty = true;
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (_enemies.Remove(enemy))
            _enemyListDirty = true;
    }

    public void RegisterDummy(TrainingDummy dummy)
    {
        if (_dummies.Add(dummy))
            _dummyListDirty = true;
    }

    public void UnregisterDummy(TrainingDummy dummy)
    {
        if (_dummies.Remove(dummy))
            _dummyListDirty = true;
    }

    /// <summary>
    /// Get all targetable transforms within range of a position.
    /// More efficient than searching all objects every frame.
    /// </summary>
    public void GetTargetsInRange(Vector3 position, float range, List<Transform> results)
    {
        results.Clear();
        float rangeSqr = range * range;

        foreach (var enemy in _enemies)
        {
            if (enemy == null) continue;
            if ((enemy.transform.position - position).sqrMagnitude <= rangeSqr)
                results.Add(enemy.transform);
        }

        foreach (var dummy in _dummies)
        {
            if (dummy == null) continue;
            if ((dummy.transform.position - position).sqrMagnitude <= rangeSqr)
                results.Add(dummy.transform);
        }
    }
}
