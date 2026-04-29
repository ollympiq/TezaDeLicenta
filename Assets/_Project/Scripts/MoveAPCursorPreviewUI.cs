using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MoveAPCursorPreviewUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerNavMeshMover mover;
    [SerializeField] private PlayerTurnController playerTurnController;
    [SerializeField] private PlayerCombatController combatController;
    [SerializeField] private PlayerAP playerAP;

    [Header("UI")]
    [SerializeField] private RectTransform previewRoot;
    [SerializeField] private TextMeshProUGUI previewText;
    [SerializeField] private Vector2 screenOffset = new Vector2(28f, -20f);

    [Header("Behavior")]
    [SerializeField] private bool hideWhileMoving = true;
    [SerializeField] private bool hideWhenSkillSelected = true;
    [SerializeField] private bool hideWhenPointerOverUI = true;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (mover == null)
            mover = FindFirstObjectByType<PlayerNavMeshMover>();

        if (playerTurnController == null)
            playerTurnController = FindFirstObjectByType<PlayerTurnController>();

        if (combatController == null)
            combatController = FindFirstObjectByType<PlayerCombatController>();

        if (playerAP == null)
            playerAP = FindFirstObjectByType<PlayerAP>();

        if (previewRoot == null)
            previewRoot = transform as RectTransform;

        if (previewRoot != null)
        {
            canvasGroup = previewRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = previewRoot.gameObject.AddComponent<CanvasGroup>();
        }

        HidePreviewImmediate();
    }

    private void Update()
    {
        if (Mouse.current == null || mover == null || previewRoot == null || previewText == null)
        {
            HidePreviewImmediate();
            return;
        }

        if (playerTurnController != null && !playerTurnController.IsTurnActive)
        {
            HidePreviewImmediate();
            return;
        }

        if (hideWhileMoving && mover.IsCurrentlyMoving)
        {
            HidePreviewImmediate();
            return;
        }

        if (hideWhenSkillSelected && combatController != null && combatController.HasTargetingSkillSelected)
        {
            HidePreviewImmediate();
            return;
        }

        if (hideWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            HidePreviewImmediate();
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (!mover.TryCalculateMovePreviewAtScreenPoint(mousePos, out int apCost, out float pathLength, out Vector3 destination))
        {
            HidePreviewImmediate();
            return;
        }

        if (playerAP != null && apCost > playerAP.CurrentAP)
        {
            HidePreviewImmediate();
            return;
        }

        if (apCost <= 0)
        {
            HidePreviewImmediate();
            return;
        }

        ShowPreview(apCost, mousePos);
    }

    private void ShowPreview(int apCost, Vector2 mousePos)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        previewText.text = apCost == 1 ? "1 AP" : apCost + " AP";
        previewRoot.position = mousePos + screenOffset;
    }

    private void HidePreviewImmediate()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}