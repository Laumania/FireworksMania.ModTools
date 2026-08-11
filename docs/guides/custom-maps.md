# Custom Maps

Everything that goes into shipping a custom map: the `MapDefinition` asset field by field, the spawn point, and the scene rules the mod build enforces.

---

## What a map mod is made of

| Piece | What it is |
|---|---|
| A **scene** (`.unity`) | The map itself. Give it a unique name — it shares a namespace with every other mod installed on the player's machine. |
| A **`MapDefinition`** asset | Points at the scene by name and carries the map's display name and environment overrides. |
| A **Player Spawn Location** | An empty prefab instance marking where the player appears. Without one you spawn at world origin. |

That is the whole contract. A map mod does not need an `EntityDefinition`, an icon, or any C# at all.

!!! tip "One mod can hold more than a map"
    Nothing stops you from putting fireworks and props in the same mod as your map. The build validates each asset type independently.

---

## Creating the MapDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Map Definition`  
**Base Class:** `ScriptableObject`

Right-click in your mod folder → **Create → Fireworks Mania → Definitions → Map Definition**. The asset is created as `New Map Definition`; rename it to something of your own.

The Inspector is grouped under five headers, in this order:

| Header | Field | Type |
|---|---|---|
| **General** | Map Name | `string` |
| **Scene** | Scene Name | `string` |
| **Game Settings** | Object Catcher Depth | `SerializableNullable<int>` |
| **Multiplayer Settings** | Network Object Prefabs | `List<GameObject>` |
| **Environment Settings** | Time / Lighting / Sky / Audio / Weather Settings | nested structs |

!!! note "There is no Thumbnails field, and no Description"
    `MapDefinition` has a `_thumbnails` field in source, but its `[SerializeField]` sits behind an internal compile symbol that is not defined in the shipped Mod Tools — so the field never appears in your Inspector, and `MapDefinition.Thumbnails` always reads back `null` from a mod-authored asset. `Description` was removed too, and is marked `[Obsolete(..., true)]`, meaning any script that reads it will not compile.

---

## The override checkbox — read this before you touch Environment Settings

Every value under **Game Settings** and **Environment Settings** draws as **a small checkbox followed by a greyed-out value field**. That is deliberate, not a bug.

Those fields are `SerializableNullable<T>` — the Mod Tools' Unity-serializable stand-in for `System.Nullable<T>`. Its custom drawer lays out a 15-pixel checkbox, then the value field, and disables the value field while the checkbox is unticked.

| Checkbox | What the game uses |
|---|---|
| **Unticked** (value greyed out) | The game's own default for that setting. Your value is ignored — it is not even readable. |
| **Ticked** | Your value, overriding the game default. |

So: **tick the box first, then set the value.** Setting the value while the box is unticked is impossible — the field is disabled — and leaving a box unticked is the correct, normal state for anything you do not want to change.

!!! warning "Untick to go back to the default"
    There is no "reset" button. Unticking the box is how you hand a setting back to the game. The value you typed stays serialized behind the checkbox, so re-ticking it brings your old value back.

In code, that maps onto `HasValue` / `Value`. Reading `Value` while `HasValue` is `false` throws `InvalidOperationException("Serializable nullable object must have a value.")`, so always check first:

```csharp
using FireworksMania.Core.Definitions;
using UnityEngine;

public class MapTimeReader : MonoBehaviour
{
    [SerializeField]
    private MapDefinition _mapDefinition;

    private void Start()
    {
        var time = _mapDefinition.TimeSettings;

        if (time.StartTimeOfDay.HasValue)
            Debug.Log($"This map starts the day at {time.StartTimeOfDay.Value}");
        else
            Debug.Log("This map uses the game's default start time");
    }
}
```

!!! warning "Name clash if you read Audio or Lighting settings from code"
    `FireworksMania.Core.Definitions` declares types called `AudioSettings`, `LightingSettings` and `GameSettings`. `UnityEngine` also has `AudioSettings` and `LightingSettings`. A script with both `using UnityEngine;` and `using FireworksMania.Core.Definitions;` that writes either name unqualified gets **CS0104: ambiguous reference**. Fully qualify it, or alias it: `using FmAudioSettings = FireworksMania.Core.Definitions.AudioSettings;`

---

## General and Scene

| Field | Type | Default | Notes |
|---|---|---|---|
| **Map Name** | `string` | `"Untitled Map"` | Tooltip: *"Name of the map. Used to display in map selection UI"*. Fill it in — but read the note below about where the name players see actually comes from. |
| **Scene Name** | `string` | *(empty)* | Tooltip: *"Exact name of the scene in your mod holds the map. Important, name your scene something unique"*. |

**Scene Name is a plain typed string, not an object reference.** Nothing validates it. If it does not exactly match your scene's filename (without `.unity`), the map will not resolve — and you will get no warning from the Mod Tools, because `MapDefinition` has no active `OnValidate`. Copy-paste the name rather than retyping it.

