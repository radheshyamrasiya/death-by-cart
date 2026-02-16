using UnityEngine;

/// <summary>
/// Calculates and visualizes the noise radius for the cart and player.
/// Cart noise = speed × fullness. Player noise = movement state.
/// Draws ground circles showing noise reach.
/// Attach to the CART GameObject.
/// </summary>
public class NoiseSystem : MonoBehaviour
{
    [Header("Cart Noise")]
    [Tooltip("Base noise multiplier for the cart")]
    [SerializeField] private float cartNoiseMult = 4f;
    [Tooltip("Max cart noise radius")]
    [SerializeField] private float maxCartNoise = 40f;
    [Tooltip("Sneak mode noise multiplier")]
    [SerializeField] private float sneakMult = 0.1f;
    [Tooltip("Sprint noise bonus multiplier")]
    [SerializeField] private float sprintMult = 1.8f;

    [Header("Player Noise")]
    [Tooltip("Noise radius when walking on foot")]
    [SerializeField] private float playerWalkNoise = 3f;
    [Tooltip("Noise radius when sprinting on foot")]
    [SerializeField] private float playerSprintNoise = 8f;
    [Tooltip("Extra noise when carrying a heavy item (added)")]
    [SerializeField] private float carryNoiseBonus = 2f;
    [Tooltip("Noise multiplier when crouching on foot")]
    [SerializeField] private float crouchNoiseMult = 0.15f;

    [Header("Debug Visuals")]
    [SerializeField] private bool showDebugCircles = true;
    [SerializeField] private Color cartNoiseColorQuiet = new Color(0.2f, 0.8f, 0.2f, 0.25f);
    [SerializeField] private Color cartNoiseColorLoud = new Color(1f, 0.1f, 0.1f, 0.35f);
    [SerializeField] private Color playerNoiseColor = new Color(0.3f, 0.6f, 1f, 0.2f);

    // Runtime
    private CartController cart;
    private GridInventory gridInventory;
    private float currentCartNoise;
    private float currentPlayerNoise;
    private float currentRCCarNoise;
    private Vector3 rcCarPosition;
    private float currentFartBombNoise;
    private Vector3 fartBombPosition;

    // Ground circle objects
    private Transform cartCircle;
    private Transform playerCircle;
    private Transform rcCarCircle;
    private Renderer cartCircleRenderer;
    private Renderer playerCircleRenderer;
    private Renderer rcCarCircleRenderer;
    private Material cartCircleMat;
    private Material playerCircleMat;
    private Material rcCarCircleMat;

    // Public API — zombies read these
    public float CartNoiseRadius => currentCartNoise;
    public float PlayerNoiseRadius => currentPlayerNoise;
    public float RCCarNoiseRadius => currentRCCarNoise;
    public float FartBombNoiseRadius => currentFartBombNoise;
    public Vector3 CartPosition => transform.position;
    public Vector3 RCCarPosition => rcCarPosition;
    public Vector3 FartBombPosition => fartBombPosition;
    public bool ShowDebug { get => showDebugCircles; set => showDebugCircles = value; }

    private void Start()
    {
        cart = GetComponent<CartController>();
        gridInventory = GetComponent<GridInventory>();

        if (cart == null)
            Debug.LogWarning("[NOISE] No CartController found!");

        // Create ground circles
        cartCircle = CreateGroundCircle("CartNoiseCircle", transform);
        playerCircle = CreateGroundCircle("PlayerNoiseCircle", null);
        rcCarCircle = CreateGroundCircle("RCCarNoiseCircle", null);

        cartCircleRenderer = cartCircle.GetComponent<Renderer>();
        playerCircleRenderer = playerCircle.GetComponent<Renderer>();
        rcCarCircleRenderer = rcCarCircle.GetComponent<Renderer>();

        cartCircleMat = cartCircleRenderer.material;
        playerCircleMat = playerCircleRenderer.material;
        rcCarCircleMat = rcCarCircleRenderer.material;
    }

    private void Update()
    {
        UpdateCartNoise();
        UpdatePlayerNoise();
        UpdateRCCarNoise();
        UpdateFartBombNoise();
        UpdateVisuals();
    }

    private void UpdateCartNoise()
    {
        if (cart == null) { currentCartNoise = 0f; return; }

        float speed = cart.CurrentSpeed;
        float fullness = cart.CartFullness;

        // Noise = speed × (1 + fullness) × multiplier
        float noise = speed * (1f + fullness * 2f) * cartNoiseMult;

        // Sprint/sneak modifiers
        if (cart.IsSneaking)
            noise *= sneakMult;
        else if (cart.IsSprinting)
            noise *= sprintMult;

        // Stationary = no noise
        if (speed < 0.3f)
            noise = 0f;

        currentCartNoise = Mathf.Min(noise, maxCartNoise);
    }

    private void UpdatePlayerNoise()
    {
        // Find player
        var player = FindFirstObjectByType<CartInteraction>();
        if (player == null) { currentPlayerNoise = 0f; return; }

        // Only generate noise when player is on foot (not attached to cart)
        if (player.IsAttached)
        {
            currentPlayerNoise = 0f;
            return;
        }

        // Check movement
        var tpc = player.GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc == null) { currentPlayerNoise = 0f; return; }

        // Approximate: check if player is moving by looking at velocity
        var rb = player.GetComponent<Rigidbody>();
        var cc = player.GetComponent<CharacterController>();
        float playerSpeed = 0f;

        if (cc != null) playerSpeed = cc.velocity.magnitude;
        else if (rb != null) playerSpeed = rb.linearVelocity.magnitude;

