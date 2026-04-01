using UnityEngine;

[CreateAssetMenu(menuName = "DCJam/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string enemyName;
    public Element element;
    public Element secondPhaseElement; // boss only
    public int maxHP;
    public int attackDamage;
    public bool isBoss;
    public Sprite sprite;               // shown on combat screen
    public GameObject elementVFXPrefab; // colored particle in dungeon
}
