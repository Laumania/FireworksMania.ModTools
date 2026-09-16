using UnityEngine;

namespace FireworksMania.Core.Definitions
{
    /// <summary>
    /// What the two grouped grids need from "a collection" (#2048): a stable id, the name a header
    /// shows, and an optional logo. <see cref="BrandCollection"/> implements it for fireworks and
    /// props, <see cref="CharacterCollection"/> for characters — different assets with different
    /// authoring, one grouping path. Additive to the modder-facing Core surface.
    /// </summary>
    public interface IItemCollection
    {
        string Id { get; }
        string Name { get; }
        Sprite Icon { get; }
    }
}
