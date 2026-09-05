using UnityEngine;

public class ProceduralAnimator : MonoBehaviour
{
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    public float swingSpeed = 10f;
    public float swingAngle = 45f;

    private Rigidbody rb;
    private float animationTime = 0f;
    private float punchTime = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (rb == null) return;

        if (punchTime > 0f)
        {
            punchTime -= Time.deltaTime;
            // Rapid punch motion
            float punchAngle = Mathf.Lerp(0, -90f, punchTime / 0.2f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(punchAngle, 0, 0);
            return; // Override running animation for right arm
        }

        // Check horizontal velocity
        Vector3 vel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float speed = vel.magnitude;

        if (speed > 0.1f)
        {
            // Increase animation time based on speed
            animationTime += Time.deltaTime * swingSpeed * (speed / 3f);

            float sinVal = Mathf.Sin(animationTime);

            // Swing arms and legs opposite to each other
            float angle = sinVal * swingAngle;

            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(angle, 0, 0);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(-angle, 0, 0);
            
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(-angle, 0, 0);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(angle, 0, 0);
        }
        else
        {
            // Lerp back to idle
            LerpToIdle(leftArm);
            LerpToIdle(rightArm);
            LerpToIdle(leftLeg);
            LerpToIdle(rightLeg);
        }
    }

    private void LerpToIdle(Transform t)
    {
        if (t != null)
        {
            t.localRotation = Quaternion.Slerp(t.localRotation, Quaternion.identity, Time.deltaTime * 5f);
        }
    }

    public void Punch()
    {
        punchTime = 0.2f;
    }
}
