# 🔧 Gadgets System

> Tactical tools: RC Car, Fart Bomb, and Stun Mine.

## Scripts

| Script | Purpose |
|--------|---------|
| `GadgetInventory.cs` | 3-slot system, cycling (Q), usage (V), HUD display |
| `RCCarController.cs` | RC car CharacterController movement, noise, boost, honk |
| `RCCarItem.cs` | Deploy/recall RC car, camera target switch, player freeze |
| `FartBombController.cs` | Place → detonate → green gas cloud + fart sound sequence |
| `StunMineController.cs` | Proximity mine → AoE stun blast on all nearby zombies |

---

## Gadget Inventory

- **3 slots:** Cycle with `Q` (Holster → Slot1 → Slot2 → Slot3 → Holster)
- **Use:** `V` to activate selected gadget
- **HUD:** Bottom-left shows all 3 slots with names, active slot highlighted
- **Pickups:** Found scattered in the world (spawned by SceneBuilder/ItemSpawner)

---

## 🚗 RC Car

### Purpose
Distraction tool — drive a remote-controlled car to lure zombies away.

### Visual Model
Orange box body with:
- Black antenna pole
- 4 dark wheels (cylinders)
- Red blinking light on top
- Scale: 0.4×

### Controls (while deployed)
| Key | Action |
|-----|--------|
| `W/A/S/D` | Drive RC car |
| `Left Shift` | Boost speed |
| `Space` | Honk horn (loud noise burst) |
| `V` / `Escape` | Recall → return to player |

### Flow
```
Press V → Car spawns 1.5m ahead of player
       → Player frozen (ControllingRC state)
       → Camera follows RC car
       → WASD drives car, Space honks
       → V/Escape → car destroyed, player unfrozen
```

### Noise
- **Engine:** Continuous noise while active (medium radius)
- **Honk:** Large burst attracts zombies from far away

---

## 💨 Fart Bomb

### Purpose
Area denial + distraction — creates a loud gas cloud.

### Usage (2-press system)
1. **First V:** Places bomb at player's feet (small sphere)
2. **Second V:** Remote detonation → activates gas cloud

### Detonation Sequence
- Random fart-like noise pattern (multiple bursts over time)
- Green gas cloud spawns and expands
- Cloud fades out after duration ends
- All noise originates from bomb location (not player)

### Effects
- Zombies hear the noise → investigate bomb location
- Player can place and walk away before detonating

---

## 💣 Stun Mine

### Purpose
Defensive trap — auto-triggers when zombie steps on it.

### Visual
Red sphere with emissive glow sitting on ground, built from code:
- Main red sphere (emissive)
- Ring detail
- Placed at player's feet

### Mechanics

| Parameter | Value |
|-----------|-------|
| Trigger radius | 3.5m (any zombie enters) |
| Stun radius | AoE — hits ALL nearby zombies |
| Stun duration | 3 seconds |
| Speed reduction | 95% (0.05× multiplier) |
| Uses | Single — destroyed after triggering |
| Affects player | ❌ No |

### Stun Effects on Zombies
1. Speed reduced to 5% of original
2. Path cleared (`agent.ResetPath()`)
3. Blue tint visual applied
4. **Random 90–180° rotation** (disorientation)
5. After stun wears off: speeds/color restored, zombie faces wrong direction

### Trigger Sequence
```
Mine placed → Sits forever → Zombie enters 3.5m radius
           → AoE blast → All nearby zombies stunned
           → Flash effect → Mine destroyed
```
