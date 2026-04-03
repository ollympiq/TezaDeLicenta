using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class EnemyInspectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCombatController playerCombatController;

    [Header("Raycast")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float rayDistance = 500f;

    private CharacterStats inspectedTarget;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerCombatController == null)
            playerCombatController = FindFirstObjectByType<PlayerCombatController>();
    }

    private void Update()
    {
        if (EnemyStatsTooltipUI.Instance == null)
            return;

        if (Mouse.current == null || mainCamera == null)
        {
            ClearInspection();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            ClearInspection();
            return;
        }

        if (playerCombatController != null && playerCombatController.HasTargetingSkillSelected)
        {
            ClearInspection();
            return;
        }

        CharacterStats hoveredEnemy = GetHoveredEnemy();

        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (hoveredEnemy != null)
            {
                inspectedTarget = hoveredEnemy;
                EnemyStatsTooltipUI.Instance.Show(
                    inspectedTarget,
                    inspectedTarget.GetComponent<CharacterHealth>()
                );
            }
            else
            {
                ClearInspection();
            }

            return;
        }

        if (inspectedTarget != null)
        {
            if (hoveredEnemy == null || hoveredEnemy != inspectedTarget)
            {
                ClearInspection();
            }
            else
            {
                EnemyStatsTooltipUI.Instance.Show(
                    inspectedTarget,
                    inspectedTarget.GetComponent<CharacterHealth>()
                );
            }
        }
    }

    private CharacterStats GetHoveredEnemy()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, enemyMask))
            return null;

        return hit.collider.GetComponentInParent<CharacterStats>();
    }

    public void ClearInspection()
    {
        inspectedTarget = null;

        if (EnemyStatsTooltipUI.Instance != null)
            EnemyStatsTooltipUI.Instance.Hide();
    }
}