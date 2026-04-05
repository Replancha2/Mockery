using UnityEngine;
using System.Collections.Generic;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP         = 30;
    public int baseSpellDamage = 10;
    public int startingPotionCharges = 3;

    public int CurrentHP { get; private set; }
    public int PotionCharges { get; private set; }

    public int DefenseBonus          { get; private set; }
    public int FloorHealBonusPercent { get; private set; }
    public int GoldBonusPerKill      { get; private set; }
    public int PotionHealBonusPercent { get; private set; }
    public int VendorDiscountPercent  { get; private set; }

    public bool      HasEarplugs  { get; private set; }
    public SpellType BonusElement { get; private set; } = (SpellType)(-1);
    public bool      HasSheetMusic => (int)BonusElement >= 0;

    private Dictionary<SpellType, int> spellDamageBonuses;

    public event System.Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        spellDamageBonuses = new Dictionary<SpellType, int>
        {
            { SpellType.Consonant, 0 },
            { SpellType.Assonant,  0 },
            { SpellType.Dissonant, 0 },
        };

        // Initialize in Awake so any early gameplay callback can't see zero HP.
        CurrentHP = maxHP;
        PotionCharges = startingPotionCharges;
    }

    void Start()
    {
        if (CurrentHP <= 0)
            CurrentHP = maxHP;
        if (PotionCharges <= 0)
            PotionCharges = startingPotionCharges;
    }

    public void ResetForNewRun()
    {
        CurrentHP = maxHP;
        foreach (var key in new List<SpellType>(spellDamageBonuses.Keys))
            spellDamageBonuses[key] = 0;
        DefenseBonus          = 0;
        FloorHealBonusPercent = 0;
        GoldBonusPerKill      = 0;
        PotionHealBonusPercent = 0;
        VendorDiscountPercent  = 0;
        PotionCharges = startingPotionCharges;
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
        maxHP = Mathf.Max(1, maxHP + amount);
        CurrentHP = Mathf.Clamp(CurrentHP + amount, 0, maxHP);
        OnStatsChanged?.Invoke();
    }

    public void AddDefenseBonus(int amount)          { DefenseBonus           += amount; OnStatsChanged?.Invoke(); }
    public void AddFloorHealBonusPercent(int amount) { FloorHealBonusPercent  += amount; OnStatsChanged?.Invoke(); }
    public void AddGoldBonus(int amount)             { GoldBonusPerKill       += amount; OnStatsChanged?.Invoke(); }
    public void AddPotionHealBonusPercent(int amount){ PotionHealBonusPercent += amount; OnStatsChanged?.Invoke(); }
    public void AddVendorDiscountPercent(int amount) { VendorDiscountPercent  += amount; OnStatsChanged?.Invoke(); }

    public int GetDiscountedPrice(int basePrice)
    {
        if (VendorDiscountPercent <= 0) return basePrice;
        float multiplier = Mathf.Clamp01(1f - VendorDiscountPercent / 100f);
        return Mathf.Max(1, Mathf.RoundToInt(basePrice * multiplier));
    }

    public bool TryUsePotion()
    {
        if (PotionCharges <= 0) return false;
        if (CurrentHP >= maxHP) return false;

        int healAmount = Mathf.Max(1, Mathf.CeilToInt(maxHP * (0.25f + PotionHealBonusPercent / 100f)));
        PotionCharges--;
        CurrentHP = Mathf.Min(maxHP, CurrentHP + healAmount);
        OnStatsChanged?.Invoke();
        return true;
    }

    public void AddPotionCharges(int amount)
    {
        PotionCharges = Mathf.Max(0, PotionCharges + amount);
        OnStatsChanged?.Invoke();
    }

    public int GetFloorHealAmount()
    {
        float totalPercent = 0.25f + (FloorHealBonusPercent / 100f);
        int amount = Mathf.CeilToInt(maxHP * totalPercent);
        return Mathf.Max(1, amount);
    }

    public void ApplyEarplugs()               { HasEarplugs  = true; OnStatsChanged?.Invoke(); }
    public void ApplySheetMusic(SpellType e)   { BonusElement = e;    OnStatsChanged?.Invoke(); }

    public int GetSongDamage(SpellType song, float multiplier)
    {
        int bonus = (HasSheetMusic && song == BonusElement) ? 5 : 0;
        int rawDamage = baseSpellDamage + bonus + spellDamageBonuses[song];
        if (multiplier <= 0f) return 0;
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * multiplier));
    }
}
