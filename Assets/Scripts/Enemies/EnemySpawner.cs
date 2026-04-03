using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public EnemyData[] basicEnemies; // regular enemy templates — assign in inspector
    public EnemyData   bossData;     // dragon final boss
    public GameObject  enemyPrefab;
    public Sprite      placeholderSprite; // assign a simple sprite for dungeon visibility

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

            var go = Instantiate(enemyPrefab, transform);
            var ei = go.GetComponent<EnemyInstance>();
            ei.Init(d, data.EnemySpawns[i]);
            activeEnemies.Add(ei);

            // Add AI movement
            go.AddComponent<EnemyAI>();

            // Add placeholder sprite for dungeon visibility
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = placeholderSprite;
        }
    }

    public EnemyInstance GetEnemyAt(Vector2Int pos)
        => activeEnemies.Find(e => e != null && e.GridPos == pos);

    public void RemoveEnemy(EnemyInstance e)
    {
        activeEnemies.Remove(e);
        Destroy(e.gameObject);
    }
}
