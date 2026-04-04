using UnityEngine;
using System.Collections.Generic;

public class VendorNPC : MonoBehaviour
{
    [Header("Inventory")]
    public ItemData[] itemPool;      // all possible items — assign in inspector
    public int        stockSize = 3; // how many items are for sale per floor

    public List<ItemData> CurrentStock { get; private set; } = new();
    public Vector2Int GridPos { get; private set; }

    private bool dismissed = false;

    // Called by NPCSpawner each floor to set position and roll new stock
    public void InitForFloor(Vector2Int pos)
    {
        GridPos = pos;
        transform.position = GridToWorld(pos);
        Debug.Log($"VendorNPC spawned at coordinates: {pos}");
        dismissed = false;
        RollStock();
    }

    private static Vector3 GridToWorld(Vector2Int pos)
        => new Vector3(pos.x * 4f, 1.6f, pos.y * 4f);

    void RollStock()
    {
        CurrentStock.Clear();
        if (itemPool == null || itemPool.Length == 0)
        {
            Debug.LogWarning("VendorNPC: itemPool is empty. Vendor will have no stock.");
            return;
        }

        var pool = new List<ItemData>();
        foreach (var item in itemPool)
        {
            if (item != null) pool.Add(item);
        }

        if (pool.Count == 0)
        {
            Debug.LogWarning("VendorNPC: itemPool only contains null entries. Vendor will have no stock.");
            return;
        }

        for (int i = 0; i < stockSize && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            CurrentStock.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
    }

    public void OpenShop()
    {
        if (dismissed) return;
        if (CurrentStock.Count == 0)
            Debug.LogWarning("VendorNPC: Opening shop with empty stock.");

        GameManager.Instance.SetState(GameState.Shopping);
        if (ShopUI.Instance != null) ShopUI.Instance.Show(this, CurrentStock);
    }

    // Called by ShopUI when a purchase is made
    public void BuyItem(ItemData item)
    {
        if (!FloorManager.Instance.SpendGold(item.buyPrice)) return;
        CurrentStock.Remove(item);
        // Spawn the item at the player's feet so ItemPickup.Collect() handles applying stats
        ItemSpawner.Instance.SpawnItemAt(item, FindFirstObjectByType<GridMover>().GetGridPos());
        if (CurrentStock.Count == 0) DismissShop();
    }

    public void CloseShop()
    {
        GameManager.Instance.SetState(GameState.Exploring);
        if (ShopUI.Instance != null) ShopUI.Instance.Hide();
    }

    // Called when stock is exhausted — vendor can't be reopened this floor
    void DismissShop()
    {
        dismissed = true;
        CloseShop();
    }
}
