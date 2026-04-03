using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterInventory inventory;
    [SerializeField] private CharacterEquipment equipment;
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject panelRoot;

    [Header("UI Slots")]
    [SerializeField] private EquipmentSlotUI[] equipmentSlots;
    [SerializeField] private InventorySlotUI[] inventorySlots;

    [Header("Settings")]
    [SerializeField] private bool startOpened = false;

    private void Start()
    {
        if (inventory == null)
            inventory = FindFirstObjectByType<CharacterInventory>();

        if (equipment == null)
            equipment = FindFirstObjectByType<CharacterEquipment>();

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

        if (panelRoot != null)
            panelRoot.SetActive(startOpened);

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
        if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
            TogglePanel();
    }

    public void TogglePanel()
    {
        if (panelRoot == null)
            return;

        bool willOpen = !panelRoot.activeSelf;
        panelRoot.SetActive(willOpen);

        if (willOpen)
        {
            RefreshAll();
        }
        else
        {
            if (ItemTooltipUI.Instance != null)
                ItemTooltipUI.Instance.Hide();
        }
    }

    public void HandleInventorySlotClicked(int slotIndex)
    {
        if (inventory == null || equipment == null)
            return;

        ItemInstance item = inventory.GetItemAt(slotIndex);
        if (item == null || item.Definition == null)
            return;

        switch (item.Definition.Category)
        {
            case ItemCategory.Weapon:
            case ItemCategory.Armor:
                EquipItemFromInventory(slotIndex);
                break;

            case ItemCategory.Consumable:
                bool used = inventory.UseAt(slotIndex, inventory.gameObject);
                if (!used)
                    Debug.Log("Itemul nu a putut fi folosit.");
                break;
        }
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
            Debug.Log("Inventarul este plin.");
        }
    }

    public void RefreshAll()
    {
        RefreshEquipment();
        RefreshInventory();
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
                Debug.Log("Inventarul este plin. Nu se poate face schimbul.");
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

            ItemInstance item = inventory.GetItemAt(i);
            inventorySlots[i].Refresh(item);
        }
    }
}