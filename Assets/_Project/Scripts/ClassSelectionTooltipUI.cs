using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassSelectionTooltipUI : MonoBehaviour
{
    public static ClassSelectionTooltipUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private Canvas rootCanvas;

    [Header("Position")]
    [SerializeField] private Vector2 offset = new Vector2(24f, -24f);
    [SerializeField] private float screenPadding = 12f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Instance = this;

        if (panelRoot == null)
            panelRoot = gameObject;

        if (panelRect == null)
            panelRect = GetComponent<RectTransform>();

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        DisableRaycastTargets();

        Hide();
    }

    private void Update()
    {
        if (panelRoot == null || !panelRoot.activeSelf)
            return;

        FollowMouse();
    }

    public void Show(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Hide();
            return;
        }

        if (tooltipText != null)
            tooltipText.text = text;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        transform.SetAsLastSibling();
        DisableRaycastTargets();
        FollowMouse();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void DisableRaycastTargets()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        if (canvasGroup != null)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    private void FollowMouse()
    {
        if (panelRect == null || rootCanvas == null)
            return;

        Vector2 mousePosition = Input.mousePosition;
        Vector2 targetPosition = mousePosition + offset;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            targetPosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out localPoint
        );

        panelRect.anchoredPosition = ClampToCanvas(localPoint, canvasRect);
    }

    private Vector2 ClampToCanvas(Vector2 position, RectTransform canvasRect)
    {
        Vector2 halfCanvas = canvasRect.rect.size * 0.5f;
        Vector2 panelSize = panelRect.rect.size;

        float minX = -halfCanvas.x + screenPadding;
        float maxX = halfCanvas.x - panelSize.x - screenPadding;

        float minY = -halfCanvas.y + panelSize.y + screenPadding;
        float maxY = halfCanvas.y - screenPadding;

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }
}