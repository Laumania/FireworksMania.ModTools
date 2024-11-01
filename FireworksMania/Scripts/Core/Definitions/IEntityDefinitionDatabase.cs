using FireworksMania.Core.Definitions.EntityDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FireworksMania.Core.Definitions
{
    public interface IEntityDefinitionDatabase
    {
        BaseEntityDefinition GetEntityDefinition(string entityDefinitionId);
    }
}
