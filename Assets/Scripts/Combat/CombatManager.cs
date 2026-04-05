using UnityEngine;

/// Real-time DOOM-style combat: no turn-based UI, continuous action
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Rewards")]
    [SerializeField] private int minGoldPerKill = 1;
    [SerializeField] private int maxGoldPerKill = 5;
    [SerializeField, Range(0f, 1f)] private float nonBossPotionDropChance = 0.10f;

    public event System.Action OnCombatEnd; // Invoked when enemy defeated

    private EnemyInstance currentCombatEnemy;
    private SpellType selectedSpell;
    private float enemyAttackCooldown = 0f;
    private const float AttackCooldownDuration = 2f;
    private bool bossPhase2Active = false;
    private float bossElementShiftCooldown = 0f;
    private const float BossShiftIntervalHigh = 3f;
    private const float BossShiftIntervalMid = 2f;
    private const float BossShiftIntervalLow = 1.5f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SongInputHandler.Instance.OnSuccess += HandleSpellSuccess;
        SongInputHandler.Instance.OnFail    += HandleSpellFail;
    }

    void Update()
    {
        // Spell hotkeys (for testing)
        if (Input.GetKeyDown(KeyCode.Q))
            CastSpell(SpellType.Consonant);
        if (Input.GetKeyDown(KeyCode.E))
            CastSpell(SpellType.Assonant);
        if (Input.GetKeyDown(KeyCode.R))
            CastSpell(SpellType.Dissonant);

        // Keep tracking nearest adjacent enemy
        UpdateCombatTarget();
        
        // Real-time enemy attacks on cooldown
        if (currentCombatEnemy != null)
        {
            enemyAttackCooldown -= Time.deltaTime;
            if (enemyAttackCooldown <= 0)
            {
                EnemyAttack();
                enemyAttackCooldown = AttackCooldownDuration;
            }

            if (bossPhase2Active && currentCombatEnemy.data != null && currentCombatEnemy.data.isBoss)
            {
                bossElementShiftCooldown -= Time.deltaTime;
                if (bossElementShiftCooldown <= 0f)
                {
                    SpellType next = GetRandomDifferentResistance(currentCombatEnemy.Resistance);
                    currentCombatEnemy.SetResistance(next);
                    EnemySpawner.Instance?.RefreshEnemyVisual(currentCombatEnemy);
                    if (AudioManager.Instance != null && currentCombatEnemy != null && currentCombatEnemy.data != null)
                        AudioManager.Instance.PlaySFX(currentCombatEnemy.data.elementShiftClip);
                    HUDController.Instance?.Log($"{currentCombatEnemy.DisplayName} shifts element to {next}!");
                    bossElementShiftCooldown = GetBossElementShiftInterval(currentCombatEnemy);
                }
            }
        }
    }

    void UpdateCombatTarget()
    {
        if (GridMover.Instance == null) return;

        Vector2Int playerPos = GridMover.Instance.GridPos;
        EnemyInstance nextTarget = null;

        // Find first adjacent enemy
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var dir in directions)
        {
            Vector2Int checkPos = playerPos + dir;
            EnemyInstance enemy = EnemySpawner.Instance.GetEnemyAt(checkPos);
            if (enemy != null)
            {
                nextTarget = enemy;
                break;
            }
        }

        if (nextTarget != currentCombatEnemy)
        {
            currentCombatEnemy = nextTarget;
            enemyAttackCooldown = AttackCooldownDuration;
            bossPhase2Active = false;
            bossElementShiftCooldown = 0f;

            if (CombatUIController.Instance != null)
            {
                if (currentCombatEnemy != null) CombatUIController.Instance.SetTargetEnemy(currentCombatEnemy);
                else CombatUIController.Instance.ClearTargetEnemy();
            }

            if (currentCombatEnemy != null)
            {
                Debug.Log($"[COMBAT START] Engaged with {currentCombatEnemy.DisplayName} (Resist: {currentCombatEnemy.Resistance})");
                HUDController.Instance?.Log($"You encounter {currentCombatEnemy.DisplayName}!");
            }
        }
    }

    public void CastSpell(SpellType spell)
    {
        Debug.Log($"[SPELL INITIATE] Player attempts to cast {spell}");
        
        // Find target (adjacent enemy or none)
        if (currentCombatEnemy == null)
        {
            Debug.LogWarning("[SPELL CAST] No adjacent enemy - spell cast but no target!");
        }
        else
        {
            Debug.Log($"[SPELL TARGET] {currentCombatEnemy.DisplayName}");
        }

        selectedSpell = spell;
        Debug.Log($"[SPELL INPUT SEQUENCE] Player begins {spell} sequence...");
        SongInputHandler.Instance.StartInput(spell);
    }


    void HandleSpellSuccess()
    {
        Debug.Log($"[SPELL SEQUENCE COMPLETE] {selectedSpell} successfully cast!");
        
        // If no enemy adjacent, spell fizzles harmlessly
        if (currentCombatEnemy == null)
        {
            Debug.Log("[SPELL RESULT] Cast with no target - spell dispersed harmlessly");
            return;
        }

        SpellHitResult hitResult = ElementSystem.GetHitResult(selectedSpell, currentCombatEnemy.Resistance);
        float multiplier = ElementSystem.GetDamageMultiplier(hitResult);
        int damage = PlayerStats.Instance.GetSongDamage(selectedSpell, multiplier);

        string resultText = hitResult switch
        {
            SpellHitResult.Extra => "RESONANT!",
            SpellHitResult.Reduced => "WEAK HIT!",
            SpellHitResult.Immune => "NO EFFECT!",
            _ => ""
        };

        Debug.Log($"[SPELL HIT] {selectedSpell} vs {currentCombatEnemy.Resistance} resistance: {resultText} (+{damage} dmg)");
        CombatUIController.Instance?.ShowSpellResult(resultText, damage);

        if (hitResult != SpellHitResult.Immune)
        {
            bool died = currentCombatEnemy.TakeDamage(damage);
            Debug.Log($"[DAMAGE] {currentCombatEnemy.DisplayName} HP: {currentCombatEnemy.CurrentHP}/{currentCombatEnemy.data.maxHP}");
            if (AudioManager.Instance != null && currentCombatEnemy != null && currentCombatEnemy.data != null)
                AudioManager.Instance.PlaySFX(currentCombatEnemy.data.attackClip);

            string hitMsg = hitResult == SpellHitResult.Extra
                ? $"Resonance! {selectedSpell} deals {damage} damage to {currentCombatEnemy.DisplayName}."
                : hitResult == SpellHitResult.Reduced
                    ? $"{selectedSpell} is resisted and deals {damage} damage to {currentCombatEnemy.DisplayName}."
                    : $"{selectedSpell} deals {damage} damage to {currentCombatEnemy.DisplayName}.";
            HUDController.Instance?.Log(hitMsg);

            if (currentCombatEnemy.data.isBoss &&
                currentCombatEnemy.CurrentHP <= currentCombatEnemy.data.maxHP / 2 &&
                currentCombatEnemy.data.secondPhaseElement != currentCombatEnemy.Resistance)
            {
                Debug.Log($"[BOSS PHASE 2] {currentCombatEnemy.DisplayName} transforms!");
                currentCombatEnemy.SetResistance(currentCombatEnemy.data.secondPhaseElement);
                EnemySpawner.Instance?.RefreshEnemyVisual(currentCombatEnemy);
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(currentCombatEnemy.data.elementShiftClip);
                HUDController.Instance?.Log($"{currentCombatEnemy.DisplayName} shifts element!");
                bossPhase2Active = true;
                bossElementShiftCooldown = GetBossElementShiftInterval(currentCombatEnemy);
            }

            if (died)
            {
                CombatUIController.Instance?.RegisterEnemyDefeat(currentCombatEnemy.DisplayName);

                bool wasBoss = currentCombatEnemy.data != null && currentCombatEnemy.data.isBoss;

                int rollMin = Mathf.Min(minGoldPerKill, maxGoldPerKill);
                int rollMax = Mathf.Max(minGoldPerKill, maxGoldPerKill);
                int goldReward = Random.Range(rollMin, rollMax + 1) + PlayerStats.Instance.GoldBonusPerKill;
                goldReward = Mathf.Max(1, goldReward);
                FloorManager.Instance?.AddGold(goldReward);

                if (!wasBoss && Random.value < nonBossPotionDropChance)
                {
                    PlayerStats.Instance?.AddPotionCharges(1);
                    HUDController.Instance?.Log("The enemy dropped a potion.");
                }

                Debug.Log($"[VICTORY] {currentCombatEnemy.DisplayName} defeated!");
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(currentCombatEnemy.data.deathClip);
                HUDController.Instance?.Log($"{currentCombatEnemy.DisplayName} defeated!");
                HUDController.Instance?.Log($"You found {goldReward} gold.");
                currentCombatEnemy.DropLoot();
                EnemySpawner.Instance.RemoveEnemy(currentCombatEnemy);
                currentCombatEnemy = null;
                bossPhase2Active = false;
                bossElementShiftCooldown = 0f;
                OnCombatEnd?.Invoke();
                return;
            }

            enemyAttackCooldown = 0.5f;
        }
        else
        {
            CombatUIController.Instance?.RegisterResistanceDiscovery(currentCombatEnemy.DisplayName, currentCombatEnemy.Resistance);
            HUDController.Instance?.Log($"{selectedSpell} has no effect on {currentCombatEnemy.DisplayName}.");
        }

    }

    void HandleSpellFail()
    {
        Debug.LogWarning($"[SPELL FAIL] Wrong sequence for {selectedSpell}!");
        HUDController.Instance?.Log($"Wrong sequence for {selectedSpell}!");
        CombatUIController.Instance?.ShowSpellMiss();
        enemyAttackCooldown = 0f;
    }

    void EnemyAttack()
    {
        if (currentCombatEnemy == null) return;

        currentCombatEnemy.TriggerAttackVisual();
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(currentCombatEnemy.data != null ? currentCombatEnemy.data.attackClip : null);

        int dmg = currentCombatEnemy.data.attackDamage;
        if (PlayerStats.Instance.HasEarplugs) dmg = Mathf.Max(1, dmg - 2);
        
        bool dead = PlayerStats.Instance.TakeDamage(dmg);
        Debug.Log($"[ENEMY ATTACK] {currentCombatEnemy.DisplayName} attacks! -{dmg} HP (Player HP: {PlayerStats.Instance.CurrentHP})");
        HUDController.Instance?.Log($"{currentCombatEnemy.DisplayName} hits you for {dmg} damage.");
        CombatUIController.Instance?.ShowPlayerHit(dmg);

        if (dead)
        {
            Debug.LogError("[GAME OVER] Player defeated!");
            GameManager.Instance.GameOver();
        }
    }

    public EnemyInstance GetCurrentEnemy() => currentCombatEnemy;

    static SpellType GetRandomDifferentResistance(SpellType current)
    {
        SpellType[] all = { SpellType.Consonant, SpellType.Assonant, SpellType.Dissonant };
        SpellType first = all[0] == current ? all[1] : all[0];
        SpellType second = all[2] == current ? all[1] : all[2];
        return Random.value < 0.5f ? first : second;
    }

    static float GetBossElementShiftInterval(EnemyInstance boss)
    {
        if (boss == null || boss.MaxHP <= 0) return BossShiftIntervalHigh;

        float hpRatio = (float)boss.CurrentHP / boss.MaxHP;
        if (hpRatio <= 0.2f) return BossShiftIntervalLow;
        if (hpRatio <= 0.4f) return BossShiftIntervalMid;
        return BossShiftIntervalHigh;
    }
}

