using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Zombie AI with 4 states: Idle, Patrol, Chase, Attack.
/// Each zombie has individual vision cone and hearing radius.
/// Draws debug visuals: vision cone on ground, hearing circle, state label.
/// Requires NavMeshAgent.
/// </summary>
public class ZombieAI : MonoBehaviour
{
    public enum ZombieState { Idle, Patrol, Chase, Attack }
    public enum ZombieType { Shambler, Runner, Listener, Brute }

    [Header("Zombie Type")]
    [SerializeField] private ZombieType zombieType = ZombieType.Shambler;

    [Header("Vision")]
    [SerializeField] private float visionRange = 12f;
    [SerializeField] private float visionAngle = 60f;
    [SerializeField] private float visionCheckInterval = 0.3f;

    [Header("Hearing")]
    [SerializeField] private float hearingRange = 20f;
    [SerializeField] private float hearingCheckInterval = 0.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float patrolWaitTime = 3f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDamage = 20f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackKnockback = 3f;

    [Header("Memory")]
    [SerializeField] private float chaseMemory = 5f;

    [Header("Hunting Mode (Chase/Attack)")]
    [Tooltip("Multiplier for vision & hearing when in Chase or Attack")]
    [SerializeField] private float huntingMultiplier = 2f;

    [Header("Debug Visuals")]
    [SerializeField] private bool showDebugVisuals = true;

    // Effective ranges — 2x when hunting
    private bool IsHunting => currentState == ZombieState.Chase || currentState == ZombieState.Attack;
    private float EffectiveVisionRange => IsHunting ? visionRange * huntingMultiplier : visionRange;
    private float EffectiveVisionAngle => IsHunting ? Mathf.Min(visionAngle * huntingMultiplier, 180f) : visionAngle;
    private float EffectiveHearingRange => IsHunting ? hearingRange * huntingMultiplier : hearingRange;

    // Runtime
    private ZombieState currentState = ZombieState.Idle;
    private NavMeshAgent agent;
    private Transform target;           // Player
    private HealthSystem targetHealth;
    private Vector3 lastKnownPosition;
    private Vector3 homePosition;
    private Vector3 patrolTarget;

    private float visionTimer;
    private float hearingTimer;
    private float attackTimer;
    private float stateTimer;
    private float memoryTimer;
    private bool canSeeTarget;
    private bool canHearTarget;
    private float alertSnapTimer; // Instant face-target on alert

    // Stun
    private bool isStunned;
    private float stunTimer;
    private float originalMoveSpeed;
    private float originalChaseSpeed;
    private Renderer zombieRenderer;
    private Color originalColor;

    // Debug visuals
    private Transform visionCone;
    private Transform hearingCircle;
    private Renderer visionRenderer;
    private Renderer hearingRenderer;
    private Material visionMat;
    private Material hearingMat;
    private float lastBuiltConeAngle; // Track to rebuild when angle changes

    // Public
    public ZombieState State => currentState;
    public ZombieType Type => zombieType;
    public bool ShowDebug { get => showDebugVisuals; set => showDebugVisuals = value; }
    public bool IsStunned => isStunned;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
        agent.angularSpeed = 200f;
        agent.radius = 0.4f;
        agent.height = 1.8f;

        homePosition = transform.position;

        // Find player
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (cartInteraction != null)
        {
            target = cartInteraction.transform;
            targetHealth = target.GetComponent<HealthSystem>();
        }

        // Apply type preset
        ApplyTypePreset();

        // Create debug visuals
        CreateDebugVisuals();

