# Definitions

Definitions are [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) assets that describe items, maps, sounds, characters and startup logic in Fireworks Mania. They act as data containers — they hold configuration values but contain no runtime behaviour themselves.

---

## What You Can Create

Right-click in the Project window → **Create → Fireworks Mania → Definitions →** …

| Menu entry | Type | Used for |
|---|---|---|
| Firework Entity Definition | `FireworkEntityDefinition` | Any item that is a firework |
| Prop Entity Definition | `PropEntityDefinition` | Any item that is not a firework |
| Map Definition | `MapDefinition` | A custom map |
| Game Sound Definition | `GameSoundDefinition` | A sound your mod can play |
| StartupPrefab Definition | `StartupPrefabDefinition` | Map-wide logic that runs on load |
| Character Definition | `CharacterDefinition` | A playable character model |

That is the complete list. `EntityDefinitionType`, `EntityDiameterDefinition` and `SoundCollection` are also definitions, but their Create menu entries are compiled out of the shipped Mod Tools — you reference the assets that ship rather than authoring your own.

---

## EntityDefinition Hierarchy

```
BaseEntityDefinition (abstract)
└── BaseInventoryEntityDefinition (abstract)
    ├── FireworkEntityDefinition
    └── PropEntityDefinition
```

---

## BaseEntityDefinition

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Type:** `abstract ScriptableObject`

The root base class for all entity definitions. Every item that can be spawned in Fireworks Mania has a definition that ultimately derives from this class.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Id** | `string` | **Globally unique** identifier for this entity. Used to save and restore the entity in blueprints. Set this once and never change it after publishing. Use the **Set Id to filename** context-menu action to populate it automatically from the asset filename. |
| **Prefab Game Object** | `GameObject` | The prefab that will be instantiated in the game world when this entity is spawned. |

### Notes

- The `Id` field defaults to `"INSERT UNIQUE DEFINITION ID"` as a reminder to set it. Leaving it at the default value will produce a console error.
- The `SetIdToFilename()` context-menu method sets `Id` to match the asset filename — the recommended approach for consistency.

---

## BaseInventoryEntityDefinition

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Type:** `abstract ScriptableObject` (extends `BaseEntityDefinition`)

Extends `BaseEntityDefinition` with the fields needed for items that appear in the player inventory.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Item Name** | `string` | Display name shown in the inventory UI. Defaults to `"Untitled Entity Definition"`. |
| **Icon** | `Sprite` | Inventory thumbnail sprite. Recommended size: 256×256 or 512×512 pixels. |
| **Entity Definition Type** | `EntityDefinitionType` | Reference to the category the item is grouped under in the inventory. You cannot create your own — pick one of the shipped assets listed under **Entity Definition Types** below. |

!!! note "Validation arrives one editor tick late"
    Only **Entity Definition Type** and **Prefab Game Object** are validated, and the check is deferred to the next editor tick. The resulting `Missing 'EntityDefinitionType' on '<AssetName>'` error therefore appears in the Console a moment after you edited the asset, which can make it look like it came from something else. **Item Name** and **Icon** are never validated.

---

## FireworkEntityDefinition

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Menu:** `Fireworks Mania/Definitions/Firework Entity Definition`  
**Type:** `ScriptableObject` (extends `BaseInventoryEntityDefinition`)

Definition for any item that is a firework. The prefab referenced by this definition must have a component that extends `BaseFireworkBehavior`.

### Usage

1. Right-click in your `Definitions` folder → **Create → Fireworks Mania → Definitions → Firework Entity Definition**.
2. Set the `Id` field (or use **Set Id to filename**).
3. Assign the firework prefab to **Prefab Game Object**.
4. Fill in **Item Name**, **Icon**, and **Entity Definition Type**.

---

## PropEntityDefinition

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Menu:** `Fireworks Mania/Definitions/Prop Entity Definition`  
**Type:** `ScriptableObject` (extends `BaseInventoryEntityDefinition`)

Definition for a static or interactive prop (non-firework item). Use this for decorative objects, furniture, terrain decorations, and similar items.

### Usage

Identical to `FireworkEntityDefinition` — only use `PropEntityDefinition` when the item is **not** a firework.

---

## Entity Definition Types

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Type:** `ScriptableObject`

The inventory category an item appears under. Every `BaseInventoryEntityDefinition` must reference one.

