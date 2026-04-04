using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    [Header("Bars")]
    public Slider hpBar;

    [Header("Labels")]
    public TextMeshProUGUI floorLabel;
    public TextMeshProUGUI goldLabel;

    [Header("Player Portrait")]
    public Image portraitImage;
    // Assign 4 sprites in the Inspector: index 0 = full HP, index 3 = near death
    public Sprite[] portraitSprites = new Sprite[4];

    [Header("Inventory Panel")]
    // One Image per slot — assign in Inspector in this order: Head, Chest, Legs, Feet, Feather
    public Image[] inventorySlots = new Image[5];

    [Header("Console Log")]
    public TextMeshProUGUI consoleText;
    public int maxLines = 8;

    private readonly Queue<string> _lines = new Queue<string>();

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
        Refresh();
        RefreshInventory();
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance) PlayerStats.Instance.OnStatsChanged -= Refresh;
        if (GameManager.Instance) GameManager.Instance.OnStateChanged -= OnStateChanged;
        if (PlayerInventory.Instance) PlayerInventory.Instance.OnInventoryChanged -= RefreshInventory;
    }

    // -------------------------------------------------------------------------

    void OnStateChanged(GameState state)
    {
        floorLabel.text = $"Floor {FloorManager.Instance.CurrentFloor} / {FloorManager.MaxFloors}";
        goldLabel.text  = $"Gold: {FloorManager.Instance.Gold}";
    }

    void Refresh()
    {
        hpBar.maxValue = PlayerStats.Instance.maxHP;
        hpBar.value    = PlayerStats.Instance.CurrentHP;
        goldLabel.text = $"Gold: {FloorManager.Instance.Gold}";
        RefreshPortrait();
    }

    void RefreshPortrait()
    {
        if (portraitImage == null || portraitSprites == null || portraitSprites.Length < 4) return;

        float ratio = (float)PlayerStats.Instance.CurrentHP / PlayerStats.Instance.maxHP;

        // 0: >75%  1: >50%  2: >25%  3: <=25%
        int index;
        if      (ratio > 0.75f) index = 0;
        else if (ratio > 0.50f) index = 1;
        else if (ratio > 0.25f) index = 2;
        else                    index = 3;

        if (portraitSprites[index] != null)
            portraitImage.sprite = portraitSprites[index];
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
