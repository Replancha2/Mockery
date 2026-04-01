using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Bars")]
    public Slider          hpBar;
    public Slider          manaBar;

    [Header("Labels")]
    public TextMeshProUGUI floorLabel;

    void Start()
    {
        PlayerStats.Instance.OnStatsChanged += Refresh;
        GameManager.Instance.OnStateChanged += OnStateChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance) PlayerStats.Instance.OnStatsChanged -= Refresh;
        if (GameManager.Instance) GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    void OnStateChanged(GameState state)
    {
        floorLabel.text = $"Floor {FloorManager.Instance.CurrentFloor} / {FloorManager.MaxFloors}";
    }

    void Refresh()
    {
        hpBar.maxValue   = PlayerStats.Instance.maxHP;
        hpBar.value      = PlayerStats.Instance.CurrentHP;
        manaBar.maxValue = PlayerStats.Instance.maxMana;
        manaBar.value    = PlayerStats.Instance.CurrentMana;
    }
}
