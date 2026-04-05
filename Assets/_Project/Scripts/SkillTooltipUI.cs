using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SkillTooltipUI : MonoBehaviour
{
    public static SkillTooltipUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI detailsText;

    [Header("Preview Source")]
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("Position")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(18f, 18f);
    [SerializeField] private float screenPadding = 12f;

    private SkillDefinition currentSkill;
    private CharacterStats previewCasterStats;
    private CharacterEquipment previewCasterEquipment;

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

        ResolvePreviewReferences();
        Hide();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.gameObject.activeSelf || currentSkill == null)
            return;

        ResolvePreviewReferences();
        RefreshCurrentTooltip();
        UpdatePosition();
    }

    public void Show(SkillDefinition skill)
    {
        if (skill == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentSkill = skill;
        ResolvePreviewReferences();
        RefreshCurrentTooltip();

        panelRoot.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        panelRoot.SetAsLastSibling();
        UpdatePosition();
    }

    public void Hide()
    {
        currentSkill = null;

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
    }

    private void ResolvePreviewReferences()
    {
        if (playerCombatController == null)
            playerCombatController = FindFirstObjectByType<PlayerCombatController>();

        if (playerCombatController != null)
        {
            if (previewCasterStats == null)
                previewCasterStats = playerCombatController.GetComponent<CharacterStats>();

            if (previewCasterEquipment == null)
                previewCasterEquipment = playerCombatController.GetComponent<CharacterEquipment>();
        }
    }

    private void RefreshCurrentTooltip()
    {
        if (currentSkill == null)
            return;

        if (nameText != null)
        {
            string titleColor = currentSkill.SkillType == SkillType.Passive
                ? UIRichTextColors.MagicPower
                : UIRichTextColors.DamageTypeColor(currentSkill.DamageType);

            nameText.text = UIRichTextColors.Paint(currentSkill.DisplayName, titleColor);
        }

        if (detailsText != null)
            detailsText.text = BuildDetails(currentSkill);
    }

    private string BuildDetails(SkillDefinition skill)
    {
        if (skill == null)
            return string.Empty;

        return skill.SkillType == SkillType.BasicAttack
            ? BuildBasicAttackDetails(skill)
            : BuildSkillDetails(skill);
    }

    private string BuildBasicAttackDetails(SkillDefinition fallbackSkill)
    {
        WeaponDefinition weapon = previewCasterEquipment != null ? previewCasterEquipment.EquippedWeaponDefinition : null;
        if (previewCasterStats == null || weapon == null)
            return BuildFallbackSkillDetails(fallbackSkill);

        DamagePreviewUtility.TryBuildWeaponPreview(previewCasterStats, weapon, out DamagePreviewInfo preview);
        string dmgColor = UIRichTextColors.DamageTypeColor(weapon.DamageType);

        return
            UIRichTextColors.Line("AP Cost", weapon.ApCost.ToString(), UIRichTextColors.AP) + "\n" +
            UIRichTextColors.Line("Damage Type", weapon.DamageType.ToString(), dmgColor) + "\n" +
            UIRichTextColors.Line("Damage", $"{preview.MinPreview}-{preview.MaxPreview}", dmgColor) + "\n" +
            UIRichTextColors.Line("Range", $"{weapon.Range:0.0}", UIRichTextColors.White) + "\n" +
            UIRichTextColors.Line("Area Radius", "-", UIRichTextColors.White);
    }

    private string BuildSkillDetails(SkillDefinition skill)
    {
        string areaText = skill.AreaMode == SkillAreaMode.Circle
            ? UIRichTextColors.Line("Area Radius", $"{skill.AreaRadius:0.0}", UIRichTextColors.White)
            : UIRichTextColors.Line("Area Radius", "-", UIRichTextColors.White);

        if (previewCasterStats == null)
            return BuildFallbackSkillDetails(skill);

        DamagePreviewUtility.TryBuildSkillPreview(previewCasterStats, skill, out DamagePreviewInfo preview);
        string dmgColor = UIRichTextColors.DamageTypeColor(skill.DamageType);

        return
            UIRichTextColors.Line("AP Cost", skill.ApCost.ToString(), UIRichTextColors.AP) + "\n" +
            UIRichTextColors.Line("Damage Type", skill.DamageType.ToString(), dmgColor) + "\n" +
            UIRichTextColors.Line("Damage", $"{preview.MinPreview}-{preview.MaxPreview}", dmgColor) + "\n" +
            UIRichTextColors.Line("Range", $"{skill.Range:0.0}", UIRichTextColors.White) + "\n" +
            areaText;
    }

    private string BuildFallbackSkillDetails(SkillDefinition skill)
    {
        string areaText = skill.AreaMode == SkillAreaMode.Circle
            ? UIRichTextColors.Line("Area Radius", $"{skill.AreaRadius:0.0}", UIRichTextColors.White)
            : UIRichTextColors.Line("Area Radius", "-", UIRichTextColors.White);

        string dmgColor = UIRichTextColors.DamageTypeColor(skill.DamageType);

        return
            UIRichTextColors.Line("AP Cost", skill.ApCost.ToString(), UIRichTextColors.AP) + "\n" +
            UIRichTextColors.Line("Damage Type", skill.DamageType.ToString(), dmgColor) + "\n" +
            UIRichTextColors.Line("Damage", $"{skill.MinDamage}-{skill.MaxDamage}", dmgColor) + "\n" +
            UIRichTextColors.Line("Range", $"{skill.Range:0.0}", UIRichTextColors.White) + "\n" +
            areaText;
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