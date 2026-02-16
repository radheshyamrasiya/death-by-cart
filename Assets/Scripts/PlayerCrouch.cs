using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player crouch controller. Press C (keyboard) or Left Stick Click (gamepad) to toggle crouch.
/// When crouching: move speed reduced, noise drastically reduced, CharacterController height shrinks.
/// The crouch ANIMATION handles the visual — no model scaling needed.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerCrouch : MonoBehaviour
{
    [Header("Crouch Settings")]
    [SerializeField] private float crouchSpeedMultiplier = 0.4f;
    [SerializeField] private float crouchNoiseMultiplier = 0.15f;
    [SerializeField] private float crouchCCHeightMultiplier = 0.6f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    // Runtime
    private bool isCrouching;
    private StarterAssets.ThirdPersonController tpc;
    private CharacterController cc;
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private bool savedSpeeds;
    private float originalCCHeight;
    private Vector3 originalCCCenter;
    private float targetCCHeight;

    // Public API
    public bool IsCrouching => isCrouching;
    public float NoiseMultiplier => isCrouching ? crouchNoiseMultiplier : 1f;

    private void Start()
    {
        tpc = GetComponent<StarterAssets.ThirdPersonController>();
        cc = GetComponent<CharacterController>();

        if (tpc != null)
        {
            baseMoveSpeed = tpc.MoveSpeed;
            baseSprintSpeed = tpc.SprintSpeed;
            savedSpeeds = true;
        }

        if (cc != null)
        {
            originalCCHeight = cc.height;
            originalCCCenter = cc.center;
            targetCCHeight = originalCCHeight;
        }
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

        // Smooth CharacterController height transition
        if (cc != null)
        {
            cc.height = Mathf.Lerp(cc.height, targetCCHeight, Time.deltaTime * crouchTransitionSpeed);
            // Keep the CC center at half-height so feet stay on the ground
            cc.center = new Vector3(originalCCCenter.x, cc.height * 0.5f, originalCCCenter.z);
        }
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

        // Shrink CharacterController height (collision/detection only)
        // The crouch ANIMATION handles the visual change
        targetCCHeight = originalCCHeight * crouchCCHeightMultiplier;

        // Reduce speed — overrides carry slowdown
        if (tpc != null && savedSpeeds)
        {
            tpc.MoveSpeed *= crouchSpeedMultiplier;
            tpc.SprintSpeed *= crouchSpeedMultiplier;
        }

        Debug.Log("[CROUCH] 🦆 Crouching — movement slow, very quiet");
    }

    public void StandUp()
    {
        if (!isCrouching) return;
        isCrouching = false;

        // Restore CharacterController height
        targetCCHeight = originalCCHeight;

        // Restore speed — but keep carry weight penalty if carrying
        if (tpc != null && savedSpeeds)
        {
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
