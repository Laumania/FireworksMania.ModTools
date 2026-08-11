# Guides

Task-oriented walkthroughs for building, fixing and shipping a mod. If you know what you want to do but not where the button is, start here.

---

## I Want To…

| Goal | Where to go |
|---|---|
| Install the tools and build my very first mod | [Getting Started](../getting-started.md) |
| Make a firework without assembling one from scratch | [Templates & Sample Assets](templates-and-samples.md) |
| Find a menu item — *"where is Build Mod / Generate Preview / Rebuild Reference Cache?"* | [Editor Menu Reference](editor-tools.md) |
| Give my firework an inventory icon | [Icons & Sounds](icons-and-sounds.md) |
| Use one of the built-in game sounds, or add my own | [Icons & Sounds](icons-and-sounds.md) |
| Build a custom map | [Custom Maps](custom-maps.md) |
| Fix a build that fails, or an error in the Console | [Troubleshooting & Build Errors](troubleshooting.md) |
| Fix *"my mod looks brighter and flatter in the Editor than in game"* | [Getting Started](../getting-started.md) |
| Name things properly and lay out my project sensibly | [Best Practices](../best-practices.md) |
| Make my mod smaller and quicker to load | [Optimization](../optimization.md) |
| Put my mod on mod.io so other people can play it | [Publishing Your Mod](publishing.md) |
| Write C# code for my mod | [Scripting](../scripting/index.md) |
| Just get a quick answer | [FAQ](../faq.md) |

---

## The Guides

| Guide | What it covers |
|---|---|
| [Editor Menu Reference](editor-tools.md) | Every menu item the Mod Tools add, with exact paths and which right-click they live under |
| [Templates & Sample Assets](templates-and-samples.md) | The ready-made firework templates, the sample assets that ship with the package, and the prefab editing scene |
| [Icons & Sounds](icons-and-sounds.md) | Generating inventory icons, and how the game sound picker works |
| [Custom Maps](custom-maps.md) | `MapDefinition`, spawn points, and the scene rules the build enforces |
| [Troubleshooting & Build Errors](troubleshooting.md) | Symptom → cause → fix for the errors you will actually hit |
| [Publishing Your Mod](publishing.md) | Pre-flight checklist, mod.io, and versioning without breaking anyone's blueprints |

Two more pages sit alongside these guides: [Best Practices](../best-practices.md) for conventions, and [Optimization](../optimization.md) for keeping your mod lean.

---

## Two Rules Worth Knowing Up Front

!!! danger "Never change an EntityDefinition Id after publishing"
    The `Id` is what gets written into players' blueprints, and there is no migration or aliasing mechanism anywhere in the Mod Tools. See [Publishing Your Mod](publishing.md).

!!! warning "Always restart Unity after upgrading the Mod Tools"
    Back up the project, update the package, then restart Unity — the CHANGELOG calls the restart out as very important on every release. See [Troubleshooting & Build Errors](troubleshooting.md).

---

## No Coding Required

Everything in these guides can be done through the Unity Inspector. Scripting is entirely optional — if you do want to write code, the [Scripting](../scripting/index.md) section covers setup, lifecycle, and the parts of the API that are safe to use.
