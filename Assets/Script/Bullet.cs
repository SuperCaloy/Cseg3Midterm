using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 3f;
    public float gravityMultiplier = 1.5f;

    private Rigidbody rb;
    private Color myColor;

    // This global shared ammo color
    public static Color globalAmmoColor = Color.white;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0f;

        // Generate a completely random vibrant color so every bullet and hit is unique!
        myColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
        globalAmmoColor = myColor; // Update next shot too

        Renderer bulletRenderer = GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            bulletRenderer.material.color = myColor;
            if (bulletRenderer.material.HasProperty("_BaseColor"))
            {
                bulletRenderer.material.SetColor("_BaseColor", myColor);
            }
        }

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
                enemy.TakeDamage(10f); // adjust damage per bullet
            }
        }
        else if (hitObject.CompareTag("Obstacle"))
        {
            Renderer hitRenderer = hitObject.GetComponent<Renderer>();
            if (hitRenderer != null)
            {
                Color randomColor = new Color(Random.value, Random.value, Random.value);
                hitRenderer.material.color = randomColor;
            }
        }

        Destroy(gameObject);
    }
}