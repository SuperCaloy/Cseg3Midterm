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

    [Header("Skill Settings")]
    public float rollSpeed = 15f;
    public float rollDuration = 0.5f;
    public float rollCooldown = 3f;
    public float teleportDistance = 5f;
    public float teleportCooldown = 5f;

    private CharacterController charController;
    private Animator animator;
    private Vector3 currentVelocity;

    private CharacterType currentCharacter = CharacterType.CharacterA;
    private int rollCharges = 2;
    private float nextRollTime = 0f;
    private float nextTeleportTime = 0f;
    private bool hasDoubleJumped = false;
    private bool isRolling = false;
    private float rollEndTime = 0f;
    private Vector3 rollDirection;

    void Start()
    {
        charController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Load selected character
        currentCharacter = CharacterSelectionData.SelectedCharacter;
    }

    void Update()
    {
        HandleSkills();

        if (isRolling)
        {
            charController.Move(rollDirection * rollSpeed * Time.deltaTime);
            if (Time.time >= rollEndTime)
            {
                isRolling = false;
            }
            return; // Skip normal movement while rolling
        }

        // 1. Ground Check & Jumping
        if (charController.isGrounded)
        {
            if (currentVelocity.y < 0f)
            {
                currentVelocity.y = -2f;
            }
            
            hasDoubleJumped = false;

            if (Input.GetButtonDown("Jump"))
            {
                currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (animator != null) animator.SetTrigger("Jump");
            }
        }
        else
        {
            // Character B: Double Jump
            if (currentCharacter == CharacterType.CharacterB && !hasDoubleJumped && Input.GetButtonDown("Jump"))
            {
                currentVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                hasDoubleJumped = true;
                if (animator != null) animator.SetTrigger("Jump"); // Or a specific double jump trigger
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

    private void HandleSkills()
    {
        // Regenerate Roll Charges
        if (currentCharacter == CharacterType.CharacterA)
        {
            if (rollCharges < 2 && Time.time >= nextRollTime)
            {
                rollCharges++;
                nextRollTime = Time.time + rollCooldown; // Start cooldown for the next charge
            }
        }

        // Skill activation with 'E' key
        if (Input.GetKeyDown(KeyCode.E))
        {
            switch (currentCharacter)
            {
                case CharacterType.CharacterA:
                    if (rollCharges > 0 && !isRolling)
                    {
                        rollCharges--;
                        if (rollCharges == 1) // If we just dropped below max, start cooldown timer
                        {
                            nextRollTime = Time.time + rollCooldown;
                        }
                        
                        isRolling = true;
                        rollEndTime = Time.time + rollDuration;
                        
                        // Roll in current input direction, or forward if no input
                        float inputX = Input.GetAxisRaw("Horizontal");
                        float inputZ = Input.GetAxisRaw("Vertical");
                        Vector3 inputDir = (transform.right * inputX + transform.forward * inputZ).normalized;
                        rollDirection = inputDir.magnitude > 0.1f ? inputDir : transform.forward;
                        
                        if (animator != null) animator.SetTrigger("Roll"); // Optional: Needs animation
                    }
                    break;
                case CharacterType.CharacterC:
                    if (Time.time >= nextTeleportTime)
                    {
                        nextTeleportTime = Time.time + teleportCooldown;
                        
                        // Teleport forward
                        charController.Move(transform.forward * teleportDistance);
                        
                        // Ensure y velocity doesn't get messed up (though CharacterController handles it decently)
                    }
                    break;
            }
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