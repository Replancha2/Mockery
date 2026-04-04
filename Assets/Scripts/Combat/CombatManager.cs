using UnityEngine;

/// Real-time DOOM-style combat: no turn-based UI, continuous action
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Rewards")]
    [SerializeField] private int minGoldPerKill = 1;
    [SerializeField] private int maxGoldPerKill = 5;

    public event System.Action OnCombatEnd; // Invoked when enemy defeated

    private EnemyInstance currentCombatEnemy;
    private SpellType selectedSpell;
    private float enemyAttackCooldown = 0f;
    private const float AttackCooldownDuration = 2f;

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

            if (currentCombatEnemy != null)
            {
                Debug.Log($"[COMBAT START] Engaged with {currentCombatEnemy.data.enemyName} (Resist: {currentCombatEnemy.Resistance})");
                HUDController.Instance?.Log($"You encounter {currentCombatEnemy.data.enemyName}!");
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
            Debug.Log($"[SPELL TARGET] {currentCombatEnemy.data.enemyName}");
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

        if (hitResult != SpellHitResult.Immune)
        {
            bool died = currentCombatEnemy.TakeDamage(damage);
            Debug.Log($"[DAMAGE] {currentCombatEnemy.data.enemyName} HP: {currentCombatEnemy.CurrentHP}/{currentCombatEnemy.data.maxHP}");

            string hitMsg = hitResult == SpellHitResult.Extra
                ? $"Resonance! {selectedSpell} deals {damage} damage to {currentCombatEnemy.data.enemyName}."
                : hitResult == SpellHitResult.Reduced
                    ? $"{selectedSpell} is resisted and deals {damage} damage to {currentCombatEnemy.data.enemyName}."
                    : $"{selectedSpell} deals {damage} damage to {currentCombatEnemy.data.enemyName}.";
            HUDController.Instance?.Log(hitMsg);

            if (currentCombatEnemy.data.isBoss &&
                currentCombatEnemy.CurrentHP <= currentCombatEnemy.data.maxHP / 2 &&
                currentCombatEnemy.data.secondPhaseElement != currentCombatEnemy.Resistance)
            {
                Debug.Log($"[BOSS PHASE 2] {currentCombatEnemy.data.enemyName} transforms!");
                currentCombatEnemy.SetResistance(currentCombatEnemy.data.secondPhaseElement);
                EnemySpawner.Instance?.RefreshEnemyVisual(currentCombatEnemy);
                HUDController.Instance?.Log($"{currentCombatEnemy.data.enemyName} shifts element!");
            }

            if (died)
            {
                int rollMin = Mathf.Min(minGoldPerKill, maxGoldPerKill);
                int rollMax = Mathf.Max(minGoldPerKill, maxGoldPerKill);
                int goldReward = Random.Range(rollMin, rollMax + 1) + PlayerStats.Instance.GoldBonusPerKill;
                goldReward = Mathf.Max(1, goldReward);
                FloorManager.Instance?.AddGold(goldReward);

                Debug.Log($"[VICTORY] {currentCombatEnemy.data.enemyName} defeated!");
                HUDController.Instance?.Log($"{currentCombatEnemy.data.enemyName} defeated!");
                HUDController.Instance?.Log($"You found {goldReward} gold.");
                EnemySpawner.Instance.RemoveEnemy(currentCombatEnemy);
                currentCombatEnemy = null;
                OnCombatEnd?.Invoke();
                return;
            }

            enemyAttackCooldown = 0.5f;
        }
        else
        {
            HUDController.Instance?.Log($"{selectedSpell} has no effect on {currentCombatEnemy.data.enemyName}.");
        }

    }

    void HandleSpellFail()
    {
        Debug.LogWarning($"[SPELL FAIL] Wrong sequence for {selectedSpell}!");
        HUDController.Instance?.Log($"Wrong sequence for {selectedSpell}!");
        enemyAttackCooldown = 0f;
    }

    void EnemyAttack()
    {
        if (currentCombatEnemy == null) return;

        currentCombatEnemy.TriggerAttackVisual();

        int dmg = currentCombatEnemy.data.attackDamage;
        if (PlayerStats.Instance.HasEarplugs) dmg = Mathf.Max(1, dmg - 2);
        
        bool dead = PlayerStats.Instance.TakeDamage(dmg);
        Debug.Log($"[ENEMY ATTACK] {currentCombatEnemy.data.enemyName} attacks! -{dmg} HP (Player HP: {PlayerStats.Instance.CurrentHP})");
        HUDController.Instance?.Log($"{currentCombatEnemy.data.enemyName} hits you for {dmg} damage.");

        if (dead)
        {
            Debug.LogError("[GAME OVER] Player defeated!");
            GameManager.Instance.GameOver();
        }
    }

    public EnemyInstance GetCurrentEnemy() => currentCombatEnemy;
}

