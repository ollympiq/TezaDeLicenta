using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClassSelectionSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotContentType
    {
        Empty = 0,
        Weapon = 1,
        Skill = 2
    }

    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual")]
    [SerializeField] private float emptyAlpha = 0.15f;
    [SerializeField] private float filledAlpha = 1f;

    private SlotContentType contentType = SlotContentType.Empty;
    private WeaponDefinition weapon;
    private SkillDefinition skill;

    private void Awake()
    {
        ResolveReferences();

        // IMPORTANT:
        // Nu chemam Clear() aici.
        // Altfel, la Play, slotul poate sterge iconurile dupa ce cardul deja le-a setat.
    }

    private void OnDisable()
    {
        if (ClassSelectionTooltipUI.Instance != null)
            ClassSelectionTooltipUI.Instance.Hide();
    }

    public void BindWeapon(WeaponDefinition weaponDefinition)
    {
        ResolveReferences();

        contentType = weaponDefinition != null ? SlotContentType.Weapon : SlotContentType.Empty;
        weapon = weaponDefinition;
        skill = null;

        if (iconImage != null)
        {
            iconImage.sprite = weaponDefinition != null ? weaponDefinition.Icon : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        SetAlpha(weaponDefinition != null ? filledAlpha : emptyAlpha);
    }

    public void BindSkill(SkillDefinition skillDefinition)
    {
        ResolveReferences();

        contentType = skillDefinition != null ? SlotContentType.Skill : SlotContentType.Empty;
        skill = skillDefinition;
        weapon = null;

        if (iconImage != null)
        {
            iconImage.sprite = skillDefinition != null ? skillDefinition.Icon : null;
            iconImage.enabled = iconImage.sprite != null;
            iconImage.preserveAspect = true;
        }

        SetAlpha(skillDefinition != null ? filledAlpha : emptyAlpha);
    }

    public void Clear()
    {
        ResolveReferences();

        contentType = SlotContentType.Empty;
        weapon = null;
        skill = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        SetAlpha(emptyAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        string tooltip = BuildTooltip();

        if (!string.IsNullOrWhiteSpace(tooltip) && ClassSelectionTooltipUI.Instance != null)
            ClassSelectionTooltipUI.Instance.Show(tooltip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ClassSelectionTooltipUI.Instance != null)
            ClassSelectionTooltipUI.Instance.Hide();
    }

    private string BuildTooltip()
    {
        switch (contentType)
        {
            case SlotContentType.Weapon:
                return BuildWeaponTooltip();

            case SlotContentType.Skill:
                return SkillTooltipTextBuilder.Build(skill);

            default:
                return string.Empty;
        }
    }

    private string BuildWeaponTooltip()
    {
        if (weapon == null)
            return string.Empty;

        StringBuilder builder = new StringBuilder();

        builder.AppendLine($"<color=#FFD166><b>{weapon.DisplayName}</b></color>");
        builder.AppendLine($"<color=#FFFFFF>Weapon Type:</color> <color=#A8DADC>{weapon.WeaponFamily}</color>");
        builder.AppendLine($"<color=#FFFFFF>Damage Type:</color> <color=#FFDD66>{weapon.DamageType}</color>");
        builder.AppendLine($"<color=#FFFFFF>Damage:</color> <color=#FF6B6B>{weapon.MinDamage}-{weapon.MaxDamage}</color>");
        builder.AppendLine($"<color=#FFFFFF>Range:</color> {weapon.Range:0.##}");
        builder.AppendLine($"<color=#FFFFFF>AP Cost:</color> <color=#4CC9F0>{weapon.ApCost}</color>");

        if (weapon.BonusAccuracy > 0f)
            builder.AppendLine($"<color=#FFFFFF>Accuracy:</color> +{weapon.BonusAccuracy:0.#}%");

        builder.AppendLine($"<color=#FFFFFF>Can Crit:</color> {(weapon.CanCrit ? "Yes" : "No")}");

        return builder.ToString().TrimEnd();
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (iconImage != null)
        {
            Color color = iconImage.color;
            color.a = alpha;
            iconImage.color = color;
        }
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            Transform iconTransform = transform.Find("Icon");

            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();

            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>(true);
        }

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }
}