using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera controller with 3 switchable modes.
/// Attach to the Main Camera. Assign the cart as the Target in the Inspector.
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
    [Tooltip("Drag the Cart GameObject here")]
    [SerializeField] private Transform target;

    [Header("Smooth Follow")]
    [Tooltip("How quickly the camera position catches up")]
    [SerializeField] private float followSpeed = 8f;

    [Tooltip("How quickly the camera rotation catches up")]
    [SerializeField] private float rotationSpeed = 6f;

    [Header("Mode Settings")]
    [SerializeField] private CameraMode currentMode = CameraMode.Chase;

    // --- Chase Cam (classic racing) ---
    [Header("Chase Cam")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 8f, -10f);
    [SerializeField] private float chaseLookAhead = 3f;

    // --- Top-Down (Hades / Diablo style) ---
    [Header("Top-Down Cam")]
    [SerializeField] private Vector3 topDownOffset = new Vector3(0f, 18f, -8f);

    // --- Over-the-Shoulder (RE4 style) ---
    [Header("Over-the-Shoulder Cam")]
    [SerializeField] private Vector3 shoulderOffset = new Vector3(1.5f, 3f, -4f);
    [SerializeField] private float shoulderLookAhead = 2f;

    private void Update()
    {
        // --- Toggle camera mode with C key ---
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

        Vector3 desiredPosition;
        Quaternion desiredRotation;

        switch (currentMode)
        {
            case CameraMode.Chase:
                desiredPosition = target.position + target.TransformDirection(chaseOffset);
                Vector3 chaseLookTarget = target.position + target.forward * chaseLookAhead;
                desiredRotation = Quaternion.LookRotation(chaseLookTarget - desiredPosition);
                break;

            case CameraMode.TopDown:
                // Top-down doesn't rotate with the cart — fixed world-space offset
                desiredPosition = target.position + topDownOffset;
                desiredRotation = Quaternion.LookRotation(target.position - desiredPosition);
                break;

            case CameraMode.OverTheShoulder:
                desiredPosition = target.position + target.TransformDirection(shoulderOffset);
                Vector3 shoulderLookTarget = target.position + target.forward * shoulderLookAhead;
                desiredRotation = Quaternion.LookRotation(shoulderLookTarget - desiredPosition);
                break;

            default:
                return;
        }

        // Smooth follow
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

        Debug.Log($"Camera Mode: {currentMode}");
    }
}
