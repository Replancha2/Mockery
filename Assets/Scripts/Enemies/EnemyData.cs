using UnityEngine;

[CreateAssetMenu(menuName = "Mockery/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public Sprite sprite;            // shown on combat screen
    public GameObject vfxPrefab;     // particle in dungeon

    [Header("Stats")]
    public int maxHP        = 20;
    public int attackDamage = 5;
    public bool isBoss      = false;
    public bool isMini      = false; // mini-boss: HP/damage scaled at runtime

    [Header("Ability Weights (must sum > 0)")]
    [Range(0, 1)] public float healWeight       = 0.2f;
    [Range(0, 1)] public float dotWeight        = 0.4f;
    [Range(0, 1)] public float powerStrikeWeight = 0.4f;

    [Header("Heal")]
    [Range(0f, 1f)] public float healPercent = 0.2f; // fraction of maxHP restored

    [Header("DOT")]
    public int   dotDamagePerTurn = 3;
    public int   dotDuration      = 3;

    [Header("Power Strike")]
    [Range(1f, 3f)] public float powerStrikeMultiplier = 1.5f;

    [Header("Loot")]
    public int  goldMin       = 1;
    public int  goldMax       = 5;
    [Range(0f, 1f)] public float itemDropChance = 0.25f;
}
