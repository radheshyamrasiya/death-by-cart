using UnityEngine;

/// <summary>
/// Shared stamina/sprint meter. Works both on foot and when pushing the cart.
/// Drains while sprinting, regens after a delay when not sprinting.
/// Attach to the Player GameObject.
/// </summary>
public class StaminaSystem : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainRate = 25f;    // per second while sprinting
    [SerializeField] private float regenRate = 15f;    // per second while resting
    [SerializeField] private float regenDelay = 1.5f;  // seconds after stopping sprint before regen

    [Header("Exhaustion")]
    [Tooltip("Below this %, sprint is locked until regen reaches minSprintThreshold")]
    [SerializeField] private float exhaustionThreshold = 0f;
    [Tooltip("Must regen to this % before sprinting again after exhaustion")]
    [SerializeField] private float minSprintThreshold = 20f;

    // Runtime
    private float currentStamina;
    private float regenTimer;
    private bool isSprinting;
    private bool isExhausted;
    private float currentDrainMultiplier = 1f;

    // Public API
    public float StaminaPercent => currentStamina / maxStamina;
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public bool CanSprint => !isExhausted && currentStamina > 0f;
    public bool IsSprinting => isSprinting;
    public bool IsExhausted => isExhausted;

    // Events
    public System.Action OnExhausted;
    public System.Action OnRecovered;

    private void Awake()
    {
        currentStamina = maxStamina;
    }

    private void Update()
    {
        if (isSprinting && currentStamina > 0f)
        {
            // Drain — multiplied by weight factor
            currentStamina -= drainRate * currentDrainMultiplier * Time.deltaTime;
            regenTimer = regenDelay;

            if (currentStamina <= exhaustionThreshold)
            {
                currentStamina = 0f;
                isExhausted = true;
                isSprinting = false;
                OnExhausted?.Invoke();
                Debug.Log("[STAMINA] 💀 Exhausted!");
            }
        }
        else
        {
            // Regen after delay
            regenTimer -= Time.deltaTime;
            if (regenTimer <= 0f && currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);

                // Recover from exhaustion
                if (isExhausted && currentStamina >= minSprintThreshold)
                {
                    isExhausted = false;
                    OnRecovered?.Invoke();
                    Debug.Log("[STAMINA] ✅ Recovered!");
                }
            }
        }
    }

    /// <summary>
    /// Call this every frame from CartController or player movement.
    /// Pass true when the sprint button is held AND speed > 0.
    /// drainMult: weight-based multiplier (1.0 = normal, 2.5 = heavy cart)
    /// </summary>
    public void SetSprinting(bool sprinting, float drainMult = 1f)
    {
        currentDrainMultiplier = drainMult;
        if (sprinting && !CanSprint)
        {
            isSprinting = false;
            return;
        }
        isSprinting = sprinting;
    }
}
