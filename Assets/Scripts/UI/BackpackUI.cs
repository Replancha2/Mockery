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
    [SerializeField] private Button backpackItemButtonPrefab;
    [SerializeField] private TextMeshProUGUI backpackEmptyLabel;

    private readonly List<Button> _entryButtons = new List<Button>();

    // -------------------------------------------------------------------------

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        if (backpackPanel != null)
            backpackPanel.SetActive(false);
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
        if (backpackListRoot == null || backpackItemButtonPrefab == null) return;

        for (int i = 0; i < _entryButtons.Count; i++)
        {
            if (_entryButtons[i] != null)
                Destroy(_entryButtons[i].gameObject);
        }
        _entryButtons.Clear();

        var backpackItems = PlayerInventory.Instance.BackpackItems;

        if (backpackEmptyLabel != null)
            backpackEmptyLabel.gameObject.SetActive(backpackItems.Count == 0);

        for (int i = 0; i < backpackItems.Count; i++)
        {
            ItemData item = backpackItems[i];
            if (item == null) continue;

            Button entry = Instantiate(backpackItemButtonPrefab, backpackListRoot);
            _entryButtons.Add(entry);

            var label = entry.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = item.itemName;

            int index = i;
            entry.onClick.AddListener(() => EquipItem(index));
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
