# Behaviors

Behaviors are `MonoBehaviour` (or `NetworkBehaviour`) components you add to a prefab to give it functionality in Fireworks Mania. This page covers the general-purpose behaviors first, then the firework types. The smaller pieces they reference — fuses, thrusters, explosions, mortar tubes — live on [Firework Parts](firework-parts.md).

!!! note
    A component listed here as a `NetworkBehaviour` needs a `NetworkObject` on the prefab, and its replicated state only exists once the object is **spawned**. Several of them behave differently in a plain editor test scene, where nothing is spawned at all. Those cases are called out where they matter.

---

## General Behaviors

These components can be added to any GameObject regardless of type.

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
| **Play On Start** | `bool` | If `true`, `Start()` plays the sound. That is once, the first time the component runs — not again each time the object is re-enabled. |
| **Follow Transform** | `bool` | If `true`, the audio source follows the transform as it moves. Only enable this when necessary (e.g. a moving rocket engine sound), as it has a small performance cost. |

#### Public Methods

| Method | Description |
|---|---|
| `PlaySound()` | Starts playing the sound. |
| `StopSound()` | Stops the sound. |
| `Toggle()` | Toggles between playing and stopped. |

!!! note
    This component is **local**. It broadcasts a play/stop message on the in-process event bus and nothing is replicated, so in multiplayer only the machine that called `PlaySound()` hears it. To make a sound audible to everyone, trigger it on each peer yourself — see [Multiplayer & Netcode](../scripting/networking.md).

---

### PlaySoundOnImpactBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/PlaySoundOnImpactBehavior`  
**Base Class:** `MonoBehaviour`

Plays a sound when the object takes a physics collision hard enough to clear a fixed threshold. A short cooldown stops the sound from machine-gunning while the object settles.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Sound** | `string` ([GameSound]) | Name of the `GameSoundDefinition` to play on impact. |

#### Public Methods

| Method | Description |
|---|---|
| `PlaySingleImpactSound()` | Plays the impact sound once, ignoring the cooldown. |

#### Notes

- The gate is an **impulse** gate, not a velocity gate. The collision's `impulse.magnitude` must exceed `0.5`. Both this threshold and the `0.3 s` cooldown are hard-coded and not exposed in the Inspector.
- Detection is an ordinary `OnCollisionEnter` on this component, so put it where Unity delivers the collision callback — normally the GameObject that carries the `Rigidbody`.
- The component is **local**. It broadcasts the sound on the in-process event bus and nothing is replicated, so each machine plays the impacts its own physics simulation produced.

---

### ToggleBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/ToggleBehavior`  
**Base Class:** `MonoBehaviour`

A simple on/off toggle that fires `UnityEvent`s. Useful for lights, doors, or anything that can be switched between two states. This version is **purely local** — nothing is replicated, so in multiplayer each machine has its own state.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Initial Toggle State** | `bool` | Seeds the internal state at `Start` (default `true`). See the warning below — it does **not** drive the initial visuals. |
| **On Toggle On** | `UnityEvent` | Invoked when the object is toggled on. |
| **On Toggle Off** | `UnityEvent` | Invoked when the object is toggled off. |

#### Public Methods

| Method | Description |
|---|---|
| `Toggle()` | Flips the current state. |
| `ToggleOn()` | Forces the on state and fires `OnToggleOn`. |
| `ToggleOff()` | Forces the off state and fires `OnToggleOff`. |

!!! warning "Initial Toggle State does not apply itself"
    `Start()` assigns the boolean and fires **neither** `UnityEvent`. So a light wired to **On Toggle On**, with **Initial Toggle State** ticked, starts out visually *off* while the component believes it is *on* — and the first `Toggle()` turns it off a second time.

    Set the initial visual state yourself (leave the `Light` enabled in the prefab, for example), or use `ToggleNetworkBehavior` below, which does apply the initial state on spawn.

---

### ToggleNetworkBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/ToggleNetworkBehavior`  
**Base Class:** `NetworkBehaviour`

