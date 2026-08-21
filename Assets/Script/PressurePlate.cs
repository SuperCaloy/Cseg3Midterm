using UnityEngine;

public class PressurePlate : MonoBehaviour
{
    [Tooltip("Drag the Light object you want to control into this slot.")]
    public Light targetLight;

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    void OnCollisionEnter(Collision collision)
    {
        // Check if the physical object touching the plate is the Player
        if (collision.gameObject.CompareTag("Player"))
        {
            // Make sure you actually assigned a light in the Inspector!
            if (targetLight != null)
            {
                // The "!" symbol means "Not". 
                // This line sets the light to the exact OPPOSITE of whatever it is right now.
                // If it is ON, it becomes OFF. If it is OFF, it becomes ON.
                targetLight.enabled = !targetLight.enabled;
            }
            else
            {
                Debug.LogWarning("Pressure Plate was stepped on, but no Light was assigned in the Inspector!");
            }
        }
    }
}