# 🧟 Zombie AI System

> 4 distinct zombie types with vision, hearing, and memory-based AI.

## Scripts

| Script | Purpose |
|--------|---------|
| `ZombieAI.cs` | Full state machine: Idle, Patrol, Chase, Attack |
| `ZombieSpawner.cs` | Spawns zombies at runtime with random types and NavMesh placement |

---

## Zombie Types

| Type | Vision | Angle | Hearing | Move | Chase | Attack | Nav Radius | Special |
|------|--------|-------|---------|------|-------|--------|------------|---------|
| **Shambler** | 8m | 45° | 15m | 1.5 | 3.0 | 15 dmg | 0.4 | Standard slow zombie |
| **Runner** | 12m | 60° | 10m | 3.0 | 6.0 | 10 dmg | 0.3 | Fast but weak |
| **Listener** | 5m | 30° | 35m | 2.0 | 4.0 | 15 dmg | 0.4 | Nearly blind, amazing hearing |
| **Brute** | 15m | 90° | 20m | 1.2 | 2.5 | 35 dmg | 0.55 | Tank — wide vision, high damage, slow |

---

## Visual Appearance

| Type | Color | Scale | Eyes |
|------|-------|-------|------|
| Shambler | Sickly green | 0.5 × 0.9 × 0.5 | Red glow |
| Runner | Reddish | 0.4 × 0.85 × 0.4 | Red glow |
| Listener | Purple | 0.45 × 0.95 × 0.45 | Cloudy gray |
| Brute | Brown | 0.7 × 1.1 × 0.7 | Red glow |

---

## AI State Machine

```
Idle ──→ Patrol ──→ Chase ──→ Attack
  ↑         ↑         │         │
  └─────────┘←────────┘←────────┘
          (lost target)    (out of range)
```

### Idle
- Stand still for 2–5 seconds
- Transition to Patrol when timer expires

### Patrol
- Walk to random point within patrol radius
- Speed: `moveSpeed`
- If player detected (vision or hearing) → Chase

### Chase
- Speed: `chaseSpeed` (2× turn speed)
- Track `lastKnownPosition`
- Memory timer: if target lost, continue heading to last known position
- If memory timer expires and not investigating → Patrol
- If close enough (attackRange) AND can see player → Attack

### Attack
- Stop and face player
- Attack on cooldown interval
- Deals `attackDamage` to `HealthSystem`
- Brute adds `attackKnockback`
- If player too far → Chase
- If player hiding → Patrol

---

## Detection Systems

### Vision
- **Range:** Varies by type (5m–15m)
- **Angle:** Forward cone (30°–90°)
- **Method:** Raycast from eye position → blocks by walls and obstacles
- **Blocked by:** Anything with a collider (walls, NavMeshObstacles)
- **Hidden player:** Automatically fails if `HidingSpot.IsPlayerHiding`

### Hearing
- **Range:** Varies by type (10m–35m)
- **Sources checked (in order):**
  1. Cupboard noise (entry/exit — currently disabled)
  2. Cart noise (NoiseSystem)
  3. Player noise (footsteps, sprinting)
  4. RC Car noise (engine + honk)
  5. Fart Bomb noise
- **Hidden player:** Regular player noise skipped while hiding

### Memory
- After losing sight/hearing, zombie continues to last known position
- Memory duration: `chaseMemory` seconds
- After memory expires → Patrol

---

## Investigation System (Cupboard)

- **Trigger:** Zombie was in **Chase** state AND had **visual line of sight** (`CanSeePlayer`) when player entered cupboard
- **Behavior:** Zombie walks to cupboard position, bangs for 0.5s, drags player out
- **After investigation:** Zombie transitions to Patrol with path reset
- **Key fix:** Only ONE zombie investigates (the first qualifying one)

---

## Stun System

- **Triggered by:** Stun Mine proximity
- **Duration:** 3 seconds
- **Effects:**
  - Speed reduced to 5% of original
  - Path cleared
  - Blue visual tint
  - Random 90–180° rotation (disorientation)
- **Recovery:** Speed and color restored, zombie faces random direction
