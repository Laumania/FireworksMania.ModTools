# Setting Up Scripts in a Mod

The mechanics of getting a C# file from "it exists in my project" to "it runs inside the game". If you have not read the [Scripting overview](index.md) yet, start there — it explains what a mod script is and what you are and are not allowed to write.

---

## Before you start

| Requirement | How to check |
|---|---|
| You have a mod folder, created via **Mod Tools → Create → New Mod** | It exists somewhere under `Assets/`, e.g. `Assets/Mods/YourNick_ModName/` |
| The mod folder is inside the Unity project | uMod refuses otherwise: *"Failed to create mod at path '…' because it is not inside the 'Assets' folder"*. Pointing it at `Assets` itself is refused too: *"The specified mod folder path points to the 'Assets' folder which is not allowed. Please select a subfolder of 'Assets' as the mod folder"* |
| Your C# project files have been generated | Run **Assets → Open C# Project** once. If it does nothing, install the Visual Studio or Rider editor package from the Package Manager first |
| The project compiles cleanly | No red errors in the Console. A project that does not compile in the Editor cannot be built into a mod |

If you have not built a mod at all yet, do [Getting Started](../getting-started.md) first and confirm you can produce a working `.mod` without any code. Adding scripting to a build that already works is far easier to debug than doing both at once.

---

## Step 1 — Add a `Scripts` folder

Keep code beside the rest of your mod's content, inside the mod folder:

```
Assets/
└── Mods/
    └── YourNick_ModName/
        ├── Definitions/
        ├── Icons/
        ├── Models/
        ├── Prefabs/
        └── Scripts/      ← your .cs files go here
```

The folder name does not matter — `Scripts` is just a convention. What matters is that the file sits **somewhere inside the mod folder**. uMod is explicit about this: *"Only scripts inside the mod folder will be compiled"*.

---

## Step 2 — Create a `.cs` file

Right-click inside your `Scripts` folder and use Unity's **Create** menu to add a C# script. The exact label of that entry has moved around between Unity versions, so pick whichever one creates a plain C# / MonoBehaviour script.

!!! warning "The file name must match the class name"
    This is a Unity rule, not a Mod Tools one: a `MonoBehaviour` in `HelloMod.cs` must be called `HelloMod`. If they differ, Unity cannot attach the component to a prefab, and the Inspector shows an empty script slot.

---

## Step 3 — Namespace it with your nickname

Put every class you write inside a namespace that starts with your modder nickname:

```csharp
namespace YourNick.YourMod
{
    // ...
}
```

Two concrete reasons, not just tidiness:

- **A single Unity project can hold several mods**, and in the Editor all of their scripts compile into the same `Assembly-CSharp`. If two mods both define a `Firework` class with no namespace, that is a hard compile error that stops you building either of them.
- **The [Messenger](messaging.md) bus keys events by the message type's full name**, namespace included. Unique namespaces are what stop your custom message types colliding with someone else's.

!!! warning "Class names tend to end up baked into saved blueprints"
    If you later implement `ISaveableComponent`, the key written into players' blueprint files is whatever your `SaveableComponentTypeId` property returns. Every implementation shipped with the game returns `this.GetType().Name` — the **short class name**, without the namespace — and if you follow that convention, renaming the class breaks every blueprint that already contains your item. Choose the name once and keep it forever. See [Saving & Loading (Blueprints)](persistence.md).

---

## Step 4 — Write a MonoBehaviour

A complete, minimal first script. It does nothing useful, which is the point — build this first and confirm it reaches the game before writing anything real.

```csharp
using UnityEngine;

namespace YourNick.YourMod
{
    public class HelloMod : MonoBehaviour
    {
        [SerializeField]
        private string _message = "Hello from YourMod!";

        private void Start()
        {
            Debug.Log($"[YourMod] {_message}");
        }

        private void OnDestroy()
        {
            Debug.Log("[YourMod] Cleaning up.");
        }
    }
}
```

Notes on the shape of it:

- `[SerializeField] private` rather than `public` — the field shows in the Inspector without becoming part of your public API.
- `Start()` and `OnDestroy()` are the two lifecycle hooks you can rely on everywhere, including on a prefab spawned by a [StartupPrefabDefinition](../script-reference/definitions.md).
- Prefixing logs with your mod name makes them findable in a player's log file. You are sharing the Console with the game and every other mod.

---

## Step 5 — Put it on something the game will load

A script only runs if something in your mod puts it into the scene. The routes:

| Route | Use it for |
|---|---|
| A component on a prefab referenced by an `EntityDefinition` | Behaviour that belongs to a spawnable item — a firework, a prop |
| A component on a prefab referenced by a `StartupPrefabDefinition` | Map-wide logic that should run once after the map and all mods have loaded |
| A component on a GameObject in a scene shipped as a custom map | Logic that only makes sense on that one map |

