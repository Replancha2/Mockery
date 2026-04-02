using System.Collections.Generic;
using UnityEngine;

public enum ElementType { Asonante, Discordante, Consonante }
public enum ResistanceTier { Immune, Normal, Weak }

public static class ElementSystem
{
    // Cycle: Discordante > Consonante > Asonante > Discordante
    public static ElementType GetCounter(ElementType e) => e switch
    {
        ElementType.Asonante    => ElementType.Consonante,
        ElementType.Discordante => ElementType.Asonante,
        ElementType.Consonante  => ElementType.Discordante,
        _                       => e
    };

    // 0 = immune | 1 = normal | 2 = weak
    public static float GetMultiplier(ResistanceTier tier) => tier switch
    {
        ResistanceTier.Immune => 0f,
        ResistanceTier.Normal => 1f,
        ResistanceTier.Weak   => 2f,
        _                     => 1f
    };
}

// Randomly assigns one Immune / Normal / Weak per element (each tier used exactly once)
public class EnemyResistance
{
    public Dictionary<ElementType, ResistanceTier> Tiers = new();

    public EnemyResistance()
    {
        var elements = new List<ElementType>
            { ElementType.Asonante, ElementType.Discordante, ElementType.Consonante };
        var tiers = new List<ResistanceTier>
            { ResistanceTier.Immune, ResistanceTier.Normal, ResistanceTier.Weak };

        for (int i = tiers.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (tiers[i], tiers[j]) = (tiers[j], tiers[i]);
        }

        for (int i = 0; i < elements.Count; i++)
            Tiers[elements[i]] = tiers[i];
    }

    public ResistanceTier Get(ElementType e) => Tiers[e];
}
