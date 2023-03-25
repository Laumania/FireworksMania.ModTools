using System.Collections.Generic;

namespace FireworksMania.Core.Persistence
{
    public interface ISaveablePostActivatedComponent
    {
        void PostActivate(IDictionary<string, SaveableEntity> entityDictionary);
    }
}
