using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RE4-style grid inventory. Manages a 2D grid where items occupy cells based on their shape.
/// Tracks both space and weight. Pure logic — no UI code.
/// Attach to the Cart GameObject (replaces CartInventory's role).
/// </summary>
public class GridInventory : MonoBehaviour
{
    [Header("Grid Size")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;

    [Header("Weight")]
    [SerializeField] private float maxWeight = 50f;

    // Grid state — each cell stores the index of the item occupying it, or -1
    private int[,] grid;
    private List<PlacedItem> placedItems = new List<PlacedItem>();
    private float currentWeight;

    // --- Public properties (compatible with old CartInventory API) ---
    public int GridWidth => gridWidth;
    public int GridHeight => gridHeight;
    public float MaxWeight => maxWeight;
    public float CurrentWeight => currentWeight;
    public float Fullness => Mathf.Clamp01(currentWeight / maxWeight);
    public int ItemCount => placedItems.Count;
    public bool IsFull => currentWeight >= maxWeight || !HasAnyFreeCell();
    public IReadOnlyList<PlacedItem> PlacedItems => placedItems;

    /// <summary>Fullness tier for CartController physics scaling (backward compat).</summary>
    public CartInventory.FullnessTier Tier
    {
        get
        {
            float f = Fullness;
            if (f < 0.2f) return CartInventory.FullnessTier.Empty;
            if (f < 0.5f) return CartInventory.FullnessTier.Light;
            if (f < 0.8f) return CartInventory.FullnessTier.Half;
            return CartInventory.FullnessTier.Full;
        }
    }

    // Events
    public System.Action<PlacedItem> OnItemPlaced;
    public System.Action<PlacedItem> OnItemRemoved;
    public System.Action OnInventoryChanged;

    private void Awake()
    {
        grid = new int[gridWidth, gridHeight];
        ClearGrid();
    }

    private void ClearGrid()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                grid[x, y] = -1;
    }

    // ======================== PLACEMENT ========================

    /// <summary>Check if an item fits at position (gridX, gridY).</summary>
    public bool CanFit(ItemData item, int gridX, int gridY, bool rotated = false)
    {
        if (item == null) return false;
        if (currentWeight + item.weight > maxWeight) return false;

        int w = item.GetWidth(rotated);
        int h = item.GetHeight(rotated);

        // Bounds check
        if (gridX < 0 || gridY < 0 || gridX + w > gridWidth || gridY + h > gridHeight)
            return false;

        // Cell occupancy check
        bool[,] shape = item.GetShape(rotated);
        for (int sx = 0; sx < w; sx++)
        {
            for (int sy = 0; sy < h; sy++)
            {
                if (shape[sx, sy] && grid[gridX + sx, gridY + sy] != -1)
                    return false;
            }
        }
        return true;
    }

