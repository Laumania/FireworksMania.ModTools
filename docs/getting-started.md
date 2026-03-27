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

> **No coding required.** Mods can be built entirely through the Unity Inspector. Scripting is available for advanced users only.

---

## Step 1 — Install Unity Hub & Git

1. [Download and install Unity Hub](https://unity.com/download). Unity Hub is the launcher that manages your Unity Editor installations.
2. [Download and install Git](https://git-scm.com). Unity needs Git to fetch the Mod Tools package from GitHub.
3. **Restart your PC** after installing both tools before continuing.

---

## Step 2 — Install the Correct Unity Editor Version

The Mod Tools target a specific Unity version. Using any other version may cause errors.

1. Check the [CHANGELOG](https://github.com/Laumania/FireworksMania.ModTools/blob/main/CHANGELOG.md) for the current target Unity version.
2. Click the version number in the CHANGELOG — this opens Unity's website where you can click **Install** to add it to Unity Hub automatically.

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

## Step 4 — Install the Fireworks Mania Mod Tools

1. In Unity, go to **Window → Package Manager**.
2. Click the **+** button (top-left) and select **Add package from git URL…**
3. Paste the following URL and click **Add**:

```
https://github.com/Laumania/FireworksMania.ModTools.git
```

The installation may take a few minutes. If a dialog appears asking to restart the Editor, click **Yes**.

After the Editor restarts, click **Play** again to verify there are no errors, then exit Play mode.

---

## Step 5 — Create Your Mod Folder Structure

It is good practice to keep all mods inside a dedicated `Mods` folder in your project.

1. In the **Project** window, right-click in `Assets` and create a new folder named `Mods`.
2. Go to **Mod Tools → Create New Mod** from the Unity menu bar.
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

## Step 6 — Configure Export Settings

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

## Step 7 — Create an EntityDefinition

An `EntityDefinition` is a ScriptableObject that describes a spawnable item — it holds the item name, icon, prefab reference, and a globally unique ID.

To create a `FireworkEntityDefinition`:

1. Right-click in your `Definitions` folder.
2. Select **Create → Fireworks Mania → Definitions → Firework Entity Definition**.
3. Name it using the convention `YourNick_Type_ItemName`, for example `Laumania_Cake_TutorialCake`.

!!! tip "Naming is the ID"
    The filename of the definition becomes its **EntityDefinitionId**. This ID is used to save items in blueprints. **Never rename a definition after publishing a mod**, or existing blueprints that reference it will break.

Fill in the **Inspector** fields:

| Field | Description |
|---|---|
| **Id** | Unique string ID — use the context menu **Set Id to filename** to set it automatically |
| **Prefab Game Object** | The prefab that will be spawned in-game |
| **Item Name** | The display name shown in the inventory |
| **Icon** | A sprite used for the inventory thumbnail |
| **Entity Definition Type** | The category in the inventory (select from the list) |

---

## Step 8 — Create Your Prefab

1. Create a new prefab in your `Prefabs` folder.
2. Add the appropriate firework behavior component to the root object (e.g. `CakeBehavior`).
3. Assign the `EntityDefinition` and `Fuse` references in the Inspector.
4. Add all required child objects (particle systems, fuse visual, etc.).

Refer to the existing prefabs in `FireworksMania/Prefabs/` for examples.

---

## Step 9 — Build and Test Your Mod

1. Go to **Mod Tools → Build Mod** (or press **Ctrl+Shift+B**).
2. Start Fireworks Mania, load a map, and your mod will appear in the inventory.

When you make a change, rebuild the mod and then use **Restart Map** inside the game — the game detects that the mod file has changed and reloads it automatically.
