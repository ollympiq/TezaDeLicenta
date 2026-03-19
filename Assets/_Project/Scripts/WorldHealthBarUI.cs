using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class WorldHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI hpText;

    [Header("Follow")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.3f, 0f);

    private CharacterHealth targetHealth;
    private Transform followTarget;
    private Canvas rootCanvas;
    private Camera worldCamera;
    private RectTransform rectTransform;
    private RectTransform canvasRect;
    private CanvasGroup canvasGroup;

    public void Initialize(
        CharacterHealth health,
        Transform target,
        Canvas canvas,
        Camera cam)
    {
        targetHealth = health;
        followTarget = target;
        rootCanvas = canvas;
        worldCamera = cam;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (rootCanvas != null)
            canvasRect = rootCanvas.transform as RectTransform;

        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged += HandleHealthChanged;
            targetHealth.OnDied += HandleDied;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (targetHealth != null)
        {
            targetHealth.OnHealthChanged -= HandleHealthChanged;
            targetHealth.OnDied -= HandleDied;
        }
    }

    private void Update()
    {
        UpdateScreenPosition();
    }

    private void HandleHealthChanged(int current, int max)
    {
        Refresh();
    }

    private void HandleDied(CharacterHealth deadHealth)
    {
        Destroy(gameObject);
    }

    private void Refresh()
    {
        if (targetHealth == null)
            return;

        int current = targetHealth.CurrentHP;
        int max = targetHealth.MaxHP;

        float ratio = max > 0 ? (float)current / max : 0f;
        ratio = Mathf.Clamp01(ratio);

        if (fillImage != null)
            fillImage.fillAmount = ratio;

        if (hpText != null)
            hpText.text = $"{current} / {max}";
    }

    private void UpdateScreenPosition()
    {
        if (followTarget == null || canvasRect == null || worldCamera == null || rectTransform == null)
            return;

        Vector3 worldPos = followTarget.position + worldOffset;
        Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0f)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
            return;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        Camera uiCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? rootCanvas.worldCamera
            : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, uiCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }
}