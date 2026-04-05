using UnityEngine;

public enum EquipmentSlot { Head, Chest, Legs, Feet, Feather }

[CreateAssetMenu(menuName = "Mockery/ItemData")]
public class ItemData : ScriptableObject
{
    public string        itemName;
    public EquipmentSlot slot;
    public SpellType     elementBonus;  // only used if type == SheetMusic
    public Sprite        icon;
    public int           restoreAmount; // for HealthPotion and LuteString

    public int           buyPrice;

    [Header("Stat Bonuses")]
    public int hpBonus;
    public int defenseBonus;
    public int AssonantBonus;
    public int DissonantBonus;
    public int ConsonantBonus;

    public int TotalStats() => hpBonus + defenseBonus + AssonantBonus + DissonantBonus + ConsonantBonus;
}
