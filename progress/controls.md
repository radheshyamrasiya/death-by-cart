# 🎮 Controls — Death By Cart

> Full keybinding reference for keyboard and controller.

## Keyboard Controls

### Movement
| Key | Action |
|-----|--------|
| `W/A/S/D` | Move player (free roam) / Move cart (pushing) |
| `Left Shift` | Sprint (drains stamina) |
| `C` | Toggle crouch (reduces noise, slower movement) |
| `Mouse` | Look around / orbit camera |

### Camera
| Key | Action |
|-----|--------|
| `C` | Cycle camera mode (Chase → TopDown → OverTheShoulder → FirstPerson) |

> **Note:** Camera mode (`C`) and crouch (`C`) share the same key — crouch is Left Stick Click on gamepad.

### Cart Interaction
| Key | Action |
|-----|--------|
| `E` | Attach to / detach from cart |
| `E` | Pick up / drop items |
| `E` | Enter / exit hiding spot (cupboard) |

### Inventory
| Key | Action |
|-----|--------|
| `Tab` | Open / close inventory |
| `Mouse Click` | Pick up / place items in grid |
| `R` | Rotate item (while holding in inventory) |
| `G` | Drop carried item to world |

### Gadgets
| Key | Action |
|-----|--------|
| `Q` | Cycle gadget slots (Holster → Slot1 → Slot2 → Slot3 → Holster) |
| `V` | Use active gadget |
|     | → RC Car: Deploy / Cancel if already deployed |
|     | → Fart Bomb: Place → Detonate (2-press) |
|     | → Stun Mine: Place mine at feet |

### RC Car (while controlling)
| Key | Action |
|-----|--------|
| `W/A/S/D` | Drive RC car |
| `Left Shift` | RC car boost |
| `Space` | Honk horn (loud — attracts zombies) |
| `V` / `Escape` | Return to player |

### System
| Key | Action |
|-----|--------|
| `F1` | Toggle debug HUD (8 collapsible sections) |

---

## Controller Controls (Gamepad)

### Movement
| Button | Action |
|--------|--------|
| `Left Stick` | Move player / cart |
| `Right Stick` | Look around (camera) |
| `Left Trigger` | Sprint |
| `Left Stick Click (L3)` | Toggle crouch |

### Interaction
| Button | Action |
|--------|--------|
| `X / Square` | Interact (E equivalent) — cart, items, hiding |

### Inventory
| Button | Action |
|--------|--------|
| `Y / Triangle` | Open / close inventory |
| `D-Pad` | Navigate inventory grid |
| `A / Cross` | Select / place item |
| `B / Circle` | Rotate item |

### Gadgets
| Button | Action |
|--------|--------|
| `Left Bumper` | Cycle gadget slots |
| `Right Bumper` | Use active gadget |

---

## Context-Sensitive Controls

The `E` key behavior changes based on context and proximity:

1. **Inside hiding spot** → Exit hiding spot (top priority)
2. **Near cupboard** → Enter hiding spot
3. **Near cart** → Attach to / detach from cart
4. **Near world item** → Pick up item
5. **Carrying item** → Drop item

---

## Debug HUD Sections (F1)

| Section | Contents |
|---------|----------|
| PLAYER | State, speed, position, cart status, crouch, carrying |
| HEALTH | HP bar, current/max, invincibility |
| STAMINA | Stamina bar, sprint status, exhaustion |
| INVENTORY | Grid weight, fullness, item count |
| CART | Cart speed, weight, mass, noise |
| NOISE | Cart/player/RC/bomb noise radii |
| ZOMBIES | Count, states, nearest distance, kill count |
| CONTROLS | Full keybinding reference |