Drag `HelloMod` onto the prefab's root GameObject like any other component. Full detail on the definition-driven routes, and the order things happen in, is on [Entry Points & Lifecycle](entry-points.md); the map route is on [Custom Maps](../guides/custom-maps.md).

!!! tip "A script nobody references never runs"
    There is no auto-discovery, no plugin registry and no `[RuntimeInitializeOnLoadMethod]` entry point in Fireworks Mania. If your code is not on a prefab that an asset in your mod points at, or on an object in a map scene you ship, it is dead weight in the assembly.

---

## Step 6 — Build and check the log

Run **Mod Tools → Build Mod** (++ctrl+shift+b++) and watch the Console. When your mod contains script content you should see a line beginning:

```
Netcode for Gameobject CodeGen patching assembly:
```

That line is emitted once per compiled mod assembly, so seeing it is a good signal your scripts were picked up and compiled. If your mod has no `NetworkBehaviour` in it, the next thing you see is normal and harmless:

```
ILPP returned null (likely WillProcess == false). No changes were applied.
```

!!! note "Netcode diagnostics don't go to the Unity Console"
    The code generator's own diagnostics are written with `Console.WriteLine`, which means they land in **Editor.log**, not the Unity Console window. If a networked build fails in a way the Console cannot explain, open Editor.log.

---

## Do not add an Assembly Definition

!!! danger "An `.asmdef` in your mod folder will silently ship a mod with no code"
    This is the opposite of normal Unity advice, and it is the single most common way to end up with a build that "succeeds" but contains none of your components.

### Why

The mod build pipeline does not consume Unity's compiled assemblies. It locates the project's script `.csproj` — the one Unity generates for `Assembly-CSharp` — takes the compiler settings and references from it, and compiles the subset of `.cs` files that live inside your mod folder into a fresh assembly.[^csproj]

Unity generates **one `.csproj` per Assembly Definition**. The moment you add an `.asmdef` to your mod folder, your scripts move out of the `Assembly-CSharp` project and into `YourAsmdef.csproj` — a file the mod build pipeline is not looking at.

[^csproj]: The exact mechanism here is inferred from the diagnostic strings inside uMod's build engine assembly (`Failed to locate script project file. Scripts cannot be compiled for the mod export. Make sure the .csproj file exists`, `Failed to locate script firstpass project file…`, `found {0} potential matches when locating project file, selecting project {1}`, plus MSBuild vocabulary such as `PropertyGroup` / `ItemGroup` / `Reference` / `DefineConstants`), not from source we can read. The practical rule — keep your scripts in `Assembly-CSharp`, i.e. no `.asmdef` — is what all of the evidence points at, and it costs you nothing to follow.

=== "Correct"

    ```
    Assets/Mods/YourNick_ModName/
    ├── Prefabs/
    └── Scripts/
        └── HelloMod.cs          ← compiles into Assembly-CSharp
    ```

=== "Broken"

    ```
    Assets/Mods/YourNick_ModName/
    ├── Prefabs/
    └── Scripts/
        ├── YourNick.YourMod.asmdef   ← don't
        └── HelloMod.cs               ← now invisible to the mod build
    ```

### What the failure looks like

It is quiet. The build reports success and produces a `.mod` file — it just does not contain your code, so your components are missing when the game loads the mod.

There is no dedicated warning for it. What you have instead is the compile log: uMod prints one line per file it is actually going to compile,

```
Adding source file to build: <path>
```

so if your `.cs` files are not in that list, they are not in the mod. When nothing at all matches, it says so:

```
Script compilation will be skipped. No source files were specified!
```

Stale project files produce the same symptom, so if you see it and you have no `.asmdef`, run **Assets → Open C# Project** to regenerate and build again.

### You already have every reference an asmdef would give you

See [What you get for free](index.md) — `FireworksMania.Core`, `Unity.Netcode`, `Unity.Collections`, UniTask, DOTween, TextMeshPro and Newtonsoft.Json are all auto-referenced into `Assembly-CSharp`. There is no reference an asmdef would add that you do not already have.

---

## Build-time script diagnostics

Messages you may see during **Mod Tools → Build Mod**, quoted as they appear so you can search for them.

| Message | What it means | Fix |
|---|---|---|
| `Failed to locate script project file. Scripts cannot be compiled for the mod export. Make sure the .csproj file exists` | No `Assembly-CSharp.csproj` in the project root | **Assets → Open C# Project**, or install the Visual Studio / Rider editor package and regenerate |
| `Script compilation will be skipped. No source files were specified!` | uMod found the project file but none of your mod folder's `.cs` files in it — stale project files, or the scripts sit in their own `.asmdef` | Regenerate project files; remove the `.asmdef` |
| `Only scripts inside the mod folder will be compiled` | Informational. Code outside the mod folder is not exported | Move the code inside the mod folder |
| `Scripts will not be compiled because the API compatibility level is not supported by the target game. Please switch to a lower API level` | Player Settings API level is too high | Set **Player → Other Settings → Api Compatibility Level** back to **.NET Standard**, which is what the Mod Tools project ships with |
| `One or more C# scripts compiled into '…' failed security verification` | You used something on the deny list | See the next section |

