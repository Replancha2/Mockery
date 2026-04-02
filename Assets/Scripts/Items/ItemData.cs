using UnityEngine;

public enum EquipmentSlot { Head, Chest, Legs, Feet, Feather }

[CreateAssetMenu(menuName = "Mockery/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string        itemName;
    public EquipmentSlot slot;
    public Sprite        icon;

    [Header("Stat Bonuses")]
    public int hpBonus       = 0;
    public int defenseBonus  = 0;
    public int asonanteBonus    = 0;
    public int discordanteBonus = 0;
    public int consonanteBonus  = 0;

    [Header("Shop")]
    public int buyPrice = 5;
}
