# 🎮 Death By Cart — Controls Reference

> All controls use Unity's **New Input System**. Keyboard and gamepad work simultaneously — the game auto-detects which one you're using.

---

## 🚶 On Foot (Free Roam)

Player is walking around, not attached to the cart.

| Action | Keyboard | Controller |
|--------|----------|------------|
| **Move** | WASD | Left Stick |
| **Look** | Mouse | Right Stick |
| **Sprint** | Left Shift (hold) | Right Trigger (hold) |
| **Jump** | Space | South Button (A / ✕) |

### Sprint & Stamina
- Sprinting drains the **stamina meter** (visible as a bar on screen).
- When stamina hits 0%, you become **exhausted** and can't sprint until it regens to 20%.
- Stamina regens automatically after a 1.5s delay when not sprinting.

---

## 🛒 Cart Interaction

| Action | Keyboard | Controller |
|--------|----------|------------|
| **Attach to cart** | E (near cart) | Y / △ (near cart) |
| **Detach from cart** | E (while attached) | Y / △ (while attached) |

### Rules
- You must be within ~3m of the cart to attach.
- The player snaps behind the cart and faces forward.
- Movement switches to cart controls (see below).
- Detaching re-enables on-foot movement.

---

## 🛒 Pushing Cart (Attached)

Player is attached to the cart and pushing it.

| Action | Keyboard | Controller |
|--------|----------|------------|
| **Push Forward** | W | Left Stick Up |
| **Pull Backward** | S | Left Stick Down |
| **Strafe Left** | A | Left Stick Left |
| **Strafe Right** | D | Left Stick Right |
| **Sprint** | Left Shift (hold) | Right Trigger (hold) |
| **Sneak** | Left Ctrl (hold) | Left Trigger (hold) |
| **Open Inventory** | Tab | Select / Back |
| **Detach** | E | Y / △ |

### Cart Physics
- The cart uses **Rigidbody physics** — it has momentum, mass, drift, and wobble.
- **Heavier cart = slower movement, more drift, more wobble, higher mass**.
- Sprint + push drains stamina **faster** when the cart is heavier (up to 2.5x drain at 100% full).
- Sneak mode = 30% speed, quieter movement.
- Sprint and sneak cancel each other — sneak takes priority.

### Weight Scaling (Cart)

| Cart Fullness | Speed | Extra Mass | Wobble | Drift |
|---------------|-------|------------|--------|-------|
| **0% (Empty)** | 100% | +0 kg | Minimal | Tight |
| **25% (Light)** | 85% | +3.75 kg | Low | Normal |
| **50% (Half)** | 70% | +7.5 kg | Moderate | Loose |
| **75% (Heavy)** | 55% | +11.25 kg | High | Very Loose |
| **100% (Full)** | 40% | +15 kg | 4x base | Maximum drift |

---

## 📦 Item Pickup & Carry

| Action | Keyboard | Controller |
|--------|----------|------------|
| **Pick up item** | F (near item, not on cart) | X / □ |
| **Deposit to cart** | F (carrying, near cart) | X / □ |
| **Drop carried item** | G | B / ○ |

### Pickup Flow
1. **Detach** from the cart (E / Y).
2. **Walk** to a world item (glowing, spinning cubes).
3. Press **F / X** to pick it up — you are now **carrying** it.
4. Walk back to the cart.
5. Press **F / X** near the cart to **deposit** it.
   - If the cart has space, it auto-places into the grid.
   - If no auto-space is available, the **inventory opens** with the item on your cursor for manual placement.
6. You can also press **G / B** at any time to drop the item on the ground.

### Carry Weight Slowdown

When carrying an item, your walk and sprint speeds are reduced based on item weight:

| Item Weight | Speed Penalty |
|-------------|---------------|
| **0.5 kg** (Beans) | ~97% speed (barely noticeable) |
| **1.5 kg** (Gem) | ~92% speed |
| **3.0 kg** (Medkit) | ~85% speed |
| **5.0 kg** (Ammo Box) | ~75% speed |
| **8.0 kg** (Gold Bar) | ~60% speed |
| **10.0 kg** (Crate) | **50% speed** (maximum penalty) |

Speed is **fully restored** when you deposit or drop the item.

### HUD Indicators
- **Green prompt** at bottom of screen when near a pickupable item (shows name, size, weight).
- **Blue banner** at top of screen when carrying (shows item info, controls, speed penalty).
- If attached to cart, prompt changes to **orange** saying "Leave Cart First".

---

## 📋 Inventory (Grid Management)

The inventory is a **RE4-style grid** that opens when attached to the cart.

### Mouse Controls

| Action | Input |
|--------|-------|
| **Open / Close** | Tab |
| **Pick up item** | Left Click on item |
| **Place item** | Left Click on valid grid cell |
| **Rotate item** | R (while holding) |
| **Drop item to ground** | Q (while holding) |
| **Drop item to ground** | Left Click outside grid (while holding) |
| **Cancel (put back)** | Right Click (while holding) |
| **Close** | Tab or Escape |

### Gamepad Controls

| Action | Button |
|--------|--------|
| **Open / Close** | Select / Back |
| **Move cursor** | D-Pad or Left Stick |
| **Pick up item** | A / ✕ |
| **Place item** | A / ✕ (on valid cell) |
| **Rotate item** | Y / △ |
| **Drop item to ground** | X / □ |
| **Cancel (put back)** | B / ○ |

### Gamepad Cursor
- A **yellow pulsing border** shows your current grid position.
- The cursor **resizes** to match the item's footprint when holding an item.
- Items under the cursor are **highlighted in blue** to show their full footprint.
- **D-Pad** = precise cell-by-cell movement.
- **Left Stick** = smooth scrolling with 0.18s repeat delay.

### Auto-Detection
- The UI **auto-detects** whether you're using mouse or gamepad.
- Touch the controller → gamepad cursor appears, mouse cursor hides.
- Move the mouse → mouse mode activates, gamepad cursor hides.
- **Hint text** at the bottom updates dynamically to show relevant controls.

### Grid Rules
- Items occupy cells based on their **width × height** shape.
- Items can be **rotated** (R or Y) to fit in different orientations.
- The **weight bar** at the bottom shows current / max weight with color coding:
  - 🟢 Green: under 50%
  - 🟡 Yellow: 50-80%
  - 🔴 Red: over 80%
- Items that don't fit are shown with **red highlight**; valid placements show **green**.
- Dropped items spawn as pickupable WorldItems near the cart.

---

## 🔑 Quick Reference Card

```
╔══════════════════════════════════════════════════╗
║  E / Y     = Cart attach / detach               ║
║  F / X     = Pick up item / Deposit item         ║
║  G / B     = Drop carried item                   ║
║  Tab / Sel = Open / Close inventory              ║
║  Shift / RT = Sprint                             ║
║  Ctrl / LT  = Sneak (cart only)                  ║
║  R / Y     = Rotate item (in inventory)          ║
║  Q / X     = Drop item to ground (in inventory)  ║
╚══════════════════════════════════════════════════╝
```
