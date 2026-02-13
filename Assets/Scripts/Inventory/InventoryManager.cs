using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages inventory open/close and bridges GridInventory with InventoryUI.
/// Tab / Gamepad Select to toggle. Pauses player input when open.
/// Attach to the Cart (same as GridInventory).
/// </summary>
public class InventoryManager : MonoBehaviour
{
    private GridInventory gridInventory;
    private InventoryUI inventoryUI;
    private PlayerStateMachine playerState;
    private CartController cartController;

    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Start()
    {
        gridInventory = GetComponent<GridInventory>();
        if (gridInventory == null)
        {
            gridInventory = gameObject.AddComponent<GridInventory>();
            Debug.Log("[INVMGR] Created GridInventory");
        }

        // Create UI
        inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null)
        {
            inventoryUI = gameObject.AddComponent<InventoryUI>();
        }
        inventoryUI.Initialize(gridInventory);

        // Find player
        playerState = FindFirstObjectByType<PlayerStateMachine>();
        cartController = GetComponent<CartController>();

        Debug.Log("[INVMGR] ✅ Inventory system ready! Press Tab to open.");
    }

    private void Update()
    {
        // Tab to toggle
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            Toggle();
        }

        // Gamepad Select/Back
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.selectButton.wasPressedThisFrame)
        {
            Toggle();
        }

        // Escape to close
        if (isOpen && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        // Only open if player is attached to cart
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (cartInteraction == null || !cartInteraction.IsAttached)
        {
            Debug.Log("[INVMGR] Can't open inventory — not attached to cart!");
            return;
        }

        isOpen = true;
        inventoryUI.Open();

        // Disable cart movement while inventory is open
        if (cartController != null)
            cartController.SetInputActive(false);

        // Set player state
        if (playerState != null)
            playerState.TransitionTo(PlayerStateMachine.PlayerState.InventoryOpen);

        Debug.Log("[INVMGR] 📦 Inventory opened");
    }

    public void Close()
    {
        isOpen = false;
        inventoryUI.Close();

        // Check if player is still attached to the cart
        var cartInteraction = FindFirstObjectByType<CartInteraction>();
        bool stillAttached = cartInteraction != null && cartInteraction.IsAttached;

        if (stillAttached)
        {
            // Re-enable cart movement (player is still holding the cart)
            if (cartController != null)
                cartController.SetInputActive(true);

            // Return to pushing state
            if (playerState != null)
                playerState.TransitionTo(PlayerStateMachine.PlayerState.PushingCart);
        }
        else
        {
            // Player already detached — make sure cart input stays off
            if (cartController != null)
                cartController.SetInputActive(false);

            // Go to free roam
            if (playerState != null)
                playerState.TransitionTo(PlayerStateMachine.PlayerState.FreeRoam);
        }

        Debug.Log($"[INVMGR] 📦 Inventory closed (attached: {stillAttached})");
    }

    /// <summary>
    /// Add an item directly (e.g. from auto-pickup). Returns true if placed.
    /// </summary>
    public bool AddItem(ItemData item)
    {
        if (gridInventory == null) return false;
        return gridInventory.AutoPlace(item);
    }

    /// <summary>
    /// Check if cart has space for this item.
    /// </summary>
    public bool HasSpaceFor(ItemData item)
    {
        if (gridInventory == null) return false;

        // Check weight
        if (gridInventory.CurrentWeight + item.weight > gridInventory.MaxWeight)
            return false;

        // Check space (try phantom placement)
        for (int pass = 0; pass < 2; pass++)
        {
            bool rotated = pass == 1;
            if (!item.canRotate && rotated) continue;

            int w = item.GetWidth(rotated);
            int h = item.GetHeight(rotated);

            for (int y = 0; y <= gridInventory.GridHeight - h; y++)
            {
                for (int x = 0; x <= gridInventory.GridWidth - w; x++)
                {
                    if (gridInventory.CanFit(item, x, y, rotated))
                        return true;
                }
            }
        }
        return false;
    }
}
