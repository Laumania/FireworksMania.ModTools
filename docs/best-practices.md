# Best Practices

The conventions that keep a mod maintainable, multiplayer-safe and pleasant for players. Read this once you have built your first mod and are starting a second.

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

The `Id` field on every `EntityDefinition` must be **globally unique across all mods**. The safest approach is to keep it identical to the asset filename, which the **Set Id to filename** action does for you.

That action lives on the **Inspector's** context menu — select the asset, then use the **⋮** button (or right-click) on the Inspector header. It is not in the Project window's right-click menu.

!!! note "The Id and the filename are two separate things"
    **Set Id to filename** copies the filename into the Id at the moment you click it. Renaming the asset later does not update the Id. If you rename a definition file, re-run the action — or, if the mod is already published, leave the Id exactly as it was.

!!! danger "Never change the Id after publishing"
    The Id is stored inside players' blueprint save files. Change it and every blueprint referencing that item can no longer resolve it. Nothing in the Mod Tools performs Id aliasing or migration. Set it once and keep it forever.

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
- **Assign every field, even the unchecked ones.** Only three things are actually validated for you: the placeholder `Id`, a missing **Entity Definition Type**, and a missing **Prefab Game Object**. **Item Name** and **Icon** are never checked — an inventory item with a blank name and no icon builds happily.
- **Don't trust the timing of those errors.** Validation on inventory definitions is deferred by one editor tick, so the Console message appears a moment *after* you edited the asset. It can easily look like it came from whatever you clicked next.
- **Test the ID.** Enter Play mode and check the Console.

!!! note "The messages worth recognising"
    `Please update unique id to something unique` means the `Id` is still the default `INSERT UNIQUE DEFINITION ID`. `Missing 'EntityDefinitionType' on '<AssetName>'` and `Missing 'PrefabGameObject' on '<AssetName>'` mean exactly what they say — and you will only ever see one of those two at a time, because the type check runs first and stops there.

---

## Prefab Guidelines

### Required Components

Every firework prefab **must** have:

| Component | Added for you? |
|---|---|
| The appropriate firework behavior (e.g. `CakeBehavior`, `RocketBehavior`) | No — this is the one you add first |
| A `Fuse` component (and its `FuseConnectionPoint` child) | No |
| A `SaveableEntity` component | Yes, but only on prefabs carrying a `BaseFireworkBehavior` |
| An `ErasableBehavior` component | Yes, same condition |
| A `NetworkObject` component (required for multiplayer) | No |
| A `Rigidbody` component | Added by several firework behaviors; verify rather than assume |

!!! warning "Props get nothing added automatically"
    The auto-add lives in the editor-time validation of `BaseFireworkBehavior`. A **prop** prefab has no firework behavior, so nothing is added — you must place `SaveableEntity` yourself and assign its **Entity Definition**, or the prop will not save into blueprints. The symptom is a Console error along the lines of *"…is missing on component… please fix else save/load won't work"*.

    The auto-add is also conditional on a firework prefab: `BaseFireworkBehavior.OnValidate` logs and returns early if the **Entity Definition** or the **Fuse** reference is still empty, so nothing is added until both are assigned. Fill those two in first, then check the components list.

!!! note "Unity won't warn you either"
    There is not a single `[RequireComponent]` attribute anywhere in the Fireworks Mania Core scripts. Every requirement is enforced in code — usually in `OnValidate` (Console messages while authoring) or in `Awake` (Console errors at runtime). Adding a component never drags its dependencies in the way you may expect from other Unity packages, so read the Console after you build a prefab.

### Keep Hierarchy Flat

Deeply nested hierarchies increase overhead. Only add child objects when necessary (particle systems, fuse visuals, mortar tubes, etc.).

### Particle Systems

- Use **GPU Instancing** on particle materials to reduce draw calls. Unity only applies it to particle systems whose **Renderer → Render Mode** is **Mesh**, so it does nothing for ordinary billboard particles.
- You do not need to register your particle systems anywhere. Drop them under the prefab and Unity plays them; the Fireworks Mania components only get involved when you add one of the particle parts (`ParticleSystemObserver` and friends) alongside a system.
- Avoid very high particle counts — aim for the minimum number of particles that still looks good. See [Optimization](optimization.md) for what the particle components actually cost at runtime, and for the per-particle explosion-force trap.

### Prefab Editor Scene