The replicated sibling of `ToggleBehavior`. Same three Inspector fields, same three public methods — but the state lives in a server-write `NetworkVariable<bool>`, so every player sees the same thing.

#### Behaviour Differences

- On spawn, the server writes **Initial Toggle State** into the network variable and **every peer** then invokes the matching `UnityEvent`. Unlike `ToggleBehavior`, the initial state really is applied.
- Any client may call `Toggle()` / `ToggleOn()` / `ToggleOff()`. The write is routed to the server by RPC, and the resulting value change fans back out to everyone.
- The prefab needs a `NetworkObject`, as with any `NetworkBehaviour`.

!!! tip
    Wire `UseableNetworkBehavior.OnBeginUse` to `ToggleNetworkBehavior.Toggle()` for a light switch that works correctly for host and clients alike. See [Multiplayer & Netcode](../scripting/networking.md) for the pattern behind it.

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

!!! note
    `UseableBehavior` is **local only** — nothing is replicated, so only the player who pressed Use sees the result. That is sometimes exactly what you want (a UI panel, a personal light). When everyone should see it, use `UseableNetworkBehavior` instead.

---

### UseableNetworkBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/UseableNetworkBehavior`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IUseable`

The replicated sibling of `UseableBehavior`. Identical Inspector fields (**Custom Text**, **Show Highlight**, **Show Interaction UI**, **On Begin Use**, **On End Use**), but the "in use" flag is a server-write `NetworkVariable<bool>`, so all players see the same state.

#### Behaviour Differences

- `BeginUse()` / `EndUse()` are callable from any client. Each sends an RPC to the server, which writes the network variable; every peer then invokes the matching `UnityEvent` from the value-changed handler.
- The prefab needs a `NetworkObject`.

!!! warning "On End Use fires once at spawn"
    `OnNetworkSpawn` subscribes to the value-changed handler and then calls it once with the current value, which is `false` at spawn. That means **On End Use** fires on every peer the moment the object spawns. Keep anything with a visible side effect (a sound, a door slam) off that event, or guard it.

---

### ErasableBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/ErasableBehavior`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IErasable`

Allows the player to delete the object using the in-game Eraser Tool. `BaseFireworkBehavior` **adds this for you** — from its `OnValidate` when you drop a firework behavior onto a prefab in the editor, and again from its `Awake()` at runtime if it is still missing. So you only need to add it by hand to props and to `MortarBehavior`, which is not a `BaseFireworkBehavior`.

It has no Inspector fields. The whole interface is one method:

| Method | Description |
|---|---|
| `Erase()` | Removes the object — despawning it when it is spawned, destroying it when it is not. |

!!! note
    Only one `ErasableBehavior` is allowed per GameObject (`[DisallowMultipleComponent]`).

    There is no erase animation yet. In the editor the component logs the warning `Todo: Implement nice Erase animation in ErasableBehavior` and the object simply disappears.

!!! warning "Erase the object from the server"
    On a spawned object `Erase()` despawns the `NetworkObject`, and Netcode for GameObjects only permits the server to despawn. Gate the call with `if (IsServer)`, or route it through an RPC. On an object that is **not** spawned — a plain prefab in a test scene — it is just destroyed locally, which is why it appears to work in the editor and then fails in a multiplayer game.

---

### IgnorePhysicsToolBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnorePhysicsToolBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the object from being affected by the player's Physics Tool (the tool that pushes and pulls objects). Add this to objects that should remain stationary regardless of the Physics Tool.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Ignore Type** | `PhysicsToolIgnoreTypes` | When the object should be ignored. Default is `OnlyWhenKinematicOnce`. |

#### Ignore Type Values

| Value | Meaning |
|---|---|
| `Always` | Always ignore the Physics Tool. |
| `OnlyWhenKinematic` | Ignore only while the object is marked Kinematic (Static). |
| `OnlyWhenKinematicOnce` | Ignore until the object has been non-Kinematic once. **Default.** |

