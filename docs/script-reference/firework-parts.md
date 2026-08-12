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

| Field | Type | Default | Description |
|---|---|---|---|
| **Fuse Time** | `float` (0–50 s) | `4` | How long the fuse burns before completing. |
| **Ignition Threshold** | `float` | `50` | How much ignition force has to be applied before the fuse lights. See the warning below — this value is spent cumulatively. |
| **Fuse Connection Point** | `FuseConnectionPoint` | — | The child `FuseConnectionPoint` that is the physical attach point for other fuses. **Required** — `Awake()` fails hard without it. |
| **Particle System** | `ParticleSystem` | — | The spark/burn effect played while the fuse is burning. **Required** — `Awake()` fails hard without it. |
| **Fuse Ignited Sound** | `string` (`[GameSound]`) | — | Sound played when the fuse is lit. Leaving it empty logs an error from `OnValidate`. |
| **On Fuse Ignited** | `UnityEvent` | — | Fired when the fuse starts burning. |
| **On Fuse Completed** | `UnityEvent` | — | Fired when the fuse finishes burning. |

!!! warning "Ignition Threshold is cumulative and never resets itself"
    `Ignite(force)` **subtracts** `force` from the remaining threshold and lights the fuse only once it reaches zero. That subtraction is permanent for the lifetime of the object: three separate sparks of 20 force each will light a default 50-threshold fuse, even if they arrive minutes apart. Only the game's internal fuse reset restores the original value, and that method is `internal` — mod code cannot call it. A "harmless" weak ignition source applied repeatedly will eventually light anything.

!!! note "A strong ignition source also shortens the burn"
    At the moment the threshold is crossed, the remaining fuse time is reduced by `ignitionForce × Time.deltaTime` (clamped to the range 0 … Fuse Time). A powerful source therefore both lights the fuse and gives you slightly less of it.

### Key Events

| Event | Where it lives | Description |
|---|---|---|
| `OnFuseIgnited` | `IFuse` **and** `Fuse` | C# `event Action` raised the moment the fuse is lit. This is the only event `IFuse` declares. |
| `OnFuseCompleted` | `Fuse` only | C# `event Action` raised when the fuse burns out. `BaseFireworkBehavior` subscribes to this to trigger the launch. It is **not** on `IFuse` — you need a reference to the concrete `Fuse` class, or wire the **On Fuse Completed** UnityEvent in the Inspector instead. |

### Networking

`IsIgnited` and `IsUsed` are `NetworkVariable<bool>` values (read: everyone, write: server) replicated to all clients.

All three ignition entry points — `Ignite(float)`, `IgniteInstant()` and `IgniteWithoutFuseTime()` — are **callable from any peer**. They evaluate the threshold locally and then forward to the server themselves through a private `[Rpc(SendTo.Server)]`. The burn countdown and the completion event run server-side only. You do not need an `IsServer` check before lighting a fuse from your own script.

!!! warning "`IgniteWithoutFuseTime()` only removes the burn on the server"
    It zeroes the remaining fuse time on the peer that calls it, but that field is plain local state, not a `NetworkVariable` — and the countdown runs on the server. Called from a client, the fuse still burns its full **Fuse Time**. The ignition threshold works the same way: every peer subtracts from its own copy.

If you see the console error `Unable to call RequestIgniteServerOnly if not IsServer`, it comes from the fuse's own private server RPC — there is no public member by that name to call or fix.

---

## FuseConnectionPoint

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/FuseConnectionPoint`  
**Base Class:** `MonoBehaviour`  
**Implements:** `IFuseConnectionPoint`

Marks the physical location on a fuse (or firework) where another fuse can be connected. The player connects fuses between `FuseConnectionPoint`s with the fuse connection tool in-game.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Active Indicator** | `GameObject` | The visual switched on while the player holds the fuse connection tool and this fuse is still unused and unlit. **Required** — `Awake()` fails hard without it, and `OnValidate` logs `Missing active indicator on '<name>'`. |

`SetAsActiveSource(bool)` — from `IFuseConnectionPoint` — animates the **connection point's own transform**, not the indicator: `true` punch-scales it, `false` tweens it back to scale 1.

### Setup

Place a `FuseConnectionPoint` as a **child** of the GameObject carrying the `Fuse` component. The `Fuse` Inspector field **Fuse Connection Point** must reference this component.

!!! warning "Every connection point needs an owning fuse"
    `Fuse.Awake()` calls `Setup(this)` on the connection point it references. A `FuseConnectionPoint` that no fuse points at never gets that call and throws in `Start()`.

---

## Thruster

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/Thruster`  
**Base Class:** `NetworkBehaviour`

