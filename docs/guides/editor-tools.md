# Editor Menu Reference

Every menu item the Mod Tools add to Unity, in one place. Use this when you know a tool exists but cannot remember where it lives — or when a tutorial mentions a menu path that no longer matches what you see.

---

## Where the Menus Live

The Mod Tools add items to four different places, and they are *not* interchangeable. If you are looking in the wrong one, the item simply is not there.

| Menu root | Where you find it | Acts on |
|---|---|---|
| `Mod Tools` | Top-level menu in the Unity menu bar | The whole project / the mod build |
| `GameObject → Fireworks Mania` | The **GameObject** menu, or right-click in the **Hierarchy** | GameObjects in the open scene |
| `Assets → Fireworks Mania` | The **Assets** menu, or right-click in the **Project** window | Prefab assets on disk |
| Component / asset context menus | Right-click a component or asset header in the **Inspector**, or click the **⋮** button in that header | The one component or asset you have selected |

!!! warning "`GameObject → …` and `Assets → …` are different menus"
    Two things appear under both roots: `Generate Preview` (with different entries under each) and `Add Network Components` (identical name, both roots). The `GameObject` version acts on a **scene instance** in the Hierarchy; the `Assets` version acts on the **prefab asset** in the Project window.

---

## The `Mod Tools` Menu

This is the uMod-generated menu that drives the actual mod build.

| Menu path | Shortcut | What it does |
|---|---|---|
| `Mod Tools → Create → New Mod` | — | Opens the Create Mod window, where you name and create a new mod |
| `Mod Tools → Exporter` | — | Opens the Exporter window |
| `Mod Tools → Export Settings` | — | Opens the Export Settings window — mod name, version, author, export directory, build options |
| `Mod Tools → Build Mod` | ++ctrl+shift+b++ | Builds the mod using the active export settings |
| `Mod Tools → Help` | — | Opens the uMod Help window |
| `Mod Tools → About` | — | Opens the uMod About window |
| `Mod Tools → Referencing → Rebuild Reference Cache` | — | Rebuilds the mod tooling's reference cache |

!!! note "Create is a submenu"
    The path is `Mod Tools → Create → New Mod`. Written as "Create New Mod" it reads like a single item, which is why people hunt for it in the wrong place.

!!! tip "Before you start deleting things"
    **Rebuild Reference Cache** is uMod's maintenance action for the reference cache it keeps of your project — worth a try when the build complains about assets that clearly exist. For the general "everything is weird since the upgrade" case, the README names **Reimport All** and a Unity restart; see [Troubleshooting & Build Errors](troubleshooting.md).

!!! info "If Build Mod throws about missing export settings"
    **Build Mod** loads the active export settings first and throws `The export settings are missing from this mod tools package` if there are none. Open **Mod Tools → Export Settings** and configure the mod before building.

### Utilities → Upgrade

Two recovery tools for projects that have been through a Mod Tools upgrade.

| Menu path | What it does |
|---|---|
| `Mod Tools → Utilities → Upgrade → Find all Prefabs with missing scripts` | Scans every `.prefab` under `Assets/` and logs one line per null component: `<path/to/gameobject> has an empty script attached in position: <i>`. Click a log line to ping the GameObject. **Read-only — it reports, it does not fix.** |
| `Mod Tools → Utilities → Upgrade → Remap legacy Core script to new Core` | Rewrites script references in every `.prefab`, `.unity` and `.asset` file under `Assets/`, mapping 40 legacy Fireworks Mania Core scripts onto their current ones. Logs `Replacing missing script '<name>' in '<path>'` per hit. |

The remapping tool is the fix for *"all my components turned into Missing (Mono Script) after upgrading"*. It requires two project settings, and aborts with `Remapping not possible. SerializationMode needs to be set to 'ForceText' and VersionControlMode to 'Visible Meta Files'` if they are wrong:

- **Edit → Project Settings → Editor → Asset Serialization → Mode** = `Force Text`
- **Edit → Project Settings → Editor → Version Control → Mode** = `Visible Meta Files`

