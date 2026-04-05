using UnityEngine;
using System.Collections;

public enum EnemyVisualState { Idle, Move, Attack }

public class EnemyInstance : MonoBehaviour
{
    public EnemyData      data;
    public int             CurrentHP   { get; private set; }
    public int             MaxHP       { get; private set; }
    public Vector2Int      GridPos     { get; private set; }
    public SpellType       Resistance  { get; private set; }
    public string DisplayName { get; private set; }
    public EnemyVisualState CurrentVisualState { get; private set; } = EnemyVisualState.Idle;
    public Vector3 BaseLocalScale { get; private set; } = Vector3.one;

    [Header("Hit Feedback")]
    [SerializeField] private float hitShakeDuration = 0.10f;
    [SerializeField] private float hitShakeStrength = 0.15f;
    [SerializeField] private float hitShakeFrequency = 0.02f;

    private Coroutine _hitShakeRoutine;
    private Coroutine _visualStateRoutine;
    private Coroutine _ambientSoundRoutine;
    private SpriteRenderer _spriteRenderer;

    public void Init(EnemyData d, Vector2Int pos)
    {
        data      = d;
        MaxHP     = d.maxHP;
        CurrentHP = MaxHP;
        GridPos   = pos;
        Resistance = (d != null && d.isBoss)
            ? GetRandomResistanceExcluding(d.secondPhaseElement)
            : (SpellType)Random.Range(0, 3);
        DisplayName = ResolveDisplayName();
        BaseLocalScale = transform.localScale;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = GridMover.GridToWorld(pos);
        SetVisualState(EnemyVisualState.Idle);
        if (d != null && d.isBoss)
            Debug.Log($"[BOSS INIT] {DisplayName} starts with {Resistance} (phase 2: {d.secondPhaseElement}).");

        if (_ambientSoundRoutine != null)
            StopCoroutine(_ambientSoundRoutine);
        _ambientSoundRoutine = StartCoroutine(RandomAmbientSoundRoutine());
        // if (d.vfxPrefab) Instantiate(d.vfxPrefab, transform);
    }

    public void SetResistance(SpellType newResistance)
    {
        Resistance = newResistance;
        DisplayName = ResolveDisplayName();
        RefreshVisual();
    }

    public static string GetNameForResistance(SpellType resistance)
    {
        return resistance switch
        {
            SpellType.Consonant => "Waton",
            SpellType.Assonant => "Imp",
            SpellType.Dissonant => "Demon",
            _ => "Unknown"
        };
    }

    string ResolveDisplayName()
    {
        if (data != null && data.isBoss)
        {
            if (!string.IsNullOrWhiteSpace(data.enemyName))
                return data.enemyName;
            return "Dragon";
        }

        return GetNameForResistance(Resistance);
    }