Provides the thrust for rocket-style fireworks. It normally sits on a child object of the firework root; the parent `RocketBehavior` calls `Setup(Rigidbody)` and then `TurnOn()` to activate it. The component sleeps until `TurnOn()` — `Start()` disables it.

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Thrust Force Per Second** | `float` | `2500` | Force applied per second while thrusting. |
| **Thrust Time** | `float` | `3` | Duration (seconds) of the thrust. The actual value is randomised ±10 % in `Awake()` for natural variation. |
| **Thrust Effect Curve** | `AnimationCurve` | flat at `1`, 0 s → 1 s | Multiplier applied to the thrust force as the burn progresses. The default is flat, i.e. constant force. |
| **Thrust Force Mode** | `ForceMode` | `Force` | Unity physics force mode. `Force` is the standard choice. |
| **Effect** | `ParticleSystem` | — | Particle system used for the exhaust visual. **Required** — `Awake()` logs `Missing at least one particle system on Thruster` without it. |
| **Thrust At Position** | `bool` | `false` | If `true`, force is applied at the thruster's own position (`AddForceAtPosition`), which can add torque. If `false`, it is applied to the whole rigidbody along the thruster's up axis. |
| **Thrust Sound** | `string` (`[GameSound]`) | — | Sound started when thrust begins and stopped when it ends, following the thruster's transform. Started and stopped on **every** peer from the replicated thrust state. |

!!! warning "The curve is evaluated in seconds, not 0–1"
    **Thrust Effect Curve** is sampled with the accumulated thrust time in **seconds**, not with normalised progress. A curve you author from 0 to 1 on the time axis is fully consumed in the first second of a three-second burn, and everything after that reads the curve's last key. Author the curve to span **Thrust Time** seconds.

### Methods

| Method | Description |
|---|---|
| `Setup(Rigidbody)` | Must be called before `TurnOn()`. Provides the rigidbody to apply force to. Without it, `TurnOn()` logs `Missing Rigidbody to apply thrust too! Did you forget to call Setup()?` and does nothing. |
| `TurnOn()` | Starts the thrust. **Server only** — a silent no-op on clients. |
| `TurnOff()` | Stops the thrust and disables the component. **Server only**. |
| `IsThrusting` | `bool` property, replicated, readable on every peer. |

---

## ExplosionBehavior

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionBehavior`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IExplosion`

Triggers an explosion: plays the explosion particle effect, plays the explosion sound, and activates the physics force effect. Used as a component on shells, aerial effects, and any firework that produces a burst.

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Explosion Particle Effect** | `ParticleSystem` | — | The burst particle system. **Required** — `Awake()` fails hard with `Explosion Particle System cannot be null on ExplosionBehavior` without it. Its GameObject is deactivated in `Awake()` and activated for the burst. |
| **Play On Start** | `bool` | `false` | If `true`, the server calls `Explode()` as soon as the object spawns. |
| **Force Explosion Always Up** | `bool` | `false` | If `true`, the force effect's transform is reset to world identity rotation just before the burst, so the explosion is oriented the same way no matter how the object ended up rotated. |
| **Delay In Seconds Between Sound And Explosion Effect** | `float` | `0` | The **sound plays first**, then the particle effect starts this many seconds later. Useful for shells where the bang reaches you before the burst is visible. |
| **Explosion Sound** | `string` (`[GameSound]`) | — | Sound to play on explosion. Broadcast on every peer at the object's position, with the distance delay enabled. |

### Requirements

`ExplosionBehavior` needs an `ExplosionPhysicsForceEffect` on the **same GameObject**. There is no `[RequireComponent]` — instead `OnValidate()` adds one for you in edit mode and logs `Added require ExplosionPhysicsForceEffect`. If it is still missing at runtime, `Awake()` fails hard.

### Networking

`Explode()` is **server-driven**: it returns immediately unless the peer is the server and the object is spawned. The server writes a replicated launch state containing a random `Seed`, and every peer then plays the same seeded particle burst, so all players see an identical explosion. `IsExploding` is readable everywhere.

---

