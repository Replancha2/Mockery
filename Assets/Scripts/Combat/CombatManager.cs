using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public event System.Action<EnemyInstance> OnCombatStart;
    public event System.Action                OnCombatEnd;

    private EnemyInstance currentEnemy;
    private ElementType   selectedSong;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        SongInputHandler.Instance.OnSuccess += HandleSuccess;
        SongInputHandler.Instance.OnFail    += HandleFail;
    }

    public void StartCombat(EnemyInstance enemy)
    {
        currentEnemy = enemy;
        OnCombatStart?.Invoke(enemy);
        GameManager.Instance.SetState(GameState.InCombat);
        StartPlayerTurn();
    }

    // Called by CombatUI when player selects a spell
    public void SelectSong(ElementType song)
    {
        selectedSong = song;
        SongInputHandler.Instance.StartInput(song);
    }

    void StartPlayerTurn()
    {
        // Tick DOTs at the start of the player's turn
        int dotDamage = PlayerStats.Instance.TickDOTs();
        if (PlayerStats.Instance.IsDead) { GameManager.Instance.GameOver(); return; }

        CombatUIController.Instance.ShowNewTurn(dotDamage);
    }

    void HandleSuccess()
    {
        ResistanceTier tier   = currentEnemy.Resistance.Get(selectedSong);
        float          mult   = ElementSystem.GetMultiplier(tier);
        int            damage = Mathf.RoundToInt(PlayerStats.Instance.GetSpellDamage(selectedSong) * mult);

        bool died = damage > 0 && currentEnemy.TakeDamage(damage);
        CombatUIController.Instance.ShowResult(tier, damage);

        if (died)
        {
            currentEnemy.DropLoot();
            EnemySpawner.Instance.RemoveEnemy(currentEnemy);
            EndCombat();
            return;
        }

        // Enemy only counter-attacks if hit was not super-effective
        if (tier != ResistanceTier.Weak)
            EnemyTurn();
        else
            StartPlayerTurn();
    }

    void HandleFail()
    {
        CombatUIController.Instance.ShowFailFeedback();
        EnemyTurn();
    }

    void EnemyTurn()
    {
        EnemyInstance.AbilityType ability = currentEnemy.Act();
        CombatUIController.Instance.ShowEnemyAction(ability, currentEnemy);

        if (PlayerStats.Instance.IsDead) { GameManager.Instance.GameOver(); return; }

        StartPlayerTurn();
    }

    public void EndCombat()
    {
        GameManager.Instance.SetState(GameState.Exploring);
        OnCombatEnd?.Invoke();
    }

    public void AttemptFlee()
    {
        if (Random.value < 0.4f) EndCombat();
        else EnemyTurn();
    }
}
