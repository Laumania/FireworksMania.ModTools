# Multiplayer & Netcode

This page is for modders whose mod behaves differently for the host and for everyone else — objects that move on one screen but not another, sounds only the host hears, state that resets when a player joins.

---

!!! success "Yes, your mod can use Netcode for GameObjects"
    Since Mod Tools **v2025.8.1** the mod build pipeline runs Netcode for GameObjects' own code generator over your compiled mod assembly. That means a `NetworkBehaviour` in your mod with `NetworkVariable<T>` fields and `[Rpc(...)]` methods compiles and works.

    The CHANGELOG entry that shipped this puts it best: *"you should be able to create scripts that use Netcode for GameObject, like Rpcs, NetworkVariables etc. However, this is advanced stuff, so 99% of mod creators will never use this."*

    Older documentation — including parts of the repository README — still says code generation cannot run for mods. That is out of date.

If you have read a tutorial or a forum post claiming mods cannot use NetworkVariables, it was written before this existed.

---

## How it works

When you press ++ctrl+shift+b++ (**Mod Tools → Build Mod**), uMod compiles the `.cs` files in your mod folder into a fresh managed assembly. A Fireworks Mania build processor then:

1. Logs `Netcode for Gameobject CodeGen patching assembly: '<YourAssembly>'` to the Unity Console.
2. Reflects into the Editor's `Unity.Netcode.Editor.CodeGen` assembly and runs `NetworkBehaviourILPP` — Unity's real Netcode post-processor — over your assembly.
3. Removes the unpatched assembly from the build and registers the patched bytes in its place.

If your compiled mod assembly does not reference `Unity.Netcode.Runtime` — which is what happens when none of your scripts touch a Netcode type — the code generator declines the assembly and the processor logs `ILPP returned null (likely WillProcess == false). No changes were applied.` before moving on. That message is normal, not an error.

!!! warning "Only builds made through Mod Tools → Build Mod get patched"
    The patching happens as part of the mod build. Testing a `NetworkBehaviour` by pressing Play inside the Mod Tools project exercises the Editor's own compilation, not the mod assembly that ships in your `.mod` file. Always verify networked behaviour in the actual game.

---

## The `FMNetworkVariable*` shims are gone

`FMNetworkVariableBool`, `FMNetworkVariableInteger` and `FMNetworkVariableString` were a temporary workaround from before code generation worked. All three are now marked obsolete with `error: true`, so referencing one is a **compile error**, not a warning:

> This should not be used anymore as we now have real Netcode for Gameobjects CodeGen available in the mod build pipeline. You can use regular NGO NetworkVariables in your own code now.

Delete them from any older mod and use `Unity.Netcode.NetworkVariable<T>` directly.

---

## Use the modern `[Rpc]` attribute

Netcode has two generations of remote-procedure-call syntax. Fireworks Mania uses the newer **universal RPC attribute**, where you declare *where the call runs* with a `SendTo` value.

| Attribute | Where the method body runs | Typical use |
|---|---|---|
| `[Rpc(SendTo.Server)]` | On the server only | A client asking the server to change authoritative state |
| `[Rpc(SendTo.Everyone)]` | On every peer, including the caller | Playing an effect or sound that everyone must see |
| `[Rpc(SendTo.ClientsAndHost)]` | On all clients and the host | Server-driven presentation updates |

The method name must end in `Rpc` — that is a Netcode requirement, and every RPC in the Fireworks Mania codebase follows it (`ToggleOnRpc`, `BeginUseRpc`, `EndUseRpc`).

Netcode defines more `SendTo` targets than these three; the three above are the ones used throughout Fireworks Mania and the only ones documented here.

!!! warning "Do not copy `[ServerRpc]` from generic Netcode tutorials"
    `[ServerRpc]` does not appear anywhere in this codebase, and neither does `RequireOwnership` or `ServerRpcParams`. Most Netcode tutorials you will find online predate the universal attribute.

    `[ClientRpc]` survives in exactly two places in the shipped code, both in `Fuse.cs`, as legacy. Treat it the same way — write new code with `[Rpc(SendTo.…)]`.

    You will still see method *names* like `IgniteOnServerRpc` and `PlayShellLoadSoundClientRpc` in the codebase. Those are just names on methods that carry the modern attribute; the name has to end in `Rpc` and everything before that is free-form.

