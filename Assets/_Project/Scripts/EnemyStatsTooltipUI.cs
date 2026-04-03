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

        sb.AppendLine($"Class: {stats.Class}");

        if (health != null)
            sb.AppendLine($"HP: {health.CurrentHP}/{health.MaxHP}");
        else
            sb.AppendLine($"HP: {stats.MaxHP}/{stats.MaxHP}");

        sb.AppendLine($"AP: {stats.MaxAP}");
        sb.AppendLine();

        sb.AppendLine($"Strength: {stats.Strength}");
        sb.AppendLine($"Constitution: {stats.Constitution}");
        sb.AppendLine($"Dexterity: {stats.Dexterity}");
        sb.AppendLine($"Intelligence: {stats.Intelligence}");
        sb.AppendLine();

        sb.AppendLine($"Physical Power: {stats.PhysicalPower}");
        sb.AppendLine($"Magic Power: {stats.MagicPower}");
        sb.AppendLine($"Crit Chance: {stats.CritChance:F1}%");
        sb.AppendLine($"Initiative: {stats.Initiative}");
        sb.AppendLine($"Accuracy: {stats.Accuracy:F1}%");
        sb.AppendLine($"Evasion: {stats.Evasion:F1}%");
        sb.AppendLine();

        sb.AppendLine($"Armor: {stats.Armor}");
        sb.AppendLine($"Physical Resistance: {stats.PhysicalResistance:F1}%");
        sb.AppendLine($"Fire Resistance: {stats.FireResistance:F1}%");
        sb.AppendLine($"Earth Resistance: {stats.EarthResistance:F1}%");
        sb.AppendLine($"Wind Resistance: {stats.WindResistance:F1}%");
        sb.AppendLine($"Lightning Resistance: {stats.LightningResistance:F1}%");
        sb.AppendLine($"Ice Resistance: {stats.IceResistance:F1}%");

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