using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Grid inventory UI with dual input support:
///   Mouse: click to pick/place, drag items, R rotate, Q drop, right-click cancel
///   Gamepad: D-Pad/Stick to move cursor, A pick/place, Y rotate, X drop, B cancel
/// Auto-detects which input was used last and shows appropriate cursor/hints.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Grid Visual")]
    [SerializeField] private int cellSize = 60;
    [SerializeField] private Color emptyCellColor = new Color(0.12f, 0.14f, 0.18f, 0.95f);
    [SerializeField] private Color occupiedCellColor = new Color(0.22f, 0.24f, 0.28f, 0.95f);
    [SerializeField] private Color validHoverColor = new Color(0.1f, 0.8f, 0.3f, 0.6f);
    [SerializeField] private Color invalidHoverColor = new Color(0.95f, 0.15f, 0.15f, 0.6f);
    [SerializeField] private Color gridLineColor = new Color(0.25f, 0.28f, 0.35f, 1f);
    [SerializeField] private Color cursorColor = new Color(1f, 0.9f, 0.3f, 0.9f);

    [Header("Gamepad Cursor")]
    [SerializeField] private float stickRepeatDelay = 0.18f;
    [SerializeField] private float stickDeadzone = 0.4f;

    // References
    private GridInventory inventory;
    private Canvas canvas;
    private RectTransform gridPanel;
    private RectTransform weightBarFill;
    private Text weightText;
    private Text titleText;
    private Text hintText;
    private Image[,] cellImages;
    private List<RectTransform> itemVisuals = new List<RectTransform>();

    // Drag state
    private PlacedItem draggingItem;
    private bool isDragging;
    private bool dragRotated;
    private ItemData dragItemData;
    private RectTransform dragGhost;
    private int hoverGridX, hoverGridY;

    // Gamepad cursor
    private int cursorX, cursorY;
    private bool usingGamepad;
    private float stickRepeatTimer;
    private Vector2 lastStickDir;
    private RectTransform cursorVisual;      // Yellow border cursor
    private Image cursorImage;

    // Root objects
    private GameObject canvasObj;
    private GameObject panel;

    public bool IsOpen => canvasObj != null && canvasObj.activeSelf;

    /// <summary>Start placing an item (deposit from hand).</summary>
    public void StartPlacingItem(ItemData item)
    {
        dragItemData = item;
        dragRotated = false;
        draggingItem = null;
        isDragging = true;
        UpdateDragGhostSize();
        if (dragGhost != null) dragGhost.gameObject.SetActive(true);
    }

    public void Initialize(GridInventory inv)
    {
        inventory = inv;
        CreateUI();
        inv.OnInventoryChanged += RefreshGrid;
        RefreshGrid();
        Close();
    }

    // ======================== UI CREATION ========================

    private void CreateUI()
    {
        // Canvas
        canvasObj = new GameObject("InventoryCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Dark overlay
        GameObject overlay = CreateImage(canvasObj.transform, "Overlay",
            new Color(0, 0, 0, 0.7f));
        StretchFill(overlay.GetComponent<RectTransform>());

        // Main panel (centered)
        int panelW = inventory.GridWidth * cellSize + 60;
        int panelH = inventory.GridHeight * cellSize + 140;
        panel = CreateImage(canvasObj.transform, "InventoryPanel",
            new Color(0.06f, 0.07f, 0.1f, 0.97f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(panelW, panelH);

        // Panel border
        GameObject borderObj = CreateImage(panel.transform, "PanelBorder",
            new Color(0.3f, 0.5f, 0.9f, 0.4f));
        StretchFill(borderObj.GetComponent<RectTransform>());
        GameObject innerPanel = CreateImage(panel.transform, "InnerPanel",
            new Color(0.06f, 0.07f, 0.1f, 0.97f));
        RectTransform innerRect = innerPanel.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(2, 2);
        innerRect.offsetMax = new Vector2(-2, -2);

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(panel.transform, false);
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "CART INVENTORY";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 26;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.7f, 0.85f, 1f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -8);
        titleRect.sizeDelta = new Vector2(0, 40);
        Outline titleOutline = titleObj.AddComponent<Outline>();
        titleOutline.effectColor = new Color(0.2f, 0.3f, 0.6f, 0.8f);
        titleOutline.effectDistance = new Vector2(1, -1);

        // Grid container
        GameObject gridObj = new GameObject("Grid");
        gridObj.transform.SetParent(panel.transform, false);
        gridPanel = gridObj.AddComponent<RectTransform>();
        gridPanel.anchorMin = new Vector2(0.5f, 0.5f);
        gridPanel.anchorMax = new Vector2(0.5f, 0.5f);
        gridPanel.pivot = new Vector2(0.5f, 0.5f);
        gridPanel.anchoredPosition = new Vector2(0, 10);
        gridPanel.sizeDelta = new Vector2(inventory.GridWidth * cellSize,
                                           inventory.GridHeight * cellSize);

        // Grid background (visible grid lines)
        GameObject gridBg = CreateImage(gridObj.transform, "GridBg", gridLineColor);
        StretchFill(gridBg.GetComponent<RectTransform>());

        // Create grid cells
        cellImages = new Image[inventory.GridWidth, inventory.GridHeight];
        for (int y = 0; y < inventory.GridHeight; y++)
        {
            for (int x = 0; x < inventory.GridWidth; x++)
            {
                GameObject cell = CreateImage(gridPanel, $"Cell_{x}_{y}", emptyCellColor);
                RectTransform cellRect = cell.GetComponent<RectTransform>();
                cellRect.anchorMin = Vector2.zero;
                cellRect.anchorMax = Vector2.zero;
                cellRect.pivot = new Vector2(0, 1);
                cellRect.anchoredPosition = new Vector2(
                    x * cellSize + 1,
                    -y * cellSize + inventory.GridHeight * cellSize - 1);
                cellRect.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);
                cellImages[x, y] = cell.GetComponent<Image>();
            }
        }

        // ── Gamepad cursor (yellow border, rendered on top of cells) ──
        GameObject cursorObj = new GameObject("GamepadCursor");
        cursorObj.transform.SetParent(gridPanel, false);
        cursorImage = cursorObj.AddComponent<Image>();
        cursorImage.color = cursorColor;
        cursorImage.raycastTarget = false;
        cursorVisual = cursorObj.GetComponent<RectTransform>();
        cursorVisual.anchorMin = Vector2.zero;
        cursorVisual.anchorMax = Vector2.zero;
        cursorVisual.pivot = new Vector2(0, 1);
        cursorVisual.sizeDelta = new Vector2(cellSize, cellSize);
        // Create inner cutout to make it a border
        GameObject cursorInner = CreateImage(cursorObj.transform, "CursorInner",
            new Color(0, 0, 0, 0));
        cursorInner.GetComponent<Image>().raycastTarget = false;
        RectTransform innerCursorRect = cursorInner.GetComponent<RectTransform>();
        innerCursorRect.anchorMin = Vector2.zero;
        innerCursorRect.anchorMax = Vector2.one;
        innerCursorRect.offsetMin = new Vector2(3, 3);
        innerCursorRect.offsetMax = new Vector2(-3, -3);
        cursorVisual.gameObject.SetActive(false);

        // Weight bar background
        GameObject weightBg = CreateImage(panel.transform, "WeightBarBg",
            new Color(0.1f, 0.1f, 0.1f, 0.9f));
        RectTransform wbRect = weightBg.GetComponent<RectTransform>();
        wbRect.anchorMin = new Vector2(0, 0);
        wbRect.anchorMax = new Vector2(1, 0);
        wbRect.pivot = new Vector2(0.5f, 0);
        wbRect.anchoredPosition = new Vector2(0, 10);
        wbRect.sizeDelta = new Vector2(-40, 25);

        // Weight bar fill
        GameObject weightFill = CreateImage(weightBg.transform, "WeightBarFill",
            new Color(0.2f, 0.7f, 0.3f, 1f));
        weightBarFill = weightFill.GetComponent<RectTransform>();
        weightBarFill.anchorMin = Vector2.zero;
        weightBarFill.anchorMax = new Vector2(0, 1);
        weightBarFill.pivot = new Vector2(0, 0.5f);
        weightBarFill.offsetMin = new Vector2(2, 2);
        weightBarFill.offsetMax = new Vector2(0, -2);

        // Weight text
        GameObject wtObj = new GameObject("WeightText");
        wtObj.transform.SetParent(weightBg.transform, false);
        weightText = wtObj.AddComponent<Text>();
        weightText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        weightText.fontSize = 14;
        weightText.alignment = TextAnchor.MiddleCenter;
        weightText.color = Color.white;
        StretchFill(wtObj.GetComponent<RectTransform>());

        // Controls hint (dynamic — updates based on input mode)
        GameObject hintObj = new GameObject("Hint");
        hintObj.transform.SetParent(panel.transform, false);
        hintText = hintObj.AddComponent<Text>();
        hintText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintText.fontSize = 13;
        hintText.alignment = TextAnchor.MiddleCenter;
        hintText.color = new Color(0.6f, 0.7f, 0.9f, 0.7f);
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintRect.anchoredPosition = new Vector2(0, -15);
        hintRect.sizeDelta = new Vector2(0, 22);
        UpdateHintText();

        // Drag ghost (hidden by default)
        GameObject ghostObj = CreateImage(canvasObj.transform, "DragGhost",
            new Color(1, 1, 1, 0.4f));
        dragGhost = ghostObj.GetComponent<RectTransform>();
        dragGhost.gameObject.SetActive(false);
    }

    // ======================== OPEN / CLOSE ========================

    public void Open()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(true);
            cursorX = 0;
            cursorY = 0;
            stickRepeatTimer = 0;
            RefreshGrid();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            UpdateHintText();
        }
    }

    public void Close()
    {
        if (canvasObj != null)
        {
            canvasObj.SetActive(false);
            if (isDragging && dragItemData != null)
            {
                inventory.AutoPlace(dragItemData);
            }
            isDragging = false;
            draggingItem = null;
            dragItemData = null;
            if (dragGhost != null)
                dragGhost.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ======================== UPDATE ========================

    private void Update()
    {
        if (!IsOpen || inventory == null) return;

        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // ─── Detect input mode switch ───
        DetectInputMode(mouse, gamepad);

        if (usingGamepad)
        {
            UpdateGamepad(gamepad, keyboard);
        }
        else
        {
            UpdateMouse(mouse, keyboard);
        }

        // Update visuals
        UpdateCursorVisual();
        UpdateHoverHighlight();
        UpdateHintText();
    }

    // ─────────── INPUT MODE DETECTION ───────────

    private void DetectInputMode(Mouse mouse, Gamepad gamepad)
    {
        // Switch to gamepad if any gamepad input detected
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.magnitude > stickDeadzone ||
                gamepad.dpad.ReadValue().magnitude > 0.1f ||
                gamepad.buttonSouth.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame ||
                gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.buttonEast.wasPressedThisFrame)
            {
                if (!usingGamepad)
                {
                    usingGamepad = true;
                    // Snap cursor to grid center on first switch
                    cursorX = Mathf.Clamp(cursorX, 0, inventory.GridWidth - 1);
                    cursorY = Mathf.Clamp(cursorY, 0, inventory.GridHeight - 1);
                    Cursor.visible = false;
                }
            }
        }

        // Switch to mouse if mouse moved or clicked
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            if (delta.magnitude > 1f ||
                mouse.leftButton.wasPressedThisFrame ||
                mouse.rightButton.wasPressedThisFrame)
            {
                if (usingGamepad)
                {
                    usingGamepad = false;
                    Cursor.visible = true;
                }
            }
        }
    }

    // ─────────── GAMEPAD INPUT ───────────

    private void UpdateGamepad(Gamepad gamepad, Keyboard keyboard)
    {
        if (gamepad == null) return;

        // ── D-Pad / Left Stick navigation ──
        HandleGamepadNavigation(gamepad);

        // Use cursor position as hover position
        hoverGridX = cursorX;
        hoverGridY = cursorY;

        // ── A / Cross = pick up / place ──
        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            HandleAction();
        }

        // ── Y / Triangle = rotate ──
        if (gamepad.buttonNorth.wasPressedThisFrame && isDragging
            && dragItemData != null && dragItemData.canRotate)
        {
            dragRotated = !dragRotated;
            UpdateDragGhostSize();
        }

        // ── X / Square = drop item to ground ──
        if (gamepad.buttonWest.wasPressedThisFrame && isDragging)
        {
            DropDraggedItem();
        }

        // ── B / Circle = cancel drag ──
        if (gamepad.buttonEast.wasPressedThisFrame)
        {
            if (isDragging)
                CancelDrag();
        }

        // Keyboard R also works for rotate
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && isDragging
            && dragItemData != null && dragItemData.canRotate)
        {
            dragRotated = !dragRotated;
            UpdateDragGhostSize();
        }
    }

    private void HandleGamepadNavigation(Gamepad gamepad)
    {
        // D-Pad (immediate, one press = one cell)
        Vector2 dpad = gamepad.dpad.ReadValue();
        // Left stick (with repeat delay)
        Vector2 stick = gamepad.leftStick.ReadValue();

        int moveX = 0, moveY = 0;

        // D-Pad takes priority (instant)
        if (Mathf.Abs(dpad.x) > 0.1f || Mathf.Abs(dpad.y) > 0.1f)
        {
            // Only move on initial press
            Vector2 currentDir = new Vector2(
                dpad.x > 0.1f ? 1 : (dpad.x < -0.1f ? -1 : 0),
                dpad.y > 0.1f ? 1 : (dpad.y < -0.1f ? -1 : 0));

            if (currentDir != lastStickDir)
            {
                moveX = (int)currentDir.x;
                moveY = -(int)currentDir.y; // Invert Y (up = -Y in grid)
                stickRepeatTimer = stickRepeatDelay * 2f; // Longer initial delay
            }
            else
            {
                stickRepeatTimer -= Time.deltaTime;
                if (stickRepeatTimer <= 0)
                {
                    moveX = (int)currentDir.x;
                    moveY = -(int)currentDir.y;
                    stickRepeatTimer = stickRepeatDelay;
                }
            }
            lastStickDir = currentDir;
        }
        else if (stick.magnitude > stickDeadzone)
        {
            Vector2 currentDir = new Vector2(
                stick.x > stickDeadzone ? 1 : (stick.x < -stickDeadzone ? -1 : 0),
                stick.y > stickDeadzone ? 1 : (stick.y < -stickDeadzone ? -1 : 0));

            if (currentDir != lastStickDir)
            {
                moveX = (int)currentDir.x;
                moveY = -(int)currentDir.y;
                stickRepeatTimer = stickRepeatDelay * 2f;
            }
            else
            {
                stickRepeatTimer -= Time.deltaTime;
                if (stickRepeatTimer <= 0)
                {
                    moveX = (int)currentDir.x;
                    moveY = -(int)currentDir.y;
                    stickRepeatTimer = stickRepeatDelay;
                }
            }
            lastStickDir = currentDir;
        }
        else
        {
            lastStickDir = Vector2.zero;
            stickRepeatTimer = 0;
        }

        // Apply movement
        if (moveX != 0 || moveY != 0)
        {
            cursorX = Mathf.Clamp(cursorX + moveX, 0, inventory.GridWidth - 1);
            cursorY = Mathf.Clamp(cursorY + moveY, 0, inventory.GridHeight - 1);
        }
    }

    // ─────────── MOUSE INPUT ───────────

    private void UpdateMouse(Mouse mouse, Keyboard keyboard)
    {
        if (mouse == null) return;

        // Get mouse position relative to grid
        UpdateMouseHoverPosition(mouse);

        // R to rotate while dragging
        if (isDragging && keyboard != null && keyboard.rKey.wasPressedThisFrame
            && dragItemData != null && dragItemData.canRotate)
        {
            dragRotated = !dragRotated;
            UpdateDragGhostSize();
        }

        // Left-click = pick/place
        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleAction();
        }

        // Right-click = cancel drag
        if (mouse.rightButton.wasPressedThisFrame && isDragging)
        {
            CancelDrag();
        }

        // Q to drop
        if (isDragging && keyboard != null && keyboard.qKey.wasPressedThisFrame)
        {
            DropDraggedItem();
        }

        // Update drag ghost position to follow mouse
        if (dragGhost != null && isDragging)
        {
            dragGhost.position = mouse.position.ReadValue();
        }
    }

    private void UpdateMouseHoverPosition(Mouse mouse)
    {
        if (gridPanel == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gridPanel, mousePos, null, out localPoint);

        float halfW = gridPanel.sizeDelta.x * 0.5f;
        float halfH = gridPanel.sizeDelta.y * 0.5f;

        hoverGridX = Mathf.FloorToInt((localPoint.x + halfW) / cellSize);
        hoverGridY = Mathf.FloorToInt((halfH - localPoint.y) / cellSize);
    }

    // ─────────── SHARED ACTIONS ───────────

    /// <summary>Pick up or place — works for both mouse click and gamepad A.</summary>
    private void HandleAction()
    {
        bool inGrid = hoverGridX >= 0 && hoverGridX < inventory.GridWidth &&
                      hoverGridY >= 0 && hoverGridY < inventory.GridHeight;

        if (isDragging)
        {
            if (inGrid && dragItemData != null)
            {
                if (inventory.CanFit(dragItemData, hoverGridX, hoverGridY, dragRotated))
                {
                    inventory.PlaceItem(dragItemData, hoverGridX, hoverGridY, dragRotated);
                    StopDrag();
                }
            }
            else if (!inGrid && dragItemData != null && !usingGamepad)
            {
                // Mouse clicked outside grid → drop on ground
                DropDraggedItem();
            }
        }
        else
        {
            if (inGrid)
            {
                PlacedItem item = inventory.GetPlacedItemAt(hoverGridX, hoverGridY);
                if (item != null)
                {
                    dragItemData = item.data;
                    dragRotated = item.isRotated;
                    draggingItem = item;
                    inventory.RemoveItem(item);
                    isDragging = true;
                    UpdateDragGhostSize();
                    if (dragGhost != null) dragGhost.gameObject.SetActive(true);
                }
            }
        }
    }

    private void CancelDrag()
    {
        if (dragItemData != null)
        {
            inventory.AutoPlace(dragItemData);
        }
        StopDrag();
    }

    private void StopDrag()
    {
        isDragging = false;
        draggingItem = null;
        dragItemData = null;
        if (dragGhost != null)
            dragGhost.gameObject.SetActive(false);
        RefreshGrid();
    }

    private void DropDraggedItem()
    {
        if (dragItemData == null) { StopDrag(); return; }

        Vector3 dropPos = transform.position + transform.right * 2f + Vector3.up * 0.5f;
        ItemPickup.SpawnDroppedItem(dragItemData, dropPos);
        Debug.Log($"[INV] Dropped {dragItemData.itemName} on the ground!");
        StopDrag();
    }

    // ─────────── VISUALS ───────────

    private void UpdateDragGhostSize()
    {
        if (dragGhost == null || dragItemData == null) return;
        int w = dragItemData.GetWidth(dragRotated);
        int h = dragItemData.GetHeight(dragRotated);
        dragGhost.sizeDelta = new Vector2(w * cellSize, h * cellSize);
        dragGhost.GetComponent<Image>().color = dragItemData.itemColor * new Color(1, 1, 1, 0.5f);
    }

    private void UpdateCursorVisual()
    {
        if (cursorVisual == null) return;

        if (usingGamepad)
        {
            cursorVisual.gameObject.SetActive(true);

            // Size: if dragging, show item footprint; otherwise 1x1
            int w = 1, h = 1;
            if (isDragging && dragItemData != null)
            {
                w = dragItemData.GetWidth(dragRotated);
                h = dragItemData.GetHeight(dragRotated);
            }

            cursorVisual.sizeDelta = new Vector2(w * cellSize, h * cellSize);
            cursorVisual.anchoredPosition = new Vector2(
                cursorX * cellSize,
                -cursorY * cellSize + inventory.GridHeight * cellSize);

            // Pulse the cursor alpha
            float pulse = Mathf.Sin(Time.time * 4f) * 0.2f + 0.8f;
            Color c = cursorColor;
            c.a = pulse;
            cursorImage.color = c;

            // Hide mouse drag ghost in gamepad mode
            if (dragGhost != null && isDragging)
                dragGhost.gameObject.SetActive(false);
        }
        else
        {
            cursorVisual.gameObject.SetActive(false);

            // Show mouse drag ghost
            if (dragGhost != null && isDragging)
                dragGhost.gameObject.SetActive(true);
        }
    }

    private void UpdateHoverHighlight()
    {
        // Reset all cells
        RefreshCellColors();

        // Show hover/cursor preview
        if (isDragging && dragItemData != null)
        {
            int w = dragItemData.GetWidth(dragRotated);
            int h = dragItemData.GetHeight(dragRotated);
            bool canFit = inventory.CanFit(dragItemData, hoverGridX, hoverGridY, dragRotated);
            Color hoverColor = canFit ? validHoverColor : invalidHoverColor;

            for (int sx = 0; sx < w; sx++)
            {
                for (int sy = 0; sy < h; sy++)
                {
                    int gx = hoverGridX + sx;
                    int gy = hoverGridY + sy;
                    if (gx >= 0 && gx < inventory.GridWidth && gy >= 0 && gy < inventory.GridHeight)
                    {
                        cellImages[gx, gy].color = hoverColor;
                    }
                }
            }
        }
        else if (usingGamepad)
        {
            // Highlight the cell under cursor even when not dragging
            if (cursorX >= 0 && cursorX < inventory.GridWidth &&
                cursorY >= 0 && cursorY < inventory.GridHeight)
            {
                PlacedItem item = inventory.GetPlacedItemAt(cursorX, cursorY);
                if (item != null)
                {
                    // Highlight entire item footprint
                    int w = item.data.GetWidth(item.isRotated);
                    int h = item.data.GetHeight(item.isRotated);
                    Color highlight = new Color(0.5f, 0.7f, 1f, 0.4f);
                    for (int sx = 0; sx < w; sx++)
                    {
                        for (int sy = 0; sy < h; sy++)
                        {
                            int gx = item.gridX + sx;
                            int gy = item.gridY + sy;
                            if (gx >= 0 && gx < inventory.GridWidth &&
                                gy >= 0 && gy < inventory.GridHeight)
                            {
                                cellImages[gx, gy].color = highlight;
                            }
                        }
                    }
                }
            }
        }
    }

    private void UpdateHintText()
    {
        if (hintText == null) return;

        if (usingGamepad)
        {
            if (isDragging)
                hintText.text = "A Place    Y Rotate    X Drop    B Cancel    Select Close";
            else
                hintText.text = "D-Pad Move    A Pick Up    X Drop    Select Close";
        }
        else
        {
            if (isDragging)
                hintText.text = "[Click] Place    [R] Rotate    [Q] Drop    [Right-Click] Cancel    [Tab] Close";
            else
                hintText.text = "[Click] Pick Up    [R] Rotate    [Q] Drop    [Tab] Close";
        }
    }

    // ======================== REFRESH ========================

    public void RefreshGrid()
    {
        if (inventory == null || cellImages == null) return;

        RefreshCellColors();
        UpdateWeightBar();

        foreach (var vis in itemVisuals)
            if (vis != null) Destroy(vis.gameObject);
        itemVisuals.Clear();

        foreach (var placed in inventory.PlacedItems)
        {
            if (placed == null) continue;
            CreateItemVisual(placed);
        }
    }

    private void RefreshCellColors()
    {
        if (cellImages == null || inventory == null) return;
        for (int x = 0; x < inventory.GridWidth; x++)
        {
            for (int y = 0; y < inventory.GridHeight; y++)
            {
                cellImages[x, y].color = inventory.GetItemAt(x, y) != -1
                    ? occupiedCellColor : emptyCellColor;
            }
        }
    }

    private void CreateItemVisual(PlacedItem placed)
    {
        int w = placed.data.GetWidth(placed.isRotated);
        int h = placed.data.GetHeight(placed.isRotated);

        GameObject itemObj = CreateImage(gridPanel, $"Item_{placed.data.itemName}", placed.data.itemColor);
        RectTransform itemRect = itemObj.GetComponent<RectTransform>();
        itemRect.anchorMin = Vector2.zero;
        itemRect.anchorMax = Vector2.zero;
        itemRect.pivot = new Vector2(0, 1);
        itemRect.anchoredPosition = new Vector2(
            placed.gridX * cellSize + 2,
            -placed.gridY * cellSize + inventory.GridHeight * cellSize - 2);
        itemRect.sizeDelta = new Vector2(w * cellSize - 4, h * cellSize - 4);

        if (placed.data.icon != null)
        {
            itemObj.GetComponent<Image>().sprite = placed.data.icon;
            itemObj.GetComponent<Image>().type = Image.Type.Simple;
            itemObj.GetComponent<Image>().preserveAspect = true;
        }

        itemObj.GetComponent<Image>().raycastTarget = false;

        // Name label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(itemObj.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.text = placed.data.itemName;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = Mathf.Min(14, cellSize / 4);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        StretchFill(labelObj.GetComponent<RectTransform>());

        Outline outline = labelObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(1, -1);

        itemVisuals.Add(itemRect);
    }

    private void UpdateWeightBar()
    {
        if (weightBarFill == null || inventory == null) return;

        float pct = inventory.CurrentWeight / inventory.MaxWeight;
        weightBarFill.anchorMax = new Vector2(Mathf.Clamp01(pct), 1);

        Color barColor;
        if (pct > 0.8f)
            barColor = new Color(0.9f, 0.2f, 0.1f);
        else if (pct > 0.5f)
            barColor = new Color(1f, 0.7f, 0.1f);
        else
            barColor = new Color(0.2f, 0.7f, 0.3f);
        weightBarFill.GetComponent<Image>().color = barColor;

        if (weightText != null)
            weightText.text = $"Weight: {inventory.CurrentWeight:F1} / {inventory.MaxWeight:F0} kg";
    }

    // ======================== HELPERS ========================

    private GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }

    private void StretchFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
