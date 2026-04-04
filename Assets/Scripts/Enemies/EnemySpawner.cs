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
        sr.sprite = GetSpriteForEnemy(enemy, enemy.CurrentVisualState) ?? placeholderSprite;
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

        // Boss keeps its own single sprite.
        if (enemy.data != null && enemy.data.isBoss)
            return enemy.data.sprite != null ? enemy.data.sprite : placeholderSprite;

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
