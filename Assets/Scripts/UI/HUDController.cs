using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Bars")]
    public Slider hpBar;

    [Header("Labels")]
    public TextMeshProUGUI floorLabel;
    public TextMeshProUGUI goldLabel;

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
        floorLabel.text = $"Piso {FloorManager.Instance.CurrentFloor} / {FloorManager.MaxFloors}";
        goldLabel.text  = $"Oro: {FloorManager.Instance.Gold}";
    }

    void Refresh()
    {
        hpBar.maxValue = PlayerStats.Instance.maxHP;
        hpBar.value    = PlayerStats.Instance.CurrentHP;
        goldLabel.text = $"Oro: {FloorManager.Instance.Gold}";

    }
}
