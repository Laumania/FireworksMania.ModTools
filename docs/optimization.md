# Optimization

A small mod downloads faster, loads faster and is easier on players' hardware. This page is for when your mod works and you want it to be lean — it covers what to cut, and the few places where the game's own components will quietly do a lot of work on your behalf.

---

## How to Read the Numbers on This Page

Two different kinds of numbers appear below, and it matters which is which.

| Kind | What it means | Examples |
|---|---|---|
| **Authored guidance** | Rules of thumb from experience. Nothing checks them, nothing enforces them, and a good reason to exceed one is a good enough reason. | Triangle counts, texture resolutions, mod file-size targets |
| **How the code behaves** | Things the Fireworks Mania components genuinely do at runtime, which decide what your prefab actually costs. | The per-particle explosion pass, the particle observer's per-frame work |

Everything under *3D Models*, *Audio* and *Mod File Size Budget* is guidance. Where a section describes what the code does, it says so explicitly.

!!! note "There are no enforced budgets"
    Nothing in the Mod Tools caps your particle counts, rations your debris spawns, or refuses to build a mod for being expensive. Every number below is either yours to judge or a description of work the game will happily do as many times as you ask it to.

---

## Build Settings — File Size Optimization

This is the single most important setting. In **Mod Tools → Export Settings**, under the **Build** tab, always set **Optimize for** to **File Size**.

!!! danger "Always use File Size optimization"
    If you leave this at the default setting your mod will be significantly larger than necessary. This increases download time for every player who subscribes to your mod and slows down game loading.

---

## 3D Models

!!! note "Guidance, not limits"
    Nothing in the Mod Tools inspects your meshes or textures, and no build step rejects a model for being too dense. The numbers in this section are suggestions that keep mods small and consistent with the game's own art — treat them as a starting point, not a rule.

### Polygon Count

- Aim for the **lowest polygon count** that still looks good in-game.
- Fireworks are small objects — players rarely zoom in closely. High-poly meshes add file size without visible improvement.
- A typical firework shell or tube: **200–800 triangles** is usually plenty.
- Avoid smooth-shading on hard-edged low-poly objects; use flat shading instead.

### Texture Atlasing

- Combine multiple small textures into a single **texture atlas** to reduce draw calls.
- Use a shared atlas across multiple items in the same mod if possible.

### Texture Resolution

Suggested ceilings, not enforced ones:

| Object Size | Suggested Max Texture Resolution |
|---|---|
| Small (fuse, cap) | 64×64 or 128×128 |
| Medium (tube, rocket body) | 256×256 |
| Large (mortar, map prop) | 512×512 |

Avoid 1024×1024 or higher unless the object is very large and viewed up close.

### Import Settings

In the Unity Inspector for each texture, check:

- **Compression**: leave it on **Normal Quality** and tick **Use Crunch Compression** where the artefacts are acceptable.
- **Max Size**: Set to the lowest value that looks acceptable.
- **Generate Mipmaps**: Enable for objects that appear at varying distances.
- **Read/Write Enabled**: Disable unless explicitly required.

---

## Particle Systems

Particle systems have a large impact on both file size and runtime performance.

### General Rules

- Use as **few particles** as possible while achieving the desired visual effect.
- Prefer **short-lived particles** over long-lived ones — they are cheaper and produce less overdraw.
- Use **Sprite Sheet animations** instead of many separate particle systems for complex effects.

### Max Particles and the particle observer

`ParticleSystemObserver` — the component that watches a particle system and reports when individual particles are born and die — does that the expensive way. Every frame it reads *all* currently live particles into a freshly allocated array and diffs that array against the set it saw last frame, so both the work and the garbage it produces scale directly with how many particles are alive.

There is no cap and no warning. If you raise **Max Particles** on an observed system, the observer will faithfully do the extra work.

The cost is only paid while something is listening. The observer returns immediately when nothing has subscribed to its spawn and destroy events, so a dense particle system with no `ParticleSystemExplosion`, `ParticleSystemSound` or `ParticleSystemShellSound` next to it costs nothing extra.

!!! tip "Keep observed systems small"
    Put the observer on the sparse system that needs per-particle events, and leave the dense system that provides the visual unobserved. A few dozen observed particles is a very different proposition from a few thousand.

    Two other things worth knowing about the observer: it needs a `ParticleSystem` on the *same* GameObject — it logs an error if there isn't one — and once its system has been alive and then finished, the observer switches itself off as an optimisation and does not come back on its own.

### GPU Instancing

Enable **GPU Instancing** on particle materials:

1. Select the material used by the particle system.
2. In the Inspector, enable **Enable GPU Instancing**.

This cuts CPU overhead when many instances of the same particle effect are active at once.

!!! note "Only mesh particles benefit"
    Unity applies particle GPU instancing only when the Particle System's **Renderer → Render Mode** is **Mesh** and the material's shader supports instancing. On the billboard particles most firework effects are built from, ticking the box changes nothing. It is worth doing on mesh-based debris, shells and tubes.

