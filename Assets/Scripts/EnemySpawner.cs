using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    private List<Enemy> _enemies = new List<Enemy>();

    public IReadOnlyList<Enemy> Enemies => _enemies;
    public int EnemyCount => _enemies.Count;
    public int AliveCount => _enemies.FindAll(e => e != null).Count;

    void Start()
    {
        // Find all Enemy components in children
        Enemy[] childEnemies = GetComponentsInChildren<Enemy>();
        _enemies.AddRange(childEnemies);

        Debug.Log($"[EnemySpawner] Managing {_enemies.Count} enemies");
        foreach (var e in _enemies)
        {
            Debug.Log($"  - {e.name} at {e.transform.position}");
        }
    }

    // Get all alive enemies
    public List<Enemy> GetAliveEnemies()
    {
        _enemies.RemoveAll(e => e == null);
        return _enemies;
    }

    // Optional: spawn an additional enemy at runtime
    public Enemy SpawnEnemy(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;

        GameObject enemyObj = Instantiate(prefab, position, Quaternion.identity, transform);
        Enemy enemy = enemyObj.GetComponent<Enemy>();

        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }

        _enemies.Add(enemy);
        Debug.Log($"[EnemySpawner] Spawned {enemyObj.name} at {position}");
        return enemy;
    }
}
