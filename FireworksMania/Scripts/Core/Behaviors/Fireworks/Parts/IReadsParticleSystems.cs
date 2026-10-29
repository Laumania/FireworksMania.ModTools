using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors.Fireworks.Parts
{
    /// <summary>
    /// A component that plays, stops or waits on particle systems reports them here. Under heavy load the game switches
    /// off the particle systems a player is least likely to notice (#3214), and it never switches off one a component
    /// reads, wherever in the hierarchy it sits.
    /// </summary>
    public interface IReadsParticleSystems
    {
        /// <summary>Adds the particle systems this component plays, stops or waits on to <paramref name="results"/>. Unassigned ones are skipped.</summary>
        void CollectReadParticleSystems(List<ParticleSystem> results);
    }
}
