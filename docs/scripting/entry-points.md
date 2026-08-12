# Entry Points & Lifecycle

Your mod is a folder of assets, not a program with a `Main()`. This page answers the question every modder hits the moment their first script compiles: **where does my code actually start running?**

---

## There Are Exactly Two Entry Points

| Entry point | What it is | When it runs | Use it for |
|---|---|---|---|
| **A component on your own prefab** | Any `MonoBehaviour` or `NetworkBehaviour` you add to a firework, prop or part prefab | When the game spawns that entity | Anything that belongs to one object |
| **A `StartupPrefabDefinition`** | A ScriptableObject asset that points at a prefab | Once per map, after every mod has finished loading | Map-wide managers, global event listeners, anything not tied to a spawned entity |

There is no third hook. Fireworks Mania has no "mod main class" and no plugin-registration API — nothing in the Mod Tools package scans your assembly at runtime looking for a class to call into. Your code runs because Unity instantiated a GameObject that has your component on it, and nothing else.

!!! note "What about a custom map?"
    Components you place directly in a map scene do run when Unity loads that scene — that is ordinary Unity behaviour, not a Fireworks Mania feature. It only ever runs for players who picked your map, so it is not a substitute for a `StartupPrefabDefinition`. See [Custom Maps](../guides/custom-maps.md).

!!! tip "Not sure you need a script at all?"
    Most fireworks and props need zero code. Check [Writing a Custom Firework](custom-fireworks.md) for the table of shipped behaviors before you write your own.

---

## Entry Point 1 — A Component on Your Prefab

This is ordinary Unity. Put your `MonoBehaviour` on the prefab that your `EntityDefinition` references, and it gets `Awake()`, `OnEnable()` and `Start()` when the game spawns the object.

Two things differ from a normal Unity project:

- **If the prefab has a `NetworkObject`**, the GameObject exists for a short window *before* it is network-spawned. `IsServer`, `IsClient`, `NetworkVariable<T>` values and RPCs are not usable until `OnNetworkSpawn()` — see the ordering table further down this page.
- **If you are driving a firework's launch sequence**, do not write your own `Update` loop — subclass `BaseFireworkBehavior` and let the base class call you at the right moment. See [Writing a Custom Firework](custom-fireworks.md).

---

## Entry Point 2 — StartupPrefabDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/StartupPrefab Definition`  
**Base Class:** `ScriptableObject`

This is the only hook for logic that is not attached to a spawned entity. A single instance of the referenced prefab is instantiated in the map after all mods have loaded, and the tooltips name `Start()` and `OnDestroy()` as the hooks you are expected to use.[^tooltip]

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Prefab Game Object** | `GameObject` | – | The prefab to instantiate. Put your startup scripts on it. |
| **Sort Order** | `int` | `0` | Startup prefabs are instantiated sorted by this value, **lowest number first**. |

### Setting One Up

1. Create an empty prefab in your mod folder — call it something like `MyMod_Startup`.
2. Add your `MonoBehaviour` to it.
3. Right-click in your `Definitions` folder → **Create → Fireworks Mania → Definitions → StartupPrefab Definition**.
4. Drag the prefab into **Prefab Game Object**.
5. Build the mod with **Mod Tools → Build Mod** (++ctrl+shift+b++).

The definition asset must be inside your mod folder — that is how the game finds it.

### "Hello, mod"

```csharp
using FireworksMania.Core;
using UnityEngine;

namespace YourNick.YourMod
{
    // Put this on the prefab referenced by your StartupPrefabDefinition.
    public class MyModStartup : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log($"[MyMod] Hello, mod. Multiplayer: {CoreSettings.IsMultiplayer}");
        }

        private void OnDestroy()
        {
            Debug.Log("[MyMod] Goodbye - the map is unloading.");
        }
    }
}
```

`CoreSettings` lives in `FireworksMania.Core` and is populated by the game. Treat every property on it as **read only**. Most of them do have a public setter — the changelog that introduced them says outright not to use it. `IsMultiplayer` has no setter at all outside the game's own build.

!!! warning "OnDestroy is not optional"
    `OnDestroy()` is where you undo everything `Start()` did — Messenger listeners especially. A startup object lives for the whole map, and a listener that outlives its object keeps getting invoked. See [The Messenger Event Bus](messaging.md).

### Ordering Between Startup Prefabs

**Sort Order** decides which startup prefab is instantiated first, ascending. It is a plain number with no namespacing, so you cannot assume yours is unique across every mod a player has installed. Use it to order *your own* startup prefabs relative to each other, and do not build logic that depends on running before or after somebody else's mod.

### What You Cannot Assume

- **Game services may not resolve.** `DependencyResolver.Instance?.Get<T>()` returns `null` when nothing matching is active in the scene, and it is *always* null inside the Mod Tools project because no game code is present. Null-check every lookup — see [Services & Interfaces](services-and-interfaces.md).
- **`CoreSettings` values are all `false` in the Editor.** They are set by the main game, so anything gated on `CoreSettings.AutoDespawnFireworks` appears not to run when you press Play in the Mod Tools project.

---

## The Ordering That Actually Bites