### Sub-Emitters

- Use sub-emitters sparingly. Each sub-emitter adds overhead.
- Collapse multiple sub-emitters into a single particle system where the visual result is the same.

### Simulation Space

- Use **Local** simulation space for effects that should move with the firework.
- Use **World** simulation space for effects that should stay in place after launch (e.g. smoke trail).

---

## Explosion Forces From Particles

`ParticleSystemExplosion` is what makes individual stars from a burst push objects around and set things alight. It sits next to a `ParticleSystemObserver` and has two fields — **Particle Spawned Physics Effect** and **Particle Destroyed Physics Effect** — each taking an `ExplosionPhysicsForceEffect`.

!!! warning "This runs a full explosion per particle"
    Every particle that spawns, and every particle that dies, triggers a complete explosion pass on the effect you assigned: a physics overlap query around that particle's position, followed by force, ignition and shake work on everything it found.

    A system emitting a few hundred particles therefore performs a few hundred of those passes. It is one of the most expensive things you can build here by accident, and nothing warns you about it — it shows up as a framerate cliff on players' machines the moment your firework bursts.

Practical rules:

- If an effect doesn't need to push objects around, leave both fields **blank** — or simply don't add `ParticleSystemExplosion` at all. With both fields blank, no force pass is wired up.
- If you need force, put `ParticleSystemExplosion` on a **small, sparse** particle system — a handful of heavy stars — rather than on the dense system that provides the visual.
- Keep the assigned `ExplosionPhysicsForceEffect`'s **Range** small. The cost of each pass scales with how many colliders fall inside it.
- Test with a lot of props in the scene, not an empty map. The overlap query is what gets expensive, and an empty map hides it completely.

See [Firework Parts](script-reference/firework-parts.md) for the full field reference on both components.

---

## Destruction Debris

When a `DestructibleBehavior` object runs out of hit points, its **Destroyed Prefab** is fetched from the game's destruction object pool, network-spawned in its place, and the original is despawned at the end of the frame.

Nothing rations that. There is no per-frame budget and no queue — a blast that breaks twenty objects performs twenty of those swaps in the same frame, so the cost of your debris prefab is paid twenty times over at exactly the moment the game is already busy.

Practical rules:

- Keep the debris prefab **cheap**: few chunks, low poly, one shared material. It is the thing that multiplies.
- Use **Ignore Damage Under** on `DestructibleBehavior` so small nearby explosions don't trigger a full swap for a scratch.
- Test destruction with many destructibles in range of one burst, not a single object.

!!! note "Destruction only happens in the running game"
    Damage is applied on the **server** only, and only while the game's global destruction setting is enabled. The object pool the debris comes from is provided by the game, not by the Mod Tools — so none of this can be exercised in the Editor.

---

## Audio

### Audio Clip Format

In the Unity Inspector for each `AudioClip`:

- **Load Type**: Use **Compressed In Memory** for short sound effects. Use **Streaming** only for long ambient tracks.
- **Compression Format**: Use **Vorbis** (quality ~50–70 is usually sufficient).
- **Sample Rate**: 44100 Hz is standard; lower rates (22050 Hz) are acceptable for distant/ambient sounds.

### Sound Variations

Adding 2–3 variations to a `GameSoundDefinition` costs very little in file size but greatly improves the feel of your mod. Players notice when the same sound plays identically every time.

---

## Mod File Size Budget

There is no maximum mod size. Nothing in the build pipeline warns you or fails when a mod is large. The table below is a sanity check — if your mod is several times one of these numbers, something in it is probably heavier than you intended:

| Mod Type | Rough Target |
|---|---|
| Single firework item | < 1 MB |
| Small firework pack (5–10 items) | < 5 MB |
| Large firework pack (20+ items) | < 15 MB |
| Custom map | < 50 MB |

Sizes vary a lot with complexity, and a legitimately ambitious map will exceed this. The point is not the number — it is to avoid unnecessary assets (high-res textures, uncompressed audio, leftover test assets) quietly bloating the build.

---

## Unused Assets

Before building, remove any assets from your mod folder that are not referenced by any definition or prefab. Unused assets still get included in the build if they are inside the mod folder.

---

## Profiling

After building, check the size of the exported `.mod` file. If it is larger than expected:

1. Look for **oversized textures** — these are usually the biggest culprit.
2. Check for **uncompressed audio clips**.
3. Check for **duplicate assets** (e.g. the same model imported multiple times).
4. Open the Unity **Editor Log** — the **⋮** menu in the top-right of the **Console** window, then **Open Editor Log** — and search it for the size breakdown Unity writes after a bundle build. It lists assets sorted by uncompressed size, which is usually enough to spot the offender.

If the build itself is failing rather than merely being fat, see [Troubleshooting & Build Errors](guides/troubleshooting.md).
