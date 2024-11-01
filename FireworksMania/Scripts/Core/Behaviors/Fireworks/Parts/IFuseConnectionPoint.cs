using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IFuseConnectionPoint
    {
        void Setup(Fuse fuse);
        void SetAsActiveSource(bool active);
        Fuse Fuse           { get; }
        Vector3 Position    { get; }
        Transform Transform { get; }
    }
}
