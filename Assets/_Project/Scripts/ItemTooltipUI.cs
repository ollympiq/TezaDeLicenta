using System;
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
    private string currentExtraDetails;
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

    public void Show(ItemInstance item, string extraDetails = null)
    {
        if (item == null || item.Definition == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentItem = item;
        currentExtraDetails = extraDetails;

        if (nameText != null)
            nameText.text = UIRichTextColors.Paint(item.DisplayName, UIRichTextColors.RarityColor(item.Rarity));

        if (detailsText != null)
            detailsText.text = BuildDetails(item, currentExtraDetails);

        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        UpdatePosition();
    }

    public void Hide()
    {
        currentItem = null;
        currentExtraDetails = null;

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
    }

    private string BuildDetails(ItemInstance item, string extraDetails)
    {
        if (item == null || item.Definition == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        sb.AppendLine(UIRichTextColors.Line("Rarity", item.Rarity.ToString(), UIRichTextColors.RarityColor(item.Rarity)));
        sb.AppendLine(UIRichTextColors.Line("Category", GetCategoryDisplayName(item.Definition.Category), GetCategoryColor(item.Definition.Category)));
        sb.AppendLine(UIRichTextColors.Line("Item Level", item.ItemLevel.ToString(), UIRichTextColors.Level));

        if (!string.IsNullOrWhiteSpace(item.Definition.Description))
        {
            sb.AppendLine();
            sb.AppendLine(item.Definition.Description);
        }

        AppendWeaponSection(sb, item);
        AppendArmorSection(sb, item);
        AppendPotionSection(sb, item);
        AppendSkillBookSection(sb, item);
        AppendBonusSection(sb, item);

        if (item.StackCount > 1)
        {
            sb.AppendLine();
            sb.AppendLine(UIRichTextColors.Line("Stack", item.StackCount.ToString(), UIRichTextColors.White));
        }
        if (!string.IsNullOrWhiteSpace(extraDetails))
        {
            sb.AppendLine();
            sb.AppendLine(UIRichTextColors.Paint(extraDetails, UIRichTextColors.Gold));
        }
        return sb.ToString().TrimEnd();
    }

    private void AppendWeaponSection(StringBuilder sb, ItemInstance item)
    {
        WeaponDefinition weapon = item.WeaponDefinition;
        if (weapon == null)
            return;

        string dmgColor = UIRichTextColors.DamageTypeColor(weapon.DamageType);

        sb.AppendLine();
        sb.AppendLine(UIRichTextColors.Line("Damage", $"{item.GetWeaponMinDamage()}-{item.GetWeaponMaxDamage()}", dmgColor));
        sb.AppendLine(UIRichTextColors.Line("Damage Type", weapon.DamageType.ToString(), dmgColor));
        sb.AppendLine(UIRichTextColors.Line("Range", $"{weapon.Range:0.0}", UIRichTextColors.White));
        sb.AppendLine(UIRichTextColors.Line("AP Cost", weapon.ApCost.ToString(), UIRichTextColors.AP));
        sb.AppendLine(UIRichTextColors.Line("Weapon Family", weapon.WeaponFamily.ToString(), UIRichTextColors.White));

        string scalingText = BuildWeaponScalingText(weapon);
        if (!string.IsNullOrEmpty(scalingText))
            sb.AppendLine(UIRichTextColors.Line("Scaling", scalingText, UIRichTextColors.White));
    }

    private void AppendArmorSection(StringBuilder sb, ItemInstance item)
    {
        ArmorDefinition armor = item.ArmorDefinition;
        if (armor == null)
            return;

        sb.AppendLine();

        int totalArmor = item.GetArmorValue();
        if (totalArmor > 0)
            sb.AppendLine(UIRichTextColors.Line("Armor", totalArmor.ToString(), UIRichTextColors.Armor));

        AppendResistanceLine(sb, item, ItemBonusType.PhysicalResistance, armor.PhysicalResistance, "Physical Resistance", UIRichTextColors.Physical);
        AppendResistanceLine(sb, item, ItemBonusType.FireResistance, armor.FireResistance, "Fire Resistance", UIRichTextColors.Fire);
        AppendResistanceLine(sb, item, ItemBonusType.EarthResistance, armor.EarthResistance, "Earth Resistance", UIRichTextColors.Earth);
        AppendResistanceLine(sb, item, ItemBonusType.WindResistance, armor.WindResistance, "Wind Resistance", UIRichTextColors.Wind);
        AppendResistanceLine(sb, item, ItemBonusType.LightningResistance, armor.LightningResistance, "Lightning Resistance", UIRichTextColors.Lightning);
        AppendResistanceLine(sb, item, ItemBonusType.IceResistance, armor.IceResistance, "Ice Resistance", UIRichTextColors.Ice);
    }

    private void AppendPotionSection(StringBuilder sb, ItemInstance item)
    {
        PotionDefinition potion = item.PotionDefinition;
        if (potion == null)
            return;

        sb.AppendLine();
        AppendIfNonZero(sb, "Heal", potion.HealAmount, "", UIRichTextColors.HP);
        AppendIfNonZero(sb, "Restore AP", potion.RestoreAP, "", UIRichTextColors.AP);
    }

    private void AppendSkillBookSection(StringBuilder sb, ItemInstance item)
    {
        SkillBookDefinition skillBook = item.SkillBookDefinition;
        if (skillBook == null)
            return;

        sb.AppendLine();

        SkillDefinition skill = skillBook.TaughtSkill;
        if (skill == null)
        {
            sb.AppendLine(UIRichTextColors.Line("Teaches", "No Skill Assigned", UIRichTextColors.Intelligence));
            return;
        }

        string dmgColor = UIRichTextColors.DamageTypeColor(skill.DamageType);

        sb.AppendLine(UIRichTextColors.Line("Teaches", skill.DisplayName, UIRichTextColors.Intelligence));
        sb.AppendLine(UIRichTextColors.Line("Skill Type", skill.SkillType.ToString(), UIRichTextColors.White));
        sb.AppendLine(UIRichTextColors.Line("Targeting", skill.TargetingMode.ToString(), UIRichTextColors.White));
        sb.AppendLine(UIRichTextColors.Line("Area", skill.AreaMode.ToString(), UIRichTextColors.White));
        sb.AppendLine(UIRichTextColors.Line("Damage", $"{skill.MinDamage}-{skill.MaxDamage}", dmgColor));
        sb.AppendLine(UIRichTextColors.Line("Damage Type", skill.DamageType.ToString(), dmgColor));
        sb.AppendLine(UIRichTextColors.Line("Power Scaling", $"x{skill.PowerScaling:0.##}", UIRichTextColors.MagicPower));
        sb.AppendLine(UIRichTextColors.Line("AP Cost", skill.ApCost.ToString(), UIRichTextColors.AP));
        sb.AppendLine(UIRichTextColors.Line("Range", $"{skill.Range:0.0}", UIRichTextColors.White));

        if (skill.AreaRadius > 0f && skill.AreaMode != SkillAreaMode.SingleTarget)
            sb.AppendLine(UIRichTextColors.Line("Area Radius", $"{skill.AreaRadius:0.0}", UIRichTextColors.White));

        if (skill.BonusAccuracy > 0f)
            sb.AppendLine(UIRichTextColors.Line("Bonus Accuracy", $"{skill.BonusAccuracy:0.##}", UIRichTextColors.Accuracy));

        sb.AppendLine(UIRichTextColors.Line("Can Crit", skill.CanCrit ? "Yes" : "No", skill.CanCrit ? UIRichTextColors.Crit : UIRichTextColors.White));
    }

    private void AppendBonusSection(StringBuilder sb, ItemInstance item)
    {
        StringBuilder bonusLines = new StringBuilder();

        Array values = Enum.GetValues(typeof(ItemBonusType));
        for (int i = 0; i < values.Length; i++)
        {
            ItemBonusType bonusType = (ItemBonusType)values.GetValue(i);

            if (IsShownInMainSection(item, bonusType))
                continue;

            float total = GetTotalBonus(item, bonusType);
            if (Mathf.Abs(total) < 0.001f)
                continue;

            string color = UIRichTextColors.BonusTypeColor(bonusType);
            string label = GetBonusDisplayName(bonusType);
            string suffix = IsPercentBonus(bonusType) ? "%" : "";

            bonusLines.AppendLine(UIRichTextColors.Line(label, $"+{total:0.##}{suffix}", color));
        }

        if (bonusLines.Length <= 0)
            return;

        sb.AppendLine();
        sb.AppendLine("Bonuses:");
        sb.Append(bonusLines);
    }

    private void AppendResistanceLine(StringBuilder sb, ItemInstance item, ItemBonusType bonusType, float baseValue, string label, string color)
    {
        float total = baseValue + GetDefinitionModifierTotal(item.Definition, bonusType) + item.GetRolledBonus(bonusType);
        AppendIfNonZero(sb, label, total, "%", color);
    }

    private float GetTotalBonus(ItemInstance item, ItemBonusType bonusType)
    {
        if (item == null || item.Definition == null)
            return 0f;

        return GetDefinitionModifierTotal(item.Definition, bonusType) + item.GetRolledBonus(bonusType);
    }

    private float GetDefinitionModifierTotal(ItemDefinition definition, ItemBonusType bonusType)
    {
        if (definition == null || definition.StatModifiers == null)
            return 0f;

        float total = 0f;
        for (int i = 0; i < definition.StatModifiers.Count; i++)
        {
            ItemStatModifier mod = definition.StatModifiers[i];
            if (mod != null && mod.BonusType == bonusType)
                total += mod.Value;
        }

        return total;
    }

    private bool IsShownInMainSection(ItemInstance item, ItemBonusType bonusType)
    {
        if (item == null)
            return false;

        if (item.ArmorDefinition == null)
            return false;

        switch (bonusType)
        {
            case ItemBonusType.Armor:
            case ItemBonusType.PhysicalResistance:
            case ItemBonusType.FireResistance:
            case ItemBonusType.EarthResistance:
            case ItemBonusType.WindResistance:
            case ItemBonusType.LightningResistance:
            case ItemBonusType.IceResistance:
                return true;

            default:
                return false;
        }
    }

    private bool IsPercentBonus(ItemBonusType bonusType)
    {
        switch (bonusType)
        {
            case ItemBonusType.CritChance:
            case ItemBonusType.PhysicalResistance:
            case ItemBonusType.FireResistance:
            case ItemBonusType.EarthResistance:
            case ItemBonusType.WindResistance:
            case ItemBonusType.LightningResistance:
            case ItemBonusType.IceResistance:
            case ItemBonusType.ElementalDamageBonusPercent:
                return true;

            default:
                return false;
        }
    }

    private string GetBonusDisplayName(ItemBonusType bonusType)
    {
        switch (bonusType)
        {
            case ItemBonusType.MaxHP: return "Max HP";
            case ItemBonusType.MaxAP: return "Max AP";
            case ItemBonusType.PhysicalPower: return "Physical Power";
            case ItemBonusType.MagicPower: return "Magic Power";
            case ItemBonusType.CritChance: return "Crit Chance";
            case ItemBonusType.PhysicalResistance: return "Physical Resistance";
            case ItemBonusType.FireResistance: return "Fire Resistance";
            case ItemBonusType.EarthResistance: return "Earth Resistance";
            case ItemBonusType.WindResistance: return "Wind Resistance";
            case ItemBonusType.LightningResistance: return "Lightning Resistance";
            case ItemBonusType.IceResistance: return "Ice Resistance";
            case ItemBonusType.ElementalDamageBonusPercent: return "Elemental Damage";
            default: return bonusType.ToString();
        }
    }

    private string GetCategoryDisplayName(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.SkillBook: return "Skill Book";
            default: return category.ToString();
        }
    }

    private string GetCategoryColor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.SkillBook:
                return UIRichTextColors.Intelligence;

            default:
                return UIRichTextColors.CategoryColor(category);
        }
    }

    private void AppendIfNonZero(StringBuilder sb, string label, float value, string suffix, string color)
    {
        if (Mathf.Abs(value) < 0.001f)
            return;

        sb.AppendLine(UIRichTextColors.Line(label, $"{value:0.##}{suffix}", color));
    }

    private string BuildWeaponScalingText(WeaponDefinition weapon)
    {
        if (weapon == null || weapon.Scaling == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();
        AppendScaling(sb, "STR", weapon.Scaling.StrengthScale, UIRichTextColors.Strength);
        AppendScaling(sb, "CON", weapon.Scaling.ConstitutionScale, UIRichTextColors.Constitution);
        AppendScaling(sb, "DEX", weapon.Scaling.DexterityScale, UIRichTextColors.Dexterity);
        AppendScaling(sb, "INT", weapon.Scaling.IntelligenceScale, UIRichTextColors.Intelligence);
        return sb.ToString().Trim();
    }

    private void AppendScaling(StringBuilder sb, string shortName, float value, string color)
    {
        if (value <= 0f)
            return;

        if (sb.Length > 0)
            sb.Append(" | ");

        sb.Append(UIRichTextColors.Paint(shortName, color));
        sb.Append(" ");
        sb.Append(UIRichTextColors.Paint($"x{value:0.##}", color));
    }

    private void UpdatePosition()
    {
        if (Mouse.current == null || panelRoot == null || rootCanvas == null)
            return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouseScreenPos, uiCamera, out Vector2 localMousePos))
            return;

        float panelWidth = panelRoot.rect.width;
        float panelHeight = panelRoot.rect.height;
        float canvasHalfWidth = canvasRect.rect.width * 0.5f;
        float canvasHalfHeight = canvasRect.rect.height * 0.5f;

        bool placeRight = mouseScreenPos.x + cursorOffset.x + panelWidth + screenPadding <= Screen.width;
        bool placeBelow = mouseScreenPos.y - cursorOffset.y - panelHeight - screenPadding >= 0f;

        panelRoot.anchorMin = panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
        panelRoot.pivot = new Vector2(placeRight ? 0f : 1f, placeBelow ? 1f : 0f);

        Vector2 offset = new Vector2(
            placeRight ? cursorOffset.x : -cursorOffset.x,
            placeBelow ? -cursorOffset.y : cursorOffset.y
        );

        Vector2 anchoredPos = localMousePos + offset;

        float minX = -canvasHalfWidth + screenPadding + panelWidth * panelRoot.pivot.x;
        float maxX = canvasHalfWidth - screenPadding - panelWidth * (1f - panelRoot.pivot.x);
        float minY = -canvasHalfHeight + screenPadding + panelHeight * panelRoot.pivot.y;
        float maxY = canvasHalfHeight - screenPadding - panelHeight * (1f - panelRoot.pivot.y);

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        panelRoot.anchoredPosition = anchoredPos;
    }
}