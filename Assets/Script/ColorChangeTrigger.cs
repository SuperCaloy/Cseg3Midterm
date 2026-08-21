using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorChangeTrigger : MonoBehaviour
{
    [Tooltip("How long the obstacles stay changed before reverting (in seconds).")]
    public float revertDelay = 60f;

    // Remember original colors globally so we don't accidentally save a random color as the original
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();

    // Keep track of the timer so we can restart it if the player steps back in
    private Coroutine activeTimer;

    // OnTriggerEnter is called when another collider enters this object's trigger collider
    void OnTriggerEnter(Collider other)
    {
        // Check if the object stepping into the trigger is the Player
        if (other.CompareTag("Player"))
        {
            // If the timer is already running, stop it to restart the process
            if (activeTimer != null)
            {
                StopCoroutine(activeTimer);
            }

            // Change colors right now, then start a fresh 60 second timer
            ChangeObstacleColors();
            activeTimer = StartCoroutine(RevertColorsRoutine());
        }
    }

    void ChangeObstacleColors()
    {
        GameObject[] obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
        int count = obstacles.Length;

        // 1. Create a list of visually distinct colors based on how many obstacles there are
        List<Color> distinctColors = new List<Color>();
        for (int i = 0; i < count; i++)
        {
            // Divide the color wheel evenly to guarantee no two colors are the same
            float hue = (float)i / count;

            // Convert hue to an actual color (hue, saturation=1, brightness=1)
            distinctColors.Add(Color.HSVToRGB(hue, 1f, 1f));
        }

        // 2. Shuffle the colors randomly like a deck of cards
        for (int i = 0; i < distinctColors.Count; i++)
        {
            Color temp = distinctColors[i];
            int randomIndex = Random.Range(i, distinctColors.Count);
            distinctColors[i] = distinctColors[randomIndex];
            distinctColors[randomIndex] = temp;
        }

        // 3. Assign the unique colors to each obstacle
        for (int i = 0; i < count; i++)
        {
            GameObject obs = obstacles[i];
            Renderer obsRenderer = obs.GetComponent<Renderer>();

            if (obsRenderer != null)
            {
                // Only store the original color if we haven't saved it yet
                if (!originalColors.ContainsKey(obsRenderer))
                {
                    originalColors.Add(obsRenderer, obsRenderer.material.color);
                }

                // Assign the guaranteed unique random color from our shuffled list
                obsRenderer.material.color = distinctColors[i];
            }
        }
    }

    IEnumerator RevertColorsRoutine()
    {
        // Pause this specific function for 60 seconds (1 minute)
        yield return new WaitForSeconds(revertDelay);

        // Timer finished! Loop through our saved Dictionary and put the original colors back
        foreach (KeyValuePair<Renderer, Color> item in originalColors)
        {
            Renderer rend = item.Key;
            Color origColor = item.Value;

            // Double-check the object wasn't destroyed while we waited
            if (rend != null)
            {
                rend.material.color = origColor;
            }
        }

        // Clear the active timer since it finished
        activeTimer = null;
    }
}