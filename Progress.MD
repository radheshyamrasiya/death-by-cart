# 📊 Death By Cart — Development Progress

> Last updated: February 14, 2026

---

## 🏗️ Project Setup

### Unity Project
- **Engine**: Unity (New Input System enabled)
- **Project Path**: `e:\DEATH BY CART\My project`
- **Main Scene**: `Assets/Scenes/TestGround.unity`

### Asset Dependencies
- **Starter Assets — Third Person Controller** (Unity Asset Store)
  - Provides `ThirdPersonController.cs` for player movement
  - Character model, animations, and input bindings
- **New Input System** package (`com.unity.inputsystem`)

### Editor Tooling
- **SceneBuilder** (`Assets/Scripts/Editor/SceneBuilder.cs`)
  - Menu: `Tools > Death By Cart > Setup Scene`
  - Auto-configures the scene: finds/creates player, cart, ground, obstacles
  - Attaches all required scripts to player and cart GameObjects
  - Sets up physics materials, Rigidbody settings, camera configs
  - Adds ItemDataLibrary, ItemSpawner, GridInventory, InventoryManager, InventoryUI to cart
  - Adds CartInteraction, PlayerStateMachine, StaminaSystem, ItemPickup to player

---

## ✅ Completed Systems

### 1. Player Character
**Scripts**: `PlayerStateMachine.cs`, `PlayerMotor.cs`, `PlayerStaminaBridge.cs`

- **State Machine** with 3 states:
  - `FreeRoam` — normal on-foot movement via ThirdPersonController
  - `PushingCart` — player movement disabled, cart controls active
  - `InventoryOpen` — movement disabled, cursor unlocked
- Player movement uses **Starter Assets ThirdPersonController** (MoveSpeed = 10, SprintSpeed = 15)
- `PlayerMotor` provides fallback movement enable/disable
- `PlayerStaminaBridge` connects stamina system to on-foot sprinting

---

### 2. Cart System
**Scripts**: `CartController.cs`, `CartInteraction.cs`, `CartInventory.cs` (legacy)

#### CartController (`Assets/Scripts/CartController.cs`)
- **Physics-based** movement using Rigidbody forces
- **Strafe controls**: W/S = forward/back, A/D = left/right (not tank turning)
- Full **gamepad support**: Left Stick for movement, RT sprint, LT sneak
- **Weight-based scaling** (reads from `GridInventory`):
  - Speed scales from 100% (empty) to 40% (full)
  - Mass dynamically increases with weight (+15kg at full)
  - Sideways drift worsens with weight
  - Cart wobble intensifies with weight (4x at full)
- **Sprint** with stamina drain — drain multiplied by cart weight (up to 2.5x)
- **Sneak mode** — 30% speed for stealth
- Speed cap (horizontal only) — preserves Y velocity to prevent physics bounce
- Periodic debug logging of speed, mass, weight, and mode

#### CartInteraction (`Assets/Scripts/CartInteraction.cs`)
- E / Y to **attach/detach** from nearby cart
- Snaps player behind cart at push position
- Enables/disables cart input on attach/detach
- Force-closes inventory when detaching
- Transitions player state machine between FreeRoam ↔ PushingCart

#### CartInventory (`Assets/Scripts/CartInventory.cs`) — LEGACY
- Old tier-based fullness system (Empty/Light/Half/Full)
- Still present for backward compatibility with `GridInventory.Tier`
- Consider removing once fully migrated

---

### 3. Camera System
**Scripts**: `CameraController.cs`

- **Cinemachine-based** third-person camera
- Follows player with smooth damping
- Mouse look controls with sensitivity settings
- Works in both on-foot and cart-pushing modes

---

### 4. Stamina System
**Scripts**: `StaminaSystem.cs`, `UI/StaminaBarUI.cs`

- **Shared stamina meter** — works for both on-foot and cart sprinting
- Configurable: maxStamina (100), drainRate (25/s), regenRate (15/s), regenDelay (1.5s)
- **Exhaustion mechanic**: at 0% stamina, sprint locks until regen reaches 20%
- **Weight-based drain multiplier**: `SetSprinting(bool, float drainMult)` — heavier cart = faster drain
- Events: `OnExhausted`, `OnRecovered`
- **UI bar** renders at top-right of screen with color-coded fill (green → yellow → red)

---

### 5. Grid Inventory System (RE4-Style)
**Scripts**: `GridInventory.cs`, `InventoryUI.cs`, `InventoryManager.cs`, `ItemData.cs`

#### GridInventory (`Assets/Scripts/Inventory/GridInventory.cs`)
- **2D grid** (default 10×10) where items occupy cells based on their shape
- Tracks both **space** (cell occupancy) and **weight** (current/max)
- Core API:
  - `CanFit(item, x, y, rotated)` — bounds + occupancy + weight check
  - `PlaceItem(item, x, y, rotated)` — marks cells, updates weight
  - `RemoveItem(index or PlacedItem)` — clears cells, restores weight
  - `AutoPlace(item)` — finds first valid position (tries unrotated, then rotated)
  - `SpillAll()` — dumps everything (cart tip-over mechanic)
