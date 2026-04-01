using UnityEngine;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    public event System.Action<EnemyInstance> OnCombatStart;
    public event System.Action                OnCombatEnd;

    private EnemyInstance currentEnemy;
    private Element       selectedSong;
    private const int     BaseDamage = 10;

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
        GameManager.Instance.SetState(GameState.InCombat);
        OnCombatStart?.Invoke(enemy);
    }

    // Called by CombatUI when player presses a song button
    public void SelectSong(Element song)
    {
        if (!PlayerStats.Instance.SpendMana()) { HandleFail(); return; }
        selectedSong = song;
        SongInputHandler.Instance.StartInput(song);
    }

    void HandleSuccess()
    {
        int multiplier = ElementSystem.GetMultiplier(selectedSong, currentEnemy.data.element);
        int damage     = PlayerStats.Instance.GetSongDamage(selectedSong, multiplier);

        if (multiplier > 0)
        {
            bool died = currentEnemy.TakeDamage(damage);
            // Boss phase switch at half HP
            if (currentEnemy.data.isBoss &&
                currentEnemy.CurrentHP <= currentEnemy.data.maxHP / 2 &&
                currentEnemy.data.secondPhaseElement != currentEnemy.data.element)
            {
                currentEnemy.data.element = currentEnemy.data.secondPhaseElement;
                CombatUIController.Instance.ShowPhaseChange();
            }
            if (died) { EnemySpawner.Instance.RemoveEnemy(currentEnemy); EndCombat(); return; }
        }

        PlayerStats.Instance.RegenerateMana();
        CombatUIController.Instance.ShowResult(multiplier, damage);

        if (multiplier != 2) EnemyAttacks();
    }

    void HandleFail()
    {
        PlayerStats.Instance.RegenerateMana();
        CombatUIController.Instance.ShowFailFeedback();
        EnemyAttacks();
    }

    void EnemyAttacks()
    {
        int dmg = currentEnemy.data.attackDamage;
        if (PlayerStats.Instance.HasEarplugs) dmg = Mathf.Max(1, dmg - 2);
        bool dead = PlayerStats.Instance.TakeDamage(dmg);
        if (dead) { GameManager.Instance.GameOver(); return; }
        CombatUIController.Instance.ShowNewTurn();
    }

    public void EndCombat()
    {
        GameManager.Instance.SetState(GameState.Exploring);
        OnCombatEnd?.Invoke();
    }

    public void AttemptFlee()
    {
        if (UnityEngine.Random.value < 0.4f) EndCombat();
        else EnemyAttacks();
    }
}
