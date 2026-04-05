using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Bars")]
    public Slider hpBar;

    [Header("Labels")]
    public TextMeshProUGUI floorLabel;
    public TextMeshProUGUI goldLabel;
    public TextMeshProUGUI potionCountLabel;

    [Header("Player Portrait")]
    public Image portraitImage;
    // Assign 4 sprites in the Inspector: index 0 = full HP, index 3 = near death
    public Sprite[] portraitSprites = new Sprite[4];

    [Header("Portrait Damage Shake")]
    [SerializeField] private float portraitShakeDuration = 0.14f;
    [SerializeField] private float portraitShakeStrength = 10f;
    [SerializeField] private float portraitShakeFrequency = 0.02f;

    [Header("Damage Screen Impact")]
    [SerializeField] private Image damageImpactImage;
    [SerializeField] private float damageImpactMaxAlpha = 0.35f;
    [SerializeField] private float damageImpactFadeDuration = 0.20f;

    [Header("Inventory Panel")]
    // One Image per slot — assign in Inspector in this order: Head, Chest, Legs, Feet, Feather
    public Image[] inventorySlots = new Image[5];

    [Header("Potion")]
    [SerializeField] private Button potionButton;

    [Header("Backpack")]
    [SerializeField] private Button backpackButton;

    [Header("Console Log")]
    public TextMeshProUGUI consoleText;
    public int maxLines = 8;

    private readonly Queue<string> _lines = new Queue<string>();
    private int _lastKnownHP;
    private Coroutine _portraitShakeRoutine;
    private Coroutine _damageImpactRoutine;
    private RectTransform _portraitRect;
    private Vector2 _portraitBaseAnchoredPos;
    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        PlayerStats.Instance.OnStatsChanged += Refresh;
        GameManager.Instance.OnStateChanged += OnStateChanged;
        PlayerInventory.Instance.OnInventoryChanged += RefreshInventory;
        if (FloorManager.Instance != null) FloorManager.Instance.OnGoldChanged += OnGoldChanged;

        if (portraitImage != null)
        {
            _portraitRect = portraitImage.rectTransform;
            _portraitBaseAnchoredPos = _portraitRect.anchoredPosition;
        }

        if (potionButton != null)
            potionButton.onClick.AddListener(OnPotionPressed);

        if (backpackButton != null)
            backpackButton.onClick.AddListener(() => BackpackUI.Instance.Toggle());

        _lastKnownHP = PlayerStats.Instance.CurrentHP;
        SetDamageImpactAlpha(0f);
        Refresh();
        RefreshInventory();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance) PlayerStats.Instance.OnStatsChanged -= Refresh;
        if (GameManager.Instance) GameManager.Instance.OnStateChanged -= OnStateChanged;
        if (PlayerInventory.Instance) PlayerInventory.Instance.OnInventoryChanged -= RefreshInventory;
        if (FloorManager.Instance) FloorManager.Instance.OnGoldChanged -= OnGoldChanged;
        if (_portraitRect != null)
            _portraitRect.anchoredPosition = _portraitBaseAnchoredPos;

        if (backpackButton != null)
            backpackButton.onClick.RemoveAllListeners();
        if (potionButton != null)
            potionButton.onClick.RemoveListener(OnPotionPressed);

        SetDamageImpactAlpha(0f);
    }

    // -------------------------------------------------------------------------

    void OnStateChanged(GameState state)
    {
        floorLabel.text = $"Floor {FloorManager.Instance.CurrentFloor} / {FloorManager.MaxFloors}";
        goldLabel.text  = $"{FloorManager.Instance.Gold}";
    }

    void Refresh()
    {
        int currentHP = PlayerStats.Instance.CurrentHP;
        if (currentHP < _lastKnownHP)
        {
            TriggerPortraitShake();
            TriggerDamageImpact();
        }

        hpBar.maxValue = PlayerStats.Instance.maxHP;
        hpBar.value    = currentHP;
        goldLabel.text = $"{FloorManager.Instance.Gold}";
        if (potionCountLabel != null)
            potionCountLabel.text = $"x{PlayerStats.Instance.PotionCharges}";
        if (potionButton != null)
            potionButton.interactable = true;
        RefreshPortrait();
        _lastKnownHP = currentHP;
    }

    void OnPotionPressed()
    {
        if (PlayerStats.Instance.TryUsePotion())
        {
            int healAmount = Mathf.Max(1, Mathf.CeilToInt(PlayerStats.Instance.maxHP * 0.25f));
            Log($"You drink a potion and recover {healAmount} HP.");
        }
        else if (PlayerStats.Instance.PotionCharges <= 0)
        {
            Log("No potions left.");
        }
        else
        {
            Log("You are already at full health.");
        }
    }

    void OnGoldChanged(int newGold)
    {
        if (goldLabel == null) return;
        goldLabel.text = $"{newGold}";
    }

    void RefreshPortrait()
    {
        if (portraitImage == null || portraitSprites == null || portraitSprites.Length < 4) return;

        float ratio = (float)PlayerStats.Instance.CurrentHP / PlayerStats.Instance.maxHP;

        // 0: >75%  1: >50%  2: >25%  3: <=25%
        int index;
        if      (ratio > 0.65f) index = 0;
        else if (ratio > 0.40f) index = 1;
        else if (ratio > 0.01f) index = 2;
        else                    index = 3;

        if (portraitSprites[index] != null)
            portraitImage.sprite = portraitSprites[index];
    }

    void TriggerPortraitShake()
    {
        if (_portraitRect == null) return;

        if (_portraitShakeRoutine != null)
            StopCoroutine(_portraitShakeRoutine);

        _portraitShakeRoutine = StartCoroutine(PortraitShakeRoutine());
    }

    void TriggerDamageImpact()
    {
        if (damageImpactImage == null) return;

        if (_damageImpactRoutine != null)
            StopCoroutine(_damageImpactRoutine);

        _damageImpactRoutine = StartCoroutine(DamageImpactRoutine());
    }

    IEnumerator PortraitShakeRoutine()
    {
        float elapsed = 0f;

        while (elapsed < portraitShakeDuration)
        {
            Vector2 offset = Random.insideUnitCircle * portraitShakeStrength;
            _portraitRect.anchoredPosition = _portraitBaseAnchoredPos + offset;
            elapsed += portraitShakeFrequency;
            yield return new WaitForSeconds(portraitShakeFrequency);
        }

        _portraitRect.anchoredPosition = _portraitBaseAnchoredPos;
        _portraitShakeRoutine = null;
    }

    IEnumerator DamageImpactRoutine()
    {
        SetDamageImpactAlpha(damageImpactMaxAlpha);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, damageImpactFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetDamageImpactAlpha(Mathf.Lerp(damageImpactMaxAlpha, 0f, t));
            yield return null;
        }

        SetDamageImpactAlpha(0f);
        _damageImpactRoutine = null;
    }

    void SetDamageImpactAlpha(float alpha)
    {
        if (damageImpactImage == null) return;
        Color c = damageImpactImage.color;
        c.a = Mathf.Clamp01(alpha);
        damageImpactImage.color = c;
    }

    void RefreshInventory()
    {
        if (inventorySlots == null) return;

        EquipmentSlot[] order = { EquipmentSlot.Head, EquipmentSlot.Chest, EquipmentSlot.Legs, EquipmentSlot.Feet, EquipmentSlot.Feather };

        for (int i = 0; i < order.Length && i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null) continue;

            ItemData equipped = PlayerInventory.Instance.GetEquipped(order[i]);
            inventorySlots[i].sprite  = equipped != null ? equipped.icon : null;
            inventorySlots[i].enabled = equipped != null;
        }

    }

    // -------------------------------------------------------------------------
    // Console log — call HUDController.Instance.Log("something happened");

    public void Log(string message)
    {
        _lines.Enqueue(message);
        while (_lines.Count > maxLines)
            _lines.Dequeue();

        consoleText.text = string.Join("\n", _lines);
    }
}