- Properties: `Fullness` (0-1 weight ratio), `CurrentWeight`, `MaxWeight`, `IsFull`, `ItemCount`
- Backward-compat `Tier` property maps to old `CartInventory.FullnessTier`
- Events: `OnItemPlaced`, `OnItemRemoved`, `OnInventoryChanged`

#### InventoryUI (`Assets/Scripts/Inventory/InventoryUI.cs`)
- **Code-driven UI** — creates entire Canvas, grid, cells, weight bar, hints at runtime
- **Dual input support**: mouse AND gamepad, with auto-detection
- **Mouse mode**: click to pick/place, drag ghost follows cursor, R rotate, Q drop, right-click cancel
- **Gamepad mode**: yellow pulsing cursor, D-Pad/Stick navigation with repeat delay (0.18s), A pick/place, Y rotate, X drop, B cancel
- Visual feedback:
  - Green/red highlight for valid/invalid placement
  - Blue highlight on item footprint when gamepad cursor hovers
  - Drag ghost with item color and transparency
  - Dynamic hint text (changes between keyboard and gamepad labels)
- **Weight bar**: green (< 50%), yellow (50-80%), red (> 80%)
- **Drop from cart**: dragged items can be dropped to ground (Q key, X button, or click outside grid)
- `StartPlacingItem(ItemData)` — opens with item already on cursor (used by deposit flow)

#### InventoryManager (`Assets/Scripts/Inventory/InventoryManager.cs`)
- Tab / Select to toggle inventory open/close
- Escape to close
- Only opens when **attached to cart**
- Manages cursor lock state
- Links GridInventory ↔ InventoryUI

#### ItemData (`Assets/Scripts/Inventory/ItemData.cs`)
- ScriptableObject defining item properties
- Fields: `itemName`, `width`, `height`, `weight`, `canRotate`, `itemColor`, `icon`
- Shape support via `GetShape(rotated)` — returns 2D bool array
- `GetWidth/GetHeight(rotated)` for rotated dimensions

---

### 6. Item Pickup System (Carry & Deposit)
**Scripts**: `ItemPickup.cs`, `WorldItem.cs`, `ItemDataLibrary.cs`, `ItemSpawner.cs`

#### ItemPickup (`Assets/Scripts/Inventory/ItemPickup.cs`)
- Attached to **player**
- **F / X = context-sensitive**: pick up (not carrying) or deposit (carrying near cart)
- **G / B = drop** carried item on ground
- **Carry weight slowdown**:
  - Modifies ThirdPersonController `MoveSpeed` and `SprintSpeed` at runtime
  - Scales from 100% (0kg) to 50% (10kg+) — tunable via Inspector
  - Restores speed on deposit/drop
- Finds nearest item within pickupRange (3m), nearest cart within depositRange (4m)
- **HUD**: green pickup prompt (bottom), blue carrying banner (top) with speed penalty display
- Dynamic prompt shows gamepad buttons when controller detected
- `SpawnDroppedItem(ItemData, Vector3)` — static method to create dropped WorldItems

#### WorldItem (`Assets/Scripts/Inventory/WorldItem.cs`)
- Attached to pickupable objects in the world
- Visual effects: **bobbing** (0.2m amplitude), **spinning** (90°/s), **glow pulse** (emission)
- **Pickup animation**: shrinks to 0 over 0.3s, then destroys
- References an `ItemData` ScriptableObject

#### ItemDataLibrary (`Assets/Scripts/Inventory/ItemDataLibrary.cs`)
- **Runtime singleton** — creates ItemData ScriptableObjects in memory (no asset files needed)
- 8 item types defined:

| Item | Size | Weight | Color |
|------|------|--------|-------|
| Canned Beans | 1×1 | 0.5 kg | Orange |
| Gemstone | 1×1 | 1.5 kg | Cyan |
| Medkit | 2×1 | 3.0 kg | White |
| Ammo Box | 2×1 | 5.0 kg | Yellow |
| Gold Bar | 1×2 | 8.0 kg | Gold |
| Pipe Bomb | 1×1 | 2.0 kg | Red |
| Shotgun | 4×1 | 6.0 kg | Gray |
| Supply Crate | 3×2 | 10.0 kg | Brown |

#### ItemSpawner (`Assets/Scripts/Inventory/ItemSpawner.cs`)
- Spawns **15 pickupable items** as colored cubes scattered around the map
- Uses **raycast** to find ground level for proper placement
- Each cube gets a WorldItem component + random ItemData from library
- Trigger collider (2x size) for easy pickup detection

---

### 7. Debug Tools
**Scripts**: `CartDebugHUD.cs`

- On-screen HUD showing cart status:
  - Current speed / effective max speed
  - Input values (move, turn)
  - Sprint / sneak state
  - Fullness percentage and tier

---

## 📁 File Structure

