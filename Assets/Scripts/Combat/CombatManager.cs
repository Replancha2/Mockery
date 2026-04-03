using UnityEngine;

/// Real-time DOOM-style combat: no turn-based UI, continuous action
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

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
            if (currentCombatEnemy != null)
                Debug.Log($"[COMBAT END] Lost sight of {currentCombatEnemy.data.enemyName}");
            
            currentCombatEnemy = nextTarget;
            enemyAttackCooldown = AttackCooldownDuration;
            
            if (currentCombatEnemy != null)
                Debug.Log($"[COMBAT START] Engaged with {currentCombatEnemy.data.enemyName} ({currentCombatEnemy.data.element})");
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

        int multiplier = ElementSystem.GetMultiplier(selectedSpell, currentCombatEnemy.data.element);
        int damage = PlayerStats.Instance.GetSongDamage(selectedSpell, multiplier);

        string resultText = multiplier switch
        {
            2 => "RESONANT!",
            1 => "HIT!",
            0 => "NO EFFECT!",
            _ => ""
        };

        Debug.Log($"[SPELL HIT] {selectedSpell} vs {currentCombatEnemy.data.element}: {resultText} (+{damage} dmg)");

        if (multiplier > 0)
        {
            bool died = currentCombatEnemy.TakeDamage(damage);
            Debug.Log($"[DAMAGE] {currentCombatEnemy.data.enemyName} HP: {currentCombatEnemy.CurrentHP}/{currentCombatEnemy.data.maxHP}");

            if (currentCombatEnemy.data.isBoss &&
                currentCombatEnemy.CurrentHP <= currentCombatEnemy.data.maxHP / 2 &&
                currentCombatEnemy.data.secondPhaseElement != currentCombatEnemy.data.element)
            {
                Debug.Log($"[BOSS PHASE 2] {currentCombatEnemy.data.enemyName} transforms!");
                currentCombatEnemy.data.element = currentCombatEnemy.data.secondPhaseElement;
            }

            if (died)
            {
                Debug.Log($"[VICTORY] {currentCombatEnemy.data.enemyName} defeated!");
                EnemySpawner.Instance.RemoveEnemy(currentCombatEnemy);
                currentCombatEnemy = null;
                OnCombatEnd?.Invoke();
                return;
            }

            enemyAttackCooldown = 0.5f;
        }

    }

    void HandleSpellFail()
    {
        Debug.LogWarning($"[SPELL FAIL] Wrong sequence for {selectedSpell}!");
        enemyAttackCooldown = 0f;
    }

    void EnemyAttack()
    {
        if (currentCombatEnemy == null) return;

        int dmg = currentCombatEnemy.data.attackDamage;
        if (PlayerStats.Instance.HasEarplugs) dmg = Mathf.Max(1, dmg - 2);
        
        bool dead = PlayerStats.Instance.TakeDamage(dmg);
        Debug.Log($"[ENEMY ATTACK] {currentCombatEnemy.data.enemyName} attacks! -{dmg} HP (Player HP: {PlayerStats.Instance.CurrentHP})");
        
        if (dead)
        {
            Debug.LogError("[GAME OVER] Player defeated!");
            GameManager.Instance.GameOver();
        }
    }

    public EnemyInstance GetCurrentEnemy() => currentCombatEnemy;
}

