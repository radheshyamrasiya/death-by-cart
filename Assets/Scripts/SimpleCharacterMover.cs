using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple WASD controller for the character test scene.
/// Attach to a character with a CharacterController.
/// Auto-fits camera to character's actual bounds at Start().
/// </summary>
public class SimpleCharacterMover : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float rotationSpeed = 720f;
    public float gravity = -20f;

    [Header("Camera Orbit (auto-calculated if 0)")]
    public float mouseSensitivity = 2f;
    public float cameraDistance = 0f;   // 0 = auto-fit to character height
    public float cameraHeight = 0f;     // 0 = auto-fit
    public float lookAtHeight = 0f;     // 0 = auto-fit

    private CharacterController cc;
    private Animator animator;
    private Camera cam;
    private float yaw;
    private float pitch = 15f;
    private float verticalVelocity;
    private float characterHeight = 1.75f;
    private bool isCrouching;
    private Vector3 camSmoothVel; // for SmoothDamp

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int CrouchHash = Animator.StringToHash("IsCrouching");

    void Start()
    {
        cc = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        cam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (animator != null)
            animator.applyRootMotion = false;

        // Measure character height and auto-fit camera
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            foreach (var r in renderers)
                totalBounds.Encapsulate(r.bounds);

            characterHeight = totalBounds.size.y;

            Debug.Log("══════════════════════════════════════");
            Debug.Log($"[CHAR MOVER] Character height: {characterHeight:F3}m");
            Debug.Log($"[CHAR MOVER] Bounds: {totalBounds.min} → {totalBounds.max}");
            Debug.Log("══════════════════════════════════════");

            // Auto-fit camera to character size
            if (cameraDistance <= 0f) cameraDistance = characterHeight * 2.5f;
            if (cameraHeight <= 0f)   cameraHeight = characterHeight * 0.7f;
            if (lookAtHeight <= 0f)   lookAtHeight = characterHeight * 0.5f;
        }
        else
        {
            // Fallback defaults
            if (cameraDistance <= 0f) cameraDistance = 4f;
            if (cameraHeight <= 0f)   cameraHeight = 2f;
            if (lookAtHeight <= 0f)   lookAtHeight = 1f;
        }

        Debug.Log($"[CHAR MOVER] Camera: dist={cameraDistance:F1} height={cameraHeight:F1} lookAt={lookAtHeight:F1}");
        Debug.Log("[CONTROLS] WASD = Move | Shift = Run | C = Crouch | Mouse = Look | ESC = Unlock cursor");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // ESC to unlock cursor
        if (kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        // C to toggle crouch
        if (kb.cKey.wasPressedThisFrame)
            isCrouching = !isCrouching;

        // Mouse look
        var mouse = Mouse.current;
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw += delta.x * mouseSensitivity * 0.1f;
            pitch -= delta.y * mouseSensitivity * 0.1f;
            pitch = Mathf.Clamp(pitch, -10f, 60f);
        }

        // Movement input
        Vector2 input = Vector2.zero;
        if (kb.wKey.isPressed) input.y += 1;
        if (kb.sKey.isPressed) input.y -= 1;
        if (kb.aKey.isPressed) input.x -= 1;
        if (kb.dKey.isPressed) input.x += 1;
        input = input.normalized;

        bool sprinting = kb.leftShiftKey.isPressed && !isCrouching;
        float speed = isCrouching ? walkSpeed * 0.5f : (sprinting ? runSpeed : walkSpeed);

        // Camera-relative movement
        Vector3 camForward = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
        Vector3 camRight = Quaternion.Euler(0, yaw, 0) * Vector3.right;
        Vector3 moveDir = (camForward * input.y + camRight * input.x).normalized;

        // Gravity
        if (cc.isGrounded)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = moveDir * speed + Vector3.up * verticalVelocity;
        cc.Move(motion * Time.deltaTime);

        // Rotate character to face movement direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Update animator
        float currentSpeed = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;
        if (animator != null)
        {
            animator.SetFloat(SpeedHash, currentSpeed);
            animator.SetBool(CrouchHash, isCrouching);
        }
    }

    /// <summary>
    /// Camera follows in LateUpdate with SmoothDamp to prevent wobble/jitter.
    /// </summary>
    void LateUpdate()
    {
        if (cam == null) return;

        Vector3 offset = Quaternion.Euler(pitch, yaw, 0) * new Vector3(0, 0, -cameraDistance);
        Vector3 targetPos = transform.position + Vector3.up * cameraHeight + offset;

        // Smooth follow — eliminates the left/right wobble
        cam.transform.position = Vector3.SmoothDamp(
            cam.transform.position, targetPos, ref camSmoothVel, 0.06f);

        cam.transform.LookAt(transform.position + Vector3.up * lookAtHeight);
    }
}
