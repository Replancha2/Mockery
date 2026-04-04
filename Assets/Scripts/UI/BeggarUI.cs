using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeggarUI : MonoBehaviour
{
    public static BeggarUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Choice phase")]
    public GameObject choiceGroup;   // parent of giveButton + refuseButton
    public Button     giveButton;
    public Button     refuseButton;

    [Header("Result phase")]
    public GameObject      resultGroup;  // parent of messageText + closeButton
    public TextMeshProUGUI messageText;
    public Button          closeButton;

    private BeggarNPC _currentBeggar;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    void Start()
    {
        giveButton.onClick.AddListener(OnGive);
        refuseButton.onClick.AddListener(Hide);
        closeButton.onClick.AddListener(Hide);
    }

    void Update()
    {
        if (!panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();
        if (Input.GetMouseButtonDown(0))
        {
            if (choiceGroup.activeSelf)
            {
                if (HitsButton(giveButton))   OnGive();
                if (HitsButton(refuseButton)) Hide();
            }
            else if (resultGroup.activeSelf && HitsButton(closeButton))
                Hide();
        }
    }

    bool HitsButton(Button btn)
    {
        var rt = btn.GetComponent<RectTransform>();
        return RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
    }

    public void Open(BeggarNPC beggar)
    {
        _currentBeggar = beggar;
        GameManager.Instance.SetState(GameState.Shopping);
        panel.SetActive(true);

        if (FloorManager.Instance.Gold >= 1)
        {
            choiceGroup.SetActive(true);
            resultGroup.SetActive(false);
        }
        else
        {
            ShowResult(_currentBeggar.ResolveStab());
        }
    }

    void OnGive()
    {
        ShowResult(_currentBeggar.ResolveGive());
    }

    void ShowResult(BeggarResult result)
    {
        choiceGroup.SetActive(false);
        resultGroup.SetActive(true);

        messageText.text = result.outcome switch
        {
            BeggarOutcome.Buff    => $"The beggar gives you: {result.rewardName}!",
            BeggarOutcome.Item    => $"The beggar throws you: {result.rewardName}!",
            BeggarOutcome.Nothing => "The beggar ignores you and leaves.",
            BeggarOutcome.Stab    => "The beggar stabs you!",
            _                    => ""
        };
    }

    public void Hide()
    {
        _currentBeggar = null;
        panel.SetActive(false);
        GameManager.Instance.SetState(GameState.Exploring);
    }
}
