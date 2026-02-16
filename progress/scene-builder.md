# 🏗️ Scene Builder

> Editor script for one-click scene setup.

## Script

| Script | Purpose |
|--------|---------|
| `SceneBuilder.cs` | Editor-only script that builds the entire game scene from code |

---

## What It Creates

Running the Scene Builder (via Unity Editor menu) creates:

### Environment
- **Ground plane:** Large flat surface with green material
- **Walls:** L-shaped building with gray concrete walls
  - All walls have `Collider` components (block player/zombie movement)
  - All walls have `NavMeshObstacle` with carving enabled (zombies pathfind around)
- **Cupboard:** Brown box inside the building with `HidingSpot` component

### Player
- Player capsule with all necessary components:
  - `CharacterController`, `PlayerMotor`, `PlayerStateMachine`
  - `HealthSystem`, `StaminaSystem`, `PlayerCrouch`
  - `CartInteraction`, `ItemPickup`
  - `ThirdPersonController` (StarterAssets)

### Cart
- Shopping cart with:
  - `Rigidbody` (physics-based)
  - `CartController`, `CartInventory`, `GridInventory`
  - `NoiseSystem`, `ZombieSpawner`

### Camera
- Third-person camera with `CameraController`

### Items & Gadgets
- Item spawner with various `WorldItem` pickups scattered in the world
- Gadget pickups: RC Car, Fart Bomb, Stun Mine

### UI
- `HealthBarUI`, `StaminaBarUI`
- `CartDebugHUD` (F1 toggle)
- `InventoryUI`

### NavMesh
- `NavMeshSurface` auto-baked for zombie pathfinding

---

## Usage

1. Open Unity Editor
2. Go to menu: **Death By Cart → Build Scene**
3. Everything is created automatically — no manual setup needed
4. Press Play to test

---

## NavMesh Notes

- NavMesh is baked at runtime via `NavMeshSurface`
- Walls use `NavMeshObstacle` with carving (not static geometry)
- Zombie agent radius varies by type (0.3–0.55)
- Brute may not fit through narrow gaps (intended)
