using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI floorText;
    public Button          tryAgainButton;

    void Start()
    {
        bool isVictory = GameManager.Instance != null &&
                         GameManager.Instance.CurrentState == GameState.Victory;

        messageText.text = isVictory ? "THE DUNGEON IS PURGED" : "YOU FELL";

        int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
        floorText.text   = isVictory ? "All 5 floors conquered!" : $"Reached floor {floor}";

        tryAgainButton.onClick.AddListener(() => GameManager.Instance.StartNewRun());
    }
}
