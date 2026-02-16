using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The RC Car itself — spawned when the player deploys it.
/// Drives with WASD, emits noise, has battery + HP.
/// Destroyed when battery runs out, HP hits 0, or player cancels.
/// </summary>
public class RCCarController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float turnSpeed = 180f;
    [SerializeField] private float gravity = -20f;

    [Header("Battery")]
    [SerializeField] private float maxBattery = 10f;

    [Header("Health")]
    [SerializeField] private float maxHP = 10f;

    [Header("Noise")]
    [SerializeField] private float noiseRadius = 12f;

    // Runtime
    private float currentBattery;
    private float currentHP;
    private CharacterController cc;
    private float verticalVelocity;
    private bool isActive;
    private bool isDestroyed;

    // Public API
    public float BatteryPercent => currentBattery / maxBattery;
    public float HPPercent => currentHP / maxHP;
    public float CurrentBattery => currentBattery;
    public float MaxBattery => maxBattery;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float NoiseRadius => isActive ? noiseRadius : 0f;
    public bool IsActive => isActive;
    public bool IsDestroyed => isDestroyed;

    // Callback when car is destroyed/expires
    public System.Action OnCarDestroyed;

    public void Activate()
    {
        currentBattery = maxBattery;
        currentHP = maxHP;
        isActive = true;
        isDestroyed = false;

        // Add CharacterController for movement
        cc = gameObject.AddComponent<CharacterController>();
        cc.height = 0.4f;
        cc.radius = 0.2f;
        cc.center = new Vector3(0, 0.2f, 0);
        cc.slopeLimit = 45f;
        cc.stepOffset = 0.2f;

        Debug.Log("[RC CAR] Activated! 10s battery, WASD to drive.");
    }

    private void Update()
    {
        if (!isActive || isDestroyed) return;

        // Battery drain
        currentBattery -= Time.deltaTime;
        if (currentBattery <= 0f)
        {
            currentBattery = 0f;
            DestroyCar("Battery depleted");
            return;
        }

        // Movement
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (cc == null) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float forward = 0f;
        float turn = 0f;

        if (keyboard.wKey.isPressed) forward += 1f;
        if (keyboard.sKey.isPressed) forward -= 1f;
        if (keyboard.aKey.isPressed) turn -= 1f;
        if (keyboard.dKey.isPressed) turn += 1f;

        // Gamepad support
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            forward += stick.y;
            turn += stick.x;
        }

        // Turn
        if (Mathf.Abs(turn) > 0.01f)
        {
            transform.Rotate(Vector3.up, turn * turnSpeed * Time.deltaTime);
        }

        // Move forward/backward
        Vector3 move = transform.forward * forward * moveSpeed;

        // Gravity
        if (cc.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        move.y = verticalVelocity;

        cc.Move(move * Time.deltaTime);
    }

    /// <summary>Called by zombies when they attack the car.</summary>
    public void TakeDamage(float damage)
    {
        if (!isActive || isDestroyed) return;

        currentHP -= damage;
        Debug.Log($"[RC CAR] Took {damage} damage! HP: {currentHP:F0}/{maxHP:F0}");

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            DestroyCar("Destroyed by zombie");
        }
    }

    /// <summary>Called by player to cancel early.</summary>
    public void CancelByPlayer()
    {
        if (!isActive || isDestroyed) return;
        DestroyCar("Cancelled by player");
    }

    private void DestroyCar(string reason)
    {
        if (isDestroyed) return;
        isDestroyed = true;
        isActive = false;

        Debug.Log($"[RC CAR] {reason}! Returning control to player.");
        OnCarDestroyed?.Invoke();

        // Destruction effect — shrink and vanish
        StartCoroutine(DestroyAnimation());
    }

    private System.Collections.IEnumerator DestroyAnimation()
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            float p = t / 0.4f;
            transform.localScale = startScale * (1f - p);
            transform.position += Vector3.up * Time.deltaTime * 2f;
            yield return null;
        }
        Destroy(gameObject);
    }

    /// <summary>Draw HUD showing battery and HP.</summary>
    private void OnGUI()
    {
        if (!isActive || isDestroyed) return;

        float cx = Screen.width / 2f;
        float barW = 200f;
        float barH = 12f;

        // ─── RC CAR label ───
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.9f, 0.3f) }
        };
        GUI.Label(new Rect(cx - 100, Screen.height - 100, 200, 24), "RC CAR", titleStyle);

        // ─── Battery bar ───
        float batY = Screen.height - 75;
        DrawHUDBar(cx - barW / 2, batY, barW, barH, BatteryPercent,
            Color.Lerp(Color.red, Color.green, BatteryPercent), "BAT");

        // ─── HP bar ───
        float hpY = Screen.height - 58;
        DrawHUDBar(cx - barW / 2, hpY, barW, barH, HPPercent,
            Color.Lerp(Color.red, new Color(0.2f, 0.8f, 1f), HPPercent), "HP");

        // ─── Timer text ───
        GUIStyle timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(cx - 60, Screen.height - 40, 120, 20),
            $"{currentBattery:F1}s  |  V = cancel", timerStyle);
    }

    private void DrawHUDBar(float x, float y, float w, float h, float fill, Color color, string label)
    {
        Color old = GUI.color;

        // Background
        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);

        // Fill
        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(fill), h), Texture2D.whiteTexture);

        // Label
        GUI.color = Color.white;
        GUIStyle s = new GUIStyle(GUI.skin.label)
        {
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(x + 4, y - 1, 50, h), label, s);

        GUI.color = old;
    }
}
