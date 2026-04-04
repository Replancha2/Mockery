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
    public EnemyVisualState CurrentVisualState { get; private set; } = EnemyVisualState.Idle;
    public Vector3 BaseLocalScale { get; private set; } = Vector3.one;

    [Header("Hit Feedback")]
    [SerializeField] private float hitShakeDuration = 0.10f;
    [SerializeField] private float hitShakeStrength = 0.15f;
    [SerializeField] private float hitShakeFrequency = 0.02f;

    private Coroutine _hitShakeRoutine;
    private Coroutine _visualStateRoutine;
    private SpriteRenderer _spriteRenderer;

    public void Init(EnemyData d, Vector2Int pos)
    {
        data      = d;
        MaxHP     = d.maxHP;
        CurrentHP = MaxHP;
        GridPos   = pos;
        Resistance = (SpellType)Random.Range(0, 3);
        BaseLocalScale = transform.localScale;
        _spriteRenderer = GetComponent<SpriteRenderer>();

        transform.position = GridMover.GridToWorld(pos);
        SetVisualState(EnemyVisualState.Idle);
        // if (d.vfxPrefab) Instantiate(d.vfxPrefab, transform);
    }

    public void SetResistance(SpellType newResistance)
    {
        Resistance = newResistance;
        RefreshVisual();
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

    // public void DropLoot()
    // {
    //     int gold = Random.Range(data.goldMin, data.goldMax + 1);
    //     FloorManager.Instance.AddGold(gold);

    //     if (Random.value < data.itemDropChance)
    //         ItemSpawner.Instance.SpawnRandomItemAt(GridPos);
    // }
}
