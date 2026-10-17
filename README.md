# THE HOUSE IS ALIVE (Meta Quest 3 / Mixed Reality Horror)

> **"Something followed you home."**  
> A psychological mixed-reality horror game where your actual room becomes the haunted architecture.

---

## Technical Stack
- **Engine**: Unity 6 (`6000.6.0f1`)
- **Platform**: Meta Horizon OS (Quest 3 & Quest 3S)
- **Target Architecture**: Android ARM64, Vulkan, ASTC
- **Core SDKs**:
  - `com.meta.xr.sdk.core` (v207.0.0): Tracking, OVRCameraRig, passthrough
  - `com.meta.xr.mrutilitykit` (v207.0.0): Room planes, semantic labels, anchor placement, occlusion
  - `com.meta.xr.sdk.audio` (v85.0.0): HRTF 3D spatial acoustic propagation behind physical walls
  - `com.meta.xr.sdk.interaction` (v207.0.0): Direct hand & gaze interaction
- **Dev Tooling**: `metavr` CLI + Meta XR Simulator (`/Applications/MetaXRSimulator.app`)

---

## Core Systems & Implementation

### 1. `DontLookAwayTracker.cs`
- Real-time dot-product gaze tracking against entity world positions.
- Distinguishes between direct observation cone (<60°) and outer peripheral vision (<100°).
- Dispatches `OnLookedAway` and `OnDirectGaze` events to drive creature state machines.

### 2. `SpatialKnockSequencer.cs`
- Places audio emitters slightly recessed behind detected physical room boundaries (`WALL_FACE`, `DOOR_FRAME`).
- Generates muffled 3D acoustics that simulate an entity physically knocking from inside the player's walls.

### 3. `FlickerLightingEffect.cs`
- Controls dynamic virtual room illumination and mixed-reality passthrough exposure.
- Simulates rapid brownouts and flickering lamps matching supernatural events.

### 4. `ShadowEntityController.cs`
- Finite State Machine (`Hiding`, `Peeking`, `Scurrying`, `Stalking`, `Frozen`).
- Advances toward the player only when unobserved; freezes or retreats into cover when looked at directly.

### 5. `RoomScareDirector.cs`
- Master encounter timeline manager.
- Sequences the three escalating phases of the 10-minute prototype across physical furniture anchors.

---

## Development & Testing Workflow

### 1. Testing in Meta XR Simulator (No Headset Required)
Launch the simulator directly on the Mac mini:
```bash
./build-quest.sh sim
```
Or via CLI:
```bash
open /Applications/MetaXRSimulator.app
```

### 2. Compiling the Quest 3 APK
```bash
./build-quest.sh build
```

### 3. Sideloading to Physical Quest 3
Connect Quest 3 via USB-C or Wi-Fi (`adb connect <quest-ip>:5555`), then:
```bash
./build-quest.sh install
```
