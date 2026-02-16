using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawns zombies around the map. Creates capsule-shaped zombie GameObjects
/// with NavMeshAgent and ZombieAI. Assigns random zombie types.
/// Attach to any GameObject (e.g. the Cart or an empty "GameManager").
/// </summary>
public class ZombieSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private int zombieCount = 8;
    [SerializeField] private float spawnRadius = 40f;
    [SerializeField] private float minDistFromPlayer = 15f;
    [SerializeField] private float spawnHeight = 1f;

    [Header("Zombie Colors")]
    [SerializeField] private Color shamblerColor = new Color(0.3f, 0.5f, 0.3f);  // Sickly green
    [SerializeField] private Color runnerColor = new Color(0.7f, 0.3f, 0.3f);    // Reddish
    [SerializeField] private Color listenerColor = new Color(0.4f, 0.3f, 0.6f);  // Purple
    [SerializeField] private Color bruteColor = new Color(0.5f, 0.4f, 0.3f);     // Brown

    private void Start()
    {
        // Wait a frame for NavMesh to be ready, then spawn
        Invoke(nameof(SpawnZombies), 0.5f);
    }

    private void SpawnZombies()
    {
        Transform player = null;
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (cartInteraction != null) player = cartInteraction.transform;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = zombieCount * 10;

        while (spawned < zombieCount && attempts < maxAttempts)
        {
            attempts++;

            // Random position
            Vector2 randomXZ = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = transform.position + new Vector3(randomXZ.x, spawnHeight, randomXZ.y);

            // Check min distance from player
            if (player != null)
            {
                float distToPlayer = Vector3.Distance(spawnPos, player.position);
                if (distToPlayer < minDistFromPlayer) continue;
            }

            // Snap to NavMesh
            if (!NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                continue;

            spawnPos = hit.position;

            // Pick random type
            ZombieAI.ZombieType type = (ZombieAI.ZombieType)Random.Range(0, 4);

            // Create zombie
            GameObject zombie = CreateZombieObject(spawnPos, type, spawned);
            if (zombie != null)
            {
                spawned++;
                Debug.Log($"[SPAWN] 🧟 Zombie #{spawned} ({type}) at {spawnPos}");
            }
        }

        Debug.Log($"[SPAWN] ✅ Spawned {spawned}/{zombieCount} zombies ({attempts} attempts)");
    }

    private GameObject CreateZombieObject(Vector3 position, ZombieAI.ZombieType type, int index)
    {
        // Body — capsule
        GameObject zombie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        zombie.name = $"Zombie_{type}_{index}";
        zombie.transform.position = position;
        zombie.transform.localScale = GetScaleForType(type);
        zombie.layer = 0;
        zombie.tag = "Untagged";

        // Color
        Renderer rend = zombie.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = GetColorForType(type);
            // Slight emissive so they're visible
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", GetColorForType(type) * 0.2f);
            rend.material = mat;
        }

        // Rigidbody (kinematic — NavMesh controls movement)
        Rigidbody rb = zombie.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Remove default CapsuleCollider, add a trigger for hit detection
        var existingCol = zombie.GetComponent<CapsuleCollider>();
        if (existingCol != null) existingCol.isTrigger = false; // Keep as solid

        // NavMeshAgent
        NavMeshAgent agent = zombie.AddComponent<NavMeshAgent>();
        agent.speed = 2f;
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.angularSpeed = 200f;

        // ZombieAI
        ZombieAI ai = zombie.AddComponent<ZombieAI>();
        // Set zombie type via serialized field workaround
        var typeField = ai.GetType().GetField("zombieType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null) typeField.SetValue(ai, type);

        // Add "eyes" — small sphere on front to show facing direction
        GameObject eyes = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eyes.name = "Eyes";
        eyes.transform.SetParent(zombie.transform);
        eyes.transform.localPosition = new Vector3(0, 0.35f, 0.35f);
        eyes.transform.localScale = new Vector3(0.3f, 0.15f, 0.15f);
        var eyeCol = eyes.GetComponent<Collider>();
        if (eyeCol != null) Object.Destroy(eyeCol);

        Renderer eyeRend = eyes.GetComponent<Renderer>();
        if (eyeRend != null)
        {
            Material eyeMat = new Material(Shader.Find("Standard"));
            eyeMat.color = type == ZombieAI.ZombieType.Listener
                ? new Color(0.5f, 0.5f, 0.5f) // Cloudy eyes for blind zombie
                : new Color(1f, 0.2f, 0.1f);   // Red glowing eyes
            eyeMat.EnableKeyword("_EMISSION");
            eyeMat.SetColor("_EmissionColor", eyeMat.color * 0.5f);
            eyeRend.material = eyeMat;
        }

        return zombie;
    }

    private Vector3 GetScaleForType(ZombieAI.ZombieType type)
    {
        switch (type)
        {
            case ZombieAI.ZombieType.Shambler: return new Vector3(0.5f, 0.9f, 0.5f);
            case ZombieAI.ZombieType.Runner: return new Vector3(0.4f, 0.85f, 0.4f);
            case ZombieAI.ZombieType.Listener: return new Vector3(0.45f, 0.95f, 0.45f);
            case ZombieAI.ZombieType.Brute: return new Vector3(0.7f, 1.1f, 0.7f);
            default: return new Vector3(0.5f, 0.9f, 0.5f);
        }
    }

    private Color GetColorForType(ZombieAI.ZombieType type)
    {
        switch (type)
        {
            case ZombieAI.ZombieType.Shambler: return shamblerColor;
            case ZombieAI.ZombieType.Runner: return runnerColor;
            case ZombieAI.ZombieType.Listener: return listenerColor;
            case ZombieAI.ZombieType.Brute: return bruteColor;
            default: return shamblerColor;
        }
    }
}
