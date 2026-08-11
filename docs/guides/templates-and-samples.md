# Templates & Sample Assets

The Mod Tools ship thirteen ready-made firework templates, covering the common firework types. Starting from a template is the fastest way to get a working firework — you swap in your own model, point it at your own definition, and you are done.

---

## What a Template Is

A template is a fully wired firework prefab that lives inside the Mod Tools package. You create one from the **Hierarchy**:

1. Right-click in the Hierarchy (or use the **GameObject** menu).
2. Choose **Fireworks Mania → Templates → Fireworks → …**

The template is instantiated into the open scene — or into the prefab you currently have open in Prefab Mode — and immediately **unpacked**.

!!! info "Templates are auto-unpacked — this is deliberate"
    Right after the template is created, the Mod Tools unpack the outermost root, so the new GameObject has **no prefab link back to the shipped template**. It is yours: rename it, delete components, restructure children, and drag it into your own mod folder to make it your own prefab.

    Only the **outermost root** is unpacked. Nested prefab instances inside the template — the Dummy model, `FuseStandardPrefab`, the mortar tube parts and so on — stay linked to the package prefabs, which is why they still show the blue prefab icon in the Hierarchy.

A few practical details:

- The new object is parented to whatever you right-clicked in the Hierarchy, and lands at that parent's origin (or world origin if you right-clicked empty space). It is **not** placed at the Scene view pivot.
- It is created with Undo support and selected for you.
- It is still named `<Type>_Template_Prefab`. **Rename it** before you save it as your prefab.

---

## The Template Menu

All entries live under `GameObject > Fireworks Mania > Templates`.

### Fireworks

| Menu item | Main component | Notes |
|---|---|---|
| **Mortar 3 Inch Template** | `MortarBehavior` | One `MortarTube`; diameter set to `Diameter_3_Inch` |
| **Mortar Rack 6 Inch Template** | `MortarBehavior` | Three tubes (`MortarTube01`, `(1)`, `(2)`); diameter `Diameter_6_Inch` |
| **Cake Template** | `CakeBehavior` | `ParticleSystemExplosion`, `ParticleSystemSound`, `ParticleSystemObserver` |
| **Firecracker Template** | `FirecrackerBehavior` | Also carries `ExplosionBehavior` |
| **Fountains Template** | `FountainBehavior` | Creates `Fountain_Template_Prefab` (menu says "Fountains", asset is singular) |
| **PreloadedTube Template** | `PreloadedTubeBehavior` | Same particle trio as the Cake, plus the launch muzzle and smoke-trail effects |
| **Rocket Template** | `RocketBehavior` | Also carries `Thruster` and `ExplosionBehavior` |
| **Roman Candle Template** | `RomanCandleBehavior` | Same particle trio as the Cake |
| **Smoke Bomb Template** | `SmokeBombBehavior` | |
| **Whistler Template** | `WhistlerBehavior` | Also carries `Thruster` and `ExplosionBehavior` |
| **Zipper Template** | `ZipperBehavior` | Two `ParticleSystemObserver`s |
| **Shell 3 Inch Template** | `ShellBehavior` | `ParticleSystemShellSound`; diameter `Diameter_3_Inch` |
| **Shell 6 Inch Template** | `ShellBehavior` | Same components as Shell 3 Inch; diameter `Diameter_6_Inch` |

### Parts

| Menu item | Main component |
|---|---|
| **Unwrapped Shell Fuse Template** | `UnwrappedShellFuse` (with an `IgnitePosition` child) |

This one is a part, not an inventory item — it has no definition and no networking components.

---

## What Every Firework Template Carries

Beyond the type-specific behaviour, every firework template comes with:

| Component | Why it is there |
|---|---|
| `SaveableEntity` | Lets the firework be stored in and restored from blueprints |
| `ErasableBehavior` | Lets the player remove it with the eraser tool |
| `NetworkObject` | Netcode identity — required for multiplayer |
| `NetworkRigidbody` | Netcode physics sync |
| `ClientNetworkTransform` | Fireworks Mania's client-authoritative transform sync |

Most templates additionally carry `Fuse` and `ExplosionPhysicsForceEffect`. The two mortar templates do not — they hold *other* fireworks rather than exploding themselves.

!!! warning "Do not strip the networking components"
    `NetworkObject`, `NetworkRigidbody` and `ClientNetworkTransform` are pre-wired on every firework template. Removing them will break your firework in multiplayer, even if it looks fine in singleplayer. If you ever build a firework from scratch, **GameObject → Fireworks Mania → Add Network Components** adds the equivalents for you.

!!! note "Templates use `NetworkRigidbody`, the menu adds `ClientNetworkRigidbody`"
    The shipped templates reference Netcode's own `NetworkRigidbody`, while **Add Network Components** adds Fireworks Mania's `ClientNetworkRigidbody` (and only when the object already has a `Rigidbody`). Both ship in the Mod Tools; leave whichever one your object already has alone rather than swapping it.

