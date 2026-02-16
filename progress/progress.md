# 🛒 Death By Cart — Development Progress

> **Last Updated:** February 16, 2026

## 📋 Project Overview

**Death By Cart** is a third-person stealth/survival game where the player pushes a shopping cart through zombie-infested environments, collecting items while avoiding detection. The game features a physics-based cart, a grid inventory system, multiple zombie types with distinct AI behaviors, and tactical gadgets.

---

## 🎮 Core Systems

| System | Status | Script(s) |
|--------|--------|-----------|
| Player Movement | ✅ Done | `PlayerMotor.cs`, `PlayerStateMachine.cs` |
| Camera System | ✅ Done | `CameraController.cs` |
| Cart Physics | ✅ Done | `CartController.cs`, `CartInteraction.cs` |
| Cart Inventory | ✅ Done | `CartInventory.cs`, `GridInventory.cs` |
| Grid Inventory UI | ✅ Done | `InventoryUI.cs`, `InventoryManager.cs` |
| Item Pickup/Drop | ✅ Done | `ItemPickup.cs`, `WorldItem.cs`, `ItemData.cs` |
| Health System | ✅ Done | `HealthSystem.cs`, `HealthBarUI.cs` |
| Stamina System | ✅ Done | `StaminaSystem.cs`, `StaminaBarUI.cs` |
| Player Crouch | ✅ Done | `PlayerCrouch.cs` |
| Noise System | ✅ Done | `NoiseSystem.cs` |
| Zombie AI | ✅ Done | `ZombieAI.cs`, `ZombieSpawner.cs` |
| Gadget System | ✅ Done | `GadgetInventory.cs` |
| RC Car Gadget | ✅ Done | `RCCarController.cs`, `RCCarItem.cs` |
| Fart Bomb Gadget | ✅ Done | `FartBombController.cs` |
| Stun Mine Gadget | ✅ Done | `StunMineController.cs` |
| Hiding System | ✅ Done | `HidingSpot.cs` |
| Scene Builder | ✅ Done | `SceneBuilder.cs` (Editor) |
| Debug HUD | ✅ Done | `CartDebugHUD.cs` |

---

## 🗂️ Detailed Documentation

Each system has its own detailed doc file:

- **[Controls](controls.md)** — Full keybindings (keyboard + controller)
- **[Player System](player-system.md)** — Movement, stamina, crouch, health, states
- **[Camera System](camera-system.md)** — 4 camera modes, mouse look, target switching
- **[Cart System](cart-system.md)** — Physics cart, weight scaling, interaction
- **[Inventory System](inventory-system.md)** — Grid inventory, pickup/drop, cart storage
- **[Zombie AI](zombie-ai.md)** — 4 zombie types, vision, hearing, states
- **[Gadgets](gadgets.md)** — RC Car, Fart Bomb, Stun Mine
- **[Hiding System](hiding-system.md)** — Cupboard mechanic, balance, investigation
- **[Noise System](noise-system.md)** — Cart noise, player noise, gadget noise
- **[Scene Builder](scene-builder.md)** — Editor tool, auto-spawning, NavMesh

---

## 🏗️ Architecture

```
Assets/Scripts/
├── Editor/
│   └── SceneBuilder.cs          # One-click scene setup
├── Gadgets/
│   ├── GadgetInventory.cs       # Gadget slot system (Q to cycle, V to use)
│   ├── RCCarController.cs       # RC car movement + noise
│   ├── RCCarItem.cs             # RC car deploy/recall
│   ├── FartBombController.cs    # Fart bomb lifecycle + noise
│   └── StunMineController.cs    # Proximity stun mine
├── Inventory/
│   ├── GridInventory.cs         # Tetris-style grid logic
│   ├── InventoryUI.cs           # Visual grid + drag/drop
│   ├── InventoryManager.cs      # Inventory state management
│   ├── ItemData.cs              # ScriptableObject item definitions
│   ├── ItemDataLibrary.cs       # Runtime item database
│   ├── ItemPickup.cs            # Pick up / carry / drop items
│   ├── ItemSpawner.cs           # Spawn items in world
│   └── WorldItem.cs             # World-space item representation
├── UI/
│   ├── HealthBarUI.cs           # Health bar display
│   └── StaminaBarUI.cs          # Stamina bar display
├── CameraController.cs          # Third-person + RC car camera
├── CartController.cs            # Physics cart with weight scaling
├── CartDebugHUD.cs              # F1 debug panel
├── CartInteraction.cs           # E to attach/detach from cart
├── CartInventory.cs             # Cart weight/fullness tracking
├── HealthSystem.cs              # HP, damage, invincibility frames
├── HidingSpot.cs                # Cupboard hiding mechanic
├── NoiseSystem.cs               # Noise detection + visualization
├── PlayerCrouch.cs              # Ctrl to crouch
├── PlayerMotor.cs               # WASD movement + gravity
├── PlayerStaminaBridge.cs       # Stamina ↔ sprint integration
├── PlayerStateMachine.cs        # Player state management
├── StaminaSystem.cs             # Stamina drain/regen
├── ZombieAI.cs                  # Zombie state machine + detection
└── ZombieSpawner.cs             # Spawn zombies at runtime
```

---

## 📊 Stats at a Glance

- **Total scripts:** 25+
- **Zombie types:** 4 (Shambler, Runner, Listener, Brute)
- **Gadgets:** 3 (RC Car, Fart Bomb, Stun Mine)
- **Player states:** 5 (FreeRoam, PushingCart, InventoryOpen, ControllingRC, Hiding)
- **Inventory system:** Tetris-style grid with weight/fullness tracking
