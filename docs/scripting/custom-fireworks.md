# Writing a Custom Firework

For modders who want a firework that behaves in a way none of the shipped types can be configured into. This page assumes you already have a `.cs` file compiling into your mod — if not, start with [Setting Up Scripts in a Mod](setup.md).

---

## First — Do You Actually Need Code?

Most firework ideas are a shipped behavior plus a well-authored particle system. A cake, a rocket, a fountain, a shell, a smoke bomb, a zipper and a mortar are all already implemented and all take your own meshes, effects and sounds without a single line of C#.

Skip to the **"I want to make a…"** table at the bottom of this page first. If your idea is on it, close your code editor — you are done faster. Write a subclass only when you need a **launch sequence** that no shipped behavior performs.

---

## BaseFireworkBehavior

**Namespace:** `FireworksMania.Core.Behaviors.Fireworks`  
**Base Class:** `NetworkBehaviour`  
**Implements:** `IAmGameObject`, `ISaveableComponent`, `IHaveBaseEntityDefinition`, `IIgnitable`, `IHaveFuse`, `IHaveFuseConnectionPoint`, `IFiringSystemReceiver`

Every shipped firework except the mortar derives from this class. It already handles ignition, the fuse, replicating the launch to every peer, the destroy animation, blueprint save state and firing-system wiring. You inherit all of that and fill in one method.

### Inspector Fields You Inherit

| Field | Type | Description |
|---|---|---|
| **Entity Definition** | `FireworkEntityDefinition` | Under the **General** header. Required — `Awake` logs an error and gives up without it. |
| **Fuse** | `Fuse` | Required. The field is `protected`, so your subclass can use `_fuse` directly. |

That is the *entire* serialized surface of the base class. Every other field you see in the Inspector on a shipped firework comes from its concrete subclass.

### Members You Will Actually Use

| Member | Access | What it is for |
|---|---|---|
| `LaunchInternalAsync(CancellationToken)` | `protected abstract UniTask` | **The one member you must implement.** |
| `_fuse` | `protected Fuse` | The fuse assigned in the Inspector. |
| `_launchState` | `protected NetworkVariable<LaunchState>` | `IsLaunched`, `ServerStartTimeAsFloat`, `Seed`. Read by everyone, written by the server. |
| `_cancellationTokentoken` | `protected CancellationToken` | Cancelled when the object is destroyed. The typo is in the shipped source — that really is the field name. |
| `GetLaunchTimeDifference()` | `protected float` | Server time now minus the launch time. Feed this to `SetRandomSeed`. |
| `DestroyFireworkAsync(CancellationToken)` | `protected virtual UniTask` | Plays the destroy animation and then despawns the object. The whole body is server-only — on a client it returns immediately — so it is safe to call from code that runs on every peer, and the despawn replicates. |
| `ResetLaunchState()` | `protected virtual void` | Server-only. Clears the launch state back to `default`. |
| `GetFuse()`, `Ignite(float)`, `IgniteInstant()` | `public virtual` | Ignition entry points; all forward to the `Fuse`. |
| `CaptureState()` / `RestoreState(...)` | `public virtual` | Override **and call `base`** if you want to save your own data. |

---

## The One Member You Must Implement

```csharp
protected abstract UniTask LaunchInternalAsync(CancellationToken token);
```

That is the complete abstract surface of `BaseFireworkBehavior`. Everything else is concrete or virtual.

`UniTask` comes from `Cysharp.Threading.Tasks` and is already referenced by `Assembly-CSharp` — no package or assembly-definition setup is needed.

!!! warning "`LaunchInternalAsync` runs on every peer"
    Host **and** clients. Do not wrap it in `if (!IsServer) return;` — that would make the show invisible to everybody but the host. Anything authoritative (physics impulses, despawning) is either already server-gated inside the parts you call, or is your responsibility to gate. See [Multiplayer & Netcode](networking.md).

---

## The Lifecycle, End to End

| # | What happens | Runs on |
|---|---|---|
| 1 | Something calls `Ignite(force)` or `IgniteInstant()` on your firework. The base class forwards it to the `Fuse`. | the peer doing the igniting |
| 2 | The fuse marks the entity as no longer valid for saving, then subtracts `force` from its **Ignition Threshold**. If the threshold is still above zero nothing further happens — but the subtraction is kept. | same peer |
| 3 | Once the threshold reaches zero the fuse sends its ignite RPC to the server. | same peer |
| 4 | The server burns down the remaining fuse time. Spark particles and the fuse sound show on every peer via replicated state. | server drives, everyone sees |
| 5 | The fuse completes. On the **server only**, the base class writes `_launchState` with `IsLaunched = true`, the current server time, and a freshly rolled `Seed` byte. | server |
| 6 | `_launchState` replicates. On every peer the change handler starts `LaunchInternalAsync`. | every peer |
| 7 | **Your code runs.** | every peer |
| 8 | If you call `DestroyFireworkAsync`, the destroy animation plays and the object is despawned. Both happen on the server only; the despawn replicates, so the firework disappears everywhere. | server |

