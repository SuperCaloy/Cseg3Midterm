using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    [Header("Generation Settings")]
    public int targetWaypointCount = 14;
    public float minDistanceBetweenWaypoints = 5f;
    public float minDistanceFromPlayerSpawn = 6f;
    public float obstacleClearanceRadius = 0.6f;
    public LayerMask obstacleMask = ~0; // Checks against walls and obstacles

    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = Color.cyan;

    [Header("In-Game Visualization")]
    public bool showInGameWaypoints = true;
    public KeyCode toggleWaypointsKey = KeyCode.G;

    private GameObject inGameMarkersContainer;
    private static Material waypointNodeMaterial;
    private static Material waypointPathMaterial;

    [SerializeField]
    private List<Vector3> waypoints = new List<Vector3>();

    public IReadOnlyList<Vector3> Waypoints => waypoints;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        // Generate waypoints if empty or regenerate at runtime for fresh randomness
        GenerateWaypoints();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleWaypointsKey))
        {
            showInGameWaypoints = !showInGameWaypoints;
            if (inGameMarkersContainer != null)
            {
                inGameMarkersContainer.SetActive(showInGameWaypoints);
            }
        }
    }

    public void GenerateWaypoints()
    {
        waypoints.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            Debug.LogWarning("WaypointManager: No NavMesh found to generate waypoints.");
            return;
        }

        Vector3 playerSpawn = Vector3.zero;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerSpawn = playerObj.transform.position;
        }

        Vector3 navMeshSpawn = playerSpawn;
        if (NavMesh.SamplePosition(playerSpawn, out NavMeshHit sHit, 5.0f, NavMesh.AllAreas))
        {
            navMeshSpawn = sHit.position;
        }

        int maxAttempts = targetWaypointCount * 50;
        int attempts = 0;
        NavMeshPath pathTest = new NavMeshPath();
        float currentMinDist = minDistanceBetweenWaypoints;

        while (waypoints.Count < targetWaypointCount && attempts < maxAttempts)
        {
            attempts++;

            // Periodically relax spacing if density is high
            if (attempts > maxAttempts / 2 && currentMinDist > 3f)
            {
                currentMinDist = 3f;
            }

            // Sample a random point from NavMesh triangles
            int triangleIndex = Random.Range(0, triangulation.indices.Length / 3) * 3;
            Vector3 v1 = triangulation.vertices[triangulation.indices[triangleIndex]];
            Vector3 v2 = triangulation.vertices[triangulation.indices[triangleIndex + 1]];
            Vector3 v3 = triangulation.vertices[triangulation.indices[triangleIndex + 2]];

            // Uniform barycentric sampling inside triangle
            float r1 = Random.value;
            float r2 = Random.value;
            if (r1 + r2 > 1f)
            {
                r1 = 1f - r1;
                r2 = 1f - r2;
            }
            Vector3 candidate = v1 + r1 * (v2 - v1) + r2 * (v3 - v1);

            // 1. Confirm precise NavMesh placement
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
            {
                continue;
            }

            Vector3 validPoint = hit.position;

            // 2. Player spawn distance check
            if (Vector3.Distance(validPoint, playerSpawn) < minDistanceFromPlayerSpawn)
            {
                continue;
            }

            // 3. Minimum distance to existing waypoints
            bool tooClose = false;
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (Vector3.Distance(validPoint, waypoints[i]) < currentMinDist)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // 4. Clearance check for walls and obstacles (sampled at waist/chest height above floor)
            Collider[] hitColliders = Physics.OverlapSphere(validPoint + Vector3.up * 0.8f, obstacleClearanceRadius, obstacleMask.value, QueryTriggerInteraction.Ignore);
            bool obstructed = false;
            foreach (Collider c in hitColliders)
            {
                if (playerObj != null && (c.transform == playerObj.transform || c.transform.IsChildOf(playerObj.transform)))
                {
                    continue;
                }
                // Ignore horizontal floor colliders underneath the waypoint
                if (c.bounds.max.y <= validPoint.y + 0.1f)
                {
                    continue;
                }

                obstructed = true;
                break;
            }
            if (obstructed) continue;

            // 5. Reachability check from player spawn
            if (NavMesh.CalculatePath(navMeshSpawn, validPoint, NavMesh.AllAreas, pathTest))
            {
                if (pathTest.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }
            }

            waypoints.Add(validPoint);
        }

        Debug.Log("WaypointManager: Generated " + waypoints.Count + " valid waypoints across the map.");
        CreateInGameVisuals();
    }

    public void CreateInGameVisuals()
    {
        if (!Application.isPlaying) return;

        if (inGameMarkersContainer != null)
        {
            Destroy(inGameMarkersContainer);
        }

        if (waypoints == null || waypoints.Count == 0) return;

        inGameMarkersContainer = new GameObject("InGameWaypointVisuals");
        inGameMarkersContainer.transform.SetParent(transform, false);

        if (waypointNodeMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            waypointNodeMaterial = new Material(sh);
            waypointNodeMaterial.color = new Color(0f, 0.9f, 1f, 0.75f);
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            node.name = "WaypointNode_" + i;
            node.transform.SetParent(inGameMarkersContainer.transform);
            node.transform.position = waypoints[i] + Vector3.up * 0.05f;
            node.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
            node.GetComponent<Renderer>().sharedMaterial = waypointNodeMaterial;
            Collider c = node.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        LineRenderer pathLine = inGameMarkersContainer.AddComponent<LineRenderer>();
        if (waypointPathMaterial == null)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            waypointPathMaterial = new Material(sh);
            waypointPathMaterial.color = new Color(0f, 0.9f, 1f, 0.4f);
        }
        pathLine.material = waypointPathMaterial;
        pathLine.startWidth = 0.06f;
        pathLine.endWidth = 0.06f;
        pathLine.startColor = new Color(0f, 0.9f, 1f, 0.4f);
        pathLine.endColor = new Color(0f, 0.9f, 1f, 0.4f);
        pathLine.useWorldSpace = true;
        pathLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathLine.receiveShadows = false;
        pathLine.loop = true;
        pathLine.positionCount = waypoints.Count;

        for (int i = 0; i < waypoints.Count; i++)
        {
            pathLine.SetPosition(i, waypoints[i] + Vector3.up * 0.1f);
        }

        inGameMarkersContainer.SetActive(showInGameWaypoints);
    }

    public Vector3 GetRandomWaypoint(Vector3 currentPosition, float minDistance = 2f)
    {
        if (waypoints.Count == 0)
        {
            return currentPosition;
        }

        if (waypoints.Count == 1)
        {
            return waypoints[0];
        }

        // Pick a waypoint that is different and far enough from current
        for (int i = 0; i < 10; i++)
        {
            Vector3 chosen = waypoints[Random.Range(0, waypoints.Count)];
            if (Vector3.Distance(chosen, currentPosition) >= minDistance)
            {
                return chosen;
            }
        }

        return waypoints[Random.Range(0, waypoints.Count)];
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || waypoints == null) return;

        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.DrawSphere(waypoints[i] + Vector3.up * 0.3f, 0.4f);
            if (i < waypoints.Count - 1)
            {
                Gizmos.DrawLine(waypoints[i] + Vector3.up * 0.3f, waypoints[i + 1] + Vector3.up * 0.3f);
            }
        }
    }
}
