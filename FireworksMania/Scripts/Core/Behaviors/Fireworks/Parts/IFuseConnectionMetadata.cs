using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IFuseConnectionMetadata
    {
        string TypeId                  { get; }
        string TypeName                { get; }
        float BurnRateInMeterPerSecond { get; }
        Material Material              { get; }
    }
}
