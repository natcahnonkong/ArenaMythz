using UnityEngine;

public class RandomResource : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject resourcePrefab;
    public int numberOfResources = 10;
    public Vector2 spawnAreaSize = new Vector2(50f, 50f);
    
    [Header("Height Settings")]
    public float spawnHeight = 0f;
    public bool useRaycast = false;
    public LayerMask groundLayer;
    
    void Start()
    {
        SpawnResources();
    }
    
    void SpawnResources()
    {
        for (int i = 0; i < numberOfResources; i++)
        {
            Vector3 randomPosition = GetRandomPosition();
            Instantiate(resourcePrefab, randomPosition, Quaternion.identity);
        }
    }
    
    Vector3 GetRandomPosition()
    {
        float x = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
        float z = Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2);
        
        Vector3 position = transform.position + new Vector3(x, spawnHeight, z);
        
        // Optional: Use raycast to place on terrain
        if (useRaycast)
        {
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f, groundLayer))
            {
                position = hit.point;
            }
        }
        
        return position;
    }
    
    // Visualize spawn area in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up * spawnHeight, 
                           new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y));
    }
}