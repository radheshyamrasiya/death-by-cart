using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Physics-based shopping cart controller with tank-style controls.
/// Reads CartInventory fullness to scale speed, wobble, mass, and handling.
/// WASD to move, Shift to sprint/ram, Ctrl to sneak.
/// </summary>
public class CartController : MonoBehaviour
{
    [Header("Movement — Base (Empty Cart)")]
    [SerializeField] private float moveForce = 80f;
    [SerializeField] private float turnTorque = 40f;
    [SerializeField] private float sprintMultiplier = 3f;
    [SerializeField] private float maxSpeed = 20f;

    [Header("Movement — Full Cart Penalties")]
    [Tooltip("Speed multiplier when cart is 100% full (0.4 = 40% of base speed)")]
    [SerializeField] private float fullSpeedMultiplier = 0.4f;

    [Tooltip("Turn multiplier when cart is 100% full (0.3 = sluggish turns)")]
    [SerializeField] private float fullTurnMultiplier = 0.3f;

    [Tooltip("Extra Rigidbody mass added at 100% full")]
    [SerializeField] private float fullExtraMass = 15f;

    [Header("Sneak Mode (Ctrl)")]
    [Tooltip("Speed multiplier when sneaking")]
    [SerializeField] private float sneakSpeedMultiplier = 0.3f;

    [Header("Drift / Friction")]
    [Tooltip("Sideways friction (0 = full drift, 1 = no drift)")]
    [SerializeField] private float sidewaysFriction = 0.9f;

    [Tooltip("Drift gets worse when cart is full (lower = more drift)")]
    [SerializeField] private float fullDriftMultiplier = 0.5f;

    [Header("Cart Wobble")]
    [SerializeField] private float wobbleIntensity = 1.5f;
    [SerializeField] private float wobbleInterval = 0.3f;

    [Tooltip("Wobble multiplier at 100% full")]
    [SerializeField] private float fullWobbleMultiplier = 4f;

    // --- Runtime state ---
    private Rigidbody rb;
    private CartInventory inventory;
    private float baseMass;
    private float moveInput;
    private float turnInput;
    private bool isSprinting;
    private bool isSneaking;
    private float debugLogTimer;
    private CartInventory.FullnessTier lastLoggedTier;
    private float wobbleTimer;
    private float currentWobbleDir;
    private bool inputActive = false; // Starts disabled — enabled when player grabs cart

    // --- Public getters for debug HUD ---
    public float MoveInput => moveInput;
    public float TurnInput => turnInput;
    public bool IsSprinting => isSprinting;
    public bool IsSneaking => isSneaking;
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float EffectiveMaxSpeed => GetEffectiveMaxSpeed();

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inventory = GetComponent<CartInventory>();

        if (rb == null)
        {
            Debug.LogError("[CART] ❌ No Rigidbody found! Cart won't move.");
            enabled = false;
            return;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[CART] ⚠️ No CartInventory found — fullness scaling disabled.");
        }

        baseMass = rb.mass;
        rb.angularDamping = 3f;
        rb.linearDamping = 0.3f;

