using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackText;

    private InventoryUI owner;
    private int slotIndex;
    private ItemInstance currentItem;

    public void Setup(InventoryUI inventoryUI, int index)
    {
        owner = inventoryUI;
        slotIndex = index;
    }

    public void Refresh(ItemInstance item)
    {
        currentItem = item;

        if (iconImage != null)
        {
            iconImage.enabled = item != null && item.Icon != null;
            iconImage.sprite = item != null ? item.Icon : null;
        }

        if (stackText != null)
        {
            if (item != null && item.StackCount > 1)
                stackText.text = item.StackCount.ToString();
            else
                stackText.text = string.Empty;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        owner?.HandleInventorySlotClicked(slotIndex, eventData.button);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem == null || ItemTooltipUI.Instance == null)
            return;

        string extraText = owner != null ? owner.GetTooltipExtraTextForSlot(slotIndex) : null;

        if (string.IsNullOrWhiteSpace(extraText))
            ItemTooltipUI.Instance.Show(currentItem);
        else
            ItemTooltipUI.Instance.Show(currentItem, extraText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }
}