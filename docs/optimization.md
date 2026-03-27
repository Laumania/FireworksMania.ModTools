# Optimization

Keeping your mod small and efficient makes it faster to download, faster to load, and easier on players' hardware. This page covers the most impactful optimization techniques.

---

## Build Settings — File Size Optimization

This is the single most important setting. In **Mod Tools → Export Settings**, under the **Build** tab, always set **Optimize for** to **File Size**.

!!! danger "Always use File Size optimization"
    If you leave this at the default setting your mod will be significantly larger than necessary. This increases download time for every player who subscribes to your mod and slows down game loading.

---

## 3D Models

### Polygon Count

- Aim for the **lowest polygon count** that still looks good in-game.
- Fireworks are small objects — players rarely zoom in closely. High-poly meshes add file size without visible improvement.
- A typical firework shell or tube: **200–800 triangles** is usually sufficient.
- Avoid smooth-shading on hard-edged low-poly objects; use flat shading instead.

### Texture Atlasing

- Combine multiple small textures into a single **texture atlas** to reduce draw calls.
- Use a shared atlas across multiple items in the same mod if possible.

### Texture Resolution

| Object Size | Recommended Max Texture Resolution |
|---|---|
| Small (fuse, cap) | 64×64 or 128×128 |
| Medium (tube, rocket body) | 256×256 |
| Large (mortar, map prop) | 512×512 |

Avoid 1024×1024 or higher unless the object is very large and viewed up close.

### Import Settings

In the Unity Inspector for each texture, check:

- **Compression**: Use **Crunch Compression** where possible.
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

### GPU Instancing

Always enable **GPU Instancing** on particle materials:

1. Select the material used by the particle system.
2. In the Inspector, enable **Enable GPU Instancing**.

This dramatically reduces CPU overhead when many instances of the same particle effect are active simultaneously.

### Sub-Emitters

- Use sub-emitters sparingly. Each sub-emitter adds overhead.
- Collapse multiple sub-emitters into a single particle system where the visual result is the same.

### Simulation Space

- Use **Local** simulation space for effects that should move with the firework.
- Use **World** simulation space for effects that should stay in place after launch (e.g. smoke trail).

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

As a rough guide:

| Mod Type | Target Size |
|---|---|
| Single firework item | < 1 MB |
| Small firework pack (5–10 items) | < 5 MB |
| Large firework pack (20+ items) | < 15 MB |
| Custom map | < 50 MB |

These are guidelines, not hard limits. Sizes will vary depending on complexity. The most important thing is to avoid unnecessary assets (high-res textures, uncompressed audio, unused assets) bloating the build.

---

## Unused Assets

Before building, remove any assets from your mod folder that are not referenced by any definition or prefab. Unused assets still get included in the build if they are inside the mod folder.

---

## Profiling

After building, check the size of the exported `.mod` file. If it is larger than expected:

1. Look for **oversized textures** — these are usually the biggest culprit.
2. Check for **uncompressed audio clips**.
3. Check for **duplicate assets** (e.g. the same model imported multiple times).
4. Use the Unity **Editor Log** (Help → Open Editor Log) to see the build size report, which lists each asset and its contribution to the total size.
