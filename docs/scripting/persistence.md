# Saving & Loading (Blueprints)

When a player saves a blueprint, your mod's objects are written into it. This page is for modders whose custom firework or prop carries state — a switch position, a colour, a counter — that should still be there when the blueprint is loaded again.

---

## The model in one paragraph

A blueprint is a list of **entities**. Each entity records two things: the **EntityDefinitionId** of the item (so the game knows which prefab to spawn) and a dictionary of **custom component data** (so each component on that prefab can stash whatever it likes). The game spawns the prefab fresh from the definition, then hands each component its slice of the dictionary back. Nothing about your prefab's hierarchy, materials or Inspector values is stored — only the definition id, the transform and rigidbody state `SaveableEntity` captures on its own, and the data your components explicitly write.

The types involved all live in `FireworksMania.Core.Persistence`. Their shape, with method bodies omitted:

```csharp
using System;
using System.Collections.Generic;

namespace FireworksMania.Core.Persistence
{
    [Serializable]
    public struct SaveableEntityData
    {
        public string EntityInstanceId;
        public string EntityDefinitionId;
        public Dictionary<string, CustomEntityComponentData> CustomComponentData;
    }

    [Serializable]
    public struct CustomEntityComponentData
    {
        public Dictionary<string, object> CustomData;

        public void Add<T>(string key, T data);
        public T    Get<T>(string key);
    }
}
```

| Level | Key | Value |
|---|---|---|
| Entity | `EntityDefinitionId` | The `Id` of your `BaseEntityDefinition` — see [Definitions](../script-reference/definitions.md) |
| Entity | `CustomComponentData` | One entry per saveable component, keyed by that component's `SaveableComponentTypeId` |
| Component | `CustomData` | Your own keys and values |

Blueprints are serialised with Newtonsoft Json.NET (`com.unity.nuget.newtonsoft-json`), so everything you store has to survive a JSON round-trip.

!!! danger "The definition Id is forever"
    `EntityDefinitionId` is written into every blueprint that contains your item. There is **no** id-remapping or migration mechanism anywhere in the Mod Tools — change the **Id** on a published definition and every existing blueprint that uses it is orphaned. The field's own tooltip says as much: *"avoid changing this Id once it have been set, as it will break users Blueprints"*.

---

## SaveableEntity

**Namespace:** `FireworksMania.Core.Persistence`  
**Menu:** `Fireworks Mania/Persistence/SaveableEntity`  
**Base Class:** `MonoBehaviour`

`SaveableEntity` is the component that makes a GameObject show up in a blueprint at all. It is a plain `MonoBehaviour`, not a `NetworkBehaviour`.

### Inspector fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Entity Definition** | `BaseEntityDefinition` | `null` | *"Reference to the EntityDefinition for this prefab"* |
| **Save Transform Data** | `bool` | `true` | *"Enabling this will include position, rotation and scale as part of the saved data. Disable to lower blueprint file size, if you do not need these data."* |

### Who gets one automatically

| Prefab type | `SaveableEntity` added for you? |
|---|---|
| Firework deriving from `BaseFireworkBehavior` | Yes — `OnValidate` adds one if missing and wires the definition |
| Prop, or any other custom prefab | **No** — add it yourself |

!!! warning "Always assign Entity Definition by hand"
    `SaveableEntity.Awake()` tries to find a definition on the same GameObject via `GetComponent<IHaveBaseEntityDefinition>()`, but `SaveableEntity` itself implements that interface, so the lookup can find *itself* and come up empty. When it does, you get:

    `'BaseEntityDefinition' is missing on component 'SaveableEntity' on '<your object>', please fix else save/load won't work`

    Setting the field in the Inspector avoids the whole problem. The mod build also fails if a `SaveableEntity` on a definition's prefab has no `EntityDefinition`, or points at a different one — see [Troubleshooting & Build Errors](../guides/troubleshooting.md).

---

## The ISaveableComponent contract

Three members. That is the whole interface.

```csharp
namespace FireworksMania.Core.Persistence
{
    public interface ISaveableComponent
    {
        CustomEntityComponentData CaptureState();
        void RestoreState(CustomEntityComponentData customComponentData);

        string SaveableComponentTypeId { get; }
    }
}
```

