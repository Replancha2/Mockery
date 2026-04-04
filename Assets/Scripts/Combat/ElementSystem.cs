public enum SpellType { Consonant, Assonant, Dissonant }

public enum SpellHitResult { Extra, Reduced, Immune }

public static class ElementSystem
{
    // Resistance matrix:
    // Dissonant resistance: Extra from Assonant, Immune to Dissonant, Reduced from Consonant
    // Consonant resistance: Extra from Dissonant, Immune to Consonant, Reduced from Assonant
    // Assonant resistance: Extra from Consonant, Immune to Assonant, Reduced from Dissonant
    public static SpellHitResult GetHitResult(SpellType attack, SpellType resistance)
    {
        if (attack == resistance) return SpellHitResult.Immune;
        if (attack == GetStrongAgainstResistance(resistance)) return SpellHitResult.Extra;
        return SpellHitResult.Reduced;
    }

    public static float GetDamageMultiplier(SpellHitResult result) => result switch
    {
        SpellHitResult.Extra   => 2f,
        SpellHitResult.Reduced => 0.5f,
        SpellHitResult.Immune  => 0f,
        _                     => 1f,
    };

    public static SpellType GetStrongAgainstResistance(SpellType resistance) => resistance switch
    {
        SpellType.Dissonant => SpellType.Assonant,
        SpellType.Consonant => SpellType.Dissonant,
        SpellType.Assonant  => SpellType.Consonant,
        _                  => SpellType.Consonant,
    };
}