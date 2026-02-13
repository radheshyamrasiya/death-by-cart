using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person orbit camera with mouse look.
/// Mouse moves the camera around the player. WASD movement is relative to camera facing.
/// Press C to cycle modes. Supports both free-roam and cart-pushing states.
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Chase,
        TopDown,
        OverTheShoulder,
        FirstPerson
    }

    [Header("Target")]
    [Tooltip("The player transform to follow (auto-found if empty)")]
    [SerializeField] private Transform target;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 12f;

    [Header("Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.Chase;

    [Header("Chase Cam")]
    [SerializeField] private float chaseDistance = 10f;
    [SerializeField] private float chaseHeight = 5f;

    [Header("Top-Down Cam")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 18f, -8f);

    [Header("Over-the-Shoulder Cam")]
    [SerializeField] private float shoulderDistance = 4f;
    [SerializeField] private float shoulderHeight = 2f;
    [SerializeField] private float shoulderSide = 1.5f;

    [Header("First-Person Cam (Cart Mode)")]
    [SerializeField] private float fpEyeHeight = 1.7f;
    [SerializeField] private float fpMinPitch = -60f;
    [SerializeField] private float fpMaxPitch = 70f;
    [Tooltip("Degrees player can look left/right before the cart starts turning")]
    [SerializeField] private float fpFreeYawRange = 30f;
    [Tooltip("How fast the cart turns to follow the camera when looking far")]
    [SerializeField] private float fpCartTurnSpeed = 3f;

    // Mouse orbit state
    private float yaw;
    private float pitch = 20f;

    // Auto-switch state
    private CameraMode modeBeforeCart;
    private bool wasAttached;
    private float cartBaseYaw; // cart's forward yaw when attached

    // Player references
    private CartInteraction playerCartInteraction;

    private void Start()
    {
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Auto-find the player if not assigned
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerMotor>();
            if (player != null)
            {
                target = player.transform;
                playerCartInteraction = player.GetComponent<CartInteraction>();
                Debug.Log("[CAMERA] ✅ Auto-found player target.");
            }
            else
            {
                var cart = FindFirstObjectByType<CartController>();
                if (cart != null)
                {
                    target = cart.transform;
                    Debug.Log("[CAMERA] No player found, falling back to cart target.");
                }
            }
        }
        else
        {
            playerCartInteraction = target.GetComponent<CartInteraction>();
        }

        // Initialize yaw from current camera angle
        yaw = transform.eulerAngles.y;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // Cycle camera mode: C key or D-pad Up
        if ((keyboard != null && keyboard.cKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.dpad.up.wasPressedThisFrame))
        {
            CycleMode();
        }

        // Mouse look input
        var mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity * 0.1f;
            pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
        }

        // Gamepad right stick look
        if (gamepad != null)
        {
            Vector2 rightStick = gamepad.rightStick.ReadValue();
            if (rightStick.sqrMagnitude > 0.01f)
            {
                yaw += rightStick.x * mouseSensitivity * 2f * Time.deltaTime * 60f;
                pitch -= rightStick.y * mouseSensitivity * 2f * Time.deltaTime * 60f;
            }
        }

        pitch = Mathf.Clamp(pitch, 
            currentMode == CameraMode.FirstPerson ? fpMinPitch : minPitch, 
            currentMode == CameraMode.FirstPerson ? fpMaxPitch : maxPitch);

        // --- Cart mode: limit yaw and steer cart ---
        if (currentMode == CameraMode.FirstPerson && playerCartInteraction != null 
            && playerCartInteraction.IsAttached && playerCartInteraction.AttachedCart != null)
        {
            Transform cartTransform = playerCartInteraction.AttachedCart.transform;
            cartBaseYaw = cartTransform.eulerAngles.y;

            // How far are we looking from cart's forward?
            float yawOffset = Mathf.DeltaAngle(cartBaseYaw, yaw);

            if (Mathf.Abs(yawOffset) > fpFreeYawRange)
            {
                // Turn the cart toward where we're looking
                float turnDir = Mathf.Sign(yawOffset);
                float turnAmount = (Mathf.Abs(yawOffset) - fpFreeYawRange) * fpCartTurnSpeed * Time.deltaTime;
                cartTransform.Rotate(Vector3.up, turnDir * turnAmount, Space.World);
            }

            // Clamp camera yaw to not go too far from cart forward (±90° hard limit)
            float maxYawOffset = 90f;
            float clampedOffset = Mathf.Clamp(Mathf.DeltaAngle(cartBaseYaw, yaw), -maxYawOffset, maxYawOffset);
            yaw = cartBaseYaw + clampedOffset;
        }

        // Auto-switch to FirstPerson when grabbing cart
        if (playerCartInteraction != null)
        {
            bool attached = playerCartInteraction.IsAttached;
            if (attached && !wasAttached)
            {
                modeBeforeCart = currentMode;
                currentMode = CameraMode.FirstPerson;
                // Snap yaw to cart's forward direction
                if (playerCartInteraction.AttachedCart != null)
                    yaw = playerCartInteraction.AttachedCart.transform.eulerAngles.y;
                pitch = 10f;
                Debug.Log("[CAMERA] Auto-switched to FirstPerson (cart mode)");
            }
            else if (!attached && wasAttached)
            {
                currentMode = modeBeforeCart;
                Debug.Log($"[CAMERA] Restored to {currentMode}");
            }
            wasAttached = attached;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition;
        Quaternion desiredRotation;

        switch (currentMode)
        {
            case CameraMode.Chase:
                desiredPosition = CalculateOrbitPosition(chaseDistance, chaseHeight, 0f);
                desiredRotation = Quaternion.LookRotation(target.position + Vector3.up * 1.5f - desiredPosition);
                break;

            case CameraMode.TopDown:
                // Top-down: yaw rotates around player, pitch is mostly locked high
                desiredPosition = target.position + topDownOffset;
                desiredRotation = Quaternion.LookRotation(target.position - desiredPosition);
                break;

            case CameraMode.OverTheShoulder:
                desiredPosition = CalculateOrbitPosition(shoulderDistance, shoulderHeight, shoulderSide);
                Vector3 shoulderLookTarget = target.position + Vector3.up * 1.5f;
                desiredRotation = Quaternion.LookRotation(shoulderLookTarget - desiredPosition);
                break;

            case CameraMode.FirstPerson:
                // Camera at player's eye height, looking where mouse points
                desiredPosition = target.position + Vector3.up * fpEyeHeight;
                desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
                break;

            default:
                return;
        }

        // Smooth follow (except FP which snaps immediately)
        if (currentMode == CameraMode.FirstPerson)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            transform.rotation = desiredRotation;
        }
    }

    /// <summary>
    /// Calculate orbit position around target using yaw/pitch angles.
    /// </summary>
    private Vector3 CalculateOrbitPosition(float distance, float height, float sideOffset)
    {
        // Convert yaw/pitch to a direction
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(sideOffset, 0f, -distance);
        offset.y += height;

        return target.position + offset;
    }

    /// <summary>
    /// Get the camera's yaw rotation as a Quaternion for camera-relative movement.
    /// Used by PlayerMotor to orient WASD input.
    /// </summary>
    public Quaternion GetYawRotation()
    {
        return Quaternion.Euler(0f, yaw, 0f);
    }

    /// <summary>
    /// Get the camera's yaw angle in degrees.
    /// </summary>
    public float GetYaw()
    {
        return yaw;
    }

    private void CycleMode()
    {
        currentMode = currentMode switch
        {
            CameraMode.Chase => CameraMode.TopDown,
            CameraMode.TopDown => CameraMode.OverTheShoulder,
            CameraMode.OverTheShoulder => CameraMode.FirstPerson,
            CameraMode.FirstPerson => CameraMode.Chase,
            _ => CameraMode.Chase
        };

        Debug.Log($"[CAMERA] Mode: {currentMode}");
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        playerCartInteraction = newTarget != null ? newTarget.GetComponent<CartInteraction>() : null;
    }
}
