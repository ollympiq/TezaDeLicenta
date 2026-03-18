using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private EquipmentSlot equipmentSlot = EquipmentSlot.None;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI slotLabelText;

    private InventoryUI owner;
    private ItemInstance currentItem;

    public EquipmentSlot SlotType => equipmentSlot;

    public void Setup(InventoryUI inventoryUI)
    {
        owner = inventoryUI;
    }

    public void Refresh(ItemInstance item)
    {
        currentItem = item;

        if (iconImage != null)
        {
            iconImage.enabled = item != null && item.Icon != null;
            iconImage.sprite = item != null ? item.Icon : null;
        }

        if (slotLabelText != null)
            slotLabelText.text = GetShortSlotLabel(equipmentSlot);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleEquipmentSlotClicked(equipmentSlot);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null && ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Show(currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private string GetShortSlotLabel(EquipmentSlot slot)
    {
        switch (slot)
        {
            case EquipmentSlot.Weapon: return "W";
            case EquipmentSlot.Head: return "H";
            case EquipmentSlot.Chest: return "C";
            case EquipmentSlot.Hands: return "G";
            case EquipmentSlot.Legs: return "L";
            case EquipmentSlot.Feet: return "F";
            case EquipmentSlot.Ring: return "R";
            case EquipmentSlot.Amulet: return "A";
            default: return "?";
        }
    }
}