See [Behaviors](../script-reference/behaviors.md) and [Firework Parts](../script-reference/firework-parts.md) for what each of these components actually does.

---

## Swapping In Your Own Model

Eleven of the thirteen firework templates have a child GameObject literally named **`Model`**. That is where the grey-box Dummy mesh sits, and it is the one thing you are expected to replace.

1. Expand the created object in the Hierarchy and select the **`Model`** child.
2. Delete it and drag your own `.fbx` in as a child in its place, keeping the same name and local position so the rest of the wiring stays readable.
3. Check the colliders still match your new mesh — the Dummy meshes are simple grey-box placeholders, and your model probably is not.
4. Check the particle systems and effect transforms still sit where you want them (nozzle, muzzle, fuse tip).

The other two have no `Model` child and are laid out differently:

| Template | Where the visuals live |
|---|---|
| Mortar 3 Inch / Mortar Rack 6 Inch | `MortarTube` (3 inch) or `MortarTube01`, `MortarTube01 (1)`, `MortarTube01 (2)` (6 inch), plus a `Colliders/Collider` child |

!!! tip "Keep the fuse"
    Eleven of the thirteen firework templates nest the shipped `FuseStandardPrefab` — everything except the two mortars. It stays a linked prefab instance after the template is unpacked, so you get the standard `Fuse` and `FuseConnectionPoint` setup for free. Move and rotate it to fit your model rather than deleting it.

---

## Replace the Dummy Definition — Always

Every firework template ships pre-wired to one of the sample **Dummy** `FireworkEntityDefinition` assets that live inside the Mod Tools package.

!!! danger "Shipping a mod that still points at a Dummy definition will collide with every other mod that did the same"
    The definition `Id` is what blueprints save. If two mods ship entities claiming `Cake_DummyCake`, they fight over the same id and players' blueprints break. Your item would also show up in the inventory as "Dummy Cake" with the sample's grey-box icon, since the name, icon and category all come from the definition.

    Create your own definition, give it a globally unique `Id` (prefix it with your nick), and re-point the firework behaviour on the prefab root at it before you build.

The mapping, so you know what you are replacing and which category the samples use:

| Template | Dummy definition it ships with | Entity Definition Type |
|---|---|---|
| Cake | `Cake_DummyCake` | `Fireworks_Cake` |
| Firecracker | `Firecracker_DummyFirecracker` | `Fireworks_Firecracker` |
| Fountains | `Fountain_DummyFountain` | `Fireworks_Novelty` |
| Mortar 3 Inch | `Mortar_3inch_DummyMortar` | `Fireworks_Mortar` |
| Mortar Rack 6 Inch | `Mortar_6inch_DummyMortarRack` | `Fireworks_Mortar` |
| PreloadedTube | `PreloadedTube_DummyPreloadedTube` | `Fireworks_Tube` |
| Rocket | `Rocket_DummyRocket` | `Fireworks_Rocket` |
| Roman Candle | `RomanCandle_DummyRomanCandle` | `Fireworks_Novelty` |
| Shell 3 Inch | `Shell_3inch_DummyShell` | `Fireworks_Shell` |
| Shell 6 Inch | `Shell_6inch_DummyShell` | `Fireworks_Shell` |
| Smoke Bomb | `SmokeBomb_DummySmokeBomb` | `Fireworks_Smoke` |
| Whistler | `Whistler_DummyWhistler` | `Fireworks_Novelty` |
| Zipper | `Zipper_DummyZipper` | `Fireworks_Novelty` |

The mod build validates this both ways: your definition must reference your prefab **and** the behaviour on your prefab root must reference that same definition, otherwise the build fails. See [Troubleshooting & Build Errors](troubleshooting.md) for the exact messages, and [Definitions](../script-reference/definitions.md) for every field on the definition itself.

---

## Ready-Made Parts

Separate from templates, `GameObject > Fireworks Mania` also creates individual parts. Unlike templates these are **not** unpacked — they stay connected prefab instances of the shipped package prefab, so your changes show up as prefab overrides and Mod Tools updates flow straight into them.

| Menu item | What you get |
|---|---|
| **Parts → Common → Standard Fuse Prefab** | `Fuse` + `FuseConnectionPoint` — the standard burnable fuse |
| **Parts → Mortar → Mortar Top Prefab** | `MortarTubeTop` |
| **Parts → Mortar → Mortar Bottom Prefab** | `MortarTubeBottom` |
| **Parts → Mortar → Unwrapped Shell Fuse Pivot Position Prefab** | `UnwrappedShellFusePivotPosition` |
| **Parts → Character → Character Camera Position Prefab** | `CharacterCameraPosition` — first-person camera position on a character |
| **Maps → Player Spawn Location Prefab** | `PlayerSpawnLocation` — see [Custom Maps](custom-maps.md) |

