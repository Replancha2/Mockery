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
        // Intentionally using manual click hit-testing in Update for reliability.
    }

    void Update()
    {
        if (!panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            Hide();

        if (!Input.GetMouseButtonDown(0)) return;

        if (choiceGroup.activeSelf)
        {
            if (HitsButton(giveButton))
            {
                OnGive();
                return;
            }

            if (HitsButton(refuseButton))
            {
                Hide();
                return;
            }
        }
        else if (resultGroup.activeSelf && HitsButton(closeButton))
        {
            Hide();
            return;
        }
    }

    bool HitsButton(Button btn)
    {
        if (btn == null) return false;
        var rt = btn.GetComponent<RectTransform>();
        return rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null);
    }

    public void Open(BeggarNPC beggar)
    {
        _currentBeggar = beggar;
        GameManager.Instance.SetState(GameState.Shopping);
        ResetUIState();

        if (messageText != null)
            messageText.text = "Spare a coin?";

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

    void ResetUIState()
    {
        if (messageText != null)
            messageText.text = string.Empty;

        if (choiceGroup != null)
            choiceGroup.SetActive(false);

        if (resultGroup != null)
            resultGroup.SetActive(false);
    }

    void OnGive()
    {   
        if (_currentBeggar == null) return;
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
        ResetUIState();
        _currentBeggar = null;
        panel.SetActive(false);
        GameManager.Instance.SetState(GameState.Exploring);
    }
}
