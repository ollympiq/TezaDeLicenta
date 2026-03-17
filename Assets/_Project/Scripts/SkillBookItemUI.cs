using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillBookItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;

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

        if (nameText != null)
            nameText.text = skill != null ? skill.DisplayName : "Empty";
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (skill == null || rootCanvas == null)
            return;

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
}