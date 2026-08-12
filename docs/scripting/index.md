# Scripting

This section is for modders who have hit the ceiling of what the Unity Inspector can do and want to write a bit of C# to get past it. If you have not hit that ceiling yet, you can safely skip the whole section.

---

## Scripting is optional

Fireworks, props, maps, sounds and icons are all built from prefabs, components and ScriptableObject definitions. Most published mods contain **no code at all** — they are meshes, materials, particle systems and definitions wired together in the Inspector.

Reach for a script only when you want something the shipped components genuinely cannot do:

| Good reason to write a script | Where it is covered |
|---|---|
| A firework type that behaves unlike anything the game ships | [Writing a Custom Firework](custom-fireworks.md) |
| Map-wide logic that runs once when the map loads | [Entry Points & Lifecycle](entry-points.md) |
| Reacting to game events such as day/night changes | [The Messenger Event Bus](messaging.md) |
| Custom state that has to survive a blueprint save | [Saving & Loading (Blueprints)](persistence.md) |

!!! tip "Try the Inspector route first"
    Before writing a script, check [Behaviors](../script-reference/behaviors.md) and [Firework Parts](../script-reference/firework-parts.md). A surprising amount of custom behaviour is achievable by combining existing components and their UnityEvents — and Inspector-only mods are far less likely to break when the game updates.

---

## What a mod script actually is

A mod script is a plain `.cs` file that lives **inside your mod folder** in your Unity project. It is not a plugin, not a patch, and not a DLL you drop next to the game.

When you run **Mod Tools → Build Mod** (++ctrl+shift+b++), the Mod Tools:

1. Compile the C# source files found inside your mod folder into a **fresh managed assembly**.
2. Run Unity's Netcode for GameObjects code generator over that assembly.
3. Pack the resulting assembly into your `.mod` file alongside your assets.

What that means in practice:

| Fact | Consequence for you |
|---|---|
| You ship **source**, never binaries | The Mod Tools are configured with `allowAssembliesInMods: 0`. A precompiled `.dll` in your mod folder will not be included. Third-party libraries have to be vendored as `.cs` source — and they have to pass the same security check your own code does. |
| Only scripts **inside the mod folder** are compiled | uMod's own words: *"Only scripts inside the mod folder will be compiled"*. Helper code parked elsewhere in your Unity project compiles fine in the Editor but never ships. |
| Your scripts live in Unity's default `Assembly-CSharp` | Do **not** put an Assembly Definition (`.asmdef`) in your mod folder. This is the single most common way to end up with a mod that silently ships no code — see [Setting Up Scripts in a Mod](setup.md). |
| The assembly is security-checked on every build | Certain namespaces and APIs are denied outright and fail the build. See below. |

---

## What you get for free

Because your mod scripts live in `Assembly-CSharp`, every auto-referenced assembly in the project is already available to them. There is **nothing to set up** — no references to add, no package to import, no asmdef to configure. Just type the `using`.

| `using` | What it gives you |
|---|---|
| `FireworksMania.Core` and its sub-namespaces | The whole game API — behaviors, definitions, messaging, persistence, utilities |
| `UnityEngine`, `UnityEngine.Events`, … | Unity itself |
| `Unity.Netcode` | Netcode for GameObjects — `NetworkBehaviour`, `NetworkVariable<T>`, `[Rpc(...)]` |
| `Unity.Collections` | Native containers, `FixedString*` types used by NGO serialisation |
| `Cysharp.Threading.Tasks` | UniTask — the async/await library the firework base class is built on |
| `DG.Tweening` | DOTween — tweening and easing |
| `TMPro` | TextMeshPro |
| `Newtonsoft.Json` | JSON serialisation — the same library the blueprint system uses |

!!! note "This is why you don't need an asmdef"
    People coming from normal Unity work reach for an Assembly Definition to *gain* references. Here you already have all of them, and adding an asmdef takes your scripts out of `Assembly-CSharp` — which is exactly where the mod build pipeline looks for them.

!!! warning "`ClientNetworkTransform` and `ClientNetworkRigidbody` are Fireworks Mania types"
    They live in `FireworksMania.Core.Common`, **not** in `Unity.Netcode.Components`. Generic NGO tutorials will tell you otherwise. Details on [Multiplayer & Netcode](networking.md).

---

## What you can never do

Every mod build runs a code security check over your compiled assembly. Touch anything on the deny list — `System.IO`, `System.Reflection`, `System.Runtime.InteropServices`, `System.AppDomain`, `System.Threading.Process`, `UnityEngine.Application.Quit`, P/Invoke, or the `UnityEditor` and `Mono.Cecil` assemblies — and the build **fails** rather than shipping.

On top of that: no precompiled `.dll` files — C# source only. `unsafe` code is off in the Mod Tools project (`Allow 'unsafe' Code` is unchecked in Player Settings) and there is no good reason for a mod to turn it on.

!!! info "This is why a mod can't read or write its own config file"
    `System.IO` is denied, so there is no file access of any kind from a mod. It is a deliberate trade: a player can install any mod from the internet without auditing it first. Persist your state through the blueprint system instead — see [Saving & Loading (Blueprints)](persistence.md).

The full deny list, the verbatim failure message, and what to do when you hit it are on [Setting Up Scripts in a Mod](setup.md).

---

## Netcode: supported, but advanced

Mods **can** use Netcode for GameObjects directly — `NetworkVariable<T>` fields and `[Rpc(SendTo.…)]` methods on your own `NetworkBehaviour` subclasses. This changed in **Mod Tools v2025.8.1**, when the build pipeline started running Unity's own `NetworkBehaviourILPP` code generator over the compiled mod assembly. Treat it as **supported but advanced**: the pipeline is in place and the game's own firework base class uses these features, but not every NGO construct has been proven end to end in a shipped mod.

!!! warning "Two things that are still true"
    - The obsolete `FMNetworkVariableBool` / `FMNetworkVariableInteger` / `FMNetworkVariableString` helpers are now **compile errors** by design. Use real NGO `NetworkVariable<T>` instead.
    - `MapDefinition` → **Network Object Prefabs** is a separate, still-unfixed limitation. The field's own tooltip begins: `[This is currently not working - awaiting a fix from Unity and NetCode Team]`. Do not confuse the two.

Everything about writing networked mod code lives on [Multiplayer & Netcode](networking.md).

---

## Where to next

| I want to… | Page |
|---|---|
| Get a script compiling and actually shipping inside my mod | [Setting Up Scripts in a Mod](setup.md) |
| Understand where my code starts running, and in what order | [Entry Points & Lifecycle](entry-points.md) |
| Build a firework type the game does not ship | [Writing a Custom Firework](custom-fireworks.md) |
| Make my mod behave the same for the host and for clients | [Multiplayer & Netcode](networking.md) |
| React to game events — day/night, notifications, sounds, shake | [The Messenger Event Bus](messaging.md) |
| Keep my custom state in players' saved blueprints | [Saving & Loading (Blueprints)](persistence.md) |
| Talk to game systems such as the sky, UI, input or entity database | [Services & Interfaces](services-and-interfaces.md) |

Looking for a specific component's fields rather than a how-to? Start at the [Script Reference](../script-reference/index.md). Build failing? [Troubleshooting & Build Errors](../guides/troubleshooting.md) and the [FAQ](../faq.md) cover the common ones.
