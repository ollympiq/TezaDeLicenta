using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillBarSlotUI : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image selectedFrame;
    [SerializeField] private TextMeshProUGUI slotIndexText;

    [Header("Cooldown UI")]
    [SerializeField] private Image cooldownOverlay;
    [SerializeField] private TextMeshProUGUI cooldownText;

    private SkillBarUI owner;
    private PlayerSkillLoadout loadout;
    private int slotIndex;
    private SkillDefinition currentSkill;

    public void Setup(SkillBarUI newOwner, PlayerSkillLoadout newLoadout, int newSlotIndex)
    {
        owner = newOwner;
        loadout = newLoadout;
        slotIndex = newSlotIndex;

        if (slotIndexText != null)
            slotIndexText.text = (slotIndex + 1).ToString();

        SetCooldownVisible(false, 0);
    }

    public void Refresh(SkillDefinition skill, bool isSelected, int cooldownRemaining)
    {
        currentSkill = skill;

        if (iconImage != null)
        {
            iconImage.enabled = skill != null && skill.Icon != null;
            iconImage.sprite = skill != null ? skill.Icon : null;
        }

        if (selectedFrame != null)
            selectedFrame.enabled = isSelected;

        SetCooldownVisible(skill != null && cooldownRemaining > 0, cooldownRemaining);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null)
        {
            UISkillDragState.Clear();
            return;
        }

        SkillDefinition draggedSkill = UISkillDragState.CurrentSkill;

        if (draggedSkill == null)
        {
            UISkillDragState.Clear();
            return;
        }

        bool assigned = owner.TryAssignSkillFromSkillBook(draggedSkill, slotIndex);

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentSkill != null && SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Show(currentSkill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }

    private void SetCooldownVisible(bool visible, int cooldownRemaining)
    {
        if (cooldownOverlay != null)
            cooldownOverlay.gameObject.SetActive(visible);

        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(visible);
            cooldownText.text = visible ? cooldownRemaining.ToString() : string.Empty;
        }
    }
}