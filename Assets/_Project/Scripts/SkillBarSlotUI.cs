using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillBarSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedFrame;
    [SerializeField] private TextMeshProUGUI slotIndexText;

    private SkillBarUI owner;
    private PlayerSkillLoadout loadout;
    private int slotIndex;

    public void Setup(SkillBarUI newOwner, PlayerSkillLoadout newLoadout, int newSlotIndex)
    {
        owner = newOwner;
        loadout = newLoadout;
        slotIndex = newSlotIndex;

        if (slotIndexText != null)
            slotIndexText.text = (slotIndex + 1).ToString();
    }

    public void Refresh(SkillDefinition skill, bool isSelected)
    {
        if (iconImage != null)
        {
            iconImage.enabled = skill != null && skill.Icon != null;
            iconImage.sprite = skill != null ? skill.Icon : null;
        }

        if (selectedFrame != null)
            selectedFrame.enabled = isSelected;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || loadout == null)
            return;

        if (UISkillDragState.CurrentSkill == null)
            return;

        bool assigned = loadout.AssignSkillToSlot(UISkillDragState.CurrentSkill, slotIndex);

        UISkillDragState.Clear();

        if (assigned)
            owner.RefreshNow();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        owner?.HandleSlotClicked(slotIndex);
    }
}