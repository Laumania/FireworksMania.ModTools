# Troubleshooting & Build Errors

This page is for the moment something goes wrong. Every message below is quoted exactly as the Console prints it, so you can paste your error into the search box at the top of the page and land on the right row.

---

## How to Read a Fireworks Mania Error

Most problems announce themselves in the Unity Console before they ever reach the game. There are three different kinds and they look very different from each other.

| Kind | Looks like | What it means |
|---|---|---|
| **Build failure** | A red error, then the build stops without producing a `.mod` file | The mod build pipeline refused your content. Fix it and build again. |
| **Validation error** | A red `Missing ...` error while you edit a prefab or a definition | Something is unassigned in the Inspector. The mod will still build, but the item will misbehave in game. |
| **Exception** | A red error with a stack trace, often naming only a type | A null check failed at runtime. See **An exception that names only a type** under Runtime & Editor Problems below. |

In every message, `{...}` is filled in at runtime with your asset's name — search for the words around it, not the whole line.

!!! tip "Click the message"
    Almost every error in the Mod Tools is logged with a context object. Click the message in the Console and Unity pings the exact GameObject or asset in the Hierarchy or Project window. This is usually faster than reading the message.

---

## Build-Blocking Errors

These stop the build. Nothing is exported until you fix them.

### EntityDefinition validation

Every `.asset` in your mod folder is checked at build time. If it is an `EntityDefinition`, these four rules apply.

| Message | Cause | Fix |
|---|---|---|
| `'{name}' (BaseEntityDefinition) is not referencing any prefab. This is required.` | **Prefab Game Object** is empty on the definition | Drag your prefab into **Prefab Game Object** |
| `'{prefab}' (Prefab) is missing reference to an EntityDefinition` | A component on the prefab root wants an `EntityDefinition` but its field is empty | Select the prefab root and assign **Entity Definition** |
| `'{prefab}' (Prefab) is not referencing '{definition}' (EntityDefinition) which is should, as '{definition}' is referencing '{prefab}'` | The two-way link is crossed — the definition points at the prefab, but the prefab points at a *different* definition | Make both point at each other |
| `'{name}' (BaseInventoryEntityDefinition) is missing an EntityDefinitionType. This is required.` | **Entity Definition Type** is empty | Assign one of the shipped types — see the note about the eye icon below |

!!! warning "The definition ↔ prefab link goes both ways"
    This is the single most common build failure. The definition must reference the prefab **and** the prefab must reference the definition. Duplicating either asset to make a variant is what usually breaks it, because the copy keeps pointing at the original's partner.

    The crossed-link error is deliberately logged **twice** — once ending in `(Click to select Prefab)` and once ending in `(Click to select EntityDefinition)` — so you can click through to either side and fix whichever one is wrong.

### Scene validation

| Message | Cause | Fix |
|---|---|---|
| `Found 'EventSystem' in scene '{scene}' on GameObject '{go}'. The game already have a EventSystem so this should not be in your scene. Delete the EventSystem GameObject and build the mod again.` | An `EventSystem` component exists anywhere in a scene included in your mod | Delete the `EventSystem` GameObject and rebuild |

!!! note "Where the EventSystem came from"
    You almost certainly did not add it on purpose. Unity creates an `EventSystem` automatically the first time you add a UI Canvas via **GameObject → UI**. The check looks at inactive objects too, and at every depth — so search the whole Hierarchy, not just the roots.

### Script compilation (only if your mod contains C#)

If your mod has no `.cs` files, none of these can happen to you.

| Message | Cause | Fix |
|---|---|---|
| `Failed to locate script project file. Scripts cannot be compiled for the mod export. Make sure the .csproj file exists` | Unity has never generated the C# project files | **Assets → Open C# Project**, or install the Visual Studio / Rider editor package, then rebuild |
| `The C# source file '{0}' exists in the Unity project but not in the .csproj file and will not be compiled. You may need to regenerate the script project file` | Stale project files, or the script was moved into its own assembly definition | Regenerate the project files. If you added an `.asmdef`, remove it — see [Setting Up Scripts in a Mod](../scripting/setup.md) |
| `Assembly '{0}' has failed code security verification. Illegal Assembly Reference = '{1}', Illegal Namespace References = '{2}', Illegal Type References = '{3}', Illegal Member References = '{4}', Illegal PInvoke References = '{5}'` | Your code touches something on the deny list — `System.IO`, `System.Reflection`, interop, `AppDomain`, `Application.Quit`, P/Invoke, `UnityEditor` | Remove the offending code. The full deny list is on [Setting Up Scripts in a Mod](../scripting/setup.md) |

