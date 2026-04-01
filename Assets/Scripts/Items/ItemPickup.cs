using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData   data;
    public Vector2Int GridPos;

    public void Collect()
    {
        switch (data.type)
        {
            case ItemType.HealthPotion: PlayerStats.Instance.RestoreHP(data.restoreAmount);     break;
            case ItemType.LuteString:  PlayerStats.Instance.RestoreMana(data.restoreAmount);    break;
            case ItemType.SheetMusic:  PlayerStats.Instance.ApplySheetMusic(data.elementBonus); break;
            case ItemType.Earplugs:    PlayerStats.Instance.ApplyEarplugs();                    break;
        }
        ItemSpawner.Instance.RemoveItem(this);
        Destroy(gameObject);
    }
}