The package ships `PrefabEditorScene`, a neutral grid with scale reference cubes, so you can edit a prefab against something other than Unity's empty grey void. It is **not** wired up for you — point Unity at it once under **Edit → Project Settings → Editor → Prefab Mode → Editing Environment**. [Templates & Sample Assets](guides/templates-and-samples.md) has the full walkthrough.

---

## Audio Guidelines

- Use `GameSoundDefinition` ScriptableObjects to define all sounds — do **not** reference `AudioClip` assets directly in behavior components.
- Use the `[GameSound]` attribute on string fields to get a sound picker in the Inspector.
- Add **variation clips** to `GameSoundDefinition` for sounds that should not be identical every time (e.g. explosions). Even 2–3 variations make a big difference.
- Choose the correct **Sound Bus**:

| Bus | Use For |
|---|---|
| `Default` | Most sounds (ignition, thrust, small effects) |
| `Ambient` | Background/looping ambient sounds — described as forced to 2D |
| `UI` | Interface feedback — described as forced to 2D |
| `Explosion` | Loud explosion sounds — described as ducking other sounds briefly to emphasise how loud it is |

!!! note "Where those descriptions come from"
    The 2D-forcing and ducking behaviour is what the **Sound Bus** field's own tooltip says the game does with each bus.[^soundbus] The code that acts on the value lives in the game, not in the Mod Tools package, so it cannot be observed here — pick the bus that matches your intent and confirm it by ear in-game.

[^soundbus]: The tooltip on `GameSoundDefinition`'s `Sound Bus` field reads, in part: "Default: Used for most sounds, Ambient: Used for ambient sounds and are forced to be 2D, UI: Used for UI sounds and are forced to be 2D, Explosions: Used for loud explosions as sounds of this type will duck other sounds for a short while to emphasize how loud it is."

---

## Multiplayer Considerations

Fireworks Mania supports multiplayer, and anything a player spawns from the inventory is a networked object — it carries a `NetworkObject`, which you add yourself. Right-click the prefab in the **Project** window (or the object in the **Hierarchy**) and pick **Fireworks Mania → Add Network Components** to get it and the matching transform/rigidbody components in one go. So:

- All behavior components that affect gameplay (fuse ignition, launch, explosion) must run correctly on both the **server** and **clients**.
- To remove a whole spawned entity, use the `DestroyOrDespawn()` extension method (`FireworksMania.Core.Utilities`) rather than calling `Destroy()` on it. It despawns through Netcode when the object is spawned and falls back to `Destroy()` when it is not, so the same call works in both cases. Plain `Destroy()` on a non-networked child object — a spent fuse particle system, a one-shot effect — is fine and the shipped components do it in several places.
- **Gameplay state** must originate on the **server**. Read `NetworkVariable` values on clients; write them only from the server.

!!! note "'Server-only' applies to state, not to presentation"
    It is easy to over-apply the rule. The shipped components deliberately do **not** gate everything on the server: sounds, camera shake and explosion physics forces are raised locally on every peer, because each machine already knows enough to produce them and round-tripping through the server would only add latency.

    The distinction to hold onto: *what happened* is decided by the server and replicated. *How it looks and sounds* is produced by each peer independently.

### Custom netcode is supported now

If you write your own C# for a mod, you can use Netcode for GameObjects directly — regular `NetworkVariable<T>` fields and the modern `[Rpc(SendTo.…)]` attribute. This became possible in Mod Tools v2025.8.1, when the mod build pipeline started running NGO's code generation over mod assemblies. Older documentation saying mods cannot use RPCs or NetworkVariables is out of date.

Treat it as supported-but-advanced, and read [Multiplayer & Netcode](scripting/networking.md) before you start.

### Network Object Prefabs in a MapDefinition

!!! warning "This does not currently work"
    `MapDefinition` has a **Network Object Prefabs** list and a **Populate NetworkObjectPrefabs from current open scene** context-menu action. The field's own tooltip opens with *"[This is currently not working - awaiting a fix from Unity and NetCode Team]"*.

    Populate the list if you like — it costs nothing and will be correct when a fix lands — but do not design a map around scene-placed `NetworkObject`s working today.

    This is a separate limitation from the code-generation one above, which *is* fixed. Do not conflate them.

If you do run the action, note that it refuses to do anything unless the currently open scene's name exactly matches the `Scene Name` field on the `MapDefinition`; it logs a warning and stops.

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
