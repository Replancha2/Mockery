using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

[System.Serializable]
public class BestiaryPortraitEntry
{
    public string enemyName;
    public Sprite portrait;
}

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

    [Header("Book Motion")]
    [SerializeField] private float walkShakeStrength = 6f;
    [SerializeField] private float walkShakeSpeed = 12f;

    [Header("Spell Prepare Animation")]
    [SerializeField] private Image bookFrameImage;
    [SerializeField] private Sprite[] spellPrepareFrames;
    [SerializeField] private float spellPrepareFps = 18f;

    [Header("Bestiary")]
    [SerializeField] private TextMeshProUGUI bestiaryText;
    [SerializeField] private BestiaryPortraitEntry[] bestiaryPortraits;
    [SerializeField] private Image[] bestiaryImageSlots;            // exactly 3 recommended
    [SerializeField] private TextMeshProUGUI[] bestiaryNameSlots;   // exactly 3 recommended
    [SerializeField] private TextMeshProUGUI[] bestiaryResistSlots; // exactly 3 recommended
    [SerializeField] private TextMeshProUGUI bestiaryPageLabel;
    [SerializeField] private Button bestiaryPrevButton;
    [SerializeField] private Button bestiaryNextButton;
    [SerializeField] private Color bestiaryHiddenImageColor = Color.black;
    [SerializeField] private Color bestiaryVisibleImageColor = Color.white;

    private EnemyInstance displayedEnemy;
    private float feedbackTimer = 0f;
    private RectTransform _bookRect;
    private Vector2 _bookBasePos;
    private bool _bookBaseCaptured;
    private Coroutine _spellPrepareAnimRoutine;
    private bool _bestiaryInitialized;
    private int _bestiaryPageIndex;
    private Vector3 _lastPlayerPosForBookShake;

    private readonly List<string> _bestiaryOrder = new();
    private readonly Dictionary<string, bool> _defeatedByName = new();
    private readonly Dictionary<string, SpellType> _knownResistanceByName = new();

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
        {
            spellbookPanel.SetActive(true);
            _bookRect = spellbookPanel.GetComponent<RectTransform>();
            if (_bookRect != null)
            {
                _bookBasePos = _bookRect.anchoredPosition;
                _bookBaseCaptured = true;
            }
        }

        ShowIdleSpellbook();
        TryInitializeBestiary();

        if (bestiaryPrevButton != null)
            bestiaryPrevButton.onClick.AddListener(PrevBestiaryPage);
        if (bestiaryNextButton != null)
            bestiaryNextButton.onClick.AddListener(NextBestiaryPage);
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

        if (_spellPrepareAnimRoutine != null)
            StopCoroutine(_spellPrepareAnimRoutine);

        if (bestiaryPrevButton != null)
            bestiaryPrevButton.onClick.RemoveListener(PrevBestiaryPage);
        if (bestiaryNextButton != null)
            bestiaryNextButton.onClick.RemoveListener(NextBestiaryPage);

        ResetBookPosition();
    }

    void Update()
    {
        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0 && feedbackText != null)
            feedbackText.text = "";

        TryInitializeBestiary();
        UpdateBookShake();

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
            targetNameText.text = $"{enemy.DisplayName} ({enemy.Resistance} Resistant)";

        if (targetHPText != null)
            targetHPText.text = $"HP: {enemy.CurrentHP}/{enemy.data.maxHP}";

        Debug.Log($"[HUD] Target set: {enemy.DisplayName}");
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
        if (castingLabel != null)
            castingLabel.text = $"Casting: {spell}";

        PlaySpellPrepareAnimation();

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
        ShowIdleSpellbook();
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
        if (keyIcons == null) return;

        foreach (var icon in keyIcons)
        {
            if (icon != null)
            {
                icon.color = Color.gray;
                icon.rectTransform.localRotation = Quaternion.identity;
                icon.gameObject.SetActive(false);
            }
        }
    }

    void ShowIdleSpellbook()
    {
        ClearSequenceDisplay();

        if (castingLabel != null)
        {
            castingLabel.text = "Consonant Spell = Q Key\n" +
                               "Assonant Spell = E Key\n" +
                               "Discordant Spell = R Key\n" +
                               "To cast a spell, keep Shift pressed + the sequence shown";
        }
    }

    void UpdateBookShake()
    {
        if (_bookRect == null || !_bookBaseCaptured)
            return;

        bool shouldShake = IsPlayerMovingForBookShake();
        if (!shouldShake)
        {
            ResetBookPosition();
            return;
        }

        float t = Time.time * walkShakeSpeed;
        Vector2 offset = new Vector2(Mathf.Sin(t), Mathf.Cos(t * 1.17f)) * walkShakeStrength;
        _bookRect.anchoredPosition = _bookBasePos + offset;
    }

    void ResetBookPosition()
    {
        if (_bookRect != null && _bookBaseCaptured)
            _bookRect.anchoredPosition = _bookBasePos;
    }

    void PlaySpellPrepareAnimation()
    {
        if (bookFrameImage == null || spellPrepareFrames == null || spellPrepareFrames.Length == 0)
            return;

        TryPlayPageFlipSfx();

        if (_spellPrepareAnimRoutine != null)
            StopCoroutine(_spellPrepareAnimRoutine);

        _spellPrepareAnimRoutine = StartCoroutine(PlaySpellPrepareAnimationRoutine());
    }

    IEnumerator PlaySpellPrepareAnimationRoutine()
    {
        float frameDelay = 1f / Mathf.Max(1f, spellPrepareFps);

        for (int i = 0; i < spellPrepareFrames.Length; i++)
        {
            if (spellPrepareFrames[i] != null)
                bookFrameImage.sprite = spellPrepareFrames[i];

            yield return new WaitForSeconds(frameDelay);
        }

        _spellPrepareAnimRoutine = null;
    }

    void TryPlayPageFlipSfx()
    {
        AudioManager am = AudioManager.Instance;
        if (am == null) return;

        System.Type t = am.GetType();

        // Newer API path.
        MethodInfo playPageFlip = t.GetMethod("PlayPageFlip", BindingFlags.Instance | BindingFlags.Public);
        if (playPageFlip != null)
        {
            playPageFlip.Invoke(am, null);
            return;
        }

        // Older API path: get clip and feed it to PlaySFX(AudioClip).
        AudioClip clip = null;
        FieldInfo clipField = t.GetField("pageFlipClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (clipField != null)
            clip = clipField.GetValue(am) as AudioClip;

        if (clip == null)
        {
            PropertyInfo clipProp = t.GetProperty("pageFlipClip", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (clipProp != null)
                clip = clipProp.GetValue(am, null) as AudioClip;
        }

        if (clip == null) return;

        MethodInfo playSfx = t.GetMethod("PlaySFX", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(AudioClip) }, null);
        if (playSfx != null)
            playSfx.Invoke(am, new object[] { clip });
    }

    bool IsPlayerMovingForBookShake()
    {
        if (GridMover.Instance == null) return false;

        // Try public property first via reflection for compatibility across versions.
        PropertyInfo prop = typeof(GridMover).GetProperty("IsMoving", BindingFlags.Instance | BindingFlags.Public);
        if (prop != null && prop.PropertyType == typeof(bool))
        {
            object v = prop.GetValue(GridMover.Instance);
            if (v is bool b) return b;
        }

        // Fallback to private field used by older versions.
        FieldInfo field = typeof(GridMover).GetField("isMoving", BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(bool))
        {
            object v = field.GetValue(GridMover.Instance);
            if (v is bool b) return b;
        }

        // Last fallback: infer movement from transform delta.
        Vector3 p = GridMover.Instance.transform.position;
        bool moved = (p - _lastPlayerPosForBookShake).sqrMagnitude > 0.000001f;
        _lastPlayerPosForBookShake = p;
        return moved;
    }

    void TryInitializeBestiary()
    {
        if (_bestiaryInitialized) return;
        if (EnemySpawner.Instance == null) return;

        _bestiaryOrder.Clear();
        _defeatedByName.Clear();
        _knownResistanceByName.Clear();

        string[] fixedNames =
        {
            EnemyInstance.GetNameForResistance(SpellType.Consonant),
            EnemyInstance.GetNameForResistance(SpellType.Assonant),
            EnemyInstance.GetNameForResistance(SpellType.Dissonant),
        };

        foreach (string name in fixedNames)
        {
            if (_bestiaryOrder.Contains(name)) continue;
            _bestiaryOrder.Add(name);
            _defeatedByName[name] = false;
        }

        _bestiaryInitialized = true;
        _bestiaryPageIndex = 0;
        RefreshBestiaryText();
        RefreshBestiaryPage();
    }

    public void RegisterEnemyDefeat(string enemyName)
    {
        if (string.IsNullOrWhiteSpace(enemyName)) return;

        if (!_defeatedByName.ContainsKey(enemyName))
            _bestiaryOrder.Add(enemyName);

        _defeatedByName[enemyName] = true;
        RefreshBestiaryText();
        RefreshBestiaryPage();
    }

    public void RegisterResistanceDiscovery(string enemyName, SpellType resistance)
    {
        if (string.IsNullOrWhiteSpace(enemyName)) return;

        if (!_defeatedByName.ContainsKey(enemyName))
        {
            _bestiaryOrder.Add(enemyName);
            _defeatedByName[enemyName] = false;
        }

        _knownResistanceByName[enemyName] = resistance;
        RefreshBestiaryText();
        RefreshBestiaryPage();
    }

    void RefreshBestiaryText()
    {
        if (bestiaryText == null) return;

        if (_bestiaryOrder.Count == 0)
        {
            bestiaryText.text = "Bestiary\n(No entries yet)";
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Bestiary");

        for (int i = 0; i < _bestiaryOrder.Count; i++)
        {
            string enemyName = _bestiaryOrder[i];
            bool defeated = _defeatedByName.TryGetValue(enemyName, out bool isDefeated) && isDefeated;
            string shownName = defeated ? enemyName : "???";

            string resistanceText = _knownResistanceByName.TryGetValue(enemyName, out SpellType resistance)
                ? $"{resistance} Resistant"
                : "???";

            sb.AppendLine($"- {shownName} [{resistanceText}]");
        }

        bestiaryText.text = sb.ToString();
    }

    void RefreshBestiaryPage()
    {
        int pageSize = 3;
        int totalEntries = _bestiaryOrder.Count;
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(totalEntries / (float)pageSize));

        _bestiaryPageIndex = Mathf.Clamp(_bestiaryPageIndex, 0, totalPages - 1);

        if (bestiaryPageLabel != null)
            bestiaryPageLabel.text = $"Page {_bestiaryPageIndex + 1}/{totalPages}";

        if (bestiaryPrevButton != null)
            bestiaryPrevButton.interactable = _bestiaryPageIndex > 0;
        if (bestiaryNextButton != null)
            bestiaryNextButton.interactable = _bestiaryPageIndex < totalPages - 1;

        for (int i = 0; i < pageSize; i++)
        {
            int entryIndex = _bestiaryPageIndex * pageSize + i;
            bool hasEntry = entryIndex < totalEntries;

            if (i < bestiaryImageSlots.Length && bestiaryImageSlots[i] != null)
                bestiaryImageSlots[i].gameObject.SetActive(hasEntry);
            if (i < bestiaryNameSlots.Length && bestiaryNameSlots[i] != null)
                bestiaryNameSlots[i].gameObject.SetActive(hasEntry);
            if (i < bestiaryResistSlots.Length && bestiaryResistSlots[i] != null)
                bestiaryResistSlots[i].gameObject.SetActive(hasEntry);

            if (!hasEntry) continue;

            string enemyName = _bestiaryOrder[entryIndex];
            bool defeated = _defeatedByName.TryGetValue(enemyName, out bool isDefeated) && isDefeated;

            if (i < bestiaryNameSlots.Length && bestiaryNameSlots[i] != null)
                bestiaryNameSlots[i].text = defeated ? enemyName : "???";

            if (i < bestiaryResistSlots.Length && bestiaryResistSlots[i] != null)
            {
                bestiaryResistSlots[i].text = _knownResistanceByName.TryGetValue(enemyName, out SpellType resistance)
                    ? $"{resistance} Resistant"
                    : "???";
            }

            if (i < bestiaryImageSlots.Length && bestiaryImageSlots[i] != null)
            {
                bestiaryImageSlots[i].sprite = GetBestiaryPortrait(enemyName);
                bestiaryImageSlots[i].color = defeated ? bestiaryVisibleImageColor : bestiaryHiddenImageColor;
            }
        }
    }

    Sprite GetBestiaryPortrait(string enemyName)
    {
        if (bestiaryPortraits != null)
        {
            for (int i = 0; i < bestiaryPortraits.Length; i++)
            {
                var entry = bestiaryPortraits[i];
                if (entry == null) continue;
                if (entry.enemyName == enemyName && entry.portrait != null)
                    return entry.portrait;
            }
        }

        if (EnemySpawner.Instance != null && EnemySpawner.Instance.basicEnemies != null)
        {
            for (int i = 0; i < EnemySpawner.Instance.basicEnemies.Length; i++)
            {
                var enemy = EnemySpawner.Instance.basicEnemies[i];
                if (enemy == null) continue;
                if (enemy.enemyName == enemyName && enemy.sprite != null)
                    return enemy.sprite;
            }
        }

        if (EnemySpawner.Instance != null && EnemySpawner.Instance.bossData != null &&
            EnemySpawner.Instance.bossData.enemyName == enemyName)
        {
            return EnemySpawner.Instance.bossData.sprite;
        }

        return null;
    }

    public void NextBestiaryPage()
    {
        _bestiaryPageIndex++;
        RefreshBestiaryPage();
    }

    public void PrevBestiaryPage()
    {
        _bestiaryPageIndex--;
        RefreshBestiaryPage();
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
