using UnityEngine;

public enum BuffEffectType
{
    HPBonus,             // raises maxHP and heals the difference
    SpellDamageBonus,    // +value to one SpellType's base damage
    DefenseBonus,        // flat damage reduction on all incoming hits
    HealOnCombatStart,   // restores value HP at the start of every combat
    GoldBonus,           // +value gold per enemy kill
}

[CreateAssetMenu(menuName = "Mockery/BuffData")]
public class BuffData : ScriptableObject
{
    public string       buffName;
    [TextArea] public string description;
    public BuffEffectType effectType;
    public SpellType  element; // only used for SpellDamageBonus
    public int          value;

    //Applies this buff's permanent effect to the player
    public void Apply()
    {
        switch (effectType)
        {
            case BuffEffectType.HPBonus:
                PlayerStats.Instance.AddMaxHP(value);
                break;
            case BuffEffectType.SpellDamageBonus:
                PlayerStats.Instance.AddSpellDamageBonus(element, value);
                break;
            case BuffEffectType.DefenseBonus:
                PlayerStats.Instance.AddDefenseBonus(value);
                break;
            case BuffEffectType.HealOnCombatStart:
                PlayerStats.Instance.AddCombatStartHeal(value);
                break;
            case BuffEffectType.GoldBonus:
                PlayerStats.Instance.AddGoldBonus(value);
                break;
        }
    }
}
