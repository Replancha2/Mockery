using UnityEngine;

public struct BeggarResult
{
    public BeggarOutcome outcome;
    public string        rewardName;
}

public class BeggarNPC : MonoBehaviour
{
    [Header("Outcome Weights")]
    [Range(0f, 1f)] public float chanceGood    = 0.40f;
    [Range(0f, 1f)] public float chanceNothing = 0.30f;

    [Header("Stab")]
    [Range(0f, 1f)] public float stabHPPercent = 0.35f;
    public AudioClip stabClip;

    [Header("Good Reward Pool")]
    public BuffData[] buffPool;
    public ItemData[] itemPool;

    [Header("Visual")]
    [SerializeField] private float spawnY = 1.337f;

    public Vector2Int GridPos     { get; private set; }
    public bool       AlreadyUsed { get; private set; } = false;

    public void InitForFloor(Vector2Int pos)
    {
        GridPos = pos;
        transform.position = GridToWorld(pos);
        AlreadyUsed = false;
    }

    private Vector3 GridToWorld(Vector2Int pos)
        => new Vector3(pos.x * 4f, spawnY, pos.y * 4f);

    public void Interact()
    {
        if (AlreadyUsed) return;
        BeggarUI.Instance?.Open(this);
    }

    // Called by BeggarUI when player chooses to give gold
    public BeggarResult ResolveGive()
    {
        AlreadyUsed = true;
        FloorManager.Instance.SpendGold(1);

        float roll = Random.value;

        if (roll < chanceGood)
            return GiveReward();

        if (roll < chanceGood + chanceNothing)
            return new BeggarResult { outcome = BeggarOutcome.Nothing };

        return ResolveStab();
    }

    // Called by BeggarUI when player has no gold — auto stab
    public BeggarResult ResolveStab()
    {
        AlreadyUsed = true;
        AudioManager.Instance?.PlaySFX(stabClip);
        int damage = Mathf.RoundToInt(PlayerStats.Instance.maxHP * stabHPPercent);
        PlayerStats.Instance.TakeDamage(damage);
        if (PlayerStats.Instance.CurrentHP <= 0) GameManager.Instance.GameOver();
        return new BeggarResult { outcome = BeggarOutcome.Stab };
    }

    BeggarResult GiveReward()
    {
        bool giveBuff = Random.value > 0.5f && buffPool.Length > 0;

        if (giveBuff)
        {
            BuffData buff = buffPool[Random.Range(0, buffPool.Length)];
            FloorManager.Instance.ApplyBuff(buff);
            return new BeggarResult { outcome = BeggarOutcome.Buff, rewardName = buff.buffName };
        }

        if (itemPool.Length > 0)
        {
            ItemData item = itemPool[Random.Range(0, itemPool.Length)];
            var result = PlayerInventory.Instance.AcquireItem(item);
            if (result == PlayerInventory.ItemAcquireResult.StoredInBackpack)
                HUDController.Instance?.Log($"Backpack: {item.itemName}");
            else
                HUDController.Instance?.Log($"Equipped {item.itemName} ({item.slot}).");
            return new BeggarResult { outcome = BeggarOutcome.Item, rewardName = item.itemName };
        }

        return new BeggarResult { outcome = BeggarOutcome.Nothing };
    }
}

public enum BeggarOutcome { Buff, Item, Nothing, Stab }
