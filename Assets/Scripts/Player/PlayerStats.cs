using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP        = 30;
    public int maxMana      = 20;
    public int songManaCost = 3;

    public int CurrentHP   { get; private set; }
    public int CurrentMana { get; private set; }

    public bool      HasEarplugs  { get; private set; }
    public SpellType BonusElement { get; private set; } = (SpellType)(-1);
    public bool      HasSheetMusic => (int)BonusElement >= 0;

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

    public void ApplyEarplugs()                      { HasEarplugs = true;   OnStatsChanged?.Invoke(); }
    public void ApplySheetMusic(SpellType e)          { BonusElement = e;     OnStatsChanged?.Invoke(); }

    public int GetSongDamage(SpellType song, int multiplier)
    {
        int bonus = (HasSheetMusic && song == BonusElement) ? 5 : 0;
        return (10 + bonus) * multiplier;
    }
}