Note that **Maps** is a sibling of **Parts**, not a child of it.

---

## The ModSamples Folder

The Dummy assets behind the templates live in the package at:

```
FireworksMania/Resources/ModSamples/
├── Definitions/   13 Dummy FireworkEntityDefinition assets
├── Icons/         12 icon PNGs
├── Models/        13 grey-box Dummy .fbx models
└── Prefabs/       14 template prefabs (13 fireworks + 1 part)
```

They are worth opening to see how a finished firework is put together — and they are also the reason "Dummy" keeps showing up in your Hierarchy.

!!! warning "Do not edit or ship the ModSamples assets"
    They live inside the Mod Tools package, not in your mod folder. Editing them changes them for every mod in the project, and package updates will overwrite your changes. Copy what you need into your own `Assets/Mods/YourNick_ModName/` folder instead.

!!! note "The samples are placeholders, not a style guide"
    There are a few known quirks in them: the Firecracker sample points at the Fountain icon, the 6 inch shell reuses the 3 inch icon, and a couple of the definition assets were saved by an older version of the Mod Tools and still contain long-removed fields. They load fine — but do not treat them as the canonical example of a tidy mod.

---

## The PrefabEditorScene

By default, Unity opens prefabs in a featureless grey void, which makes it very hard to judge the size of a firework. The Mod Tools ship a purpose-built editing environment instead:

```
FireworksMania/Scenes/Editor/PrefabEditorScene.unity
```

It contains a grid floor and reference cubes labelled **1 m**, **2 m**, **5 m**, **10 m** and **50 m**, so you can see at a glance whether your cake is the size of a shoebox or the size of a car.

!!! info "It is not wired up for you"
    A fresh Mod Tools project does **not** have this scene set as the prefab editing environment. You have to point Unity at it yourself, once per project.

### Setting it up

1. Go to **Edit → Project Settings → Editor → Prefab Mode → Editing Environment → Regular Environment**.
2. Drag `PrefabEditorScene` into the field, or click the small circle selector and search for it.

From then on, **every** prefab in the project opens inside this scene in Prefab Mode — not just Fireworks Mania ones.

The first time the scene opens you may get the TextMesh Pro "import essentials" popup. Accept it; the reference cube labels need it.

### Fixing "my mod looks too bright"

The other complaint that shows up the moment you open a prefab is that everything looks washed out: a new Unity project renders in **Gamma** color space and the game renders in **Linear**. `FireworksMania/Scenes/Editor/Readme.txt`, next to the scene, says the same thing. [Getting Started](../getting-started.md) has the fix — do it early, because changing color space after you have hand-tuned a lot of materials means re-tuning them.

---

## Real Examples Worth Opening

Beyond the templates, the package ships prefabs that are not in a `Resources` folder and have no menu item — drag them straight from the Project window.

| Folder | What is in it |
|---|---|
| `FireworksMania/Prefabs/Fireworks/Effects/` | `FX_ExplosionSmoke_Prefab`, `FX_LaunchMuzzleAndSmoke_Prefab`, `FX_ParticlesLight_Prefab`, `FX_SmokeTrail_Prefab` — pure particle/light prefabs with no Fireworks Mania scripts on them, so they drop into anything |
| `FireworksMania/Prefabs/Fireworks/Parts/` | `FuseConnectionPointPrefab`, `Thruster_Rocket_Default`, `Thruster_Whistler_Default` |
| `FireworksMania/Prefabs/Fireworks/Shells/` | `UnwrappedShellFuse_3inch_01_Prefab` |
| `FireworksMania/Prefabs/Fireworks/Shells/Effects/` | `Shell_2inch_Launch_Effect_Prefab` through `Shell_6inch_Launch_Effect_Prefab` — launch flashes, no scripts |

Opening these is the quickest way to learn how the shipped fireworks tune their particle systems.

!!! tip "Can't see package assets in an object picker?"
    Unity's object picker can hide assets that live inside a package. If the Fireworks Mania prefabs and definitions do not show up when you click a field's circle selector, switch the picker to include package assets using the toggles in the picker window's own toolbar.

---

## From Template to Finished Firework

- [ ] Create the template from the Hierarchy menu
- [ ] Rename it from `*_Template_Prefab` to your own name
- [ ] Replace the mesh under the `Model` child (or the tube/rack children)
- [ ] Fix up colliders, particle positions and the fuse position
- [ ] Drag it into your mod folder to make it your own prefab
- [ ] Create your own `FireworkEntityDefinition` with a unique `Id` and assign the prefab
- [ ] Assign that definition on the firework behaviour on the prefab root
- [ ] Generate an icon and pick your sounds — see [Icons & Sounds](icons-and-sounds.md)
- [ ] Build with **Mod Tools → Build Mod** (++ctrl+shift+b++) and test in game

New to all this? [Getting Started](../getting-started.md) walks through the surrounding setup — mod folder, export settings and the build itself.