## ExplosionPhysicsForceEffect

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ExplosionPhysicsForceEffect`  
**Base Class:** `MonoBehaviour`

The blast itself. Everything inside **Range** gets the full treatment: an outward push on rigidbodies, fire force on `IFlammable`s, damage on `IDestructible`s and ignition of nearby `IIgnitable`s — plus camera shake for the player, which uses its own multiplied range. Required alongside `ExplosionBehavior`, and also used on its own by `ParticleSystemExplosion`.

### Inspector Fields

Listed in Inspector order, grouped by their headers.

#### Explosion Effect

| Field | Type | Default | Description |
|---|---|---|---|
| **Range** | `float` | `0.2` | The radius the explosion has any effect inside. Only GameObjects inside are affected. |
| **Upwardsmodifier** | `float` | `0.25` | Adjustment to the apparent position of the explosion to make it seem to lift objects. (The one-word label is what the Inspector shows.) |
| **Force Mode** | `ForceMode` | `Impulse` | The method used to apply the force to its targets. |
| **Explosion Force** | `float` | `100` | The amount of explosion force applied to surrounding rigidbodies. |
| **Apply Force Relative To Mass** | `bool` | `true` | Scales the force by the target's mass, so heavy things do not fly like paper. |
| **Ignore Kinematic** | `bool` | `true` | Reads backwards: when `true`, kinematic rigidbodies **are** affected, and their `Is Kinematic` is switched off so they can fly. Objects tagged `Player` are left kinematic. |
| **Ignore Rigidbodies** | `Rigidbody[]` | empty | Rigidbodies to skip. `OnValidate` warns about entries left at `None`. |

#### Ignitable Effect

| Field | Type | Default | Description |
|---|---|---|---|
| **Ignite Surrounding Ignitables** | `bool` | `true` | Whether GameObjects with an ignitable component should be lit by the blast. |

#### Shake Effect

| Field | Type | Default | Description |
|---|---|---|---|
| **Enable Shake Effect** | `bool` | `true` | Whether the player gets camera shake when close enough. |
| **Shake Range Multipler** | `float` (0–100) | `1` | Range multiplier for the shake effect. (The misspelling is in the Inspector label itself.) |

#### Events

| Field | Type | Description |
|---|---|---|
| **On Apply Explosion Force** | `UnityEvent` | Invoked first thing when the force is applied, before any physics work, on whichever peer applies it. |

!!! tip "Use the gizmos to tune it"
    With the component selected, the red wire sphere is **Range** and the blue wire sphere is **Range × Shake Range Multipler**. Both defaults are small — `0.2` m is a firecracker, not a shell.

### Methods

```csharp
public void ApplyExplosionForce(bool applyPhysicsForce = true, bool applyShakeEffect = true, bool applyIgnition = true);
public void ApplyExplosionForce(Vector3 position, bool applyPhysicsForce = true, bool applyShakeEffect = true, bool applyIgnition = true);
```

The overload without a position uses the component's own `transform.position`.

### Limits and testing gotchas

- The overlap query writes into a **shared static 2500-entry collider buffer**. More than 2500 colliders inside **Range** are silently ignored.
- The affected layers are not authorable — the field is hidden and is unconditionally overwritten in `Awake()` with `Default`, `Interactable`, `Player` and `DestroyItDebris`.
- Of those four layers, **only `Default` exists in the Mod Tools project**. The rest are defined in the game. Explosions therefore cannot be tested faithfully in the editor.
- Forces, ignition and shake are not applied directly — they are broadcast as Messenger events that a game-side manager acts on. Nothing in the Mod Tools package listens for them, so an explosion you trigger in the editor will not actually push anything around. `IFlammable.ApplyFireForce` and `IDestructible.ApplyDamage` are the exceptions: those are called straight on the interface and do run in the editor.

---

## ParticleSystemObserver

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemObserver`  
**Base Class:** `MonoBehaviour`

Watches the `ParticleSystem` on the same GameObject and reports individual particles being born and dying. It has **no Inspector fields** — it exists so other components (and your own scripts) can hook into per-particle events.

Requires a `ParticleSystem` on the same GameObject. Both `ParticleSystemExplosion` and `ParticleSystemSound` require a `ParticleSystemObserver` as a sibling.

### Public members

| Member | Type | Description |
|---|---|---|
| `OnParticleSpawned` | `Action<Vector3>` | Called with the world position of each newly spawned particle. |
| `OnParticleDestroyed` | `Action<Vector3>` | Called with the world position of each particle that died. |