```
Assets/Scripts/
├── CartController.cs          — Cart physics, movement, weight scaling
├── CartInteraction.cs         — Player ↔ Cart attach/detach
├── CartInventory.cs           — Legacy tier-based inventory (kept for compat)
├── CameraController.cs        — Cinemachine camera setup
├── CartDebugHUD.cs            — Debug overlay for cart stats
├── PlayerStateMachine.cs      — Player state: FreeRoam / PushingCart / InventoryOpen
├── PlayerMotor.cs             — Movement enable/disable wrapper
├── PlayerStaminaBridge.cs     — Connects stamina to on-foot sprinting
├── StaminaSystem.cs           — Shared stamina meter with weight drain
├── Editor/
│   └── SceneBuilder.cs        — One-click scene setup tool
├── Inventory/
│   ├── GridInventory.cs       — 2D grid logic (space + weight)
│   ├── InventoryUI.cs         — Grid UI with dual mouse/gamepad input
│   ├── InventoryManager.cs    — Open/close/toggle inventory
│   ├── ItemData.cs            — ScriptableObject for item definitions
│   ├── ItemDataLibrary.cs     — Runtime item type definitions (8 types)
│   ├── ItemPickup.cs          — Carry & deposit flow + weight slowdown
│   ├── ItemSpawner.cs         — Spawns world items around map
│   └── WorldItem.cs           — Pickupable world object with animations
└── UI/
    └── StaminaBarUI.cs        — Stamina bar renderer
```

---

## 🔧 Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│                      PLAYER                          │
│  PlayerStateMachine ← manages states                 │
│  ThirdPersonController ← movement (Starter Assets)   │
│  CartInteraction ← E to attach/detach                │
│  ItemPickup ← F to carry/deposit, weight slowdown    │
│  StaminaSystem ← shared sprint meter                 │
│  PlayerMotor ← movement enable/disable               │
│  PlayerStaminaBridge ← on-foot sprint drain           │
└────────────────────────┬─────────────────────────────┘
                         │ E / Y
                         ▼
┌──────────────────────────────────────────────────────┐
│                       CART                            │
│  CartController ← physics, weight scaling, sprinting  │
│  GridInventory ← 2D grid (cells + weight tracking)   │
│  InventoryManager ← Tab to open/close                │
│  InventoryUI ← grid rendering, mouse + gamepad input  │
│  CartInventory ← legacy (backward compat only)       │
│  ItemDataLibrary ← runtime item definitions          │
│  ItemSpawner ← spawns world items                    │
└──────────────────────────────────────────────────────┘
                         ▲
                    F / X deposit
                         │
┌──────────────────────────────────────────────────────┐
│                   WORLD ITEMS                        │
│  WorldItem ← bobbing, spinning, glow, pickup anim    │
│  ItemData ← name, size, weight, color                │
└──────────────────────────────────────────────────────┘
```

---

## 🐛 Known Issues / Future Work

- [ ] **Remove `CartInventory.cs`** — fully migrated to `GridInventory`, old script only kept for `FullnessTier` enum reference
- [ ] **Item sprites/icons** — currently items are solid color blocks; could add proper icons
- [ ] **TextMeshPro** — inventory UI uses legacy `UnityEngine.UI.Text`; upgrade for sharper fonts
- [ ] **Sound effects** — no audio yet (cart rattling, item pickup, inventory open/close)
- [ ] **Enemies / zombies** — cart noise = zombie attraction (designed but not implemented)
- [ ] **Cart tip-over** — `SpillAll()` exists but no trigger condition yet
- [ ] **Save/load** — inventory state is runtime-only
- [ ] **Multiple item shapes** — ItemData supports custom shapes but all current items are rectangles
- [ ] **Item stacking** — not implemented (each placement is unique)

---

## 📝 Changelog

### Session 1 — Foundation
- Created project structure and SceneBuilder editor tool
- Implemented CartController with physics-based movement
- Built PlayerStateMachine with FreeRoam/PushingCart/InventoryOpen states
- Set up CameraController with Cinemachine
- Created CartInteraction for attach/detach
- Added CartInventory with fullness tiers

### Session 2 — Cart Physics & Stamina
- Added sprint/dash system with stamina meter
- Implemented StaminaBarUI
- Added weight-based cart physics (speed, mass, wobble, drift)
- Fixed cart physics bounce bug (Y velocity preservation)
- Added controller support (gamepad) to all systems

### Session 3 — Grid Inventory
- Built RE4-style GridInventory (10×10 grid, weight tracking)
- Created InventoryUI with code-driven UI generation
- Implemented drag-and-drop item placement with rotation
- Added ItemData ScriptableObject and ItemDataLibrary (8 item types)
- Created InventoryManager for open/close/toggle

### Session 4 — Item Pickup & Polish
- Implemented full carry-and-deposit pickup flow (F to pick up, F to deposit)
- Created WorldItem with bobbing/spinning/glow animations
- Added ItemSpawner for map population (15 items)
- Fixed E key conflict between ItemPickup and CartInteraction
- Improved inventory UI visuals (larger cells, grid lines, color palette)
- Added **full gamepad inventory controls** (D-Pad cursor, A/Y/X/B buttons)
- Implemented **drop items from cart** (Q key, X button, click outside grid)
- Implemented **carry weight slowdown** (heavier items = slower player)
- Rewired **CartController to GridInventory** (weight-based speed/mass/wobble)
- Added **weight-based stamina drain** (heavier cart = faster drain)
