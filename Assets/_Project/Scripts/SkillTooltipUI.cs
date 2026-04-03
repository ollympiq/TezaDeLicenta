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
            nameText.text = currentSkill.DisplayName;

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
        WeaponDefinition weapon = previewCasterEquipment != null
            ? previewCasterEquipment.EquippedWeaponDefinition
            : null;

        if (previewCasterStats == null || weapon == null)
            return BuildFallbackSkillDetails(fallbackSkill);

        DamagePreviewUtility.TryBuildWeaponPreview(previewCasterStats, weapon, out DamagePreviewInfo preview);

        return
     $"AP Cost: {weapon.ApCost}\n" +
     $"Damage Type: {weapon.DamageType}\n" +
     $"Damage: {preview.MinPreview}-{preview.MaxPreview}\n" +
     $"Range: {weapon.Range:0.0}\n" +
     $"Area Radius: -";
    }

    private string BuildSkillDetails(SkillDefinition skill)
    {
        string areaText = skill.AreaMode == SkillAreaMode.Circle
            ? $"Area Radius: {skill.AreaRadius:0.0}"
            : "Area Radius: -";

        if (previewCasterStats == null)
            return BuildFallbackSkillDetails(skill);

        DamagePreviewUtility.TryBuildSkillPreview(previewCasterStats, skill, out DamagePreviewInfo preview);

        return
    $"AP Cost: {skill.ApCost}\n" +
    $"Damage Type: {skill.DamageType}\n" +
    $"Damage: {preview.MinPreview}-{preview.MaxPreview}\n" +
    $"Range: {skill.Range:0.0}\n" +
    $"{areaText}";
    }

    private string BuildFallbackSkillDetails(SkillDefinition skill)
    {
        string areaText = skill.AreaMode == SkillAreaMode.Circle
            ? $"Area Radius: {skill.AreaRadius:0.0}"
            : "Area Radius: -";

        return
    $"AP Cost: {skill.ApCost}\n" +
    $"Damage Type: {skill.DamageType}\n" +
    $"Damage: {skill.MinDamage}-{skill.MaxDamage}\n" +
    $"Range: {skill.Range:0.0}\n" +
    $"{areaText}";
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