!!! warning "Subscribe with `+=`, never with `=`"
    These are public delegate **fields**, not C# `event`s. Assigning with `=` wipes out every other subscriber, including the game's own.

!!! example "Reacting to individual particles"
    ```csharp
    using FireworksMania.Core.Behaviors.Fireworks.Parts;
    using UnityEngine;

    [RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemObserver))]
    public class LogParticleDeaths : MonoBehaviour
    {
        private ParticleSystemObserver _observer;

        private void Awake()     => _observer = GetComponent<ParticleSystemObserver>();
        private void OnEnable()  => _observer.OnParticleDestroyed += HandleParticleDestroyed;
        private void OnDisable() => _observer.OnParticleDestroyed -= HandleParticleDestroyed;

        private void HandleParticleDestroyed(Vector3 worldPosition)
        {
            Debug.Log($"Particle died at {worldPosition}");
        }
    }
    ```

### Limits

- Every frame the observer allocates a fresh array the size of the system's current particle count and reads the whole system into it, then diffs that against the particles it saw last frame. The cost — and the garbage — scales directly with **Max Particles**, so keep observed systems small.
- The per-frame pass is skipped entirely while nothing has subscribed to either delegate, so an observer with no `ParticleSystemExplosion` or `ParticleSystemSound` beside it costs almost nothing.
- The observer disables itself permanently once its particle system has been alive and then finished. It does not switch itself back on.
- It runs in `Update()` on every peer. There is no networking here.

---

## ParticleSystemExplosion

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemExplosion`  
**Base Class:** `MonoBehaviour`

Fires an `ExplosionPhysicsForceEffect` **for every single particle** the sibling `ParticleSystemObserver` reports — once when a particle is born, once when it dies. It is how a burst of stars can each push and ignite things where they land.

It implements no interfaces. In particular it is **not** an `IExplosion` and cannot be used where an `IExplosion` is expected.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Particle Spawned Physics Effect** | `ExplosionPhysicsForceEffect` | Applied at the position of each spawned particle. Can be left blank if no effect is needed. |
| **Particle Destroyed Physics Effect** | `ExplosionPhysicsForceEffect` | Applied at the position of each destroyed particle. Can be left blank if no effect is needed. |

!!! danger "One full explosion pass per particle"
    Every particle event runs a **complete** `ExplosionPhysicsForceEffect` pass: an overlap query into the shared 2500-entry collider buffer, plus the rigidbody, flammable, destructible, ignition and shake passes. A system emitting a few hundred particles pays that cost a few hundred times, in one frame. Keep particle counts tiny, keep **Range** small, and leave both fields blank unless the effect genuinely needs per-particle forces. See [Optimization](../optimization.md).

### Requirements

A `ParticleSystemObserver` on the **same** GameObject.

!!! warning "The console message here is wrong"
    If the observer is missing, `Awake()` logs *"… was missing ParticleSystemObserver so it was added automatically"* — it is **not** added automatically; the line that would do it is commented out. Add the `ParticleSystemObserver` yourself. Without it the component throws a `NullReferenceException` on enable (as soon as either physics-effect field is assigned) and unconditionally on disable. `OnValidate` logs a separate, accurate error in the Editor. More misleading messages are collected in [Troubleshooting & Build Errors](../guides/troubleshooting.md).

---

## ParticleSystemSound

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemSound`  
**Base Class:** `MonoBehaviour`

Plays a sound for each particle born or killed by the observed `ParticleSystem`. Useful for giving individual particle emissions — crackling stars, tails, pops — their own sound.

Requires a `ParticleSystemObserver` on the same GameObject.

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Particle Spawned Sound** | `string` (`[GameSound]`) | — | Sound played for each spawned particle. |
| **Play Single Spawn Sound** | `bool` | `false` | Play the spawn sound only once, at the first event. Use this when the system spawns a lot of particles. |
| **Particle Destroyed Sound** | `string` (`[GameSound]`) | — | Sound played for each destroyed particle. |
| **Play Single Destroy Sound** | `bool` | `false` | Same idea for the destroy sound. |

- Leaving a sound blank costs nothing — an empty string is normalised to `[None]` and the component simply does not subscribe.
- The "play single" flags never reset. Once that one sound has played, this component instance will not play it again.
- Sounds are broadcast locally on each peer, positioned at the particle.

---

