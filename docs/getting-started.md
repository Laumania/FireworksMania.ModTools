# Getting Started

This guide walks you through installing the Fireworks Mania Mod Tools and building your first mod from scratch.

---

## Prerequisites

Before you begin, make sure you have a basic familiarity with the following tools. No advanced skills are required, but some experience will help.

| Tool | Why You Need It | Resources |
|---|---|---|
| [Unity](https://unity.com) | The mod tools run inside the Unity Editor | [Unity beginner tutorial](https://www.youtube.com/watch?v=pwZpJzpE2lQ) |
| [Blender](https://www.blender.org) (or any .fbx exporter) | Creating 3D models for your mod | [Low poly Blender tutorial](https://www.youtube.com/watch?v=1jHUY3qoBu8) |
| [Git](https://git-scm.com) | Unity uses Git to install packages | — |

> **No coding required.** Mods can be built entirely through the Unity Inspector. C# [scripting](scripting/index.md) is available if you want it, but nothing on this page needs it.

---

## Step 1 — Install Unity Hub & Git

1. [Download and install Unity Hub](https://unity.com/download). Unity Hub is the launcher that manages your Unity Editor installations.
2. [Download and install Git](https://git-scm.com). Unity needs Git to fetch the Mod Tools package from GitHub.
3. **Restart your PC** after installing both tools before continuing.

---

## Step 2 — Install the Correct Unity Editor Version

The Mod Tools target a specific Unity version. Using any other version may cause errors.

1. Check the [CHANGELOG](https://github.com/Laumania/FireworksMania.ModTools/blob/main/CHANGELOG.md) for the current release — each entry names its target Unity version at the top.
2. Install that version from Unity Hub. Clicking the version number in the CHANGELOG opens Unity's website, where **Install** adds it to Unity Hub for you.

You can have multiple Unity versions installed side-by-side, so this will not affect any other projects.

---

## Step 3 — Create a New Unity Project

1. Open Unity Hub and click **New project**.
2. Choose the **3D (Built-in Render Pipeline)** template.
3. Name your project. A good convention is something like `YourNick.FireworksMania.Mods` since a single Unity project can hold multiple mods.
4. Click **Create project**.

Once the project opens, click the **Play** button to enter Play mode and confirm there are no errors in the Console. Click **Play** again to exit Play mode before continuing.

> ⚠️ **Always exit Play mode before making changes.** Modifications made while in Play mode are lost when you stop.

---

## Step 4 — Match the Game's Color Space

A brand new Unity project uses **Gamma** color space. Fireworks Mania runs in **Linear**. If you skip this step, everything you author will look brighter and more washed out in the Editor than it does in the game — and you will spend hours "fixing" lighting that was never broken.

1. Go to **Edit → Project Settings → Player**.
2. Open **Other Settings → Rendering**.
3. Set **Color Space** to **Linear**.

Unity will reprocess the project's assets, which takes a while. Do it now, before installing the Mod Tools — there is far less to reimport on an empty project.

!!! tip "The single most common visual complaint"
    "My mod looks too bright compared to the game" is nearly always this setting. This instruction comes from `FireworksMania/Scenes/Editor/Readme.txt`, which ships inside the Mod Tools package.

---

## Step 5 — Install the Fireworks Mania Mod Tools

1. In Unity, go to **Window → Package Manager**.
2. Click the **+** button (top-left) and select **Add package from git URL…**
3. Paste the following URL and click **Add**:

```
https://github.com/Laumania/FireworksMania.ModTools.git
```

The installation may take a few minutes. If a dialog appears asking to restart the Editor, click **Yes**.

After the Editor restarts, click **Play** again to verify there are no errors, then exit Play mode.

---

## Step 6 — Create Your Mod Folder Structure

It is good practice to keep all mods inside a dedicated `Mods` folder in your project.

1. In the **Project** window, right-click in `Assets` and create a new folder named `Mods`.
2. Go to **Mod Tools → Create → New Mod** from the Unity menu bar.
3. Give your mod a unique name. Prefix it with your nickname to avoid conflicts with other mods:

    ```
    YourNick_ModName
    ```

    Avoid spaces and special characters in the mod name.

4. Place the mod folder inside `Assets/Mods`.

Inside your new mod folder, create the following subfolders to keep things organized:

```
Assets/
└── Mods/
    └── YourNick_ModName/
        ├── Definitions/   ← ScriptableObject definitions
        ├── Icons/         ← Inventory icons (sprites)
        ├── Models/        ← 3D model files (.fbx)
        └── Prefabs/       ← Assembled prefabs
```

---

## Step 7 — Configure Export Settings

Go to **Mod Tools → Export Settings** and fill in the following fields under **Mod Information**:

| Field | Description |
|---|---|
| **Mod Name** | Display name shown to players |
| **Mod Version** | Semantic version, e.g. `1.0.0` |
| **Mod Author** | Your name or nickname |

Under the **Build** tab, set **Optimize for** to **File Size**.

!!! warning "File Size Optimization is Critical"
    Always set **Optimize for** to **File Size**. Skipping this step will make your mod larger than necessary, increasing download time and game load time for every player who uses it.

Set the **Mod Export Directory** to the game's local Mods folder so that every time you build the mod it is automatically available in-game:

```
%userprofile%\appdata\locallow\Laumania ApS\Fireworks mania\Mods
```

Paste this path into the address bar of the file picker dialog that opens when you click the **…** button.

---

## Step 8 — Create an EntityDefinition

An `EntityDefinition` is a ScriptableObject that describes a spawnable item — it holds the item name, icon, prefab reference, and a globally unique ID.

To create a `FireworkEntityDefinition`:

1. Right-click in your `Definitions` folder.
2. Select **Create → Fireworks Mania → Definitions → Firework Entity Definition**.
3. Name it using the convention `YourNick_Type_ItemName`, for example `Laumania_Cake_TutorialCake`.

!!! warning "The filename is not the ID"
    The **Id** is its own serialized text field, completely separate from the asset filename. A new definition starts out with the placeholder `INSERT UNIQUE DEFINITION ID`, and the Console keeps nagging you until you change it.

    The **Set Id to filename** action copies the filename into the Id *once, when you click it*. Renaming the asset afterwards does **not** update the Id, and changing the Id does not rename the asset. Keeping the two in sync is a convention you maintain by hand.

!!! danger "Never change the Id after publishing"
    The Id — not the filename — is what gets written into players' blueprint save files. Change it after release and every blueprint that placed your item can no longer find it. There is no aliasing or migration mechanism. Set it once, keep it forever.

Fill in the **Inspector** fields:

| Field | Description |
|---|---|
| **Id** | Unique string ID. Fill it in yourself, or use the **Set Id to filename** context-menu action described below |
| **Prefab Game Object** | The prefab that will be spawned in-game |
| **Item Name** | The display name shown in the inventory |
| **Icon** | A sprite used for the inventory thumbnail |
| **Entity Definition Type** | The category the item appears under in the inventory. This is an object reference to an `EntityDefinitionType` asset that ships inside the Mod Tools package — not a dropdown |

!!! tip "The type picker looks empty"
    The `EntityDefinitionType` assets live inside the Mod Tools package, and Unity's object picker hides package assets by default. If the picker comes up with nothing in it, toggle the eye icon in the picker window to show assets from packages.

!!! tip "Where *is* Set Id to filename?"
    It lives on the **Inspector's** context menu, not the Project window's. Select the definition asset so it shows in the Inspector, then click the **⋮** (three-dot) button at the top-right of the Inspector header — or right-click the header itself. **Set Id to filename** is in that menu.

    Right-clicking the asset in the **Project** window will not show it.

---

## Step 9 — Create Your Prefab

1. Create a new prefab in your `Prefabs` folder.
2. Add the appropriate firework behavior component to the root object (e.g. `CakeBehavior`).
3. Assign the `EntityDefinition` and `Fuse` references in the Inspector.
4. Add all required child objects (particle systems, fuse visual, etc.).

Refer to the existing prefabs in `FireworksMania/Prefabs/` for examples. Faster still: start from one of the ready-made templates under **GameObject → Fireworks Mania → Templates** — see [Templates & Sample Assets](guides/templates-and-samples.md).

---

## Step 10 — Build and Test Your Mod

1. Go to **Mod Tools → Build Mod** (or press ++ctrl+shift+b++).
2. Start Fireworks Mania, load a map, and your mod will appear in the inventory.

When you make a change, rebuild the mod and then use **Restart Map** inside the game — the game detects that the mod file has changed and reloads it automatically.

---

## What's Next

You now have a mod that builds. Where you go from here depends on what you are making.

| I want to… | Go to |
|---|---|
| Start from a ready-made firework instead of an empty prefab | [Templates & Sample Assets](guides/templates-and-samples.md) |
| Give my item an inventory icon and custom sounds | [Icons & Sounds](guides/icons-and-sounds.md) |
| Build a custom map | [Custom Maps](guides/custom-maps.md) |
| Fix a build error or a weird Console message | [Troubleshooting & Build Errors](guides/troubleshooting.md) |
| Find a menu item I can't locate | [Editor Menu Reference](guides/editor-tools.md) |
| Keep my mod maintainable and multiplayer-safe | [Best Practices](best-practices.md) |
| Keep the download small and the framerate high | [Optimization](optimization.md) |
| Write C# and do things the Inspector can't | [Scripting](scripting/index.md) |
| Look up a component, definition or field | [Script Reference](script-reference/index.md) |
| Upload it for other players | [Publishing Your Mod](guides/publishing.md) |

Stuck on something small? The [FAQ](faq.md) covers the questions that come up most often, and the [Guides](guides/index.md) index routes to everything else.
