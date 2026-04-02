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
    [SerializeField] private LayerMask enemyMask;
    [SerializeField] private float navMeshSampleDistance = 1.5f;

    [Header("AP Cost")]
    [SerializeField] private float unitsPerAP = 2f;
    public float UnitsPerAP => unitsPerAP;

    [Header("Enemy Blocking")]
    [SerializeField] private float stopBeforeEnemyBuffer = 0.12f;
    [SerializeField] private float emergencyStopExtraGap = 0.05f;
    [SerializeField] private float castHeight = 0.8f;

    private NavMeshAgent agent;
    private PlayerAP playerAP;
    private CharacterHealth health;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        playerAP = GetComponent<PlayerAP>();
        health = GetComponent<CharacterHealth>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (combatController == null)
            combatController = GetComponent<PlayerCombatController>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
        {
            StopMovementImmediately();
            return;
        }

        // Plasa de siguranta: daca tot ajunge prea aproape de enemy, opreste-l.
        if (agent != null && agent.enabled && agent.hasPath && IsTooCloseToEnemy())
        {
            StopMovementImmediately();
            return;
        }

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            playerAP.RestoreAllAP();
            Debug.Log("AP resetat la maxim.");
        }

        if (Mouse.current == null || mainCamera == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI())
                return;

            if (combatController != null && combatController.HasTargetingSkillSelected)
                return;

            TryMoveToMouse();
        }
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

        Vector3 desiredDestination = navHit.position;
        Vector3 finalDestination = ClampDestinationBeforeEnemy(desiredDestination);

        if (!NavMesh.SamplePosition(finalDestination, out NavMeshHit finalNavHit, navMeshSampleDistance, NavMesh.AllAreas))
            return;

        NavMeshPath path = new NavMeshPath();
        bool foundPath = agent.CalculatePath(finalNavHit.position, path);

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
        agent.SetDestination(finalNavHit.position);

        Debug.Log($"Move cost: {apCost} AP | Path length: {pathLength:F2}");
    }

    private Vector3 ClampDestinationBeforeEnemy(Vector3 desiredDestination)
    {
        Vector3 start = transform.position;
        Vector3 flatDir = desiredDestination - start;
        flatDir.y = 0f;

        float distance = flatDir.magnitude;
        if (distance < 0.01f)
            return desiredDestination;

        Vector3 dir = flatDir.normalized;
        float ownRadius = GetOwnBodyRadius();

        Vector3 castOrigin = start + Vector3.up * castHeight;

        bool hitEnemy = Physics.SphereCast(
            castOrigin,
            ownRadius,
            dir,
            out RaycastHit hit,
            distance,
            enemyMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hitEnemy)
            return desiredDestination;

        float safeDistance = Mathf.Max(0f, hit.distance - stopBeforeEnemyBuffer);
        Vector3 clamped = start + dir * safeDistance;
        clamped.y = desiredDestination.y;

        return clamped;
    }

    private bool IsTooCloseToEnemy()
    {
        float ownRadius = GetOwnBodyRadius();
        float checkRadius = ownRadius + emergencyStopExtraGap;

        Vector3 center = transform.position + Vector3.up * castHeight;
        Collider[] hits = Physics.OverlapSphere(center, checkRadius, enemyMask, QueryTriggerInteraction.Ignore);

        return hits != null && hits.Length > 0;
    }

    private float GetOwnBodyRadius()
    {
        if (TryGetComponent<CapsuleCollider>(out var capsule))
        {
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            return Mathf.Max(0.1f, capsule.radius * scale);
        }

        if (TryGetComponent<SphereCollider>(out var sphere))
        {
            float scale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            return Mathf.Max(0.1f, sphere.radius * scale);
        }

        if (TryGetComponent<Collider>(out var col))
            return Mathf.Max(0.1f, Mathf.Max(col.bounds.extents.x, col.bounds.extents.z));

        if (agent != null)
            return Mathf.Max(0.1f, agent.radius);

        return 0.4f;
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
}