using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DamageNumberUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI textLabel;

    [Header("Animation")]
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseDistance = 45f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Text Layout")]
    [SerializeField] private float minWidth = 420f;
    [SerializeField] private float maxWidth = 900f;
    [SerializeField] private float height = 90f;
    [SerializeField] private float horizontalPadding = 60f;

    private RectTransform rectTransform;
    private RectTransform textRectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private RectTransform canvasRect;
    private Camera worldCamera;

    private Transform followTarget;
    private Vector3 fallbackWorldPosition;

    private float elapsed;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (textLabel == null)
            textLabel = GetComponentInChildren<TextMeshProUGUI>();

        if (textLabel != null)
            textRectTransform = textLabel.rectTransform;

        ConfigureTextLabel();
    }

    public void Initialize(
        Canvas canvas,
        Camera cameraRef,
        Transform target,
        string displayText,
        Color color,
        float scaleMultiplier = 1f)
    {
        rootCanvas = canvas;
        worldCamera = cameraRef;
        followTarget = target;

        if (target != null)
            fallbackWorldPosition = target.position;

        if (rootCanvas != null)
            canvasRect = rootCanvas.transform as RectTransform;

        ConfigureTextLabel();

        string cleanText = NormalizeOneLine(displayText);

        if (textLabel != null)
        {
            textLabel.text = cleanText;
            textLabel.color = color;
            textLabel.ForceMeshUpdate();
        }

        ResizeToFitText();

        transform.localScale = Vector3.one * scaleMultiplier;
        canvasGroup.alpha = 1f;

        UpdatePosition();
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        UpdatePosition();

        float t = Mathf.Clamp01(elapsed / lifetime);
        canvasGroup.alpha = 1f - t;

        if (elapsed >= lifetime)
            Destroy(gameObject);
    }

    private void ConfigureTextLabel()
    {
        if (textLabel == null)
            return;

        textLabel.enableWordWrapping = false;
        textLabel.overflowMode = TextOverflowModes.Overflow;
        textLabel.alignment = TextAlignmentOptions.Center;
        textLabel.raycastTarget = false;
    }

    private string NormalizeOneLine(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string result = value.Replace("\r", " ");
        result = result.Replace("\n", " ");

        while (result.Contains("  "))
            result = result.Replace("  ", " ");

        return result.Trim();
    }

    private void ResizeToFitText()
    {
        if (rectTransform == null)
            return;

        float targetWidth = minWidth;

        if (textLabel != null)
        {
            textLabel.ForceMeshUpdate();
            float preferredWidth = textLabel.preferredWidth + horizontalPadding;
            targetWidth = Mathf.Clamp(preferredWidth, minWidth, maxWidth);
        }

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        if (textRectTransform != null)
        {
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
        }
    }

    private void UpdatePosition()
    {
        if (canvasRect == null || worldCamera == null)
            return;

        Vector3 anchorWorldPos = followTarget != null
            ? followTarget.position + worldOffset
            : fallbackWorldPosition + worldOffset;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(anchorWorldPos);

        if (screenPos.z < 0f)
        {
            canvasGroup.alpha = 0f;
            return;
        }

        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPoint))
        {
            float rise = Mathf.Lerp(0f, riseDistance, Mathf.Clamp01(elapsed / lifetime));
            rectTransform.anchoredPosition = localPoint + Vector2.up * rise;
        }
    }
}