### Netcode CodeGen

The build runs Unity's Netcode for GameObjects code generator over your compiled mod assembly. When that machinery cannot start, it throws.

| Message | What it means |
|---|---|
| `Cannot find 'Unity.Netcode.Editor.CodeGen' assembly.` | The Netcode for GameObjects package is missing or failed to import |
| `Cannot find NetworkBehaviourILPP type.` | Same family — the installed Netcode version is not what the Mod Tools expect |
| `Cannot find Process method.` | Same family |
| `Unity.CompilationPipeline.Common.dll not loaded` | The Editor has not loaded a required assembly. Restart Unity |
| `Referenced assembly '{name}' not found in AppDomain.` | Your mod script references an assembly the Editor has not loaded. Restart Unity, then rebuild |
| `Failed to register patches assembly for build! : {assemblyName}` | The patched assembly was rejected. Restart Unity and rebuild; if it persists, temporarily remove your `NetworkBehaviour` scripts to confirm they are the trigger |

!!! info "CodeGen diagnostics do not appear in the Console"
    The code generator writes its own diagnostics with `Console.WriteLine`, which lands in Unity's **Editor.log**, not in the Console window. If a Netcode-related build failure gives you nothing useful on screen, open the Editor log.

    `Netcode for Gameobject CodeGen patching assembly: '{assemblyName}'` is **not** an error. It is the normal log line printed on every build that contains scripts.

---

## Build Warnings

These do **not** stop the build, but they are almost always worth acting on.

| Message | Cause | Fix |
|---|---|---|
| `Found 'Camera' in scene '{scene}'. Scenes should not contains a 'Camera' when used in a mod as it will most likely break the game when mod is loaded, unless you know what you are doing.` | A `Camera` in a mod scene — Unity's default scene ships with a Main Camera | Delete the Main Camera from your map scene |
| `Seems like you have a Directional Light in your scene '{scene}'. This will most likely make the day/night cycle in the game look odd, so consider removing it` | A `Light` with type Directional in a mod scene | Delete it — the game drives its own sun and moon |

See [Custom Maps](custom-maps.md) for what a map scene should and should not contain.

---

## Runtime & Editor Problems

### Assets show as missing, FuseIndicator disappears, references break for no reason

This is the classic "Unity got confused" cluster. Work down the list.

| Step | Action |
|---|---|
| 1 | Right-click in the Project window → **Reimport All** |
| 2 | Restart Unity |
| 3 | **Mod Tools → Referencing → Rebuild Reference Cache** |
| 4 | If you just upgraded the Mod Tools: restart Unity *again*. The CHANGELOG flags this as very important |

!!! tip "After every Mod Tools upgrade"
    Back up your project first, then upgrade, then restart Unity. If components come back as **Missing (Mono Script)** afterwards, see [Editor Menu Reference](editor-tools.md) for the upgrade utilities under **Mod Tools → Utilities → Upgrade**.

### I can't find the Fireworks Mania assets in the object picker

Fields like **Entity Definition Type** and the shell/mortar **Diameter** can only be filled with assets that ship inside the Mod Tools *package*, and Unity hides package assets from the object picker by default.

Click the small **eye icon** in the object picker window to toggle package assets on. They appear immediately.

### An exception that names only a type

You get an `ArgumentNullException` with a stack trace, and the only useful part of it is the parameter name — a namespace-qualified type name in quotes, followed by a hierarchy path:

```
'FireworksMania.Core.Behaviors.Fireworks.Parts.FuseConnectionPoint' (Hierarchy Path: 'MyRocket/Fuse')
```

That is a null check firing with no custom message. It means **a reference of that type is null**. The type name tells you *what* is missing; the `Hierarchy Path` tells you *which child object* it is missing on. Select that object and look for an empty field of that type.

Because these are exceptions rather than logged errors, they read very differently from the `Missing ...` messages — but the cure is the same: fill in the empty Inspector field.

