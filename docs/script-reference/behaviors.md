# Behaviors

Behaviors are `MonoBehaviour` (or `NetworkBehaviour`) components you add to a prefab to give it functionality in Fireworks Mania. This page covers all behavior components provided by the Mod Tools.

---

## General Behaviors

These components can be added to any game object regardless of type.

---

### PlaySoundBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/PlaySoundBehavior`  
**Base Class:** `MonoBehaviour`

Plays a `GameSoundDefinition` sound. Can be triggered from code, a `UnityEvent`, or automatically on `Start`.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Sound** | `string` ([GameSound]) | Name of the `GameSoundDefinition` asset to play. |
| **Play On Start** | `bool` | If `true`, the sound starts playing immediately when the object becomes active. |
| **Follow Transform** | `bool` | If `true`, the audio source follows the transform as it moves. Only enable this when necessary (e.g. a moving rocket engine sound), as it has a small performance cost. |

#### Public Methods

| Method | Description |
|---|---|
| `PlaySound()` | Starts playing the sound. |
| `StopSound()` | Stops the sound. |
| `Toggle()` | Toggles between playing and stopped. |

---

### PlaySoundOnImpactBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/PlaySoundOnImpactBehavior`  
**Base Class:** `MonoBehaviour`

Plays a sound when the object receives a physics collision above a minimum impulse threshold. A short cooldown prevents the sound from spamming when the object settles.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Sound** | `string` ([GameSound]) | Name of the `GameSoundDefinition` to play on impact. |

#### Notes

- Requires a `Collider` on the same or a child game object.
- The velocity threshold and cooldown (`0.3 s`) are fixed values — they are not exposed in the Inspector.

---

### ToggleBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/ToggleBehavior`  
**Base Class:** `MonoBehaviour`

A simple on/off toggle that fires `UnityEvent`s. Useful for lights, doors, or anything that can be switched between two states.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Initial Toggle State** | `bool` | The state the object starts in (`true` = toggled on). |
| **On Toggle On** | `UnityEvent` | Invoked when the object is toggled on. |
| **On Toggle Off** | `UnityEvent` | Invoked when the object is toggled off. |

#### Public Methods

| Method | Description |
|---|---|
| `Toggle()` | Flips the current state. |
| `ToggleOn()` | Forces the on state and fires `OnToggleOn`. |
| `ToggleOff()` | Forces the off state and fires `OnToggleOff`. |

---

### UseableBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/UseableBehavior`  
**Base Class:** `MonoBehaviour`  
**Implements:** `IUseable`

Makes an object interactable by the player. When the player looks at an object with this component and presses the Use key, `BeginUse` is triggered. Releasing the key triggers `EndUse`.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Custom Text** | `string` | Optional text displayed below the interaction UI tooltip. |
| **Show Highlight** | `bool` | Whether to highlight the object when the player looks at it (default `true`). |
| **Show Interaction UI** | `bool` | Whether to show the interaction tooltip UI (default `true`). |
| **On Begin Use** | `UnityEvent` | Fired when the player begins using the object. |
| **On End Use** | `UnityEvent` | Fired when the player stops using the object. |

#### Combining with ToggleBehavior

A common pattern is to wire `OnBeginUse` to `ToggleBehavior.Toggle()` so that pressing Use toggles a light, door, or sound.

---

### ErasableBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/ErasableBehavior`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IErasable`

Allows the player to delete the object using the in-game Eraser Tool. This component is **automatically added** to any prefab that uses a `BaseFireworkBehavior`, so you only need to add it manually to prop prefabs.

!!! note
    Only one `ErasableBehavior` is allowed per game object (`[DisallowMultipleComponent]`).

---

### IgnorePhysicsToolBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnorePhysicsToolBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the object from being affected by the player's Physics Tool (the tool that pushes and pulls objects). Add this to objects that should remain stationary regardless of the Physics Tool.

---

### IgnorePickUpBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnorePickUpBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the player from picking up the object. Add this to large or anchored objects that should not be lifted.

---

### IgnoreExplosionPhysicsForcesBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnoreExplosionPhysicsForcesBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the object from being knocked around by explosion physics forces. Useful for permanent map props or fixed installations.

---

### DestructibleBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/DestructibleBehavior`  
**Base Class:** `MonoBehaviour`  
**Implements:** `IDestructible`

Marks an object as destructible (it can be broken by explosions or other forces). Combine with `IFlammable` for objects that also catch fire.

---

### DayNightCycleTriggerBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/DayNightCycleTriggerBehavior`  
**Base Class:** `MonoBehaviour`

Fires events at configurable times of day as the in-game day/night cycle progresses. Useful for automatically turning on lights at dusk or triggering other time-based effects.

---

## Firework Behaviors

All firework behaviors extend `BaseFireworkBehavior`. Place the correct component on the **root** game object of your firework prefab.

---

### BaseFireworkBehavior

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks`  
**Type:** `abstract NetworkBehaviour`  
**Implements:** `IIgnitable`, `IHaveFuse`, `IHaveFuseConnectionPoint`, `IFiringSystemReceiver`, `ISaveableComponent`

The abstract base class from which all firework behaviors inherit. You do not add this component directly — use one of the concrete subclasses below.

#### Required Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Entity Definition** | `FireworkEntityDefinition` | The definition asset for this firework. |
| **Fuse** | `Fuse` | Reference to the `Fuse` component on this prefab. |

#### Lifecycle

1. Player (or Firing System) calls `Ignite()` → fuse starts burning.
2. When the fuse burns out, `OnFuseCompleted` fires on the server.
3. The server sets `_launchState.IsLaunched = true` (replicated to all clients).
4. Each client calls `LaunchInternalAsync()` (implemented by the subclass).
5. The firework eventually destroys itself via `DestroyFireworkAsync()`.

#### Networking

Launch state is synchronised via `NetworkVariable<LaunchState>`. Only the **server** writes to this variable; all clients react to value changes. Subclasses should follow the same pattern for any additional networked state.

#### Firing System Integration

`BaseFireworkBehavior` implements `IFiringSystemReceiver`. When a `FiringSystemElectricFuse` is assigned in the Firing System panel, the firework ignites when the matching module/cue signal is sent.

---

### CakeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/CakeBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A multi-tube firework that fires a sequence of effects. Typically used for battery cakes with many individual tubes.

---

### RocketBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RocketBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A self-propelled firework. Requires a `Thruster` component on a child object to provide thrust.

---

### RocketStrobeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RocketStrobeBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A rocket variant that produces a strobe visual effect during flight.

---

### MortarBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/MortarBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A mortar tube that fires a shell upward. The shell (`ShellBehavior`) is a separate prefab that gets launched from the tube. Requires `MortarTube`, `MortarTubeTop`, and `MortarTubeBottom` components.

---

### ShellBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/ShellBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

Represents a mortar shell in flight. Typically contains an `ExplosionBehavior` to produce the burst effect at the top of the trajectory.

---

### RomanCandleBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RomanCandleBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A firework that fires multiple projectiles or stars sequentially from a single tube.

---

### FountainBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/FountainBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A stationary firework that produces a sustained shower of sparks without launching.

---

### FirecrackerBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/FirecrackerBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A small explosive cracker that produces a sharp bang and a small visual effect.

---

### SmokeBombBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/SmokeBombBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A smoke-generating device. Typically outputs a sustained stream of coloured smoke.

---

### WhistlerBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/WhistlerBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A firework that produces a whistle or screeching sound during flight.

---

### PreloadedTubeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/PreloadedTubeBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A tube-style firework that has its shells pre-loaded. Fires a sequence of shells on ignition.

---

### ZipperBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/ZipperBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A chain-reaction firework. Igniting one end starts a cascade that fires multiple effects in sequence along the zipper's length.
