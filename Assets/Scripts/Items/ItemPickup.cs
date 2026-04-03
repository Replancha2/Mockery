using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData   data;
    public Vector2Int GridPos;

    public void Collect()
    {
        // Apply stat bonuses directly (equipment is stat-only, no visual cosmetics)
        var stats = PlayerStats.Instance;
        if (data.hpBonus > 0)          stats.RestoreHP(data.hpBonus);
        if (data.defenseBonus > 0)     stats.AddDefenseBonus(data.defenseBonus);
        if (data.AssonantBonus > 0)    stats.AddSpellDamageBonus(SpellType.Assonant,    data.AssonantBonus);
        if (data.DissonantBonus > 0) stats.AddSpellDamageBonus(SpellType.Dissonant, data.DissonantBonus);
        if (data.ConsonantBonus > 0)  stats.AddSpellDamageBonus(SpellType.Consonant,  data.ConsonantBonus);

        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
