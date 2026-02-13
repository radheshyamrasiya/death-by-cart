using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera controller with 3 switchable modes.
/// Follows the PLAYER at all times (whether free roaming or pushing the cart).
/// Press C to cycle modes: Chase → Top-Down → Over-the-Shoulder.
/// </summary>
public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        Chase,
        TopDown,
        OverTheShoulder
    }

    [Header("Target")]
    [Tooltip("The player transform to follow (auto-found if empty)")]
    [SerializeField] private Transform target;

    [Header("Smooth Follow")]
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Mode")]
    [SerializeField] private CameraMode currentMode = CameraMode.Chase;

    [Header("Chase Cam")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 8f, -10f);
    [SerializeField] private float chaseLookAhead = 3f;

    [Header("Top-Down Cam")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 18f, -8f);

    [Header("Over-the-Shoulder Cam")]
    [SerializeField] private Vector3 shoulderOffset = new Vector3(1.5f, 3f, -4f);
    [SerializeField] private float shoulderLookAhead = 2f;

    private CartInteraction playerCartInteraction;

    private void Start()
    {
        // Auto-find the player if not assigned
        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerMotor>();
            if (player != null)
            {
                target = player.transform;
                playerCartInteraction = player.GetComponent<CartInteraction>();
                Debug.Log("[CAMERA] Auto-found player target.");
            }
            else
            {
                // Fallback: look for cart (legacy behavior)
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
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.cKey.wasPressedThisFrame)
        {
            CycleMode();
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Determine the "look-at" reference — use the cart's forward when pushing
        Transform lookRef = target;
        if (playerCartInteraction != null && playerCartInteraction.IsAttached && playerCartInteraction.AttachedCart != null)
        {
            lookRef = playerCartInteraction.AttachedCart.transform;
        }

        Vector3 desiredPosition;
        Quaternion desiredRotation;

        switch (currentMode)
        {
            case CameraMode.Chase:
                desiredPosition = target.position + lookRef.TransformDirection(chaseOffset);
                Vector3 chaseLookTarget = target.position + lookRef.forward * chaseLookAhead;
                desiredRotation = Quaternion.LookRotation(chaseLookTarget - desiredPosition);
                break;

            case CameraMode.TopDown:
                desiredPosition = target.position + topDownOffset;
                desiredRotation = Quaternion.LookRotation(target.position - desiredPosition);
                break;

            case CameraMode.OverTheShoulder:
                desiredPosition = target.position + lookRef.TransformDirection(shoulderOffset);
                Vector3 shoulderLookTarget = target.position + lookRef.forward * shoulderLookAhead;
                desiredRotation = Quaternion.LookRotation(shoulderLookTarget - desiredPosition);
                break;

            default:
                return;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
    }

    private void CycleMode()
    {
        currentMode = currentMode switch
        {
            CameraMode.Chase => CameraMode.TopDown,
            CameraMode.TopDown => CameraMode.OverTheShoulder,
            CameraMode.OverTheShoulder => CameraMode.Chase,
            _ => CameraMode.Chase
        };

        Debug.Log($"[CAMERA] Mode: {currentMode}");
    }

    /// <summary>
    /// Set the camera target at runtime.
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        playerCartInteraction = newTarget != null ? newTarget.GetComponent<CartInteraction>() : null;
    }
}
