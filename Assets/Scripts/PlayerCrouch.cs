using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player crouch controller. Press C (keyboard) or Left Stick Click (gamepad) to toggle crouch.
/// When crouching: move speed reduced, noise drastically reduced, player model scales down.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeedMultiplier = 0.4f;
    [SerializeField] private float crouchNoiseMultiplier = 0.15f;
    [SerializeField] private float crouchScaleY = 0.6f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    // Runtime
    private bool isCrouching;
    private StarterAssets.ThirdPersonController tpc;
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private bool savedSpeeds;
    private float targetScaleY = 1f;
    private float originalScaleY = 1f;

    // Public API
    public bool IsCrouching => isCrouching;
    public float NoiseMultiplier => isCrouching ? crouchNoiseMultiplier : 1f;

    private void Start()
    {
        tpc = GetComponent<StarterAssets.ThirdPersonController>();
        if (tpc != null)
        {
            baseMoveSpeed = tpc.MoveSpeed;
            baseSprintSpeed = tpc.SprintSpeed;
            savedSpeeds = true;
        }
        originalScaleY = transform.localScale.y;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // Toggle crouch: C key or Left Stick Click (L3)
        bool crouchPressed = false;
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame) crouchPressed = true;
        if (gamepad != null && gamepad.leftStickButton.wasPressedThisFrame) crouchPressed = true;

        if (crouchPressed)
        {
            ToggleCrouch();
        }

        // Can't crouch while pushing cart — auto-stand
        var cartInteraction = GetComponent<CartInteraction>();
        if (cartInteraction != null && cartInteraction.IsAttached && isCrouching)
        {
            StandUp();
        }

        // Smooth scale transition
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Lerp(scale.y, targetScaleY, Time.deltaTime * crouchTransitionSpeed);
        transform.localScale = scale;
    }

    private void ToggleCrouch()
    {
        if (isCrouching)
            StandUp();
        else
            Crouch();
    }

    private void Crouch()
    {
        if (isCrouching) return;
        isCrouching = true;
        targetScaleY = originalScaleY * crouchScaleY;

        // Reduce speed — overrides carry slowdown
        if (tpc != null && savedSpeeds)
        {
            // Read current speed (might already be slowed by carry weight)
            tpc.MoveSpeed *= crouchSpeedMultiplier;
            tpc.SprintSpeed *= crouchSpeedMultiplier;
        }

        Debug.Log("[CROUCH] 🦆 Crouching — movement slow, very quiet");
    }

    public void StandUp()
    {
        if (!isCrouching) return;
        isCrouching = false;
        targetScaleY = originalScaleY;

        // Restore speed — but keep carry weight penalty if carrying
        if (tpc != null && savedSpeeds)
        {
            // Undo crouch multiplier
            tpc.MoveSpeed /= crouchSpeedMultiplier;
            tpc.SprintSpeed /= crouchSpeedMultiplier;
        }

        Debug.Log("[CROUCH] 🧍 Standing up");
    }

    // ======================== HUD ========================

    private void OnGUI()
    {
        if (!isCrouching) return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = new Color(0.7f, 0.9f, 1f);
        style.fontStyle = FontStyle.Bold;

        bool hasGamepad = Gamepad.current != null;
        string key = hasGamepad ? "L3" : "C";

        float w = 200, h = 30;
        Rect rect = new Rect((Screen.width - w) / 2f, Screen.height - 70, w, h);
        GUI.backgroundColor = new Color(0.1f, 0.3f, 0.5f, 0.8f);
        GUI.Box(rect, $"🦆 CROUCHING  [{key}] Stand", style);
        GUI.backgroundColor = Color.white;
    }
}
