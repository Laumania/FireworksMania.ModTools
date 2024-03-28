using FireworksMania.Core.Definitions.EntityDefinitions;

namespace FireworksMania.Core.Persistence
{
    public interface IHaveBaseEntityDefinition
    {
        BaseEntityDefinition EntityDefinition { get; set; }
    }
}
