using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EnemyLootSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackText;

    private EnemyLootUI owner;
    private int slotIndex;
    private ItemInstance currentItem;

    public void Setup(EnemyLootUI lootUI, int index)
    {
        owner = lootUI;
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

    public void ClearSlot()
    {
        currentItem = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (stackText != null)
            stackText.text = string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleLootSlotClicked(slotIndex);
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

    private void OnDisable()
    {
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }
}