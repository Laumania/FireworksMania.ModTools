using FireworksMania.Core.Definitions.EntityDefinitions;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// A firework that is listed by its diameter, caliber or class in the inventory and Shop (#1497).
    /// Null means the reference was never set: the item still shows, just under no size or class.
    /// </summary>
    public interface IHaveEntityClassificationDefinition
    {
        EntityClassificationDefinition ClassificationDefinition { get; }
    }
}