---

## Server authority: the one rule that matters

Every networked state change must originate on the server. Clients ask; the server decides; the result replicates back to everyone.

=== "Server"

    The server is the only machine allowed to write a `NetworkVariable` that has `NetworkVariableWritePermission.Server`, and the only machine allowed to despawn a `NetworkObject`. Guard those code paths with `if (IsServer)`.

=== "Client"

    A client never writes state directly. It calls a method that forwards the request through `[Rpc(SendTo.Server)]`, then reacts to the replicated result through `OnValueChanged` — exactly like every other peer, including the host.

### Server-authoritative state

A `NetworkVariable<bool>` that only the server may write, with every peer reacting to the change:

```csharp
using Unity.Netcode;
using UnityEngine;

namespace YourNick.YourMod
{
    // Put this on a prefab that also has a NetworkObject component.
    public class NetworkedLamp : NetworkBehaviour
    {
        [SerializeField]
        private Light _lamp;

        private readonly NetworkVariable<bool> _isOn = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isOn.OnValueChanged += HandleIsOnChanged;
            ApplyState(_isOn.Value);            // catch up with the current value on join
        }

        public override void OnNetworkDespawn()
        {
            _isOn.OnValueChanged -= HandleIsOnChanged;
            base.OnNetworkDespawn();
        }

        // Server-only entry point. On a client this silently does nothing.
        public void SetOn(bool isOn)
        {
            if (IsServer == false)
                return;

            _isOn.Value = isOn;
        }

        private void HandleIsOnChanged(bool previousValue, bool newValue) => ApplyState(newValue);

        private void ApplyState(bool isOn)
        {
            if (_lamp != null)
                _lamp.enabled = isOn;
        }
    }
}
```

Two details that are easy to miss:

- **Subscribe in `OnNetworkSpawn`, unsubscribe in `OnNetworkDespawn`.** Both are `public override void` and both should call `base`.
- **Apply the current value once at spawn.** `OnValueChanged` only fires on *changes*, so a player who joins after the lamp was switched on never gets the callback. Reading `_isOn.Value` at spawn time closes that gap. The shipped `ToggleNetworkBehavior` does the same thing.

### Client request, then effect for everyone

The canonical two-hop pattern: a client asks the server, the server validates, the server tells everyone to play the effect.

```csharp
using FireworksMania.Core.Attributes;
using FireworksMania.Core.Messaging;
using Unity.Netcode;
using UnityEngine;

namespace YourNick.YourMod
{
    public class NetworkedSparkler : NetworkBehaviour
    {
        [SerializeField]
        private ParticleSystem _effect;

        [SerializeField]
        [GameSound]
        private string _sound;

        // Any peer may call this. The request travels to the server.
        public void Activate()
        {
            // An RPC on an unspawned NetworkObject has nowhere to go, so check first.
            // The shipped ExplosionBehavior guards the same way.
            if (IsSpawned == false)
                return;

            ActivateRpc();
        }

        [Rpc(SendTo.Server)]
        private void ActivateRpc()
        {
            // Server-side checks belong here - the server decides whether it happens at all.
            PlayEffectRpc();
        }

        [Rpc(SendTo.Everyone)]
        private void PlayEffectRpc()
        {
            if (_effect != null)
                _effect.Play();

            // Messenger never crosses the network, which is exactly why this
            // broadcast sits inside a SendTo.Everyone RPC.
            Messenger.Broadcast(new MessengerEventPlaySound(_sound, this.transform));
        }
    }
}
```

The comment on that broadcast is the thing to remember when mixing the two systems: the RPC crosses the network, the `Messenger` broadcast does not. See [The Messenger Event Bus](messaging.md).

!!! tip "Replicate the trigger, not the result"
    Send the smallest possible message — "this happened" — and let every machine produce the sound, the particles and the shake locally. This is what the shipped `MortarTube` does when a shell is loaded: the server calls a `[Rpc(SendTo.Everyone)]` method whose whole body is a local `Messenger` broadcast, so each machine plays its own copy of the sound.

---