| Member | Called when | Notes |
|---|---|---|
| `CaptureState()` | The player saves a blueprint | Must be cheap, pure and idempotent — it runs **twice** per save |
| `RestoreState(data)` | The blueprint loads, **before** the transform is applied and **before** `OnNetworkSpawn` | Only touch plain fields here |
| `SaveableComponentTypeId` | Both | The dictionary key your data is filed under — it is part of your save format |

Every implementation shipped in the package writes the same thing:

```csharp
public string SaveableComponentTypeId => this.GetType().Name;
```

!!! danger "Renaming your class breaks existing blueprints"
    With `GetType().Name`, the class name **is** the on-disk key. Rename `PropLampBehavior` to `LampBehavior` after publishing and every blueprint out there still holds a `"PropLampBehavior"` entry that nothing will ever read again — the state silently reverts to your field defaults. Pick the class name before you publish and leave it alone. The same applies to a custom firework: `BaseFireworkBehavior` uses `GetType().Name` too, so the key is your concrete subclass name.

---

## Sample — a prop that remembers whether its light is on

Put this on the **same GameObject** as the `SaveableEntity`.

```csharp
using FireworksMania.Core.Persistence;
using UnityEngine;

namespace YourNick.MyMod
{
    [AddComponentMenu("My Mod/Prop Lamp Behavior")]
    [DisallowMultipleComponent]
    public class PropLampBehavior : MonoBehaviour, ISaveableComponent
    {
        //A constant, not a literal - this string is part of your save format forever
        private const string IsOnKey = "IsOn";

        [SerializeField]
        [Tooltip("Light that is switched on and off")]
        private Light _light;

        //Defaults matter: if a blueprint holds no data for this component, RestoreState is
        //never called - and Get<T> returns default(T) for a key it doesn't find, so a
        //missing key looks exactly like a saved 'false'. See "Get<T> fails silently" below.
        private bool _isOn = true;

        private void Start() => ApplyState();

        public void Toggle()
        {
            _isOn = !_isOn;
            ApplyState();
        }

        private void ApplyState()
        {
            if (_light != null)
                _light.enabled = _isOn;
        }

        //Runs twice per save - keep it cheap and free of side effects
        public CustomEntityComponentData CaptureState()
        {
            var data = new CustomEntityComponentData();
            data.Add<bool>(IsOnKey, _isOn);
            return data;
        }

        //Runs before the transform is restored and before the NetworkObject is spawned
        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            _isOn = customComponentData.Get<bool>(IsOnKey);
            ApplyState();
        }

        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
```

`ApplyState()` is called from both `Start()` and `RestoreState()` on purpose — the loader lives in the game, so treat `Start()` as "may run before or after `RestoreState`" and make your apply method safe to call from either.[^startorder]

### Prefab checklist

- [ ] Root GameObject has **SaveableEntity** (Add Component → Fireworks Mania → Persistence → SaveableEntity)
- [ ] Its **Entity Definition** points at your definition asset, and the definition's **Prefab Game Object** points back at the prefab
- [ ] The definition has a permanent **Id** (Inspector context menu → **Set Id to filename**)
- [ ] Your `ISaveableComponent` sits on the *same* GameObject as `SaveableEntity`
- [ ] **Save Transform Data** left ticked unless you genuinely do not need position/rotation/scale

---

## What you can safely store

These types are proven by the game's own components:

| Type | Used by |
|---|---|
| `bool` | `SaveableEntity` rigidbody `isKinematic` |
| `int` / `byte` | Firing system cue index, firework module/cue index |
| `string` | Legacy mortar shell entity ids |
| `[Serializable]` structs | `SerializableVector3`, `SerializableRotation` |
| `List<T>` of a `[Serializable]` struct | `MortarBehavior` saves a `List<MortarTubeSaveData>` under one key |

!!! tip "Save positions with SerializableVector3, not Vector3"
    `FireworksMania.Core.Persistence` ships `SerializableVector3` (`X`, `Y`, `Z`) and `SerializableRotation` (`X`, `Y`, `Z`, `W`) as public types, and `SaveableEntity` uses them for its own transform data. Use them for anything positional. The package never stores a raw `UnityEngine.Vector3` or `Quaternion`, so nothing proves those would survive the round-trip.

Never store a reference to a `GameObject`, a `Component` or a `ScriptableObject`. Store an `EntityInstanceId` string (for another blueprint entity) or a definition `Id` string (for an item type) and resolve it back on load.

---

## The traps

