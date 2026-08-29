using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float currentHealth;

    [Tooltip("Drag the Slider from the enemy's world-space Canvas here.")]
    public Slider healthBar;

    [Tooltip("Health bar always faces the camera.")]
    public bool billboardHealthBar = true;

    private Camera mainCamera;

    void Start()
    {
        // Ensure the enemy has a collider so it can be hit by bullets
        if (GetComponent<Collider>() == null)
        {
            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.height = 2f;
            col.radius = 0.5f;
            col.center = new Vector3(0, 1f, 0); // Typically centers the capsule on a standard humanoid model
        }

        currentHealth = maxHealth;
        mainCamera = Camera.main;

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;
        }
    }

    void LateUpdate()
    {
        if (billboardHealthBar && healthBar != null && mainCamera != null)
        {
            healthBar.transform.parent.forward = mainCamera.transform.forward;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }
}