If your prefab has a `NetworkObject` and can appear in a blueprint, this is the order of events. Getting it wrong is the single most common cause of "it works when I place it by hand, but not when I load my blueprint".

| # | Step | Runs on | What is available |
|---|---|---|---|
| 1 | `Awake()` / `OnEnable()` | every peer that instantiates the object | Serialized fields. **Not** `IsServer`, **not** `NetworkVariable<T>`, **not** RPCs. |
| 2 | `ISaveableComponent.RestoreState(...)` | the peer loading the blueprint | Your saved data. The transform has **not** been restored yet. The `NetworkObject` is **not** spawned yet. |
| 3 | `NetworkObject.Spawn()` | server | – |
| 4 | `OnNetworkSpawn()` | every peer | Everything: `IsServer`, `NetworkVariable<T>`, RPCs, `NetworkObjectId`. |
| 5 | `ISaveablePostActivatedComponent.PostActivate(...)` | the peer loading the blueprint, once every entity in it exists | Every other entity from the same blueprint, handed to you as an `IDictionary<string, SaveableEntity>`. |

Steps 2 and 5 only happen for objects that came out of a blueprint. An object the player places by hand skips straight from 1 to 3.

!!! note "Where does `Start()` fit?"
    Deliberately not in the table. The blueprint loader lives in the game, not in the Mod Tools package, so the position of `Start()` relative to `RestoreState()` is not something this package can prove. Write your "apply my state" method so it is safe to call from both, and idempotent.

The gap between step 1 and step 3 is real and observable: the shipped `ExplosionBehavior` guards its own async explosion routine with `if (!IsServer || !IsSpawned) return;` — and checks `IsSpawned` again after every `await` — precisely because the object can be alive and running logic while its `NetworkObject` is not spawned.

### The Rule That Falls Out Of It

!!! warning "Never write a `NetworkVariable` or send an RPC from `RestoreState`"
    At that point the `NetworkObject` is not spawned, so the write does not replicate and the RPC does not reach anyone. Stash the restored value in a nullable field and apply it in `OnNetworkSpawn()` under an `IsServer` check.

The shape of it — a nullable field carrying the value across the gap:

```csharp
//Blueprint data has to wait here until the object is spawned
private bool? _restoredIsOn;

//Step 2. Not spawned yet, so only touch plain fields
public void RestoreState(CustomEntityComponentData customComponentData)
{
    _restoredIsOn = customComponentData.Get<bool>(IsOnKey);
}

//Step 4. Spawned, so networking is finally safe to use
public override void OnNetworkSpawn()
{
    base.OnNetworkSpawn();

    if (IsServer && _restoredIsOn.HasValue)
    {
        _isOn.Value   = _restoredIsOn.Value;
        _restoredIsOn = null;
    }
}
```

This is the pattern the shipped `FiringSystemReceiverSingleCueBehavior` uses — read it for the pattern only, the class itself is marked `[Obsolete]`.

[Saving & Loading (Blueprints)](persistence.md) has this as a complete, working component, along with the save format, the reserved keys and why `SaveableComponentTypeId` is dangerous to rename. [Multiplayer & Netcode](networking.md) covers the netcode half.

!!! warning "Do not read `transform.position` in `RestoreState`"
    Transform and rigidbody state are restored **after** every `RestoreState` call has finished, so inside your method the object is still sitting wherever it was instantiated.

---

## Cleaning Up

Every subscription needs a matching teardown, and *which* teardown depends on where you subscribed.

| You subscribed in… | Unsubscribe in… |
|---|---|
| `Awake()` or `Start()` | `OnDestroy()` |
| `OnEnable()` | `OnDisable()` |
| `OnNetworkSpawn()` | `OnNetworkDespawn()` |

!!! warning "Overriding `OnDestroy` on a `NetworkBehaviour`"
    `NetworkBehaviour` already declares `OnDestroy`, so you must write `public override void OnDestroy()` and call `base.OnDestroy()`. Declaring a `private void OnDestroy()` instead hides the base implementation and stops Netcode's own cleanup from running.

---

## Where to Go Next

| I want to… | Page |
|---|---|
| Get my first `.cs` file compiling into the mod | [Setting Up Scripts in a Mod](setup.md) |
| Build a firework type the game does not ship | [Writing a Custom Firework](custom-fireworks.md) |
| Make my object behave the same for host and clients | [Multiplayer & Netcode](networking.md) |
| React to game events like day/night or notifications | [The Messenger Event Bus](messaging.md) |
| Persist my own state into blueprints | [Saving & Loading (Blueprints)](persistence.md) |
| Look up a game service such as the sky manager | [Services & Interfaces](services-and-interfaces.md) |
| Fix a build error | [Troubleshooting & Build Errors](../guides/troubleshooting.md) |

[^tooltip]: The instantiation behaviour of `StartupPrefabDefinition` — one instance per definition, created in the map after all mods have loaded, ordered by ascending **Sort Order** — is described by the tooltips on the asset's own fields. The code that reads these assets lives in the game, not in the Mod Tools package, so nothing in this repository can be used to confirm the exact timing or to describe it in more detail than the tooltips do.
