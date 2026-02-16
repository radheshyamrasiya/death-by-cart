using UnityEngine;

/// <summary>
/// Player state machine — manages transitions between FreeRoam, PushingCart, and InventoryOpen.
/// Attach to the Player root GameObject.
/// </summary>
public class PlayerStateMachine : MonoBehaviour
{
    public enum PlayerState
    {
        FreeRoam,
        PushingCart,
        InventoryOpen,
        ControllingRC,
        Hiding
    }

    [Header("Current State (read-only)")]
    [SerializeField] private PlayerState currentState = PlayerState.FreeRoam;

    public PlayerState CurrentState => currentState;

    // References
    private PlayerMotor motor;
    private CartInteraction cartInteraction;

    // Events
    public System.Action<PlayerState, PlayerState> OnStateChanged;

    private void Awake()
    {
        motor = GetComponent<PlayerMotor>();
        cartInteraction = GetComponent<CartInteraction>();
    }

    private void Start()
    {
        EnterState(PlayerState.FreeRoam);
        Debug.Log("[PLAYER] State Machine initialized — FreeRoam");
    }

    public void TransitionTo(PlayerState newState)
    {
        if (newState == currentState) return;

        PlayerState oldState = currentState;
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);

        OnStateChanged?.Invoke(oldState, newState);
        Debug.Log($"[PLAYER] State: {oldState} → {newState}");
    }

    private void EnterState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.FreeRoam:
                if (motor != null) motor.SetMovementEnabled(true);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case PlayerState.PushingCart:
                if (motor != null) motor.SetMovementEnabled(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case PlayerState.InventoryOpen:
                if (motor != null) motor.SetMovementEnabled(false);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                // Time.timeScale = 0.1f; // Optional: slow-mo while managing inventory
                break;

            case PlayerState.ControllingRC:
                if (motor != null) motor.SetMovementEnabled(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;

            case PlayerState.Hiding:
                if (motor != null) motor.SetMovementEnabled(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                break;
        }
    }

    private void ExitState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.InventoryOpen:
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                // Time.timeScale = 1f;
                break;
        }
    }
}
