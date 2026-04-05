using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI floorText;
    public Button          tryAgainButton;
    public Button          quitButton;

    void Start()
    {
        bool isVictory = GameManager.Instance != null &&
                         GameManager.Instance.CurrentState == GameState.Victory;

        messageText.text = isVictory ? "CONGRATULATIONS" : "GAME OVER";

        if (isVictory)
        {
            floorText.gameObject.SetActive(false);
        }
        else
        {
            int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
            floorText.text = $"You reached floor {floor}";
        }

        tryAgainButton.onClick.AddListener(() => GameManager.Instance.StartNewRun());
        quitButton.onClick.AddListener(() => Application.Quit());
    }
}
