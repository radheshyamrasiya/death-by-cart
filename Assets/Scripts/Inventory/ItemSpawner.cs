using UnityEngine;

/// <summary>
/// Spawns pickupable items (red boxes) around the world at runtime.
/// Attach to any empty GameObject. Uses ItemDataLibrary for item data.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private int itemCount = 15;
    [SerializeField] private float spawnRadius = 30f;
    [SerializeField] private float spawnHeight = 0.5f;
    [SerializeField] private float minDistance = 5f;  // Min distance between items
    [SerializeField] private float boxScale = 0.4f;

    private void Start()
    {
        // Wait a frame for ItemDataLibrary to initialize
        Invoke(nameof(SpawnItems), 0.1f);
    }

    private void SpawnItems()
    {
        if (ItemDataLibrary.Instance == null)
        {
            Debug.LogWarning("[SPAWNER] No ItemDataLibrary found!");
            return;
        }

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = itemCount * 10;

        while (spawned < itemCount && attempts < maxAttempts)
        {
            attempts++;

            // Random position around origin
            Vector2 rndCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 pos = new Vector3(rndCircle.x, spawnHeight, rndCircle.y);

            // Avoid spawning too close to center (where cart is)
            if (pos.magnitude < 5f) continue;

            // Try raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 10f, Vector3.down, out hit, 20f))
            {
                pos = hit.point + Vector3.up * spawnHeight;
            }

            // Create the red box
            ItemData itemData = ItemDataLibrary.Instance.GetRandomSmallItem();
            GameObject itemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            itemObj.name = $"Item_{itemData.itemName}_{spawned}";
            itemObj.transform.position = pos;
            itemObj.transform.localScale = Vector3.one * boxScale;

            // Remove default collider (WorldItem will add trigger)
            Object.Destroy(itemObj.GetComponent<BoxCollider>());

            // Add trigger collider
            BoxCollider trigger = itemObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = Vector3.one * 2f; // Bigger trigger than visual

            // Material — bright colored
            Renderer rend = itemObj.GetComponent<Renderer>();
            if (rend != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", itemData.itemColor);
                block.SetColor("_EmissionColor", itemData.itemColor * 0.3f);
                rend.SetPropertyBlock(block);
            }

            // Add WorldItem component
            WorldItem worldItem = itemObj.AddComponent<WorldItem>();
            worldItem.itemData = itemData;

            spawned++;
        }

        Debug.Log($"[SPAWNER] ✅ Spawned {spawned} items across the map!");
    }
}
