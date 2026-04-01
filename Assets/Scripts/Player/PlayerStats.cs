using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    public int maxHP        = 30;
    public int maxMana      = 20;
    public int songManaCost = 3;

    public int CurrentHP   { get; private set; }
    public int CurrentMana { get; private set; }

    public bool    HasEarplugs  { get; private set; }
    public Element BonusElement { get; private set; } = (Element)(-1);
    public bool    HasSheetMusic => (int)BonusElement >= 0;

    public event System.Action OnStatsChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start() { CurrentHP = maxHP; CurrentMana = maxMana; }

    public bool TakeDamage(int amount)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        OnStatsChanged?.Invoke();
        return CurrentHP <= 0;
    }

    public void RestoreHP(int amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnStatsChanged?.Invoke();
    }

    public void RestoreMana(int amount)
    {
        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        OnStatsChanged?.Invoke();
    }

    public bool SpendMana()
    {
        if (CurrentMana < songManaCost) return false;
        CurrentMana -= songManaCost;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void RegenerateMana()
    {
        CurrentMana = Mathf.Min(maxMana, CurrentMana + 2);
        OnStatsChanged?.Invoke();
    }

    public void ApplyEarplugs()            { HasEarplugs = true;   OnStatsChanged?.Invoke(); }
    public void ApplySheetMusic(Element e) { BonusElement = e;     OnStatsChanged?.Invoke(); }

    public int GetSongDamage(Element song, int multiplier)
    {
        int bonus = (HasSheetMusic && song == BonusElement) ? 5 : 0;
        return (10 + bonus) * multiplier;
    }
}
