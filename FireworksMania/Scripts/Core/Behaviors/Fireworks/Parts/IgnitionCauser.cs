using FireworksMania.Core.Behaviors;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// Server-side holder for "which player's ignition chain lit this object". First real causer
    /// wins: once set, later claims are ignored until <see cref="Reset"/>. Never replicated, never
    /// serialized - purely the server's bookkeeping for destruction attribution.
    /// </summary>
    public struct IgnitionCauser
    {
        private ulong _value;
        private bool  _hasValue;

        public ulong Value => _hasValue ? _value : ExplosionDamageSource.NoCauser;

        /// <summary>Sets the causer if none is set yet. A NoCauser claim never consumes the slot.</summary>
        public bool TrySet(ulong causerClientId)
        {
            if (_hasValue || causerClientId == ExplosionDamageSource.NoCauser)
                return false;

            _value    = causerClientId;
            _hasValue = true;
            return true;
        }

        public void Reset()
        {
            _hasValue = false;
        }
    }
}