!!! warning "You cannot create your own category"
    `EntityDefinitionType`'s Create menu entry is compiled out of the shipped Mod Tools, so there is no **Create → Fireworks Mania → Definitions → Entity Definition Type**. Pick one of the eleven assets that ship instead — that is the whole list.

| Asset | Use it for |
|---|---|
| `Fireworks_Cake` | Cakes |
| `Fireworks_Firecracker` | Firecrackers |
| `Fireworks_Fountains` | Fountains |
| `Fireworks_Mortar` | Mortars, mortar racks, single-shot racks |
| `Fireworks_Novelty` | Novelty items — roman candles, whistlers, zippers |
| `Fireworks_Rocket` | Rockets |
| `Fireworks_Shell` | Shells |
| `Fireworks_Smoke` | Smoke bombs |
| `Fireworks_Tube` | Preloaded tubes |
| `Hidden` | Items that should not show up in the inventory list |
| `Prop` | Everything that is not a firework — use this for `PropEntityDefinition` |

They live in the package under `FireworksMania/Resources/EntityDefinitionTypes/`. If they do not show up in the object picker, enable the **eye icon** in the picker window so it includes package assets.

!!! tip "Not sure which one to pick?"
    Look at the sample definitions in `FireworksMania/Resources/ModSamples/Definitions/` — there is one per firework type and each already has a sensible category assigned. Note that the shipped Fountain sample uses `Fireworks_Novelty`, not `Fireworks_Fountains`.

---

## Entity Diameter Definitions

**Namespace:** `FireworksMania.Core.Definitions.EntityDefinitions`  
**Type:** `ScriptableObject`

Describes a shell diameter in inches. `ShellBehavior`, `MortarTube` and `MortarBehavior` each reference one. A shell loads into a tube when the shell's diameter is less than or equal to the tube's **and** the shell is not already ignited.

Like `EntityDefinitionType`, these are **not creatable by modders** — the Create menu entry is compiled out. Ten assets ship, in `FireworksMania/Resources/EntityDiameterDefinitions/`:

| Asset | Diameter (inches) |
|---|---|
| `Diameter_1_75_Inch` | 1.75 |
| `Diameter_2_Inch` | 2 |
| `Diameter_3_Inch` | 3 |
| `Diameter_4_Inch` | 4 |
| `Diameter_5_Inch` | 5 |
| `Diameter_6_Inch` | 6 |
| `Diameter_8_Inch` | 8 |
| `Diameter_10_Inch` | 10 |
| `Diameter_12_Inch` | 12 |
| `Diameter_16_Inch` | 16 |

Leaving one off a shell, a mortar or a tube logs `Missing EntityDiameterDefinition on <GameObjectName>`.

---

## MapDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Map Definition`  
**Type:** `ScriptableObject`

Describes a custom map (level). A `MapDefinition` asset must be present in any mod that adds a new map to the game.

### Inspector Fields

The Inspector groups the fields under five headers, in this order.

#### General

| Field | Type | Description |
|---|---|---|
| **Map Name** | `string` | Display name of the map shown in the map selection UI. Defaults to `"Untitled Map"`. |

#### Scene

| Field | Type | Description |
|---|---|---|
| **Scene Name** | `string` | The exact name of the Unity scene that contains the map. Must match the scene file name precisely. Use a unique name to avoid conflicts with other mods. |

#### Game Settings

| Field | Type | Description |
|---|---|---|
| **Object Catcher Depth** | `SerializableNullable<int>` | Y coordinate the game's **ObjectCatcher** is positioned at. It catches the player and respawns them, and it catches and destroys objects that fall off the edge of the map. Normally placed not far below ground level. If your map sits at the usual Y=0, leave this alone — see *Overriding a setting* below. |

#### Multiplayer Settings

| Field | Type | Description |
|---|---|---|
| **Network Object Prefabs** | `List<GameObject>` | Intended to hold every prefab in the scene that has a `NetworkObject` component. Objects with a `NetworkObject` in a map have to be prefab instances, and the prefab itself is meant to be referenced here. |

