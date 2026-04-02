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
        if (data.asonanteBonus > 0)    stats.AddSpellDamageBonus(ElementType.Asonante,    data.asonanteBonus);
        if (data.discordanteBonus > 0) stats.AddSpellDamageBonus(ElementType.Discordante, data.discordanteBonus);
        if (data.consonanteBonus > 0)  stats.AddSpellDamageBonus(ElementType.Consonante,  data.consonanteBonus);

        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
