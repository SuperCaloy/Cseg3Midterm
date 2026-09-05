using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public enum CombatType { Ranged, Melee }
    public CombatType combatType = CombatType.Ranged;

    public Transform player;
    public float speed = 1.5f; // Slowness
    public float stoppingDistance = 5f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    private float nextFireTime;

    public float bulletDamage = 10f;
    public float bulletSpeed = 20f;
    public bool canShoot = true;
    
    private Rigidbody rb;
    private ProceduralAnimator anim;

    void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.freezeRotation = true; // prevent falling over
        rb.isKinematic = false; 
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        anim = GetComponent<ProceduralAnimator>();

        // Set stopping distance based on combat type
        if (combatType == CombatType.Melee)
        {
            stoppingDistance = 1.5f;
            canShoot = false;
        }

        // Scale difficulty with Stage
        if (GameSessionManager.Instance != null)
        {
            int stage = GameSessionManager.Instance.CurrentStage;
            float multiplier = Mathf.Pow(1.2f, stage - 1); // 20% harder per stage
            
            speed *= multiplier;
            fireRate = Mathf.Max(0.5f, fireRate / multiplier);
            bulletDamage *= multiplier;
        }
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Basic follow logic
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Look at player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep horizontal
        if (distanceToPlayer > stoppingDistance)
        {
            // Boids-style Obstacle Avoidance (Raycast Steering) + Jumping
            Vector3 desiredDir = direction;
            float rayDist = 2.5f; // Look ahead distance
            
            // Check if there's a wall directly in front (Low ray for jumping, Mid ray for steering)
            bool hitLow = Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit lowHit, rayDist);
            bool hitMid = Physics.Raycast(transform.position + Vector3.up * 1.5f, transform.forward, out RaycastHit midHit, rayDist);
            
            if (hitLow || hitMid)
            {
                RaycastHit hit = hitMid ? midHit : lowHit;
                if (!hit.transform.CompareTag("Player") && !hit.transform.CompareTag("Enemy"))
                {
                    // If we hit something low, but NOT something mid-height, it's a short obstacle! We can jump it.
                    if (hitLow && !hitMid && IsGrounded())
                    {
                        rb.AddForce(Vector3.up * 5f, ForceMode.Impulse); // Small jump
                    }
                    else
                    {
                        // It's a tall wall! Try to steer left or right
                        Vector3 leftRay = Quaternion.Euler(0, -45, 0) * transform.forward;
                        Vector3 rightRay = Quaternion.Euler(0, 45, 0) * transform.forward;
                        
                        bool hitLeft = Physics.Raycast(transform.position + Vector3.up * 1f, leftRay, rayDist);
                        bool hitRight = Physics.Raycast(transform.position + Vector3.up * 1f, rightRay, rayDist);
                        
                        if (!hitLeft && hitRight)
                        {
                            desiredDir = leftRay; // Steer left
                        }
                        else if (hitLeft && !hitRight)
                        {
                            desiredDir = rightRay; // Steer right
                        }
                        else if (!hitLeft && !hitRight)
                        {
                            // Both open, pick the one closer to player
                            float dLeft = Vector3.Distance(transform.position + leftRay * rayDist, player.position);
                            float dRight = Vector3.Distance(transform.position + rightRay * rayDist, player.position);
                            desiredDir = dLeft < dRight ? leftRay : rightRay;
                        }
                        else 
                        {
                            // Both blocked, just turn around or try perpendicular
                            desiredDir = Quaternion.Euler(0, 90, 0) * transform.forward;
                        }
                    }
                }
            }

            // Smoothly rotate towards desired direction (avoidance or player)
            if (desiredDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(desiredDir), Time.deltaTime * 5f);
            }

            // Move forward based on current rotation (so we actually move where we steer)
            Vector3 moveVelocity = transform.forward * speed;
            moveVelocity.y = rb.linearVelocity.y; // maintain gravity
            rb.linearVelocity = moveVelocity;
        }
        else
        {
            // Stop moving but keep looking at player
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
            }
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);

            // Melee Attack Logic
            if (combatType == CombatType.Melee && Time.time >= nextFireTime)
            {
                PunchPlayer();
                nextFireTime = Time.time + fireRate;
            }
        }

        // Ranged Attack Logic
        if (combatType == CombatType.Ranged && canShoot && Time.time >= nextFireTime)
        {
            if (HasLineOfSight())
            {
                Shoot();
                nextFireTime = Time.time + fireRate;
            }
        }
    }

    void PunchPlayer()
    {
        if (anim != null) anim.Punch();

        // Deal damage
        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(bulletDamage * 1.5f); // Melee hits harder
        }
    }

    bool HasLineOfSight()
    {
        if (player == null) return false;
        
        Vector3 rayStart = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f + transform.forward;
        Vector3 rayEnd = player.position + Vector3.up * 1.5f;
        Vector3 direction = rayEnd - rayStart;
        float distance = direction.magnitude;

        if (Physics.Raycast(rayStart, direction.normalized, out RaycastHit hit, distance))
        {
            if (hit.transform == player || hit.transform.CompareTag("Player"))
            {
                return true; 
            }
            return false; 
        }
        
        return true; 
    }

    void Shoot()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f + transform.forward;
        Vector3 shootDir = (player.position + Vector3.up * 1.5f - spawnPos).normalized; 
        
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
    
    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.2f);
    }
}
