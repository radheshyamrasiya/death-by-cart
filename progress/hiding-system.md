# 🚪 Hiding System

> Cupboard hiding mechanic with zombie investigation balance.

## Script

| Script | Purpose |
|--------|---------|
| `HidingSpot.cs` | Full hiding lifecycle — enter, hide, investigation, drag out, exit |

---

## How It Works

### Entering (E key near cupboard)
1. Player must be within 2m of cupboard
2. **Cannot enter if:** pushing cart, carrying item, controlling RC car
3. Player is teleported inside cupboard, made invisible
4. Camera switches to look at cupboard
5. `PlayerStateMachine` → Hiding state
6. Check: was any zombie **chasing AND visually seeing** the player?
   - If yes → that zombie gets `SetInvestigateTarget(cupboardPosition)` — called ONCE

### While Hiding
- `IsPlayerHiding = true` (static flag)
- Zombies skip vision and normal hearing checks for the player
- If an investigating zombie exists, monitor its distance to cupboard

### Exiting (E key or forced drag-out)
1. Player teleported to `cupboard.forward * 1.5f` (in front)
2. CharacterController toggled off/on for position reset
3. ThirdPersonController re-enabled
4. Renderers turned back on
5. Camera target back to player
6. State → FreeRoam
7. **No cooldown** — instantly reusable

---

## Investigation Balance

### Trigger Conditions
A zombie investigates the cupboard ONLY if:
1. It was in **Chase state** at the moment the player pressed E
2. It had **actual visual line of sight** (`CanSeePlayer == true`) to the player
3. It was within **25m** of the cupboard

> Zombies chasing due to noise alone (cart, fart bomb, etc.) will NOT investigate.

### Investigation Flow
```
Zombie saw player enter cupboard
    → SetInvestigateTarget(position) — called ONCE
    → Zombie walks to cupboard (stays in Chase state)
    → Within 1.5m: bang timer starts
    → 0.5 seconds of banging → DRAG OUT
    → Player exits, zombie transitions to Patrol
```

### After Drag Out
- `ClearInvestigateTarget()` → resets path + transitions zombie to Patrol
- Player can immediately re-enter (no cooldown)
- Zombie walks away to a random patrol point

---

## Safety

- **Null checks:** If investigating zombie is destroyed, reference is cleared
- **Attack prevention:** Zombies cannot attack a hiding player
  - `UpdateChase` skips attack transition when `IsPlayerHiding`
  - `UpdateAttack` transitions to Patrol when `IsPlayerHiding`

---

## Bugs Fixed

| Bug | Fix |
|-----|-----|
| `SetInvestigateTarget` called 60×/sec | Called once on entry |
| Zombies attack invisible player | Attack checks `IsPlayerHiding` |
| Zombies stuck at cupboard after investigation | `ClearInvestigateTarget` resets path + patrols |
| Zombies investigate without seeing player | Added `CanSeePlayer` check |
| Brute clips through walls | NavMesh radius increased to 0.55 |
| Debug HUD null crash | Added section null guards |
