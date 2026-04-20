using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class LobbyInteractionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private NavMeshAgent playerAgent;
    [SerializeField] private TraderShopUI traderShopUI;

    [Header("Raycast Layers")]
    [SerializeField] private LayerMask traderLayerMask;
    [SerializeField] private LayerMask groundLayerMask;

    [Header("Movement")]
    [SerializeField] private bool allowGroundMovement = true;
    [SerializeField] private float arrivalSlack = 0.2f;
    [SerializeField] private bool stopPlayerWhenShopOpens = true;
    [SerializeField] private bool rotatePlayerTowardTraderOnOpen = true;

    private TraderInteractable pendingTrader;

    private void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        if (playerAgent == null && playerRoot != null)
            playerAgent = playerRoot.GetComponent<NavMeshAgent>();

        if (playerRoot == null && playerAgent != null)
            playerRoot = playerAgent.transform;

        if (traderShopUI == null)
            traderShopUI = FindFirstObjectByType<TraderShopUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (worldCamera == null || playerRoot == null || playerAgent == null)
            return;

        UpdatePendingTraderArrival();
        HandleMouseInput();
    }

    private void HandleMouseInput()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (traderShopUI != null && traderShopUI.IsOpen)
            return;

        Ray ray = worldCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit traderHit, 1000f, traderLayerMask, QueryTriggerInteraction.Collide))
        {
            TraderInteractable trader = traderHit.collider.GetComponentInParent<TraderInteractable>();
            if (trader != null)
            {
                BeginTraderInteraction(trader);
                return;
            }
        }

        if (!allowGroundMovement)
            return;

        if (Physics.Raycast(ray, out RaycastHit groundHit, 1000f, groundLayerMask, QueryTriggerInteraction.Ignore))
        {
            pendingTrader = null;
            playerAgent.SetDestination(groundHit.point);
        }
    }

    private void BeginTraderInteraction(TraderInteractable trader)
    {
        if (trader == null)
            return;

        pendingTrader = trader;

        if (trader.IsInRange(playerRoot.position, arrivalSlack))
        {
            OpenPendingTraderShop();
            return;
        }

        playerAgent.SetDestination(trader.GetInteractionPosition());
    }

    private void UpdatePendingTraderArrival()
    {
        if (pendingTrader == null)
            return;

        if (pendingTrader.IsInRange(playerRoot.position, arrivalSlack))
        {
            OpenPendingTraderShop();
            return;
        }

        if (playerAgent.pathPending)
            return;

        if (!playerAgent.hasPath)
        {
            playerAgent.SetDestination(pendingTrader.GetInteractionPosition());
            return;
        }

        if (playerAgent.remainingDistance <= pendingTrader.InteractionDistance + arrivalSlack)
            OpenPendingTraderShop();
    }

    private void OpenPendingTraderShop()
    {
        if (pendingTrader == null)
            return;

        if (stopPlayerWhenShopOpens)
            playerAgent.ResetPath();

        if (rotatePlayerTowardTraderOnOpen)
            FaceToward(pendingTrader.transform.position);

        pendingTrader.OpenShop();
        pendingTrader = null;
    }

    private void FaceToward(Vector3 worldTarget)
    {
        Vector3 dir = worldTarget - playerRoot.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
            return;

        playerRoot.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
    }
}