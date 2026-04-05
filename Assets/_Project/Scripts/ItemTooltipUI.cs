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

        if (nameText != null)
            nameText.text = UIRichTextColors.Paint(item.DisplayName, UIRichTextColors.RarityColor(item.Rarity));

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

        sb.AppendLine(UIRichTextColors.Line(
            "Rarity",
            item.Rarity.ToString(),
            UIRichTextColors.RarityColor(item.Rarity)));

        sb.AppendLine(UIRichTextColors.Line(
            "Category",
            item.Definition.Category.ToString(),
            UIRichTextColors.CategoryColor(item.Definition.Category)));

        if (!string.IsNullOrWhiteSpace(item.Definition.Description))
        {
            sb.AppendLine();
            sb.AppendLine(item.Definition.Description);
        }

        WeaponDefinition weapon = item.WeaponDefinition;
        if (weapon != null)
        {
            string dmgColor = UIRichTextColors.DamageTypeColor(weapon.DamageType);

            sb.AppendLine();
            sb.AppendLine(UIRichTextColors.Line(
                "Damage",
                $"{weapon.MinDamage}-{weapon.MaxDamage}",
                dmgColor));

            sb.AppendLine(UIRichTextColors.Line(
                "Damage Type",
                weapon.DamageType.ToString(),
                dmgColor));

            sb.AppendLine(UIRichTextColors.Line(
                "Range",
                $"{weapon.Range:0.0}",
                UIRichTextColors.White));

            sb.AppendLine(UIRichTextColors.Line(
                "AP Cost",
                weapon.ApCost.ToString(),
                UIRichTextColors.AP));

            sb.AppendLine(UIRichTextColors.Line(
                "Weapon Family",
                weapon.WeaponFamily.ToString(),
                UIRichTextColors.White));

            string scalingText = BuildScalingText(weapon);
            if (!string.IsNullOrEmpty(scalingText))
            {
                sb.AppendLine(UIRichTextColors.Line(
                    "Scaling",
                    scalingText,
                    UIRichTextColors.White));
            }
        }

        ArmorDefinition armor = item.ArmorDefinition;
        if (armor != null)
        {
            sb.AppendLine();
            sb.AppendLine(UIRichTextColors.Line(
                "Armor",
                armor.ArmorValue.ToString(),
                UIRichTextColors.Armor));

            AppendIfNonZero(sb, "Physical Resistance", armor.PhysicalResistance, "%", UIRichTextColors.Physical);
            AppendIfNonZero(sb, "Fire Resistance", armor.FireResistance, "%", UIRichTextColors.Fire);
            AppendIfNonZero(sb, "Earth Resistance", armor.EarthResistance, "%", UIRichTextColors.Earth);
            AppendIfNonZero(sb, "Wind Resistance", armor.WindResistance, "%", UIRichTextColors.Wind);
            AppendIfNonZero(sb, "Lightning Resistance", armor.LightningResistance, "%", UIRichTextColors.Lightning);
            AppendIfNonZero(sb, "Ice Resistance", armor.IceResistance, "%", UIRichTextColors.Ice);
        }

        PotionDefinition potion = item.PotionDefinition;
        if (potion != null)
        {
            sb.AppendLine();
            AppendIfNonZero(sb, "Heal", potion.HealAmount, "", UIRichTextColors.HP);
            AppendIfNonZero(sb, "Restore AP", potion.RestoreAP, "", UIRichTextColors.AP);
        }

        var modifiers = item.Definition.StatModifiers;
        if (modifiers != null && modifiers.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Bonuses:");

            for (int i = 0; i < modifiers.Count; i++)
            {
                ItemStatModifier mod = modifiers[i];
                if (mod == null) continue;

                string color = UIRichTextColors.BonusTypeColor(mod.BonusType);
                sb.AppendLine(UIRichTextColors.Paint($"+{mod.Value:0.##} {mod.BonusType}", color));
            }
        }

        if (item.StackCount > 1)
        {
            sb.AppendLine();
            sb.AppendLine(UIRichTextColors.Line(
                "Stack",
                item.StackCount.ToString(),
                UIRichTextColors.White));
        }

        return sb.ToString();
    }

    private void AppendIfNonZero(StringBuilder sb, string label, float value, string suffix, string color)
    {
        if (Mathf.Abs(value) < 0.001f) return;
        sb.AppendLine(UIRichTextColors.Line(label, $"{value:0.##}{suffix}", color));
    }

    private string BuildScalingText(WeaponDefinition weapon)
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

    private void AppendIfNonZero(StringBuilder sb, string label, float value, string suffix)
    {
        if (Mathf.Abs(value) < 0.001f)
            return;

        sb.AppendLine($"{label}: {value:0.##}{suffix}");
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
        Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mouseScreenPos,
                uiCamera,
                out Vector2 localMousePos))
        {
            return;
        }

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