using UnityEngine;
using System.Collections.Generic;

public class DungeonRenderer : MonoBehaviour
{
    public static DungeonRenderer Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject ceilingPrefab;
    public GameObject stairsPrefab;

    private DungeonData currentData;
    private List<GameObject> spawnedObjects = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Render(DungeonData data)
    {
        currentData = data;
        foreach (var obj in spawnedObjects) Destroy(obj);
        spawnedObjects.Clear();

        float cell = GridMover.CellSize;

        for (int x = 0; x < data.Width; x++)
        {
            for (int y = 0; y < data.Height; y++)
            {
                var pos2D = new Vector2Int(x, y);
                if (!data.IsFloor(pos2D)) continue;

                Vector3 worldPos = new Vector3(x * cell, 0, y * cell);

                Spawn(floorPrefab,   worldPos,                     Quaternion.Euler(-90, 0, 0));
                Spawn(ceilingPrefab, worldPos + Vector3.up * cell, Quaternion.Euler(90, 0, 0));

                if (pos2D == data.StairsPos)
                    Spawn(stairsPrefab, worldPos, Quaternion.identity);

                SpawnWallIfNeeded(data, x, y, -1,  0, 270f);
                SpawnWallIfNeeded(data, x, y,  1,  0,  90f);
                SpawnWallIfNeeded(data, x, y,  0, -1, 180f);
                SpawnWallIfNeeded(data, x, y,  0,  1,   0f);
            }
        }
    }

    void SpawnWallIfNeeded(DungeonData data, int x, int y, int dx, int dy, float rotY)
    {
        if (!data.IsFloor(new Vector2Int(x + dx, y + dy)))
        {
            float cell = GridMover.CellSize;
            Vector3 wallPos = new Vector3(
                (x + dx * 0.5f) * cell,
                cell * 0.5f,
                (y + dy * 0.5f) * cell
            );
            Spawn(wallPrefab, wallPos, Quaternion.Euler(0, rotY, 0));
        }
    }

    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        var obj = Instantiate(prefab, pos, rot, transform);
        spawnedObjects.Add(obj);
        return obj;
    }

    public bool IsWalkable(Vector2Int pos) => currentData != null && currentData.IsFloor(pos);
    public bool IsStairs(Vector2Int pos)   => currentData != null && pos == currentData.StairsPos;

    public RoomTag GetRoomTag(Vector2Int pos)
    {
        if (currentData == null) return RoomTag.None;
        if (pos.x < 0 || pos.x >= currentData.Width || pos.y < 0 || pos.y >= currentData.Height)
            return RoomTag.None;
        return currentData.Tags[pos.x, pos.y];
    }
}
