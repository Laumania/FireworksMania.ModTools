# Services & Interfaces

The Mod Tools package declares 31 public interfaces, and they are not all the same kind of thing. This page sorts the ones that matter into three buckets — the ones you implement, the ones you ask the game for, and the ones you should leave alone.

---

## Which bucket am I in?

| Bucket | What it means | Examples |
|---|---|---|
| **1. Implement** | You write a component that implements the interface. Game systems find it with `GetComponent` / `TryGetComponent` and call into it. | `IUseable`, `IDestructible`, `IFlammable`, `IIgnitable`, `IErasable`, `ISaveableComponent` |
| **2. Consume** | Nothing in the Mod Tools implements it — the **game** does. You ask `DependencyResolver` for an instance and call it. | `IEnviroSkyManager`, `ICustomUIManager`, `IEntityDefinitionDatabase`, `IDestructionObjectPool` |
| **3. Leave alone** | Declared in the package, but with zero implementers and zero consumers — or explicitly obsolete. | `IPoolable`, `IHaveFusetime`, `IFuseConnectionMetadata`, `IExtinguishable`, `IShakeable` |

Everything below lives in the `FireworksMania.Core` assembly, which is auto-referenced — a script in your mod can `using` it with no setup at all. See [Setting Up Scripts in a Mod](setup.md).

---

## 1. Implement these on your own components

| Interface | Namespace | Use this when… | Members |
|---|---|---|---|
| `IUseable` | `FireworksMania.Core.Behaviors` | The player's Use key should do something on your object, with the interaction prompt and highlight | `BeginUse()`, `EndUse()`, `IsInUse`, `ShowHighlight`, `ShowInteractionUI`, `CustomText`, `GameObject` |
| `IDestructible` | `FireworksMania.Core.Behaviors` | Your object should take explosion damage and break | `ApplyDamage(float)`, `IsDestroyed` |
| `IFlammable` | `FireworksMania.Core.Behaviors` | Your object should react to being near an explosion — catch fire, char, ignite an effect | `ApplyFireForce(float)` |
| `IIgnitable` | `FireworksMania.Core.Behaviors.Fireworks.Parts` | Your object should be lightable by a torch or an explosion, but is not a firework | `IgnitePositionTransform`, `Ignite(float)`, `IgniteInstant()`, `Enabled`, `IsIgnited` |
| `IErasable` | `FireworksMania.Core.Behaviors` | You want to override what the in-game Eraser tool does to your object | `Erase()` |
| `ISaveableComponent` | `FireworksMania.Core.Persistence` | Your object carries state that should survive a blueprint save | `CaptureState()`, `RestoreState(CustomEntityComponentData)`, `SaveableComponentTypeId` |

`ISaveableComponent` has a page of its own — see [Saving & Loading (Blueprints)](persistence.md).

### Shipped components you can use instead of writing code

| Interface | Component | Menu |
|---|---|---|
| `IUseable` | `UseableBehavior` (local, not synced) | `Fireworks Mania/Behaviors/Other/UseableBehavior` |
| `IUseable` | `UseableNetworkBehavior` (server-authoritative) | `Fireworks Mania/Behaviors/Other/UseableNetworkBehavior` |
| `IErasable` | `ErasableBehavior` | `Fireworks Mania/Behaviors/Other/ErasableBehavior` |
| `IDestructible` | `DestructibleBehavior` | *no* `[AddComponentMenu]` — it shows up under Unity's default **Scripts** category |

Both `IUseable` components expose the same Inspector surface — **Custom Text**, **Show Highlight**, **Show Interaction UI**, and `OnBeginUse` / `OnEndUse` UnityEvents — so a lot of "usable object" work is zero code. `ErasableBehavior` is added automatically to every firework deriving from `BaseFireworkBehavior`: `OnValidate` adds one in the Editor, and `Awake()` adds one at runtime if the GameObject still has no `IErasable`.

!!! warning "A custom IErasable does not stop the Editor from adding ErasableBehavior"
    The runtime check in `BaseFireworkBehavior.Awake()` skips the add when the GameObject already has *any* `IErasable`. The Editor check in `OnValidate` looks for the concrete `ErasableBehavior` only, so it adds one anyway and you end up with two `IErasable` components on the prefab. If you are replacing the eraser behaviour, delete the auto-added `ErasableBehavior` from the prefab by hand.

!!! tip "IUseable is easy to miss"
    It does not have its own file. `IUseable` is declared at the top of `Behaviors/UseableBehavior.cs`, which is why searching the project for `IUseable.cs` comes up empty.

### The lookup rule that trips everyone up

Explosions do not search your hierarchy. They query the exact GameObject they hit, and *which* GameObject that is depends on the interface:

| Interface | Found on | Consequence |
|---|---|---|
| `IDestructible` | the **Collider's** GameObject | A destructible on the parent of a collider is never found |
| `IFlammable` | the **Collider's** GameObject | Same |
| `IIgnitable` | the **Rigidbody's** GameObject | Must sit next to the `Rigidbody`, not next to the collider |

