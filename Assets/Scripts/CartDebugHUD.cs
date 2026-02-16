using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Master debug panel built with Unity UI (Canvas).
/// Shows all game stats in collapsible sections on the right side.
/// F1 to toggle the panel. Click section headers to expand/collapse.
/// Attach to the Cart GameObject.
/// </summary>
public class CartDebugHUD : MonoBehaviour
{
    // ─── References (auto-found) ───
    private CartController cart;
    private GridInventory gridInv;
    private InventoryManager invManager;
    private Rigidbody cartRb;
    private NoiseSystem noise;
    private PlayerStateMachine playerState;
    private CartInteraction cartInteraction;
    private StaminaSystem stamina;
    private HealthSystem health;
    private ItemPickup itemPickup;
    private PlayerCrouch crouch;
    private CharacterController playerCC;
    private Transform playerTransform;

    // ─── UI Objects ───
    private Canvas canvas;
    private GameObject panelRoot;
    private RectTransform contentParent;
    private ScrollRect scrollRect;

    // ─── Section tracking ───
    private class Section
    {
        public string title;
        public bool expanded;
        public Button headerBtn;
        public Text headerText;
        public GameObject contentObj;
        public List<Text> labels = new List<Text>();
        public List<Text> values = new List<Text>();
        public List<Image> bars = new List<Image>();
        public List<GameObject> rows = new List<GameObject>();
        public Button actionBtn;
        public Text actionBtnText;
    }

    private Section secPlayer, secHealth, secStamina, secInventory, secCart, secNoise, secZombies, secControls;
    private Text fpsText;
    private Text titleText;

    // ─── Colors ───
    private static readonly Color BG_COLOR = new Color(0.06f, 0.06f, 0.12f, 0.92f);
    private static readonly Color HEADER_BG = new Color(0.12f, 0.15f, 0.22f, 1f);
    private static readonly Color HEADER_BG_OPEN = new Color(0.15f, 0.2f, 0.32f, 1f);
    private static readonly Color ROW_BG = new Color(0.08f, 0.09f, 0.14f, 0.8f);
    private static readonly Color LABEL_COLOR = new Color(0.6f, 0.6f, 0.65f);
    private static readonly Color VALUE_COLOR = new Color(0.95f, 0.95f, 1f);
    private static readonly Color ACCENT = new Color(0.4f, 0.7f, 1f);
    private static readonly Color BAR_BG = new Color(0.15f, 0.15f, 0.2f);