!!! warning "Network Object Prefabs is currently not working"
    The field's own tooltip in the Inspector opens with:

    > [This is currently not working - awaiting a fix from Unity and NetCode Team]

    So filling this list in will not make in-scene `NetworkObject`s behave correctly in a mod map, and no amount of populating it will. Do not design a map around in-scene networked objects yet.

    This limitation is **separate** from mod C# scripts, which *can* use Netcode for GameObjects — see [Multiplayer & Netcode](../scripting/networking.md).

The **Populate NetworkObjectPrefabs from current open scene** context-menu action on the asset header still exists and still fills the list, but it refuses to run unless the open scene's name matches the **Scene Name** field exactly. [Custom Maps](../guides/custom-maps.md) has the detail and the exact warning text.

#### Environment Settings

Five nested groups: **Time Settings**, **Lighting Settings**, **Sky Settings**, **Audio Settings** and **Weather Settings**. Every field inside them is optional — see *Overriding a setting* below.

| Section | Field | Description |
|---|---|---|
| **Time Settings** | Start Time Of Day | Initial time of day in hours (`12.5` = 12:30). |
| **Time Settings** | Start Month | Initial month, 1–12. The game is set in northern Europe, so the sun's path — and therefore the whole day/night cycle — looks very different in January (1), July (7) and December (12). |
| **Lighting Settings** | Ambient Intensity Curve | Ambient light intensity across the day/night cycle. |
| **Lighting Settings** | Ambient Sky Color Gradient | Ambient sky colour gradient. |
| **Lighting Settings** | Sun Intensity Curve | Sun directional light intensity. |
| **Lighting Settings** | Moon Intensity Curve | Moon directional light intensity. |
| **Lighting Settings** | Ambient Mode | Unity's `UnityEngine.Rendering.AmbientMode`. |
| **Sky Settings** | Intensity Curve / Intensity | Sky exposure. |
| **Audio Settings** | Ambient Day Clip / Ambient Day Volume Curve | Clip and volume curve for daytime ambience. |
| **Audio Settings** | Ambient Night Clip / Ambient Night Volume Curve | Clip and volume curve for nighttime ambience. |
| **Weather Settings** | Start Weather | Initial weather preset for the map (see the table below). |

#### Overriding a Setting

Every field under **Game Settings** and **Environment Settings** is a `SerializableNullable<T>` (`FireworksMania.Core.Common`), which draws as a **small checkbox followed by the value field**:

- **Unticked** — the value field is greyed out and the game uses its own default for that setting.
- **Ticked** — the value field becomes editable and your value overrides the game default.

This catches nearly everyone the first time. If a curve or clip you set is being ignored, check whether its checkbox is actually ticked.

!!! tip "Only tick what you actually want to change"
    A `MapDefinition` with nothing ticked is perfectly valid — the map simply inherits the game's defaults, and keeps inheriting improvements to them. Tick a box only when you have a reason to differ.

### Weather Presets

`WeatherPresetType`, the type behind **Start Weather**. The numbers are what Unity writes into the `.asset` file.

| Value | Serialized as |
|---|---|
| `ClearSky` | `0` |
| `Cloudy` | `1` |
| `Foggy` | `2` |
| `Rain` | `3` |
| `Snow` | `4` |
| `DarkCloudy` | `5` |
| `VeryFoggy` | `6` |
| `FoggySnow` | `7` |
| `Storm` | `8` |

### Reading a MapDefinition from Code

Two things bite scripters here:

- `MapDefinition.Thumbnails` always returns `null` for a mod-authored asset. The backing field is only serialized in the internal build of the Mod Tools, so there is no Thumbnails field in your Inspector and nothing to read back.
- `MapDefinition.Description` is `[Obsolete(..., true)]` — referencing it is a **compile error**, not a warning.

!!! warning "`AudioSettings` and `LightingSettings` clash with Unity's own types"
    `FireworksMania.Core.Definitions` declares public types named `AudioSettings`, `LightingSettings` and `GameSettings`. `UnityEngine` also has `AudioSettings` and `LightingSettings`, so a script with both `using UnityEngine;` and `using FireworksMania.Core.Definitions;` that names either of them unqualified fails with **CS0104: ambiguous reference**. Alias one of them:

    ```csharp
    using UnityEngine;
    using FireworksMania.Core.Definitions;
    using FmAudioSettings = FireworksMania.Core.Definitions.AudioSettings;

    public class MapSettingsReader : MonoBehaviour
    {
        [SerializeField]
        private MapDefinition _mapDefinition;

        private void Start()
        {
            FmAudioSettings audio = _mapDefinition.AudioSettings;
            if (audio.AmbientDayClip.HasValue)
                Debug.Log($"Day ambience: {audio.AmbientDayClip.Value.name}");
        }
    }
    ```

    Note the `HasValue` check — that is the `SerializableNullable<T>` checkbox from the Inspector. Reading `.Value` when the box was never ticked throws `InvalidOperationException`.

