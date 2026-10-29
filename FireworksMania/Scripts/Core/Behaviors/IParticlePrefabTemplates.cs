using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Instantiates a prefab, or a part of one, from the game's particle mesh template of that prefab when it has one,
    /// so the clone does not pay Unity's cost for a particle mesh shared by thousands of renderers (#3109, #3123).
    /// MortarTube clones a loaded shell's effects through it. The result is a fresh clone the caller owns, placed as
    /// Object.Instantiate(original, parent) would place it. A clone of a part of a prefab keeps that prefab's template
    /// in memory until the clone is destroyed.
    ///
    /// Implemented by the game and found through DependencyResolver. Mods should not implement it.
    /// </summary>
    public interface IParticlePrefabTemplates
    {
        GameObject Instantiate(GameObject original, Transform parent);
    }
}
