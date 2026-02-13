using UnityEngine;

/// <summary>
/// On-screen debug HUD showing cart + player stats in real-time.
/// Attach to the Cart GameObject (same as CartController + CartInventory).
/// </summary>
public class CartDebugHUD : MonoBehaviour
{
    private CartController cart;
    private CartInventory inventory;
    private GridInventory gridInv;
    private InventoryManager invManager;
    private Rigidbody rb;

    // Player references (auto-found)
    private PlayerStateMachine playerState;
    private PlayerMotor playerMotor;
    private CartInteraction cartInteraction;
    private StaminaSystem stamina;
    private CharacterController playerCC;
    private Transform playerTransform;

    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle tierStyle;
    private GUIStyle controlsStyle;
    private bool stylesInitialized;

    private void Awake()
    {
        cart = GetComponent<CartController>();
        inventory = GetComponent<CartInventory>();
        gridInv = GetComponent<GridInventory>();
        invManager = GetComponent<InventoryManager>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Auto-find player
        playerMotor = FindFirstObjectByType<PlayerMotor>();
        if (playerMotor != null)
        {
            playerState = playerMotor.GetComponent<PlayerStateMachine>();
            cartInteraction = playerMotor.GetComponent<CartInteraction>();
            stamina = playerMotor.GetComponent<StaminaSystem>();
            playerCC = playerMotor.GetComponent<CharacterController>();
            playerTransform = playerMotor.transform;
        }
        // Fallback: find on any object
        if (stamina == null)
            stamina = FindFirstObjectByType<StaminaSystem>();
        if (cartInteraction == null)
            cartInteraction = FindFirstObjectByType<CartInteraction>();
        if (playerState == null)
            playerState = FindFirstObjectByType<PlayerStateMachine>();
        if (playerCC == null)
            playerCC = FindFirstObjectByType<CharacterController>();
        if (playerCC != null && playerTransform == null)
            playerTransform = playerCC.transform;
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };

        tierStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        controlsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = new Color(1f, 1f, 1f, 0.6f) }
        };

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        float panelWidth = 320;
        float panelX = Screen.width - panelWidth - 20;
        float y = 20;
        float lineHeight = 22;

        // --- Background panel ---
        GUI.Box(new Rect(panelX - 10, y - 10, panelWidth + 20, 680), "");

        // --- Player State ---
        GUI.Label(new Rect(panelX, y, panelWidth, 30), "👤 PLAYER", headerStyle);
        y += 28;

        if (playerState != null)
        {
            string stateIcon = playerState.CurrentState switch
            {
                PlayerStateMachine.PlayerState.FreeRoam => "🚶",
                PlayerStateMachine.PlayerState.PushingCart => "🛒",
                PlayerStateMachine.PlayerState.InventoryOpen => "📦",
                _ => "?"
            };
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"State: {stateIcon} {playerState.CurrentState}", labelStyle);
            y += lineHeight;
        }

        // Player speed from CharacterController
        if (playerCC != null)
        {
            Vector3 vel = playerCC.velocity;
            float speed = new Vector3(vel.x, 0, vel.z).magnitude;
            bool sprinting = stamina != null && stamina.IsSprinting;
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Speed: {speed:F1} m/s {(sprinting ? "💨 SPRINT" : "")}", labelStyle);
            y += lineHeight;
        }
        else if (playerMotor != null)
        {
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Speed: {playerMotor.CurrentSpeed:F1} m/s {(playerMotor.IsSprinting ? "💨 SPRINT" : "")}", labelStyle);
            y += lineHeight;
        }

        // Player position
        if (playerTransform != null)
        {
            Vector3 pos = playerTransform.position;
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Pos: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})", labelStyle);
            y += lineHeight;
        }

        if (cartInteraction != null)
        {
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Cart: {(cartInteraction.IsAttached ? "✅ Attached" : "— Not attached")}", labelStyle);
            y += lineHeight;
        }

        y += 10;

        // --- Stamina ---
        GUI.Label(new Rect(panelX, y, panelWidth, 30), "⚡ STAMINA", headerStyle);
        y += 28;

        if (stamina != null)
        {
            float stPct = stamina.StaminaPercent;
            string stState = stamina.IsExhausted ? "💀 EXHAUSTED" :
                             stamina.IsSprinting ? "💨 DRAINING" :
                             stPct < 1f ? "♻️ Regen" : "✅ Full";

            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Stamina: {stamina.CurrentStamina:F0}/{stamina.MaxStamina:F0}  {stState}", labelStyle);
            y += lineHeight;

            // Stamina color
            Color stColor;
            if (stamina.IsExhausted)
                stColor = new Color(0.5f, 0f, 0f);
            else if (stPct > 0.5f)
                stColor = Color.Lerp(Color.yellow, Color.green, (stPct - 0.5f) * 2f);
            else
                stColor = Color.Lerp(Color.red, Color.yellow, stPct * 2f);

            DrawBar(panelX, y, panelWidth - 20, 16, stPct, stColor);
            y += 24;
        }
        else
        {
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "— No StaminaSystem found", labelStyle);
            y += lineHeight;
        }

        y += 10;

        // --- Grid Inventory ---
        GUI.Label(new Rect(panelX, y, panelWidth, 30), "📦 GRID INVENTORY", headerStyle);
        y += 28;

        if (gridInv != null)
        {
            int occupied = gridInv.OccupiedCells();
            int total = gridInv.GridWidth * gridInv.GridHeight;
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Space: {occupied}/{total} cells ({gridInv.SpaceFullness * 100:F0}%)", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Weight: {gridInv.CurrentWeight:F1}/{gridInv.MaxWeight:F0} kg", labelStyle);
            y += lineHeight;

            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Items: {gridInv.ItemCount}", labelStyle);
            y += lineHeight;

            // Space bar
            DrawBar(panelX, y, panelWidth - 20, 14, gridInv.SpaceFullness,
                Color.Lerp(Color.cyan, Color.magenta, gridInv.SpaceFullness));
            y += 20;

            if (invManager != null)
            {
                GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                    $"UI: {(invManager.IsOpen ? "📦 OPEN (Tab to close)" : "— Closed (Tab to open)")}", labelStyle);
                y += lineHeight;
            }
        }
        else
        {
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "— No GridInventory", labelStyle);
            y += lineHeight;
        }

        y += 10;

        // --- Cart Header ---
        GUI.Label(new Rect(panelX, y, panelWidth, 30), "🛒 CART", headerStyle);
        y += 28;

        if (inventory != null)
        {
            // --- Fullness bar ---
            float fullness = inventory.Fullness;
            Color barColor = GetTierColor(inventory.Tier);

            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Fullness: {fullness * 100:F0}%  ({inventory.CurrentWeight:F1}/{50f} kg)", labelStyle);
            y += lineHeight;

            // Draw the bar
            DrawBar(panelX, y, panelWidth - 20, 20, fullness, barColor);
            y += 28;

            // --- Tier display ---
            tierStyle.normal.textColor = barColor;
            string tierText = inventory.Tier switch
            {
                CartInventory.FullnessTier.Empty => "🥷 STEALTH MODE",
                CartInventory.FullnessTier.Light => "👟 LIGHT LOAD",
                CartInventory.FullnessTier.Half => "⚠️ SQUEAKY...",
                CartInventory.FullnessTier.Full => "🔔 DINNER BELL!!!",
                _ => "???"
            };
            GUI.Label(new Rect(panelX, y, panelWidth, 30), tierText, tierStyle);
            y += 30;

            // --- Item count ---
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Items: {inventory.ItemCount}", labelStyle);
            y += lineHeight;
        }

        if (cart != null)
        {
            // --- Speed ---
            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Cart Speed: {cart.CurrentSpeed:F1} / {cart.EffectiveMaxSpeed:F1} m/s", labelStyle);
            y += lineHeight;

            // --- Mass ---
            if (rb != null)
            {
                GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                    $"Mass: {rb.mass:F1} kg", labelStyle);
                y += lineHeight;
            }

            // --- Movement state ---
            string moveState = "Idle";
            if (cart.IsSneaking) moveState = "🤫 SNEAKING";
            else if (cart.IsSprinting) moveState = "💨 SPRINTING";
            else if (Mathf.Abs(cart.MoveInput) > 0.01f) moveState = "🚶 Moving";

            GUI.Label(new Rect(panelX, y, panelWidth, lineHeight),
                $"Cart State: {moveState}", labelStyle);
            y += lineHeight + 10;
        }

        // --- Controls help ---
        GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "─── Controls ───", labelStyle);
        y += lineHeight;
        GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "WASD = Move    Shift = Sprint", controlsStyle);
        y += 18;
        GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "E/🎮Y = Grab Cart   Ctrl = Sneak", controlsStyle);
        y += 18;
        GUI.Label(new Rect(panelX, y, panelWidth, lineHeight), "C/D-Up = Camera   🎮RT=Sprint LT=Sneak", controlsStyle);
    }

    private void DrawBar(float x, float y, float width, float height, float fill, Color color)
    {
        Color oldColor = GUI.color;
        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);

        GUI.color = color;
        GUI.DrawTexture(new Rect(x, y, width * fill, height), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Box(new Rect(x, y, width, height), "");

        GUI.color = oldColor;
    }

    private Color GetTierColor(CartInventory.FullnessTier tier)
    {
        return tier switch
        {
            CartInventory.FullnessTier.Empty => new Color(0.3f, 0.9f, 0.4f),
            CartInventory.FullnessTier.Light => new Color(0.9f, 0.9f, 0.3f),
            CartInventory.FullnessTier.Half => new Color(1f, 0.6f, 0.2f),
            CartInventory.FullnessTier.Full => new Color(1f, 0.2f, 0.2f),
            _ => Color.white
        };
    }
}
