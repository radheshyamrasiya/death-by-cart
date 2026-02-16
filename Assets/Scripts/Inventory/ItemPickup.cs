using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to the Player.
/// Controls:
///   Keyboard: F = pick up / deposit | G = drop | Tab = inventory
///   Gamepad:  X = pick up / deposit | B = drop | Select = inventory
///
/// Flow: Leave cart → walk to item → F to carry → walk to cart → F to deposit.
/// If cart is full, opens inventory for manual placement.
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private float depositRange = 4f;

    [Header("Carry Weight Penalty")]
    [Tooltip("Heaviest item weight that causes max slowdown")]
    [SerializeField] private float heavyItemWeight = 10f;
    [Tooltip("Speed multiplier at max carry weight (0.5 = 50% speed)")]
    [SerializeField] private float maxCarrySlowdown = 0.5f;

    // State
    private WorldItem nearestItem;
    private float nearestDist;
    private ItemData carriedItem;
    private bool isCarrying;

    // References
    private CartInteraction cartInteraction;
    private StarterAssets.ThirdPersonController tpc;
    private StaminaSystem stamina;
    private float baseMoveSpeed;
    private float baseSprintSpeed;
    private bool savedSpeeds;

    // Prompt style
    private GUIStyle promptStyle;
    private GUIStyle carryStyle;

    public bool IsCarrying => isCarrying;
    public ItemData CarriedItem => carriedItem;

    private void Start()
    {
        cartInteraction = GetComponent<CartInteraction>();
        tpc = GetComponent<StarterAssets.ThirdPersonController>();
        stamina = GetComponent<StaminaSystem>();

        // Save base speeds
        if (tpc != null)
        {
            baseMoveSpeed = tpc.MoveSpeed;
            baseSprintSpeed = tpc.SprintSpeed;
            savedSpeeds = true;
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // Always scan for nearby items
        FindNearestItem();

        // ─── F / Gamepad X = context-sensitive item action ───
        bool itemActionPressed = false;
        if (keyboard != null && keyboard.fKey.wasPressedThisFrame) itemActionPressed = true;
        if (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) itemActionPressed = true;

        // ─── G / Gamepad B = drop carried item ───
        bool dropPressed = false;
        if (keyboard != null && keyboard.gKey.wasPressedThisFrame) dropPressed = true;
        if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) dropPressed = true;

        if (isCarrying)
        {
            // Currently carrying an item
            if (itemActionPressed)
            {
                // F = deposit to cart (priority over picking up another item)
                TryDeposit();
            }
            else if (dropPressed)
            {
                // G = drop on ground
                DropItem();
            }
        }
        else
        {
            // Not carrying — F = pick up nearby item
            if (itemActionPressed)
            {
                TryPickup();
            }
        }
    }

    private void FindNearestItem()
    {
        nearestItem = null;
        nearestDist = pickupRange;

        WorldItem[] items = FindObjectsByType<WorldItem>(FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item.IsPickedUp) continue;
            if (item.itemData == null) continue;

            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist < nearestDist)
            {
                nearestItem = item;
                nearestDist = dist;
            }
        }
    }

    private void TryPickup()
    {
        if (nearestItem == null || nearestItem.itemData == null) return;
        if (isCarrying) return;

        // Must NOT be attached to cart to pick up
        if (cartInteraction != null && cartInteraction.IsAttached)
        {
            Debug.Log("[PICKUP] Let go of the cart first! (Press E)");
            return;
        }

        // Pick it up — carry it
        carriedItem = nearestItem.itemData;
        nearestItem.OnPickedUp();
        nearestItem = null;

        // Check if it's a gadget (RC car, etc.) — route to gadget inventory
        if (carriedItem.type == ItemData.ItemType.Gadget)
        {
            var gadgetInv = GetComponent<GadgetInventory>();
            if (gadgetInv != null && gadgetInv.AddGadget(carriedItem))
            {
                carriedItem = null; // Don't carry it as a normal item
                return;
            }
            else
            {
                Debug.Log("[PICKUP] Gadget slots full! Can't pick up.");
                carriedItem = null;
                return;
            }
        }

        isCarrying = true;

        // Apply speed penalty
        ApplyCarrySlowdown();

        Debug.Log($"[PICKUP] Carrying: {carriedItem.itemName} ({carriedItem.weight:F1}kg) — go to cart and press F to deposit!");
    }

    private void TryDeposit()
    {
        if (!isCarrying || carriedItem == null) return;

        // Gadgets can't go in the cart
        if (carriedItem.type == ItemData.ItemType.Gadget)
        {
            Debug.Log("[PICKUP] Gadgets can't be placed in the cart!");
            return;
        }

        // Find nearest cart
        CartController[] carts = FindObjectsByType<CartController>(FindObjectsSortMode.None);
        CartController nearestCart = null;
        float nearestCartDist = depositRange;

        foreach (var cart in carts)
        {
            float dist = Vector3.Distance(transform.position, cart.transform.position);
            if (dist < nearestCartDist)
            {
                nearestCart = cart;
                nearestCartDist = dist;
            }
        }

        if (nearestCart == null)
        {
            Debug.Log("[PICKUP] No cart nearby! Walk closer to the cart.");
            return;
        }

        // Get inventory
        InventoryManager invManager = nearestCart.GetComponent<InventoryManager>();
        GridInventory gridInv = nearestCart.GetComponent<GridInventory>();

        if (invManager == null || gridInv == null)
        {
            Debug.LogWarning("[PICKUP] Cart has no inventory system!");
            return;
        }

        // Try auto-place first
        if (gridInv.AutoPlace(carriedItem))
        {
            Debug.Log($"[PICKUP] ✅ Deposited {carriedItem.itemName} into cart!");
            carriedItem = null;
            isCarrying = false;
            RestoreSpeed();
            return;
        }

        // Cart full — open inventory for manual placement
        Debug.Log($"[PICKUP] No auto-space! Opening inventory to place manually...");

        // Attach to cart if not already
        if (cartInteraction != null && !cartInteraction.IsAttached)
        {
            cartInteraction.AttachToCart(nearestCart);
        }

        // Open inventory with this item on cursor
        InventoryUI invUI = nearestCart.GetComponent<InventoryUI>();
        invManager.Open();
        if (invUI != null)
        {
            invUI.StartPlacingItem(carriedItem);
        }

        carriedItem = null;
        isCarrying = false;
        RestoreSpeed();
    }

    private void DropItem()
    {
        if (!isCarrying || carriedItem == null) return;

        Debug.Log($"[PICKUP] Dropped {carriedItem.itemName}!");
        SpawnDroppedItem(carriedItem, transform.position + transform.forward * 1.5f);

        carriedItem = null;
        isCarrying = false;
        RestoreSpeed();
    }

    // ======================== SPEED ========================

    private void ApplyCarrySlowdown()
    {
        if (tpc == null || carriedItem == null || !savedSpeeds) return;

        // Calculate slowdown: heavier item = more slowdown
        float weightRatio = Mathf.Clamp01(carriedItem.weight / heavyItemWeight);
        float speedMult = Mathf.Lerp(1f, maxCarrySlowdown, weightRatio);

        tpc.MoveSpeed = baseMoveSpeed * speedMult;
        tpc.SprintSpeed = baseSprintSpeed * speedMult;

        Debug.Log($"[PICKUP] Speed penalty: {speedMult * 100:F0}% (item: {carriedItem.weight:F1}kg)");
    }

    private void RestoreSpeed()
    {
        if (tpc == null || !savedSpeeds) return;
        tpc.MoveSpeed = baseMoveSpeed;
        tpc.SprintSpeed = baseSprintSpeed;
        Debug.Log("[PICKUP] Speed restored to normal.");
    }

    /// <summary>Spawn a world item from an ItemData.</summary>
    public static void SpawnDroppedItem(ItemData data, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Dropped_{data.itemName}";
        obj.transform.position = position + Vector3.up * 0.5f;
        obj.transform.localScale = Vector3.one * 0.4f;

        Object.Destroy(obj.GetComponent<BoxCollider>());
        BoxCollider trigger = obj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = Vector3.one * 2f;

        Renderer rend = obj.GetComponent<Renderer>();
        if (rend != null)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", data.itemColor);
            rend.SetPropertyBlock(block);
        }

        WorldItem worldItem = obj.AddComponent<WorldItem>();
        worldItem.itemData = data;
    }

    // ======================== UI ========================

    private void OnGUI()
    {
        if (isCarrying && carriedItem != null)
            DrawCarryingHUD();

        if (!isCarrying && nearestItem != null && nearestItem.itemData != null)
            DrawPickupPrompt();
    }

    private void DrawCarryingHUD()
    {
        if (carryStyle == null)
        {
            carryStyle = new GUIStyle(GUI.skin.box);
            carryStyle.fontSize = 20;
            carryStyle.alignment = TextAnchor.MiddleCenter;
            carryStyle.normal.textColor = Color.white;
            carryStyle.fontStyle = FontStyle.Bold;
            carryStyle.padding = new RectOffset(20, 20, 10, 10);
        }

        string text = $"Carrying: {carriedItem.itemName} ({carriedItem.width}x{carriedItem.height}, {carriedItem.weight:F1}kg)";

        // Speed penalty info
        float weightRatio = Mathf.Clamp01(carriedItem.weight / heavyItemWeight);
        float speedPct = Mathf.Lerp(1f, maxCarrySlowdown, weightRatio) * 100f;
        string speedInfo = speedPct < 99f ? $"  ⚡{speedPct:F0}% Speed" : "";

        // Check gamepad
        bool hasGamepad = Gamepad.current != null;
        string depositKey = hasGamepad ? "X" : "F";
        string dropKey = hasGamepad ? "B" : "G";
        string controls = $"[{depositKey}] Deposit to Cart    [{dropKey}] Drop{speedInfo}";

        float w = 520;
        float h = 60;
        Rect rect = new Rect((Screen.width - w) / 2f, 20, w, h);

        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.8f, 0.9f);
        GUI.Box(rect, $"{text}\n{controls}", carryStyle);
        GUI.backgroundColor = Color.white;
    }

    private void DrawPickupPrompt()
    {
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box);
            promptStyle.fontSize = 18;
            promptStyle.alignment = TextAnchor.MiddleCenter;
            promptStyle.normal.textColor = Color.white;
            promptStyle.fontStyle = FontStyle.Bold;
            promptStyle.padding = new RectOffset(15, 15, 8, 8);
        }

        bool attached = cartInteraction != null && cartInteraction.IsAttached;
        string itemName = nearestItem.itemData.itemName;
        string weightStr = $"{nearestItem.itemData.weight:F1}kg";
        string sizeStr = $"{nearestItem.itemData.width}x{nearestItem.itemData.height}";

        bool hasGamepad = Gamepad.current != null;
        string pickupKey = hasGamepad ? "X" : "F";
        string cartKey = hasGamepad ? "Y" : "E";

        string action = attached ? $"[{cartKey}] Leave Cart First" : $"[{pickupKey}] Pick Up";
        Color bgColor = attached
            ? new Color(0.4f, 0.2f, 0.1f, 0.9f)
            : new Color(0.1f, 0.4f, 0.1f, 0.9f);

        string text = $"{action}\n{itemName}  ({sizeStr}, {weightStr})";

        float w = 320;
        float h = 55;
        Rect rect = new Rect((Screen.width - w) / 2f, Screen.height - h - 60, w, h);

        GUI.backgroundColor = bgColor;
        GUI.Box(rect, text, promptStyle);
        GUI.backgroundColor = Color.white;
    }
}
