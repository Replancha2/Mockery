using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    public event System.Action OnInventoryChanged;

    private readonly Dictionary<EquipmentSlot, ItemData> _equipped = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public ItemData GetEquipped(EquipmentSlot slot)
        => _equipped.TryGetValue(slot, out var item) ? item : null;

    public void Equip(ItemData item)
    {
        if (_equipped.TryGetValue(item.slot, out var old) && old != null)
            Unapply(old);

        Apply(item);
        _equipped[item.slot] = item;
        OnInventoryChanged?.Invoke();
    }

    public void ResetInventory()
    {
        _equipped.Clear();
        OnInventoryChanged?.Invoke();
    }

    // -------------------------------------------------------------------------

    static void Apply(ItemData item)
    {
        var stats = PlayerStats.Instance;
        if (item.hpBonus > 0)        stats.RestoreHP(item.hpBonus);
        if (item.defenseBonus > 0)   stats.AddDefenseBonus(item.defenseBonus);
        if (item.AssonantBonus > 0)  stats.AddSpellDamageBonus(SpellType.Assonant,  item.AssonantBonus);
        if (item.DissonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Dissonant, item.DissonantBonus);
        if (item.ConsonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Consonant, item.ConsonantBonus);
    }

    static void Unapply(ItemData item)
    {
        var stats = PlayerStats.Instance;
        // hpBonus was a one-time heal — not reversed on unequip
        if (item.defenseBonus > 0)   stats.AddDefenseBonus(-item.defenseBonus);
        if (item.AssonantBonus > 0)  stats.AddSpellDamageBonus(SpellType.Assonant,  -item.AssonantBonus);
        if (item.DissonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Dissonant, -item.DissonantBonus);
        if (item.ConsonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Consonant, -item.ConsonantBonus);
    }
}
