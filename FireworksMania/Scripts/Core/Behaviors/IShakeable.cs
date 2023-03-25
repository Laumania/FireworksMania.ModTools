using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IShakeable
    {
        Vector3 Position { get; }
        void InduceStress(float stress);
    }
}
