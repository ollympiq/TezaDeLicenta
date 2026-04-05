using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EnemyStatsTooltipUI : MonoBehaviour
{
    public static EnemyStatsTooltipUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI detailsText;

    [Header("Position")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(18f, 18f);
    [SerializeField] private float screenPadding = 12f;

    private CharacterStats currentStats;
    private CharacterHealth currentHealth;

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
        if (panelRoot == null || !panelRoot.gameObject.activeSelf || currentStats == null)
            return;

        RefreshCurrentTooltip();
        UpdatePosition();
    }

    public void Show(CharacterStats stats, CharacterHealth health = null)
    {
        if (stats == null || panelRoot == null)
        {
            Hide();
            return;
        }

        currentStats = stats;
        currentHealth = health != null ? health : stats.GetComponent<CharacterHealth>();

        RefreshCurrentTooltip();

        panelRoot.gameObject.SetActive(true);
        panelRoot.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        UpdatePosition();
    }

    public void Hide()
    {
        currentStats = null;
        currentHealth = null;

        if (panelRoot != null)
            panelRoot.gameObject.SetActive(false);
    }

    private void RefreshCurrentTooltip()
    {
        if (currentStats == null)
            return;

        if (nameText != null)
            nameText.text = currentStats.gameObject.name;

        if (detailsText != null)
            detailsText.text = BuildDetails(currentStats, currentHealth);
    }

    private string BuildDetails(CharacterStats stats, CharacterHealth health)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(UIRichTextColors.DualLine("Class", stats.Class.ToString(), UIRichTextColors.White, UIRichTextColors.ClassColor(stats.Class)));

        if (health != null)
            sb.AppendLine(UIRichTextColors.Line("HP", $"{health.CurrentHP}/{health.MaxHP}", UIRichTextColors.HP));
        else
            sb.AppendLine(UIRichTextColors.Line("HP", $"{stats.MaxHP}/{stats.MaxHP}", UIRichTextColors.HP));

        sb.AppendLine(UIRichTextColors.Line("AP", $"{stats.MaxAP}", UIRichTextColors.AP));

        sb.AppendLine();
        sb.AppendLine(UIRichTextColors.Line("Strength", $"{stats.Strength}", UIRichTextColors.Strength));
        sb.AppendLine(UIRichTextColors.Line("Constitution", $"{stats.Constitution}", UIRichTextColors.Constitution));
        sb.AppendLine(UIRichTextColors.Line("Dexterity", $"{stats.Dexterity}", UIRichTextColors.Dexterity));
        sb.AppendLine(UIRichTextColors.Line("Intelligence", $"{stats.Intelligence}", UIRichTextColors.Intelligence));

        sb.AppendLine();
        sb.AppendLine(UIRichTextColors.Line("Physical Power", $"{stats.PhysicalPower}", UIRichTextColors.PhysicalPower));
        sb.AppendLine(UIRichTextColors.Line("Magic Power", $"{stats.MagicPower}", UIRichTextColors.MagicPower));
        sb.AppendLine(UIRichTextColors.Line("Crit Chance", $"{stats.CritChance:F1}%", UIRichTextColors.Crit));
        sb.AppendLine(UIRichTextColors.Line("Initiative", $"{stats.Initiative}", UIRichTextColors.Initiative));
        sb.AppendLine(UIRichTextColors.Line("Accuracy", $"{stats.Accuracy:F1}%", UIRichTextColors.Accuracy));
        sb.AppendLine(UIRichTextColors.Line("Evasion", $"{stats.Evasion:F1}%", UIRichTextColors.Evasion));

        sb.AppendLine();
        sb.AppendLine(UIRichTextColors.Line("Armor", $"{stats.Armor}", UIRichTextColors.Armor));
        sb.AppendLine(UIRichTextColors.Line("Physical Resistance", $"{stats.PhysicalResistance:F1}%", UIRichTextColors.Physical));
        sb.AppendLine(UIRichTextColors.Line("Fire Resistance", $"{stats.FireResistance:F1}%", UIRichTextColors.Fire));
        sb.AppendLine(UIRichTextColors.Line("Earth Resistance", $"{stats.EarthResistance:F1}%", UIRichTextColors.Earth));
        sb.AppendLine(UIRichTextColors.Line("Wind Resistance", $"{stats.WindResistance:F1}%", UIRichTextColors.Wind));
        sb.AppendLine(UIRichTextColors.Line("Lightning Resistance", $"{stats.LightningResistance:F1}%", UIRichTextColors.Lightning));
        sb.AppendLine(UIRichTextColors.Line("Ice Resistance", $"{stats.IceResistance:F1}%", UIRichTextColors.Ice));

        return sb.ToString();
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