### DependencyResolver.Instance is null

Expected. The `DependencyResolver` class ships in the Mod Tools, but the instance and every service it hands out are created by the **game** — so in the Editor there is nothing to resolve and `Instance` stays null. `Get<T>()` also only searches **active** MonoBehaviours and returns `null` rather than throwing when it finds nothing.

Always write `DependencyResolver.Instance?.Get<IMyService>()` and null-check the result. See [Services & Interfaces](../scripting/services-and-interfaces.md).

### My mod does not show up in the game

| Check | Detail |
|---|---|
| Did the build actually succeed? | Scroll up in the Console. A failed build produces no `.mod` file |
| Is the export directory right? | **Mod Tools → Export Settings → Mod Export Directory** should point at `%userprofile%\appdata\locallow\Laumania ApS\Fireworks mania\Mods` |
| Did you restart the map? | The game reloads a changed mod when you restart the map, not while you are standing in it |
| Are you testing in multiplayer? | Test in **singleplayer**. Other players cannot see your mod unless they have installed it themselves |

### My changes are not showing up after rebuilding

Rebuild, then use **Restart Map** inside the game. The game notices the `.mod` file changed and reloads it. Nothing you do in Unity reaches a map that is already running.

### The player spawns at 0,0,0 in my map

Your map has no spawn point. Add one with **GameObject → Fireworks Mania → Maps → Player Spawn Location Prefab**.

!!! warning "You will get no warning about this"
    The build-time check for a missing spawn location is commented out in the build processor, with a note from the author that it did not work reliably. So a map with no spawn point builds completely silently. Check it yourself.

### My prefab keeps getting marked as edited every time I click it

Netcode requires `NetworkObject` to be the first component on a GameObject, so Unity reorders it and dirties the prefab. When Unity prompts you about the prefab being edited, press **Keep Changes** — it should settle down afterwards.

For a scene full of them there is **Mod Tools → Utilities → Multiplayer → Revert All NetworkObject Overrides In Current Scene**.

### Preview generation does nothing

First check you are using the right menu family. The `Assets → Fireworks Mania → Generate Preview → ...` entries work on a **prefab asset** selected in the Project window; the `GameObject → Fireworks Mania → Generate Preview → Perspective → Current Veiw In Scene` entry works on a **scene instance**. (The misspelling of "Veiw" is in the source.)

The `Assets/...` entries return silently when the selection is not a prefab asset, or is an imported model — so nothing happens and nothing is logged. Select the prefab itself, not an `.fbx` and not a scene object.

If generation ran but produced no image you will see `Failed to Produce Texture` or `Texture Could not be Read` instead. A successful run logs `Saved generated preview '{name}' at path: {path}` and pings the new asset. See [Icons & Sounds](icons-and-sounds.md).

---

## Common "Missing ..." Errors

These fire in the Inspector while you edit, at `Awake` in Play mode, or both. They all mean the same thing: an Inspector field is empty. The table is here so the exact wording is searchable.

