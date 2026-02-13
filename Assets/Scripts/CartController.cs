using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Physics-based shopping cart controller with strafe controls.
/// Reads GridInventory weight to scale speed, wobble, mass, and handling.
/// WASD to move, Shift to sprint, Ctrl to sneak.
/// </summary>
public class CartController : MonoBehaviour
{
    [Header("Movement — Base (Empty Cart)")]
    [SerializeField] private float moveForce = 80f;
    [SerializeField] private float turnTorque = 40f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float maxSpeed = 10f;

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

    [Header("Stamina Drain")]
    [Tooltip("Extra stamina drain multiplier when cart is 100% full")]
    [SerializeField] private float fullStaminaDrainMultiplier = 2.5f;

    // --- Runtime state ---
    private Rigidbody rb;
    private GridInventory gridInventory;      // NEW: uses GridInventory
    private CartInventory oldInventory;       // LEGACY: fallback
    private StaminaSystem stamina;
    private float baseMass;
    private float moveInput;
    private float turnInput;
    private bool isSprinting;
    private bool isSneaking;
    private float debugLogTimer;
    private float wobbleTimer;
    private float currentWobbleDir;
    private bool inputActive = false;

    // --- Public getters ---
    public float MoveInput => moveInput;
    public float TurnInput => turnInput;
    public bool IsSprinting => isSprinting;
    public bool IsSneaking => isSneaking;
    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude : 0f;
    public float EffectiveMaxSpeed => GetEffectiveMaxSpeed();

    /// <summary>Get cart fullness 0-1 from GridInventory (weight-based).</summary>
    public float CartFullness
    {
        get
        {
            if (gridInventory != null) return gridInventory.Fullness;
            if (oldInventory != null) return oldInventory.Fullness;
            return 0f;
        }
    }

    /// <summary>Get current cart weight in kg.</summary>
    public float CartWeight
    {
        get
        {
            if (gridInventory != null) return gridInventory.CurrentWeight;
            return 0f;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gridInventory = GetComponent<GridInventory>();
        oldInventory = GetComponent<CartInventory>();

        if (rb == null)
        {
            Debug.LogError("[CART] ❌ No Rigidbody found! Cart won't move.");
            enabled = false;
            return;
        }

        if (gridInventory != null)
            Debug.Log("[CART] ✅ Using GridInventory for weight scaling.");
        else if (oldInventory != null)
            Debug.Log("[CART] ⚠️ Using legacy CartInventory (GridInventory not found).");
        else
            Debug.LogWarning("[CART] ⚠️ No inventory found — weight scaling disabled.");

        baseMass = rb.mass;
        rb.angularDamping = 3f;
        rb.linearDamping = 0.3f;

        Debug.Log($"[CART] ✅ CartController ready! Mass={baseMass}, MoveForce={moveForce}, MaxSpeed={maxSpeed}");
    }

    private void Start()
    {
        stamina = FindFirstObjectByType<StaminaSystem>();
        if (stamina == null)
            Debug.LogWarning("[CART] ⚠️ No StaminaSystem found — sprint won't drain stamina.");

        Debug.Log("[CART] ✅ Controls: WASD=Move, Shift=Sprint, Ctrl=Sneak");
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

        // --- Keyboard input ---
        var keyboard = Keyboard.current;
        moveInput = 0f;
        turnInput = 0f;
        isSprinting = false;
        isSneaking = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) moveInput += 1f;
            if (keyboard.sKey.isPressed) moveInput -= 1f;
            if (keyboard.dKey.isPressed) turnInput += 1f;
            if (keyboard.aKey.isPressed) turnInput -= 1f;
            isSprinting = keyboard.leftShiftKey.isPressed;
            isSneaking = keyboard.leftCtrlKey.isPressed;
        }

        // --- Gamepad input ---
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 leftStick = gamepad.leftStick.ReadValue();
            if (Mathf.Abs(leftStick.y) > Mathf.Abs(moveInput))
                moveInput = leftStick.y;
            if (Mathf.Abs(leftStick.x) > Mathf.Abs(turnInput))
                turnInput = leftStick.x;

