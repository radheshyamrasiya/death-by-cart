using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages the player ↔ cart bond.
/// Press E near a cart to grab it. Press E again to release.
/// When grabbing: player snaps behind cart, cart input is enabled, player motor is disabled.
/// When releasing: player walks free, cart stays put.
/// </summary>
public class CartInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float pushOffsetBehind = 1.5f;
    [SerializeField] private float pushOffsetUp = 0f;

    [Header("References (auto-found if empty)")]
    [SerializeField] private CartController currentCart;

    // Runtime
    private PlayerStateMachine stateMachine;
    private PlayerMotor motor;
    private bool isAttached;

    // Events
    public System.Action<CartController> OnCartAttached;
    public System.Action OnCartDetached;

    // Public
    public bool IsAttached => isAttached;
    public CartController AttachedCart => currentCart;

    private void Awake()
    {
        stateMachine = GetComponent<PlayerStateMachine>();
        motor = GetComponent<PlayerMotor>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // E key to toggle cart
        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (isAttached)
            {
                DetachFromCart();
            }
            else
            {
                TryAttachToNearbyCart();
            }
        }

        // Stay snapped behind cart while pushing
        if (isAttached && currentCart != null)
        {
            SnapPlayerBehindCart();
        }
    }

    private void TryAttachToNearbyCart()
    {
        // Find the nearest cart
        CartController[] carts = FindObjectsByType<CartController>(FindObjectsSortMode.None);
        CartController nearest = null;
        float nearestDist = interactRange;

        foreach (var cart in carts)
        {
            float dist = Vector3.Distance(transform.position, cart.transform.position);
            if (dist < nearestDist)
            {
                nearest = cart;
                nearestDist = dist;
            }
        }

        if (nearest == null)
        {
            Debug.Log("[INTERACTION] No cart nearby to grab.");
            return;
        }

        AttachToCart(nearest);
    }

    public void AttachToCart(CartController cart)
    {
        if (cart == null) return;

        currentCart = cart;
        isAttached = true;

        // Enable cart controls
        cart.SetInputActive(true);

        // Snap player behind cart
        SnapPlayerBehindCart();

        // Switch state
        if (stateMachine != null)
        {
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.PushingCart);
        }

        Debug.Log("[INTERACTION] 🛒 Grabbed the cart!");
        OnCartAttached?.Invoke(cart);
    }

    public void DetachFromCart()
    {
        if (!isAttached || currentCart == null) return;

        // Disable cart controls
        currentCart.SetInputActive(false);

        isAttached = false;

        // Switch state
        if (stateMachine != null)
        {
            stateMachine.TransitionTo(PlayerStateMachine.PlayerState.FreeRoam);
        }

        Debug.Log("[INTERACTION] Released the cart.");
        OnCartDetached?.Invoke();

        currentCart = null;
    }

    private void SnapPlayerBehindCart()
    {
        if (currentCart == null || motor == null) return;

        // Position: behind the cart
        Vector3 pushPos = currentCart.GetPushPosition(pushOffsetBehind, pushOffsetUp);

        // Face the same direction as the cart
        Quaternion pushRot = currentCart.transform.rotation;

        motor.TeleportTo(pushPos, pushRot);
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
