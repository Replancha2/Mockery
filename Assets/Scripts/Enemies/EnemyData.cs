using UnityEngine;

[CreateAssetMenu(menuName = "Mockery/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public SpellType element;
    public SpellType secondPhaseElement; // boss only
    public int maxHP;
    public int attackDamage;
    public bool isBoss;
    public Sprite sprite;               // shown on combat screen
    public GameObject elementVFXPrefab; // colored particle in dungeon

    [Header("Loot")]
    [Range(0f, 1f)] public float itemDropChance;
    public ItemData[] itemPool;
}
