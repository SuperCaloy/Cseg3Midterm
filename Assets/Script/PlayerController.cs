using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;
    public float kickStrength = 8f;
    // Added the missing semicolon below
    public float upwardLift = 2f;

    private CharacterController charController;
    private Vector3 currentVelocity;

    void Start()
    {
        charController = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Ground check to reset downward velocity
        if (charController.isGrounded && currentVelocity.y < 0f)
        {
            currentVelocity.y = -2f;
        }
        Debug.Log("Grounded: " + charController.isGrounded);

        // Gather WASD or Arrow Key input
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        // Check for run input
        float currentSpeed = walkSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed;
        }

        // Apply horizontal movement
        charController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // Check for jump input
        if (Input.GetKeyDown(KeyCode.Space) && charController.isGrounded)
        {
            // Physics formula for calculating jump velocity
            currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Apply gravity to vertical movement
        currentVelocity.y += gravity * Time.deltaTime;
        charController.Move(currentVelocity * Time.deltaTime);
    } // Added the missing closing brace here for the Update function

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Check if the object we bumped into has a physics Rigidbody
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            // Only kick objects that have the Ball tag
            if (hit.gameObject.CompareTag("Ball"))
            {
                // Calculate the direction to push the ball based on position
                Vector3 pushDirection = hit.gameObject.transform.position - transform.position;

                // Keep the initial push strictly horizontal
                pushDirection.y = 0f;
                pushDirection.Normalize();

                // Add the upward lift to make it fly a little high
                pushDirection.y = upwardLift;

                // Apply the physical force to the ball
                rb.AddForce(pushDirection * kickStrength);
            }
        }
    }
}