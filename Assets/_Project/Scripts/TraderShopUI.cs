using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TraderShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TraderShopSlotUI[] stockSlots;

    [Header("Layout")]
    [SerializeField] private RectTransform shopPanelRect;
    [SerializeField] private Vector2 normalAnchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 traderAnchoredPosition = new Vector2(320f, 0f);

    [Header("References")]
    [SerializeField] private PlayerWallet playerWallet;
    [SerializeField] private CharacterInventory playerInventory;
    [SerializeField] private InventoryUI playerInventoryUI;

    [Header("Stock")]
    [SerializeField, Min(1)] private int fallbackStockItemCount = 24;
    [SerializeField] private EnemyLootTier traderStockTier = EnemyLootTier.MiniBoss;
    [SerializeField, Min(0)] private int minItemLevelOffset = 0;
    [SerializeField, Min(0)] private int maxItemLevelOffset = 1;

    [Header("Behavior")]
    [SerializeField] private bool closeOnEscape = true;

    private TraderInteractable currentTrader;
    private readonly List<TraderShopStockEntry> stockEntries = new List<TraderShopStockEntry>();
    private bool stockGeneratedForThisLobby;
    private bool cachedNormalPosition;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
    public TraderInteractable CurrentTrader => currentTrader;

    private void Awake()
    {
        if (playerWallet == null)
            playerWallet = FindFirstObjectByType<PlayerWallet>();

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<CharacterInventory>();

        if (playerInventoryUI == null)
            playerInventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);

        if (shopPanelRect == null && panelRoot != null)
            shopPanelRect = panelRoot.GetComponent<RectTransform>();

        if (!cachedNormalPosition && shopPanelRect != null)
        {
            normalAnchoredPosition = shopPanelRect.anchoredPosition;
            cachedNormalPosition = true;
        }

        if (stockSlots != null)
        {
            for (int i = 0; i < stockSlots.Length; i++)
            {
                if (stockSlots[i] != null)
                    stockSlots[i].Setup(this, i);
            }
        }

        Close();
    }

    private void Start()
    {
        GenerateLobbyStock();
    }

    private void OnEnable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged += RefreshGoldDisplay;
    }

    private void OnDisable()
    {
        if (playerWallet != null)
            playerWallet.OnGoldChanged -= RefreshGoldDisplay;
    }

    private void Update()
    {
        if (!IsOpen || !closeOnEscape || Keyboard.current == null)
            return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void OpenForTrader(TraderInteractable trader)
    {
        currentTrader = trader;

        if (!stockGeneratedForThisLobby)
            GenerateLobbyStock();

        if (panelRoot != null)
            panelRoot.SetActive(true);

        ApplyShopLayout(true);
        playerInventoryUI?.OpenForTraderMode(this);
        RefreshAll();
    }

    public void Close()
    {
        currentTrader = null;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        ApplyShopLayout(false);
        playerInventoryUI?.CloseTraderMode();

        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    public void HandleTraderSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= stockEntries.Count)
            return;

        TraderShopStockEntry entry = stockEntries[slotIndex];
        if (entry == null || !entry.IsValid)
            return;

        if (playerInventory == null)
        {
            GameLog.Warning("Lipseste CharacterInventory pentru cumparare.");
            return;
        }

        if (!playerInventory.CanAddItemInstance(entry.Item))
        {
            GameLog.Warning("Inventarul este plin.");
            return;
        }

        if (playerWallet == null || !playerWallet.SpendGold(entry.BuyPrice))
        {
            GameLog.Warning("Nu ai destul gold.");
            return;
        }

        ItemInstance purchasedItem = entry.Item.Clone();
        if (purchasedItem == null || !playerInventory.AddItemInstance(purchasedItem))
        {
            if (playerWallet != null)
                playerWallet.AddGold(entry.BuyPrice);

            GameLog.Warning("Cumpararea a esuat.");
            return;
        }

        stockEntries.RemoveAt(slotIndex);
        GameLog.Success($"Ai cumparat {purchasedItem.DisplayName} pentru {entry.BuyPrice} Gold.");
        RefreshAll();
        playerInventoryUI?.RefreshAll();
    }

    public bool TrySellInventoryItem(int slotIndex)
    {
        if (playerInventory == null || playerWallet == null)
            return false;

        ItemInstance item = playerInventory.GetItemAt(slotIndex);
        if (item == null || !item.IsValid)
            return false;

        int sellPrice = CalculateSellPrice(item);
        if (sellPrice <= 0)
        {
            GameLog.Warning("Acest item nu poate fi vandut.");
            return false;
        }

        ItemInstance removed = playerInventory.TakeAt(slotIndex);
        if (removed == null || !removed.IsValid)
            return false;

        playerWallet.AddGold(sellPrice);
        GameLog.Success($"Ai vandut {removed.DisplayName} pentru {sellPrice} Gold.");

        RefreshAll();
        playerInventoryUI?.RefreshAll();
        return true;
    }

    public int CalculateSellPrice(ItemInstance item)
    {
        if (item == null || item.Definition == null)
            return 0;

        int basePrice = Mathf.Max(1, item.Definition.SellPrice);
        float rarityMultiplier = GetSellRarityMultiplier(item.Rarity);
        float levelMultiplier = 1f + Mathf.Max(0, item.ItemLevel - 1) * 0.10f;

        return Mathf.Max(1, Mathf.RoundToInt(basePrice * rarityMultiplier * levelMultiplier));
    }

    public void GenerateLobbyStock()
    {
        stockEntries.Clear();
        stockGeneratedForThisLobby = false;

        LootGenerator generator = LootGenerator.Instance;
        if (generator == null)
        {
            GameLog.Warning("Nu exista LootGenerator in scena pentru trader.");
            return;
        }

        int desiredCount = stockSlots != null && stockSlots.Length > 0 ? stockSlots.Length : fallbackStockItemCount;
        desiredCount = Mathf.Max(1, desiredCount);

        for (int i = 0; i < desiredCount; i++)
        {
            int rolledItemLevel = RollShopItemLevel();
            List<ItemInstance> generated = generator.GenerateTraderItems(1, rolledItemLevel, traderStockTier);

            if (generated == null || generated.Count == 0)
                continue;

            ItemInstance item = generated[0];
            if (item == null || !item.IsValid)
                continue;

            stockEntries.Add(new TraderShopStockEntry(item, CalculateBuyPrice(item)));
        }

        stockGeneratedForThisLobby = true;
        RefreshAll();
    }

    public void RefreshAll()
    {
        RefreshTitle();
        RefreshGoldDisplay();
        RefreshSlots();
    }

    public void RefreshGoldDisplay()
    {
        if (goldText == null)
            return;

        int gold = playerWallet != null ? playerWallet.CurrentGold : 0;
        goldText.text = $": {gold}";
    }

    private void RefreshTitle()
    {
        if (titleText == null)
            return;

        string traderName = currentTrader != null ? currentTrader.TraderDisplayName : "Trader";
        titleText.text = $"{traderName} Shop";
    }

    private void RefreshSlots()
    {
        if (stockSlots == null)
            return;

        for (int i = 0; i < stockSlots.Length; i++)
        {
            if (stockSlots[i] == null)
                continue;

            if (i < stockEntries.Count)
                stockSlots[i].Refresh(stockEntries[i]);
            else
                stockSlots[i].ClearSlot();
        }
    }

    private int RollShopItemLevel()
    {
        int baseLevel = ResolveBaseShopLevel();
        int minOffset = Mathf.Max(0, minItemLevelOffset);
        int maxOffset = Mathf.Max(minOffset, maxItemLevelOffset);
        int offset = Random.Range(minOffset, maxOffset + 1);

        return Mathf.Max(1, baseLevel + offset);
    }

    private int ResolveBaseShopLevel()
    {
        if (CurrentLevelContext.Instance != null)
            return CurrentLevelContext.Instance.CurrentLevel;

        return 1;
    }

    private int CalculateBuyPrice(ItemInstance item)
    {
        if (item == null || item.Definition == null)
            return 1;

        int basePrice = Mathf.Max(1, item.Definition.BuyPrice);
        float rarityMultiplier = GetBuyRarityMultiplier(item.Rarity);
        float levelMultiplier = 1f + Mathf.Max(0, item.ItemLevel - 1) * 0.15f;

        return Mathf.Max(1, Mathf.RoundToInt(basePrice * rarityMultiplier * levelMultiplier));
    }

    private float GetBuyRarityMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return 1.25f;
            case ItemRarity.Rare: return 1.6f;
            case ItemRarity.Epic: return 2.1f;
            case ItemRarity.Legendary: return 3f;
            case ItemRarity.Unique: return 4f;
            default: return 1f;
        }
    }

    private float GetSellRarityMultiplier(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return 1.15f;
            case ItemRarity.Rare: return 1.35f;
            case ItemRarity.Epic: return 1.7f;
            case ItemRarity.Legendary: return 2.4f;
            case ItemRarity.Unique: return 3.2f;
            default: return 1f;
        }
    }

    private void ApplyShopLayout(bool traderMode)
    {
        if (shopPanelRect == null)
            return;

        shopPanelRect.anchoredPosition = traderMode ? traderAnchoredPosition : normalAnchoredPosition;
    }
}