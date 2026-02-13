using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Lightweight third-person character motor using CharacterController.
/// Handles WASD movement relative to camera, gravity, and basic animation hooks.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundMask = ~0;

    // Runtime
    private CharacterController cc;
    private CameraController camController;
    private Vector3 velocity;
    private float turnSmoothVelocity;
    private bool movementEnabled = true;
    private bool isGrounded;

    // Public getters
    public bool IsMoving => movementEnabled && GetMoveInput().sqrMagnitude > 0.01f;
    public bool IsGrounded => isGrounded;
    public float CurrentSpeed => cc != null ? new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude : 0f;
    public bool IsSprinting { get; private set; }

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        Debug.Log("[PLAYER] ✅ PlayerMotor ready!");
    }

    private void Start()
    {
        // Find camera controller (may not exist in Awake yet)
        camController = FindFirstObjectByType<CameraController>();
    }

    private void Update()
    {
        if (!movementEnabled || cc == null) 
        {
            // Still apply gravity even when disabled
            ApplyGravity();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Ground check
        isGrounded = cc.isGrounded;

        // Gravity
        ApplyGravity();

        // Sprint
        IsSprinting = keyboard.leftShiftKey.isPressed;

        // Movement input
        Vector2 input = GetMoveInput();

        if (input.sqrMagnitude > 0.01f)
        {
            float speed = IsSprinting ? sprintSpeed : walkSpeed;

            // Direction relative to camera yaw
            float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg;
            if (camController != null)
            {
                targetAngle += camController.GetYaw();
            }

            // Smooth rotation
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, 
                targetAngle, 
                ref turnSmoothVelocity, 
                rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move in facing direction
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            cc.Move(moveDir.normalized * speed * Time.deltaTime + velocity * Time.deltaTime);
        }
        else
        {
            // Just apply gravity
            cc.Move(velocity * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (cc == null) return;

        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f; // Small downward force to stay grounded
        }

        velocity.y += gravity * Time.deltaTime;

        if (!movementEnabled)
        {
            cc.Move(velocity * Time.deltaTime);
        }
    }

    private Vector2 GetMoveInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;

        return input.normalized;
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
        if (!enabled)
        {
            IsSprinting = false;
        }
    }

    /// <summary>
    /// Teleport the player to a position with a specific rotation.
    /// Used when attaching to cart push position.
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        cc.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        cc.enabled = true;
    }
}
