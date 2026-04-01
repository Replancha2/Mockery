using UnityEngine;
using System.Collections.Generic;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    public ItemData[] itemPool;   // assign in inspector: HealthPotion, LuteString, etc.
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
        {
            ItemData d  = itemPool[Random.Range(0, itemPool.Length)];
            var go      = Instantiate(itemPickupPrefab, GridMover.GridToWorld(pos), Quaternion.identity, transform);
            var pickup  = go.GetComponent<ItemPickup>();
            pickup.data    = d;
            pickup.GridPos = pos;
            activeItems.Add(pickup);
        }
    }

    public ItemPickup GetItemAt(Vector2Int pos)
        => activeItems.Find(i => i != null && i.GridPos == pos);

    public void RemoveItem(ItemPickup item)
    {
        activeItems.Remove(item);
    }
}