!!! danger "Back up before remapping"
    **Remap legacy Core script to new Core** edits asset files directly on disk across your whole `Assets/` folder. There is no undo. Commit to version control or take a copy of the project first.

### Utilities → Multiplayer

| Menu path | What it does |
|---|---|
| `Mod Tools → Utilities → Multiplayer → Revert All NetworkObject Overrides In Current Scene` | Finds active `NetworkObject`s in the open scene and reverts prefab overrides on them. Logs `Found <n> NetworkObjects in current scene`, then `Reverted Overrides on NetworkObject '<name>'` per object it touched. |
| `Mod Tools → Utilities → Multiplayer → Mark all NetworkObjects as dirty in current scene` | Marks every `NetworkObject` in the open scene dirty, including ones on inactive GameObjects, forcing Unity to re-serialize them. Logs `Marked NetworkObject '<name>' as dirty (force update)` per object. |

!!! tip "The 'my prefab keeps getting marked as edited' fix"
    Netcode for GameObjects appears to want `NetworkObject` as the first component on a GameObject, so it gets moved there and the prefab is dirtied just from clicking it. CHANGELOG v2024.4.2 describes the behaviour and the workaround: press **Keep Changes** when prompted and it should not happen again. Use **Revert All NetworkObject Overrides In Current Scene** to clean up a scene that already collected accidental overrides.

---

## `GameObject → Fireworks Mania` (Hierarchy)

Right-click in the Hierarchy, or use the **GameObject** menu. If you right-click an existing GameObject, the new object is created as a child of it.

### Templates → Fireworks

Thirteen ready-made firework setups. Each one is instantiated and then immediately unpacked, so it arrives with **no prefab link** back to the shipped template — it is yours to modify and save as your own prefab.

| Menu path |
|---|
| `GameObject → Fireworks Mania → Templates → Fireworks → Mortar 3 Inch Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Mortar Rack 6 Inch Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Cake Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Firecracker Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Fountains Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → PreloadedTube Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Rocket Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Roman Candle Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Smoke Bomb Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Whistler Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Zipper Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Shell 3 Inch Template` |
| `GameObject → Fireworks Mania → Templates → Fireworks → Shell 6 Inch Template` |

### Templates → Parts

| Menu path |
|---|
| `GameObject → Fireworks Mania → Templates → Parts → Unwrapped Shell Fuse Template` |

Also unpacked on creation, like the firework templates.

!!! note "What each template actually contains"
    [Templates & Sample Assets](templates-and-samples.md) breaks down the components on every template, and warns about the placeholder *Dummy* definitions they ship wired to.

### Parts and Map Pieces

Unlike the templates, these stay **linked prefab instances** of the shipped Mod Tools prefab. Do not unpack them unless you have a reason to.

| Menu path | What it adds |
|---|---|
| `GameObject → Fireworks Mania → Parts → Common → Standard Fuse Prefab` | A ready-made fuse (`Fuse` + `FuseConnectionPoint`) |
| `GameObject → Fireworks Mania → Parts → Mortar → Mortar Top Prefab` | The `MortarTubeTop` piece |
| `GameObject → Fireworks Mania → Parts → Mortar → Mortar Bottom Prefab` | The `MortarTubeBottom` piece |
| `GameObject → Fireworks Mania → Parts → Mortar → Unwrapped Shell Fuse Pivot Position Prefab` | The `UnwrappedShellFusePivotPosition` pivot |
| `GameObject → Fireworks Mania → Parts → Character → Character Camera Position Prefab` | The `CharacterCameraPosition` marker for character mods |
| `GameObject → Fireworks Mania → Maps → Player Spawn Location Prefab` | A `PlayerSpawnLocation` for a custom map |

!!! note "`Maps` is a sibling of `Parts`, not a child"
    The spawn location lives under **Maps**, not under **Parts** — the full path is `GameObject → Fireworks Mania → Maps → Player Spawn Location Prefab`. See [Custom Maps](custom-maps.md).

### Generate Preview — from the Scene View

One entry, which works on a **scene instance** and uses your current Scene View camera as the framing.

