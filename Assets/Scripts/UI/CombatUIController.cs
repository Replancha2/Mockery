using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIController : MonoBehaviour
{
    public static CombatUIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject combatPanel;

    [Header("Enemy Info")]
    public Image            enemySprite;
    public TextMeshProUGUI  enemyNameText;
    public Slider           enemyHPSlider;
    public TextMeshProUGUI  elementLabel;

    [Header("Sequence Display")]
    public Image[]          keyIcons;    // 6 icons
    public Sprite[]         dirSprites;  // index: 0=Up 1=Down 2=Left 3=Right
    public TextMeshProUGUI  counterLabel;

    [Header("Timer & Feedback")]
    public Slider           timerBar;
    public TextMeshProUGUI  resultText;

    [Header("Buttons")]
    public GameObject       songButtonsGroup;

    private EnemyInstance activeEnemy;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        combatPanel.SetActive(false);
    }

    void Start()
    {
        CombatManager.Instance.OnCombatStart += ShowCombat;
        CombatManager.Instance.OnCombatEnd   += HideCombat;
        SongInputHandler.Instance.OnKeyCorrect += UpdateSequenceDisplay;
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.InCombat) return;
        timerBar.value = SongInputHandler.Instance.TimeRemaining
                       / FloorManager.Instance.GetInputTimeLimit();
    }

    void ShowCombat(EnemyInstance enemy)
    {
        activeEnemy            = enemy;
        combatPanel.SetActive(true);
        enemySprite.sprite     = enemy.data.sprite;
        enemyNameText.text     = enemy.data.enemyName;
        enemyHPSlider.maxValue = enemy.data.maxHP;
        enemyHPSlider.value    = enemy.CurrentHP;
        elementLabel.text      = $"ELEMENT: {enemy.data.element}";
        resultText.text        = "";
        songButtonsGroup.SetActive(true);
    }

    void HideCombat() => combatPanel.SetActive(false);

    // Wired to each song button's OnClick in the inspector — pass 0/1/2/3
    public void OnSongSelected(int elementIndex)
    {
        Element e         = (Element)elementIndex;
        string counterName = ElementSystem.GetCounter(activeEnemy.data.element).ToString();
        counterLabel.text  = $"COUNTER: {counterName} | CASTING: {e}";
        BuildSequenceDisplay(e);
        songButtonsGroup.SetActive(false);
        CombatManager.Instance.SelectSong(e);
    }

    void BuildSequenceDisplay(Element e)
    {
        var seq = SongInputHandler.Songs[e];
        for (int i = 0; i < keyIcons.Length; i++)
        {
            keyIcons[i].sprite = dirSprites[(int)seq[i]];
            keyIcons[i].color  = Color.gray;
        }
    }

    void UpdateSequenceDisplay(int newIndex)
    {
        for (int i = 0; i < newIndex; i++)
            keyIcons[i].color = Color.white;
    }

    public void ShowResult(int multiplier, int damage)
    {
        resultText.text = multiplier switch
        {
            2 => $"RESONANT!  -{damage} HP",
            1 => $"HIT!  -{damage} HP",
            0 => "NO EFFECT...",
            _ => ""
        };
        enemyHPSlider.value = activeEnemy.CurrentHP;
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowFailFeedback()
    {
        resultText.text = "WRONG KEYS!";
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowNewTurn()
    {
        enemyHPSlider.value = activeEnemy != null ? activeEnemy.CurrentHP : 0;
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowPhaseChange()
    {
        resultText.text   = "— SECOND PHASE! —";
        elementLabel.text = $"ELEMENT: {activeEnemy.data.element}";
    }

    void ReEnableSongButtons()
    {
        resultText.text = "";
        songButtonsGroup.SetActive(true);
    }
}
