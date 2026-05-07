using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MoveRangeGridVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAP playerAP;
    [SerializeField] private PlayerNavMeshMover mover;
    [SerializeField] private Transform center;

    [Header("Grid")]
    [SerializeField] private float cellSize = 0.35f;
    [SerializeField] private int maxRadiusCells = 50;
    [SerializeField] private float cellOverlap = 1.05f;

    [Header("Reachability Sampling")]
    [SerializeField] private float sampleMaxDist = 0.35f;
    [SerializeField] private float sampleSnapTolerance = 0.25f;
    [SerializeField] private float maxHeightDifference = 0.8f;

    [Header("Surface Projection")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private bool projectCornersToGround = true;
    [SerializeField] private float groundRaycastUp = 3f;
    [SerializeField] private float groundRaycastDown = 8f;
    [SerializeField] private float cornerNavSampleMaxDist = 0.4f;
    [SerializeField] private float maxCornerHeightDifference = 1.0f;
    [SerializeField] private bool requireCornersNearNavMesh = false;
    [SerializeField] private float yOffset = 0.06f;

    [Header("Redraw")]
    [SerializeField] private float redrawInterval = 0.15f;
    [SerializeField] private float moveThreshold = 0.03f;

    private Mesh mesh;
    private float timer;
    private Vector3 lastCenterPos;
    private int lastAP = -1;

    private bool suppressWhileMoving;
    private bool pendingRedrawAfterMove;

    private NavMeshPath reusablePath;

    private readonly List<Vector3> vertices = new List<Vector3>(4096);
    private readonly List<int> triangles = new List<int>(8192);

    private void Awake()
    {
        if (playerAP == null)
            playerAP = GetComponentInParent<PlayerAP>();

        if (mover == null)
            mover = GetComponentInParent<PlayerNavMeshMover>();

        if (center == null && mover != null)
            center = mover.transform;

        reusablePath = new NavMeshPath();

        mesh = new Mesh();
        mesh.name = "MoveRangeGridMesh";
        mesh.MarkDynamic();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
    }

    private void OnEnable()
    {
        if (reusablePath == null)
            reusablePath = new NavMeshPath();

        if (playerAP != null)
            playerAP.OnAPChanged += OnAPChanged;

        if (mover != null)
        {
            mover.OnMoveStarted += HandleMoveStarted;
            mover.OnMoveFinished += HandleMoveFinished;
        }
    }

    private void OnDisable()
    {
        if (playerAP != null)
            playerAP.OnAPChanged -= OnAPChanged;

        if (mover != null)
        {
            mover.OnMoveStarted -= HandleMoveStarted;
            mover.OnMoveFinished -= HandleMoveFinished;
        }
    }

    private void Start()
    {
        Redraw(true);
    }

    private void Update()
    {
        if (center == null || playerAP == null || mover == null)
            return;

        if (suppressWhileMoving)
            return;

        bool moved = Vector3.Distance(center.position, lastCenterPos) > moveThreshold;
        bool apChanged = playerAP.CurrentAP != lastAP;

        if (moved || apChanged)
            Redraw(false);
    }

    private void OnAPChanged(int current, int max)
    {
        if (suppressWhileMoving)
        {
            pendingRedrawAfterMove = true;
            return;
        }

        Redraw(true);
    }

    public void BeginHideUntilMovementEnds()
    {
        suppressWhileMoving = true;
        pendingRedrawAfterMove = true;
        ClearMesh();
    }

    private void HandleMoveStarted()
    {
        BeginHideUntilMovementEnds();
    }

    private void HandleMoveFinished()
    {
        suppressWhileMoving = false;

        if (pendingRedrawAfterMove)
        {
            pendingRedrawAfterMove = false;
            Redraw(true);
        }
    }

    private void Redraw(bool force)
    {
        if (center == null || playerAP == null || mover == null)
            return;

        if (mesh == null)
            return;

        if (reusablePath == null)
            reusablePath = new NavMeshPath();

        if (!NavMesh.SamplePosition(center.position, out NavMeshHit centerHit, 2f, NavMesh.AllAreas))
        {
            ClearMesh();
            return;
        }

        Vector3 navCenter = centerHit.position;
        int currentAP = playerAP.CurrentAP;

        bool moved = Vector3.Distance(navCenter, lastCenterPos) > moveThreshold;
        bool apChanged = currentAP != lastAP;

        if (!force && !moved && !apChanged && timer < redrawInterval)
            return;

        timer = 0f;
        lastCenterPos = navCenter;
        lastAP = currentAP;

        float maxMoveDistance = currentAP * mover.GetEffectiveUnitsPerAP();
        int radiusCells = Mathf.Min(maxRadiusCells, Mathf.CeilToInt(maxMoveDistance / cellSize) + 2);

        BuildGridMesh(navCenter, maxMoveDistance, radiusCells);
    }

    private void BuildGridMesh(Vector3 start, float maxMoveDistance, int radiusCells)
    {
        mesh.Clear();

        if (maxMoveDistance <= 0.01f)
            return;

        vertices.Clear();
        triangles.Clear();

        float extra = cellSize;
        float maxDistanceSqr = (maxMoveDistance + extra) * (maxMoveDistance + extra);

        for (int x = -radiusCells; x <= radiusCells; x++)
        {
            for (int z = -radiusCells; z <= radiusCells; z++)
            {
                Vector3 rawCenter = start + new Vector3(x * cellSize, 0f, z * cellSize);

                Vector2 planarDelta = new Vector2(rawCenter.x - start.x, rawCenter.z - start.z);
                if (planarDelta.sqrMagnitude > maxDistanceSqr)
                    continue;

                rawCenter.y = start.y;

                if (!TryGetReachableCell(start, rawCenter, maxMoveDistance, out Vector3 cellCenter))
                    continue;

                AddProjectedQuad(cellCenter, cellSize * cellOverlap);
            }
        }

        if (vertices.Count == 0)
            return;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private bool TryGetReachableCell(Vector3 start, Vector3 rawCenter, float maxMoveDistance, out Vector3 finalCenter)
    {
        finalCenter = Vector3.zero;

        if (reusablePath == null)
            reusablePath = new NavMeshPath();

        if (!NavMesh.SamplePosition(rawCenter, out NavMeshHit navHit, sampleMaxDist, NavMesh.AllAreas))
            return false;

        if (Mathf.Abs(navHit.position.y - start.y) > maxHeightDifference)
            return false;

        Vector2 snapDelta = new Vector2(navHit.position.x - rawCenter.x, navHit.position.z - rawCenter.z);
        if (snapDelta.sqrMagnitude > sampleSnapTolerance * sampleSnapTolerance)
            return false;

        float straightDistance = Vector3.Distance(start, navHit.position);

        if (straightDistance > maxMoveDistance + 0.01f)
            return false;

        finalCenter = navHit.position;
        return true;
    }

    private void AddProjectedQuad(Vector3 centerPoint, float size)
    {
        float half = size * 0.5f;

        Vector3 rawV0 = new Vector3(centerPoint.x - half, centerPoint.y, centerPoint.z - half);
        Vector3 rawV1 = new Vector3(centerPoint.x - half, centerPoint.y, centerPoint.z + half);
        Vector3 rawV2 = new Vector3(centerPoint.x + half, centerPoint.y, centerPoint.z + half);
        Vector3 rawV3 = new Vector3(centerPoint.x + half, centerPoint.y, centerPoint.z - half);

        if (!TryProjectPointToSurface(rawV0, centerPoint.y, out Vector3 v0))
            return;

        if (!TryProjectPointToSurface(rawV1, centerPoint.y, out Vector3 v1))
            return;

        if (!TryProjectPointToSurface(rawV2, centerPoint.y, out Vector3 v2))
            return;

        if (!TryProjectPointToSurface(rawV3, centerPoint.y, out Vector3 v3))
            return;

        float minY = Mathf.Min(v0.y, v1.y, v2.y, v3.y);
        float maxY = Mathf.Max(v0.y, v1.y, v2.y, v3.y);

        if (maxY - minY > maxCornerHeightDifference)
            return;

        int baseIndex = vertices.Count;

        vertices.Add(transform.InverseTransformPoint(v0));
        vertices.Add(transform.InverseTransformPoint(v1));
        vertices.Add(transform.InverseTransformPoint(v2));
        vertices.Add(transform.InverseTransformPoint(v3));

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);
    }

    private bool TryProjectPointToSurface(Vector3 rawPoint, float fallbackY, out Vector3 projectedPoint)
    {
        projectedPoint = Vector3.zero;

        Vector3 navSamplePoint = new Vector3(rawPoint.x, fallbackY, rawPoint.z);

        if (!NavMesh.SamplePosition(navSamplePoint, out NavMeshHit navHit, cornerNavSampleMaxDist, NavMesh.AllAreas))
            return false;

        Vector2 navDelta = new Vector2(
            navHit.position.x - rawPoint.x,
            navHit.position.z - rawPoint.z
        );

        if (navDelta.sqrMagnitude > cornerNavSampleMaxDist * cornerNavSampleMaxDist)
            return false;

        if (Mathf.Abs(navHit.position.y - fallbackY) > maxCornerHeightDifference)
            return false;

        if (projectCornersToGround)
        {
            Vector3 rayOrigin = new Vector3(navHit.position.x, navHit.position.y + groundRaycastUp, navHit.position.z);
            float rayDistance = groundRaycastUp + groundRaycastDown;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                projectedPoint = hit.point + Vector3.up * yOffset;
                return true;
            }
        }

        projectedPoint = navHit.position + Vector3.up * yOffset;
        return true;
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

    private void ClearMesh()
    {
        if (mesh != null)
            mesh.Clear();
    }
}