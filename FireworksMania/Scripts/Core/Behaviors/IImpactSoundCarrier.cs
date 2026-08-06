using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Something that can play a sound when it is hit, and that can be told whether it is currently
    /// worth paying Unity's collision-message cost for (see <see cref="ImpactCarrierSweep"/>).
    /// The interface exists so the selection policy can be unit tested without Unity objects.
    /// </summary>
    public interface IImpactSoundCarrier
    {
        Vector3 Position { get; }

        bool IsCarryingCollisionMessage { get; }

        void SetCarryingCollisionMessage(bool carrying);
    }
}