!!! note "Map Name may not be the name in the map list"
    The CHANGELOG entry for **v2023.1.6** says the game stopped loading map mods just to list them, and now builds the custom-map list from **mod.io metadata** — name and thumbnail as they are on mod.io. Mods loaded straight out of the local `Mods` folder get a fallback thumbnail and the mod name the Mod Tools reports instead. Either way the `MapDefinition`'s own metadata is not what the list reads.

    So set **Map Name**, but keep it consistent with your **Mod Name** and your mod.io title, because one of those two is what players will read.

---

## Game Settings

| Field | Type | Notes |
|---|---|---|
| **Object Catcher Depth** | `SerializableNullable<int>` | The Y coordinate for the game's "ObjectCatcher" plane. Per its tooltip, the catcher *"is responsible for catching the player and respawn, once the player hits it"* and also for *"catching and destroying gameobjects falling over the edge of the map"*. |

The tooltip is explicit about when to leave it alone: *"In most cases you don't want to change this, if you map is placed at the normal Y=0 level."* Override it only if your playable ground sits well above or below world zero.

---

## Environment Settings

Remember: every row below is a checkbox plus a value. Untick means "use the game's default".

### Time Settings

| Field | Type | Notes |
|---|---|---|
| **Start Time Of Day** | `SerializableNullable<float>` | Hours, with the fraction as minutes. Tooltip: *"Set the initial time of day in hours. (12.5 = 12:30)"* |
| **Start Month** | `SerializableNullable<int>` | Values 1–12. The tooltip explains why it matters: the game is *"based in northen Europe and therefore have seasons based on that"*, and *"the suns path on the sky varies a lot depending if its winter or summer"*. January (1), July (7) and December (12) look very different.[^startmonth] |

[^startmonth]: If you read this from a script, note the property is spelled `startMonth` with a lowercase first letter — an inconsistency in the shipped API, not a typo in this page.

### Lighting Settings

| Field | Type |
|---|---|
| **Ambient Intensity Curve** | `SerializableNullable<AnimationCurve>` |
| **Ambient Sky Color Gradient** | `SerializableNullable<Gradient>` |
| **Sun Intensity Curve** | `SerializableNullable<AnimationCurve>` |
| **Moon Intensity Curve** | `SerializableNullable<AnimationCurve>` |
| **Ambient Mode** | `SerializableNullable<UnityEngine.Rendering.AmbientMode>` |

### Sky Settings

| Field | Type |
|---|---|
| **Intensity Curve** | `SerializableNullable<AnimationCurve>` |
| **Intensity** | `SerializableNullable<float>` |

### Audio Settings

| Field | Type |
|---|---|
| **Ambient Day Clip** | `SerializableNullable<AudioClip>` |
| **Ambient Day Volume Curve** | `SerializableNullable<AnimationCurve>` |
| **Ambient Night Clip** | `SerializableNullable<AudioClip>` |
| **Ambient Night Volume Curve** | `SerializableNullable<AnimationCurve>` |

These take a raw `AudioClip`, not a `GameSoundDefinition` — the only place in the Mod Tools where that is true. Everything else that plays audio goes through the `[GameSound]` picker described in [Icons & Sounds](icons-and-sounds.md).

### Weather Settings

| Field | Type |
|---|---|
| **Start Weather** | `SerializableNullable<WeatherPresetType>` |

`WeatherPresetType` values, in declaration order:

| Value | Number |
|---|---|
| `ClearSky` | 0 |
| `Cloudy` | 1 |
| `Foggy` | 2 |
| `Rain` | 3 |
| `Snow` | 4 |
| `DarkCloudy` | 5 |
| `VeryFoggy` | 6 |
| `FoggySnow` | 7 |
| `Storm` | 8 |

!!! info "These settings are read by the game, not by the Mod Tools"
    Nothing in the Mod Tools package consumes the environment settings — the fields are data that the main game reads when it loads your map. That means the Mod Tools cannot show you a preview of them, and the exact in-game result of any given curve is something you confirm by building the mod and looking at it in game.

---

## The Player Spawn Location

Right-click in the **Hierarchy** → **Fireworks Mania → Maps → Player Spawn Location Prefab**.

This is a sibling of the `Parts` submenu, not a child of it — it lives directly under `Fireworks Mania`, alongside `Templates` and `Parts`. Unlike the firework templates, this one arrives as a **linked prefab instance**: it is not unpacked, so it stays connected to the shipped prefab.

The prefab carries a single component, `PlayerSpawnLocation` (`FireworksMania.Core.Common`), which holds no data at all. Its position is the spawn point; it draws a grey gizmo arrow along its forward axis in the Scene view so you can see which way it is pointing.[^spawnforward]

[^spawnforward]: The arrow gizmo is drawn along `transform.forward`. Whether the game uses that direction as the player's initial facing is decided by code that lives in the game, not in the Mod Tools package, so this page cannot confirm it either way. Point it somewhere sensible regardless.

`PlayerSpawnLocation` has no `[AddComponentMenu]` entry, so if you would rather add it to an existing GameObject than use the menu, type the name into **Add Component** to find it.

