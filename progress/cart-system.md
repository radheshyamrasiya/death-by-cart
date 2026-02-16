# 🛒 Cart System

> Physics-based shopping cart with weight scaling and inventory.

## Scripts

| Script | Purpose |
|--------|---------|
| `CartController.cs` | Rigidbody cart movement, weight-based speed/wobble/mass |
| `CartInteraction.cs` | E to attach/detach, snaps player to push position |
| `CartInventory.cs` | Tracks cart weight and fullness percentage |

---

## Physics

The cart uses Unity's Rigidbody physics. All movement is force-based in `FixedUpdate()`.

### Weight Scaling

As you add items to the cart, its behavior changes:

| Property | Empty Cart | Full Cart |
|----------|-----------|-----------|
| Max speed | Fast | Significantly slower |
| Wobble | None | Heavy swaying |
| Mass | Light | Heavy |
| Handling | Responsive | Sluggish |
| Noise | Quiet | Loud (attracts zombies) |

Weight is read from `GridInventory` in real-time, so adding/removing items immediately affects handling.

---

## Cart Interaction

- **Attach:** Walk near cart → press `E` → player snaps to push position behind cart
- **Detach:** Press `E` again → player returns to free roam
- **While pushing:** WASD moves the cart (strafe controls), player follows automatically
- **State:** Transitions `PlayerStateMachine` to `PushingCart` / `FreeRoam`

---

## Cart Inventory

- **Weight tracking:** Sum of all item weights in the grid
- **Fullness:** Percentage of grid cells occupied
- **Drop items:** Items can fall off the cart (physics-based)
- **Visual:** Debug HUD shows weight and fullness stats