!!! note
    The enum is nested inside the component, so from code you must write the full `IgnorePhysicsToolBehavior.PhysicsToolIgnoreTypes.Always` — a bare `PhysicsToolIgnoreTypes` does not resolve.

    This component also needs a `Rigidbody` on the same GameObject. There is no `[RequireComponent]`; instead `Start()` logs the warning `No Rigidbody found, disabling 'IgnorePhysicsToolBehavior'` and disables itself.

---

### IgnorePickUpBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnorePickUpBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the player from picking up the object. Add this to large or anchored objects that should not be lifted. It has no Inspector fields.

!!! tip
    The component reports "ignore me" only while it is **enabled**, so disabling it from a script should make the object pickable again — a cheap way to script a crate that can only be moved after some event. Test it in the game before relying on it.[^ignorepickup]

---

### IgnoreExplosionPhysicsForcesBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/IgnoreExplosionPhysicsForcesBehavior`  
**Base Class:** `MonoBehaviour`

Prevents the object from being knocked around by explosion physics forces. Useful for permanent map props or fixed installations. It is a pure marker — the class body is empty and there are no Inspector fields.

!!! note
    The explosion code looks for this component on the GameObject that carries the `Rigidbody` it is about to push. Put it anywhere else in the hierarchy and it does nothing.

---

### DestructibleBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** *(none — see the note below)*  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IDestructible`

Gives an object hit points, so explosions can break it and optionally swap it for a wreckage prefab.

!!! note "There is no Add Component menu entry"
    `DestructibleBehavior` carries no `[AddComponentMenu]`, so it does not appear under **Fireworks Mania** in the Add Component menu — it falls into Unity's default **Scripts** category. Typing the class name into the Add Component search box is the quickest way to find it.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Total Hit Points** | `float` | How much damage the object absorbs before it is destroyed. Default `0`. |
| **Current Hit Points** | `float` | Authoring value only — `Awake()` overwrites it with **Total Hit Points**. |
| **Ignore Damage Under** | `float` | Only damage bigger than this value is actually applied. Default `0`. |
| **Destroyed Prefab** | `GameObject` | Spawned to replace this GameObject when hit points reach 0. Leave empty to just destroy the object. |
| **Destroyed Prefab Spawn Location** | `Transform` | *Optional.* Used to position and rotate the destroyed prefab instance. |

#### Public API

| Member | Description |
|---|---|
| `ApplyDamage(float damage)` | Applies damage. |
| `TotalHitPoints` / `CurrentHitPoints` | `float`, get and set. |
| `Prefab` / `DestroyedPrefab` | The configured wreckage prefab. `Prefab` also has a setter. |
| `IsDestroyed` | `bool`, read-only. |

#### When damage actually lands

`ApplyDamage` does nothing unless **all four** of these hold:

1. You are the **server** (`NetworkManager.Singleton.IsServer`).
2. Destruction is enabled in the game's core settings (`CoreSettings.EnableDestruction`).
3. `damage` is greater than **Ignore Damage Under**.
4. The object is not already destroyed.

So calling `ApplyDamage` on a connected client is silently ignored.

!!! warning "It cannot be play-tested inside the Mod Tools project"
    Two things stop it working in the editor. The `IsServer` check dereferences `NetworkManager.Singleton` without a null guard, so calling `ApplyDamage` in a scene that has no `NetworkManager` throws a `NullReferenceException` rather than doing nothing. And the debris prefab is fetched from a destruction object pool that the **game** provides and the Mod Tools do not — so the swap only ever happens in the running game.

!!! danger "Officially still not recommended"
    The CHANGELOG entry that introduced this component (v2024.6.5) says *"don't use it yet really"*, and that has never been retracted. Treat `DestructibleBehavior` as unfinished: it works in the game, but the workflow around it is undocumented and may change.

---

### DayNightCycleTriggerBehavior

**Namespace:** `FireworksMania.Core.Behaviors`  
**Menu:** `Fireworks Mania/Behaviors/Other/DayNightCycleTriggerBehavior`  
**Base Class:** `MonoBehaviour`

Fires a `UnityEvent` when the game switches between day and night. This is the no-code way to turn a street lamp on at dusk.

