# Firework Parts

Firework parts are low-level components that you assemble inside a firework prefab to produce the desired behaviour. They work together with the firework behavior on the root object.

---

## Fuse

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/Fuse`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IFuse`, `IIgnitable`, `IHaveFuseConnectionPoint`

The fuse is the starting point of every firework. When ignited, it burns for a set duration and then triggers the firework's launch sequence.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Fuse Time** | `float` (0–50 s) | How long the fuse burns before completing. |
| **Ignition Threshold** | `float` | Minimum ignition force required to light the fuse. Higher values mean the fuse is harder to ignite accidentally. |
| **Fuse Connection Point** | `FuseConnectionPoint` | Reference to the child `FuseConnectionPoint` component that is the physical attach point for other fuses. |
| **Particle System** | `ParticleSystem` | The spark/burn particle effect played while the fuse is burning. |
| **Fuse Ignited Sound** | `string` ([GameSound]) | Sound played when the fuse is first ignited. |
| **On Fuse Ignited** | `UnityEvent` | Fired when the fuse starts burning. |
| **On Fuse Completed** | `UnityEvent` | Fired when the fuse finishes burning. |

### Key Events

| Event | Description |
|---|---|
| `OnFuseCompleted` | C# event raised when the fuse burns out. `BaseFireworkBehavior` subscribes to this to trigger the launch. |
| `OnFuseIgnited` | C# event raised the moment the fuse is lit. |

### Networking

`IsIgnited` and `IsUsed` are `NetworkVariable<bool>` values replicated to all clients. The fuse can only be ignited from the **server** side; client-side ignition requests are forwarded to the server via RPC.

---

## FuseConnectionPoint

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/FuseConnectionPoint`  
**Base Class:** `MonoBehaviour`  
**Implements:** `IFuseConnectionPoint`

Marks the physical location on a fuse (or firework) where another fuse can be connected. The player can connect fuses between `FuseConnectionPoint`s using the Fuse Tool in-game.

### Setup

Place a `FuseConnectionPoint` as a **child** of the `Fuse` component game object. The `Fuse` inspector field **Fuse Connection Point** must reference this component.

---

## Thruster

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/Thruster`  
**Base Class:** `NetworkBehaviour`

Provides the upward thrust for rocket-style fireworks. Must be on a child object of the firework root; the parent `RocketBehavior` calls `Setup(Rigidbody)` and then `TurnOn()` to activate it.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Thrust Force Per Second** | `float` | Force (Newtons) applied per second while thrusting. Default: `2500`. |
| **Thrust Time** | `float` | Duration (seconds) of the thrust. Actual thrust time is randomised ±10 % for natural variation. |
| **Thrust Effect Curve** | `AnimationCurve` | Controls how thrust magnitude varies over time. Default is linear (constant force). |
| **Thrust Force Mode** | `ForceMode` | Unity physics force mode. `Force` is the standard choice. |
| **Effect** | `ParticleSystem` | Particle system used for the rocket exhaust visual. |
| **Thrust At Position** | `bool` | If `true`, force is applied at the thruster's world position (uses `AddForceAtPosition`), which can add torque. If `false`, force is applied to the rigidbody's centre of mass. |
| **Thrust Sound** | `string` ([GameSound]) | Looping sound played during thrust. |

### Methods

| Method | Description |
|---|---|
| `Setup(Rigidbody)` | Must be called before `TurnOn()`. Provides the rigidbody to apply force to. |
| `TurnOn()` | Starts the thrust (server-side only). |
| `TurnOff()` | Stops the thrust (server-side only). |

---

## ExplosionBehavior

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionBehavior`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IExplosion`

Triggers an explosion: plays the explosion particle effect, plays the explosion sound, and activates the physics force effect. Used as a component on shells, aerial effects, and any firework that produces a burst.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Explosion Particle Effect** | `ParticleSystem` | The burst particle system. |
| **Play On Start** | `bool` | If `true`, the explosion fires immediately when the object activates. |
| **Force Explosion Always Up** | `bool` | If `true`, the explosion is always oriented upward regardless of the object's rotation. |
| **Delay Between Sound and Effect** | `float` | Seconds between the sound and the visual effect starting. Useful for shells where the bang is slightly before the burst. |
| **Explosion Sound** | `string` ([GameSound]) | Sound to play on explosion. Use the `Explosion` sound bus for large bursts. |