---

## GameSoundDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Game Sound Definition`  
**Type:** `ScriptableObject`

Defines a sound effect used anywhere in the game. Components never reference an `AudioClip` directly — they store a **name**, which is either the asset name of a `GameSoundDefinition` or one of the game's built-in sound names.

### Inspector Fields

| Header | Field | Type | Default | Description |
|---|---|---|---|---|
| **General** | **Volume** | `float`, `[Range(0,1)]` | `1` | Base playback volume. |
| **General** | **Loop** | `bool` | `false` | Whether the sound should loop continuously. |
| **General** | **Sound Bus** | `SoundBusGroups` | `Default` | Routing category (see below). |
| **Audio** | **Audio Variation Clips** | `AudioClip[]` | empty | One or more clips. With several clips the game picks one at random on each play — worth doing for anything the player hears often, so the same explosion doesn't sound identical every time. |
| **Distance** | **Min Distance** | `float` | `0` | If the player is closer to the sound than this (in metres), the volume is not lowered. |
| **Distance** | **Max Distance** | `float` | `100` | If the player is further away than this (in metres), the sound is not heard. |
| **Custom Fade** | **Fade In Time** | `float`, `[Range(0,10)]` | `0` | Fade time in seconds for when the audio is played. |
| **Custom Fade** | **Fade Out Time** | `float`, `[Range(0,10)]` | `0` | Fade time in seconds for when the audio is stopped. |
| **Custom Pitch** | **Random Pitch Min** | `float`, `[Range(-3,3)]` | `-0.1` | The minimum random pitch. |
| **Custom Pitch** | **Random Pitch Max** | `float`, `[Range(-3,3)]` | `0.1` | The maximum random pitch. |

The **Custom Pitch** pair is easy to overlook — it sits at the very bottom of the Inspector. Note that the defaults (`-0.1` / `0.1`) sit either side of zero rather than either side of one, which reads like an offset applied to the normal pitch rather than an absolute pitch value. The code that consumes them lives in the game, not in the Mod Tools package, so take that as a hint and tune by ear.[^pitch]

### Sound Bus Groups

| Value | Serialized as | Description |
|---|---|---|
| `Default` | `3` | Used for most sounds. |
| `Ambient` | `0` | Ambient sounds — forced to 2D. |
| `UI` | `1` | UI sounds — forced to 2D. |
| `Explosion` | `2` | Loud explosions. Sounds of this type duck other sounds for a short while to emphasise how loud they are. |

The descriptions come from the field's own tooltip; the routing, 2D forcing and ducking are implemented in the game rather than in the Mod Tools package.[^soundbus]

!!! warning "`Default` is 3 and `Ambient` is 0"
    This enum is not numbered from zero. Unity serializes an enum as its number, and anything missing reads back as `0` — which here means **`Ambient`**, not `Default`. So an old `GameSoundDefinition` saved before this field existed, or a hand-edited `.asset` with `_soundBus: 0`, silently becomes an Ambient sound and is forced to 2D.

    If one of your sounds plays at the same volume no matter where the player stands, check **Sound Bus** first.

### Referencing Sounds in Components

Components that play sounds expose a `string` field decorated with `[GameSound]` (`FireworksMania.Core.Attributes`). It is **not** a normal drop-down — it opens Unity's searchable **Game Sounds** window. The window is built from two sources: every `SoundCollection` asset in the project, whose entries are grouped under **Fireworks Mania**, and every `GameSoundDefinition` in the project, grouped under **Others**. The Mod Tools ship one `SoundCollection` — `GameSoundCollection`, in `FireworksMania/Resources/EntityDefinitions/` — and that is where the built-in sound names come from.

Only the **leaf name** of whatever you pick is written into the string field. The collection's first two entries are the two special values: `[Type In]` and `[None]`, the latter meaning "no sound". [Icons & Sounds](../guides/icons-and-sounds.md) covers the picker and how to author a sound of your own.

