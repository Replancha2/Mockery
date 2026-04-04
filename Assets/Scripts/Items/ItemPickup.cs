using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData   data;
    public Vector2Int GridPos;

    public void Collect()
    {
        var result = PlayerInventory.Instance.AcquireItem(data);
        if (result == PlayerInventory.ItemAcquireResult.StoredInBackpack)
            HUDController.Instance?.Log($"Backpack: {data.itemName}");
        else
            HUDController.Instance?.Log($"Equipped {data.itemName} ({data.slot}).");

        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
