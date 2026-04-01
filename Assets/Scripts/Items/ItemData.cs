using UnityEngine;

public enum ItemType { HealthPotion, LuteString, SheetMusic, Earplugs }

[CreateAssetMenu(menuName = "DCJam/ItemData")]
public class ItemData : ScriptableObject
{
    public string   itemName;
    public ItemType type;
    public Element  elementBonus;  // only used if type == SheetMusic
    public Sprite   icon;
    public int      restoreAmount; // for HealthPotion and LuteString
}