            if (gamepad.rightTrigger.isPressed) isSprinting = true;
            if (gamepad.leftTrigger.isPressed) isSneaking = true;
        }

        if (isSneaking) isSprinting = false;

        // Gate sprint on stamina
        if (isSprinting && stamina != null && !stamina.CanSprint)
            isSprinting = false;

        // Tell stamina system — drain harder when cart is heavier
        if (stamina != null)
        {
            bool actualSprinting = isSprinting && Mathf.Abs(moveInput) > 0.01f;
            float drainMult = 1f + (CartFullness * (fullStaminaDrainMultiplier - 1f));
            stamina.SetSprinting(actualSprinting, drainMult);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        float fullness = CartFullness;

        // --- Periodic debug log ---
        debugLogTimer -= Time.fixedDeltaTime;
        if (debugLogTimer <= 0f && rb.linearVelocity.magnitude > 0.5f)
        {
            Debug.Log($"[CART] Speed={rb.linearVelocity.magnitude:F1}/{GetEffectiveMaxSpeed():F1} | Mass={rb.mass:F1} | Weight={CartWeight:F1}kg ({fullness * 100:F0}%) | {(isSneaking ? "SNEAKING" : isSprinting ? "SPRINTING" : "Normal")}");
            debugLogTimer = 2f;
        }

        // --- Dynamic mass ---
        rb.mass = baseMass + (fullExtraMass * fullness);

        // --- Speed scaling ---
        float speedScale = Mathf.Lerp(1f, fullSpeedMultiplier, fullness);
        float turnScale = Mathf.Lerp(1f, fullTurnMultiplier, fullness);

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

        // --- Forward / Reverse ---
        float thrust = finalMoveForce * moveInput;
        rb.AddForce(transform.forward * thrust, ForceMode.Force);

        // --- Strafe ---
        if (Mathf.Abs(turnInput) > 0.01f)
        {
            float strafeForce = finalMoveForce * turnInput;
            rb.AddForce(transform.right * strafeForce, ForceMode.Force);
        }

        // --- Sideways friction ---
        float savedY = rb.linearVelocity.y;
        float effectiveFriction = Mathf.Lerp(sidewaysFriction, sidewaysFriction * fullDriftMultiplier, fullness);
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= (1f - effectiveFriction);
        rb.linearVelocity = transform.TransformDirection(localVel);

        // --- Speed cap (horizontal only) ---
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        if (horizontalVel.magnitude > effectiveMaxSpeed)
        {
            horizontalVel = horizontalVel.normalized * effectiveMaxSpeed;
        }
        rb.linearVelocity = new Vector3(horizontalVel.x, savedY, horizontalVel.z);

        // --- Cart wobble ---
        float effectiveWobble = Mathf.Lerp(wobbleIntensity, wobbleIntensity * fullWobbleMultiplier, fullness);
        wobbleTimer -= Time.fixedDeltaTime;
        if (wobbleTimer <= 0f)
        {
            currentWobbleDir = Random.Range(-1f, 1f);
            wobbleTimer = Mathf.Lerp(wobbleInterval, wobbleInterval * 0.5f, fullness);
        }

        if (rb.linearVelocity.magnitude > 1f)
        {
            float wobbleScale = Mathf.Clamp01(rb.linearVelocity.magnitude / maxSpeed);
            rb.AddTorque(Vector3.up * currentWobbleDir * effectiveWobble * wobbleScale, ForceMode.Force);
        }
    }

    private float GetEffectiveMaxSpeed()
    {
        float fullness = CartFullness;
        float speedScale = Mathf.Lerp(1f, fullSpeedMultiplier, fullness);
        float ems = maxSpeed * speedScale;
        if (isSneaking) ems *= sneakSpeedMultiplier;
        else if (isSprinting) ems *= sprintMultiplier;
        return ems;
    }

    // === Called by CartInteraction ===

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

    public Vector3 GetPushPosition(float offsetBehind = 1.5f, float offsetUp = 0f)
    {
        return transform.position 
             - transform.forward * offsetBehind 
             + Vector3.up * offsetUp;
    }
}
