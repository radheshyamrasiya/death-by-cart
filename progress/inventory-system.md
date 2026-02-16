# 📦 Inventory System

> Tetris-style grid inventory with drag-and-drop UI.

## Scripts

| Script | Purpose |
|--------|---------|
| `GridInventory.cs` | Core grid logic — place, remove, rotate, collision detection |
| `InventoryUI.cs` | Visual grid, drag/drop, item rendering, controller support |
| `InventoryManager.cs` | State management, open/close, coordination |
| `ItemData.cs` | ScriptableObject defining item properties |
| `ItemDataLibrary.cs` | Runtime item database for lookups |
| `ItemPickup.cs` | Pick up items from world, carry, drop |
| `ItemSpawner.cs` | Spawn items in world at runtime |
| `WorldItem.cs` | World-space item representation with physics |

---

## ItemData Properties

```csharp
string itemName;        // Display name
int width, height;      // Grid dimensions
float weight;           // Weight in kg
ItemType type;          // Food, Supply, Gadget, etc.
Color itemColor;        // Visual tint
string description;     // Tooltip text
```

---

## Grid System

- **Grid size:** Configurable (e.g., 8×6)
- **Placement:** Items occupy `width × height` cells
- **Rotation:** `R` key rotates items 90° (swaps width/height)
- **Collision:** Items cannot overlap
- **Weight sum:** Grid tracks total weight of all placed items

---

## Pickup / Drop Flow

```
World Item → E (pickup) → Player carries item
    → Tab (open inventory) → Click to place in grid
    → G to drop back to world

Cart proximity → items can be loaded into cart grid
```

---

## Controller Support

- **D-Pad:** Navigate grid cells
- **A/Cross:** Select / place item
- **B/Circle:** Rotate item
- **Y/Triangle:** Open / close inventory