More build errors — asset validation, scene checks — are on [Troubleshooting & Build Errors](../guides/troubleshooting.md).

---

## Security restrictions

Every mod build runs a code security check over your compiled assembly before packing it. The check exists so a player can install a mod from the internet without auditing it first: a mod cannot read their files, cannot load code at runtime, cannot call into native libraries, and cannot shut their game down.

### The deny list

| Category | Denied entries |
|---|---|
| Assembly references | `UnityEditor`, `Mono.Cecil` |
| Namespace references | `System.IO.*`, `System.Reflection.*`, `System.Runtime.InteropServices` |
| Type references | `System.AppDomain`, `System.Threading.Process` |
| Member references | `UnityEngine.Application.Quit` |
| P/Invoke | Disallowed entirely — no `[DllImport]` |

!!! note "Treat the whole area as off-limits"
    The entries above are reproduced exactly as they are stored in the Mod Tools' security settings — some carry a `.*` suffix and some do not. Rather than reasoning about which sub-namespace might slip through, just stay out of file I/O, reflection and interop altogether. There is no supported way to get at them.

### The failure message

When the check trips, the build stops during its **Running Code Validation** / **Code Security Checks** steps and you get:

```
One or more C# scripts compiled into '<YourAssembly>' failed security verification
Code security validation failed
```

The useful detail is in the per-item lines logged alongside it, one per offending reference:

```
Illegal reference to disallowed namespace: <namespace>
Indirect illegal reference via namespace exclusion to disallowed type: <type>
Indirect illegal reference via type exclusion to disallowed member: <member>
```

Read those to find out which category you tripped, then find the offending `using` in your mod folder.

### What to do instead

| You cannot | Do this instead |
|---|---|
| Read or write files (`System.IO`) — including your own config file | Put tunable values in `[SerializeField]` fields on your prefab, and persist per-instance state through the blueprint system: [Saving & Loading (Blueprints)](persistence.md) |
| Use reflection (`System.Reflection`) to poke at game internals | Reference the public `FireworksMania.Core` API directly. If something you need is not public, it is not part of the mod surface |
| Call native code (`System.Runtime.InteropServices`, `[DllImport]`) | Nothing equivalent exists. Solve it in managed C# or not at all |
| Quit the game (`UnityEngine.Application.Quit`) | Nothing. A mod is not allowed to close the player's game |
| Reference `UnityEditor` from a runtime script | Keep editor tooling **outside** your mod folder entirely — it still works for your own workflow, and it never ends up in the mod. Do not rely on `#if UNITY_EDITOR` to hide it |
| Ship a precompiled `.dll` | Vendor the third-party library as `.cs` source inside your mod folder. It has to pass the same security check, which rules out most general-purpose libraries |

---

## Sharing code between two of your own mods

A shared "common" folder does not work. Only scripts inside a given mod folder are compiled into that mod, so helper code parked elsewhere in your project builds fine in the Editor and is then missing at runtime.

If two of your mods genuinely need the same helper, copy the source into both mod folders and give each copy its own namespace — remember that in the Editor they all share one `Assembly-CSharp`, so two identically-named classes in the same namespace will not compile.

---

## Before you build, check

- [ ] Every script is inside the mod folder
- [ ] There is no `.asmdef` anywhere in the mod folder
- [ ] Every class is in a namespace starting with your nickname
- [ ] The project compiles in the Editor with no Console errors
- [ ] Your component is attached to a prefab that an `EntityDefinition` or `StartupPrefabDefinition` in your mod points at, or to an object in a map scene you ship
- [ ] The build log contains a `Netcode for Gameobject CodeGen patching assembly:` line
- [ ] No `System.IO`, `System.Reflection`, `System.Runtime.InteropServices`, `AppDomain` or `Application.Quit` anywhere in your mod's code

---

## Where to next

| I want to… | Page |
|---|---|
| Know exactly when my code runs, and in what order | [Entry Points & Lifecycle](entry-points.md) |
| Subclass the firework base class | [Writing a Custom Firework](custom-fireworks.md) |
| Make it work in multiplayer | [Multiplayer & Netcode](networking.md) |
| React to game events | [The Messenger Event Bus](messaging.md) |
| Look up a component's fields | [Script Reference](../script-reference/index.md) |
