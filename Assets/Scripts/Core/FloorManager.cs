using UnityEngine;
using System.Collections.Generic;

public class FloorManager : MonoBehaviour
{
    public static FloorManager Instance { get; private set; }

    public event System.Action<int> OnGoldChanged;

    public int CurrentFloor { get; private set; } = 1;
    public int Gold         { get; private set; } = 0;
    public const int MaxFloors = 5;

    public List<BuffData> ActiveBuffs { get; private set; } = new();

    // Timer window shrinks per floor: 6s -> 3s across 5 floors
    public float GetInputTimeLimit() => Mathf.Max(3f, 6f - (CurrentFloor - 1) * 0.5f);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetRun()
    {
        CurrentFloor = 1;
        Gold         = 0;
        ActiveBuffs.Clear();
        OnGoldChanged?.Invoke(Gold);
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
    }

    public bool SpendGold(int amount)
    {
        if (Gold < amount) return false;
        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        return true;
    }

    public void ApplyBuff(BuffData buff)
    {
        ActiveBuffs.Add(buff);
        buff.Apply();
    }

    public void NextFloor()
    {
        CurrentFloor++;
        if (CurrentFloor > MaxFloors)
        {
            HUDController.Instance?.Log("You escaped the dungeon. Victory!");
            GameManager.Instance.Victory();
        }
        else
        {
            HUDController.Instance?.Log($"You descend to floor {CurrentFloor}.");
            GameManager.Instance.SetState(GameState.BuffSelection);
        }
    }

    public void ApplyStartOfFloorEffects()
    {
        int healAmount = PlayerStats.Instance.GetFloorHealAmount();
        PlayerStats.Instance.RestoreHP(healAmount);
        HUDController.Instance?.Log($"You recover {healAmount} HP.");
    }
}