## Removing objects: `DestroyOrDespawn()`

Calling `Destroy()` on a spawned `NetworkObject` removes it on one machine and leaves it on every other. Use the `DestroyOrDespawn()` extension instead — it despawns when the object is a spawned `NetworkObject` and falls back to `GameObject.Destroy` when it is not.

It is an extension on `GameObject` (not on `Component`), so call it as `this.gameObject.DestroyOrDespawn()` and add `using FireworksMania.Core.Utilities;`.

=== "Networked object"

    ```csharp
    using FireworksMania.Core.Utilities;   // DestroyOrDespawn()
    using Unity.Netcode;
    using UnityEngine;

    namespace YourNick.YourMod
    {
        public class DespawnAfterDelay : NetworkBehaviour
        {
            [SerializeField]
            private float _lifetimeInSeconds = 10f;

            public override void OnNetworkSpawn()
            {
                base.OnNetworkSpawn();

                // Despawning is server-only, so only the server runs the timer.
                if (IsServer)
                    Invoke(nameof(Remove), _lifetimeInSeconds);
            }

            public override void OnNetworkDespawn()
            {
                CancelInvoke(nameof(Remove));
                base.OnNetworkDespawn();
            }

            private void Remove() => this.gameObject.DestroyOrDespawn();
        }
    }
    ```

=== "Plain prop"

    ```csharp
    using FireworksMania.Core.Utilities;   // DestroyOrDespawn()
    using UnityEngine;

    namespace YourNick.YourMod
    {
        public class RemoveOnCommand : MonoBehaviour
        {
            // No NetworkObject on this GameObject, so DestroyOrDespawn falls through
            // to a plain GameObject.Destroy - safe to call from anywhere.
            public void Remove() => this.gameObject.DestroyOrDespawn();
        }
    }
    ```

!!! warning "Despawn is server-only"
    On a client, `NetworkObject.Despawn` does nothing except log `Only server can despawn objects`. It fails quietly, which is worse than throwing — the object stays alive everywhere and you get no exception to trace. `DestroyOrDespawn()` does not shield you from that; it only picks the right call for the object type. Guard the call site with `IsServer` whenever the object may be a spawned `NetworkObject`.

---

## Making a prefab networked

Select the prefab or the GameObject and use **GameObject → Fireworks Mania → Add Network Components** (also available as **Assets → Fireworks Mania → Add Network Components**). It adds, only where missing:

| Component | Added when |
|---|---|
| `NetworkObject` | Always |
| `ClientNetworkTransform` | Always |
| `ClientNetworkRigidbody` | Only if the GameObject already has a `Rigidbody` |

`ClientNetworkTransform` and `ClientNetworkRigidbody` are Fireworks Mania types in namespace `FireworksMania.Core.Common` — **not** in `Unity.Netcode.Components`, whatever a generic tutorial tells you. They derive from Netcode's `NetworkTransform` / `NetworkRigidbody` and make movement owner-authoritative rather than server-authoritative.

!!! danger "Never copy these two scripts into your mod"
    Both carry `[UMod.Shared.ModDontCompile]`, which deliberately excludes them from mod-side compilation so they resolve to the game's own copies. Forking them into your mod folder means your mod ships a second, incompatible version of a core type.

One consequence worth planning for: on a machine that does not own the object, `ClientNetworkRigidbody` forces the `Rigidbody` kinematic. Physics is only simulated on the owner, so a script reading velocities or reacting to collisions only sees the truth there.

Two more helpers live under **Mod Tools → Utilities → Multiplayer** — *Revert All NetworkObject Overrides In Current Scene* and *Mark all NetworkObjects as dirty in current scene*. See the [Editor Menu Reference](../guides/editor-tools.md).

---

## Knowing whether you are in multiplayer

`FireworksMania.Core.CoreSettings.IsMultiplayer` is a read-only `bool` the game populates. Its own source comment calls it *"a temp fix for modders to know if a game is in single or multiplayer mode. Please be aware that this might change in the future."* Treat it as convenient but unstable, and never write to `CoreSettings` from a mod.

In the Mod Tools Editor every `CoreSettings` value is at its default — the game sets them, not the Mod Tools.

---

## Still not supported

