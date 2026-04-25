using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerAP))]
[RequireComponent(typeof(CharacterHealth))]
public class PlayerNavMeshMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerCombatController combatController;

    [Header("Movement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("AP Cost")]
    [SerializeField] private float unitsPerAP = 2f;
    public float UnitsPerAP => unitsPerAP;

    private NavMeshAgent agent;
    private PlayerAP playerAP;
    private CharacterHealth health;
    private bool turnInputEnabled;
    private bool blockMovementThisFrame;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerAP = GetComponent<PlayerAP>();
        health = GetComponent<CharacterHealth>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();

        turnInputEnabled = false;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StopMovementImmediately();
            return;
        }

        if (!turnInputEnabled)
        {
            blockMovementThisFrame = false;
            return;
        }

        if (Mouse.current == null || mainCamera == null)
        {
            blockMovementThisFrame = false;
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
            {
                blockMovementThisFrame = false;
                return;
            }

            if (blockMovementThisFrame)
            {
                blockMovementThisFrame = false;
                return;
            }

            if (combatController != null && combatController.BlockMovementThisFrame)
            {
                blockMovementThisFrame = false;
                return;
            }

            if (combatController != null && combatController.HasTargetingSkillSelected)
            {
                blockMovementThisFrame = false;
                return;
            }

            TryMoveToMouse();
        }

        blockMovementThisFrame = false;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void TryMoveToMouse()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
            return;

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            return;

        NavMeshPath path = new NavMeshPath();
        bool foundPath = agent.CalculatePath(navHit.position, path);

        if (!foundPath)
            return;

        if (path.status != NavMeshPathStatus.PathComplete)
            return;

        float pathLength = GetPathLength(path);

        if (pathLength < 0.05f)
            return;

        int apCost = Mathf.CeilToInt(pathLength / unitsPerAP);

        if (!playerAP.HasEnoughAP(apCost))
        {
            Debug.Log("Nu ai destul AP.");
            return;
        }

        bool spent = playerAP.SpendAP(apCost);
        if (!spent)
            return;

        agent.isStopped = false;
        agent.SetDestination(navHit.position);

        Debug.Log($"Move cost: {apCost} AP | Path length: {pathLength:F2}");
    }

    private void StopMovementImmediately()
    {
        if (agent == null || !agent.enabled)
            return;

        agent.isStopped = true;
        agent.ResetPath();
    }

    private float GetPathLength(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return 0f;

        float total = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            total += Vector3.Distance(path.corners[i - 1], path.corners[i]);

        return total;
    }

    public void SetTurnInputEnabled(bool enabled)
    {
        turnInputEnabled = enabled;

        if (!enabled)
            StopMovementImmediately();
    }

    public void BlockMovementForCurrentFrame()
    {
        blockMovementThisFrame = true;
        StopMovementImmediately();
    }
}