!!! danger "Two keys are reserved — never return them from SaveableComponentTypeId"
    `SaveableEntity` writes its own entries under `"SaveableTransformComponent"` (position, rotation, localScale) and `"SaveableRigidbodyComponent"` (isKinematic). Return either string from `SaveableComponentTypeId` and you overwrite the built-in data, so every object saved with your mod loads at the wrong position — in blueprints players already have.

!!! warning "Children are not scanned"
    `SaveableEntity` finds saveable components with `GetComponents<ISaveableComponent>()` — **same GameObject only**. An `ISaveableComponent` on a child is silently ignored: no error, no warning, it simply never saves. If your logic lives on children, keep one `ISaveableComponent` on the root that reaches into them and saves a `List<T>`, exactly like the shipped `MortarBehavior` does for its tubes.

!!! warning "Save and restore order are not mirror images"
    On save, transform and rigidbody data are captured **first**, then your components. On load, your components are restored **first**, then the transform, then the rigidbody. So inside `RestoreState` the object is still at its instantiate-time position — reading `transform.position` there gives you a value that is about to be thrown away.

!!! warning "CaptureState() is called twice per save"
    `SaveableEntity` calls your `CaptureState()` once to check whether it produced any data, then calls it **again** to produce the value it actually stores. Both calls are in shipped source. Keep the method pure: no counters, no state mutation, no logging you would not want doubled, and nothing expensive.

!!! warning "Adding the same key twice fails the whole save"
    `CustomEntityComponentData.Add` is a plain `Dictionary.Add` — a duplicate key throws, `SaveableEntity` wraps it in a `CaptureStateException`, and the blueprint save aborts. Always `new` a fresh `CustomEntityComponentData` inside `CaptureState()`; never cache one and add to it again.

!!! warning "Get&lt;T&gt; cannot tell you why it failed"
    `Get<T>` tries a JSON deserialise, then a direct cast, and swallows the exception from both. A missing key, a key you renamed, and a type that no longer matches all produce the same result: `default(T)` — `false`, `0`, `null` — with nothing in the Console. If "missing" and "wrong" need different handling, check the dictionary yourself first. `CustomData` is a public field, so this works:

    ```csharp
    if (customComponentData.CustomData != null &&
        customComponentData.CustomData.ContainsKey(IsOnKey))
    {
        _isOn = customComponentData.Get<bool>(IsOnKey);
    }
    ```

!!! note "No data means RestoreState is never called"
    If `CaptureState()` returns a `CustomEntityComponentData` whose `CustomData` is null or empty, nothing is written for your component — and on load, `RestoreState` is simply not invoked. This is a feature: the shipped firework base class only writes its module and cue indices when they are actually set. It also means **your field defaults are your load-time values**, so choose them deliberately.

---

## Restoring safely in multiplayer

`RestoreState` runs after `Awake` but **before** the `NetworkObject` is spawned. Writing a `NetworkVariable` or sending an RPC from there will not work. The pattern the package itself uses is to stash the restored value in a nullable field and apply it in `OnNetworkSpawn` on the server.

```csharp
using FireworksMania.Core.Persistence;
using Unity.Netcode;
using UnityEngine;

namespace YourNick.MyMod
{
    [AddComponentMenu("My Mod/Networked Prop Lamp Behavior")]
    [DisallowMultipleComponent]
    public class NetworkedPropLampBehavior : NetworkBehaviour, ISaveableComponent
    {
        private const string IsOnKey = "IsOn";

        [SerializeField]
        private Light _light;

        private NetworkVariable<bool> _isOn = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        //RestoreState runs before the NetworkObject is spawned, so the value waits here
        private bool? _restoredIsOn;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer && _restoredIsOn.HasValue)
            {
                _isOn.Value   = _restoredIsOn.Value;
                _restoredIsOn = null;
            }

            _isOn.OnValueChanged += OnIsOnChanged;
            ApplyState(_isOn.Value);
        }

        public override void OnNetworkDespawn()
        {
            _isOn.OnValueChanged -= OnIsOnChanged;
            base.OnNetworkDespawn();
        }

        private void OnIsOnChanged(bool previousValue, bool newValue) => ApplyState(newValue);

        private void ApplyState(bool isOn)
        {
            if (_light != null)
                _light.enabled = isOn;
        }

        public CustomEntityComponentData CaptureState()
        {
            var data = new CustomEntityComponentData();
            data.Add<bool>(IsOnKey, _isOn.Value);
            return data;
        }

        public void RestoreState(CustomEntityComponentData customComponentData)
        {
            //Never write a NetworkVariable or send an Rpc from here - not spawned yet
            _restoredIsOn = customComponentData.Get<bool>(IsOnKey);
        }

        public string SaveableComponentTypeId => this.GetType().Name;
    }
}
```