### Network Object Prefabs on a MapDefinition

This one is genuinely still broken, and it is a separate issue from code generation. The field's own tooltip in `MapDefinition` says so verbatim:

> [This is currently not working - awaiting a fix from Unity and NetCode Team] All objects in a map that have a NetworkObject component on them, HAVE to be a prefab instance. Add reference to the prefab itself here for it to work.

The **Populate NetworkObjectPrefabs from current open scene** context-menu action on `MapDefinition` fills the list, but the runtime side of it does not work for modded maps yet. Plan custom maps around that limitation — see [Custom Maps](../guides/custom-maps.md).

### Precompiled assemblies

Mods ship C# source, never a `.dll`, and the compiled result must pass the build-time security check (no `System.IO`, no `System.Reflection`, no P/Invoke, and more). [Setting Up Scripts in a Mod](setup.md) has the full deny list.

---

## Debugging a failed patch

!!! info "ILPP diagnostics never reach the Unity Console"
    When the code generator produces diagnostics, the build processor writes them with `Console.WriteLine`, not `Debug.Log`. They land in Unity's **Editor.log**, not in the Unity Console window. If a networked mod build fails in a way the Console cannot explain, open Editor.log and search for `Diagnostics from ILPP:`.

    On Windows that file is `%LOCALAPPDATA%\Unity\Editor\Editor.log`.

Messages the patcher produces, and what they mean:

| Message | Where it lands | Cause |
|---|---|---|
| `Netcode for Gameobject CodeGen patching assembly: '<X>'` | Console | Normal. The patcher found your assembly and is about to run |
| `ILPP returned null (likely WillProcess == false). No changes were applied.` | Console | Normal when nothing in your mod references Netcode. Suspicious if you *did* write a `NetworkBehaviour` — see below |
| `Cannot find 'Unity.Netcode.Editor.CodeGen' assembly.` | Console (build fails) | The Netcode for GameObjects package is missing from the project |
| `Referenced assembly '<X>' not found in AppDomain.` | Console (build fails) | Your script references an assembly the Editor has not loaded |
| `ILPP returned a result but InMemoryAssembly is null. No output written.` | Editor.log only | Code generation ran but produced nothing — read the `Diagnostics from ILPP:` block just below it |
| `InMemoryAssembly.PeData is empty. No output written.` | Editor.log only | Same as above — your assembly shipped unpatched |
| `Failed to register patches assembly for build! : <name>` | Console | The patched assembly was rejected by the mod packer |

The two Editor.log-only messages are the dangerous ones: the build still finishes, but your mod ships without the generated RPC and NetworkVariable plumbing. Seeing `ILPP returned null` on a mod that does contain a `NetworkBehaviour` has the same effect, and means the compiled assembly never picked up a direct reference to `Unity.Netcode.Runtime`.

More build errors and their fixes are in [Troubleshooting & Build Errors](../guides/troubleshooting.md).

---

## Checklist before you ship a networked mod

- [ ] Every `NetworkVariable` uses `NetworkVariableWritePermission.Server` unless you have a specific reason not to
- [ ] Every write to a `NetworkVariable` sits behind `if (IsServer)` or inside an `[Rpc(SendTo.Server)]` method
- [ ] Every `OnValueChanged` subscription in `OnNetworkSpawn` has a matching unsubscribe in `OnNetworkDespawn`
- [ ] The current value is applied once at spawn, so late joiners are not left out of sync
- [ ] No `Destroy()` on a spawned `NetworkObject` — `DestroyOrDespawn()` on the server instead
- [ ] No `[ServerRpc]` anywhere
- [ ] The prefab actually has a `NetworkObject` component
- [ ] Tested in the real game with a second player, not only in the Editor

---

## Where to go next

| You want to | Page |
|---|---|
| Get your first `.cs` file compiling into a mod | [Setting Up Scripts in a Mod](setup.md) |
| Know where your code starts running | [Entry Points & Lifecycle](entry-points.md) |
| Talk to game systems without referencing them | [The Messenger Event Bus](messaging.md) |
| Build a firework with custom behaviour | [Writing a Custom Firework](custom-fireworks.md) |
| Read the general do's and don'ts | [Best Practices](../best-practices.md) |