    /// <summary>Place an item at position. Returns true if successful.</summary>
    public bool PlaceItem(ItemData item, int gridX, int gridY, bool rotated = false)
    {
        if (!CanFit(item, gridX, gridY, rotated)) return false;

        int itemIndex = placedItems.Count;
        PlacedItem placed = new PlacedItem(item, gridX, gridY, rotated, itemIndex);

        // Mark cells
        int w = item.GetWidth(rotated);
        int h = item.GetHeight(rotated);
        bool[,] shape = item.GetShape(rotated);
        for (int sx = 0; sx < w; sx++)
        {
            for (int sy = 0; sy < h; sy++)
            {
                if (shape[sx, sy])
                    grid[gridX + sx, gridY + sy] = itemIndex;
            }
        }

        placedItems.Add(placed);
        currentWeight += item.weight;

        Debug.Log($"[GRID] + {item.itemName} at ({gridX},{gridY}) {(rotated ? "[R]" : "")} | Weight: {currentWeight:F1}/{maxWeight}");
        OnItemPlaced?.Invoke(placed);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Remove a placed item by index.</summary>
    public bool RemoveItem(int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= placedItems.Count) return false;

        PlacedItem placed = placedItems[itemIndex];
        if (placed == null) return false;

        // Clear cells
        int w = placed.data.GetWidth(placed.isRotated);
        int h = placed.data.GetHeight(placed.isRotated);
        bool[,] shape = placed.data.GetShape(placed.isRotated);
        for (int sx = 0; sx < w; sx++)
        {
            for (int sy = 0; sy < h; sy++)
            {
                if (shape[sx, sy])
                {
                    int gx = placed.gridX + sx;
                    int gy = placed.gridY + sy;
                    if (gx >= 0 && gx < gridWidth && gy >= 0 && gy < gridHeight)
                        grid[gx, gy] = -1;
                }
            }
        }

        currentWeight -= placed.data.weight;
        currentWeight = Mathf.Max(0, currentWeight);

        Debug.Log($"[GRID] - {placed.data.itemName} | Weight: {currentWeight:F1}/{maxWeight}");
        OnItemRemoved?.Invoke(placed);

        // Null out the slot (don't reindex — other items reference indices)
        placedItems[itemIndex] = null;
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Remove item by PlacedItem reference.</summary>
    public bool RemoveItem(PlacedItem placed)
    {
        if (placed == null) return false;
        return RemoveItem(placed.itemIndex);
    }

    /// <summary>Auto-place: finds the first valid position and places the item.</summary>
    public bool AutoPlace(ItemData item)
    {
        // Try unrotated first, then rotated
        for (int pass = 0; pass < 2; pass++)
        {
            bool rotated = pass == 1;
            if (!item.canRotate && rotated) continue;

            int w = item.GetWidth(rotated);
            int h = item.GetHeight(rotated);

            for (int y = 0; y <= gridHeight - h; y++)
            {
                for (int x = 0; x <= gridWidth - w; x++)
                {
                    if (CanFit(item, x, y, rotated))
                        return PlaceItem(item, x, y, rotated);
                }
            }
        }

        Debug.Log($"[GRID] ❌ No space for {item.itemName}!");
        return false;
    }

    /// <summary>Get the item index at a grid cell, or -1 if empty.</summary>
    public int GetItemAt(int gridX, int gridY)
    {
        if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
            return -1;
        return grid[gridX, gridY];
    }

    /// <summary>Get PlacedItem at a grid cell, or null.</summary>
    public PlacedItem GetPlacedItemAt(int gridX, int gridY)
    {
        int idx = GetItemAt(gridX, gridY);
        if (idx < 0 || idx >= placedItems.Count) return null;
        return placedItems[idx];
    }

    /// <summary>Check if there's any free cell anywhere.</summary>
    private bool HasAnyFreeCell()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] == -1) return true;
        return false;
    }

    /// <summary>Get total occupied cell count.</summary>
    public int OccupiedCells()
    {
        int count = 0;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                if (grid[x, y] != -1) count++;
        return count;
    }

    /// <summary>Space fullness as a percentage.</summary>
    public float SpaceFullness => (float)OccupiedCells() / (gridWidth * gridHeight);

    /// <summary>Dump everything (cart tips over).</summary>
    public void SpillAll()
    {
        Debug.Log($"[GRID] SPILL! Lost {ItemCount} items!");
        placedItems.Clear();
        currentWeight = 0;
        ClearGrid();
        OnInventoryChanged?.Invoke();
    }
}

/// <summary>
/// Represents an item placed in the grid.
/// </summary>
public class PlacedItem
{
    public ItemData data;
    public int gridX;
    public int gridY;
    public bool isRotated;
    public int itemIndex;

    public PlacedItem(ItemData data, int gridX, int gridY, bool isRotated, int itemIndex)
    {
        this.data = data;
        this.gridX = gridX;
        this.gridY = gridY;
        this.isRotated = isRotated;
        this.itemIndex = itemIndex;
    }
}
