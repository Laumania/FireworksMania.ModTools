# FAQ

Short answers to the questions that come up most often. Click a question to expand it — every answer links on to the page that goes into detail.

---

## Getting Started

??? question "Which Unity version should I use?"

    Use the exact version the Mod Tools target — the one named at the top of the current release entry in the [CHANGELOG](https://github.com/Laumania/FireworksMania.ModTools/blob/main/CHANGELOG.md). Other versions may appear to work and then fail in ways that are very hard to diagnose, so it is worth being strict about this.

    You can have several Unity versions installed side by side, so installing the right one costs you nothing and will not disturb your other projects.

    See [Getting Started](getting-started.md) for the full install walkthrough.

??? question "Why does my mod look washed out or too bright compared to the game?"

    Your project is almost certainly in **Gamma** color space. The game uses **Linear**.

    Fix it in **Edit → Project Settings → Player → Other Settings → Rendering → Color Space → Linear**. Unity will reimport a lot of assets and take a while, and then everything will look much closer to how it does in game. This is the single most common cause of "my firework looks wrong in game but fine in Unity".

    The Mod Tools also ship a `PrefabEditorScene` you can wire up so prefabs are authored under game-like lighting — see [Templates & Sample Assets](guides/templates-and-samples.md).

??? question "Where is Create New Mod? I can't find it in the menu."

    The menu path is **Mod Tools → Create → New Mod**. It is a submenu, not a single item — which is why it is easy to miss if you are scanning for the words "Create New Mod".

    The full menu, including the utilities most people never find, is on the [Editor Menu Reference](guides/editor-tools.md).

??? question "I can't see the Fireworks Mania definitions in the object picker."

    Assets like `EntityDefinitionType` and the shell/mortar diameter definitions live inside the Mod Tools *package*, and Unity hides package assets from the object picker by default.

    Click the small **eye icon** in the object picker window to toggle package assets on. They show up straight away.

??? question "I upgraded the Mod Tools and everything broke."

    Restart Unity. The CHANGELOG calls this out as very important, and a lot of odd post-upgrade behaviour clears up on a restart. Back up your project before upgrading, every time.

    If components come back as **Missing (Mono Script)** afterwards, there are dedicated repair utilities under **Mod Tools → Utilities → Upgrade**. See [Troubleshooting & Build Errors](guides/troubleshooting.md) and [Editor Menu Reference](guides/editor-tools.md).

---

## Assets & Content

??? question "Can I rename an EntityDefinition after I have published my mod?"

    No — not the **Id**. The Id is the string written into players' blueprints, and there is no aliasing or migration mechanism anywhere in the codebase. Change it and every existing blueprint that used your item loses it.

    There is a subtlety worth knowing: renaming the *asset file* does **not** change the Id. The Id is an independent string that only gets set from the filename when you use the **Set Id to filename** context-menu action. So a rename can quietly desync the two without breaking anything, and then re-running **Set Id to filename** later breaks everything at once.

    See [Publishing Your Mod](guides/publishing.md).

??? question "Can I make my own inventory category, or my own shell diameter?"

    No. `EntityDefinitionType` and `EntityDiameterDefinition` are treated as internal: their `Create` menu entries sit behind a compiler flag that is not set in the Mod Tools, so the entries never appear and there is no way to author one. Pick from the categories and diameters that ship with the package instead.

    Remember the eye icon in the object picker or you will not see any of them. The full list is on [Definitions](script-reference/definitions.md).

??? question "How do I add my own sound?"

    Create a `GameSoundDefinition` with **Create → Fireworks Mania → Definitions → Game Sound Definition**, then drop your `AudioClip`s into its **Audio Variation Clips** list.

    Then pick it from any sound field in the Inspector. Those fields open a search window, and your own sounds appear under the **Others** group while the built-in game sounds sit under **Fireworks Mania**. Only the leaf name is stored in the field, not an asset reference.

    See [Icons & Sounds](guides/icons-and-sounds.md).

??? question "Why did my sound come back as Ambient when I never set it?"

    Because of the numbering behind the **Sound Bus** dropdown: `Default` is **3** and `Ambient` is **0**. Any asset whose sound bus was never written — an older asset, or one created before that field existed — reads back as the zero value, which is `Ambient`.

    That matters because `Ambient` is forced to 2D playback, so your sound will not appear to come from anywhere in the world. Open the asset and set **Sound Bus** explicitly.

??? question "How do I make an inventory icon?"

    Select the prefab **asset** in the Project window and use **Assets → Fireworks Mania → Generate Preview → ...**, which offers Orthographic and Perspective front and back views. Output is 512×512 with a transparent background, written next to the prefab.

    The thing that catches people out is that there are two separate menu families: the `Assets/...` entries work on prefab assets, while **GameObject → Fireworks Mania → Generate Preview → Perspective → Current Veiw In Scene** works on a scene instance and renders from wherever you have parked the Scene view camera. See [Icons & Sounds](guides/icons-and-sounds.md).

??? question "A component I need is not under Add Component → Fireworks Mania. Is it missing?"

    Not necessarily. Most Mod Tools components carry an explicit menu path, but a few do not. Those land in Add Component's default **Scripts** category instead of under **Fireworks Mania** — typing the class name into the **Add Component** search box is the quickest way to reach them.

    One of them, `DestructibleBehavior`, is also flagged in the CHANGELOG as not ready for mod use, and nothing since has retracted that — treat it as experimental. See [Behaviors](script-reference/behaviors.md).

---

## Scripting

??? question "Can my mod contain C# code at all?"

    Yes. Scripting is fully supported — put `.cs` files inside your mod folder and the build compiles them into an assembly that ships inside your `.mod` file.

    The one hard rule is that you ship **source, never a compiled DLL**. Precompiled assemblies are rejected, so a third-party library has to be vendored as source and must itself pass the security checks.

    Start at [Scripting](scripting/index.md).

??? question "Should I add an .asmdef to my mod folder?"

    **No.** This is the one place where normal Unity advice is exactly wrong.

    The mod build compiles from `Assembly-CSharp.csproj`. Putting your scripts in their own assembly definition moves them out of that project, and they are then skipped — usually with the warning `The C# source file '...' exists in the Unity project but not in the .csproj file and will not be compiled. You may need to regenerate the script project file`, which is easy to miss in a busy Console.

    Leave your scripts in the default assembly. See [Setting Up Scripts in a Mod](scripting/setup.md).

??? question "What can't I use in a mod script?"

    The build enforces a security deny list. You cannot use `System.IO`, `System.Reflection`, `System.Runtime.InteropServices`, `System.AppDomain`, `System.Threading.Process`, `UnityEngine.Application.Quit`, P/Invoke, or the `UnityEditor` and `Mono.Cecil` assemblies. `unsafe` code is off as well.

    The practical consequence that surprises people most: **a mod cannot read or write files**, so there is no config file, no log file and no save file of your own. Violations fail the build with `Assembly '...' has failed code security verification.`

    The full list is on [Setting Up Scripts in a Mod](scripting/setup.md).

??? question "Do I need to reference UniTask, DOTween or Newtonsoft.Json?"

    No. They are all bundled with the Mod Tools and automatically available to your scripts, along with everything under `FireworksMania.Core`, Netcode for GameObjects and TextMeshPro. Just add the `using` directive and go.

    Do **not** install your own copy of UniTask or DOTween — a duplicate will break the build. If you are upgrading a very old project that installed them separately, remove those copies first.

??? question "Where do I put code that should run once when the map loads?"

    On a prefab referenced by a `StartupPrefabDefinition`. That is the only entry point for logic that is not attached to a spawned entity.

    The definition's tooltips describe the behaviour: one instance of the prefab is instantiated in the map after all mods have loaded, and multiple startup prefabs are ordered by **Sort Order**, lowest first.[^startupprefab] Put your setup in `Start()` and your cleanup in `OnDestroy()`.

    See [Entry Points & Lifecycle](scripting/entry-points.md).

??? question "Why is DependencyResolver returning null?"

    Three separate reasons, and all three are normal:

    - The services live in the **game**, not in the Mod Tools project, so in the Unity Editor there is simply nothing to resolve. It will always be null there.
    - `Get<T>()` only searches **active** MonoBehaviours in the loaded scenes.
    - It returns `null` rather than throwing when nothing matches.

    So the resolver is a lookup with no guarantees. Write `DependencyResolver.Instance?.Get<IMyService>()` and null-check the result every single time. See [Services & Interfaces](scripting/services-and-interfaces.md).

??? question "I renamed my custom firework class and every saved blueprint broke."

    Custom saveable components are keyed by their C# class name. Rename the class and the saved data no longer matches anything, so it is dropped on load.

    Treat your class name exactly like the definition Id: choose it carefully, then never change it. If you must reorganise, keep the class name and move the file instead. See [Saving & Loading](scripting/persistence.md).

??? question "My custom save data silently comes back empty."

    The read helper on the saved-data container swallows failures. If the stored value cannot be deserialised or cast to the type you asked for, you get `default(T)` back and **no log message at all** — so "I never saved it" and "I saved it wrong" look identical.

    Check whether the key is present yourself before reading it, so you can tell the two cases apart, and keep the saved shape simple. See [Saving & Loading](scripting/persistence.md).

---

## Multiplayer

??? question "Can mods use Netcode for GameObjects — RPCs and NetworkVariables?"

    **Yes.** This changed in Mod Tools v2025.8.1: the build pipeline now runs Netcode's own code generator over your mod assembly, which is exactly what was missing before. The old `FMNetworkVariable*` workaround types are now marked obsolete as compile errors, with a message telling you to use real `NetworkVariable`s instead.

    Any documentation or forum post saying mods cannot use RPCs or NetworkVariables — including the Mod Tools README's own "Known limitations" section — predates this and is stale.

    Two caveats worth carrying: use the modern `[Rpc(SendTo.X)]` attribute rather than the older `[ServerRpc]` style, which does not appear anywhere in this codebase; and treat the feature as supported-but-advanced. The CHANGELOG's own wording is that you *should* be able to use these, and 99% of mod creators never will. See [Multiplayer & Netcode](scripting/networking.md).

??? question "Why won't Network Object Prefabs work in my custom map?"

    Because that one really is still broken. The **Network Object Prefabs** field on `MapDefinition` carries its own warning in the tooltip: `[This is currently not working - awaiting a fix from Unity and NetCode Team]`.[^networkprefabs]

    Do not confuse this with the Netcode CodeGen limitation above — that one is fixed, this one is not. Moveable networked objects placed directly in a modded map scene should be assumed not to sync. See [Custom Maps](guides/custom-maps.md).

??? question "Why does my object work for the host but not for clients?"

    Almost always a missing server gate. In a server-authoritative setup the host runs your code and the clients do not, so anything written only on the local machine simply never reaches them.

    The pattern is: clients ask the server (an RPC), the server changes state, and the state replicates to everyone through a `NetworkVariable` that only the server may write. Guard server-only work with `if (!IsServer) return;`.

    Note the deliberate exception: sounds, camera shake and explosion forces are broadcast locally on every peer by design, so those are not server-gated. See [Multiplayer & Netcode](scripting/networking.md).

??? question "How should I destroy a networked object?"

    Use the `DestroyOrDespawn()` extension on the GameObject, not plain `Destroy()`. It despawns properly when the object is a spawned `NetworkObject` and falls back to a normal destroy when it is not — which is what you want, because the same prefab may be used in both situations.

    Since despawning is a server operation, expect this to be effectively server-only for spawned objects. See [Multiplayer & Netcode](scripting/networking.md).

??? question "Should I test in singleplayer or multiplayer?"

    Test in **singleplayer** by default. Other players cannot see your mod unless they have installed it themselves, so a multiplayer test with friends who do not have your mod tells you nothing useful — and it makes ordinary problems look like networking problems.

    Once the mod works in singleplayer, test multiplayer specifically to check replication.

---

## Building & Publishing

??? question "My build fails with something about an EventSystem."

    The exact message is `Found 'EventSystem' in scene '{scene}' on GameObject '{go}'. The game already have a EventSystem so this should not be in your scene. Delete the EventSystem GameObject and build the mod again.`

    You did not add it deliberately — Unity creates an `EventSystem` automatically when you add a UI Canvas. Delete that GameObject and rebuild. The check finds inactive objects at any depth, so search the whole Hierarchy.

    See [Troubleshooting & Build Errors](guides/troubleshooting.md).

??? question "My build fails and the error mentions an EntityDefinition."

    There are four checks on definitions and they all stop the build: a definition with no prefab; a prefab with no definition; a crossed link where the definition points at a prefab that points back at a *different* definition; and an inventory definition with no **Entity Definition Type**.

    The crossed-link case is the common one, and duplicating an asset to make a variant is how you get it. Each message is quoted verbatim with its fix on [Troubleshooting & Build Errors](guides/troubleshooting.md).

??? question "My map has no spawn point but the build succeeded — is that right?"

    Yes, unfortunately. The build-time check for a missing `PlayerSpawnLocation` is commented out in the build processor, so you get no warning whatsoever. Players will spawn at world origin.

    Add one with **GameObject → Fireworks Mania → Maps → Player Spawn Location Prefab** and verify it by eye. See [Custom Maps](guides/custom-maps.md).

??? question "Where do I publish my mod?"

    On mod.io: [mod.io/g/fireworksmania](https://mod.io/g/fireworksmania). Local `.mod` files in your own Mods folder are for testing — nobody else can get them.

    Before you upload, run through the pre-flight checklist and double-check the one thing you can never take back: the **Id** on every definition. See [Publishing Your Mod](guides/publishing.md).

??? question "How do I test a change I just made?"

    Rebuild with **Mod Tools → Build Mod** (++ctrl+shift+b++), then use **Restart Map** inside the game. The game notices the `.mod` file changed and reloads it on map restart — you do not need to quit and relaunch.

    If your change does not appear, check the Console for a failed build before you go looking for anything more exotic. See [Troubleshooting & Build Errors](guides/troubleshooting.md).

---

Still stuck? [Troubleshooting & Build Errors](guides/troubleshooting.md) has the full symptom-to-fix tables, including the exact text of every build-blocking error.

[^startupprefab]: This behaviour is described by the tooltips on the `StartupPrefabDefinition` fields. The code that reads these definitions and instantiates the prefabs lives in the game itself, not in the Mod Tools package, so it cannot be confirmed from the Mod Tools source.

[^networkprefabs]: Quoted from the tooltip on the `Network Object Prefabs` field in `MapDefinition`. Whether it is still true after the Netcode CodeGen change cannot be determined from the Mod Tools source — treat it as an open limitation until the author says otherwise.
