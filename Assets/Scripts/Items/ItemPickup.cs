using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData   data;
    public Vector2Int GridPos;

    public void Collect()
    {
        PlayerInventory.Instance.Equip(data);
        HUDController.Instance?.Log($"Equipped {data.itemName} ({data.slot}).");

        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
