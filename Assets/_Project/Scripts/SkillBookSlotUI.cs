using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillBookSlotUI : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private SkillDefinition skill;
    private Canvas rootCanvas;

    public void Bind(SkillDefinition newSkill, Canvas canvasRoot)
    {
        skill = newSkill;
        rootCanvas = canvasRoot;

        if (iconImage != null)
        {
            iconImage.enabled = skill != null && skill.Icon != null;
            iconImage.sprite = skill != null ? skill.Icon : null;
        }
    }

    public void ClearSlot()
    {
        skill = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skill == null || rootCanvas == null)
            return;

        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();

        UISkillDragState.BeginDrag(skill, rootCanvas, skill.Icon, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UISkillDragState.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UISkillDragState.Clear();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill != null && SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Show(skill);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }

    private void OnDisable()
    {
        if (SkillTooltipUI.Instance != null)
            SkillTooltipUI.Instance.Hide();
    }
}