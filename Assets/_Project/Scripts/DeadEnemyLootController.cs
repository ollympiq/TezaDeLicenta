using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DeadEnemyLootController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCombatController playerCombatController;
    [SerializeField] private PlayerNavMeshMover playerMover;
    [SerializeField] private EnemyLootUI lootUI;

    [Header("Raycast")]
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float rayDistance = 500f;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (playerCombatController == null)
            playerCombatController = FindFirstObjectByType<PlayerCombatController>();

        if (playerMover == null)
            playerMover = FindFirstObjectByType<PlayerNavMeshMover>();

        if (lootUI == null)
            lootUI = EnemyLootUI.Instance != null
                ? EnemyLootUI.Instance
                : FindFirstObjectByType<EnemyLootUI>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (lootUI == null || Mouse.current == null || mainCamera == null)
            return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            lootUI.Hide();
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (playerCombatController != null && playerCombatController.HasTargetingSkillSelected)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        EnemyLootContainer clickedLoot = GetClickedDeadEnemyLoot();

        if (clickedLoot != null)
        {
            playerMover?.BlockMovementForCurrentFrame();
            lootUI.Show(clickedLoot);
        }
        else
        {
            if (lootUI.IsOpen)
                playerMover?.BlockMovementForCurrentFrame();

            lootUI.Hide();
        }
    }

    private EnemyLootContainer GetClickedDeadEnemyLoot()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, rayDistance, enemyMask))
            return null;

        CharacterStats targetStats = hit.collider.GetComponentInParent<CharacterStats>();
        if (targetStats == null)
            return null;

        CharacterHealth targetHealth = targetStats.GetComponent<CharacterHealth>();
        if (targetHealth == null || !targetHealth.IsDead)
            return null;

        EnemyLootContainer lootContainer = targetStats.GetComponent<EnemyLootContainer>();
        if (lootContainer == null || !lootContainer.IsLootable)
            return null;

        return lootContainer;
    }
}