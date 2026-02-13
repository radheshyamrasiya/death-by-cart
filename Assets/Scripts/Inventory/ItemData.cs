using UnityEngine;

/// <summary>
/// ScriptableObject defining an item's properties for the grid inventory.
/// Create via Assets → Create → Inventory → Item Data.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemName = "Item";
    public Sprite icon;
    public Color itemColor = Color.cyan;  // Fallback color if no icon

    [Header("Grid Size")]
    [Tooltip("Width in grid cells")]
    public int width = 1;
    [Tooltip("Height in grid cells")]
    public int height = 1;

    [Header("Properties")]
    public float weight = 1f;
    public ItemType type = ItemType.Supply;
    public bool canRotate = true;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description = "";

    public enum ItemType
    {
        Valuable,
        Supply,
        Weapon,
        Trap,
        Bomb
    }

    /// <summary>
    /// Get the shape as a filled rectangle (width × height, all cells occupied).
    /// For L-shapes or custom shapes, this can be extended later.
    /// </summary>
    public bool[,] GetShape(bool rotated = false)
    {
        int w = rotated ? height : width;
        int h = rotated ? width : height;
        bool[,] shape = new bool[w, h];
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                shape[x, y] = true;
        return shape;
    }

    /// <summary>Width after applying rotation.</summary>
    public int GetWidth(bool rotated) => rotated ? height : width;
    /// <summary>Height after applying rotation.</summary>
    public int GetHeight(bool rotated) => rotated ? width : height;
}
