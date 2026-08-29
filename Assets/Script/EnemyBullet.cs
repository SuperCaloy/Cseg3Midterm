using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBullet : MonoBehaviour
{
    public float lifeTime = 3f;
    public float damage = 10f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check for PlayerHealth anywhere in the hierarchy of the hit object
        PlayerHealth ph = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (ph == null) ph = collision.gameObject.GetComponentInChildren<PlayerHealth>();

        if (ph != null)
        {
            ph.TakeDamage(damage);
        }

        // Destroy the bullet on any collision
        Destroy(gameObject);
    }
}
