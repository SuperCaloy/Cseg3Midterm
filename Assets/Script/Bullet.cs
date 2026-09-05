using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 3f;
    public float gravityMultiplier = 1.5f;
    public float damage = 20f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0f;

        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (gravityMultiplier > 1f && rb != null)
        {
            float extraGravity = gravityMultiplier - 1f;
            Vector3 extraDownwardForce = Physics.gravity * extraGravity;
            rb.AddForce(extraDownwardForce, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;

        if (hitObject.CompareTag("Enemy"))
        {
            EnemyHealth enemy = hitObject.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                // Headshot multiplier!
                // Enemy pivot is at feet (y = 0), head is near y = 2.0
                float impactHeight = collision.contacts[0].point.y - hitObject.transform.position.y;
                float finalDamage = damage;
                
                if (impactHeight > 1.5f)
                {
                    finalDamage *= 2.0f; // 2x Headshot!
                    Debug.Log("Headshot! Damage: " + finalDamage);
                }
                
                enemy.TakeDamage(finalDamage); 
            }
        }

        Destroy(gameObject);
    }
}