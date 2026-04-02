using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatUIController : MonoBehaviour
{
    public static CombatUIController Instance { get; private set; }

    [Header("Panels")]
    public GameObject combatPanel;

    [Header("Enemy Info")]
    public Image           enemySprite;
    public TextMeshProUGUI enemyNameText;
    public Slider          enemyHPSlider;

    [Header("Resistance Display (3 labels: Asonante / Discordante / Consonante)")]
    public TextMeshProUGUI[] resistanceLabels; // index 0=Asonante 1=Discordante 2=Consonante

    [Header("Sequence Display (4 icons)")]
    public Image[]         keyIcons;   // 4 icons
    public Sprite[]        dirSprites; // index: 0=Up 1=Down 2=Left 3=Right

    [Header("Timer & Feedback")]
    public Slider          timerBar;
    public TextMeshProUGUI resultText;

    [Header("Spell Buttons")]
    public GameObject      songButtonsGroup;

    private EnemyInstance activeEnemy;

    // Tracks which resistances the player has already discovered this combat
    private Dictionary<ElementType, ResistanceTier> revealed = new();

    private static readonly ElementType[] allElements =
        { ElementType.Asonante, ElementType.Discordante, ElementType.Consonante };

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
        activeEnemy = enemy;
        revealed.Clear();

        combatPanel.SetActive(true);
        enemySprite.sprite     = enemy.data.sprite;
        enemyNameText.text     = enemy.data.enemyName;
        enemyHPSlider.maxValue = enemy.MaxHP;
        enemyHPSlider.value    = enemy.CurrentHP;
        resultText.text        = "";

        // Apply combat-start heal if player has that buff
        if (PlayerStats.Instance.CombatStartHeal > 0)
            PlayerStats.Instance.RestoreHP(PlayerStats.Instance.CombatStartHeal);

        RefreshResistanceLabels();
        songButtonsGroup.SetActive(true);
    }

    void HideCombat() => combatPanel.SetActive(false);

    // Wired to each spell button's OnClick in inspector — pass 0=Asonante 1=Discordante 2=Consonante
    public void OnSongSelected(int elementIndex)
    {
        ElementType e = (ElementType)elementIndex;
        BuildSequenceDisplay(e);
        songButtonsGroup.SetActive(false);
        CombatManager.Instance.SelectSong(e);
    }

    void BuildSequenceDisplay(ElementType e)
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
        for (int i = 0; i < newIndex && i < keyIcons.Length; i++)
            keyIcons[i].color = Color.white;
    }

    // Called by CombatManager after player casts a spell
    public void ShowResult(ResistanceTier tier, int damage)
    {
        // Reveal the resistance for the element that was just cast
        ElementType cast = SongInputHandler.Instance.ActiveSong;
        revealed[cast] = tier;
        RefreshResistanceLabels();

        resultText.text = tier switch
        {
            ResistanceTier.Weak   => $"RESONANTE!  -{damage} HP",
            ResistanceTier.Normal => $"GOLPE!  -{damage} HP",
            ResistanceTier.Immune => "SIN EFECTO...",
            _                    => ""
        };
        enemyHPSlider.value = activeEnemy.CurrentHP;
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    public void ShowFailFeedback()
    {
        resultText.text = "¡TECLAS INCORRECTAS!";
        Invoke(nameof(ReEnableSongButtons), 1.2f);
    }

    // Called at the start of each player turn; dotDamage = 0 if no DOT was active
    public void ShowNewTurn(int dotDamage)
    {
        if (dotDamage > 0)
            resultText.text = $"Veneno: -{dotDamage} HP";
        else
            resultText.text = "";

        enemyHPSlider.value = activeEnemy != null ? activeEnemy.CurrentHP : 0;
        Invoke(nameof(ReEnableSongButtons), dotDamage > 0 ? 1.2f : 0f);
    }

    // Called by CombatManager after enemy acts
    public void ShowEnemyAction(EnemyInstance.AbilityType ability, EnemyInstance enemy)
    {
        resultText.text = ability switch
        {
            EnemyInstance.AbilityType.Heal        => $"{enemy.data.enemyName} se cura!",
            EnemyInstance.AbilityType.DOT         => $"{enemy.data.enemyName} te maldice!",
            EnemyInstance.AbilityType.PowerStrike => $"{enemy.data.enemyName} golpe fuerte!",
            _                                     => ""
        };
        enemyHPSlider.value = enemy.CurrentHP;
    }

    void RefreshResistanceLabels()
    {
        for (int i = 0; i < allElements.Length && i < resistanceLabels.Length; i++)
        {
            ElementType e = allElements[i];
            if (revealed.TryGetValue(e, out ResistanceTier tier))
            {
                string tierText = tier switch
                {
                    ResistanceTier.Immune => "INMUNE",
                    ResistanceTier.Normal => "NORMAL",
                    ResistanceTier.Weak   => "DEBIL",
                    _                    => "?"
                };
                resistanceLabels[i].text = $"{e}: {tierText}";
            }
            else
            {
                resistanceLabels[i].text = $"{e}: ?";
            }
        }
    }

    void ReEnableSongButtons()
    {
        resultText.text = "";
        songButtonsGroup.SetActive(true);
    }
}
