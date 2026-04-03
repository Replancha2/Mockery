using UnityEngine;

[CreateAssetMenu(menuName = "DCJam/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public SpellType element;
    public SpellType secondPhaseElement; // boss only
    public int maxHP;
    public int attackDamage;
    public bool isBoss;
    public Sprite sprite;               // shown on combat screen
    public GameObject elementVFXPrefab; // colored particle in dungeon
}