        if (playerSpeed < 0.5f)
        {
            currentPlayerNoise = 0f;
        }
        else
        {
            // Check if sprinting
            bool sprinting = playerSpeed > tpc.MoveSpeed * 1.2f;
            currentPlayerNoise = sprinting ? playerSprintNoise : playerWalkNoise;

            // Carrying heavy item = more noise
            var pickup = player.GetComponent<ItemPickup>();
            if (pickup != null && pickup.IsCarrying && pickup.CarriedItem != null)
            {
                float carryWeight = pickup.CarriedItem.weight;
                currentPlayerNoise += carryNoiseBonus * (carryWeight / 10f);
            }

            // Crouching = drastically reduced noise
            var crouch = player.GetComponent<PlayerCrouch>();
            if (crouch != null && crouch.IsCrouching)
            {
                currentPlayerNoise *= crouch.NoiseMultiplier;
            }
        }

        // Update player circle position
        if (playerCircle != null)
            playerCircle.position = player.transform.position + Vector3.up * 0.05f;
    }

    private void UpdateRCCarNoise()
    {
        // Find active RC car
        var rcCar = FindFirstObjectByType<RCCarController>();
        if (rcCar != null && rcCar.IsActive)
        {
            currentRCCarNoise = rcCar.NoiseRadius;
            rcCarPosition = rcCar.transform.position;
        }
        else
        {
            currentRCCarNoise = 0f;
        }
    }

    private void UpdateFartBombNoise()
    {
        // Find the loudest active fart bomb
        currentFartBombNoise = 0f;
        foreach (var bomb in FartBombController.ActiveBombs)
        {
            if (bomb == null || !bomb.IsDetonated || bomb.IsFinished) continue;
            if (bomb.NoiseRadius > currentFartBombNoise)
            {
                currentFartBombNoise = bomb.NoiseRadius;
                fartBombPosition = bomb.transform.position;
            }
        }
    }

    private void UpdateVisuals()
    {
        // Cart circle
        if (cartCircle != null)
        {
            bool active = showDebugCircles && currentCartNoise > 0.1f;
            cartCircle.gameObject.SetActive(active);

            if (active)
            {
                float diameter = currentCartNoise * 2f;
                cartCircle.localScale = new Vector3(diameter, 1f, diameter);
                cartCircle.position = transform.position + Vector3.up * 0.05f;

                // Color: green (quiet) → red (loud)
                float t = Mathf.Clamp01(currentCartNoise / maxCartNoise);
                Color c = Color.Lerp(cartNoiseColorQuiet, cartNoiseColorLoud, t);
                // Pulse effect
                float pulse = Mathf.Sin(Time.time * 3f * (1f + t * 2f)) * 0.05f;
                c.a = Mathf.Clamp01(c.a + pulse);
                cartCircleMat.color = c;
            }
        }

        // Player circle
        if (playerCircle != null)
        {
            bool active = showDebugCircles && currentPlayerNoise > 0.1f;
            playerCircle.gameObject.SetActive(active);

            if (active)
            {
                float diameter = currentPlayerNoise * 2f;
                playerCircle.localScale = new Vector3(diameter, 1f, diameter);
                playerCircleMat.color = playerNoiseColor;
            }
        }

        // RC Car circle
        if (rcCarCircle != null)
        {
            bool active = showDebugCircles && currentRCCarNoise > 0.1f;
            rcCarCircle.gameObject.SetActive(active);

            if (active)
            {
                float diameter = currentRCCarNoise * 2f;
                rcCarCircle.localScale = new Vector3(diameter, 1f, diameter);
                rcCarCircle.position = rcCarPosition + Vector3.up * 0.05f;

                // Orange pulsing circle
                float pulse = Mathf.Sin(Time.time * 5f) * 0.08f;
                rcCarCircleMat.color = new Color(1f, 0.6f, 0f, 0.2f + pulse);
            }
        }

        // Fart Bomb circles — one per active detonated bomb
        foreach (var bomb in FartBombController.ActiveBombs)
        {
            if (bomb == null || !bomb.IsDetonated || bomb.IsFinished) continue;
            if (bomb.NoiseRadius < 0.1f) continue;

            // Reuse a dynamically created circle or find existing
            Transform circle = bomb.transform.Find("FartNoiseCircle");
            if (circle == null && showDebugCircles)
            {
                circle = CreateGroundCircle("FartNoiseCircle", bomb.transform);
                circle.gameObject.SetActive(true);
            }
            if (circle != null)
            {
                bool show = showDebugCircles && bomb.NoiseRadius > 0.1f;
                circle.gameObject.SetActive(show);
                if (show)
                {
                    float diameter = bomb.NoiseRadius * 2f;
                    circle.localScale = new Vector3(diameter, 0.01f, diameter);
                    circle.position = bomb.transform.position + Vector3.up * 0.05f;

                    // Green pulsing circle
                    Renderer r = circle.GetComponent<Renderer>();
                    if (r != null)
                    {
                        float pulse = Mathf.Sin(Time.time * 6f) * 0.1f;
                        r.material.color = new Color(0.2f, 0.9f, 0.1f, 0.2f + pulse);
                    }
                }
            }
        }
    }

    // ======================== HELPERS ========================

    /// <summary>Create a flat disc on the ground to show radius.</summary>
    private Transform CreateGroundCircle(string name, Transform parent)
    {
        // Use a flattened cylinder as a circle
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.localScale = new Vector3(1f, 0.01f, 1f);
        obj.transform.position = Vector3.up * 0.05f;

        // Remove collider
        var col = obj.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        // Transparent material
        Renderer rend = obj.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1, 1, 1, 0.2f);
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        if (parent != null)
            obj.transform.SetParent(parent, false);

        obj.SetActive(false);
        return obj.transform;
    }
}
