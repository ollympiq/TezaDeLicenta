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

    [Header("Position")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(18f, 18f);
    [SerializeField] private float screenPadding = 12f;

    private SkillDefinition currentSkill;

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

        Hide();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.gameObject.activeSelf || currentSkill == null)
            return;

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

        if (nameText != null)
            nameText.text = skill.DisplayName;

        if (detailsText != null)
            detailsText.text = BuildDetails(skill);

        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);

        UpdatePosition();
    }

    public void Hide()
    {
        currentSkill = null;

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
    }

    private string BuildDetails(SkillDefinition skill)
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

        // Pentru pivot stanga-sus:
        // X = marginea stanga a tooltipului
        // Y = marginea de sus a tooltipului
        Vector2 desired = mousePos + cursorOffset;

        float tooltipWidth = panelRoot.rect.width;
        float tooltipHeight = panelRoot.rect.height;

        // Daca iese in dreapta, il mutam in stanga cursorului
        if (desired.x + tooltipWidth + screenPadding > Screen.width)
            desired.x = mousePos.x - tooltipWidth - cursorOffset.x;

        // Daca iese jos, il mutam deasupra cursorului
        if (desired.y - tooltipHeight - screenPadding < 0f)
            desired.y = mousePos.y + tooltipHeight + cursorOffset.y;

        // Clamp final
        desired.x = Mathf.Clamp(desired.x, screenPadding, Screen.width - tooltipWidth - screenPadding);
        desired.y = Mathf.Clamp(desired.y, tooltipHeight + screenPadding, Screen.height - screenPadding);

        panelRoot.position = desired;
    }
}