| Message | What to assign |
|---|---|
| `Missing '{FireworkEntityDefinition}' on '{go}'` | The **Entity Definition** field on the firework root |
| `Missing {FireworkEntityDefinition} on '{go}' - everything will go wrong this way!` | Same field, reported at runtime. The behavior gives up immediately |
| `Missing {Fuse} on '{go}' - this is not gonna work! Make sure this fireworks have a fuse.` | The **Fuse** field. At runtime this also disables the component |
| `Missing '{SaveableEntity}' which is a required component - make sure '{name}' have one` | Add a `SaveableEntity`. Fireworks get one added for you in the Editor; **props do not** |
| `'{id}' have '{n}' '{SaveableEntity}'s' - it can have one and only one - please delete so only one is left else it will be saved multiple times in blueprints` | Delete the duplicate `SaveableEntity` components |
| `Missing {EntityDiameterDefinition} on {go}` | The **Diameter** field on a shell or mortar. Diameter assets live in the package — use the eye icon |
| `Missing {UnwrappedShellFusePrefab} on {go}` | The unwrapped fuse prefab on a shell |
| `Prefab referenced in {UnwrappedShellFusePrefab} on {go} does not seem to have the 'UnwrappedShellFuse' component on it - which is required` | You dragged in the wrong prefab |
| `Missing {IgnitePosition} on {go}` | The **Ignite Position** transform on an `UnwrappedShellFuse`. Its rotation matters — a yellow gizmo arrow is drawn along the transform's **up** axis to show the direction |
| `Missing Fuse Connection Point on 'FireworksMania.Core.Behaviors.Fireworks.Parts.Fuse' on gameobject '{go}'` | The **Fuse Connection Point** field on the `Fuse` component |
| `Missing active indicator on '{name}'` | The **Active Indicator** on a `FuseConnectionPoint`. This is the "missing FuseIndicator" family |
| `ParticleSystemExplosion is missing ParticleSystemObserver on '{go}' else it will not work` | Add a `ParticleSystemObserver` to the same GameObject |
| `ParticleSystemSound is missing ParticleSystemObserver on '{go}' else it will not work` | Same |
| `ParticleSystemObserver is missing ParticleSystem on '{go}' else it will not work` | The observer needs a `ParticleSystem` on its own GameObject |
| `MortarTubeTop (on {go}) requieres at least one collider that is marked as a trigger to be able to know when a shell is inserted into the MortarTube` | Add a `Collider` with **Is Trigger** ticked to the `MortarTubeTop`. The spelling of "requieres" is in the source |
| `Please update unique id to something unique` | The definition's **Id** is still `INSERT UNIQUE DEFINITION ID`. Right-click the Inspector header → **Set Id to filename** |
| `'{BaseEntityDefinition}' is missing on component '{Type}' on '{go}', please fix else save/load won't work` | The **Entity Definition** on a `SaveableEntity` |

Field-by-field detail for these components lives in [Behaviors](../script-reference/behaviors.md) and [Firework Parts](../script-reference/firework-parts.md).

---

## Messages That Are Misleading

A handful of messages describe something that did not actually happen. They are not lying on purpose — the code around them changed and the text did not. If you read one of these, **do not trust the message; go and check yourself.**

| Message | What it claims | What actually happens |
|---|---|---|
| `'{go}' was missing ParticleSystemObserver so it was added automatically` | A `ParticleSystemObserver` was added for you | Nothing was added — the line that would have added it is commented out. Add the component yourself, or the object will throw a `NullReferenceException` when it is disabled |
| `'{id}' have '{n}' '{ErasableBehavior}'s' it should have one and only one - removing all the extra ones` | The extra components were removed | Nothing was removed. Delete the duplicate `ErasableBehavior` components by hand |
| `Missing Rigidbody on rocket` on a **smoke bomb** | You are looking at a rocket | Copy-pasted wording. The word "rocket" is wrong — trust the object the Console pings, not the noun in the sentence. The same applies to `Missing model reference in rocket`, `Missing Fuse on rocket` and `Missing Thruster on rocket - this is not gonna fly!` on a **whistler** |
| `Unable to call RequestIgniteServerOnly if not IsServer` | You called an API called `RequestIgniteServerOnly` incorrectly | There is no such API anywhere in the Mod Tools. It is a stale message inside an internal networking check, and it is not something your code can cause |
| `Debris layer name not found!` | A layer is missing | Unreachable. The check compares a bit-shifted layer mask against `-1`, a value that expression can never produce, so this line cannot print at all |
| *(silence)* about a missing `PlayerSpawnLocation` | Your map has a spawn point | The check is disabled. No message means nothing at all — verify by looking for the spawn prefab in the scene |

!!! note "One more place where written guidance drifts"
    The CHANGELOG mentions a context-menu action called **Set Id as filename**. The real label is **Set Id to filename**.

---

## Still Stuck?

| Where to look | For what |
|---|---|
| [FAQ](../faq.md) | Short answers to the questions that come up most |
| [Getting Started](../getting-started.md) | The full install-to-first-mod walkthrough |
| [Editor Menu Reference](editor-tools.md) | Every Mod Tools menu item, including the repair utilities |
| [Best Practices](../best-practices.md) | Conventions that prevent most of the errors on this page |
| [Setting Up Scripts in a Mod](../scripting/setup.md) | If the failure is anything to do with C# |
| [Publishing Your Mod](publishing.md) | The pre-flight checklist before you upload |
