using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class ShopUI : MonoBehaviour
{
    public static ShopUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;

    [Header("Item Slots (3)")]
    public Button[]          itemButtons;
    public TextMeshProUGUI[] itemNameLabels;
    public TextMeshProUGUI[] itemPriceLabels;
    public TextMeshProUGUI[] itemDescLabels;

    [Header("Labels")]
    public TextMeshProUGUI goldLabel;
    public Button          closeButton;

    private List<ItemData> stock = new();
    private VendorNPC _currentVendor;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    void Start()
    {
        for (int i = 0; i < itemButtons.Length; i++)
        {
            int idx = i;
            itemButtons[i].onClick.AddListener(() => TryBuy(idx));
        }
        closeButton.onClick.AddListener(() => _currentVendor?.CloseShop());
        PlayerStats.Instance.OnStatsChanged += RefreshGoldLabel;
    }

    void Update()
    {
        if (!panel.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Escape))
            _currentVendor?.CloseShop();
        if (Input.GetMouseButtonDown(0))
        {
            var rt = closeButton.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, Input.mousePosition, null))
                _currentVendor?.CloseShop();
        }
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance) PlayerStats.Instance.OnStatsChanged -= RefreshGoldLabel;
    }

    public void Show(VendorNPC vendor, List<ItemData> vendorStock)
    {
        _currentVendor = vendor;
        stock = vendorStock;
        RefreshSlots();
        RefreshGoldLabel();
        panel.SetActive(true);
    }

    public void Hide()
    {
        _currentVendor = null;
        panel.SetActive(false);
    }

    void TryBuy(int index)
    {
        if (index >= stock.Count) return;
        _currentVendor?.BuyItem(stock[index]);
        RefreshSlots();
    }

    void RefreshSlots()
    {
        if (stock == null) stock = new List<ItemData>();

        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (itemButtons[i] == null)
            {
                Debug.LogWarning($"ShopUI: itemButtons[{i}] is not assigned.");
                continue;
            }

            bool hasItem = i < stock.Count;
            itemButtons[i].gameObject.SetActive(hasItem);
            if (!hasItem) continue;

            ItemData item = stock[i];
            if (item == null)
            {
                Debug.LogWarning($"ShopUI: stock[{i}] is null. Check Vendor itemPool assignments.");
                itemButtons[i].gameObject.SetActive(false);
                continue;
            }

            if (i < itemNameLabels.Length && itemNameLabels[i] != null)
                itemNameLabels[i].text = item.itemName;
            else
                Debug.LogWarning($"ShopUI: itemNameLabels[{i}] is missing.");

            if (i < itemPriceLabels.Length && itemPriceLabels[i] != null)
                itemPriceLabels[i].text = $"{item.buyPrice} Gold";
            else
                Debug.LogWarning($"ShopUI: itemPriceLabels[{i}] is missing.");

            if (i < itemDescLabels.Length && itemDescLabels[i] != null)
                itemDescLabels[i].text = BuildStatDesc(item);
            else
                Debug.LogWarning($"ShopUI: itemDescLabels[{i}] is missing.");

            itemButtons[i].interactable = FloorManager.Instance != null && FloorManager.Instance.Gold >= item.buyPrice;
        }
    }

    void RefreshGoldLabel()
    {
        if (goldLabel == null || FloorManager.Instance == null) return;
        goldLabel.text = $"Gold: {FloorManager.Instance.Gold}";
        RefreshBuyButtonStates();
    }

    void RefreshBuyButtonStates()
    {
        for (int i = 0; i < itemButtons.Length; i++)
        {
            if (itemButtons[i] == null) continue;
            if (i >= stock.Count || stock[i] == null || FloorManager.Instance == null)
            {
                itemButtons[i].interactable = false;
                continue;
            }

            itemButtons[i].interactable = FloorManager.Instance.Gold >= stock[i].buyPrice;
        }
    }

    string BuildStatDesc(ItemData item)
    {
        var sb = new System.Text.StringBuilder();
        if (item.hpBonus > 0)            sb.AppendLine($"+{item.hpBonus} HP");
        if (item.defenseBonus > 0)       sb.AppendLine($"+{item.defenseBonus} DEF");
        if (item.AssonantBonus > 0)      sb.AppendLine($"+{item.AssonantBonus} Assonant");
        if (item.DissonantBonus > 0)   sb.AppendLine($"+{item.DissonantBonus} Dissonant");
        if (item.ConsonantBonus > 0)    sb.AppendLine($"+{item.ConsonantBonus} Consonant");
        return sb.ToString().TrimEnd();
    }
}
