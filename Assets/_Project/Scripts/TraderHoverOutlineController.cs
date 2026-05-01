using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class TraderHoverOutlineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TraderShopUI traderShopUI;

    [Header("Raycast")]
    [SerializeField] private LayerMask traderMask;
    [SerializeField] private float rayDistance = 500f;

    private HoverOutlineTarget currentHovered;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (traderShopUI == null)
            traderShopUI = FindFirstObjectByType<TraderShopUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (Mouse.current == null || mainCamera == null)
        {
            ClearHover();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearHover();
            return;
        }

        if (traderShopUI != null && traderShopUI.IsOpen)
        {
            ClearHover();
            return;
        }

        HoverOutlineTarget hovered = GetHoveredOutlineTarget();

        if (hovered == currentHovered)
            return;

        if (currentHovered != null)
            currentHovered.SetHighlighted(false);

        currentHovered = hovered;

        if (currentHovered != null)
            currentHovered.SetHighlighted(true);
    }

    private HoverOutlineTarget GetHoveredOutlineTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, traderMask, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<HoverOutlineTarget>();
    }

    private void ClearHover()
    {
        if (currentHovered != null)
        {
            currentHovered.SetHighlighted(false);
            currentHovered = null;
        }
    }
}