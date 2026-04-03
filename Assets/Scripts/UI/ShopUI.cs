using UnityEngine;
using UnityEngine.UI;
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
        closeButton.onClick.AddListener(VendorNPC.Instance.CloseShop);
        PlayerStats.Instance.OnStatsChanged += RefreshGoldLabel;
    }

    void OnDestroy()
    {
        if (PlayerStats.Instance) PlayerStats.Instance.OnStatsChanged -= RefreshGoldLabel;
    }

    public void Show(List<ItemData> vendorStock)
    {
        stock = vendorStock;
        RefreshSlots();
        RefreshGoldLabel();
        panel.SetActive(true);
    }

    public void Hide() => panel.SetActive(false);

    void TryBuy(int index)
    {
        if (index >= stock.Count) return;
        VendorNPC.Instance.BuyItem(stock[index]);
        RefreshSlots();
    }

    void RefreshSlots()
    {
        for (int i = 0; i < itemButtons.Length; i++)
        {
            bool hasItem = i < stock.Count;
            itemButtons[i].gameObject.SetActive(hasItem);
            if (!hasItem) continue;

            ItemData item = stock[i];
            itemNameLabels[i].text  = item.itemName;
            itemPriceLabels[i].text = $"{item.buyPrice} oro";
            itemDescLabels[i].text  = BuildStatDesc(item);
            itemButtons[i].interactable = FloorManager.Instance.Gold >= item.buyPrice;
        }
    }

    void RefreshGoldLabel()
    {
        goldLabel.text = $"Oro: {FloorManager.Instance.Gold}";
        RefreshBuyButtonStates();
    }

    void RefreshBuyButtonStates()
    {
        for (int i = 0; i < itemButtons.Length; i++)
            if (i < stock.Count)
                itemButtons[i].interactable = FloorManager.Instance.Gold >= stock[i].buyPrice;
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
