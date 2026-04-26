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
    [SerializeField] private float cellSize = 0.3f;
    [SerializeField] private int maxRadiusCells = 50;
    [SerializeField] private float cellOverlap = 1.08f;

    [Header("Sampling")]
    [SerializeField] private float sampleMaxDist = 0.22f;
    [SerializeField] private float sampleSnapTolerance = 0.18f;
    [SerializeField] private float maxHeightDifference = 0.5f;
    [SerializeField] private float yOffset = 0.03f;

    [Header("Redraw")]
    [SerializeField] private float redrawInterval = 0.15f;
    [SerializeField] private float moveThreshold = 0.03f;

    private Mesh mesh;
    private float timer;
    private Vector3 lastCenterPos;
    private int lastAP = -1;

    private readonly List<Vector3> vertices = new List<Vector3>(4096);
    private readonly List<int> triangles = new List<int>(8192);
    private readonly List<Vector3> normals = new List<Vector3>(4096);

    private void Awake()
    {
        if (playerAP == null)
            playerAP = GetComponentInParent<PlayerAP>();

        if (mover == null)
            mover = GetComponentInParent<PlayerNavMeshMover>();

        if (center == null && mover != null)
            center = mover.transform;

        mesh = new Mesh();
        mesh.name = "MoveRangeGridMesh";
        mesh.MarkDynamic();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.sharedMesh = mesh;
    }

    private void OnEnable()
    {
        if (playerAP != null)
            playerAP.OnAPChanged += OnAPChanged;
    }

    private void OnDisable()
    {
        if (playerAP != null)
            playerAP.OnAPChanged -= OnAPChanged;
    }

    private void Start()
    {
        Redraw(true);
    }

    private void Update()
    {
        if (center == null || playerAP == null || mover == null)
            return;

        timer += Time.deltaTime;

        bool moved = Vector3.Distance(center.position, lastCenterPos) > moveThreshold;
        bool apChanged = playerAP.CurrentAP != lastAP;

        if (moved || apChanged || timer >= redrawInterval)
            Redraw(false);
    }

    private void OnAPChanged(int current, int max)
    {
        Redraw(true);
    }

    private void Redraw(bool force)
    {
        if (center == null || playerAP == null || mover == null)
            return;

        if (!NavMesh.SamplePosition(center.position, out NavMeshHit centerHit, 2f, NavMesh.AllAreas))
        {
            mesh.Clear();
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

        float maxMoveDistance = currentAP * mover.UnitsPerAP;
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
        normals.Clear();

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

                AddQuad(cellCenter, cellSize * cellOverlap);
            }
        }

        if (vertices.Count == 0)
            return;

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.RecalculateBounds();
    }

    private bool TryGetReachableCell(Vector3 start, Vector3 rawCenter, float maxMoveDistance, out Vector3 finalCenter)
    {
        finalCenter = Vector3.zero;

        if (!NavMesh.SamplePosition(rawCenter, out NavMeshHit navHit, sampleMaxDist, NavMesh.AllAreas))
            return false;

        if (Mathf.Abs(navHit.position.y - start.y) > maxHeightDifference)
            return false;

        Vector2 snapDelta = new Vector2(navHit.position.x - rawCenter.x, navHit.position.z - rawCenter.z);
        if (snapDelta.sqrMagnitude > sampleSnapTolerance * sampleSnapTolerance)
            return false;

        NavMeshPath path = new NavMeshPath();

        if (!NavMesh.CalculatePath(start, navHit.position, NavMesh.AllAreas, path))
            return false;

        if (path.status != NavMeshPathStatus.PathComplete)
            return false;

        float pathLength = GetPathLength(path);
        if (pathLength > maxMoveDistance + 0.01f)
            return false;

        finalCenter = navHit.position;
        return true;
    }

    private void AddQuad(Vector3 centerPoint, float size)
    {
        float half = size * 0.5f;
        float y = centerPoint.y + yOffset;

        Vector3 v0 = new Vector3(centerPoint.x - half, y, centerPoint.z - half);
        Vector3 v1 = new Vector3(centerPoint.x - half, y, centerPoint.z + half);
        Vector3 v2 = new Vector3(centerPoint.x + half, y, centerPoint.z + half);
        Vector3 v3 = new Vector3(centerPoint.x + half, y, centerPoint.z - half);

        int baseIndex = vertices.Count;

        vertices.Add(transform.InverseTransformPoint(v0));
        vertices.Add(transform.InverseTransformPoint(v1));
        vertices.Add(transform.InverseTransformPoint(v2));
        vertices.Add(transform.InverseTransformPoint(v3));

        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 1);
        triangles.Add(baseIndex + 2);

        triangles.Add(baseIndex + 0);
        triangles.Add(baseIndex + 2);
        triangles.Add(baseIndex + 3);
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