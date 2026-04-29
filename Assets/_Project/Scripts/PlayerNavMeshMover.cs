using System;
using System.Collections;
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
    [SerializeField] private MoveRangeGridVisualizer moveRangeVisualizer;

    [Header("Movement")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("AP Cost")]
    [SerializeField] private float unitsPerAP = 2f;
    public float UnitsPerAP => unitsPerAP;

    public event Action OnMoveStarted;
    public event Action OnMoveFinished;

    private NavMeshAgent agent;
    private PlayerAP playerAP;
    private CharacterHealth health;
    private bool turnInputEnabled;
    private bool blockMovementThisFrame;
    private Coroutine movementWatchRoutine;

    public bool IsCurrentlyMoving => IsActuallyMoving();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerAP = GetComponent<PlayerAP>();
        health = GetComponent<CharacterHealth>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();

        if (moveRangeVisualizer == null)
            moveRangeVisualizer = FindFirstObjectByType<MoveRangeGridVisualizer>();

        turnInputEnabled = false;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StopMovementImmediately(false);
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
        if (Mouse.current == null)
            return;

        if (!TryCalculateMovePreviewAtScreenPoint(
                Mouse.current.position.ReadValue(),
                out int apCost,
                out float pathLength,
                out Vector3 destination))
            return;

        if (!playerAP.HasEnoughAP(apCost))
        {
            GameLog.Warning("Nu ai destul AP pentru deplasare.");
            return;
        }

        OnMoveStarted?.Invoke();
        moveRangeVisualizer?.BeginHideUntilMovementEnds();

        bool spent = playerAP.SpendAP(apCost);
        if (!spent)
            return;

        agent.isStopped = false;
        agent.SetDestination(destination);

        if (movementWatchRoutine != null)
            StopCoroutine(movementWatchRoutine);

        movementWatchRoutine = StartCoroutine(WatchMovementUntilFinished());

        GameLog.Info($"Deplasare efectuata | Cost: {apCost} AP | Lungime traseu: {pathLength:F2}");
    }

    public bool TryCalculateMovePreviewAtScreenPoint(
        Vector2 screenPosition,
        out int apCost,
        out float pathLength,
        out Vector3 destination)
    {
        apCost = 0;
        pathLength = 0f;
        destination = Vector3.zero;

        if (mainCamera == null || agent == null)
            return false;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
            return false;

        if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            return false;

        NavMeshPath path = new NavMeshPath();
        bool foundPath = agent.CalculatePath(navHit.position, path);

        if (!foundPath)
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        pathLength = GetPathLength(path);

        if (pathLength < 0.05f)
            return false;

        apCost = Mathf.CeilToInt(pathLength / unitsPerAP);
        destination = navHit.position;
        return true;
    }

    private IEnumerator WatchMovementUntilFinished()
    {
        yield return null;

        while (IsActuallyMoving())
            yield return null;

        movementWatchRoutine = null;
        OnMoveFinished?.Invoke();
    }

    private bool IsActuallyMoving()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.pathPending)
            return true;

        if (agent.isStopped)
            return false;

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + 0.02f)
            return true;

        if (agent.velocity.sqrMagnitude > 0.0001f)
            return true;

        return false;
    }

    private void StopMovementImmediately(bool notifyFinished)
    {
        if (movementWatchRoutine != null)
        {
            StopCoroutine(movementWatchRoutine);
            movementWatchRoutine = null;
        }

        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        if (notifyFinished)
            OnMoveFinished?.Invoke();
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
            StopMovementImmediately(true);
    }

    public void BlockMovementForCurrentFrame()
    {
        blockMovementThisFrame = true;
        StopMovementImmediately(false);
    }
}