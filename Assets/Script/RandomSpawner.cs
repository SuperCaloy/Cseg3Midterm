using UnityEngine;
using System.Collections.Generic;

public class RandomSpawner : MonoBehaviour
{
    public GameObject[] prefabsToSpawn;
    public float spawnInterval = 5f;
    public int maxConcurrent = 10;
    
    [Header("Proximity Spawning")]
    public bool spawnNearPlayer = false;
    public float minSpawnRadius = 10f;
    public float maxSpawnRadius = 25f;
    private Transform player;

    public List<BoxCollider> spawnAreas = new List<BoxCollider>();

    private float nextSpawnTime;
    private List<GameObject> activeSpawns = new List<GameObject>();

    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        // Cleanup nulls (destroyed objects)
        activeSpawns.RemoveAll(item => item == null);

        if (Time.time >= nextSpawnTime)
        {
            nextSpawnTime = Time.time + spawnInterval;
            
            if (activeSpawns.Count < maxConcurrent && prefabsToSpawn != null && prefabsToSpawn.Length > 0)
            {
                SpawnObject();
            }
        }
    }

    void SpawnObject()
    {
        Vector3 spawnPos = Vector3.zero;
        bool foundPos = false;

        if (spawnNearPlayer && player != null)
        {
            // Try to find a valid spot near player
            for (int i = 0; i < 10; i++) // max 10 attempts
            {
                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minSpawnRadius, maxSpawnRadius);
                Vector3 testPos = player.position + new Vector3(randomCircle.x, 10f, randomCircle.y);
                
                // Raycast down to find floor
                if (Physics.Raycast(testPos, Vector3.down, out RaycastHit hit, 20f))
                {
                    if (hit.collider.gameObject.name.Contains("Floor") || hit.collider.gameObject.name.Contains("Plane"))
                    {
                        spawnPos = hit.point + Vector3.up * 1.0f; // float slightly above floor
                        foundPos = true;
                        break;
                    }
                }
            }
        }
        
        // Fallback to random area if proximity fails or is disabled
        if (!foundPos && spawnAreas.Count > 0)
        {
            BoxCollider area = spawnAreas[Random.Range(0, spawnAreas.Count)];
            if (area != null)
            {
                Bounds bounds = area.bounds;
                float padding = 0.5f; 
                spawnPos = new Vector3(
                    Random.Range(bounds.min.x + padding, bounds.max.x - padding),
                    bounds.max.y + 1.0f,
                    Random.Range(bounds.min.z + padding, bounds.max.z - padding)
                );
                foundPos = true;
            }
        }

        if (foundPos)
        {
            GameObject chosenPrefab = prefabsToSpawn[Random.Range(0, prefabsToSpawn.Length)];
            GameObject newObj = Instantiate(chosenPrefab, spawnPos, Quaternion.identity);
            activeSpawns.Add(newObj);
        }
    }
}