!!! warning "Colliders on children"
    The common setup — a root with the `Rigidbody` and a child with the mesh and its collider — puts these on two different GameObjects. Put your `IDestructible` / `IFlammable` on the child that has the collider, and your `IIgnitable` on the root that has the rigidbody. One component that implements all three only works if the collider and rigidbody happen to be on the same GameObject.

### Sample — a light switch the player can use

```csharp
using FireworksMania.Core.Behaviors;
using UnityEngine;

namespace YourNick.MyMod
{
    [AddComponentMenu("My Mod/Light Switch")]
    public class LightSwitch : MonoBehaviour, IUseable
    {
        [SerializeField]
        [Tooltip("Light toggled when the player uses this object")]
        private Light _light;

        public void BeginUse()
        {
            IsInUse = true;

            if (_light != null)
                _light.enabled = !_light.enabled;
        }

        public void EndUse() => IsInUse = false;

        public bool IsInUse           { get; private set; }
        public bool ShowHighlight     => true;
        public bool ShowInteractionUI => true;
        public string CustomText      => _light != null && _light.enabled ? "Turn off" : "Turn on";
        public GameObject GameObject  => this.gameObject;
    }
}
```

This is deliberately local — the light flips only on the peer that pressed Use. For a switch every player sees, back the state with a `NetworkVariable<bool>` written on the server, or just use the shipped `UseableNetworkBehavior` and wire its UnityEvents. See [Multiplayer & Netcode](networking.md).

### Sample — a crate that explosions can break

```csharp
using FireworksMania.Core.Behaviors;
using UnityEngine;

namespace YourNick.MyMod
{
    [AddComponentMenu("My Mod/Breakable Crate")]
    public class BreakableCrate : MonoBehaviour, IDestructible
    {
        [SerializeField] private float _hitPoints = 100f;
        [SerializeField] private GameObject _brokenVfx;

        public bool IsDestroyed { get; private set; }

        public void ApplyDamage(float damage)
        {
            if (IsDestroyed)
                return;

            _hitPoints -= damage;

            if (_hitPoints > 0f)
                return;

            IsDestroyed = true;

            if (_brokenVfx != null)
                Instantiate(_brokenVfx, transform.position, transform.rotation);
        }
    }
}
```

!!! note "Damage is just a number"
    `ApplyDamage` gets the damage amount and nothing else — no position, no range, no force. If you need to know *where* the explosion was, listen for `MessengerEventApplyExplosionForce` on [The Messenger Event Bus](messaging.md), which carries `Position`, `Range` and `ActualExplosionForce`.

---

## 2. Consume these from the game via DependencyResolver

These interfaces have **no implementation in the Mod Tools package**. The game provides them at runtime; your job is to ask for them.

| Interface | Namespace | Use this when… | Members |
|---|---|---|---|
| `IEnviroSkyManager` | `FireworksMania.Core.Common` | Something in your mod reacts to day/night — street lights, neon signs | `IsNight` |
| `ICustomUIManager` | `FireworksMania.Core.Common` | Your mod has a world-space `Canvas` that must be re-parented into the player's UI when opened | `ShowCanvas(Canvas)`, `HideCanvas(Canvas)`, `RegisterCanvas(Canvas, Transform)`, `UnregisterCanvas(Canvas)` |
| `IEntityDefinitionDatabase` | `FireworksMania.Core.Definitions` | You need to resolve another entity — vanilla or modded — by its definition id at runtime | `GetEntityDefinition(string)` |
| `IDestructionObjectPool` | `FireworksMania.Core.Behaviors` | Almost never. This is internal plumbing for spawning destruction debris | `GetNetworkObject(GameObject, Vector3, Quaternion)` |

!!! note "IInputManager is resolvable, but empty"
    `FireworksMania.Core.Common.IInputManager` is declared with **no members**. The game implements it and `FiringSystemControllerBehavior` resolves it, but there is nothing on it for your code to call, so resolving it yourself buys you nothing.

### DependencyResolver

**Namespace:** `FireworksMania.Core`  
**Base Class:** `MonoBehaviour`  
**API:** `public static DependencyResolver Instance { get; }` and `public T Get<T>()`

What `Get<T>()` actually does, in order:

1. Looks in an internal cache keyed by `typeof(T).FullName`, dropping the entry if the cached Unity object has since been destroyed.
2. On a miss, runs `FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, …)` and takes the first one that implements `T`.
3. Returns `default` — i.e. `null` — if nothing matches.

Three consequences follow directly from that:

| Behaviour | What it means for you |
|---|---|
| `FindObjectsInactive.Exclude` | Only **active** MonoBehaviours in loaded scenes are found. A disabled manager is invisible. |
| Returns `default` on failure | It never throws and never logs. A missing service looks exactly like a working one that returned null. |
| `Instance` is a static set in `Awake` | It is **always null inside the Mod Tools Unity project** — nothing there registers a resolver. |