## ParticleSystemShellSound

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/ParticleSystemShellSound`  
**Base Class:** `ParticleSystemSound`

A `ParticleSystemSound` with one extra trick for shells: while the shell's effect is inside a mortar tube, the normal spawn sound is replaced by a separate in-tube sound. Because it derives from `ParticleSystemSound`, the Inspector shows **both** sets of fields.

### Additional Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Particle Spawned In Mortar Sound** | `string` (`[GameSound]`) | — | Sound played for each spawned particle while the effect is loaded into a mortar tube. |
| **Play Single Spawned In Mortar Sound** | `bool` | `false` | Play that sound only once, at the first event. |

The destroy sound is unaffected. You do not set the in-mortar flag yourself — `MortarTube` sets it on the copy of the shell's effect it instantiates when a shell is loaded.

---

## MortarTube

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `Fireworks Mania/Behaviors/Fireworks/Parts/MortarTube`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IIgnitable`, `IHaveFuse`, `IHaveFuseConnectionPoint`, `IAmGameObject`, `IFiringSystemReceiver`

One tube of a mortar-style firework. It swallows a shell, holds it, and launches it. A mortar with four tubes has four of these, each a child of the `MortarBehavior` root.

### Inspector Fields

| Header | Field | Type | Description |
|---|---|---|---|
| Size | **Diameter** | `EntityDiameterDefinition` | The diameter of the tube, used to work out whether a shell will fit. Assign one of the shipped `EntityDiameterDefinition` assets. Missing it logs `Missing EntityDiameterDefinition on <name>`. |
| Parts | **Mortar Tube Top** | `MortarTubeTop` | Where the shell is put into the tube and where it is shot out. |
| Parts | **Mortar Tube Bottom** | `MortarTubeBottom` | Where the shell sits once it is fully loaded. |
| Unwrapped Shell Fuse | **Unwrapped Shell Fuse Pivot Position** | `UnwrappedShellFusePivotPosition` | Where the unwrapped shell fuse pivots over the edge of the tube. |
| Sound | **Load Sound** | `string` (`[GameSound]`) | Played when a shell enters the tube. |

### Setup

- The `MortarTube` GameObject needs **at least one `Collider`** of its own, or the player cannot ignite, erase or fuse it. `Awake()` warns if there is none.
- It must be a child of something with a `SaveableEntity` — normally the `MortarBehavior` root, which finds all its tubes automatically and assigns each fuse an index.

!!! note "Do not add a Fuse to a mortar tube"
    The tube's fuse is **created at runtime**: `Awake()` loads a fuse prefab from `Resources` and instantiates it as a child. You never author it, and it is hidden until a shell is loaded.

### Public API

| Member | Description |
|---|---|
| `Ignite(float ignitionForce)` / `IgniteInstant()` | From `IIgnitable`. Both are no-ops unless a shell is loaded. |
| `GetFuse()` | Returns the tube's internal fuse, loaded or not. |
| `IsIgnited` | True once launched, or while the internal fuse burns. |
| `Enabled` | True while a shell is loaded. |
| `ConnectionPoint` | The internal fuse's connection point. |
| `DiameterDefinition` | The assigned `EntityDiameterDefinition`. |
| `Name` | `"<Mortar name>"`, or `"<Mortar name>\n(<Shell name>)"` while loaded. |
| `GameObject` | From `IAmGameObject`. |

The save/restore members are `internal` — mod code cannot call them.

### Behaviour

Everything about loading, launching and rejecting is **server-authoritative**; clients see the result through replicated state.

- A shell fits when its diameter is **less than or equal to** the tube's, and it is not already ignited.
- A loose fit is punished: launch speed is scaled by the shell/tube diameter ratio, with an extra penalty when the shell is smaller than the tube.
- Non-shell objects can be stuffed in and shot out again, with randomised fuse times.
- Rejected outright: anything already ignited, another mortar (no mortars inside mortars), objects on the `Player` layer, scene objects, anything without a `NetworkObject`, and anything whose upright renderer bounds exceed roughly three times the tube top's detection radius on **all three** axes at once — a long, thin object still goes in.
- Rejected objects that are not kinematic and not on the `Player` layer get a small upward impulse and a reject sound, so they visibly bounce back out.

---

## MortarTubeTop

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `GameObject/Fireworks Mania/Parts/Mortar/Mortar Top Prefab` — drops in the shipped, correctly configured prefab. The component itself has no `[AddComponentMenu]`, so adding it bare means the **Scripts** section of Add Component.  
**Base Class:** `MonoBehaviour`

