using UnityEngine;

// This forces Unity to make sure a Rigidbody is attached to whatever gets this script
[RequireComponent(typeof(Rigidbody))]
public class FastBall : MonoBehaviour
{
    [Tooltip("How much faster the ball should fall compared to normal gravity.")]
    public float gravityMultiplier = 2.5f;

    private Rigidbody rb;

    void Start()
    {
        // Grab the Rigidbody from the ball so we can apply forces to it
        rb = GetComponent<Rigidbody>();

        // Upgrade collision detection so the ball doesn't clip into walls when kicked hard
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Create a slippery, bouncy physics material completely through code
        PhysicsMaterial antiStuckMaterial = new PhysicsMaterial("AntiStuck");
        antiStuckMaterial.dynamicFriction = 0f;
        antiStuckMaterial.staticFriction = 0f;
        antiStuckMaterial.bounciness = 0.6f;

        // Force the engine to use our Zero Friction and High Bounce when hitting other objects
        antiStuckMaterial.frictionCombine = PhysicsMaterialCombine.Minimum;
        antiStuckMaterial.bounceCombine = PhysicsMaterialCombine.Maximum;

        // Apply this perfect physics material to the ball's collider
        Collider ballCollider = GetComponent<Collider>();
        if (ballCollider != null)
        {
            ballCollider.material = antiStuckMaterial;
        }
    }

    // FixedUpdate is specifically used for physics calculations
    void FixedUpdate()
    {
        // Unity automatically applies 1x gravity to Rigidbodies.
        // If we want 2.5x gravity, we need to manually add the remaining 1.5x gravity.
        float extraGravity = gravityMultiplier - 1f;

        // Apply the extra downward force continuously
        Vector3 extraDownwardForce = Physics.gravity * extraGravity;
        rb.AddForce(extraDownwardForce, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if the object we bumped into has the "Obstacle" tag
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            // Get the visual Renderers of both the ball and the obstacle
            Renderer myRenderer = GetComponent<Renderer>();
            Renderer obstacleRenderer = collision.gameObject.GetComponent<Renderer>();

            // Make sure both objects actually have materials to swap
            if (myRenderer != null && obstacleRenderer != null)
            {
                // Temporarily store the current colors
                Color myCurrentColor = myRenderer.material.color;
                Color obstacleCurrentColor = obstacleRenderer.material.color;

                // Swap the colors
                myRenderer.material.color = obstacleCurrentColor;
                obstacleRenderer.material.color = myCurrentColor;
            }
        }
    }
}