using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BackpackUI : MonoBehaviour
{
    public static BackpackUI Instance { get; private set; }

    [Header("Backpack Panel")]
    [SerializeField] private GameObject backpackPanel;
    [SerializeField] private Button backpackCloseButton;
    [SerializeField] private Transform backpackListRoot;
    [SerializeField] private TextMeshProUGUI backpackEmptyLabel;

    private readonly List<BackpackItemEntry> _entries = new List<BackpackItemEntry>();

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (backpackPanel != null)
            backpackPanel.SetActive(false);

        // Ensure ContentSizeFitter is present so the grid content grows and ScrollRect can scroll
        if (backpackListRoot != null && backpackListRoot.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = backpackListRoot.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    void Start()
    {
        PlayerInventory.Instance.OnInventoryChanged += OnInventoryChanged;

        if (backpackCloseButton != null)
            backpackCloseButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (PlayerInventory.Instance) PlayerInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
        if (backpackCloseButton != null) backpackCloseButton.onClick.RemoveListener(Close);
    }

    // -------------------------------------------------------------------------

    public void Toggle()
    {
        if (backpackPanel == null) return;

        bool willOpen = !backpackPanel.activeSelf;
        backpackPanel.SetActive(willOpen);
        if (willOpen)
            RebuildList();
    }

    public void Close()
    {
        if (backpackPanel == null) return;
        backpackPanel.SetActive(false);
    }

    // -------------------------------------------------------------------------

    void OnInventoryChanged()
    {
        if (backpackPanel != null && backpackPanel.activeSelf)
            RebuildList();
    }

    void RebuildList()
    {
        if (backpackListRoot == null) return;

        for (int i = backpackListRoot.childCount - 1; i >= 0; i--)
            Destroy(backpackListRoot.GetChild(i).gameObject);
        _entries.Clear();

        var backpackItems = PlayerInventory.Instance.BackpackItems;

        if (backpackEmptyLabel != null)
            backpackEmptyLabel.gameObject.SetActive(backpackItems.Count == 0);

        for (int i = 0; i < backpackItems.Count; i++)
        {
            ItemData item = backpackItems[i];
            if (item == null) continue;

            var go = new GameObject($"Item_{i}", typeof(RectTransform), typeof(CanvasRenderer),
                                    typeof(TextMeshProUGUI), typeof(BackpackItemEntry));
            go.transform.SetParent(backpackListRoot, false);

            int index = i;
            var entry = go.GetComponent<BackpackItemEntry>();
            entry.Init(item.itemName, () => EquipItem(index));
            _entries.Add(entry);
        }
    }

    void EquipItem(int index)
    {
        if (!PlayerInventory.Instance.EquipFromBackpack(index, out var equippedItem, out var replacedItem))
            return;

        if (equippedItem != null)
        {
            if (replacedItem != null)
                HUDController.Instance.Log($"Equipped {equippedItem.itemName} ({equippedItem.slot}). Stored {replacedItem.itemName} in backpack.");
            else
                HUDController.Instance.Log($"Equipped {equippedItem.itemName} ({equippedItem.slot}).");
        }
    }
}
