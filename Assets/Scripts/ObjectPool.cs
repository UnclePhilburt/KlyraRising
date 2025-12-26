using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic object pool to avoid Instantiate/Destroy overhead.
/// Supports prefab pooling and runtime-created objects.
/// </summary>
public class ObjectPool : MonoBehaviour
{
    private static ObjectPool _instance;
    public static ObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ObjectPool");
                _instance = go.AddComponent<ObjectPool>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>();
    private Dictionary<string, GameObject> _prefabs = new Dictionary<string, GameObject>();

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    /// <summary>
    /// Get an object from the pool, or create a new one if pool is empty.
    /// </summary>
    public GameObject Get(string poolKey, GameObject prefab = null)
    {
        if (!_pools.ContainsKey(poolKey))
            _pools[poolKey] = new Queue<GameObject>();

        if (prefab != null && !_prefabs.ContainsKey(poolKey))
            _prefabs[poolKey] = prefab;

        GameObject obj;
        if (_pools[poolKey].Count > 0)
        {
            obj = _pools[poolKey].Dequeue();
            if (obj == null)
            {
                // Object was destroyed, get another or create new
                return Get(poolKey, prefab);
            }
            obj.SetActive(true);
        }
        else if (_prefabs.TryGetValue(poolKey, out GameObject storedPrefab))
        {
            obj = Instantiate(storedPrefab);
            obj.name = poolKey;
        }
        else
        {
            obj = new GameObject(poolKey);
        }

        return obj;
    }

    /// <summary>
    /// Return an object to the pool for reuse.
    /// </summary>
    public void Return(string poolKey, GameObject obj)
    {
        if (obj == null) return;

        if (!_pools.ContainsKey(poolKey))
            _pools[poolKey] = new Queue<GameObject>();

        obj.SetActive(false);
        obj.transform.SetParent(transform);
        _pools[poolKey].Enqueue(obj);
    }

    /// <summary>
    /// Pre-warm a pool with a number of instances.
    /// </summary>
    public void Prewarm(string poolKey, GameObject prefab, int count)
    {
        if (!_pools.ContainsKey(poolKey))
            _pools[poolKey] = new Queue<GameObject>();

        if (!_prefabs.ContainsKey(poolKey))
            _prefabs[poolKey] = prefab;

        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.name = poolKey;
            obj.SetActive(false);
            obj.transform.SetParent(transform);
            _pools[poolKey].Enqueue(obj);
        }
    }

    /// <summary>
    /// Clear a specific pool.
    /// </summary>
    public void ClearPool(string poolKey)
    {
        if (_pools.TryGetValue(poolKey, out Queue<GameObject> pool))
        {
            while (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                if (obj != null)
                    Destroy(obj);
            }
        }
    }
}
