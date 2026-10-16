using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum CombatType { Ranged, Melee }
    public enum AIBehaviorType { Proximity, LineOfSight, VisionCone }

    [Header("Combat Settings")]
    public CombatType combatType = CombatType.Ranged;
    public Transform player;
    public float speed = 2.5f;
    public float stoppingDistance = 5f;

    [Header("AI Behavior Type")]
    public AIBehaviorType behaviorType = AIBehaviorType.Proximity;
    public bool isAlerted = false;

    [Header("Proximity Settings")]
    public float proximityRange = 12f;

    [Header("Line of Sight Settings")]
    public float maxSightRange = 30f;

    [Header("Vision Cone Settings")]
    public float minVisionConeAngle = 45f;
    public float maxVisionConeAngle = 110f;
    public float minVisionConeRange = 10f;
    public float maxVisionConeRange = 22f;
    public float visionConeAngle = 75f;
    public float visionConeRange = 16f;

    [Header("Patrol Settings")]
    public float waypointWaitTime = 1.5f;
    private float currentWaitTimer = 0f;
    private Vector3 currentWaypoint;
    private bool hasWaypoint = false;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    private float nextFireTime;

    public float bulletDamage = 10f;
    public float bulletSpeed = 20f;
    public bool canShoot = true;

    [Header("In-Game Visualization")]
    public bool showGizmos = true;
    public bool showInGameGizmo = true;
    public KeyCode toggleGizmoKey = KeyCode.G;
    public float gizmoCloseDistance = 8f;

    private LineRenderer inGameGizmo;
    private static Material gizmoLineMaterial;
    private static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    private NavMeshAgent agent;
    private ProceduralAnimator anim;

    private Vector3 lastPlayerPosition;
    private float playerEstimatedSpeed = 0f;
    private bool isPlayerMovingForced = false;

    void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }

        if (player != null)
        {
            lastPlayerPosition = player.position;
        }

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        anim = GetComponent<ProceduralAnimator>();

        // Randomize exactly one AI behavior at spawn
        behaviorType = (AIBehaviorType)Random.Range(0, 3);

        // Apply level-based scaling for ranges and vision cone bounds
        int level = GetCurrentLevel();
        ApplyLevelScaling(level);

        // Configure NavMeshAgent parameters
        if (combatType == CombatType.Melee)
        {
            stoppingDistance = 1.5f;
            canShoot = false;
        }

        agent.speed = speed;
        agent.stoppingDistance = stoppingDistance;
        agent.acceleration = 12f;
        agent.angularSpeed = 240f;

        // Scale difficulty with stage
        if (GameSessionManager.Instance != null)
        {
            int stage = GameSessionManager.Instance.CurrentStage;
            float multiplier = Mathf.Pow(1.2f, stage - 1);

            speed *= multiplier;
            agent.speed = speed;
            fireRate = Mathf.Max(0.5f, fireRate / multiplier);
            bulletDamage *= multiplier;
        }

        SetupInGameGizmo();
    }

    public int GetCurrentLevel()
    {
        if (GameSessionManager.Instance != null)
        {
            return Mathf.Clamp(GameSessionManager.Instance.CurrentStage, 1, 6);
        }

        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName) && sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int parsedLevel))
        {
            return Mathf.Clamp(parsedLevel, 1, 6);
        }

        return 1;
    }

    public void ApplyLevelScaling(int level)
    {
        level = Mathf.Clamp(level, 1, 6);
        float t = (level - 1f) / 5f;

        // Proximity Range: 6m at Level 1 to 14m at Level 6
        proximityRange = Mathf.Lerp(6f, 14f, t);

        // Line of Sight Range: 12m at Level 1 to 28m at Level 6
        maxSightRange = Mathf.Lerp(12f, 28f, t);

        // Vision Cone Range: min 6 to 16m, max 10 to 24m
        minVisionConeRange = Mathf.Lerp(6f, 16f, t);
        maxVisionConeRange = Mathf.Lerp(10f, 24f, t);

        // Vision Cone Angle: min 35 to 75 deg, max 60 to 115 deg
        minVisionConeAngle = Mathf.Lerp(35f, 75f, t);
        maxVisionConeAngle = Mathf.Lerp(60f, 115f, t);

        if (behaviorType == AIBehaviorType.VisionCone)
        {
            visionConeAngle = Random.Range(minVisionConeAngle, maxVisionConeAngle);
            visionConeRange = Random.Range(minVisionConeRange, maxVisionConeRange);
        }
    }

    public void SetPlayerMovingForTesting(bool moving)
    {
        isPlayerMovingForced = moving;
    }

    public bool IsPlayerMoving()
    {
        if (player == null) return false;
        if (isPlayerMovingForced) return true;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.GetComponentInChildren<CharacterController>();
        if (cc != null && cc.velocity.sqrMagnitude > 0.05f)
        {
            return true;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null) rb = player.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            if (rb.linearVelocity.sqrMagnitude > 0.05f)
            {
                return true;
            }
        }

        if (playerEstimatedSpeed > 0.1f)
        {
            return true;
        }

        return false;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleGizmoKey))
        {
            showInGameGizmo = !showInGameGizmo;
            if (inGameGizmo != null) inGameGizmo.enabled = showInGameGizmo;
        }

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
            if (player == null) return;
        }

        // Track player movement speed for proximity audio detection
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            playerEstimatedSpeed = Vector3.Distance(player.position, lastPlayerPosition) / dt;
            lastPlayerPosition = player.position;
        }

        // Bug 1 fix: Check detection to switch between patrol and combat
        if (!isAlerted)
        {
            if (CheckDetection())
            {
                isAlerted = true;
                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                }
            }
            else
            {
                HandlePatrol();
                return;
            }
        }
        else
        {
            // Transition back to patrol as soon as player leaves detection range or breaks sightline
            if (!IsPlayerInDetectionRange())
            {
                isAlerted = false;
                hasWaypoint = false;
                HandlePatrol();
                return;
            }
        }

        // Alerted: Chase and Attack logic
        HandleCombat();
    }

    void HandlePatrol()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        // Acquire first waypoint or pick a new one upon arrival
        if (!hasWaypoint || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f))
        {
            currentWaitTimer += Time.deltaTime;
            if (currentWaitTimer >= waypointWaitTime || !hasWaypoint)
            {
                currentWaitTimer = 0f;
                if (WaypointManager.Instance != null)
                {
                    currentWaypoint = WaypointManager.Instance.GetRandomWaypoint(transform.position);
                    hasWaypoint = true;
                    agent.isStopped = false;
                    agent.SetDestination(currentWaypoint);
                }
            }
        }
    }

    void HandleCombat()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > stoppingDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;

            // Turn smoothly to face player
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);
            }

            // Melee attack logic
            if (combatType == CombatType.Melee && Time.time >= nextFireTime)
            {
                PunchPlayer();
                nextFireTime = Time.time + fireRate;
            }
        }

        // Ranged attack logic
        if (combatType == CombatType.Ranged && canShoot && Time.time >= nextFireTime)
        {
            if (HasUnobstructedLineOfSight(maxSightRange))
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    public bool IsPlayerInDetectionRange()
    {
        if (player == null) return false;

        switch (behaviorType)
        {
            case AIBehaviorType.Proximity:
                return Vector3.Distance(transform.position, player.position) <= proximityRange;

            case AIBehaviorType.LineOfSight:
                return HasUnobstructedLineOfSight(maxSightRange);

            case AIBehaviorType.VisionCone:
                return Vector3.Distance(transform.position, player.position) <= visionConeRange
                    && HasUnobstructedLineOfSight(visionConeRange);

            default:
                return false;
        }
    }

    public bool CheckDetection()
    {
        if (player == null) return false;

        switch (behaviorType)
        {
            case AIBehaviorType.Proximity:
                return IsPlayerMoving() && Vector3.Distance(transform.position, player.position) <= proximityRange;

            case AIBehaviorType.LineOfSight:
                return HasUnobstructedLineOfSight(maxSightRange);

            case AIBehaviorType.VisionCone:
                return IsInVisionCone();

            default:
                return false;
        }
    }

    bool IsInVisionCone()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > visionConeRange) return false;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Collider col = player.GetComponent<Collider>();
        if (col == null) col = player.GetComponentInChildren<Collider>();
        Vector3 playerCenter = col != null ? col.bounds.center : player.position + Vector3.up * 1.0f;
        Vector3 directionToPlayer = (playerCenter - eyePos).normalized;

        // Angle check against enemy forward facing
        float horizontalAngle = Vector3.Angle(transform.forward, directionToPlayer);
        if (horizontalAngle > visionConeAngle * 0.5f)
        {
            return false;
        }

        // Sightline check to ensure player is not occluded behind an obstacle
        return HasUnobstructedLineOfSight(visionConeRange);
    }

    public bool HasUnobstructedLineOfSight(float maxRange)
    {
        if (player == null) return false;

        Collider col = player.GetComponent<Collider>();
        if (col == null) col = player.GetComponentInChildren<Collider>();
        Vector3 playerTarget = col != null ? col.bounds.center : player.position + Vector3.up * 1.0f;

        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 dir = playerTarget - eyePos;
        float dist = dir.magnitude;

        if (dist > maxRange || dist < 0.01f) return false;

        Vector3 dirNorm = dir / dist;
        Vector3 rayStart = eyePos + dirNorm * 0.2f;
        float rayDist = dist - 0.2f;
        if (rayDist <= 0.05f) return true;

        int hitCount = Physics.RaycastNonAlloc(rayStart, dirNorm, hitBuffer, rayDist + 0.5f, ~0, QueryTriggerInteraction.Ignore);

        // Sort hits by distance
        for (int i = 0; i < hitCount - 1; i++)
        {
            for (int j = i + 1; j < hitCount; j++)
            {
                if (hitBuffer[j].distance < hitBuffer[i].distance)
                {
                    var temp = hitBuffer[i];
                    hitBuffer[i] = hitBuffer[j];
                    hitBuffer[j] = temp;
                }
            }
        }

        for (int i = 0; i < hitCount; i++)
        {
            var h = hitBuffer[i];
            if (h.transform == transform || h.transform.IsChildOf(transform))
                continue;

            if (h.transform == player || h.transform.CompareTag("Player") || h.transform.IsChildOf(player))
            {
                return true;
            }

            // First non-self collider hit is an obstacle or wall
            return false;
        }

        return false;
    }

    void PunchPlayer()
    {
        if (anim != null) anim.Punch();

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph == null) ph = player.GetComponentInChildren<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(bulletDamage * 1.5f);
        }
    }

    void Shoot()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f + transform.forward;
        Vector3 shootDir = (player.position + Vector3.up * 1.2f - spawnPos).normalized;

        GameObject bullet;
        if (enemyBulletPrefab != null)
        {
            bullet = Instantiate(enemyBulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));
        }
        else
        {
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = spawnPos;
            bullet.transform.localScale = Vector3.one * 0.5f;
            bullet.GetComponent<Renderer>().material.color = Color.black;
            bullet.AddComponent<EnemyBullet>();
        }

        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb == null) eb = bullet.AddComponent<EnemyBullet>();
        eb.damage = bulletDamage;

        if (bullet.GetComponent<Collider>() == null)
        {
            bullet.AddComponent<SphereCollider>();
        }

        Rigidbody rbBullet = bullet.GetComponent<Rigidbody>();
        if (rbBullet == null) rbBullet = bullet.AddComponent<Rigidbody>();

        if (rbBullet != null)
        {
            rbBullet.useGravity = false;
            rbBullet.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rbBullet.linearVelocity = shootDir * bulletSpeed;
        }
    }

    public bool ShouldDrawGizmo()
    {
        if (player == null) return false;
        if (isAlerted) return true;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= gizmoCloseDistance) return true;

        return HasUnobstructedLineOfSight(maxSightRange);
    }

    void LateUpdate()
    {
        UpdateInGameGizmo();
    }

    private static Material GetGizmoMaterial()
    {
        if (gizmoLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            gizmoLineMaterial = new Material(shader);
        }
        return gizmoLineMaterial;
    }

    void SetupInGameGizmo()
    {
        if (!Application.isPlaying) return;

        GameObject gizmoObj = new GameObject("InGameAIGizmo");
        gizmoObj.transform.SetParent(transform, false);
        gizmoObj.transform.localPosition = Vector3.zero;

        inGameGizmo = gizmoObj.AddComponent<LineRenderer>();
        inGameGizmo.material = GetGizmoMaterial();
        inGameGizmo.startWidth = 0.08f;
        inGameGizmo.endWidth = 0.08f;
        inGameGizmo.useWorldSpace = true;
        inGameGizmo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        inGameGizmo.receiveShadows = false;
        inGameGizmo.loop = false;
    }

    void UpdateInGameGizmo()
    {
        if (!showInGameGizmo || inGameGizmo == null || !ShouldDrawGizmo())
        {
            if (inGameGizmo != null && inGameGizmo.enabled) inGameGizmo.enabled = false;
            return;
        }

        if (!inGameGizmo.enabled) inGameGizmo.enabled = true;

        Vector3 floorPos = transform.position + Vector3.up * 0.15f;

        switch (behaviorType)
        {
            case AIBehaviorType.VisionCone:
            {
                int arcSegments = 16;
                inGameGizmo.positionCount = arcSegments + 3;
                inGameGizmo.SetPosition(0, floorPos);

                float halfAngle = visionConeAngle * 0.5f;
                for (int s = 0; s <= arcSegments; s++)
                {
                    float angle = -halfAngle + (visionConeAngle * ((float)s / arcSegments));
                    Vector3 rayDir = Quaternion.Euler(0, angle, 0) * transform.forward;
                    inGameGizmo.SetPosition(s + 1, floorPos + rayDir * visionConeRange);
                }
                inGameGizmo.SetPosition(arcSegments + 2, floorPos);

                Color color = isAlerted ? new Color(1f, 0.2f, 0.2f, 0.85f) : new Color(0.2f, 0.75f, 1f, 0.65f);
                inGameGizmo.startColor = color;
                inGameGizmo.endColor = color;
                break;
            }

            case AIBehaviorType.Proximity:
            {
                int circleSegments = 24;
                inGameGizmo.positionCount = circleSegments + 1;
                for (int s = 0; s <= circleSegments; s++)
                {
                    float rad = s * Mathf.PI * 2f / circleSegments;
                    Vector3 offset = new Vector3(Mathf.Sin(rad) * proximityRange, 0, Mathf.Cos(rad) * proximityRange);
                    inGameGizmo.SetPosition(s, floorPos + offset);
                }

                Color color = isAlerted ? new Color(1f, 0.2f, 0.2f, 0.85f) : new Color(1f, 0.85f, 0.2f, 0.65f);
                inGameGizmo.startColor = color;
                inGameGizmo.endColor = color;
                break;
            }

            case AIBehaviorType.LineOfSight:
            {
                int circleSegments = 24;
                inGameGizmo.positionCount = circleSegments + 1;
                for (int s = 0; s <= circleSegments; s++)
                {
                    float rad = s * Mathf.PI * 2f / circleSegments;
                    Vector3 offset = new Vector3(Mathf.Sin(rad) * maxSightRange, 0, Mathf.Cos(rad) * maxSightRange);
                    inGameGizmo.SetPosition(s, floorPos + offset);
                }

                Color color = isAlerted ? new Color(1f, 0.2f, 0.2f, 0.85f) : new Color(0.25f, 1f, 0.35f, 0.65f);
                inGameGizmo.startColor = color;
                inGameGizmo.endColor = color;
                break;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        if (Application.isPlaying && !ShouldDrawGizmo()) return;
        DrawGizmoVisuals();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        DrawGizmoVisuals();
    }

    void DrawGizmoVisuals()
    {
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;

        if (behaviorType == AIBehaviorType.Proximity)
        {
            Gizmos.color = isAlerted ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, proximityRange);
        }
        else if (behaviorType == AIBehaviorType.LineOfSight)
        {
            Gizmos.color = isAlerted ? Color.red : Color.green;
            Gizmos.DrawWireSphere(eyePos, maxSightRange);
        }
        else if (behaviorType == AIBehaviorType.VisionCone)
        {
            Gizmos.color = isAlerted ? Color.red : Color.blue;
            Vector3 forward = transform.forward;
            Quaternion leftRot = Quaternion.Euler(0, -visionConeAngle * 0.5f, 0);
            Quaternion rightRot = Quaternion.Euler(0, visionConeAngle * 0.5f, 0);

            Vector3 leftRay = leftRot * forward * visionConeRange;
            Vector3 rightRay = rightRot * forward * visionConeRange;

            Gizmos.DrawLine(eyePos, eyePos + leftRay);
            Gizmos.DrawLine(eyePos, eyePos + rightRay);
            Gizmos.DrawLine(eyePos + leftRay, eyePos + rightRay);
            Gizmos.DrawLine(eyePos, eyePos + forward * visionConeRange);
        }

        if (!isAlerted && hasWaypoint)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, currentWaypoint + Vector3.up * 0.5f);
        }
    }
}
