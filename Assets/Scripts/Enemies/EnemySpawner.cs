using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResistanceSpriteSet
{
    public Sprite idle;
    public Sprite move;
    public Sprite attack;
}

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance { get; private set; }

    public EnemyData[] basicEnemies; // regular enemy templates — assign in inspector
    public EnemyData   bossData;     // dragon final boss
    public GameObject  enemyPrefab;
    public Sprite      placeholderSprite; // assign a simple sprite for dungeon visibility

    [Header("Resistance Sprites (Non-Boss)")]
    [SerializeField] private ResistanceSpriteSet consonantResistantSprites;
    [SerializeField] private ResistanceSpriteSet assonantResistantSprites;
    [SerializeField] private ResistanceSpriteSet dissonantResistantSprites;

    [Header("Boss Sprites")]
    [SerializeField] private ResistanceSpriteSet bossSprites;

    [Header("Boss Resistance Tint")]
    [SerializeField] private bool bossUsesResistanceTint = true;
    [SerializeField] private Color consonantBossTint = new Color(0.65f, 0.80f, 1.00f, 1f);
    [SerializeField] private Color assonantBossTint = new Color(1.00f, 0.70f, 0.78f, 1f);
    [SerializeField] private Color dissonantBossTint = new Color(0.75f, 1.00f, 0.72f, 1f);

    [Header("Sprite Size Normalization")]
    [SerializeField] private bool normalizeSpriteSize = true;
    [SerializeField] private float targetSpriteWorldHeight = 2.0f;

    private List<EnemyInstance> activeEnemies = new();

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnEnemies(DungeonData data, int floor)
    {
        foreach (var e in activeEnemies) if (e) Destroy(e.gameObject);
        activeEnemies.Clear();

        bool isFinalFloor = floor >= FloorManager.MaxFloors;

        // Regular enemies
        foreach (var pos in data.EnemySpawns)
        {
            EnemyData d = isFinalFloor
                ? bossData
                : basicEnemies[Random.Range(0, basicEnemies.Length)];

            var go = Instantiate(enemyPrefab, transform);
            var ei = go.GetComponent<EnemyInstance>();
            ei.Init(d, pos);
            activeEnemies.Add(ei);

            // Add AI movement
            go.AddComponent<EnemyAI>();

            // Add placeholder sprite for dungeon visibility
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            ApplyEnemySprite(ei, sr);
        }
    }

    public void RefreshEnemyVisual(EnemyInstance enemy)
    {
        if (enemy == null) return;

        var sr = enemy.GetComponent<SpriteRenderer>();
        if (sr == null) sr = enemy.gameObject.AddComponent<SpriteRenderer>();
        ApplyEnemySprite(enemy, sr);
    }

    void ApplyEnemySprite(EnemyInstance enemy, SpriteRenderer sr)
    {
        if (enemy == null || sr == null) return;
        Sprite resolved = GetSpriteForEnemy(enemy, enemy.CurrentVisualState);
        if (resolved != null)
            sr.sprite = resolved;
        else if (sr.sprite == null && placeholderSprite != null)
            sr.sprite = placeholderSprite;

        sr.color = GetTintForEnemy(enemy);
        ApplySpriteScale(enemy, sr);
    }

    void ApplySpriteScale(EnemyInstance enemy, SpriteRenderer sr)
    {
        if (enemy == null || sr == null) return;

        if (!normalizeSpriteSize)
        {
            enemy.transform.localScale = enemy.BaseLocalScale;
            return;
        }

        if (sr.sprite == null) return;

        float spriteHeight = sr.sprite.bounds.size.y;
        if (spriteHeight <= 0.0001f) return;

        float clampedTargetHeight = Mathf.Max(0.01f, targetSpriteWorldHeight);
        float factor = clampedTargetHeight / spriteHeight;
        enemy.transform.localScale = enemy.BaseLocalScale * factor;
    }

    public Sprite GetSpriteForEnemy(EnemyInstance enemy, EnemyVisualState state)
    {
        if (enemy == null) return placeholderSprite;

        // Boss can use a dedicated state-based sprite set.
        if (enemy.data != null && enemy.data.isBoss)
        {
            Sprite bossStateSprite = state switch
            {
                EnemyVisualState.Move => bossSprites != null ? bossSprites.move : null,
                EnemyVisualState.Attack => bossSprites != null ? bossSprites.attack : null,
                _ => bossSprites != null ? bossSprites.idle : null,
            };

            if (bossStateSprite != null) return bossStateSprite;

            // Fallback for old data setups.
            return enemy.data.sprite != null ? enemy.data.sprite : placeholderSprite;
        }

        ResistanceSpriteSet set = GetResistanceSet(enemy.Resistance);
        if (set == null) return placeholderSprite;

        Sprite stateSprite = state switch
        {
            EnemyVisualState.Move => set.move,
            EnemyVisualState.Attack => set.attack,
            _ => set.idle,
        };

        // Fallback chain so missing state sprites still render.
        return stateSprite != null ? stateSprite : (set.idle != null ? set.idle : placeholderSprite);
    }

    Color GetTintForEnemy(EnemyInstance enemy)
    {
        if (enemy == null || enemy.data == null) return Color.white;

        if (!enemy.data.isBoss || !bossUsesResistanceTint)
            return Color.white;

        Color tint = enemy.Resistance switch
        {
            SpellType.Consonant => consonantBossTint,
            SpellType.Assonant => assonantBossTint,
            SpellType.Dissonant => dissonantBossTint,
            _ => Color.white,
        };

        // Safety net: avoid fully transparent tint values from hiding the sprite.
        if (tint.a <= 0.01f)
            tint.a = 1f;

        return tint;
    }

    ResistanceSpriteSet GetResistanceSet(SpellType resistance) => resistance switch
    {
        SpellType.Consonant => consonantResistantSprites,
        SpellType.Assonant => assonantResistantSprites,
        SpellType.Dissonant => dissonantResistantSprites,
        _ => null,
    };

    public EnemyInstance GetEnemyAt(Vector2Int pos)
        => activeEnemies.Find(e => e != null && e.GridPos == pos);

    public bool HasAnyActiveEnemy()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
                return true;
        }

        return false;
    }

    public void RemoveEnemy(EnemyInstance e)
    {
        activeEnemies.Remove(e);
        Destroy(e.gameObject);
    }
}
