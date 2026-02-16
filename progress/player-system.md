# 🧑 Player System

> Movement, stamina, crouch, health, and state management.

## Scripts

| Script | Purpose |
|--------|---------|
| `PlayerMotor.cs` | WASD movement, sprint, gravity, camera-relative rotation |
| `PlayerStateMachine.cs` | State transitions (FreeRoam, PushingCart, InventoryOpen, ControllingRC, Hiding) |
| `StaminaSystem.cs` | Stamina drain (sprint), regen, exhaustion |
| `PlayerStaminaBridge.cs` | Connects StaminaSystem to PlayerMotor for stamina-based speed limiting |
| `PlayerCrouch.cs` | C key to toggle crouch, reduces noise and speed |
| `HealthSystem.cs` | HP, damage, invincibility frames, death/revive |

---

## Player States

```
FreeRoam ──→ PushingCart     (E near cart)
FreeRoam ──→ InventoryOpen   (Tab)
FreeRoam ──→ ControllingRC   (V with RC Car)
FreeRoam ──→ Hiding          (E near cupboard)
   ↑            │
   └────────────┘  (E / Escape to return)
```

| State | Movement | Camera | Cursor | Input |
|-------|----------|--------|--------|-------|
| FreeRoam | ✅ WASD | Free orbit | Locked | All keys |
| PushingCart | ❌ Snapped behind cart | Follow cart (auto FirstPerson) | Locked | WASD drives cart |
| InventoryOpen | ❌ Frozen | Fixed | Visible | Mouse for grid |
| ControllingRC | ❌ Frozen | Follow RC car | Locked | WASD drives RC |
| Hiding | ❌ Frozen + invisible | On cupboard | Locked | E to exit |

---

## Movement Stats

| Parameter | Value |
|-----------|-------|
| Walk speed | 5 m/s |
| Sprint speed | 9 m/s |
| Gravity | -20 m/s² |
| Rotation smooth time | 0.1s |
| Ground check radius | 0.3 |

---

## Health System

| Parameter | Value |
|-----------|-------|
| Max HP | 100 |
| Invincibility after hit | 0.5s |
| Damage flash | 0.3s |
| Revive invincibility | 1.0s |

**Events:** `OnDamaged(damage, remainingHP)`, `OnDeath`, `OnHealed(amount)`

On death: ThirdPersonController disabled, cart detached.

---

## Stamina System

| Parameter | Value |
|-----------|-------|
| Max stamina | 100 |
| Drain rate | 25/s while sprinting |
| Regen rate | 15/s after delay |
| Regen delay | 1.5s after stopping sprint |
| Exhaustion threshold | 0% (fully drained) |
| Recovery threshold | 20% (can sprint again) |

**Weight scaling:** When pushing a heavy cart, drain multiplier increases (up to 2.5×).

**Events:** `OnExhausted`, `OnRecovered`

---

## Crouch System

| Parameter | Value |
|-----------|-------|
| Toggle key | `C` (keyboard) / `L3` (gamepad) |
| Speed multiplier | 0.4× (40% of normal) |
| Noise multiplier | 0.15× (85% quieter) |
| Visual scale Y | 0.6× (shorter model) |
| Transition speed | 8 (smooth lerp) |

- **Auto-stand:** Crouching is cancelled when attaching to cart
- **HUD:** Shows "🦆 CROUCHING [C] Stand" at bottom of screen
