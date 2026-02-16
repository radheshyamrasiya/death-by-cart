using UnityEngine;

/// <summary>
/// Player health system. Tracks HP, handles damage with invincibility frames,
/// and triggers death when HP reaches 0.
/// Attach to the Player GameObject.
/// </summary>
public class HealthSystem : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float invincibilityDuration = 0.5f;

    [Header("Damage Flash")]
    [SerializeField] private float flashDuration = 0.3f;

    // Runtime
    private float currentHealth;
    private float invincibilityTimer;
    private float flashTimer;
    private bool isDead;

    // Public API
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => currentHealth / maxHealth;
    public bool IsDead => isDead;
    public bool IsInvincible => invincibilityTimer > 0f;
    public float FlashAlpha => Mathf.Clamp01(flashTimer / flashDuration);

    // Events
    public System.Action<float, float> OnDamaged;      // (damage, remainingHP)
    public System.Action OnDeath;
    public System.Action<float> OnHealed;               // (healAmount)

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (invincibilityTimer > 0f)
            invincibilityTimer -= Time.deltaTime;

        if (flashTimer > 0f)
            flashTimer -= Time.deltaTime;
    }

    /// <summary>Deal damage to the player. Returns actual damage dealt.</summary>
    public float TakeDamage(float damage, Vector3 sourcePosition = default)
    {
        if (isDead) return 0f;
        if (invincibilityTimer > 0f) return 0f;
        if (damage <= 0f) return 0f;

        float actualDamage = Mathf.Min(damage, currentHealth);
        currentHealth -= actualDamage;
        invincibilityTimer = invincibilityDuration;
        flashTimer = flashDuration;

        Debug.Log($"[HEALTH] 💥 Took {actualDamage:F0} damage! HP: {currentHealth:F0}/{maxHealth}");
        OnDamaged?.Invoke(actualDamage, currentHealth);

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }

        return actualDamage;
    }

    /// <summary>Heal the player.</summary>
    public void Heal(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        float before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        float healed = currentHealth - before;

        Debug.Log($"[HEALTH] 💚 Healed {healed:F0}! HP: {currentHealth:F0}/{maxHealth}");
        OnHealed?.Invoke(healed);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[HEALTH] 💀 PLAYER DIED!");
        OnDeath?.Invoke();

        // Disable player movement
        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = false;

        var cartInteraction = GetComponent<CartInteraction>();
        if (cartInteraction != null && cartInteraction.IsAttached)
            cartInteraction.DetachFromCart();
    }

    /// <summary>Reset health (respawn).</summary>
    public void Revive()
    {
        currentHealth = maxHealth;
        isDead = false;
        invincibilityTimer = 1f; // Brief invincibility on respawn

        var tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null) tpc.enabled = true;

        Debug.Log("[HEALTH] ✅ Player revived!");
    }
}
