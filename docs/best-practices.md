# Best Practices

Following these guidelines will keep your mods maintainable, compatible, and a good experience for players.

---

## Naming Conventions

### Mod Folder and Files

Always prefix **everything** with your unique nickname or author tag. This prevents name collisions with other mods and with game updates.

| Item | Recommended Pattern | Example |
|---|---|---|
| Unity project | `YourNick.FireworksMania.Mods` | `Laumania.FireworksMania.Mods` |
| Mod folder | `YourNick_ModName` | `Laumania_RocketPack` |
| EntityDefinition | `YourNick_Type_ItemName` | `Laumania_Cake_GoldSparkler` |
| Prefab | `YourNick_Type_ItemName` | `Laumania_Cake_GoldSparkler` |
| Icon sprite | `YourNick_Type_ItemName_Icon` | `Laumania_Cake_GoldSparkler_Icon` |

### Entity Definition IDs

The `Id` field on every `EntityDefinition` must be **globally unique across all mods**. The safest approach is to use the filename as the ID (use the **Set Id to filename** context menu option on the ScriptableObject).

!!! danger "Never rename a definition after publishing"
    The EntityDefinitionId is stored inside players' blueprint save files. If you rename or change the ID, those blueprints will fail to load that item. Set the name once and keep it forever.

---

## Folder Structure

Keep all of a mod's assets inside a single root folder. This makes exporting, updating, and deleting a mod straightforward.

```
Assets/
└── Mods/
    └── YourNick_ModName/
        ├── Definitions/       ← ScriptableObject asset files
        ├── Icons/             ← Inventory icon sprites (256×256 or 512×512 recommended)
        ├── Models/            ← Imported .fbx files and their materials
        ├── Prefabs/           ← Assembled Unity prefabs
        └── Sounds/            ← (Optional) Custom audio clips
```

You can extend this structure as needed (e.g. `VFX/`, `Animations/`), but keeping it consistent makes collaboration and version control easier.

---

## EntityDefinition Guidelines

- **One prefab per definition.** Do not share a single prefab across multiple definitions.
- **Assign all required fields.** The Inspector will show errors if `Id`, `Prefab Game Object`, `Item Name`, `Icon`, or `Entity Definition Type` are missing.
- **Test the ID.** Enter Play mode and check the Console. Missing or duplicate IDs will be reported as errors.

---

## Prefab Guidelines

### Required Components

Every firework prefab **must** have:

- The appropriate firework behavior (e.g. `CakeBehavior`, `RocketBehavior`)
- A `Fuse` component (and `FuseConnectionPoint` child)
- A `SaveableEntity` component (added automatically if missing, but best to add it explicitly)
- An `ErasableBehavior` component (added automatically if missing)
- A `NetworkObject` component (required for multiplayer)
- A `Rigidbody` component

### Keep Hierarchy Flat

Deeply nested hierarchies increase overhead. Only add child objects when necessary (particle systems, fuse visuals, mortar tubes, etc.).

### Particle Systems

- Use **GPU Instancing** on particle materials to reduce draw calls.
- Stop particle systems when not in use (the framework handles this automatically for most behaviors).
- Avoid very high particle counts — aim for the minimum number of particles that still looks good.
- Use the **Prefab Editor Scene** (`FireworksMania/Scenes/Editor/PrefabEditorScene`) to preview your firework in isolation.

---

## Audio Guidelines

- Use `GameSoundDefinition` ScriptableObjects to define all sounds — do **not** reference `AudioClip` assets directly in behavior components.
- Use the `[GameSound]` attribute on string fields to get a drop-down sound picker in the Inspector.
- Add **variation clips** to `GameSoundDefinition` for sounds that should not be identical every time (e.g. explosions). Even 2–3 variations make a big difference.
- Choose the correct **Sound Bus**:

| Bus | Use For |
|---|---|
| `Default` | Most sounds (ignition, thrust, small effects) |
| `Ambient` | Background/looping ambient sounds (forced 2D) |
| `UI` | Interface feedback (forced 2D) |
| `Explosion` | Loud explosion sounds — this bus ducks other audio automatically |

---

## Multiplayer Considerations

Fireworks Mania supports multiplayer. All spawned objects are networked, so:

- All behavior components that affect gameplay (fuse ignition, launch, explosion) must run correctly on both the **server** and **clients**.
- Do not use Unity's built-in `Destroy()` directly — use the `DestroyOrDespawn()` extension method provided by the framework to ensure proper network cleanup.
- State changes must originate on the **server**. Read `NetworkVariable` values on clients; write them only from the server.
- If you create a `MapDefinition` with `NetworkObject` prefabs in the scene, use the **Populate NetworkObjectPrefabs from current open scene** context menu action on the `MapDefinition` to register them correctly.

---

## Version Control

- Use **Git** to track your mod project.
- Add the following to your `.gitignore`:

```gitignore
# Unity generated
[Ll]ibrary/
[Tt]emp/
[Oo]bj/
[Bb]uild/
[Bb]uilds/
[Ll]ogs/
[Mm]emoryCaptures/
*.pidb.meta
*.pdb.meta
*.mdb.meta
sysinfo.txt
MemoryCaptures/

# Asset meta data should only be ignored when the corresponding asset is also ignored
!Assets/**/*.meta

# Uncomment this line if you wish to ignore the asset store tools plugin
# /[Aa]ssets/AssetStoreTools*

# Autogenerated Jetbrains Rider plugin
[Aa]ssets/[Pp]lugins/Editor/JetBrains*
```

- **Do commit** all `.meta` files alongside their corresponding assets.
- **Do not commit** the built `.mod` output file — it is a build artifact.

---

## Testing Checklist

Before publishing a new version of your mod, verify:

- [ ] No errors in the Console in Edit mode
- [ ] No errors in the Console in Play mode
- [ ] All `EntityDefinition` IDs are set and unique
- [ ] Mod builds without errors (`Mod Tools → Build Mod`)
- [ ] Mod loads correctly in-game (restart map)
- [ ] Firework ignites, launches, and explodes without errors
- [ ] Mod works in multiplayer (host + join test)
- [ ] File size is reasonable (check the exported `.mod` file size)
