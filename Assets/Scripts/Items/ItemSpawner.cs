using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    public ItemData[] itemPool;
    public GameObject itemPickupPrefab;

    private List<ItemPickup> activeItems = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnItems(DungeonData data)
    {
        foreach (var item in activeItems) if (item) Destroy(item.gameObject);
        activeItems.Clear();

        foreach (var pos in data.ItemSpawns)
            SpawnItemAt(itemPool[Random.Range(0, itemPool.Length)], pos);
    }

    // Spawns a specific item at a grid position (used by vendor purchases and enemy drops)
    public void SpawnItemAt(ItemData d, Vector2Int pos)
    {
        var go     = Instantiate(itemPickupPrefab, GridMover.GridToWorld(pos), Quaternion.identity, transform);
        var pickup = go.GetComponent<ItemPickup>();
        pickup.data    = d;
        pickup.GridPos = pos;
        activeItems.Add(pickup);
    }

    // Spawns a random item from the pool (used by enemy loot drops)
    public void SpawnRandomItemAt(Vector2Int pos)
    {
        if (itemPool == null || itemPool.Length == 0) return;
        SpawnItemAt(itemPool[Random.Range(0, itemPool.Length)], pos);
    }

    public ItemPickup GetItemAt(Vector2Int pos)
        => activeItems.Find(i => i != null && i.GridPos == pos);

    public void RemoveItem(ItemPickup item)
        => activeItems.Remove(item);
}