Marks the **top** (muzzle) of the mortar tube. It is what actually detects a shell arriving, and it is the position the shell is shot out from. It has no Inspector fields.

!!! warning "It needs a trigger collider"
    `MortarTubeTop` requires at least one `Collider`, and at least one of them must have **Is Trigger** ticked. Without that, nothing can ever be loaded into the tube. The two `OnValidate` errors it logs both contain the source's own misspelling, *"requieres"* — search for that if you are hunting the message.

A `SphereCollider` is what the tube uses to judge how large an object may be; the shipped `MortarTubeTopPrefab` uses a trigger sphere with a radius of roughly 0.05 m. With no `SphereCollider` at all the tube falls back to a radius of `0.5`.

It exposes one public member, and it is there for `MortarTube` rather than for mods: the `OnTriggerEnterAction` event (`Action<Collider>`) that the tube subscribes to.

---

## MortarTubeBottom

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `GameObject/Fireworks Mania/Parts/Mortar/Mortar Bottom Prefab` — drops in the shipped prefab. No `[AddComponentMenu]` on the component itself.  
**Base Class:** `MonoBehaviour`

Marks the **bottom** of the mortar tube — the resting position of a fully loaded shell.

The class body is literally empty. It is a pure positional marker: nothing but its transform matters, and there is nothing to configure. Position it, and reference it from the tube's **Mortar Tube Bottom** field.

---

## UnwrappedShellFuse

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `GameObject/Fireworks Mania/Templates/Parts/Unwrapped Shell Fuse Template` — drops in an unpacked copy of the sample fuse to edit. No `[AddComponentMenu]` on the component itself.  
**Base Class:** `MonoBehaviour`

The exposed fuse that hangs out of a mortar tube once a shell is loaded, so the player can light the shell directly.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Ignite Position** | `Transform` | Where the ignite tool lights it and where the burning effect is shown. **Required** — `OnValidate` logs `Missing IgnitePosition on <name>` without it. |

The yellow gizmo arrow shows the ignite position and its up axis.

---

