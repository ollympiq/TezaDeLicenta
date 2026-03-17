using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DamageNumberUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textLabel;
    [SerializeField] private float lifetime = 0.9f;
    [SerializeField] private float riseDistance = 45f;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    private RectTransform rectTransform;
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
            textLabel = GetComponent<TextMeshProUGUI>();
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

        if (textLabel != null)
        {
            textLabel.text = displayText;
            textLabel.color = color;
        }

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