### Requirements

`ExplosionBehavior` requires an `ExplosionPhysicsForceEffect` component on the **same game object**.

---

## ExplosionPhysicsForceEffect

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionPhysicsForceEffect`  
**Base Class:** `MonoBehaviour`

Applies an outward physics impulse to all rigidbodies within a radius when an explosion occurs. Required alongside `ExplosionBehavior`.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Explosion Force** | `float` | The force (in Newtons) of the blast. |
| **Explosion Radius** | `float` | The radius (metres) within which rigidbodies are affected. |
| **Explosion Up Modifier** | `float` | Upward bias added to the explosion force. A value of `1` gives an equal upward component. |

---

## ParticleSystemExplosion

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemExplosion`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IExplosion`

A lighter-weight explosion component that only plays a particle effect and sound — without physics forces. Use this for small secondary effects where a full `ExplosionBehavior` would be excessive.

---

## ParticleSystemSound

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemSound`  
**Base Class:** `MonoBehaviour`

Plays a sound whenever a particle is born in the attached `ParticleSystem`. Useful for giving individual particle emissions (e.g. crackling stars) their own sound.

---

## ParticleSystemShellSound

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemShellSound`  
**Base Class:** `MonoBehaviour`

Similar to `ParticleSystemSound` but intended for shell launch effects. Plays a launch/whoosh sound in sync with the particle emission.

---

## MortarTube

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/MortarTube`  
**Base Class:** `MonoBehaviour`

The main tube component for mortar-style fireworks. Works together with `MortarTubeTop` and `MortarTubeBottom` to define the tube geometry and shell launch point.

---

## MortarTubeTop

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/MortarTubeTop`  
**Base Class:** `MonoBehaviour`

Marks the **top** (muzzle) of the mortar tube. The shell is launched from this transform.

---

## MortarTubeBottom

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/MortarTubeBottom`  
**Base Class:** `MonoBehaviour`

Marks the **bottom** of the mortar tube. Used for physics and visual grounding.

---

## UnwrappedShellFuse

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/UnwrappedShellFuse`  
**Base Class:** `MonoBehaviour`

An exposed (unwrapped) fuse that hangs out of a mortar tube, allowing the player to ignite a pre-loaded shell directly.

---

## UnwrappedShellFusePivotPosition

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/UnwrappedShellFusePivotPosition`  
**Base Class:** `MonoBehaviour`

Defines the pivot point of an `UnwrappedShellFuse`. Used to calculate the hanging position of the fuse relative to the tube.

---

## Interfaces

The following interfaces define the capabilities that the Mod Tools framework queries at runtime. Implement them on custom components to integrate with the game's systems.

| Interface | Description |
|---|---|
| `IFuse` | A component that acts as a fuse (has `Ignite()`, `IsIgnited`, `IsUsed`, burn events). |
| `IIgnitable` | Can be ignited by the player or by the Firing System. |
| `IExtinguishable` | Can be extinguished after being ignited. |
| `IHaveFuse` | Has a `Fuse` child component (`GetFuse()`). |
| `IHaveFuseConnectionPoint` | Exposes a `FuseConnectionPoint` for fuse-to-fuse connections. |
| `IHaveFusetime` | Exposes the fuse burn duration. |
| `IFuseConnectionPoint` | Marks a transform as a valid fuse connection attach point. |
| `IFuseConnectionMetadata` | Provides metadata about a fuse connection. |
| `IExplosion` | A component that can trigger an explosion (`Explode()`). |
| `IFiringSystemReceiver` | Can receive signals from the Firing System. |
| `IFlammable` | Can be set on fire. |
| `IShakeable` | Can be shaken/vibrated. |
| `IErasable` | Can be deleted by the Eraser Tool. |
| `IDestructible` | Can be destroyed by explosions or forces. |
| `IHaveEntityDiameterDefinition` | Exposes a diameter for collision/UI purposes. |
