using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IFuseConnectionPoint
    {
        void SetAsActiveSource(bool active);
        IFuse Fuse           { get; }
        Transform Transform  { get; }
    }
}
