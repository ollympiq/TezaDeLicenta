using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI detailsText;

    [Header("Position")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(18f, 18f);
    [SerializeField] private float screenPadding = 12f;

    private ItemInstance currentItem;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        Hide();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.gameObject.activeSelf || currentItem == null)
            return;

        UpdatePosition();
    }

    public void Show(ItemInstance item)
    {
        if (item == null || item.Definition == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentItem = item;

        if (nameText != null)
            nameText.text = item.DisplayName;

        if (detailsText != null)
            detailsText.text = BuildDetails(item);

        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        UpdatePosition();
    }

    public void Hide()
    {
        currentItem = null;

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
    }

    private string BuildDetails(ItemInstance item)
    {
        if (item == null || item.Definition == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"Rarity: {item.Rarity}");
        sb.AppendLine($"Category: {item.Definition.Category}");

        if (!string.IsNullOrWhiteSpace(item.Definition.Description))
        {
            sb.AppendLine();
            sb.AppendLine(item.Definition.Description);
        }

        WeaponDefinition weapon = item.WeaponDefinition;
        if (weapon != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Damage: {weapon.MinDamage}-{weapon.MaxDamage}");
            sb.AppendLine($"Damage Type: {weapon.DamageType}");
            sb.AppendLine($"Range: {weapon.Range:0.0}");
            sb.AppendLine($"AP Cost: {weapon.ApCost}");
            sb.AppendLine($"Weapon Family: {weapon.WeaponFamily}");

            string scalingText = BuildScalingText(weapon);
            if (!string.IsNullOrEmpty(scalingText))
                sb.AppendLine($"Scaling: {scalingText}");
        }

        ArmorDefinition armor = item.ArmorDefinition;
        if (armor != null)
        {
            sb.AppendLine();
            sb.AppendLine($"Armor: {armor.ArmorValue}");

            AppendIfNonZero(sb, "Physical Resistance", armor.PhysicalResistance, "%");
            AppendIfNonZero(sb, "Fire Resistance", armor.FireResistance, "%");
            AppendIfNonZero(sb, "Earth Resistance", armor.EarthResistance, "%");
            AppendIfNonZero(sb, "Wind Resistance", armor.WindResistance, "%");
            AppendIfNonZero(sb, "Lightning Resistance", armor.LightningResistance, "%");
            AppendIfNonZero(sb, "Ice Resistance", armor.IceResistance, "%");
        }

        PotionDefinition potion = item.PotionDefinition;
        if (potion != null)
        {
            sb.AppendLine();
            AppendIfNonZero(sb, "Heal", potion.HealAmount, "");
            AppendIfNonZero(sb, "Restore AP", potion.RestoreAP, "");
        }

        var modifiers = item.Definition.StatModifiers;
        if (modifiers != null && modifiers.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Bonuses:");

            for (int i = 0; i < modifiers.Count; i++)
            {
                ItemStatModifier mod = modifiers[i];
                if (mod == null)
                    continue;

                sb.AppendLine($"+{mod.Value:0.##} {mod.BonusType}");
            }
        }

        if (item.StackCount > 1)
        {
            sb.AppendLine();
            sb.AppendLine($"Stack: {item.StackCount}");
        }

        return sb.ToString();
    }

    private string BuildScalingText(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.Scaling == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        AppendScaling(sb, "STR", weapon.Scaling.StrengthScale);
        AppendScaling(sb, "CON", weapon.Scaling.ConstitutionScale);
        AppendScaling(sb, "DEX", weapon.Scaling.DexterityScale);
        AppendScaling(sb, "INT", weapon.Scaling.IntelligenceScale);

        return sb.ToString().Trim();
    }

    private void AppendScaling(StringBuilder sb, string shortName, float value)
    {
        if (value <= 0f)
            return;

        if (sb.Length > 0)
            sb.Append(" | ");

        sb.Append($"{shortName} x{value:0.##}");
    }

    private void AppendIfNonZero(StringBuilder sb, string label, float value, string suffix)
    {
        if (Mathf.Abs(value) < 0.001f)
            return;

        sb.AppendLine($"{label}: {value:0.##}{suffix}");
    }

    private void UpdatePosition()
    {
        if (Mouse.current == null || panelRoot == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 desired = mousePos + cursorOffset;

        float tooltipWidth = panelRoot.rect.width;
        float tooltipHeight = panelRoot.rect.height;

        if (desired.x + tooltipWidth + screenPadding > Screen.width)
            desired.x = mousePos.x - tooltipWidth - cursorOffset.x;

        if (desired.y - tooltipHeight - screenPadding < 0f)
            desired.y = mousePos.y + tooltipHeight + cursorOffset.y;

        desired.x = Mathf.Clamp(desired.x, screenPadding, Screen.width - tooltipWidth - screenPadding);
        desired.y = Mathf.Clamp(desired.y, tooltipHeight + screenPadding, Screen.height - screenPadding);

        panelRoot.position = desired;
    }
}