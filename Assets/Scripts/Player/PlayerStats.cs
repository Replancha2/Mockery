using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP        = 30;
    public int baseSpellDamage = 10;

    public int  CurrentHP    { get; private set; }
    public bool IsDead       => CurrentHP <= 0;

    // Per-element damage bonuses (added by equipment/buffs)
    private Dictionary<ElementType, int> spellDamageBonuses = new()
    {
        { ElementType.Asonante,    0 },
        { ElementType.Discordante, 0 },
        { ElementType.Consonante,  0 },
    };

    // Flat stat bonuses from buffs
    public int DefenseBonus         { get; private set; } = 0;
    public int DOTResistance        { get; private set; } = 0; // reduces DOT duration
    public int CombatStartHeal      { get; private set; } = 0;
    public int GoldBonusPerKill     { get; private set; } = 0;

    // Active damage-over-time effects
    private struct DOTEffect
    {
        public int damagePerTurn;
        public int turnsRemaining;
    }
    private List<DOTEffect> activeDOTs = new();

    public event System.Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        CurrentHP = maxHP;
    }

    public void ResetForNewRun()
    {
        CurrentHP = maxHP;
        activeDOTs.Clear();
        foreach (var key in new List<ElementType>(spellDamageBonuses.Keys))
            spellDamageBonuses[key] = 0;
        DefenseBonus     = 0;
        DOTResistance    = 0;
        CombatStartHeal  = 0;
        GoldBonusPerKill = 0;
        OnStatsChanged?.Invoke();
    }

    public bool TakeDamage(int amount)
    {
        int reduced = Mathf.Max(1, amount - DefenseBonus);
        CurrentHP = Mathf.Max(0, CurrentHP - reduced);
        OnStatsChanged?.Invoke();
        return CurrentHP <= 0;
    }

    public void RestoreHP(int amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnStatsChanged?.Invoke();
    }

    // Called at the start of the player's turn — ticks all DOTs and returns total damage dealt
    public int TickDOTs()
    {
        int total = 0;
        for (int i = activeDOTs.Count - 1; i >= 0; i--)
        {
            var dot = activeDOTs[i];
            total += dot.damagePerTurn;
            dot.turnsRemaining--;
            if (dot.turnsRemaining <= 0)
                activeDOTs.RemoveAt(i);
            else
                activeDOTs[i] = dot;
        }
        if (total > 0)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - total);
            OnStatsChanged?.Invoke();
        }
        return total;
    }

    public void ApplyDOT(int damagePerTurn, int duration)
    {
        int effectiveDuration = Mathf.Max(1, duration - DOTResistance);
        activeDOTs.Add(new DOTEffect { damagePerTurn = damagePerTurn, turnsRemaining = effectiveDuration });
    }

    public void AddSpellDamageBonus(ElementType element, int bonus)
    {
        spellDamageBonuses[element] += bonus;
    }

    public int GetSpellDamage(ElementType element)
    {
        return baseSpellDamage + spellDamageBonuses[element];
    }

    public bool HasActiveDOT() => activeDOTs.Count > 0;

    // Called by BuffData.Apply()
    public void AddMaxHP(int amount)
    {
        maxHP     += amount;
        CurrentHP  = Mathf.Min(CurrentHP + amount, maxHP);
        OnStatsChanged?.Invoke();
    }

    public void AddDefenseBonus(int amount)   => DefenseBonus     += amount;
    public void AddDOTResistance(int amount)  => DOTResistance    += amount;
    public void AddCombatStartHeal(int amount)=> CombatStartHeal  += amount;
    public void AddGoldBonus(int amount)      => GoldBonusPerKill += amount;
}
