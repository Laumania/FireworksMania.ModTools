# Script Reference — Overview

This section is the field guide to the assets and components you drop into a mod: the definition ScriptableObjects, the behavior components, and the firework parts they are built from.

---

## Namespaces

All Mod Tools scripts live under the `FireworksMania.Core` namespace hierarchy. These are the runtime namespaces you can `using` from a mod script:

| Namespace | Contents |
|---|---|
| `FireworksMania.Core` | `CoreSettings` (global game toggles) and `DependencyResolver` (how a mod reaches game services) |
| `FireworksMania.Core.Attributes` | Custom Unity Inspector attributes — `GameSoundAttribute`, `ReadOnlyAttribute` |
| `FireworksMania.Core.Behaviors` | General-purpose behavior components |
| `FireworksMania.Core.Behaviors.Fireworks` | Firework-type behaviors |
| `FireworksMania.Core.Behaviors.Fireworks.Parts` | Low-level firework part components |
| `FireworksMania.Core.Behaviors.FiringSystem` | Firing-system controller and receiver behaviors |
| `FireworksMania.Core.Common` | Shared building blocks — `SerializableNullable<T>`, `ClientNetworkTransform`, `ClientNetworkRigidbody`, `PlayerSpawnLocation` |
| `FireworksMania.Core.Definitions` | Map, sound, character and startup definitions |
| `FireworksMania.Core.Definitions.EntityDefinitions` | Entity (item) definition types |
| `FireworksMania.Core.Interactions` | `IAmGameObject`, `IsPickedUp` |
| `FireworksMania.Core.Messaging` | The `Messenger` event bus and the `MessengerEvent…` message types |
| `FireworksMania.Core.Netcode` | `FuseNetworkIdentifier`, plus the obsolete `FMNetworkVariable*` shims |
| `FireworksMania.Core.Persistence` | Blueprint save/load — `SaveableEntity`, `ISaveableComponent` |
| `FireworksMania.Core.Tools` | `IFuseConnectionTool` |
| `FireworksMania.Core.Utilities` | `GameObjectExtensions`, `TransformExtensions`, `GizmosUtility`, `Preconditions` |

!!! tip "The three you'll reach for most"
    If you are writing C# in a mod, `FireworksMania.Core.Messaging`, `FireworksMania.Core.Persistence` and `FireworksMania.Core.Utilities` are the ones that come up again and again — reacting to game events, surviving a blueprint save, and despawning things correctly.

There are also four editor-only namespaces (`FireworksMania.Core.Editor`, `.Editor.Helpers`, `.Editor.PropertyDrawers`, `.Editor.Utilities`). They hold property drawers, menu items and the build processors. Mod scripts cannot use them — editor assemblies are not available to a built mod.

---

## Writing Scripts

This section is a **reference**: what each type is, what its Inspector fields do, what it derives from. If you are looking for how to actually get C# into a mod — where the files go, what you're allowed to use, and how the lifecycle works — start with the [Scripting](../scripting/index.md) section instead.

---

## Pages in This Section

| Page | What It Covers |
|---|---|
| [Definitions](definitions.md) | The `EntityDefinition` family, `MapDefinition`, `GameSoundDefinition`, `StartupPrefabDefinition`, `CharacterDefinition`, and the shipped category and diameter assets |
| [Behaviors](behaviors.md) | `PlaySoundBehavior`, `PlaySoundOnImpactBehavior`, `ToggleBehavior`, `UseableBehavior`, `ErasableBehavior`, `IgnorePhysicsToolBehavior`, `IgnorePickUpBehavior`, firework behaviors |
| [Firework Parts](firework-parts.md) | `Fuse`, `FuseConnectionPoint`, `Thruster`, `ExplosionBehavior`, `ExplosionPhysicsForceEffect`, `MortarTube`, `ParticleSystemExplosion`, and more |

Related pages elsewhere in the docs:

| Page | What It Covers |
|---|---|
| [Scripting Overview](../scripting/index.md) | Getting C# into a mod at all, and which page to read next |
| [The Messenger Event Bus](../scripting/messaging.md) | `FireworksMania.Core.Messaging` — subscribing to and broadcasting game events |
| [Saving & Loading](../scripting/persistence.md) | `FireworksMania.Core.Persistence` — making custom state survive a blueprint save |
| [Services & Interfaces](../scripting/services-and-interfaces.md) | `DependencyResolver` and which interfaces you implement versus consume |

---

## Assembly Definitions

Two of the assembly definition files in the project hold the Fireworks Mania scripts themselves:

| Assembly | Contents |
|---|---|
| `FireworksMania.Core` | All runtime scripts (behaviors, definitions, messaging, persistence, utilities). Auto-referenced, so a mod script can `using` any of the namespaces above with no setup. |
| `FireworksMania.Core.Editor` | Editor-only scripts (property drawers, menu items, build processors) |

A third, `FireworksMania.Editor.UModTools`, holds the Mod Tools editor windows and the mod build pipeline. The remaining assembly definitions in the project — eleven `.asmdef` files ship in total — belong to the bundled third-party packages: UniTask, the DOTween modules, and Runtime Preview Generator.

!!! warning "Do not add an .asmdef to your own mod folder"
    This is the opposite of normal Unity advice. Mod scripts have to stay in the default `Assembly-CSharp` assembly or the mod build will skip them entirely. See [Setting Up Scripts in a Mod](../scripting/setup.md).
