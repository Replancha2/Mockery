using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameOverUI : MonoBehaviour
{
    [Header("Outcome Images")]
    public Image loseImage;
    public Image victoryImage;

    [Header("Texts")]
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI floorText;

    [Header("Victory Animation")]
    public Sprite[] victoryFrames;
    [Range(1f, 30f)] public float victoryFps = 12f;
    public bool loopVictoryAnimation = true;

    [Header("Buttons")]
    public Button          tryAgainButton;
    public Button          quitButton;

    private Coroutine _victoryAnimRoutine;

    void Start()
    {
        bool isVictory = GameManager.Instance != null &&
                         GameManager.Instance.CurrentState == GameState.Victory;

        if (loseImage != null)
            loseImage.gameObject.SetActive(!isVictory);
        if (victoryImage != null)
            victoryImage.gameObject.SetActive(isVictory);

        if (isVictory)
            StartVictoryAnimation();

        if (messageText != null)
            messageText.text = isVictory ? "CONGRATULATIONS" : "GAME OVER";

        if (isVictory)
        {
            if (floorText != null)
                floorText.gameObject.SetActive(false);
        }
        else
        {
            int floor = FloorManager.Instance != null ? FloorManager.Instance.CurrentFloor : 1;
            if (floorText != null)
                floorText.text = $"You reached floor {floor}";
        }

        if (tryAgainButton != null)
            tryAgainButton.onClick.AddListener(() => GameManager.Instance.StartNewRun());
        if (quitButton != null)
            quitButton.onClick.AddListener(() => Application.Quit());
    }

    void OnDisable()
    {
        if (_victoryAnimRoutine != null)
        {
            StopCoroutine(_victoryAnimRoutine);
            _victoryAnimRoutine = null;
        }
    }

    void StartVictoryAnimation()
    {
        if (victoryImage == null || victoryFrames == null || victoryFrames.Length == 0)
            return;

        if (_victoryAnimRoutine != null)
            StopCoroutine(_victoryAnimRoutine);

        _victoryAnimRoutine = StartCoroutine(VictoryAnimationRoutine());
    }

    IEnumerator VictoryAnimationRoutine()
    {
        float frameDelay = 1f / Mathf.Max(1f, victoryFps);

        do
        {
            for (int i = 0; i < victoryFrames.Length; i++)
            {
                if (victoryFrames[i] != null)
                    victoryImage.sprite = victoryFrames[i];

                yield return new WaitForSeconds(frameDelay);
            }
        }
        while (loopVictoryAnimation);

        _victoryAnimRoutine = null;
    }
}
