using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IIgnitable
    {
        Transform IgnitePositionTransform { get; }
        void Ignite(float ignitionForce);
        void IgniteInstant();
        bool Enabled { get; }
        bool IsIgnited { get; }
    }
}