It does **not** schedule anything at a configured hour. It listens for the game's `MessengerEventDayNightChanged` broadcast and reacts to it — see [The Messenger Event Bus](../scripting/messaging.md).

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Random Delay In Seconds** | `float` | Jitter, in seconds, between the day/night event arriving and your actions running. Default `0` (fire immediately). |
| **On Day Actions** | `UnityEvent` | Invoked when it becomes day. |
| **On Night Actions** | `UnityEvent` | Invoked when it becomes night. |

!!! tip "Use the jitter on rows of lamps"
    **Random Delay In Seconds** exists so a street of identical lamps does not switch in perfect lockstep. Each instance picks its own random delay between `0` and the value you set, so even a small value breaks up the uniformity.

#### Notes

- On `Awake` the component asks the game for the current sky state to seed itself. In the Mod Tools editor, and in any map without that manager, nothing answers — so no initial event fires and the component stays silent until the game broadcasts a change.
- `OnValidate` logs an error for any persistent listener on either `UnityEvent` whose method or target has gone missing. If you see one of those errors after refactoring a prefab, re-pick the target in the Inspector.

---

## Firework Behaviors

Place the correct component on the **root** GameObject of your firework prefab. Every `[AddComponentMenu]` in this section is `Fireworks Mania/Behaviors/Fireworks/<ClassName>`.

Most, but not all, of them extend `BaseFireworkBehavior`:

| Component | Extends | Has a `Fuse` field? |
|---|---|---|
| `CakeBehavior` | `BaseFireworkBehavior` | Yes |
| `RocketBehavior` | `BaseFireworkBehavior` | Yes |
| `RocketStrobeBehavior` | **`RocketBehavior`** | Yes (inherited) |
| `ShellBehavior` | `BaseFireworkBehavior` | Yes |
| `RomanCandleBehavior` | `BaseFireworkBehavior` | Yes |
| `FountainBehavior` | `BaseFireworkBehavior` | Yes |
| `FirecrackerBehavior` | `BaseFireworkBehavior` | Yes |
| `SmokeBombBehavior` | `BaseFireworkBehavior` | Yes |
| `WhistlerBehavior` | `BaseFireworkBehavior` | Yes |
| `PreloadedTubeBehavior` | `BaseFireworkBehavior` | Yes |
| `ZipperBehavior` | `BaseFireworkBehavior` | Yes |
| `MortarBehavior` | **`NetworkBehaviour`** | **No** |

`MortarBehavior` is a holder — it contains *other* fireworks rather than being one — so it has no fuse, no launch state and no `LaunchInternalAsync`. Do not expect base-class behaviour from it.

---