        Debug.Log($"[CART] ✅ CartController ready! Mass={baseMass}, MoveForce={moveForce}, MaxSpeed={maxSpeed}");
    }

    private void Start()
    {
        Debug.Log("[CART] ✅ Script is alive! Controls: WASD=Move, Shift=Sprint, Ctrl=Sneak, U=AddItem, I=RemoveItem, C=Camera");
    }

    private void Update()
    {
        if (!inputActive)
        {
            moveInput = 0f;
            turnInput = 0f;
            isSprinting = false;
            isSneaking = false;
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Movement input
        moveInput = 0f;
        if (keyboard.wKey.isPressed) moveInput += 1f;
        if (keyboard.sKey.isPressed) moveInput -= 1f;

        turnInput = 0f;
        if (keyboard.dKey.isPressed) turnInput += 1f;
        if (keyboard.aKey.isPressed) turnInput -= 1f;

        isSprinting = keyboard.leftShiftKey.isPressed;
        isSneaking = keyboard.leftCtrlKey.isPressed;

        // Can't sprint and sneak at the same time
        if (isSneaking) isSprinting = false;

        // --- Log tier transitions ---
        if (inventory != null && inventory.Tier != lastLoggedTier)
        {
            lastLoggedTier = inventory.Tier;
            string tierMsg = inventory.Tier switch
            {
                CartInventory.FullnessTier.Empty => "🥷 STEALTH MODE — Cart is silent",
                CartInventory.FullnessTier.Light => "👟 LIGHT LOAD — Slight squeak",
                CartInventory.FullnessTier.Half  => "⚠️ HALF FULL — Zombies can hear you!",
                CartInventory.FullnessTier.Full  => "🔔 DINNER BELL — RUN FOR YOUR LIFE!",
                _ => "???"
            };
            Debug.Log($"[CART] TIER CHANGED → {tierMsg} (Fullness: {inventory.Fullness * 100:F0}%)");
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float fullness = inventory != null ? inventory.Fullness : 0f;

        // --- Periodic debug log (every 2 seconds while moving) ---
        debugLogTimer -= Time.fixedDeltaTime;
        if (debugLogTimer <= 0f && rb.linearVelocity.magnitude > 0.5f)
        {
            Debug.Log($"[CART] Speed={rb.linearVelocity.magnitude:F1}/{GetEffectiveMaxSpeed():F1} | Mass={rb.mass:F1} | Fullness={fullness * 100:F0}% | {(isSneaking ? "SNEAKING" : isSprinting ? "SPRINTING" : "Normal")}");
            debugLogTimer = 2f;
        }

        // --- Dynamic mass (heavier when full) ---
        rb.mass = baseMass + (fullExtraMass * fullness);

        // --- Speed scaling based on fullness ---
        float speedScale = Mathf.Lerp(1f, fullSpeedMultiplier, fullness);
        float turnScale = Mathf.Lerp(1f, fullTurnMultiplier, fullness);

        // --- Sneak / Sprint modifiers ---
        float finalMoveForce = moveForce * speedScale;
        float effectiveMaxSpeed = maxSpeed * speedScale;

        if (isSneaking)
        {
            finalMoveForce *= sneakSpeedMultiplier;
            effectiveMaxSpeed *= sneakSpeedMultiplier;
        }
        else if (isSprinting)
        {
            finalMoveForce *= sprintMultiplier;
            effectiveMaxSpeed *= sprintMultiplier;
        }

        // --- Forward / Reverse thrust ---
        float thrust = finalMoveForce * moveInput;
        rb.AddForce(transform.forward * thrust, ForceMode.Force);

        // --- Steering ---
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float effectiveTorque = turnTorque * turnScale;
            rb.AddTorque(Vector3.up * effectiveTorque * turnInput, ForceMode.Force);
        }

        // --- Sideways friction (drift gets worse when full) ---
        float effectiveFriction = Mathf.Lerp(sidewaysFriction, sidewaysFriction * fullDriftMultiplier, fullness);
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= (1f - effectiveFriction);
        rb.linearVelocity = transform.TransformDirection(localVel);

        // --- Speed cap ---
        if (rb.linearVelocity.magnitude > effectiveMaxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * effectiveMaxSpeed;
        }

        // --- Cart wobble (scales with fullness) ---
        float effectiveWobble = Mathf.Lerp(wobbleIntensity, wobbleIntensity * fullWobbleMultiplier, fullness);

        wobbleTimer -= Time.fixedDeltaTime;
        if (wobbleTimer <= 0f)
        {
            currentWobbleDir = Random.Range(-1f, 1f);
            wobbleTimer = Mathf.Lerp(wobbleInterval, wobbleInterval * 0.5f, fullness); // wobbles faster when full
        }

        if (rb.linearVelocity.magnitude > 1f)
        {
            float wobbleScale = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            rb.AddTorque(Vector3.up * currentWobbleDir * effectiveWobble * wobbleScale, ForceMode.Force);
        }
    }

    private float GetEffectiveMaxSpeed()
    {
        float fullness = inventory != null ? inventory.Fullness : 0f;
        float speedScale = Mathf.Lerp(1f, fullSpeedMultiplier, fullness);
        float ems = maxSpeed * speedScale;
        if (isSneaking) ems *= sneakSpeedMultiplier;
        else if (isSprinting) ems *= sprintMultiplier;
        return ems;
    }

    // === Called by CartInteraction ===

    /// <summary>
    /// Enable or disable cart input. Called when player grabs/releases the cart.
    /// </summary>
    public void SetInputActive(bool active)
    {
        inputActive = active;
        if (!active)
        {
            moveInput = 0f;
            turnInput = 0f;
            isSprinting = false;
            isSneaking = false;
        }
        Debug.Log($"[CART] Input {(active ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Returns the world position behind the cart where the player should stand when pushing.
    /// </summary>
    public Vector3 GetPushPosition(float offsetBehind = 1.5f, float offsetUp = 0f)
    {
        return transform.position 
             - transform.forward * offsetBehind 
             + Vector3.up * offsetUp;
    }
}
