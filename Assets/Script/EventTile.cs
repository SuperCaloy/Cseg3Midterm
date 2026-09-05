using UnityEngine;

public class EventTile : MonoBehaviour
{
    private bool hasTriggered = false;
    public GameObject ammoPrefab;
    public GameObject[] enemyPrefabs;

    void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (Random.value > 0.5f)
            {
                TriggerAmmo();
            }
            else
            {
                TriggerAmbush();
            }
        }
    }

    void TriggerAmmo()
    {
        Debug.Log("Event Tile: Ammo Spawned!");
        if (ammoPrefab != null)
        {
            Instantiate(ammoPrefab, transform.position + Vector3.up * 1f, Quaternion.identity);
        }
    }

    void TriggerAmbush()
    {
        GameUIManager ui = FindObjectOfType<GameUIManager>();
        if (ui != null) ui.ShowNotification("Ambush triggered!");

        Debug.Log("Event Tile: Ambush!");
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // Spawn 3 enemies around the tile
            for (int i = 0; i < 3; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-2f, 2f), 1f, Random.Range(-2f, 2f));
                GameObject chosenPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                GameObject enemy = Instantiate(chosenPrefab, transform.position + offset, Quaternion.identity);
                
                EnemyAI ai = enemy.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    ai.speed *= 1.5f; // Faster ambush enemies
                    ai.fireRate *= 0.5f;
                }
            }
        }
    }
}