!!! note "Late joiners replay your launch"
    `OnNetworkSpawn` starts the launch immediately when `_launchState.Value.IsLaunched` is already `true`. A player joining mid-flight therefore runs `LaunchInternalAsync` from the beginning, but with a `GetLaunchTimeDifference()` of several seconds. Write your implementation so that starting late is survivable.

The ignition threshold is **cumulative and it never resets by itself** — repeated weak ignition sources chip away at it over the object's whole lifetime. A strong source also shortens the remaining burn. See [Firework Parts](../script-reference/firework-parts.md) for the fuse in detail.

---

## Why the Seed Matters

`LaunchState.Seed` is a single `byte`, rolled once on the server and replicated to everybody. It exists so that an effect which is *random* produces the *same* random on every machine — otherwise every player in a multiplayer session watches a different firework.

Feed it into your particle system with the `SetRandomSeed` extension method from `FireworksMania.Core.Common`:

```csharp
//public static void SetRandomSeed(this ParticleSystem particleSystem, uint randomSeed, float time = 0f)
_effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
```

What it does:

- Stops and clears the system, then — **only if that system's `Auto Random Seed` is still ticked** — switches auto seeding off and assigns the replicated seed. A system where you already unticked **Auto Random Seed** in the Inspector keeps the seed you authored.
- Recurses through the whole child hierarchy, bumping the seed by one for each particle system it walks into, so no two systems in the effect draw the same numbers.
- If `time` is greater than zero it calls `Simulate` on the root system (children included) to fast-forward it by that many seconds. That is what catches a late joiner up to the right point in the show. The system's GameObject has to be active first, or the simulate does nothing and you get a Console message about it.

!!! tip "Anything that must look identical everywhere has to come from the seed"
    `Random.Range` inside `LaunchInternalAsync` gives a different answer on every machine. That is fine for a tiny timing jitter — the shipped `RocketBehavior` uses a plain `Random.Range(0f, 0.1f)` delay before its explosion — but it is wrong for anything a player would notice, like burst shape, count or direction.

---

## A Complete Minimal Custom Firework

A ground firework whose entire show is one particle system, plus a `UnityEvent` so non-coders can hook lights or animations to the launch from the Inspector.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using FireworksMania.Core;                      // CoreSettings
using FireworksMania.Core.Behaviors.Fireworks;  // BaseFireworkBehavior
using FireworksMania.Core.Common;               // the SetRandomSeed extension
using UnityEngine;
using UnityEngine.Events;

namespace YourNick.YourMod
{
    [AddComponentMenu("Your Mod/Fireworks/Simple Firework")]
    public class SimpleFireworkBehavior : BaseFireworkBehavior
    {
        [Header("Simple Firework Settings")]
        [SerializeField]
        [Tooltip("The particle system that is the entire show")]
        private ParticleSystem _effect;

        [Header("Events")]
        [SerializeField]
        private UnityEvent _onLaunched;

        protected override void Awake()
        {
            base.Awake();   //Mandatory - see Sharp Edges below

            if (_effect == null)
            {
                Debug.LogError($"Missing Effect on '{this.gameObject.name}'", this);
                return;
            }

            StopEffect();
        }

        //Runs on EVERY peer, once the server has replicated the launch
        protected override async UniTask LaunchInternalAsync(CancellationToken token)
        {
            _onLaunched?.Invoke();

            _effect.gameObject.SetActive(true);
            _effect.SetRandomSeed(_launchState.Value.Seed, GetLaunchTimeDifference());
            _effect.Play(true);

            await UniTask.WaitWhile(() => _effect.IsAlive(true) || _effect.isPlaying, cancellationToken: token);
            token.ThrowIfCancellationRequested();

            //A drained ParticleSystem keeps ticking as long as its GameObject is active
            StopEffect();

            if (CoreSettings.AutoDespawnFireworks)
                await DestroyFireworkAsync(token);
        }

