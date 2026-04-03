using UnityEngine;

public enum EquipmentSlot { Head, Chest, Legs, Feet, Feather }

[CreateAssetMenu(menuName = "Mockery/ItemData")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    public ItemType type;
    public SpellType elementBonus;  // only used if type == SheetMusic
    public Sprite   icon;
    public int      restoreAmount; // for HealthPotion and LuteString
}
