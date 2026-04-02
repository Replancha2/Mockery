using UnityEngine;
using TMPro;

// Simple feedback panel shown after interacting with the Beggar
public class BeggarUI : MonoBehaviour
{
    public static BeggarUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject       panel;
    public TextMeshProUGUI  messageText;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowResult(BeggarOutcome outcome, string rewardName = "")
    {
        messageText.text = outcome switch
        {
            BeggarOutcome.Buff    => $"El monea te da: {rewardName}!",
            BeggarOutcome.Item    => $"El monea te lanza: {rewardName}!",
            BeggarOutcome.Nothing => "El monea te ignora y se va.",
            BeggarOutcome.Stab    => "¡El monea te apuñala!",
            _                    => ""
        };

        panel.SetActive(true);
        Invoke(nameof(Hide), 2f);
    }

    void Hide() => panel.SetActive(false);
}
