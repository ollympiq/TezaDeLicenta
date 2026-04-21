using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TraderShopSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private TraderShopUI owner;
    private int slotIndex;
    private TraderShopStockEntry currentEntry;

    public void Setup(TraderShopUI shopUI, int index)
    {
        owner = shopUI;
        slotIndex = index;
    }

    public void Refresh(TraderShopStockEntry entry)
    {
        currentEntry = entry;

        ItemInstance item = entry != null ? entry.Item : null;

        if (iconImage != null)
        {
            iconImage.enabled = item != null && item.Icon != null;
            iconImage.sprite = item != null ? item.Icon : null;
        }
    }

    public void ClearSlot()
    {
        Refresh(null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleTraderSlotClicked(slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentEntry == null || !currentEntry.IsValid || ItemTooltipUI.Instance == null)
            return;

        ItemTooltipUI.Instance.Show(currentEntry.Item, $"Cost: {currentEntry.BuyPrice} Gold");
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