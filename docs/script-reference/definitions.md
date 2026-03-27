# Definitions

Definitions are [ScriptableObject](https://docs.unity3d.com/Manual/class-ScriptableObject.html) assets that describe items, maps, sounds, and startup logic in Fireworks Mania. They act as data containers — they hold configuration values but contain no runtime behaviour themselves.

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
| **Item Name** | `string` | Display name shown in the inventory UI. |
| **Icon** | `Sprite` | Inventory thumbnail sprite. Recommended size: 256×256 or 512×512 pixels. |
| **Entity Definition Type** | `EntityDefinitionType` | Category reference that determines which tab the item appears in inside the inventory. |

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

## MapDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Map Definition`  
**Type:** `ScriptableObject`

Describes a custom map (level). A `MapDefinition` asset must be present in any mod that adds a new map to the game.

### Inspector Fields

#### General

| Field | Type | Description |
|---|---|---|
| **Map Name** | `string` | Display name of the map shown in the map selection UI. |

#### Scene

| Field | Type | Description |
|---|---|---|
| **Scene Name** | `string` | The exact name of the Unity scene that contains the map. Must match the scene file name precisely. Use a unique name to avoid conflicts with other mods. |

#### Multiplayer Settings

| Field | Type | Description |
|---|---|---|
| **Network Object Prefabs** | `List<GameObject>` | All prefabs in the scene that have a `NetworkObject` component must be registered here for multiplayer to work correctly. Use the **Populate NetworkObjectPrefabs from current open scene** context-menu action to populate this list automatically while the scene is open. |

#### Environment Settings

| Section | Field | Description |
|---|---|---|
| **Time Settings** | Start Time of Day | Initial hour of the day (e.g. `12.5` = 12:30). |
| **Time Settings** | Start Month | Month of the year (1–12). Affects the sun's path across the sky. |
| **Lighting Settings** | Ambient Intensity Curve | Override the ambient light intensity over the day/night cycle. |
| **Lighting Settings** | Ambient Sky Color Gradient | Override the ambient sky colour gradient. |
| **Lighting Settings** | Sun Intensity Curve | Override the sun directional light intensity. |
| **Lighting Settings** | Moon Intensity Curve | Override the moon directional light intensity. |
| **Sky Settings** | Intensity / Intensity Curve | Override sky exposure. |
| **Audio Settings** | Ambient Day Clip / Volume Curve | Audio clip and volume curve for daytime ambience. |
| **Audio Settings** | Ambient Night Clip / Volume Curve | Audio clip and volume curve for nighttime ambience. |
| **Weather Settings** | Start Weather | Initial weather preset for the map. |
| **Game Settings** | Object Catcher Depth | Y-coordinate of the invisible catch plane that respawns the player and destroys fallen objects. |

### Weather Presets

| Value | Name |
|---|---|
| `ClearSky` | Clear sky |
| `Cloudy` | Cloudy |
| `Foggy` | Foggy |
| `Rain` | Rain |
| `Snow` | Snow |
| `DarkCloudy` | Dark cloudy |
| `VeryFoggy` | Very foggy |
| `FoggySnow` | Foggy snow |
| `Storm` | Storm |

---

## GameSoundDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/Game Sound Definition`  
**Type:** `ScriptableObject`

Defines a sound effect used anywhere in the game. Components reference sounds by the **asset name** of the `GameSoundDefinition` rather than by a direct `AudioClip` reference.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Volume** | `float` (0–1) | Base playback volume. |
| **Loop** | `bool` | Whether the sound should loop continuously. |
| **Sound Bus** | `SoundBusGroups` | Routing category (see table below). |
| **Audio Variation Clips** | `AudioClip[]` | One or more audio clips. When multiple clips are provided, the game picks one at random on each play. Adding variations prevents repetitive sound. |
| **Min Distance** | `float` | Distance (metres) within which the sound plays at full volume. |
| **Max Distance** | `float` | Distance (metres) beyond which the sound is inaudible. |
| **Fade In Time** | `float` (0–10 s) | Duration of the volume fade-in when the sound starts. |
| **Fade Out Time** | `float` (0–10 s) | Duration of the volume fade-out when the sound stops. |

### Sound Bus Groups

| Value | Description |
|---|---|
| `Default` (3) | Standard game sounds — most sound effects use this. |
| `Ambient` (0) | Ambient/looping sounds. Forced to 2D playback. |
| `UI` (1) | Interface sounds. Forced to 2D playback. |
| `Explosion` (2) | Loud explosions. Automatically ducks other audio to emphasise impact. |

### Referencing Sounds in Components

Components that play sounds expose a `string` field decorated with `[GameSound]`. This attribute renders a drop-down in the Inspector listing all `GameSoundDefinition` assets in the project. Select the desired sound from the drop-down — the asset name is stored as the string value.

---

## StartupPrefabDefinition

**Namespace:** `FireworksMania.Core.Definitions`  
**Menu:** `Fireworks Mania/Definitions/StartupPrefab Definition`  
**Type:** `ScriptableObject`

Defines a prefab that the game instantiates once in the map immediately after all mods have finished loading. This is the entry point for any custom runtime scripting in your mod.

### Inspector Fields

| Field | Type | Description |
|---|---|---|
| **Prefab Game Object** | `GameObject` | The prefab to instantiate. Attach your startup scripts here. |
| **Sort Order** | `int` | Lower numbers are instantiated first. Useful when the initialisation order of multiple `StartupPrefabDefinition`s matters. |

### Use Cases

- Running custom initialisation logic on map load.
- Registering event listeners.
- Spawning persistent manager objects.

Place your startup logic in `Start()` and cleanup in `OnDestroy()` on a `MonoBehaviour` on the prefab.
