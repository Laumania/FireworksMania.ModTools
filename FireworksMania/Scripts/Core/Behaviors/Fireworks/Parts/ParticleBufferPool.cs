using System;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// One shared scratch buffer for <see cref="ParticleSystem.GetParticles(ParticleSystem.Particle[])"/>.
    ///
    /// Every <see cref="ParticleSystemObserver"/> used to own a buffer sized to its own maxParticles,
    /// allocated up front. With a few hundred observers alive during a big chain that added up to tens
    /// of megabytes of garbage, and the multi-megabyte arrays landed on the large object heap where they
    /// caused visible collection spikes.
    ///
    /// The buffer only has to survive a single call - it is filled by GetParticles and consumed
    /// immediately - so one buffer, grown to the largest size anyone has asked for, serves every
    /// observer. Renting is main-thread only, which is where the player loop calls Update.
    /// </summary>
    public static class ParticleBufferPool
    {
        private static ParticleSystem.Particle[] _buffer = Array.Empty<ParticleSystem.Particle>();

        /// <summary>
        /// Returns the shared buffer, grown if needed so it holds at least <paramref name="minimumSize"/>
        /// particles. The contents are whatever the previous renter left behind, so callers must only read
        /// back as many entries as GetParticles reports.
        /// </summary>
        public static ParticleSystem.Particle[] Rent(int minimumSize)
        {
            if (minimumSize <= 0)
                return Array.Empty<ParticleSystem.Particle>();

            if (_buffer.Length < minimumSize)
                _buffer = new ParticleSystem.Particle[minimumSize];

            return _buffer;
        }

        /// <summary>
        /// Drops the shared buffer so it can be collected. The next rent allocates again.
        /// </summary>
        public static void Reset() => _buffer = Array.Empty<ParticleSystem.Particle>();
    }
}
