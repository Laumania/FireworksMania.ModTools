# Publishing Your Mod

Getting your mod from your Unity project into other people's games, and the one change you cannot walk back once it is published.

---

## Pre-flight checklist

Run through this before every upload, not just the first one.

- [ ] **The build is clean.** **Mod Tools → Build Mod** (++ctrl+shift+b++) completes with no errors. Read any warnings rather than scrolling past them — a `Camera` or a Directional Light in a map scene only warns, and both are very likely to break the map in game.
- [ ] **Optimize for is set to File Size.** **Mod Tools → Export Settings → Build** tab. Every player who downloads your mod pays for this setting.
- [ ] **Mod Information is filled in.** **Mod Name**, **Mod Version** and **Mod Author** under **Mod Tools → Export Settings → Mod Information**.
- [ ] **Every definition Id is final.** This is the one you cannot walk back. See the danger note below.
- [ ] **Every definition has a real name and icon.** `Untitled Entity Definition` is the default **Item Name** on every new definition, and an empty **Icon** field means a blank inventory tile.
- [ ] **You have actually played it.** Build, start the game, load a map, spawn every item in the mod, ignite it, erase it. Then save a blueprint containing it, reload the map, and load that blueprint back.
- [ ] **You have tested it in multiplayer** — if your mod is meant to be used there. Both the host and the joining player need the mod installed; a player without it simply will not see your creations.
- [ ] **The file size is sane.** Check the built file in your **Mod Export Directory**. If a single firework is tens of megabytes, something is wrong — see [Optimization](../optimization.md).

---

## Never change a published Id

!!! danger "A changed Id orphans that entity in every blueprint that used it"
    The `Id` on an `EntityDefinition` is what gets written into players' blueprints. When a blueprint is saved, each placed entity is stored as an `EntityDefinitionId` string; when it is loaded, the game looks the entity up by that exact string.

    **There is no aliasing table, no migration hook and no "old id" field anywhere in the Mod Tools source.** A string that no longer resolves is simply an entity that no longer loads. Every blueprint any player ever built with your firework in it loses that firework, and short of hand-editing the blueprint's JSON there is no way back.

    The field's own tooltip says the same thing: *"This is used to save/load this entity in Blueprints, so avoid changing this Id once it have been set, as it will break users Blueprints."*

    Set the Id once, before you publish. Then leave it alone forever.

### The corollary that catches people out

**Renaming the asset file does not change the Id.** They are two separate pieces of data that happen to start out matching, because the **Set Id to filename** context-menu action copies one into the other — once, when you run it.

So after you publish:

- Renaming the `.asset` file is harmless. The Id is untouched and blueprints keep working. You have just made the file name and the Id disagree, which is confusing but not damaging.
- Running **Set Id to filename** again on a renamed asset is the damaging move. That is the moment the Id actually changes, and it usually happens by reflex, months later, while tidying up.

Pick your naming convention before the first upload — something like `YourNick_Type_ItemName` — run **Set Id to filename** once, and never touch that context-menu item on a published definition again.

### The same trap in C#

If your mod has a custom component that saves its own data into blueprints, the key that data is stored under is that component's `ISaveableComponent.SaveableComponentTypeId`. Every component shipped in the Mod Tools implements it as:

```csharp
public string SaveableComponentTypeId => this.GetType().Name;
```

Copy that line — as most people do — and your C# **class name** becomes the storage key. Rename the class afterwards and the saved data for that component stops being found on load, silently: `SaveableEntity.RestoreState` does a `TryGetValue` on the key and simply moves on when it misses.

You can opt out of the trap by returning a hard-coded string instead of the type name. Do that and the class can be renamed freely; do not do it and a published class name has to be treated exactly like a published Id. More on how this works in [Saving & Loading (Blueprints)](../scripting/persistence.md).

### What *is* safe to change after publishing

| Change | Safe? | Why |
|---|---|---|
| **Item Name** | Yes | Display only. Blueprints store the Id, never the name. |
| **Icon** | Yes | Display only. |
| Prefab contents — mesh, particles, sounds, tuning | Yes | The blueprint records the Id; the game spawns whatever prefab the definition points at *now*. This is how you ship an improved version of an existing firework. |
| **Entity Definition Type** | Yes | Only changes which inventory category the item shows up in. Not stored in blueprints — though players will have to go looking in a new tab. |
| The definition's **asset filename** | Yes, but pointless | The Id does not follow it. See above. |
| **Id** | **No** | Breaks every existing blueprint using it. |
| A custom `ISaveableComponent` **class name** | **No** | Breaks that component's saved data — unless its `SaveableComponentTypeId` returns a hard-coded string rather than the type name. |
| **Removing a definition** from the mod | **No** | Same effect as changing the Id — the entity no longer resolves. |

---

## Upload to mod.io

Fireworks Mania mods are distributed through mod.io:

**<https://mod.io/g/fireworksmania>**

There is no upload tooling inside the Mod Tools — the Editor builds the mod file, and everything after that happens on the mod.io website. Upload the file the build produced in your **Mod Export Directory** (the same file you have been testing with locally).

!!! tip "Local testing and publishing use the same file"
    If you followed [Getting Started](../getting-started.md), your **Mod Export Directory** already points at the game's local mods folder:

    ```
    %userprofile%\appdata\locallow\Laumania ApS\Fireworks mania\Mods
    ```

    That is fine, and it is what makes the build-test loop fast. The file sitting there after a successful build is the one you upload.

!!! note "Local mods and workshop mods are not the same thing"
    A mod in your local `Mods` folder works for **you**. For anyone else to use it — including someone joining your multiplayer game — it has to be on mod.io, and they have to have installed it from there.

---

## Versioning discipline

The **Mod Version** field in **Export Settings** is yours to manage; nothing enforces a format. Some habits that pay off:

| Habit | Why |
|---|---|
| Bump the version on every upload | Gives you and your players a shared vocabulary for "which one is broken". |
| Use `major.minor.patch` — `1.0.0`, `1.1.0`, `1.1.1` | Conventional, and it sorts sensibly. |
| Keep the **Mod Name** stable | It is how players and bug reporters refer to your mod, and for map mods loaded from the local `Mods` folder it is the name the game falls back to in the map list. Change the display copy on mod.io instead. |
| Write down what changed | Even a two-line note in the mod.io description saves you the "did I already fix that?" conversation. |
| Never reuse a version number for different content | A bug report against `1.2.0` has to mean one specific build, or it means nothing. |

### Re-publishing after a Mod Tools upgrade

When you update the Mod Tools package, **restart Unity** — the CHANGELOG repeats that instruction on every entry that moves to a new Unity version — then rebuild and re-upload. Several past releases changed something that stopped already-published mods working until they were rebuilt; the CHANGELOG calls those out per version, so read the entries you skipped rather than assuming an old build still loads.

Back up your project (or commit it) before upgrading, every time. That advice sits at the top of the CHANGELOG for a reason.

---

## Where to go next

| I want to… | Page |
|---|---|
| Shrink the mod before uploading | [Optimization](../optimization.md) |
| Fix a build error blocking the upload | [Troubleshooting & Build Errors](troubleshooting.md) |
| Understand what a blueprint actually stores | [Saving & Loading (Blueprints)](../scripting/persistence.md) |
| Get the naming and folder conventions right | [Best Practices](../best-practices.md) |
| Ship a map instead of a firework | [Custom Maps](custom-maps.md) |
