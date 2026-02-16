# 🔊 Noise System

> Sound-based zombie detection — cart, player, gadgets.

## Script

| Script | Purpose |
|--------|---------|
| `NoiseSystem.cs` | Central noise tracking, radius management, visualization |

---

## Noise Sources

### Cart Noise
- **Source:** Pushing the cart generates continuous noise
- **Radius:** Scales with cart speed and weight
- **Heavier cart = louder** → bigger noise radius → attracts zombies from further
- **Standing still:** No cart noise

### Player Noise
- **Sprinting:** Creates noise in a radius around the player
- **Walking:** Minimal noise
- **Crouching:** Greatly reduced noise (stealth mode)
- **Standing still:** No noise

### RC Car Noise
- **Engine:** Continuous noise while RC car is active
- **Honk (Space):** Burst of loud noise — large radius
- **Position:** Noise originates from RC car, not player

### Fart Bomb Noise
- **Detonation:** Creates a loud noise burst at bomb location
- **Gas cloud:** Sustained noise while cloud is active
- **Position:** Noise originates from bomb location

---

## How Zombies Hear

In `ZombieAI.CheckHearing()`, the zombie checks distances to all noise sources:

```
1. Cupboard noise (deprecated — removed)
2. Cart noise (if cart is moving)
3. Player noise (if not hiding)
4. RC Car noise (if active)
5. Fart Bomb noise (if active)
```

Each noise source has a radius. The zombie compares:
- `distance to noise source` < `noise radius` AND `distance` < `zombie hearing range`
- If true → `lastKnownPosition = noise source position` → transition to Chase

---

## Noise Radius Visualization

The debug HUD (F1) shows noise-related info:
- Current cart noise radius
- Player noise state
- Active gadget noise sources

---

## Stealth Mechanics

| Action | Noise Level |
|--------|-------------|
| Standing still | None |
| Walking | Low |
| Crouching + walking | Very low |
| Sprinting | High |
| Pushing cart (light) | Medium |
| Pushing cart (heavy) | High |
| RC Car engine | Medium (at car location) |
| RC Car honk | Very high burst |
| Fart Bomb | Very high |
| Entering/exiting cupboard | None (removed) |