!!! danger "No spawn point builds silently, then breaks in game"
    The mod build **does** contain a check for a missing `PlayerSpawnLocation`, but the call to it is commented out in the build processor, with the author's own note that it *"just wont work and I don't get why. It doesn't find the PlayerSpawnLocation even though it's right there in the scene..."*

    The practical consequence: a map with no spawn point builds cleanly, with no warning of any kind, and then drops the player at `0,0,0` in game — which for most maps means falling through the world or standing inside the terrain. Add the spawn point yourself and verify it; the tools will not remind you.

---

## Scene rules the build enforces

Every `.unity` file in your mod is opened and checked during the build. There are three checks, and only one of them stops the build.

!!! tip "Save your scene before building"
    The check works by opening each scene in your mod. Save your work first so nothing is in an unsaved state when the build takes over the Editor.

=== "Hard failure"

    An **`EventSystem`** anywhere in a mod scene fails the build:

    ```
    Found 'EventSystem' in scene '<SceneName>' on GameObject '<GameObjectName>'.
    The game already have a EventSystem so this should not be in your scene.
    Delete the EventSystem GameObject and build the mod again.
    ```

    You almost never add an `EventSystem` on purpose. You get one because you used **GameObject → UI → Canvas** (or Text, Button, Image — any UI item), and Unity silently creates an `EventSystem` alongside it. Delete the `EventSystem` GameObject; you can keep the Canvas.

    The check scans every root object with `GetComponentsInChildren<EventSystem>(true)` — the `true` means **inactive objects count too**. Disabling the GameObject will not get you past it. Delete it.

=== "Warnings only"

    A **`Camera`** in a mod scene warns:

    ```
    Found 'Camera' in scene '<SceneName>'. Scenes should not contains a 'Camera' when used in a mod
    as it will most likely break the game when mod is loaded, unless you know what you are doing.
    ```

    Unity's default new scene ships with a `Main Camera`, so this one catches almost everyone once. The game brings its own player camera — delete yours.

    A **Directional Light** warns:

    ```
    Seems like you have a Directional Light in your scene '<SceneName>'. This will most likely make
    the day/night cycle in the game look odd, so consider removing it
    ```

    The game drives the sun and moon itself as part of the day/night cycle. Your own directional light will fight it. Delete it and shape the lighting through **Environment Settings** on the `MapDefinition` instead.

    Both of these let the build succeed. Both of the messages say the result is "most likely" broken, and in practice it is. Treat them as errors.

!!! note "Color space"
    A brand-new Unity project renders in **Gamma** color space while the game renders in **Linear**, which is why a map can look washed out or too bright compared to the game. [Getting Started](../getting-started.md) has the fix.

---

## Multiplayer: the Network Object Prefabs limitation

The **Multiplayer Settings → Network Object Prefabs** list exists, has a context-menu action to fill it in, and — by the field's own tooltip — **does not currently work**:

> [This is currently not working - awaiting a fix from Unity and NetCode Team] All objects in a map that have a NetworkObject component on them, HAVE to be a prefab instance. Add reference to the prefab itself here for it to work.

That text is the tooltip shipped on the field in `MapDefinition`. Nothing in the Mod Tools retracts it.

**What this means for you:** objects you place *in the map scene* that carry a `NetworkObject` — moveable props, anything meant to sync — should not be relied on to replicate to other players. The README puts it bluntly: you can place moveable objects in a custom map, but they will not be synced with other players. Design the map so it works without them.

!!! warning "Do not confuse this with the CodeGen limitation"
    Mod **scripts** can use Netcode for GameObjects — `NetworkVariable<T>` and `[Rpc(...)]` — since Mod Tools v2025.8.1, because the build pipeline now runs Netcode's code generation over your mod assembly. That is a different thing, it was a different limitation, and it was fixed. The map `Network Object Prefabs` field was not part of that fix. See [Multiplayer & Netcode](../scripting/networking.md).

### The Populate context menu

`MapDefinition` has one context-menu action, on the asset header: **Populate NetworkObjectPrefabs from current open scene**. It clears the list, finds every `NetworkObject` in the open scene (including inactive ones), resolves each back to its source prefab, de-duplicates and sorts alphabetically.

It refuses to run unless the open scene's name exactly matches the **Scene Name** field:

```
Unable to populate networkobject prefabs from the current scene '<OpenSceneName>',
as it is not matching the scene '<SceneName>' on '<AssetName>'
```

A scene `NetworkObject` that is not a prefab instance at all is skipped without a word — no warning, no entry. The warning `Unable to find Prefab for <GameObjectName>` appears only in the narrower case where the object *is* a prefab instance but its source asset cannot be resolved.

Nothing populates this list automatically — there is no `OnValidate` on `MapDefinition` doing it behind your back. Fill it in if you like; just do not plan your map around it working.

---

## Where to go next

| I want to… | Page |
|---|---|
| Set up the project and build a mod at all | [Getting Started](../getting-started.md) |
| See the full `MapDefinition` API surface | [Definitions](../script-reference/definitions.md) |
| Find a menu item | [Editor Menu Reference](editor-tools.md) |
| Fix a build error | [Troubleshooting & Build Errors](troubleshooting.md) |
| Keep the map's file size down | [Optimization](../optimization.md) |
| Ship it | [Publishing Your Mod](publishing.md) |