### BaseFireworkBehavior

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks`  
**Type:** `abstract NetworkBehaviour`  
**Implements:** `IAmGameObject`, `ISaveableComponent`, `IHaveBaseEntityDefinition`, `IIgnitable`, `IHaveFuse`, `IHaveFuseConnectionPoint`, `IFiringSystemReceiver`

The abstract base class most firework behaviors inherit from. You do not add this component directly — use one of the concrete subclasses below, or write your own subclass, which is covered in [Writing a Custom Firework](../scripting/custom-fireworks.md).

The single member a subclass must implement is `protected abstract UniTask LaunchInternalAsync(CancellationToken token)`. Everything else — fuse wiring, ignition, save state, replication — the base class handles.

#### Required Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Entity Definition** | `FireworkEntityDefinition` | The definition asset for this firework. |
| **Fuse** | `Fuse` | Reference to the `Fuse` component on this prefab. |

A `SaveableEntity` is required too. `OnValidate` adds one in the editor and copies the **Entity Definition** onto it; at runtime `Awake()` only checks for it and logs `Missing 'SaveableEntity' which is a required component` if it is gone.

#### Lifecycle

1. Something calls `Ignite(float ignitionForce)` or `IgniteInstant()` → the fuse starts burning. The burn itself is timed on the server.
2. When the fuse burns out, `OnFuseCompleted` fires on the server.
3. The server writes `_launchState` — `IsLaunched = true`, the server start time, and a random `Seed` byte — and that is replicated to everyone.
4. **Every peer**, server included, runs `LaunchInternalAsync()` (implemented by the subclass) off the value change.
5. Most subclasses then destroy the firework via `DestroyFireworkAsync(token)`, which plays the shrink-away destroy animation and despawns — both **server-side only**; it returns immediately on a client. Most of them only do this when the game's auto-despawn setting (`CoreSettings.AutoDespawnFireworks`) is on; shells and firecrackers always despawn.

#### Networking

Launch state is synchronised via `NetworkVariable<LaunchState>`. Only the **server** writes to this variable; every peer reacts to the value change. Subclasses should follow the same pattern for any additional networked state — see [Multiplayer & Netcode](../scripting/networking.md).

The replicated `Seed` is what keeps the show identical on every machine — don't let each peer roll its own randomness. Most shipped particle-based fireworks do the same thing:

```csharp
_effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
```

`SetRandomSeed` is an extension method on `ParticleSystem` from `FireworksMania.Core.Common`, not a Unity API. It walks the system and its child systems and pins the seed on each one — but only on systems that still have **Auto Random Seed** ticked, so a system where you unticked it keeps the seed you authored. The second argument fast-forwards the simulation by that many seconds, which is how a client that joins mid-flight picks the effect up at the right point instead of restarting it. `GetLaunchTimeDifference()` is `protected` on the base class and returns server time minus the replicated launch time.

!!! danger "Renaming a firework class breaks saved blueprints"
    `SaveableComponentTypeId` returns `GetType().Name`, so your C# class name **is** the key written into players' blueprint files. Rename the class and every blueprint that contains your firework loses that component's data. Treat the class name like the definition Id: pick it once, keep it forever. See [Saving & Loading (Blueprints)](../scripting/persistence.md).

#### Firing System Integration

`BaseFireworkBehavior` implements `IFiringSystemReceiver`, so every firework can be wired to a firing system cue with no work on your part. There is nothing to author on the prefab.

The module and cue the player assigns in the in-game firing system UI live in `FiringSystemReceiverData` — two `byte`s, replicated in their own `NetworkVariable` and written into the blueprint. On the **server only**, the firework listens for `MessengerEventFiringSystemControllerSendSignal`; when the module and cue in the signal match its own, and its fuse is neither ignited nor already used, the fuse is lit with `IgniteWithoutFuseTime()` — no fuse delay.

Because that listener is server-gated, broadcasting the signal struct yourself on a client fires nothing.

---

### CakeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/CakeBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A stationary ground firework — a battery cake. The whole show is one **Effect** particle system (normally with sub-emitters doing the individual shots); the cake itself never moves. `Awake()` force-disables looping on that effect, and both the `Rigidbody` and the effect reference are required — the component throws if either is missing.

---

### RocketBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RocketBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A self-propelled firework. Thrust comes from a `Thruster` component elsewhere on the prefab — normally a child object — assigned to the **Thruster** field.

All five of **Model**, **Thruster**, **Fuse**, **Explosion** and a `Rigidbody` are required — `Awake()` throws if any of them is missing, so a half-wired rocket fails loudly rather than misbehaving. The `Rigidbody` is auto-added in the editor when you drop the component on.

There is one more field, **Random Time Delay After Thruster** (`bool`, default `true`), which adds up to `0.1 s` of jitter between the thruster burning out and the explosion. Leave it on unless you have a specific reason.

---

### RocketStrobeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RocketStrobeBehavior`  
**Type:** `NetworkBehaviour` (extends **`RocketBehavior`**)

A rocket variant with a whistle and a hang time before the burst. It inherits every `RocketBehavior` field and adds **Start Whistle Sound**, **End Whistle Sound**, and **Hang Time In Seconds After Thruster Finish** (default `1.5`).

It reuses the inherited **Random Time Delay After Thruster** for something else: instead of adding up to `0.1 s`, it multiplies the hang time by a random `0.9`–`1.1`. Turn it off and every strobe in a rack bursts at exactly the same moment.

