using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    public float lifeTime = 3f;
    public float gravityMultiplier = 1.5f;

    private Rigidbody rb;
    private Color myColor;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = 0f;

        Destroy(gameObject, lifeTime);
    }

    public void SetColor(Color color)
    {
        myColor = color;
        Renderer bulletRenderer = GetComponent<Renderer>();
        if (bulletRenderer != null)
        {
            bulletRenderer.material.color = myColor;
            if (bulletRenderer.material.HasProperty("_BaseColor"))
            {
                bulletRenderer.material.SetColor("_BaseColor", myColor);
            }
        }
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

    public float damage = 20f; // Player bullet damage

    void OnCollisionEnter(Collision collision)
    {
        GameObject hitObject = collision.gameObject;

        if (hitObject.CompareTag("Enemy"))
        {
            EnemyHealth enemy = hitObject.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); 
            }
        }
        else if (hitObject.CompareTag("Obstacle"))
        {
            // If it's a compound object (like our primitive furniture), find the root
            Transform rootObstacle = hitObject.transform;
            if (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("Obstacle"))
            {
                rootObstacle = hitObject.transform.parent;
            }

            // Color all renderers in this compound object
            Renderer[] renderers = rootObstacle.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r != null) r.material.color = myColor;
            }
            
            // Notify ObjectiveManager using the root parent
            ObjectiveManager objManager = FindObjectOfType<ObjectiveManager>();
            if (objManager != null)
            {
                objManager.OnObjectColored(rootObstacle.gameObject, myColor);
            }
        }

        Destroy(gameObject);
    }
}