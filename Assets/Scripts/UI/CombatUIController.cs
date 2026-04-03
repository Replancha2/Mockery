using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CombatUIController : MonoBehaviour
{
    public static CombatUIController Instance { get; private set; }

    [Header("Combat HUD Overlay")]
    public GameObject hudPanel;           // top-left HUD showing target info
    public TextMeshProUGUI targetNameText;
    public TextMeshProUGUI targetHPText;
    public Image targetElementIcon;
    
    [Header("Feedback")]
    public TextMeshProUGUI feedbackText;  // floating combat feedback (center-ish)
    public TextMeshProUGUI playerHPText;  // player HP in HUD

    [Header("Spell Sequence Display")]
    public Image[] keyIcons;              // 6 sequence icons
    public Sprite[] dirSprites;           // U/D/L/R sprites
    public TextMeshProUGUI castingLabel;  // "Casting: Consonant" etc

    private EnemyInstance displayedEnemy;
    private float feedbackTimer = 0f;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        if (hudPanel != null)
            hudPanel.SetActive(false);
    }

    void Start()
    {
        if (SongInputHandler.Instance != null)
            SongInputHandler.Instance.OnKeyCorrect += UpdateSequenceDisplay;

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += UpdatePlayerHP;
            UpdatePlayerHP();
        }
    }

    void Update()
    {
        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0 && feedbackText != null)
            feedbackText.text = "";

        // Update target HP in real-time
        if (displayedEnemy != null && targetHPText != null)
            targetHPText.text = $"HP: {displayedEnemy.CurrentHP}/{displayedEnemy.data.maxHP}";
    }

    public void SetTargetEnemy(EnemyInstance enemy)
    {
        displayedEnemy = enemy;
        if (enemy == null) { ClearTargetEnemy(); return; }

        if (hudPanel != null)
            hudPanel.SetActive(true);

        if (targetNameText != null)
            targetNameText.text = $"{enemy.data.enemyName} ({enemy.data.element})";

        if (targetHPText != null)
            targetHPText.text = $"HP: {enemy.CurrentHP}/{enemy.data.maxHP}";

        Debug.Log($"[HUD] Target set: {enemy.data.enemyName}");
    }

    public void ClearTargetEnemy()
    {
        displayedEnemy = null;
        if (hudPanel != null)
            hudPanel.SetActive(false);
        ClearSequenceDisplay();
    }

    public void ShowSpellResult(string resultType, int damage)
    {
        if (feedbackText == null) return;
        feedbackText.text = $"{resultType} +{damage}";
        feedbackText.color = resultType.Contains("RESONANT") ? Color.green : Color.yellow;
        feedbackTimer = 1.5f;
    }

    public void ShowSpellMiss()
    {
        if (feedbackText == null) return;
        feedbackText.text = "MISS! Wrong sequence!";
        feedbackText.color = Color.red;
        feedbackTimer = 1.5f;
    }

    public void ShowPlayerHit(int damageAmount)
    {
        if (feedbackText == null) return;
        feedbackText.text = $"-{damageAmount} HP!";
        feedbackText.color = Color.red;
        feedbackTimer = 1.5f;
    }

    void UpdateSequenceDisplay(int newIndex)
    {
        if (keyIcons == null) return;

        // Highlight completed keys in sequence
        for (int i = 0; i < keyIcons.Length; i++)
        {
            if (keyIcons[i] != null)
                keyIcons[i].color = i < newIndex ? Color.green : Color.gray;
        }
    }

    void ClearSequenceDisplay()
    {
        if (castingLabel != null)
            castingLabel.text = "";

        if (keyIcons == null) return;

        foreach (var icon in keyIcons)
        {
            if (icon != null)
                icon.color = Color.gray;
        }
    }

    void UpdatePlayerHP()
    {
        if (playerHPText != null)
            playerHPText.text = $"Player HP: {PlayerStats.Instance.CurrentHP}/{PlayerStats.Instance.maxHP}";
    }
}