!!! tip "Use `[GameSound]` on your own scripts too"
    It gives you the same searchable picker instead of a raw text box you can typo into:

    ```csharp
    using FireworksMania.Core.Attributes;
    using UnityEngine;

    public class MyNoisyThing : MonoBehaviour
    {
        [SerializeField]
        [GameSound]
        private string _sound = "[None]";
    }
    ```

    `GameSoundAttribute` is a bare `PropertyAttribute` with no members, so there is no constant to reference — write the `[None]` string literally.

!!! warning "Configure sounds in the Inspector, not from code"
    Outside the Unity Editor, every `GameSoundDefinition` property (`Volume`, `Loop`, `SoundBus`, `AudioVariationClips`, the distances, fades and pitches) is **get-only** — the setters only exist under `UNITY_EDITOR`. A mod script that assigns to them may compile happily in the Editor and then fail to build. Author the values on the asset instead.

---

## StartupPrefabDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/StartupPrefab Definition`  
**Type:** `ScriptableObject`

Defines a prefab that the game instantiates once in the map after all mods have finished loading. It is the entry point for mod logic that is not attached to a spawnable entity.

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Prefab Game Object** | `GameObject` | `null` | A single instance of this prefab is instantiated in the map after all mods have been loaded. Put your startup scripts on it and run your logic from `Start()` and `OnDestroy()`. |
| **Sort Order** | `int` | `0` | Startup prefabs are instantiated sorted by this value, lowest first. Use it when the initialisation order between several startup prefabs matters. |

!!! note "Where this behaviour is documented"
    The asset itself is a plain data container with two fields and nothing else. Everything above — that exactly one instance is created, that it happens after all mods have loaded, and that **Sort Order** decides the order — is what the two fields' Inspector tooltips say. The code that reads the asset and does the instantiating lives in the game, not in the Mod Tools package, so none of it can be verified from here.[^startup]

### Use Cases

- Running custom initialisation logic on map load.
- Registering event listeners.
- Spawning persistent manager objects.

Place your startup logic in `Start()` and cleanup in `OnDestroy()` on a `MonoBehaviour` on the prefab. [Entry Points & Lifecycle](../scripting/entry-points.md) walks through this in full.

---

## CharacterDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Character Definition`  
**Type:** `ScriptableObject`

Describes a character model — the prefab, the Humanoid `Avatar` it animates with, and the name and icon used to present it.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Id** | `string` | Globally unique id for this character definition. |
| **Name** | `string` | Human readable name of this character model. |
| **Icon** | `Sprite` | Icon for the character. |
| **Character Prefab** | `GameObject` | The prefab with the character. |
| **Character Avatar** | `Avatar` | The Humanoid Avatar this character uses for animations. |

The **Set Id to filename** context-menu action is available here too. Unlike `BaseEntityDefinition` there is no `"INSERT UNIQUE DEFINITION ID"` placeholder and no validation at all, so an empty `Id` passes silently — set it yourself, and treat it as permanent for the same reason you treat an entity `Id` as permanent.

### Filling It In

You create and fill the asset by hand, but two menu items save the fiddly parts:

- **Assets → Fireworks Mania → Generate Preview → Front View Character**, with the character prefab selected in the Project window, renders a front-on 512×512 sprite next to the prefab named `<PrefabName> AutoGeneratedImage.png`. Drop that into **Icon**.
- **GameObject → Fireworks Mania → Parts → Character → Character Camera Position Prefab** adds a `CharacterCameraPosition` as a child of the selected GameObject. That marker is where the first-person camera sits on the character, so parent it under the head bone at eye height. It draws a grey arrow gizmo along its forward axis to show you which way the camera looks.

[^pitch]: Field names, defaults and ranges read from `GameSoundDefinition.cs` in the Mod Tools package; the descriptions are the fields' own `[Tooltip]` text. No code in the package applies the pitch values.
[^soundbus]: The enum values are read from `GameSoundDefinition.cs`. The behaviour of each bus is quoted from the **Sound Bus** field's `[Tooltip]`; the audio system that acts on it ships with the game, not with the Mod Tools.
[^startup]: Quoted from the `[Tooltip]` attributes on `StartupPrefabDefinition._prefabGameObject` and `._sortOrder`. The type has no other members and nothing in the Mod Tools package reads it.
