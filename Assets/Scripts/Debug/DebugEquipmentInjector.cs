using UnityEngine;

public class DebugEquipmentInjector : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private bool equipOnStart = false;
    [SerializeField] private bool clearInventoryBeforeEquipping = true;

    [Header("Items To Equip")]
    [SerializeField] private ItemData head;
    [SerializeField] private ItemData chest;
    [SerializeField] private ItemData legs;
    [SerializeField] private ItemData feet;
    [SerializeField] private ItemData feather;

    void Start()
    {
        if (equipOnStart)
            EquipConfiguredItems();
    }

    [ContextMenu("Debug/Equip Configured Items")]
    public void EquipConfiguredItems()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[DebugEquipmentInjector] Enter Play Mode to equip items.");
            return;
        }

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[DebugEquipmentInjector] PlayerInventory not found.");
            return;
        }

        if (clearInventoryBeforeEquipping)
            PlayerInventory.Instance.ResetInventory();

        int equippedCount = 0;
        EquipIfAssigned(head, ref equippedCount);
        EquipIfAssigned(chest, ref equippedCount);
        EquipIfAssigned(legs, ref equippedCount);
        EquipIfAssigned(feet, ref equippedCount);
        EquipIfAssigned(feather, ref equippedCount);

        string msg = $"[DEBUG] Equipped {equippedCount} configured item(s).";
        Debug.Log($"[DebugEquipmentInjector] {msg}");
        HUDController.Instance?.Log(msg);
    }

    void EquipIfAssigned(ItemData item, ref int equippedCount)
    {
        if (item == null) return;

        PlayerInventory.Instance.Equip(item);
        equippedCount++;
    }
}