| Menu path | Framing |
|---|---|
| `GameObject → Fireworks Mania → Generate Preview → Perspective → Current Veiw In Scene` | Perspective, from wherever the Scene View camera currently sits relative to the selected object |

!!! note "The typo is in the menu item itself"
    It really does read *"Current Veiw In Scene"*. Place your prefab instance in a scene, frame it in the Scene View, then right-click it in the Hierarchy and run this. The PNG is written next to the **prefab asset** the instance came from, so the selected object has to be a prefab instance.

### Networking

| Menu path | What it does |
|---|---|
| `GameObject → Fireworks Mania → Add Network Components` | For each selected GameObject, adds `NetworkObject` and `ClientNetworkTransform` if missing, plus `ClientNetworkRigidbody` **only if the GameObject already has a `Rigidbody`**. Logs `Added network components to '<name>'`. Does not recurse into children. |

---

## `Assets → Fireworks Mania` (Project Window)

Right-click in the Project window with the asset selected, or use the **Assets** menu.

### Generate Preview

Five entries that work on a **prefab asset**. Each writes a 512 × 512 PNG with a transparent background next to the prefab, named `<PrefabName> AutoGeneratedImage.png` and overwriting any existing file of that name. Each one logs `Saved generated preview '<name>' at path: <path>` and pings the new sprite in the Project window.

| Menu path | Camera |
|---|---|
| `Assets → Fireworks Mania → Generate Preview → Orthographic → Front View` | Orthographic, from the front |
| `Assets → Fireworks Mania → Generate Preview → Orthographic → Back View` | Orthographic, from behind |
| `Assets → Fireworks Mania → Generate Preview → Perspective → Front View` | Perspective, from the front |
| `Assets → Fireworks Mania → Generate Preview → Perspective → Back View` | Perspective, from behind |
| `Assets → Fireworks Mania → Generate Preview → Front View Character` | Perspective, framed head-on for character prefabs |

!!! note "`Front View Character` is not under Orthographic or Perspective"
    It sits directly under `Generate Preview`, alongside the two submenus — which is why people miss it.

!!! warning "It lights the shot from your open scene"
    Generating a preview temporarily instantiates the package's `PreviewLightingPrefab` into whatever scene is currently open, and destroys it again afterwards. It does not swap to an empty scene, so an open scene with heavy fog or an unusual skybox can still tint the result. If a preview comes out wrong, try again from an empty scene. If the capture fails you get `Failed to Produce Texture` or `Texture Could not be Read` in the Console instead of a file.

Full workflow, including what to do with the generated sprite, is in [Icons & Sounds](icons-and-sounds.md).

### Networking

| Menu path | What it does |
|---|---|
| `Assets → Fireworks Mania → Add Network Components` | The same action as the `GameObject` version above, applied to the selected prefab asset |

---

## `Assets → Create → Fireworks Mania`

The definitions you can author yourself. Right-click in the Project window → **Create → Fireworks Mania → Definitions → …**

| Menu path | Type | Default filename |
|---|---|---|
| `Create → Fireworks Mania → Definitions → Firework Entity Definition` | `FireworkEntityDefinition` | `New Firework Entity Definition` |
| `Create → Fireworks Mania → Definitions → Prop Entity Definition` | `PropEntityDefinition` | `New Prop Entity Definition` |
| `Create → Fireworks Mania → Definitions → Map Definition` | `MapDefinition` | `New Map Definition` |
| `Create → Fireworks Mania → Definitions → Game Sound Definition` | `GameSoundDefinition` | `New Game Sound` |
| `Create → Fireworks Mania → Definitions → Character Definition` | `CharacterDefinition` | `New Character Definition` |
| `Create → Fireworks Mania → Definitions → StartupPrefab Definition` | `StartupPrefabDefinition` | `New StartupPrefab Definition` |