## UnwrappedShellFusePivotPosition

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks.Parts`  
**Menu:** `GameObject/Fireworks Mania/Parts/Mortar/Unwrapped Shell Fuse Pivot Position Prefab` — drops in the shipped prefab. No `[AddComponentMenu]` on the component itself.  
**Base Class:** `MonoBehaviour`

Defines where an `UnwrappedShellFuse` hangs over the edge of a tube. `MortarTube` instantiates the loaded shell's unwrapped fuse at this transform's position and rotation.

It shows **no fields** in the Inspector. Position it and reference it from the tube's **Unwrapped Shell Fuse Pivot Position** field.

!!! tip "Use the prefab, not a bare component"
    The yellow wire gizmo comes from a `[HideInInspector]` mesh reference that is only filled in on the shipped prefab. Add the component by hand from the **Scripts** section of Add Component and it works, but it draws no gizmo and you have nothing to aim with. Use the menu item above instead.

---

## Interfaces

These interfaces fall into three groups. Some you implement. Some the *game* implements and your mod consumes. A few are declared in the package but nothing anywhere calls them.

For signatures, samples and the `DependencyResolver` rules, see [Services & Interfaces](../scripting/services-and-interfaces.md).

### Implement these on your own components

| Interface | Members | Implement it when… |
|---|---|---|
| `IIgnitable` | `IgnitePositionTransform`, `Ignite(float)`, `IgniteInstant()`, `Enabled`, `IsIgnited` | you want a bespoke object that can be lit by a torch, a fuse or an explosion. Explosions look this up on the **Rigidbody's** GameObject. |
| `IExplosion` | `Explode()`, `IsExploding` | you are writing an exotic explosion. Most of the time, just add `ExplosionBehavior`. |
| `IFlammable` | `ApplyFireForce(float)` | your object should react to being near a blast. Nothing in the package implements this one — it is yours to write. Found on the **Collider's** GameObject. |
| `IDestructible` | `ApplyDamage(float)`, `IsDestroyed` | your object should take explosion damage. Also found on the **Collider's** GameObject. |
| `IErasable` | `Erase()` | you want to override what the Eraser tool does to your object. Fireworks get an `ErasableBehavior` added automatically, so you rarely need this. |
| `IAmGameObject` | `Name`, `GameObject` | you want a friendly name shown for your object in the game's UI. |

### Read these off other objects

| Interface | Members | Use it when… |
|---|---|---|
| `IFuse` | see below | you need to read or light a firework's fuse from script. |
| `IHaveFuse` | `GetFuse()` | you have a firework GameObject and need its fuse. It returns the `IFuse` interface, not the concrete `Fuse` component — so `Ignite(float)` and `OnFuseCompleted` are not on what you get back. |
| `IHaveFuseConnectionPoint` | `ConnectionPoint` | you need the `IFuseConnectionPoint` a fuse can be attached to. |
| `IFuseConnectionPoint` | `SetAsActiveSource(bool)`, `Fuse`, `Transform` | you are working with the attach point itself. |
| `IFiringSystemReceiver` | `OnFiringSystemReceiverDataUpdated`, `FiringSystemReceiverData`, `GetFiringSystemReceiverWorldPosition()` | your firework must be assignable to a Firing System module and cue. Deriving from `BaseFireworkBehavior` gives you this for free. |
| `IHaveEntityDiameterDefinition` | `DiameterDefinition` | your shell has to fit — or deliberately not fit — a given mortar calibre. |

#### IFuse members

| Member | Description |
|---|---|
| `event Action OnFuseIgnited` | The **only** event on this interface. |
| `bool IsUsed` / `bool IsIgnited` | Current fuse state. |
| `void IgniteInstant()` | Light it now, still burning for the full fuse time. |
| `void IgniteWithoutFuseTime()` | Light it now with no burn time — but only when the server calls it. See the `Fuse` networking note above. |
| `int Index` | Which fuse this is, when one saveable entity has several. Defaults to `0`. |
| `float FuseTime` | Burn duration, readable and writable. |
| `ParticleSystem Effect` / `string IgniteSound` | The fuse's spark effect and ignite sound. |
| `Transform Transform` | The fuse's transform. |
| `FuseNetworkIdentifier FuseNetworkIdentifier` | Network identity used to reference this fuse across peers. |
| `IFuseConnectionPoint ConnectionPoint` | Where other fuses attach. |
| `SaveableEntity SaveableEntityOwner` | The entity this fuse belongs to. Read-only through the interface; the concrete `Fuse` also exposes a setter. |

!!! warning "`IFuse` has no `Ignite()` and no `OnFuseCompleted`"
    `Ignite(float ignitionForce)` belongs to **`IIgnitable`**, which the concrete `Fuse` also implements — so `Ignite` works on a `Fuse` reference but not through an `IFuse` one. `OnFuseCompleted` exists only on the concrete `Fuse` class; through `IFuse` you can be told when a fuse is *lit*, not when it *finishes*.

### Provided by the game — consume, never implement

Nothing in the Mod Tools package implements these — the implementations live in the game. Your mod asks for one with `DependencyResolver.Instance?.Get<T>()`.

| Interface | Members |
|---|---|
| `IEnviroSkyManager` | `IsNight` |
| `IEntityDefinitionDatabase` | `GetEntityDefinition(string entityDefinitionId)` |
| `IDestructionObjectPool` | `GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)` |
| `IInputManager` | *(none — the interface is currently empty)* |
| `ICustomUIManager` | `ShowCanvas`, `HideCanvas`, `RegisterCanvas`, `UnregisterCanvas` |

!!! warning "Always null-check the result"
    Nothing in the Mod Tools project implements any of these, so `Get<T>()` returns `null` in the editor — and it returns `null` rather than throwing when the service is genuinely absent in game, too. It also only finds **active** components.

### Declared but unused — do not implement

The package declares these, so they compile, but nothing in it implements or calls them. Implementing one has no effect today.

| Interface | Status |
|---|---|
| `IExtinguishable` | Zero implementers, zero consumers. `Fuse` extinguishes itself privately without going through this. |
| `IHaveFusetime` | Zero implementers, zero consumers. Fuse duration is exposed as `IFuse.FuseTime` instead. |
| `IFuseConnectionMetadata` | Zero implementers, zero consumers in the package. Whatever would consume it lives on the game side. |
| `IPoolable` | Zero implementers, zero consumers. Pooling lives in the game. |
| `IShakeable` | `[Obsolete]` — camera shake now goes through the messaging system instead. |
| `IHaveObjectInfo` | `[Obsolete]` as an **error** — referencing it will not compile. Use `IAmGameObject`. |