Because it extends `RocketBehavior` rather than `BaseFireworkBehavior` directly, a subclass of your own inherits the rocket requirements too: Model, Thruster, Fuse, Explosion and a `Rigidbody`.

---

### MortarBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/MortarBehavior`  
**Type:** `NetworkBehaviour` (**not** a `BaseFireworkBehavior`)  
**Implements:** `ISaveableComponent`, `IHaveBaseEntityDefinition`, `IIgnitable`, `IHaveEntityDiameterDefinition`

A mortar rack. It holds one or more child `MortarTube` components, each of which can swallow a shell and fire it.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Entity Definition** | `FireworkEntityDefinition` | The definition asset for this mortar. |
| **Diameter** | `EntityDiameterDefinition` | Used to work out which shells fit. |

The tube list itself is `[HideInInspector]` and auto-populated from child GameObjects that carry a `MortarTube`. `Awake()` throws if it finds none, so a mortar must have at least one tube.

!!! note "No Fuse component"
    A mortar has no **Fuse** field. Each `MortarTube` loads its own internal fuse prefab from `Resources` at runtime, so there is nothing to author and nothing to assign. `Ignite()` forwards to the first tube that is enabled and not already lit.

    Because it is not a `BaseFireworkBehavior`, none of the base-class conveniences apply: no launch state, no `LaunchInternalAsync`, no auto-added `ErasableBehavior`. Add `ErasableBehavior` and `SaveableEntity` yourself.

See [Firework Parts](firework-parts.md) for `MortarTube`, `MortarTubeTop` and `MortarTubeBottom`.

---

### ShellBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/ShellBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)  
**Implements:** `IHaveEntityDiameterDefinition`

A mortar shell. It can be fired out of a `MortarTube`, or lit on the ground.

The shell body never flies under its own power. On launch it goes kinematic, disables its colliders, hides its **Model** and plays its **Effect** in place — the height comes entirely from the effect's particles. `Awake()` writes **Ground Launch Force** into that effect's start speed and forces looping off, so **Ground Launch Force** is really "how hard the burst throws its stars", whether the shell was fired from a tube or lit on the ground.

The burst is **not** an `ExplosionBehavior` on the root. In the shipped shell template the entire show lives inside the shell's **Effect** particle system, with `ParticleSystemObserver`, `ParticleSystemExplosion` and `ExplosionPhysicsForceEffect` on effect child objects doing the force and ignition work.

#### Key Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Diameter** | `EntityDiameterDefinition` | Decides which mortar tubes will accept this shell. |
| **Model** / **Model Mesh Renderer** | `GameObject` / `MeshRenderer` | The shell body. The renderer is cloned to draw the shell sitting in a tube. |
| **Ground Launch Force** | `float` | Copied into **Effect**'s start speed in `Awake()`, so it sets how far the burst throws. Default `10`. |
| **Effect** | `ParticleSystem` | The burst. Its start speed is also what the mortar reads back as the recoil impulse, so raising **Ground Launch Force** kicks the rack harder too. |
| **Launch Effect Prefab** | `ParticleSystem` | Muzzle effect used when the shell is fired from a tube. |
| **Unwrapped Shell Fuse Prefab** | `GameObject` | The fuse model that hangs over the tube edge. Must have an `UnwrappedShellFuse` component. |

!!! note
    A shell always despawns after its burst — it ignores the game's auto-despawn setting, unlike most other firework types.

---

### RomanCandleBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/RomanCandleBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A tube firework that shoots stars in sequence. Mechanically it is the same shape as `CakeBehavior` — one **Effect** particle system does the whole show — so the sequencing comes from the particle system's own bursts, not from separate projectile objects. Unlike the cake, its effect's looping is *not* force-disabled, so switch looping off yourself.

---

### FountainBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/FountainBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A stationary firework that produces a sustained shower of sparks without launching. It takes an **Effect** particle system and three sound fields: **Start Sound**, **Core Sound** and **End Sound**.

