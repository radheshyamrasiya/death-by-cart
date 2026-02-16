# 📷 Camera System

> Third-person orbit camera with 4 modes and mouse look.

## Script

| Script | Purpose |
|--------|---------|
| `CameraController.cs` | Orbit camera, 4 modes, target switching, auto-mode on cart attach |

---

## Camera Modes

Press `C` to cycle between modes:

| Mode | Distance | Height | Side | Notes |
|------|----------|--------|------|-------|
| **Chase** | 10m | 5m | Center | Default — behind and above player |
| **TopDown** | N/A | 18m | -8m Z | Bird's eye view |
| **OverTheShoulder** | 4m | 2m | 1.5m right | Close camera, good for aiming |
| **FirstPerson** | 0m | 1.7m (eye) | Center | Player POV, auto-switches when pushing cart |

---

## Mouse Look

| Parameter | Value |
|-----------|-------|
| Sensitivity | 2.0 |
| Min pitch | -30° (look up) |
| Max pitch | 75° (look down) |
| FP min pitch | -60° |
| FP max pitch | 70° |
| Follow speed | 12 (smoothing) |

---

## Target Switching

The camera follows different targets depending on context:

| Context | Target |
|---------|--------|
| Free roam | Player |
| Pushing cart | Cart (auto FirstPerson mode) |
| Controlling RC car | RC car |
| Hiding in cupboard | Cupboard |

`SetTarget(Transform)` is called by `CartInteraction`, `RCCarItem`, and `HidingSpot` to switch.

---

## First-Person Cart Mode

When pushing the cart:
- Camera auto-switches to FirstPerson
- Player sees from behind the cart at eye height
- `fpFreeYawRange = 30°` — player can look left/right freely within 30°
- Beyond 30°, the cart starts turning to follow the camera
- `fpCartTurnSpeed = 3` — how fast the cart follows the camera yaw
