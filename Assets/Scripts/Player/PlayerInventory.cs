using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public enum ItemAcquireResult { Equipped, StoredInBackpack }

    public event System.Action OnInventoryChanged;

    private readonly Dictionary<EquipmentSlot, ItemData> _equipped = new();
    private readonly List<ItemData> _backpack = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public ItemData GetEquipped(EquipmentSlot slot)
        => _equipped.TryGetValue(slot, out var item) ? item : null;

    public IReadOnlyList<ItemData> BackpackItems => _backpack;

    public void Equip(ItemData item)
    {
        if (item == null) return;

        if (_equipped.TryGetValue(item.slot, out var old) && old != null)
            Unapply(old);

        Apply(item);
        _equipped[item.slot] = item;
        OnInventoryChanged?.Invoke();
    }

    public ItemAcquireResult AcquireItem(ItemData item)
    {
        if (item == null) return ItemAcquireResult.Equipped;

        if (AreAllEquipmentSlotsFilled())
        {
            _backpack.Add(item);
            OnInventoryChanged?.Invoke();
            return ItemAcquireResult.StoredInBackpack;
        }

        Equip(item);
        return ItemAcquireResult.Equipped;
    }

    public bool EquipFromBackpack(int index, out ItemData equippedItem, out ItemData replacedItem)
    {
        equippedItem = null;
        replacedItem = null;

        if (index < 0 || index >= _backpack.Count)
            return false;

        ItemData item = _backpack[index];
        _backpack.RemoveAt(index);

        if (_equipped.TryGetValue(item.slot, out var old) && old != null)
        {
            Unapply(old);
            replacedItem = old;
            _backpack.Add(old);
        }

        Apply(item);
        _equipped[item.slot] = item;
        equippedItem = item;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ResetInventory()
    {
        _equipped.Clear();
        _backpack.Clear();
        OnInventoryChanged?.Invoke();
    }

    bool AreAllEquipmentSlotsFilled()
    {
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (!_equipped.TryGetValue(slot, out var item) || item == null)
                return false;
        }

        return true;
    }

    // -------------------------------------------------------------------------

    static void Apply(ItemData item)
    {
        var stats = PlayerStats.Instance;
        if (item.hpBonus > 0)        stats.AddMaxHP(item.hpBonus);
        if (item.defenseBonus > 0)   stats.AddDefenseBonus(item.defenseBonus);
        if (item.AssonantBonus > 0)  stats.AddSpellDamageBonus(SpellType.Assonant,  item.AssonantBonus);
        if (item.DissonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Dissonant, item.DissonantBonus);
        if (item.ConsonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Consonant, item.ConsonantBonus);
    }

    static void Unapply(ItemData item)
    {
        var stats = PlayerStats.Instance;
        if (item.hpBonus > 0)        stats.AddMaxHP(-item.hpBonus);
        if (item.defenseBonus > 0)   stats.AddDefenseBonus(-item.defenseBonus);
        if (item.AssonantBonus > 0)  stats.AddSpellDamageBonus(SpellType.Assonant,  -item.AssonantBonus);
        if (item.DissonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Dissonant, -item.DissonantBonus);
        if (item.ConsonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Consonant, -item.ConsonantBonus);
    }
}
