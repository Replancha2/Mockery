using UnityEngine;

public class EnemyInstance : MonoBehaviour
{
    public EnemyData      data;
    public EnemyResistance Resistance  { get; private set; }
    public int             CurrentHP   { get; private set; }
    public int             MaxHP       { get; private set; }
    public Vector2Int      GridPos     { get; private set; }

    public void Init(EnemyData d, Vector2Int pos)
    {
        data      = d;
        MaxHP     = d.isMini ? d.maxHP * 2 : d.maxHP;
        CurrentHP = MaxHP;
        GridPos   = pos;
        Resistance = new EnemyResistance();

        transform.position = GridMover.GridToWorld(pos);
        if (d.vfxPrefab) Instantiate(d.vfxPrefab, transform);
    }

    // Returns true if enemy died
    public bool TakeDamage(int amount)
    {
        CurrentHP -= amount;
        return CurrentHP <= 0;
    }

    public enum AbilityType { Heal, DOT, PowerStrike }

    // Enemy chooses and executes an ability this turn
    public AbilityType Act()
    {
        float total  = data.healWeight + data.dotWeight + data.powerStrikeWeight;
        float roll   = Random.Range(0f, total);

        AbilityType chosen;
        if (roll < data.healWeight)
            chosen = AbilityType.Heal;
        else if (roll < data.healWeight + data.dotWeight)
            chosen = AbilityType.DOT;
        else
            chosen = AbilityType.PowerStrike;

        switch (chosen)
        {
            case AbilityType.Heal:
                int healAmount = Mathf.RoundToInt(MaxHP * data.healPercent);
                CurrentHP = Mathf.Min(MaxHP, CurrentHP + healAmount);
                break;

            case AbilityType.DOT:
                PlayerStats.Instance.ApplyDOT(data.dotDamagePerTurn, data.dotDuration);
                break;

            case AbilityType.PowerStrike:
                int dmg = data.isMini
                    ? Mathf.RoundToInt(data.attackDamage * 2f * data.powerStrikeMultiplier)
                    : Mathf.RoundToInt(data.attackDamage * data.powerStrikeMultiplier);
                PlayerStats.Instance.TakeDamage(dmg);
                break;
        }

        return chosen;
    }

    // Normal attack (used when enemy doesn't use a special ability)
    public void BasicAttack()
    {
        int dmg = data.isMini ? data.attackDamage * 2 : data.attackDamage;
        PlayerStats.Instance.TakeDamage(dmg);
    }

    public void DropLoot()
    {
        int gold = Random.Range(data.goldMin, data.goldMax + 1);
        FloorManager.Instance.AddGold(gold);

        if (Random.value < data.itemDropChance)
            ItemSpawner.Instance.SpawnRandomItemAt(GridPos);
    }
}