    static SpellType GetRandomResistanceExcluding(SpellType excluded)
    {
        SpellType[] all = { SpellType.Consonant, SpellType.Assonant, SpellType.Dissonant };
        int excludedIndex = -1;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == excluded)
            {
                excludedIndex = i;
                break;
            }
        }

        int roll = Random.Range(0, all.Length - 1);
        if (excludedIndex >= 0 && roll >= excludedIndex)
            roll++;

        return all[Mathf.Clamp(roll, 0, all.Length - 1)];
    }

    public void SetGridPos(Vector2Int pos)
    {
        GridPos = pos;
        transform.position = GridMover.GridToWorld(pos);
        SetVisualState(EnemyVisualState.Move, 0.12f);
    }

    // Returns true if enemy died
    public bool TakeDamage(int amount)
    {
        TriggerHitShake();
        CurrentHP -= amount;
        return CurrentHP <= 0;
    }

    public void TriggerAttackVisual(float duration = 0.18f)
    {
        SetVisualState(EnemyVisualState.Attack, duration);
    }

    public void SetVisualState(EnemyVisualState state, float autoReturnToIdleAfterSeconds = 0f)
    {
        CurrentVisualState = state;
        RefreshVisual();

        if (_visualStateRoutine != null)
            StopCoroutine(_visualStateRoutine);

        if (autoReturnToIdleAfterSeconds > 0f)
            _visualStateRoutine = StartCoroutine(ReturnToIdleRoutine(autoReturnToIdleAfterSeconds));
    }

    public void RefreshVisual()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null || EnemySpawner.Instance == null) return;
        EnemySpawner.Instance.RefreshEnemyVisual(this);
    }

    IEnumerator ReturnToIdleRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        CurrentVisualState = EnemyVisualState.Idle;
        RefreshVisual();
        _visualStateRoutine = null;
    }

    void TriggerHitShake()
    {
        if (_hitShakeRoutine != null)
            StopCoroutine(_hitShakeRoutine);

        _hitShakeRoutine = StartCoroutine(HitShakeRoutine());
    }

    IEnumerator HitShakeRoutine()
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < hitShakeDuration)
        {
            Vector2 offset2D = Random.insideUnitCircle * hitShakeStrength;
            transform.position = startPos + new Vector3(offset2D.x, 0f, offset2D.y);
            elapsed += hitShakeFrequency;
            yield return new WaitForSeconds(hitShakeFrequency);
        }

        transform.position = startPos;
        _hitShakeRoutine = null;
    }

    IEnumerator RandomAmbientSoundRoutine()
    {
        while (true)
        {
            float delay = Random.Range(2.5f, 7.0f);
            yield return new WaitForSeconds(delay);

            if (data == null || data.ambientClips == null || data.ambientClips.Length == 0)
                continue;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Exploring)
                continue;

            if (AudioManager.Instance != null)
            {
                AudioClip clip = data.ambientClips[Random.Range(0, data.ambientClips.Length)];
                AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, 1f, 1f);
            }
        }
    }

    void OnDestroy()
    {
        if (_ambientSoundRoutine != null)
            StopCoroutine(_ambientSoundRoutine);
    }

    public enum AbilityType { Heal, DOT, PowerStrike }

    // Enemy chooses and executes an ability this turn
    // public AbilityType Act()
    // {
    //     float total  = data.healWeight + data.dotWeight + data.powerStrikeWeight;
    //     float roll   = Random.Range(0f, total);

    //     AbilityType chosen;
    //     if (roll < data.healWeight)
    //         chosen = AbilityType.Heal;
    //     else if (roll < data.healWeight + data.dotWeight)
    //         chosen = AbilityType.DOT;
    //     else
    //         chosen = AbilityType.PowerStrike;

    //     switch (chosen)
    //     {
    //         case AbilityType.Heal:
    //             int healAmount = Mathf.RoundToInt(MaxHP * data.healPercent);
    //             CurrentHP = Mathf.Min(MaxHP, CurrentHP + healAmount);
    //             break;

    //         case AbilityType.DOT:
    //             PlayerStats.Instance.ApplyDOT(data.dotDamagePerTurn, data.dotDuration);
    //             break;

    //         case AbilityType.PowerStrike:
    //             int dmg = data.isMini
    //                 ? Mathf.RoundToInt(data.attackDamage * 2f * data.powerStrikeMultiplier)
    //                 : Mathf.RoundToInt(data.attackDamage * data.powerStrikeMultiplier);
    //             PlayerStats.Instance.TakeDamage(dmg);
    //             break;
    //     }

    //     return chosen;
    // }

    // // Normal attack (used when enemy doesn't use a special ability)
    // public void BasicAttack()
    // {
    //     int dmg = data.isMini ? data.attackDamage * 2 : data.attackDamage;
    //     PlayerStats.Instance.TakeDamage(dmg);
    // }

    public void DropLoot()
    {
        if (data.itemPool == null || data.itemPool.Length == 0) return;
        if (Random.value > data.itemDropChance) return;

        ItemData item = data.itemPool[Random.Range(0, data.itemPool.Length)];
        var result = PlayerInventory.Instance.AcquireItem(item);
        if (result == PlayerInventory.ItemAcquireResult.StoredInBackpack)
            HUDController.Instance?.Log($"Backpack: {item.itemName}");
        else if (result == PlayerInventory.ItemAcquireResult.Swapped)
            HUDController.Instance?.Log($"Equipped {item.itemName}, replaced previous {item.slot} item.");
        else
            HUDController.Instance?.Log($"Equipped {item.itemName} ({item.slot}).");
    }
}
