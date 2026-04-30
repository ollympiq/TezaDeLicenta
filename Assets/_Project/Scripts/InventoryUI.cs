using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterInventory inventory;
    [SerializeField] private CharacterEquipment equipment;
    [SerializeField] private Button toggleButton;
    [SerializeField] private SkillBookUI skillBookUI;

    [Header("Panels")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameObject statsPanelRoot;
    [SerializeField] private GameObject skillBookPanelRoot;

    [Header("Layout")]
    [SerializeField] private RectTransform inventoryPanelRect;
    [SerializeField] private Vector2 normalAnchoredPosition = Vector2.zero;
    [SerializeField] private Vector2 traderAnchoredPosition = new Vector2(-320f, 0f);

    [Header("UI Slots")]
    [SerializeField] private EquipmentSlotUI[] equipmentSlots;
    [SerializeField] private InventorySlotUI[] inventorySlots;

    [Header("Settings")]
    [SerializeField] private bool startOpened = false;
    [SerializeField] private bool hideStatsInTraderMode = true;
    [SerializeField] private bool hideSkillBookInTraderMode = true;

    private TraderShopUI activeTraderShop;
    private bool cachedNormalPosition;

    public bool IsTraderMode => activeTraderShop != null;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<CharacterInventory>();

        if (equipment == null)
            equipment = FindFirstObjectByType<CharacterEquipment>();

        if (skillBookUI == null)
            skillBookUI = FindFirstObjectByType<SkillBookUI>(FindObjectsInactive.Include);

        if (inventoryPanelRect == null && panelRoot != null)
            inventoryPanelRect = panelRoot.GetComponent<RectTransform>();

        if (!cachedNormalPosition && inventoryPanelRect != null)
        {
            normalAnchoredPosition = inventoryPanelRect.anchoredPosition;
            cachedNormalPosition = true;
        }

        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (inventory != null)
            inventory.OnInventoryChanged += RefreshAll;

        if (equipment != null)
            equipment.OnEquipmentChanged += RefreshAll;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
                equipmentSlots[i].Setup(this);
        }

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] != null)
                inventorySlots[i].Setup(this, i);
        }

        SetPanelsVisible(startOpened);

        if (startOpened)
            RefreshAll();
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(TogglePanel);

        if (inventory != null)
            inventory.OnInventoryChanged -= RefreshAll;

        if (equipment != null)
            equipment.OnEquipmentChanged -= RefreshAll;
    }

    private void Update()
    {
        if (IsTraderMode)
            return;

        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            TogglePanel();
    }

    public void TogglePanel()
    {
        if (IsTraderMode)
            return;

        bool isOpen = panelRoot != null && panelRoot.activeSelf;
        bool willOpen = !isOpen;

        SetPanelsVisible(willOpen);
        ApplyInventoryLayout(false);

        if (willOpen)
        {
            RefreshAll();
        }
        else
        {
            HideTooltipsAndDrag();
        }
    }

    public void OpenForTraderMode(TraderShopUI traderShop)
    {
        activeTraderShop = traderShop;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        if (statsPanelRoot != null)
            statsPanelRoot.SetActive(!hideStatsInTraderMode);

        if (skillBookPanelRoot != null)
            skillBookPanelRoot.SetActive(!hideSkillBookInTraderMode);

        ApplyInventoryLayout(true);
        RefreshAll();
    }

    public void CloseTraderMode()
    {
        activeTraderShop = null;
        ApplyInventoryLayout(false);
        SetPanelsVisible(false);
        HideTooltipsAndDrag();
    }

    public void RefreshAll()
    {
        RefreshEquipment();
        RefreshInventory();

        if (skillBookUI != null)
            skillBookUI.RefreshNow();
    }

    public void HandleInventorySlotClicked(int slotIndex, PointerEventData.InputButton button)
    {
        if (inventory == null || equipment == null)
            return;

        if (slotIndex < 0 || slotIndex >= inventory.ItemCount)
            return;

        ItemInstance item = inventory.GetItemAt(slotIndex);
        if (item == null || item.Definition == null)
            return;

        if (button == PointerEventData.InputButton.Right && IsTraderMode)
        {
            activeTraderShop?.TrySellInventoryItem(slotIndex);
            return;
        }

        if (button != PointerEventData.InputButton.Left)
            return;

        switch (item.Definition.Category)
        {
            case ItemCategory.Weapon:
            case ItemCategory.Armor:
                EquipItemFromInventory(slotIndex);
                break;

            case ItemCategory.Consumable:
            case ItemCategory.SkillBook:
                bool used = inventory.UseAt(slotIndex, inventory.gameObject);
                if (!used)
                    GameLog.Warning("Itemul nu a putut fi folosit.");
                break;
        }
    }

    public string GetTooltipExtraTextForSlot(int slotIndex)
    {
        if (!IsTraderMode || activeTraderShop == null || inventory == null)
            return null;

        ItemInstance item = inventory.GetItemAt(slotIndex);
        if (item == null || !item.IsValid)
            return null;

        int sellPrice = activeTraderShop.CalculateSellPrice(item);
        return $"Sell: {sellPrice} Gold\nRight Click: Sell";
    }

    public void HandleEquipmentSlotClicked(EquipmentSlot slot)
    {
        if (equipment == null || inventory == null)
            return;

        ItemInstance removed = equipment.Unequip(slot);
        if (removed == null)
            return;

        bool added = inventory.AddItemInstance(removed);
        if (!added)
        {
            equipment.EquipItem(removed);
            GameLog.Warning("Inventarul este plin.");
        }
    }

    private void EquipItemFromInventory(int slotIndex)
    {
        if (inventory == null || equipment == null)
            return;

        ItemInstance itemToEquip = inventory.TakeAt(slotIndex);
        if (itemToEquip == null || !itemToEquip.IsValid)
            return;

        ItemInstance previous = equipment.EquipItem(itemToEquip);

        if (previous != null && previous.IsValid)
        {
            bool addedBack = inventory.AddItemInstance(previous);
            if (!addedBack)
            {
                equipment.EquipItem(previous);
                inventory.AddItemInstance(itemToEquip);
                GameLog.Warning("Inventarul este plin. Nu se poate face schimbul.");
            }
        }
    }

    private void RefreshEquipment()
    {
        if (equipmentSlots == null || equipment == null)
            return;

        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] == null)
                continue;

            ItemInstance item = equipment.GetItemInSlot(equipmentSlots[i].SlotType);
            equipmentSlots[i].Refresh(item);
        }
    }

    private void RefreshInventory()
    {
        if (inventorySlots == null || inventory == null)
            return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (inventorySlots[i] == null)
                continue;

            ItemInstance item = i < inventory.ItemCount ? inventory.GetItemAt(i) : null;
            inventorySlots[i].Refresh(item);
        }
    }

    private void SetPanelsVisible(bool visible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(visible);

        if (statsPanelRoot != null)
            statsPanelRoot.SetActive(visible);

        if (skillBookPanelRoot != null)
            skillBookPanelRoot.SetActive(visible);
    }

    private void ApplyInventoryLayout(bool traderMode)
    {
        if (inventoryPanelRect == null)
            return;

        inventoryPanelRect.anchoredPosition = traderMode ? traderAnchoredPosition : normalAnchoredPosition;
    }

    private void HideTooltipsAndDrag()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();

        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();

        UISkillDragState.Clear();
    }
}