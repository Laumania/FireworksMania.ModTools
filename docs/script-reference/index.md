# Script Reference — Overview

This section documents every script and component included in the Fireworks Mania Mod Tools that is relevant to mod creators.

---

## Namespaces

All Mod Tools scripts live under the `FireworksMania.Core` namespace hierarchy:

| Namespace | Contents |
|---|---|
| `FireworksMania.Core.Behaviors` | General-purpose behavior components |
| `FireworksMania.Core.Behaviors.Fireworks` | Firework-type behaviors |
| `FireworksMania.Core.Behaviors.Fireworks.Parts` | Low-level firework part components |
| `FireworksMania.Core.Definitions` | Map, sound, and startup definitions |
| `FireworksMania.Core.Definitions.EntityDefinitions` | Entity (item) definition types |
| `FireworksMania.Core.Attributes` | Custom Unity Inspector attributes |

---

## Pages in This Section

| Page | What It Covers |
|---|---|
| [Definitions](definitions.md) | `BaseEntityDefinition`, `FireworkEntityDefinition`, `PropEntityDefinition`, `MapDefinition`, `GameSoundDefinition`, `StartupPrefabDefinition` |
| [Behaviors](behaviors.md) | `PlaySoundBehavior`, `PlaySoundOnImpactBehavior`, `ToggleBehavior`, `UseableBehavior`, `ErasableBehavior`, `IgnorePhysicsToolBehavior`, `IgnorePickUpBehavior`, firework behaviors |
| [Firework Parts](firework-parts.md) | `Fuse`, `FuseConnectionPoint`, `Thruster`, `ExplosionBehavior`, `ExplosionPhysicsForceEffect`, `MortarTube`, `ParticleSystemExplosion`, and more |

---

## Assembly Definitions

The Mod Tools ship with two assembly definition files:

| Assembly | Contents |
|---|---|
| `FireworksMania.Core` | All runtime scripts (behaviors, definitions, utilities) |
| `FireworksMania.Core.Editor` | Editor-only scripts (property drawers, helpers) |
