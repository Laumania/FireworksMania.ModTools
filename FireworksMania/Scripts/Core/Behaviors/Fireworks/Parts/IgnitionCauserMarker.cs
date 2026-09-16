using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Carrier for objects that have no behavior of their own to hang attribution on - added at
    /// runtime to spawned wreckage so its embedded explosion credits the player whose chain
    /// destroyed the original. Server-side only; the value means nothing on clients.
    /// </summary>
    public class IgnitionCauserMarker : MonoBehaviour, IIgnitionCauserCarrier
    {
        private IgnitionCauser _ignitionCauser;

        public ulong IgnitionCauserClientId                 => _ignitionCauser.Value;
        public void TrySetIgnitionCauser(ulong causerClientId) => _ignitionCauser.TrySet(causerClientId);
        public void ResetIgnitionCauser()                      => _ignitionCauser.Reset();
    }
}
