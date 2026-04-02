using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public EnemyData[] basicEnemies; // regular enemy templates — assign in inspector
    public EnemyData   bossData;     // dragon final boss
    public GameObject  enemyPrefab;

    private List<EnemyInstance> activeEnemies = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnEnemies(DungeonData data, int floor)
    {
        foreach (var e in activeEnemies) if (e) Destroy(e.gameObject);
        activeEnemies.Clear();

        bool isFinalFloor = floor >= FloorManager.MaxFloors;

        // Regular enemies
        foreach (var pos in data.EnemySpawns)
        {
            EnemyData d = isFinalFloor
                ? bossData
                : basicEnemies[Random.Range(0, basicEnemies.Length)];

            SpawnEnemy(d, pos, mini: false);

            // Only place the boss once (at the first spawn point on the final floor)
            if (isFinalFloor) break;
        }

        // Mini-bosses (one per floor in the room before stairs)
        if (!isFinalFloor)
        {
            foreach (var pos in data.MiniBossSpawns)
            {
                EnemyData d = basicEnemies[Random.Range(0, basicEnemies.Length)];
                SpawnEnemy(d, pos, mini: true);
            }
        }
    }

    void SpawnEnemy(EnemyData d, Vector2Int pos, bool mini)
    {
        var go = Instantiate(enemyPrefab, transform);
        var ei = go.GetComponent<EnemyInstance>();

        // Override isMini at runtime without modifying the shared ScriptableObject
        if (mini)
        {
            // Clone the data so we don't alter the asset
            var cloned   = Instantiate(d);
            cloned.isMini = true;
            ei.Init(cloned, pos);
        }
        else
        {
            ei.Init(d, pos);
        }

        activeEnemies.Add(ei);
    }

    public EnemyInstance GetEnemyAt(Vector2Int pos)
        => activeEnemies.Find(e => e != null && e.GridPos == pos);

    public void RemoveEnemy(EnemyInstance e)
    {
        activeEnemies.Remove(e);
        Destroy(e.gameObject);
    }
}