!!! info "There is no `Internal` submenu in your project"
    `EntityDefinitionType`, `EntityDiameterDefinition` and `SoundCollection` each declare a Create-menu entry under `Fireworks Mania/Definitions/Internal/`, but the attribute sits behind `#if FIREWORKSMANIA_SHOW_INTERNAL_MODTOOLS`, which is not defined in a normal mod project. There is no Create-menu path to author your own inventory categories or shell diameters — reference the ones that ship with the package. See [Definitions](../script-reference/definitions.md).

---

## Inspector Context Menus

These are **not** Project-window right-clicks. Select the asset, then in the **Inspector** either right-click the header of the script/asset or click the **⋮** button on that header.

| Context menu item | Appears on | What it does |
|---|---|---|
| `Set Id to filename` | `FireworkEntityDefinition`, `PropEntityDefinition` (via `BaseEntityDefinition`) | Sets the `Id` field to the asset's filename |
| `Set Id to filename` | `CharacterDefinition` | Same |
| `Generate new unique id (based on file name)` | `EntityDefinitionType` | Despite the name, it copies the filename into the otherwise read-only `Id` field |
| `Generate new unique id (based on file name)` | `EntityDiameterDefinition` | Same |
| `Populate NetworkObjectPrefabs from current open scene` | `MapDefinition` | Collects the prefabs behind the scene's `NetworkObject`s into the map's network prefab list |

!!! tip "Set Id to filename is how you clear the 'unique id' error"
    A new definition starts with its `Id` set to the literal text `INSERT UNIQUE DEFINITION ID`, and logs `Please update unique id to something unique` until you change it. Name the asset properly first, then run **Set Id to filename** — and never touch it again after you publish.

!!! danger "Do not regenerate ids on the package's own assets"
    `Generate new unique id (based on file name)` shows up when you select one of the `EntityDefinitionType` or `EntityDiameterDefinition` assets that ship inside the Mod Tools package. Their own tooltip reads *"IMPORTANT: Do not change this id after it have initially been set to avoid breaking references"* — that is why the field is `[ReadOnly]` in the Inspector. Leave those assets alone; the context menu exists for whoever authored them, not for you.

!!! warning "Populating network prefabs will not make map objects sync"
    The `MapDefinition` field this action fills carries an in-source tooltip that begins `[This is currently not working - awaiting a fix from Unity and NetCode Team]`.[^netprefabs] The menu action itself works fine — the field it fills is the part that does not. Running it is harmless; do not expect moveable objects placed in a custom map to sync in multiplayer. This is unrelated to mod script networking, which *is* supported — see [Multiplayer & Netcode](../scripting/networking.md).

    Two things can also go wrong when you run it: it warns `Unable to populate networkobject prefabs from the current scene '<open scene>', as it is not matching the scene '<Scene Name>' on '<definition asset>'` if the open scene's name does not match the **Scene Name** field on the `MapDefinition`, and `Unable to find Prefab for <object>` for any scene `NetworkObject` that is not a prefab instance.

[^netprefabs]: This claim comes from the tooltip on the `_networkObjectPrefabs` field in `MapDefinition.cs` and from the README's known-limitations section. Nothing in the Mod Tools package proves the current state either way, since the loading code lives in the game rather than in this package.

---

## Paths From Older Tutorials

If you are following a video, a forum post or an old CHANGELOG entry, these are the ones that will not match what you see:

| You were told | It is actually |
|---|---|
| `Generate View` / `Generate Icon(s)` — the CHANGELOG v2025.4.1 entry calls it "Generate View" in its own prose | `Generate Preview` — under `Assets → Fireworks Mania` or `GameObject → Fireworks Mania` |
| `Mod Tools → Create New Mod` | `Mod Tools → Create → New Mod` — **Create** is a submenu |
| A menu item that opens the prefab editing scene | There is none. You wire `PrefabEditorScene` up by hand in Project Settings — see [Templates & Sample Assets](templates-and-samples.md) |

---

## Where to Next

- Something is broken and you want the fix → [Troubleshooting & Build Errors](troubleshooting.md)
- You want to build a firework using these templates → [Templates & Sample Assets](templates-and-samples.md)
- You want to know what every component does → [Script Reference](../script-reference/index.md)