        private void StopEffect()
        {
            _effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _effect.gameObject.SetActive(false);
        }
    }
}
```

`CoreSettings.AutoDespawnFireworks` is a player setting. Honour it — a firework that always despawns takes the choice away from the player, and a firework that never despawns fills their map with junk.

!!! note "It looks like nothing happens when you press Play in the Mod Tools project"
    Every `CoreSettings` property is `false` until the main game populates it, so `AutoDespawnFireworks`-gated code does not run in the Editor. That is expected.

### Prefab Checklist

- [ ] Root GameObject has your behavior component
- [ ] **Entity Definition** assigned, and the definition's **Prefab Game Object** points back at this prefab (the build fails if this link is not two-way)
- [ ] **Fuse** assigned — use **GameObject → Fireworks Mania → Parts → Common → Standard Fuse Prefab** as a child, then drag its `Fuse` into the field
- [ ] A `Collider` so the player can aim at it
- [ ] A `Rigidbody` if the firework should be picked up or pushed around — every shipped template root has one
- [ ] `NetworkObject` and `ClientNetworkTransform` — add both with **GameObject → Fireworks Mania → Add Network Components**, which also adds a `ClientNetworkRigidbody` if the object already has a `Rigidbody`, so add the `Rigidbody` first
- [ ] `SaveableEntity` — added for you in the Editor by the base class's `OnValidate`, but check it is there and has the right definition

The fastest way to get all of this right is to start from a template: **GameObject → Fireworks Mania → Templates → Fireworks → …** and swap the behavior component. See [Templates & Sample Assets](../guides/templates-and-samples.md).

---

## Sharp Edges

### Renaming Your Class Breaks Every Existing Blueprint

!!! danger "Your C# class name is part of your mod's save format"
    `BaseFireworkBehavior` implements `SaveableComponentTypeId => this.GetType().Name`. That string is the key your firework's data is filed under inside every blueprint a player has ever saved. Rename `SimpleFireworkBehavior` to `SimpleFireworkV2Behavior` and every blueprint written with the old name silently loses that data on load.

    Treat the class name exactly like the definition **Id**: pick it once, before you publish, and never change it again. There is no aliasing or migration mechanism anywhere in the Mod Tools.

### `Awake` Is `protected virtual` — You Must Call `base.Awake()`

Overriding without calling base skips all of this:

1. The missing-`FireworkEntityDefinition` check.
2. The missing-`Fuse` check (which also disables the component).
3. Auto-adding `ErasableBehavior` so the player's Eraser Tool can remove your firework.
4. Finding the `SaveableEntity` and handing it to the fuse — without this the fuse cannot mark the entity unsaveable when it lights, so **burning fireworks end up in blueprints**.
5. Assigning `_cancellationTokentoken`, which is what stops your async launch when the object is destroyed.

The same applies to every other overridable. Match these signatures and call base in all of them:

| Callback | Signature in your subclass |
|---|---|
| `Awake` | `protected override void Awake()` |
| `Start` | `protected override void Start()` |
| `OnValidate` | `protected override void OnValidate()` |
| `OnDestroy` | `public override void OnDestroy()` |
| `OnNetworkSpawn` | `public override void OnNetworkSpawn()` |
| `OnNetworkDespawn` | `public override void OnNetworkDespawn()` |

`Start()` in the base class subscribes to `_fuse.OnFuseCompleted`. Skip `base.Start()` and your firework never launches at all.

### The `EntityDefinition` Setter Hard-Casts

```csharp
public BaseEntityDefinition EntityDefinition
{
    get => _entityDefinition;
    set => _entityDefinition = (FireworkEntityDefinition)value;
}
```

The property comes from `IHaveBaseEntityDefinition`, so it is typed as `BaseEntityDefinition` — but the setter casts straight to `FireworkEntityDefinition`. Assigning a `PropEntityDefinition` (or anything else deriving from `BaseEntityDefinition`) through this property throws an `InvalidCastException` at runtime.

In practice you assign the definition in the Inspector and never touch the setter. Just do not write generic "copy the definition across" helper code that goes through `IHaveBaseEntityDefinition`.

### Other Things Worth Knowing

| Gotcha | Detail |
|---|---|
| **No `[RequireComponent]` anywhere** | Nothing in the package uses it. Requirements are enforced in `Awake`/`OnValidate` — some with a `Debug.LogError`, some with `Preconditions.CheckNotNull`, which throws an `ArgumentNullException`. Both failure modes exist in shipped code. |
| **`ErasableBehavior` and `SaveableEntity` are auto-added in the Editor** | Dropping your behavior on a GameObject triggers the base `OnValidate`, which adds both. `NetworkObject`, `ClientNetworkTransform`, colliders and `Rigidbody` are **not** — several shipped subclasses add a `Rigidbody` from their own `OnValidate`, but the base class never does, so your subclass gets nothing for free. |
| **`IsIgnited` means "lit or already launched"** | The base implementation returns `true` when the fuse is ignited **or** when the replicated launch state says the firework has already launched. `ShellBehavior` overrides it to look at the fuse only. |
| **Firing-system wiring is saved for you** | `CaptureState` stores the module and cue indices when both are set. If you override `CaptureState`/`RestoreState`, call `base` or you lose that. |

---

## I Want to Make a…

The shipped behaviors and what each one needs. Every `[AddComponentMenu]` path is `Fireworks Mania/Behaviors/Fireworks/<ClassName>`.

| I want to make a… | Root component | Key parts and references |
|---|---|---|
| **Ground cake / battery** | `CakeBehavior` | `Fuse`, one **Effect** particle system (its `loop` is force-disabled), `Rigidbody` (throws if missing) |
| **Rocket** | `RocketBehavior` | **Model**, **Thruster**, **Explosion** (`ExplosionBehavior`), `Fuse`, `Rigidbody`. All five throw if missing. |
| **Rocket with hang time and whistles** | `RocketStrobeBehavior` — extends `RocketBehavior`, **not** `BaseFireworkBehavior` | Everything `RocketBehavior` needs, plus Start/End Whistle Sound and Hang Time |
| **Whistler / screamer** | `WhistlerBehavior` | **Model**, **Thruster**, **Explosion**, `Fuse`, `Rigidbody`, Whistling Sound, and a **trigger collider** — the explosion only happens if the player stepped on it before it lit |
| **Fountain** | `FountainBehavior` | `Fuse`, one **Effect**, Core Sound + End Sound (the **Start Sound** field exists but is never played) |
| **Roman candle** | `RomanCandleBehavior` | `Fuse`, one **Effect**, `Rigidbody` |
| **Firecracker / banger** | `FirecrackerBehavior` | **Model**, **Explosion**, `Fuse`, `Rigidbody`. Always despawns, and skips the destroy animation entirely. |
| **Smoke bomb** | `SmokeBombBehavior` | **Smoke Effect**, **Ignition Explosion Effect** (an `ExplosionPhysicsForceEffect`), `Fuse`, Sound. Ignites neighbours but applies no physics force or camera shake. |
| **Zipper / fan** | `ZipperBehavior` | **Model**, **Effect**, `Fuse`, `Rigidbody` — the fan pattern is authored entirely in the particle system, not driven by this behavior |
| **Preloaded / single-shot tube** | `PreloadedTubeBehavior` | **Effect**, `Fuse`, `Rigidbody` (the recoil is applied to it), Recoil Force, and an **On Launched** `UnityEvent` |
| **Mortar shell** | `ShellBehavior` | **Diameter**, **Model** + **Model Mesh Renderer**, **Effect**, **Launch Effect Prefab**, **Unwrapped Shell Fuse Prefab**, `Fuse`. Always despawns regardless of the player's setting. |
| **Mortar or mortar rack** | `MortarBehavior` — a plain `NetworkBehaviour`, **not** a `BaseFireworkBehavior` | Child `MortarTube`s, each with a `MortarTubeTop` (trigger collider), `MortarTubeBottom`, `UnwrappedShellFusePivotPosition` and a diameter. **No `Fuse` component** — each tube creates its own at runtime. |
| **Something none of the above can do** | your own `class X : BaseFireworkBehavior` | The base requirements: `FireworkEntityDefinition`, `Fuse`, `SaveableEntity`, `NetworkObject` — plus your implementation of `LaunchInternalAsync` |

Field-by-field detail for all of these lives in [Behaviors](../script-reference/behaviors.md) and [Firework Parts](../script-reference/firework-parts.md).

---

## Where to Go Next

| I want to… | Page |
|---|---|
| Understand when my component starts running | [Entry Points & Lifecycle](entry-points.md) |
| Make sure it behaves the same for host and clients | [Multiplayer & Netcode](networking.md) |
| Save my own state into blueprints | [Saving & Loading (Blueprints)](persistence.md) |
| Play a sound or react to a game event | [The Messenger Event Bus](messaging.md) |
| Start from a working prefab instead of an empty one | [Templates & Sample Assets](../guides/templates-and-samples.md) |
| Work out why my build failed | [Troubleshooting & Build Errors](../guides/troubleshooting.md) |
