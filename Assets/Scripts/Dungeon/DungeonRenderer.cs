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

    [Header("Wall Materials")]
    public Material[] wallMaterials; // Drag Paredes1_01, Paredes1_02, Paredes1_03 materials here

    [Header("Floor Materials")]
    public Material[] floorMaterials; // Add materials for floor tiles

    [Header("Ceiling Materials")]
    public Material[] ceilingMaterials; // Add materials for ceiling tiles

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

                GameObject floor = Spawn(floorPrefab, worldPos, Quaternion.Euler(90, 0, 0));
                if (floorMaterials != null && floorMaterials.Length > 0)
                {
                    var mr = floor.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Material selectedMat = floorMaterials[Random.Range(0, floorMaterials.Length)];
                        if (selectedMat != null)
                            mr.sharedMaterial = selectedMat;
                    }
                }

                GameObject ceiling = Spawn(ceilingPrefab, worldPos + Vector3.up * cell, Quaternion.Euler(-90, 0, 0));
                Material[] ceilingMatsToUse = (ceilingMaterials != null && ceilingMaterials.Length > 0) 
                    ? ceilingMaterials 
                    : floorMaterials;
                
                if (ceilingMatsToUse != null && ceilingMatsToUse.Length > 0)
                {
                    var mr = ceiling.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        Material selectedMat = ceilingMatsToUse[Random.Range(0, ceilingMatsToUse.Length)];
                        if (selectedMat != null)
                            mr.sharedMaterial = selectedMat;
                    }
                }

                if (pos2D == data.StairsPos)
                    Spawn(stairsPrefab, worldPos, Quaternion.identity);

                SpawnWallIfNeeded(data, x, y, -1,  0,  90f);  // left wall, face inward (east)
                SpawnWallIfNeeded(data, x, y,  1,  0, 270f);  // right wall, face inward (west)
                SpawnWallIfNeeded(data, x, y,  0, -1,   0f);  // down wall, face inward (north)
                SpawnWallIfNeeded(data, x, y,  0,  1, 180f);  // up wall, face inward (south)
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
            GameObject wall = Spawn(wallPrefab, wallPos, Quaternion.Euler(0, rotY, 0));

            if (wallMaterials != null && wallMaterials.Length > 0)
            {
                MeshRenderer mr = wall.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    Material pick = wallMaterials[Random.Range(0, wallMaterials.Length)];
                    if (pick != null)
                        mr.sharedMaterial = pick;
                }
            }
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
