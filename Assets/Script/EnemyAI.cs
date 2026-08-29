using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float speed = 1.5f; // Slowness
    public float stoppingDistance = 5f;

    [Header("Shooting")]
    public GameObject enemyBulletPrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    private float nextFireTime;

    public bool canShoot = true;
    
    private Rigidbody rb;

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
        rb.isKinematic = false; // Fix warning about setting linearVelocity on kinematic body
    }

    void FixedUpdate()
    {
        if (player == null) return;

        // Basic follow logic
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Look at player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Keep horizontal
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 5f);
        }

        if (distanceToPlayer > stoppingDistance)
        {
            // Move using physics so we don't pass through walls
            Vector3 moveVelocity = transform.forward * speed;
            moveVelocity.y = rb.linearVelocity.y; // maintain gravity
            rb.linearVelocity = moveVelocity;
        }
        else
        {
            // Stop moving
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // Always shoot if canShoot (even while following)
        if (canShoot && Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    public float bulletDamage = 10f;
    public float bulletSpeed = 20f;

    void Shoot()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f + transform.forward;
        Vector3 shootDir = (player.position + Vector3.up * 1.5f - spawnPos).normalized; // Aim slightly up at body
        
        GameObject bullet;
        if (enemyBulletPrefab != null)
        {
            bullet = Instantiate(enemyBulletPrefab, spawnPos, Quaternion.LookRotation(shootDir));
        }
        else
        {
            // Auto-generate a simple bullet if the user hasn't assigned a prefab
            bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.transform.position = spawnPos;
            bullet.transform.localScale = Vector3.one * 0.5f;
            bullet.GetComponent<Renderer>().material.color = Color.black;
            bullet.AddComponent<EnemyBullet>();
        }
        
        EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
        if (eb == null) eb = bullet.AddComponent<EnemyBullet>();
        eb.damage = bulletDamage;
        
        // Ensure there is a Collider for OnCollisionEnter to work
        if (bullet.GetComponent<Collider>() == null)
        {
            bullet.AddComponent<SphereCollider>();
        }

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null) rb = bullet.AddComponent<Rigidbody>();
        
        if (rb != null)
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Prevent tunneling through the player
            rb.linearVelocity = shootDir * bulletSpeed; // Configurable Bullet speed
        }
    }
}