The prefab needs a `NetworkObject` for this. `NetworkVariable<T>` and `[Rpc(...)]` in mod code are supported but advanced — read [Multiplayer & Netcode](networking.md) before you commit to it.

!!! info "Effective load order"
    `Instantiate` (so `Awake` runs) → `SaveableEntity.RestoreState` → `NetworkObject.Spawn` / `OnNetworkSpawn`. Where `Start()` falls in that sequence is not something the Mod Tools package determines — the blueprint loader lives in the game — so write your apply-state method to be safe from either side.

---

## Referencing another entity in the same blueprint

During `RestoreState` the *other* entities of the blueprint do not exist yet, so you cannot look them up. Implement `ISaveablePostActivatedComponent` as well, save the other entity's `EntityInstanceId` as a string, and resolve it once everything has spawned:

```csharp
using System.Collections.Generic;

namespace FireworksMania.Core.Persistence
{
    public interface ISaveablePostActivatedComponent
    {
        void PostActivate(IDictionary<string, SaveableEntity> entityDictionary);
    }
}
```

The dictionary is keyed by `SaveableEntity.EntityInstanceId`. Stash ids in `RestoreState`, then `TryGetValue` them in `PostActivate` and skip gracefully when an id is not found — a blueprint can always have been hand-edited, or saved by an older version of your mod.

!!! note "PostActivate is called by the game, not by the Mod Tools"
    Nothing inside the Mod Tools package implements or invokes `PostActivate`; the blueprint loader that calls it lives in the game. The interface and the dictionary key are verified from source, but whether the loader also searches child GameObjects for it is not something this package can tell you — keep it on the same GameObject as your `SaveableEntity` to be safe.

---

## Odds and ends

**`IsValidForSaving`** — `SaveableEntity` exposes `SetIsValidForSaving(bool)` and `IsValidForSaving` (default `true`). The framework flips it to `false` when a fuse ignites, so a lit firework is not meant to end up in a blueprint. Inside the Mod Tools package the flag only produces a Console error if `CaptureState()` runs anyway; whether the game's save routine actually filters on it is decided game-side.[^validforsaving]

**Legacy marker components** — `SaveableTransformComponent` and `SaveableRigidbodyComponent` are empty `[Obsolete]` classes kept only so old prefabs still open. `SaveableEntity` handles transform and rigidbody itself. Delete them from any prefab you inherit.

**Two components, one id** — two instances of the same script on one GameObject return the same `SaveableComponentTypeId`, so the second capture overwrites the first and both get handed the same blob on restore. Use one component that saves a list instead.

---

## Where to go next

| Page | Why |
|---|---|
| [Definitions](../script-reference/definitions.md) | The `Id` field that anchors every saved entity |
| [Writing a Custom Firework](custom-fireworks.md) | Fireworks already implement `ISaveableComponent` — override, do not replace |
| [Multiplayer & Netcode](networking.md) | `NetworkVariable<T>`, `[Rpc(...)]` and server authority |
| [Services & Interfaces](services-and-interfaces.md) | The rest of the interfaces you can implement or consume |
| [Entry Points & Lifecycle](entry-points.md) | Where your code starts running in the first place |

[^startorder]: The relative order of `Start()` and `RestoreState` is not determined by anything in the Mod Tools package — the blueprint loader is part of the game. The rest of the sequence (`Awake` → `RestoreState` → `Spawn` → `OnNetworkSpawn`) is inferred from the stash-and-apply pattern used by the shipped `FiringSystemReceiverSingleCueBehavior` and `MortarTube`, both of which park a restored value in a nullable field and only write it to a `NetworkVariable` once `OnNetworkSpawn` runs on the server.
[^validforsaving]: `SaveableEntity.CaptureState()` logs `Entity '<id>' shouldn't be trying to CaptureState as it's not marked as valid for saving!` and then continues regardless — it is a log, not a guard. The code that decides which entities go into a blueprint is not part of the Mod Tools package.