        // Start idle
        TransitionTo(ZombieState.Idle);
    }

    private void ApplyTypePreset()
    {
        switch (zombieType)
        {
            case ZombieType.Shambler:
                visionRange = 8f; visionAngle = 45f;
                hearingRange = 15f;
                moveSpeed = 1.5f; chaseSpeed = 3f;
                attackDamage = 15f;
                break;
            case ZombieType.Runner:
                visionRange = 12f; visionAngle = 60f;
                hearingRange = 10f;
                moveSpeed = 3f; chaseSpeed = 6f;
                attackDamage = 10f;
                break;
            case ZombieType.Listener:
                visionRange = 5f; visionAngle = 30f;
                hearingRange = 35f;
                moveSpeed = 2f; chaseSpeed = 4f;
                attackDamage = 15f;
                break;
            case ZombieType.Brute:
                visionRange = 15f; visionAngle = 90f;
                hearingRange = 20f;
                moveSpeed = 1.2f; chaseSpeed = 2.5f;
                attackDamage = 35f;
                attackKnockback = 6f;
                break;
        }
    }

    private void Update()
    {
        if (target == null) return;

        // Handle stun
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                EndStun();
            }
            else
            {
                // Stunned — don't process state machine, just stand there slowly
                agent.speed = originalMoveSpeed * 0.05f;
                return;
            }
        }

        // Periodic detection checks
        visionTimer -= Time.deltaTime;
        hearingTimer -= Time.deltaTime;

        if (visionTimer <= 0f)
        {
            canSeeTarget = CheckVision();
            visionTimer = visionCheckInterval;
        }

        if (hearingTimer <= 0f)
        {
            canHearTarget = CheckHearing();
            hearingTimer = hearingCheckInterval;
        }

        // State machine
        switch (currentState)
        {
            case ZombieState.Idle:
                UpdateIdle();
                break;
            case ZombieState.Patrol:
                UpdatePatrol();
                break;
            case ZombieState.Chase:
                UpdateChase();
                break;
            case ZombieState.Attack:
                UpdateAttack();
                break;
        }

        // Update debug visuals
        UpdateDebugVisuals();
    }

    // ======================== DETECTION ========================

    private bool CheckVision()
    {
        if (target == null) return false;

        float effRange = EffectiveVisionRange;
        float effAngle = EffectiveVisionAngle;

        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > effRange) return false;

        // Angle check
        Vector3 dirToTarget = (target.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, dirToTarget);
        if (angle > effAngle * 0.5f) return false;

        // Line of sight (raycast)
        Vector3 eyePos = transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = target.position + Vector3.up * 1f;
        if (Physics.Raycast(eyePos, (targetPos - eyePos).normalized, out RaycastHit hit, effRange))
        {
            if (hit.transform == target || hit.transform.IsChildOf(target))
                return true;
        }

        return false;
    }

    private bool CheckHearing()
    {
        if (target == null) return false;

        float effHearing = EffectiveHearingRange;

        // Check cart noise
        NoiseSystem noise = FindFirstObjectByType<NoiseSystem>();
        if (noise != null)
        {
            float distToCart = Vector3.Distance(transform.position, noise.CartPosition);
            if (distToCart < noise.CartNoiseRadius && distToCart < effHearing)
            {
                lastKnownPosition = noise.CartPosition;
                return true;
            }
        }

        // Check player noise
        if (noise != null && noise.PlayerNoiseRadius > 0f)
        {
            float distToPlayer = Vector3.Distance(transform.position, target.position);
            if (distToPlayer < noise.PlayerNoiseRadius && distToPlayer < effHearing)
            {
                lastKnownPosition = target.position;
                return true;
            }
        }

        // Check RC car noise
        if (noise != null && noise.RCCarNoiseRadius > 0f)
        {
            float distToRC = Vector3.Distance(transform.position, noise.RCCarPosition);
            if (distToRC < noise.RCCarNoiseRadius && distToRC < effHearing)
            {
                lastKnownPosition = noise.RCCarPosition;
                return true;
            }
        }
        // Check fart bomb noise
        if (noise != null && noise.FartBombNoiseRadius > 0f)
        {
            float distToFart = Vector3.Distance(transform.position, noise.FartBombPosition);
            if (distToFart < noise.FartBombNoiseRadius && distToFart < effHearing)
            {
                lastKnownPosition = noise.FartBombPosition;
                return true;
            }
        }

        return false;
    }

    // ======================== STATES ========================

    private void TransitionTo(ZombieState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        switch (newState)
        {
            case ZombieState.Idle:
                agent.speed = 0f;
                agent.angularSpeed = 200f;
                agent.ResetPath();
                stateTimer = Random.Range(2f, 5f); // Wait before patrol
                break;

            case ZombieState.Patrol:
                agent.speed = moveSpeed;
                agent.angularSpeed = 200f;
                PickPatrolPoint();
                break;

            case ZombieState.Chase:
                agent.speed = chaseSpeed;
                agent.angularSpeed = 400f; // 2x turn speed when hunting
                memoryTimer = chaseMemory;
                alertSnapTimer = 0.2f; // Instant snap-face for 0.2s
                // Instantly face the target on alert
                if (target != null)
                {
                    Vector3 dir = (lastKnownPosition - transform.position).normalized;
                    dir.y = 0;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(dir);
                }
                break;

            case ZombieState.Attack:
                agent.speed = 0f;
                agent.angularSpeed = 400f; // 2x turn speed when hunting
                agent.ResetPath();
                attackTimer = 0f; // Attack immediately
                break;
        }
    }

    private void UpdateIdle()
    {
        // Detected something? → Chase
        if (canSeeTarget || canHearTarget)
        {
            lastKnownPosition = target.position;
            TransitionTo(ZombieState.Chase);
            return;
        }

        // Wait, then patrol
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0f)
        {
            TransitionTo(ZombieState.Patrol);
        }
    }

    private void UpdatePatrol()
    {
        // Detected something? → Chase
        if (canSeeTarget || canHearTarget)
        {
            lastKnownPosition = target.position;
            TransitionTo(ZombieState.Chase);
            return;
        }

        // Reached patrol point?
        if (!agent.pathPending && agent.remainingDistance < 1f)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                // Pick new point or go idle
                if (Random.value > 0.3f)
                    PickPatrolPoint();
                else
                    TransitionTo(ZombieState.Idle);
            }
        }
    }

    private void UpdateChase()
    {
        // Check if RC car is active and closer than the player
        var rcCar = FindFirstObjectByType<RCCarController>();
        bool rcCarActive = rcCar != null && rcCar.IsActive;

        float distToTarget = Vector3.Distance(transform.position, target.position);

        // If RC car is nearby, check if we should attack it
        if (rcCarActive)
        {
            float distToRC = Vector3.Distance(transform.position, rcCar.transform.position);
            if (distToRC < attackRange)
            {
                // Attack the RC car
                TransitionTo(ZombieState.Attack);
                return;
            }
        }

        // Close enough to attack player?
        if (distToTarget < attackRange && canSeeTarget)
        {
            TransitionTo(ZombieState.Attack);
            return;
        }

        // Can still see/hear target? Update position
        if (canSeeTarget)
        {
            lastKnownPosition = target.position;
            memoryTimer = chaseMemory;
        }
        else if (canHearTarget)
        {
            // lastKnownPosition was already set by CheckHearing
            // (could be RC car or player position)
            memoryTimer = chaseMemory;
        }
        else
        {
            // Lost target — use memory
            memoryTimer -= Time.deltaTime;
            if (memoryTimer <= 0f)
            {
                // Give up, go back to patrol
                TransitionTo(ZombieState.Patrol);
                return;
            }
        }

        // Move toward last known position
        agent.SetDestination(lastKnownPosition);
    }

    private void UpdateAttack()
    {
        if (target == null)
        {
            TransitionTo(ZombieState.Idle);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        // Out of attack range? Back to chase
        if (dist > attackRange * 1.5f)
        {
            TransitionTo(ZombieState.Chase);
            return;
        }

        // Face the target
        Vector3 lookDir = (target.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir.magnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(lookDir), Time.deltaTime * 8f);

        // Attack on cooldown
        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            PerformAttack();
            attackTimer = attackCooldown;
        }
    }

    private void PerformAttack()
    {
        // Check if we can hit the RC car instead
        var rcCar = FindFirstObjectByType<RCCarController>();
        if (rcCar != null && rcCar.IsActive)
        {
            float distToRC = Vector3.Distance(transform.position, rcCar.transform.position);
            if (distToRC < attackRange * 1.5f)
            {
                rcCar.TakeDamage(attackDamage);
                Debug.Log($"[ZOMBIE] {zombieType} attacks RC Car for {attackDamage} damage!");
                return;
            }
        }

        if (targetHealth == null) return;
        if (targetHealth.IsDead) { TransitionTo(ZombieState.Idle); return; }

        // Deal damage
        targetHealth.TakeDamage(attackDamage, transform.position);

        // Knockback
        if (target != null)
        {
            Vector3 knockDir = (target.position - transform.position).normalized;
            var cc = target.GetComponent<CharacterController>();
            if (cc != null)
            {
                // CharacterController doesn't use physics, so we use Move
                cc.Move(knockDir * attackKnockback * 0.1f);
            }
            var rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(knockDir * attackKnockback, ForceMode.Impulse);
            }
        }

        Debug.Log($"[ZOMBIE] {zombieType} attacks for {attackDamage} damage!");
    }

    private void PickPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir.y = 0;
        Vector3 target = homePosition + randomDir;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }

        stateTimer = patrolWaitTime;
    }

    // ======================== DEBUG VISUALS ========================

    private void CreateDebugVisuals()
    {
        // Hearing circle (blue ring on ground)
        hearingCircle = CreateGroundCircle("HearingCircle");
        hearingRenderer = hearingCircle.GetComponent<Renderer>();
        hearingMat = hearingRenderer.material;

        // Vision cone (green/red triangle on ground)
        visionCone = CreateVisionConeMesh();
        visionRenderer = visionCone.GetComponent<Renderer>();
        visionMat = visionRenderer.material;
        lastBuiltConeAngle = visionAngle; // Track initial angle
    }

    private void UpdateDebugVisuals()
    {
        // Hearing circle
        if (hearingCircle != null)
        {
            hearingCircle.gameObject.SetActive(showDebugVisuals);
            if (showDebugVisuals)
            {
                float effHearing = EffectiveHearingRange;
                float diameter = effHearing * 2f;
                hearingCircle.localScale = new Vector3(diameter, 0.01f, diameter);
                hearingCircle.position = transform.position + Vector3.up * 0.03f;

                Color c = canHearTarget
                    ? new Color(0.3f, 0.5f, 1f, 0.3f)   // Bright blue — heard something
                    : IsHunting
                        ? new Color(0.4f, 0.3f, 0.6f, 0.12f) // Purple tint — hunting mode
                        : new Color(0.2f, 0.3f, 0.6f, 0.08f); // Faint blue — idle
                hearingMat.color = c;
            }
        }

        // Vision cone
        if (visionCone != null)
        {
            visionCone.gameObject.SetActive(showDebugVisuals);
            if (showDebugVisuals)
            {
                // Position and rotate with zombie
                visionCone.position = transform.position + Vector3.up * 0.06f;
                visionCone.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

                // Rebuild cone mesh if angle changed (hunting mode toggle)
                float currentAngle = EffectiveVisionAngle;
                if (Mathf.Abs(currentAngle - lastBuiltConeAngle) > 0.5f)
                {
                    RebuildVisionConeMesh(currentAngle);
                    lastBuiltConeAngle = currentAngle;
                }

                // Scale by effective vision range (2x when hunting)
                visionCone.localScale = new Vector3(EffectiveVisionRange, 1f, EffectiveVisionRange);

                Color c;
                switch (currentState)
                {
                    case ZombieState.Chase:
                    case ZombieState.Attack:
                        c = new Color(1f, 0.1f, 0.1f, 0.3f); // Red
                        break;
                    default:
                        c = canSeeTarget
                            ? new Color(1f, 0.8f, 0f, 0.25f) // Yellow — spotted
                            : new Color(0.2f, 0.8f, 0.2f, 0.12f); // Green — idle
                        break;
                }
                visionMat.color = c;
            }
        }
    }

    // Draw state label above zombie
    private void OnGUI()
    {
        if (!showDebugVisuals) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        if (screenPos.z < 0) return; // Behind camera

        string stateText;
        Color stateColor;

        switch (currentState)
        {
            case ZombieState.Idle:
                stateText = "💤 IDLE"; stateColor = Color.gray; break;
            case ZombieState.Patrol:
                stateText = "🚶 PATROL"; stateColor = Color.yellow; break;
            case ZombieState.Chase:
                stateText = "🔴 CHASE!"; stateColor = Color.red; break;
            case ZombieState.Attack:
                stateText = "💀 ATTACK!"; stateColor = new Color(1f, 0.5f, 0f); break;
            default:
                stateText = "???"; stateColor = Color.white; break;
        }

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = stateColor;

        float labelW = 120;
        float labelH = 25;
        Rect rect = new Rect(screenPos.x - labelW / 2, Screen.height - screenPos.y - labelH / 2, labelW, labelH);
        GUI.Label(rect, $"{stateText} [{zombieType}]", style);
    }

    // ======================== MESH CREATION ========================

    private Transform CreateGroundCircle(string name)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(null);
        obj.transform.localScale = new Vector3(1f, 0.01f, 1f);

        var col = obj.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        Renderer rend = obj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.3f, 0.5f, 1f, 0.1f);
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        return obj.transform;
    }

    /// <summary>Create a cone-shaped mesh for vision visualization.</summary>
    private Transform CreateVisionConeMesh()
    {
        GameObject obj = new GameObject("VisionCone");
        obj.transform.SetParent(null);

        MeshFilter mf = obj.AddComponent<MeshFilter>();
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();

        // Build a flat cone mesh
        int segments = 20;
        float halfAngle = visionAngle * 0.5f * Mathf.Deg2Rad;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero; // Apex at zombie position

        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
            // Cone in XZ plane, pointing forward (Z+)
            vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        }

        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mf.mesh = mesh;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
        mr.material = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return obj.transform;
    }

    /// <summary>Rebuild just the mesh data on the existing vision cone with a new angle.</summary>
    private void RebuildVisionConeMesh(float newAngle)
    {
        if (visionCone == null) return;
        MeshFilter mf = visionCone.GetComponent<MeshFilter>();
        if (mf == null) return;

        int segments = 20;
        float halfAngle = newAngle * 0.5f * Mathf.Deg2Rad;

        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, (float)i / segments);
            vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = mf.mesh;
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
    // ======================== STUN ========================

    /// <summary>Apply stun: slow the zombie by (1 - slowMult) for duration seconds.</summary>
    public void ApplyStun(float duration, float slowMult)
    {
        if (isStunned) return; // Already stunned

        isStunned = true;
        stunTimer = duration;

        // Save original speeds
        originalMoveSpeed = moveSpeed;
        originalChaseSpeed = chaseSpeed;

        // Apply slow
        agent.speed *= slowMult;

        // Stop current path
        agent.ResetPath();

        // Visual: tint blue
        if (zombieRenderer == null)
            zombieRenderer = GetComponentInChildren<Renderer>();
        if (zombieRenderer != null)
        {
            originalColor = zombieRenderer.material.color;
            zombieRenderer.material.color = new Color(0.3f, 0.5f, 1f, 1f); // Blue tint
        }

        // Random disorienting spin (90-180 degrees)
        float randomAngle = Random.Range(90f, 180f) * (Random.value > 0.5f ? 1f : -1f);
        transform.Rotate(0, randomAngle, 0);

        Debug.Log($"[ZOMBIE] {name} STUNNED for {duration}s!");
    }

    private void EndStun()
    {
        isStunned = false;

        // Restore speeds
        agent.speed = (currentState == ZombieState.Chase || currentState == ZombieState.Attack)
            ? chaseSpeed : moveSpeed;

        // Restore color
        if (zombieRenderer != null)
            zombieRenderer.material.color = originalColor;

        Debug.Log($"[ZOMBIE] {name} stun wore off.");
    }

    private void OnDestroy()
    {
        if (hearingCircle != null) Destroy(hearingCircle.gameObject);
        if (visionCone != null) Destroy(visionCone.gameObject);
    }
}
