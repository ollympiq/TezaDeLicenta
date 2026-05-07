using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillCodexSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual State")]
    [SerializeField, Range(0f, 1f)] private float ownedAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float notOwnedAlpha = 0.35f;

    private SkillDefinition skill;

    private void Awake()
    {
        if (iconImage == null)
            iconImage = GetComponent<Image>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Bind(SkillDefinition newSkill, bool isOwned)
    {
        skill = newSkill;

        if (iconImage != null)
        {
            iconImage.enabled = skill != null && skill.Icon != null;
            iconImage.sprite = skill != null ? skill.Icon : null;

            Color color = iconImage.color;
            color.a = isOwned ? ownedAlpha : notOwnedAlpha;
            iconImage.color = color;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isOwned ? ownedAlpha : notOwnedAlpha;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = true;
        }

        gameObject.SetActive(skill != null);
    }

    public void ClearSlot()
    {
        skill = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;

            Color color = iconImage.color;
            color.a = ownedAlpha;
            iconImage.color = color;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = ownedAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (skill == null)
            return;

        if (SkillTooltipUI.Instance != null)
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