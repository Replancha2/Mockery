using UnityEngine;
using System.Collections.Generic;

public class VendorNPC : MonoBehaviour
{
    public static VendorNPC Instance { get; private set; }

    [Header("Inventory")]
    public ItemData[] itemPool;      // all possible items — assign in inspector
    public int        stockSize = 3; // how many items are for sale per floor

    public List<ItemData> CurrentStock { get; private set; } = new();
    public Vector2Int GridPos { get; private set; }

    private bool dismissed = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Called by DungeonOrchestrator each floor to set position and roll new stock
    public void InitForFloor(Vector2Int pos)
    {
        GridPos = pos;
        dismissed = false;
        RollStock();
    }

    void RollStock()
    {
        CurrentStock.Clear();
        var pool = new List<ItemData>(itemPool);
        for (int i = 0; i < stockSize && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            CurrentStock.Add(pool[idx]);
            pool.RemoveAt(idx);
        }
    }

    public void OpenShop()
    {
        if (dismissed || CurrentStock.Count == 0) return;
        GameManager.Instance.SetState(GameState.Shopping);
        if (ShopUI.Instance != null) ShopUI.Instance.Show(CurrentStock);
    }

    // Called by ShopUI when a purchase is made
    public void BuyItem(ItemData item)
    {
        if (!FloorManager.Instance.SpendGold(item.buyPrice)) return;
        CurrentStock.Remove(item);
        // Spawn the item at the player's feet so ItemPickup.Collect() handles applying stats
        ItemSpawner.Instance.SpawnItemAt(item, FindFirstObjectByType<GridMover>().GetGridPos());
        if (CurrentStock.Count == 0) CloseShop();
    }

    public void CloseShop()
    {
        dismissed = true;
        GameManager.Instance.SetState(GameState.Exploring);
        if (ShopUI.Instance != null) ShopUI.Instance.Hide();
    }
}
