public enum SpellType { Consonant, Assonant, Dissonant }

public static class ElementSystem
{
    // Cycle: Consonant > Dissonant > Assonant > Consonant
    public static bool Beats(SpellType attacker, SpellType defender) =>
        (attacker == SpellType.Consonant && defender == SpellType.Dissonant) ||
        (attacker == SpellType.Dissonant  && defender == SpellType.Assonant) ||
        (attacker == SpellType.Assonant  && defender == SpellType.Consonant);

    public static SpellType GetCounter(SpellType s) => s switch
    {
        SpellType.Consonant => SpellType.Assonant,
        SpellType.Assonant  => SpellType.Dissonant,
        SpellType.Dissonant => SpellType.Consonant,
        _                  => SpellType.Consonant
    };

    // 2 = super effective | 1 = neutral | 0 = not effective
    public static int GetMultiplier(SpellType attack, SpellType defense)
    {
        if (Beats(attack, defense)) return 2;
        if (Beats(defense, attack)) return 0;
        return 1;
    }
}
