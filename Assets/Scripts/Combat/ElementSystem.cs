public enum Element { Fire, Water, Earth, Wind }

public static class ElementSystem
{
    // Cycle: Fire > Wind > Earth > Water > Fire
    public static bool Beats(Element attacker, Element defender) =>
        (attacker == Element.Fire  && defender == Element.Wind)  ||
        (attacker == Element.Wind  && defender == Element.Earth) ||
        (attacker == Element.Earth && defender == Element.Water) ||
        (attacker == Element.Water && defender == Element.Fire);

    public static Element GetCounter(Element e) => e switch
    {
        Element.Fire  => Element.Water,
        Element.Water => Element.Wind,
        Element.Wind  => Element.Earth,
        Element.Earth => Element.Fire,
        _             => Element.Fire
    };

    // 2 = super effective | 1 = neutral | 0 = not effective
    public static int GetMultiplier(Element attack, Element defense)
    {
        if (Beats(attack, defense)) return 2;
        if (Beats(defense, attack)) return 0;
        return 1;
    }
}
