using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP         = 30;
    public int baseSpellDamage = 10;

    public int CurrentHP { get; private set; }

    public int DefenseBonus     { get; private set; }
    public int CombatStartHeal  { get; private set; }
    public int GoldBonusPerKill { get; private set; }

    public bool      HasEarplugs  { get; private set; }
    public SpellType BonusElement { get; private set; } = (SpellType)(-1);
    public bool      HasSheetMusic => (int)BonusElement >= 0;

    private Dictionary<SpellType, int> spellDamageBonuses;

    public event System.Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        spellDamageBonuses = new Dictionary<SpellType, int>
        {
            { SpellType.Consonant, 0 },
            { SpellType.Assonant,  0 },
            { SpellType.Dissonant, 0 },
        };
    }

    void Start()
    {
        CurrentHP = maxHP;
    }

    public void ResetForNewRun()
    {
        CurrentHP = maxHP;
        foreach (var key in new List<SpellType>(spellDamageBonuses.Keys))
            spellDamageBonuses[key] = 0;
        DefenseBonus     = 0;
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

    public void AddSpellDamageBonus(SpellType element, int bonus)
    {
        spellDamageBonuses[element] += bonus;
    }

    public int GetSpellDamage(SpellType element)
    {
        return baseSpellDamage + spellDamageBonuses[element];
    }

    // Called by BuffData.Apply()
    public void AddMaxHP(int amount)
    {
        maxHP     += amount;
        CurrentHP  = Mathf.Min(CurrentHP + amount, maxHP);
        OnStatsChanged?.Invoke();
    }

    public void AddDefenseBonus(int amount)    { DefenseBonus     += amount; OnStatsChanged?.Invoke(); }
    public void AddCombatStartHeal(int amount) { CombatStartHeal  += amount; OnStatsChanged?.Invoke(); }
    public void AddGoldBonus(int amount)       { GoldBonusPerKill += amount; OnStatsChanged?.Invoke(); }

    public void ApplyEarplugs()               { HasEarplugs  = true; OnStatsChanged?.Invoke(); }
    public void ApplySheetMusic(SpellType e)   { BonusElement = e;    OnStatsChanged?.Invoke(); }

    public int GetSongDamage(SpellType song, int multiplier)
    {
        int bonus = (HasSheetMusic && song == BonusElement) ? 5 : 0;
        return (baseSpellDamage + bonus + spellDamageBonuses[song]) * multiplier;
    }
}
