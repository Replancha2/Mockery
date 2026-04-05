using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialUI : MonoBehaviour
{
    [System.Serializable]
    public struct TutorialPage
    {
        public string title;
        [TextArea(4, 10)]
        public string body;
        public Sprite illustration; // optional
    }

    [Header("Pages")]
    public TutorialPage[] pages;

    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public Image           illustrationImage;
    public GameObject      illustrationContainer;
    public Button          prevButton;
    public Button          nextButton;
    public Button          skipButton;
    public TextMeshProUGUI pageIndicatorText;
    public TextMeshProUGUI nextButtonLabel;

    private int current = 0;

    void Start()
    {
        prevButton.onClick.AddListener(Prev);
        nextButton.onClick.AddListener(Next);
        skipButton.onClick.AddListener(StartGame);
        ShowPage(0);
    }

    void ShowPage(int index)
    {
        current = index;
        var page = pages[index];

        titleText.text = page.title;
        bodyText.text  = page.body;

        if (pageIndicatorText != null)
            pageIndicatorText.text = $"{index + 1} / {pages.Length}";

        bool hasImage = page.illustration != null;
        if (illustrationContainer != null) illustrationContainer.SetActive(hasImage);
        if (hasImage && illustrationImage != null) illustrationImage.sprite = page.illustration;

        prevButton.interactable = index > 0;

        bool isLast = index >= pages.Length - 1;
        if (nextButtonLabel != null)
            nextButtonLabel.text = isLast ? "Start!" : "Next >";
    }

    void Next()
    {
        if (current >= pages.Length - 1) StartGame();
        else ShowPage(current + 1);
    }

    void Prev() => ShowPage(current - 1);

    void StartGame() => GameManager.Instance.StartNewRun();
}
