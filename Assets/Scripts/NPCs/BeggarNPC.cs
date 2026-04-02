using UnityEngine;

public class BeggarNPC : MonoBehaviour
{
    public static BeggarNPC Instance { get; private set; }

    [Header("Outcome Weights (must sum to 1)")]
    [Range(0f, 1f)] public float chanceGood    = 0.40f; // gives buff or item
    [Range(0f, 1f)] public float chanceNothing = 0.30f; // walks away
    // remaining probability = stab

    [Header("Stab")]
    [Range(0f, 1f)] public float stabHPPercent = 0.35f; // fraction of maxHP lost

    [Header("Good Reward Pool")]
    public BuffData[] buffPool;
    public ItemData[] itemPool;

    public Vector2Int GridPos          { get; private set; }
    public bool       AlreadyUsed      { get; private set; } = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitForFloor(Vector2Int pos)
    {
        GridPos    = pos;
        AlreadyUsed = false;
    }

    public void Interact()
    {
        if (AlreadyUsed) return;

        // Prompt player through UI — for now, auto-interact if player has gold
        if (FloorManager.Instance.Gold < 1) return;

        AlreadyUsed = true;
        FloorManager.Instance.SpendGold(1);

        float roll = Random.value;

        if (roll < chanceGood)
        {
            GiveReward();
        }
        else if (roll < chanceGood + chanceNothing)
        {
            BeggarUI.Instance?.ShowResult(BeggarOutcome.Nothing);
        }
        else
        {
            Stab();
        }
    }

    void GiveReward()
    {
        bool giveBuff = Random.value > 0.5f && buffPool.Length > 0;

        if (giveBuff)
        {
            BuffData buff = buffPool[Random.Range(0, buffPool.Length)];
            FloorManager.Instance.ApplyBuff(buff);
            BeggarUI.Instance?.ShowResult(BeggarOutcome.Buff, buff.buffName);
        }
        else if (itemPool.Length > 0)
        {
            ItemData item = itemPool[Random.Range(0, itemPool.Length)];
            var mover = FindFirstObjectByType<GridMover>();
            ItemSpawner.Instance.SpawnItemAt(item, mover.GetGridPos());
            BeggarUI.Instance?.ShowResult(BeggarOutcome.Item, item.itemName);
        }
    }

    void Stab()
    {
        int damage = Mathf.RoundToInt(PlayerStats.Instance.maxHP * stabHPPercent);
        PlayerStats.Instance.TakeDamage(damage);
        BeggarUI.Instance?.ShowResult(BeggarOutcome.Stab);
        if (PlayerStats.Instance.IsDead) GameManager.Instance.GameOver();
    }
}

public enum BeggarOutcome { Buff, Item, Nothing, Stab }