!!! warning "Start Sound does nothing"
    The **Start Sound** field is serialized and shown in the Inspector, but nothing in the class ever plays it. Only **Core Sound** (played for the duration of the effect) and **End Sound** (played as the effect stops emitting) are used. Put your ignition sound on the `Fuse` instead.

---

### FirecrackerBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/FirecrackerBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A small explosive cracker: it hides its **Model**, fires its **Explosion**, and is gone. **Model**, **Fuse**, **Explosion** and a `Rigidbody` are all required and throw if missing.

!!! note
    A firecracker always despawns after exploding, and it skips the shrink-away destroy animation other fireworks play — it just disappears. It also ignores the game's auto-despawn setting.

---

### SmokeBombBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/SmokeBombBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A smoke-generating device. It takes a **Smoke Effect** particle system, a **Sound**, and an **Ignition Explosion Effect** — an `ExplosionPhysicsForceEffect`, not an `ExplosionBehavior`.

!!! note
    On ignition the smoke bomb uses that effect to **ignite nearby ignitables only**. It deliberately applies no physics force and no camera shake, so a smoke bomb will light your fireworks but will not blow anything over.

---

### WhistlerBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/WhistlerBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A firework that thrusts along the ground while playing a **Whistling Sound**. It takes a **Model**, a **Thruster** and an **Explosion** reference.

!!! note
    The **Explosion** only ever fires if the player *stepped on* the whistler before its thruster burned out. Detection is an `OnTriggerEnter` that only counts colliders tagged `Player`, so the prefab needs a trigger collider — nothing in code enforces that it has one. An ordinary whistler just thrusts, whistles, and stops.

---

### PreloadedTubeBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/PreloadedTubeBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A single-shot tube. On ignition it plays one **Effect** particle system and applies a recoil impulse to its own `Rigidbody`, so the tube kicks as it fires. It does not fire a sequence of shells — whatever pattern you want has to live inside that one particle system.

#### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Effect** | `ParticleSystem` | The shot. |
| **Recoil Force** | `float` | Impulse applied along the tube's negative up axis, at the effect's position. Default `100`. |
| **On Launched** | `UnityEvent` | Invoked on every peer just before the effect starts. |

!!! warning
    The recoil is applied on **every peer**, not just the server. If you also drive the tube's position from a networked transform, expect the two to fight. Keep the recoil modest.

---

### ZipperBehavior

**Menu:** `Fireworks Mania/Behaviors/Fireworks/ZipperBehavior`  
**Type:** `NetworkBehaviour` (extends `BaseFireworkBehavior`)

A ground firework that goes rigid, hides its **Model** and plays a single **Effect** particle system. It is *not* a chain reaction and it does not cascade along its own length — the familiar fan or sweep pattern has to be authored inside that one particle system.

`ZipperBehavior` forces the effect's rotation to identity before playing it, so the pattern is effectively authored in world space and does not follow however the zipper ended up lying on the ground.

---

## Legacy Firing System Components

The Mod Tools still ship a set of components from the original in-world firing system: `FiringSystemControllerBehavior`, `FiringSystemReceiverMultiCueBehavior` and `FiringSystemReceiverSingleCueBehavior`. All three are marked `[Obsolete("Replaced by UI version of firing system")]` and none of them has an Add Component menu entry.

Do not build new content on them. The firework side of the modern firing system is already handled for you: `BaseFireworkBehavior` and `MortarTube` are the two `IFiringSystemReceiver` implementations, and the player wires cues up through the in-game UI.

`FiringSystemElectricFuse` is *not* obsolete, but it is not part of that path either — it is the socket the old in-world controller plugged into, it implements `IFuse` rather than `IFiringSystemReceiver`, and two of its `IFuse` members (`Effect` and `IgniteSound`) throw `NotImplementedException`. It has no Add Component menu entry. Leave it alone.

[^ignorepickup]: `IgnorePickUpBehavior.ShouldBeIgnored` returns `false` when the component is disabled — that much is in the Mod Tools source. The code that *asks* the question lives in the game, not in this package, so whether the pick-up system re-checks it every frame could not be verified here.
