using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    public interface IIgnitable
    {
        Transform IgnitePositionTransform { get; }
        void Ignite(float ignitionForce);
        void IgniteInstant();
        bool Enabled                      { get; }
        bool IsIgnited                    { get; }

        //Defaulted so already-compiled mods implementing this interface keep loading (same pattern
        //as IDestructible.ApplyDamage). Implementations that support destruction attribution
        //override these; everything else silently drops the causer.
        void Ignite(float ignitionForce, ulong causerClientId) => Ignite(ignitionForce);
        void IgniteInstant(ulong causerClientId)               => IgniteInstant();
    }
}