    // Cached font
    private Font _uiFont;
    private Font UIFont
    {
        get
        {
            if (_uiFont == null)
            {
                // Try built-in fonts (name varies by Unity version)
                _uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (_uiFont == null)
                    _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_uiFont == null)
                    _uiFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
                if (_uiFont == null)
                    _uiFont = Font.CreateDynamicFontFromOSFont("Segoe UI", 14);
            }
            return _uiFont;
        }
    }

    private void Awake()
    {
        cart = GetComponent<CartController>();
        gridInv = GetComponent<GridInventory>();
        invManager = GetComponent<InventoryManager>();
        cartRb = GetComponent<Rigidbody>();
        noise = GetComponent<NoiseSystem>();
    }

    private void Start()
    {
        // Find player
        var ci = FindFirstObjectByType<CartInteraction>();
        if (ci != null)
        {
            playerTransform = ci.transform;
            cartInteraction = ci;
            playerState = ci.GetComponent<PlayerStateMachine>();
            stamina = ci.GetComponent<StaminaSystem>();
            health = ci.GetComponent<HealthSystem>();
            itemPickup = ci.GetComponent<ItemPickup>();
            crouch = ci.GetComponent<PlayerCrouch>();
            playerCC = ci.GetComponent<CharacterController>();
        }
        if (stamina == null) stamina = FindFirstObjectByType<StaminaSystem>();
        if (health == null) health = FindFirstObjectByType<HealthSystem>();

        BuildUI();
    }

    private void Update()
    {
        // F1 toggle
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            panelRoot.SetActive(!panelRoot.activeSelf);
        }

        if (!panelRoot.activeSelf) return;

        UpdatePlayerSection();
        UpdateHealthSection();
        UpdateStaminaSection();
        UpdateInventorySection();
        UpdateCartSection();
        UpdateNoiseSection();
        UpdateZombieSection();
        UpdateFPS();
    }

    // ════════════════════════════════════════════════════════════
    //  BUILD THE ENTIRE UI
    // ════════════════════════════════════════════════════════════

    private void BuildUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DebugCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Main panel (right side)
        panelRoot = CreatePanel(canvasObj.transform, "DebugPanel", BG_COLOR);
        RectTransform panelRT = panelRoot.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 1);
        panelRT.offsetMin = new Vector2(-300, 10);   // Left edge = screen right - 300
        panelRT.offsetMax = new Vector2(-10, -10);    // Right edge = screen right - 10

        // Add outline
        var outline = panelRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.5f, 0.8f, 0.3f);
        outline.effectDistance = new Vector2(1, 1);

        // Scroll view inside panel
        GameObject scrollObj = new GameObject("Scroll");
        scrollObj.transform.SetParent(panelRoot.transform, false);
        ScrollRect sr = scrollObj.AddComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;
        Image scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = Color.clear;
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(4, 4);
        scrollRT.offsetMax = new Vector2(-4, -4);

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        Image vpImg = viewport.AddComponent<Image>();
        vpImg.color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;

        // Content container with vertical layout
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        contentParent = content.GetComponent<RectTransform>() ?? content.AddComponent<RectTransform>();
        contentParent.anchorMin = new Vector2(0, 1);
        contentParent.anchorMax = new Vector2(1, 1);
        contentParent.pivot = new Vector2(0.5f, 1);
        contentParent.offsetMin = new Vector2(0, 0);
        contentParent.offsetMax = new Vector2(0, 0);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 2;
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpRT;
        sr.content = contentParent;
        scrollRect = sr;

        // ─── Title ───
        titleText = CreateLabel(contentParent, "DEBUG PANEL  [F1]", 13, ACCENT, FontStyle.Bold, 24);

        // ─── Sections ───
        secPlayer = CreateSection("PLAYER", true, 6);
        secHealth = CreateSection("HEALTH", true, 3, true);  // Has bar
        secStamina = CreateSection("STAMINA", true, 3, true); // Has bar
        secInventory = CreateSection("INVENTORY", true, 5, true);
        secCart = CreateSection("CART", true, 4);
        secNoise = CreateSection("NOISE", true, 3, true);
        secZombies = CreateSection("ZOMBIES", true, 5, false, true); // Has action button
        secControls = CreateSection("CONTROLS", false, 10);

        // ─── FPS ───
        fpsText = CreateLabel(contentParent, "FPS: --", 11, new Color(0.5f, 0.5f, 0.5f), FontStyle.Normal, 18);
    }

    // ════════════════════════════════════════════════════════════
    //  UPDATE SECTIONS
    // ════════════════════════════════════════════════════════════

    private void UpdatePlayerSection()
    {
        if (!secPlayer.expanded) return;
        int i = 0;

        // State
        if (playerState != null)
        {
            string icon = playerState.CurrentState switch
            {
                PlayerStateMachine.PlayerState.FreeRoam => "Free Roam",
                PlayerStateMachine.PlayerState.PushingCart => "Pushing Cart",
                PlayerStateMachine.PlayerState.InventoryOpen => "Inventory",
                _ => "?"
            };
            SetRow(secPlayer, i++, "State", icon);
        }
        else SetRow(secPlayer, i++, "State", "—");

        // Speed
        if (playerCC != null)
        {
            float spd = new Vector3(playerCC.velocity.x, 0, playerCC.velocity.z).magnitude;
            bool sprint = stamina != null && stamina.IsSprinting;
            SetRow(secPlayer, i++, "Speed", $"{spd:F1} m/s" + (sprint ? "  SPRINT" : ""));
        }
        else SetRow(secPlayer, i++, "Speed", "—");

        // Position
        if (playerTransform != null)
        {
            Vector3 p = playerTransform.position;
            SetRow(secPlayer, i++, "Position", $"({p.x:F0}, {p.y:F0}, {p.z:F0})");
        }
        else SetRow(secPlayer, i++, "Position", "—");

        // Cart
        SetRow(secPlayer, i++, "Cart",
            cartInteraction != null ? (cartInteraction.IsAttached ? "Attached" : "Detached") : "—");

        // Crouch
        SetRow(secPlayer, i++, "Crouch",
            crouch != null ? (crouch.IsCrouching ? "Crouching" : "Standing") : "—");

        // Carrying
        if (itemPickup != null && itemPickup.IsCarrying && itemPickup.CarriedItem != null)
            SetRow(secPlayer, i++, "Carrying", $"{itemPickup.CarriedItem.itemName} ({itemPickup.CarriedItem.weight:F1}kg)");
        else
            SetRow(secPlayer, i++, "Carrying", "—");
    }

    private void UpdateHealthSection()
    {
        if (!secHealth.expanded) return;
        if (health != null)
        {
            SetRow(secHealth, 0, "HP", $"{health.CurrentHealth:F0} / {health.MaxHealth:F0}");
            SetRow(secHealth, 1, "Status", health.IsDead ? "DEAD" : health.IsInvincible ? "Invincible" : "Alive");

            // Bar
            if (secHealth.bars.Count > 0)
            {
                Color c = health.HealthPercent > 0.6f ? new Color(0.2f, 0.85f, 0.2f) :
                          health.HealthPercent > 0.3f ? new Color(1f, 0.8f, 0.1f) :
                          new Color(0.9f, 0.15f, 0.1f);
                SetBar(secHealth, 0, health.HealthPercent, c);
            }
        }
        else
        {
            SetRow(secHealth, 0, "HP", "—");
            SetRow(secHealth, 1, "", "No HealthSystem");
        }
    }

    private void UpdateStaminaSection()
    {
        if (!secStamina.expanded) return;
        if (stamina != null)
        {
            SetRow(secStamina, 0, "Stamina", $"{stamina.CurrentStamina:F0} / {stamina.MaxStamina:F0}");

            string st = stamina.IsExhausted ? "EXHAUSTED" :
                        stamina.IsSprinting ? "Draining" :
                        stamina.StaminaPercent < 1f ? "Regen" : "Full";
            SetRow(secStamina, 1, "Status", st);

            Color c = stamina.IsExhausted ? new Color(0.8f, 0.1f, 0.1f) :
                      Color.Lerp(new Color(1f, 0.3f, 0.1f), new Color(0.2f, 0.9f, 0.3f), stamina.StaminaPercent);
            SetBar(secStamina, 0, stamina.StaminaPercent, c);
        }
        else
        {
            SetRow(secStamina, 0, "Stamina", "—");
            SetRow(secStamina, 1, "", "No StaminaSystem");
        }
    }

    private void UpdateInventorySection()
    {
        if (!secInventory.expanded) return;
        if (gridInv != null)
        {
            SetRow(secInventory, 0, "Items", $"{gridInv.ItemCount}");
            int occ = gridInv.OccupiedCells();
            int total = gridInv.GridWidth * gridInv.GridHeight;
            SetRow(secInventory, 1, "Space", $"{occ}/{total} ({gridInv.SpaceFullness * 100:F0}%)");
            SetRow(secInventory, 2, "Weight", $"{gridInv.CurrentWeight:F1} / {gridInv.MaxWeight:F0} kg");
            SetRow(secInventory, 3, "UI", invManager != null ? (invManager.IsOpen ? "Open" : "Closed") : "—");

            float f = gridInv.Fullness;
            SetBar(secInventory, 0, f, Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(1f, 0.2f, 0.3f), f));
        }
        else
        {
            SetRow(secInventory, 0, "", "No Inventory");
            for (int i = 1; i < 4; i++) SetRow(secInventory, i, "", "");
        }
    }

    private void UpdateCartSection()
    {
        if (!secCart.expanded) return;
        if (cart != null)
        {
            SetRow(secCart, 0, "Speed", $"{cart.CurrentSpeed:F1} / {cart.EffectiveMaxSpeed:F1} m/s");

            string mode = cart.IsSneaking ? "Sneaking" :
                          cart.IsSprinting ? "Sprinting" :
                          Mathf.Abs(cart.MoveInput) > 0.01f ? "Moving" : "Idle";
            SetRow(secCart, 1, "Mode", mode);

            SetRow(secCart, 2, "Mass", cartRb != null ? $"{cartRb.mass:F1} kg" : "—");
            SetRow(secCart, 3, "Fullness", $"{cart.CartFullness * 100:F0}%");
        }
        else
        {
            SetRow(secCart, 0, "", "No Cart");
            for (int i = 1; i < 4; i++) SetRow(secCart, i, "", "");
        }
    }

    private void UpdateNoiseSection()
    {
        if (!secNoise.expanded) return;
        if (noise != null)
        {
            SetRow(secNoise, 0, "Cart Noise", $"{noise.CartNoiseRadius:F1} m");
            SetRow(secNoise, 1, "Player Noise", $"{noise.PlayerNoiseRadius:F1} m");

            float t = Mathf.Clamp01(noise.CartNoiseRadius / 40f);
            SetBar(secNoise, 0, t, Color.Lerp(new Color(0.2f, 0.8f, 0.2f), new Color(1f, 0.15f, 0.1f), t));
        }
        else
        {
            SetRow(secNoise, 0, "", "No NoiseSystem");
            SetRow(secNoise, 1, "", "");
        }
    }

    private void UpdateZombieSection()
    {
        if (!secZombies.expanded) return;
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);

        SetRow(secZombies, 0, "Total", $"{zombies.Length}");

        int idle = 0, patrol = 0, chase = 0, attack = 0;
        foreach (var z in zombies)
        {
            switch (z.State)
            {
                case ZombieAI.ZombieState.Idle: idle++; break;
                case ZombieAI.ZombieState.Patrol: patrol++; break;
                case ZombieAI.ZombieState.Chase: chase++; break;
                case ZombieAI.ZombieState.Attack: attack++; break;
            }
        }
        SetRow(secZombies, 1, "Idle", $"{idle}");
        SetRow(secZombies, 2, "Patrol", $"{patrol}");
        SetRow(secZombies, 3, "Chase", $"{chase}");
        SetRow(secZombies, 4, "Attack", $"{attack}");

        // Action button
        if (secZombies.actionBtn != null)
        {
            bool debugOn = zombies.Length > 0 && zombies[0].ShowDebug;
            secZombies.actionBtnText.text = debugOn ? "Hide Gizmos" : "Show Gizmos";
        }
    }

    private void UpdateFPS()
    {
        if (fpsText != null)
            fpsText.text = $"FPS: {(1f / Time.unscaledDeltaTime):F0}";
    }

    // ════════════════════════════════════════════════════════════
    //  UI FACTORY METHODS
    // ════════════════════════════════════════════════════════════

    private Section CreateSection(string title, bool startExpanded, int rowCount,
        bool hasBar = false, bool hasActionBtn = false)
    {
        Section sec = new Section();
        sec.title = title;
        sec.expanded = startExpanded;

        // Header button
        GameObject headerObj = new GameObject($"Header_{title}");
        headerObj.transform.SetParent(contentParent, false);

        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = startExpanded ? HEADER_BG_OPEN : HEADER_BG;

        LayoutElement headerLE = headerObj.AddComponent<LayoutElement>();
        headerLE.preferredHeight = 28;
        headerLE.flexibleWidth = 1;

        Button btn = headerObj.AddComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        btn.colors = cb;
        btn.targetGraphic = headerBg;
        sec.headerBtn = btn;

        // Header text
        GameObject headerTextObj = new GameObject("Text");
        headerTextObj.transform.SetParent(headerObj.transform, false);
        Text ht = headerTextObj.AddComponent<Text>();
        ht.text = (startExpanded ? "  \u25BC  " : "  \u25B6  ") + title;
        ht.font = UIFont;
        ht.fontSize = 13;
        ht.fontStyle = FontStyle.Bold;
        ht.color = ACCENT;
        ht.alignment = TextAnchor.MiddleLeft;
        RectTransform htRT = headerTextObj.GetComponent<RectTransform>();
        htRT.anchorMin = Vector2.zero;
        htRT.anchorMax = Vector2.one;
        htRT.offsetMin = Vector2.zero;
        htRT.offsetMax = Vector2.zero;
        sec.headerText = ht;

        // Content container
        GameObject contentObj = new GameObject($"Content_{title}");
        contentObj.transform.SetParent(contentParent, false);
        VerticalLayoutGroup clg = contentObj.AddComponent<VerticalLayoutGroup>();
        clg.spacing = 1;
        clg.padding = new RectOffset(8, 8, 2, 4);
        clg.childForceExpandWidth = true;
        clg.childForceExpandHeight = false;
        clg.childControlWidth = true;
        clg.childControlHeight = true;

        ContentSizeFitter ccsf = contentObj.AddComponent<ContentSizeFitter>();
        ccsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sec.contentObj = contentObj;
        contentObj.SetActive(startExpanded);

        // Rows
        for (int i = 0; i < rowCount; i++)
        {
            CreateRow(contentObj.transform, sec);
        }

        // Bar (optional)
        if (hasBar)
        {
            CreateBar(contentObj.transform, sec);
        }

        // Action button (optional)
        if (hasActionBtn)
        {
            CreateActionButton(contentObj.transform, sec);
        }

        // Wire toggle
        Image bgRef = headerBg;
        Text textRef = ht;
        btn.onClick.AddListener(() => {
            sec.expanded = !sec.expanded;
            sec.contentObj.SetActive(sec.expanded);
            bgRef.color = sec.expanded ? HEADER_BG_OPEN : HEADER_BG;
            textRef.text = (sec.expanded ? "  \u25BC  " : "  \u25B6  ") + sec.title;
        });

        // Controls section - fill static data
        if (title == "CONTROLS" && startExpanded == false)
        {
            FillControlsSection(sec);
        }

        return sec;
    }

    private void FillControlsSection(Section sec)
    {
        string[,] controls = {
            { "E / Y", "Cart attach/detach" },
            { "F / X", "Pick up / Deposit" },
            { "G / B", "Drop item" },
            { "C / L3", "Crouch toggle" },
            { "Tab / Select", "Inventory" },
            { "Shift / RT", "Sprint" },
            { "Ctrl / LT", "Sneak (cart)" },
            { "T / D-Up", "Camera mode" },
            { "R / Y", "Rotate (inv)" },
            { "Q / X", "Drop (inv)" },
        };

        for (int i = 0; i < Mathf.Min(controls.GetLength(0), sec.labels.Count); i++)
        {
            sec.labels[i].text = controls[i, 0];
            sec.values[i].text = controls[i, 1];
            sec.labels[i].color = new Color(0.9f, 0.8f, 0.4f);
        }
    }

    private void CreateRow(Transform parent, Section sec)
    {
        GameObject row = new GameObject("Row");
        row.transform.SetParent(parent, false);

        Image rowBg = row.AddComponent<Image>();
        rowBg.color = ROW_BG;

        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = 20;
        rowLE.flexibleWidth = 1;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 4;
        hlg.padding = new RectOffset(6, 6, 0, 0);
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        Text labelText = labelObj.AddComponent<Text>();
        labelText.text = "";
        labelText.font = UIFont;
        labelText.fontSize = 12;
        labelText.color = LABEL_COLOR;
        labelText.alignment = TextAnchor.MiddleLeft;
        LayoutElement lLE = labelObj.AddComponent<LayoutElement>();
        lLE.flexibleWidth = 0.45f;

        // Value
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        Text valueText = valueObj.AddComponent<Text>();
        valueText.text = "";
        valueText.font = UIFont;
        valueText.fontSize = 12;
        valueText.fontStyle = FontStyle.Bold;
        valueText.color = VALUE_COLOR;
        valueText.alignment = TextAnchor.MiddleRight;
        LayoutElement vLE = valueObj.AddComponent<LayoutElement>();
        vLE.flexibleWidth = 0.55f;

        sec.labels.Add(labelText);
        sec.values.Add(valueText);
        sec.rows.Add(row);
    }

    private void CreateBar(Transform parent, Section sec)
    {
        GameObject barRow = new GameObject("Bar");
        barRow.transform.SetParent(parent, false);

        LayoutElement barLE = barRow.AddComponent<LayoutElement>();
        barLE.preferredHeight = 8;
        barLE.flexibleWidth = 1;

        // Background
        Image barBg = barRow.AddComponent<Image>();
        barBg.color = BAR_BG;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barRow.transform, false);
        Image fill = fillObj.AddComponent<Image>();
        fill.color = Color.green;
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);  // Start at 0 width
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        sec.bars.Add(fill);
    }

    private void CreateActionButton(Transform parent, Section sec)
    {
        GameObject btnObj = new GameObject("ActionBtn");
        btnObj.transform.SetParent(parent, false);

        Image btnBg = btnObj.AddComponent<Image>();
        btnBg.color = new Color(0.15f, 0.25f, 0.4f);

        LayoutElement btnLE = btnObj.AddComponent<LayoutElement>();
        btnLE.preferredHeight = 24;
        btnLE.flexibleWidth = 1;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnBg;
        sec.actionBtn = btn;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.text = "Toggle Gizmos";
        t.font = UIFont;
        t.fontSize = 12;
        t.fontStyle = FontStyle.Bold;
        t.color = ACCENT;
        t.alignment = TextAnchor.MiddleCenter;
        RectTransform tRT = textObj.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero;
        tRT.anchorMax = Vector2.one;
        tRT.offsetMin = Vector2.zero;
        tRT.offsetMax = Vector2.zero;
        sec.actionBtnText = t;

        btn.onClick.AddListener(() => {
            ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            bool current = zombies.Length > 0 && zombies[0].ShowDebug;
            bool newVal = !current;
            foreach (var z in zombies) z.ShowDebug = newVal;
            if (noise != null) noise.ShowDebug = newVal;
        });
    }

    private Text CreateLabel(RectTransform parent, string text, int fontSize, Color color,
        FontStyle style, float height)
    {
        GameObject obj = new GameObject("Label");
        obj.transform.SetParent(parent, false);

        Text t = obj.AddComponent<Text>();
        t.text = text;
        t.font = UIFont;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAnchor.MiddleLeft;

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;

        return t;
    }

    private GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    // ════════════════════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════════════════════

    private void SetRow(Section sec, int index, string label, string value)
    {
        if (index < sec.labels.Count) sec.labels[index].text = label;
        if (index < sec.values.Count) sec.values[index].text = value;
    }

    private void SetBar(Section sec, int index, float fill, Color color)
    {
        if (index >= sec.bars.Count) return;
        Image bar = sec.bars[index];
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(Mathf.Clamp01(fill), 1);
        bar.color = color;
    }
}