!!! warning "Always null-check, twice"
    `DependencyResolver.Instance` can be null (no resolver in the scene) *and* `Get<T>()` can return null (no implementation active). Write `DependencyResolver.Instance?.Get<T>()` and then check the result. Any code path that assumes a service exists will throw a `NullReferenceException` the first time you press Play in the Mod Tools project — where the answer is guaranteed to be null.

### Sample — resolve a service with a sane fallback

```csharp
using FireworksMania.Core;
using FireworksMania.Core.Common;
using UnityEngine;

namespace YourNick.MyMod
{
    [AddComponentMenu("My Mod/Night Light")]
    public class NightLight : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Light that is only on after dark")]
        private Light _light;

        private void Start()
        {
            //Instance is null in the Mod Tools project, and Get<T> returns null if nothing active implements it
            var sky = DependencyResolver.Instance?.Get<IEnviroSkyManager>();

            //Fallback: with no sky manager, behave as if it is daytime rather than throwing
            var isNight = sky != null && sky.IsNight;

            if (_light != null)
                _light.enabled = isNight;
        }
    }
}
```

!!! tip "For day/night specifically, you may not need code at all"
    The package ships `DayNightCycleTriggerBehavior` (`Fireworks Mania/Behaviors/Other/DayNightCycleTriggerBehavior`) with `OnDayActions` and `OnNightActions` UnityEvents and a **Random Delay In Seconds** field, so a street lamp can be wired entirely in the Inspector.

!!! warning "DestructibleBehavior needs the game running"
    The shipped `DestructibleBehavior` calls `DependencyResolver.Instance.Get<IDestructionObjectPool>()` **without** a null check when it spawns its destroyed prefab, and only does anything at all on the server. Testing destruction inside the Mod Tools project will not work — build the mod and test it in the game.

---

## 3. Declared but unused — do not implement

These compile, and they look like extension points. They are not. Implementing one costs you nothing but achieves nothing.

| Interface | Namespace | Status |
|---|---|---|
| `IPoolable` | `FireworksMania.Core.Behaviors` | Zero implementers, zero consumers in the package |
| `IHaveFusetime` | `FireworksMania.Core.Behaviors.Fireworks.Parts` | Zero implementers, zero consumers. `Fuse` exposes `FuseTime` through `IFuse` instead |
| `IFuseConnectionMetadata` | `FireworksMania.Core.Behaviors.Fireworks.Parts` | Zero implementers, zero consumers |
| `IExtinguishable` | `FireworksMania.Core.Behaviors.Fireworks.Parts` | Zero implementers, zero consumers. `Fuse` has a private `Extinguish()` but does not implement the interface |
| `IShakeable` | `FireworksMania.Core.Behaviors` | `[Obsolete]` — *"As we now use CinemachineShake and MessengerEventApplyShakeEffect instead"* |
| `IHaveObjectInfo` | `FireworksMania.Core.Interactions` | `[Obsolete(…, error: true)]` — referencing it is a **compile error**. Use `IAmGameObject` |

!!! note "Camera shake replaced IShakeable"
    Explosions now broadcast `MessengerEventApplyShakeEffect` on the Messenger bus instead of calling into an `IShakeable`. If you want to shake the camera, or react to something else shaking it, go through [The Messenger Event Bus](messaging.md).

!!! info "Why they still exist"
    They are either leftovers, or hooks whose consuming systems live in the full game rather than in the Mod Tools. Either way, nothing in the Mod Tools will ever call them, and nothing in the package shows that the game will. Do not build a feature on one.

---

## Worth knowing about

Not in any of the three buckets, but they come up:

| Type | Namespace | What it is |
|---|---|---|
| `IAmGameObject` | `FireworksMania.Core.Interactions` | Implement it (`Name`, `GameObject`) to give your object a friendly display name in the game's UI |
| `IsPickedUp` | `FireworksMania.Core.Interactions` | Despite the name, a marker **MonoBehaviour**, added and removed at runtime by the pickup system. `TryGetComponent<IsPickedUp>(out _)` to test "the player is holding this". Never add it in the Editor |
| `IHaveBaseEntityDefinition` | `FireworksMania.Core.Persistence` | `BaseEntityDefinition EntityDefinition { get; set; }` — implemented by `SaveableEntity` and every firework root, so a component can find out which definition it belongs to |

---

## Where to go next

| Page | Why |
|---|---|
| [Saving & Loading (Blueprints)](persistence.md) | `ISaveableComponent` in full, with its traps |
| [Multiplayer & Netcode](networking.md) | Which of these run server-only, and how to make your own state authoritative |
| [The Messenger Event Bus](messaging.md) | The other way game systems talk to mods |
| [Behaviors](../script-reference/behaviors.md) | The shipped components that already implement these interfaces |
| [Firework Parts](../script-reference/firework-parts.md) | `Fuse`, `FuseConnectionPoint` and the rest of the fuse interfaces |
