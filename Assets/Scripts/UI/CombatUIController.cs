using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public GameObject spellbookPanel;      // panel that represents the bard's book
    public Image[] keyIcons;              // 6 sequence icons
    public Sprite[] dirSprites;           // U/D/L/R sprites in this order
    public Sprite arrowSprite;            // Optional single sprite (must point UP by default)
    public TextMeshProUGUI castingLabel;  // "Casting: Consonant" etc

    private EnemyInstance displayedEnemy;
    private float feedbackTimer = 0f;

    private static readonly SpellType[] allElements =
        { SpellType.Assonant, SpellType.Dissonant, SpellType.Consonant };

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
        {
            SongInputHandler.Instance.OnInputStarted += ShowSpellbook;
            SongInputHandler.Instance.OnInputEnded += HideSpellbook;
            SongInputHandler.Instance.OnKeyCorrect += UpdateSequenceDisplay;
        }

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnStatsChanged += UpdatePlayerHP;
            UpdatePlayerHP();
        }

        if (spellbookPanel != null)
            spellbookPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (SongInputHandler.Instance != null)
        {
            SongInputHandler.Instance.OnInputStarted -= ShowSpellbook;
            SongInputHandler.Instance.OnInputEnded -= HideSpellbook;
            SongInputHandler.Instance.OnKeyCorrect -= UpdateSequenceDisplay;
        }

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.OnStatsChanged -= UpdatePlayerHP;
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
            targetNameText.text = $"{enemy.data.enemyName} ({enemy.Resistance} Resistant)";

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
        feedbackText.color = resultType.Contains("RESONANT") ? Color.green
            : resultType.Contains("NO EFFECT") ? Color.red
            : resultType.Contains("WEAK") ? new Color(1f, 0.65f, 0.2f)
            : Color.yellow;
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

    void ShowSpellbook(SpellType spell)
    {
        if (spellbookPanel != null)
            spellbookPanel.SetActive(true);

        if (castingLabel != null)
            castingLabel.text = $"Casting: {spell}";

        var sequence = SongInputHandler.Songs[spell];
        for (int i = 0; i < keyIcons.Length; i++)
        {
            if (keyIcons[i] == null) continue;

            bool hasStep = i < sequence.Length;
            keyIcons[i].gameObject.SetActive(hasStep);
            if (!hasStep) continue;

            ApplyDirectionToIcon(keyIcons[i], sequence[i]);
            keyIcons[i].color = Color.gray;
        }
    }

    void HideSpellbook(bool _)
    {
        if (spellbookPanel != null)
            spellbookPanel.SetActive(false);
        ClearSequenceDisplay();
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
            {
                icon.color = Color.gray;
                icon.rectTransform.localRotation = Quaternion.identity;
            }
        }
    }

    void ApplyDirectionToIcon(Image icon, SongInputHandler.Dir dir)
    {
        if (icon == null) return;

        // Preferred mode: one arrow sprite that we rotate per direction.
        if (arrowSprite != null)
        {
            icon.sprite = arrowSprite;
            icon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, GetDirRotationZ(dir));
            return;
        }

        // Fallback mode: 4 independent directional sprites.
        icon.sprite = GetDirSprite(dir);
        icon.rectTransform.localRotation = Quaternion.identity;
    }

    float GetDirRotationZ(SongInputHandler.Dir dir)
    {
        return dir switch
        {
            SongInputHandler.Dir.Up => 0f,
            SongInputHandler.Dir.Right => -90f,
            SongInputHandler.Dir.Down => 180f,
            SongInputHandler.Dir.Left => 90f,
            _ => 0f
        };
    }

    Sprite GetDirSprite(SongInputHandler.Dir dir)
    {
        if (dirSprites == null || dirSprites.Length < 4) return null;

        return dir switch
        {
            SongInputHandler.Dir.Up => dirSprites[0],
            SongInputHandler.Dir.Down => dirSprites[1],
            SongInputHandler.Dir.Left => dirSprites[2],
            SongInputHandler.Dir.Right => dirSprites[3],
            _ => null
        };
    }

    void UpdatePlayerHP()
    {
        if (playerHPText != null)
            playerHPText.text = $"Player HP: {PlayerStats.Instance.CurrentHP}/{PlayerStats.Instance.maxHP}";
    }
}
