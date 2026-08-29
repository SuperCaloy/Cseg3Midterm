using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 1.2f;
    public float gravity = -19.62f;
    public float kickStrength = 8f;
    public float upwardLift = 2f;

    private CharacterController charController;
    private Animator animator;
    private Vector3 currentVelocity;

    void Start()
    {
        charController = GetComponent<CharacterController>();

        // Find Animator on this object or any child mesh
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // 1. Ground Check & Jumping
        if (charController.isGrounded)
        {
            // Reset downward velocity when touching the ground
            if (currentVelocity.y < 0f)
            {
                currentVelocity.y = -2f;
            }

            // Fire jump physics and animation trigger
            if (Input.GetButtonDown("Jump"))
            {
                currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

                if (animator != null)
                {
                    animator.SetTrigger("Jump");
                }
            }
        }

        // 2. Apply Gravity & Vertical Movement
        currentVelocity.y += gravity * Time.deltaTime;
        charController.Move(currentVelocity * Time.deltaTime);

        // 3. Horizontal Movement
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        charController.Move(moveDirection * currentSpeed * Time.deltaTime);

        // 4. Update Animator Movement Parameter
        if (animator != null)
        {
            bool isMoving = moveDirection.magnitude > 0.1f;
            animator.SetBool("isWalking", isMoving);
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rb = hit.collider.attachedRigidbody;

        if (rb != null && !rb.isKinematic)
        {
            if (hit.gameObject.CompareTag("Ball"))
            {
                Vector3 pushDirection = hit.gameObject.transform.position - transform.position;
                pushDirection.y = 0f;
                pushDirection.Normalize();
                pushDirection.y = upwardLift;

                rb.AddForce(pushDirection * kickStrength);
            }
        }
    }
}