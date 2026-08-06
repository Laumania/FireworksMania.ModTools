using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// The only thing in the game that declares an <c>OnCollisionEnter</c> message. Added and removed
    /// at runtime by the impact-sound manager so that only a handful of objects at a time make Unity
    /// marshal managed <c>Collision</c> objects (https://github.com/Laumania/FireworksMania/issues/2236).
    ///
    /// Deliberately a plain MonoBehaviour and NOT a NetworkBehaviour: NGO assigns NetworkBehaviourId
    /// by child index in NetworkObject.InitializeChildNetworkBehaviours() at spawn time, so adding or
    /// removing a NetworkBehaviour at runtime is not safe. All networking stays on
    /// <see cref="PlaySoundOnImpactBehavior"/>, which is never added or removed.
    ///
    /// One relay serves EVERY <see cref="PlaySoundOnImpactBehavior"/> on its GameObject. Only one relay
    /// may exist ([DisallowMultipleComponent], and a second one would double the marshaling cost this
    /// whole system exists to avoid), but a GameObject may legitimately carry several behaviours - two
    /// colliders wanting two different sounds is normal mod content
    /// (https://github.com/Laumania/FireworksMania/issues/2241).
    /// </summary>
    [AddComponentMenu("")] //Runtime only - never authored on a prefab
    [DisallowMultipleComponent]
    public class ImpactCollisionRelay : MonoBehaviour
    {
        //Almost always exactly one, so a plain list beats anything cleverer.
        private readonly List<PlaySoundOnImpactBehavior> _targets = new List<PlaySoundOnImpactBehavior>(1);

        internal void AddTarget(PlaySoundOnImpactBehavior target)
        {
            if (target == null || _targets.Contains(target))
                return;

            _targets.Add(target);
        }

        /// <summary>
        /// Drops a target and reports whether the relay is still wanted by anyone on this GameObject.
        /// When it is not, the caller destroys it - see <see cref="PlaySoundOnImpactBehavior.SetCarryingCollisionMessage"/>.
        /// </summary>
        internal bool RemoveTarget(PlaySoundOnImpactBehavior target)
        {
            _targets.Remove(target);

            //A behaviour destroyed on its own (rather than with its GameObject) never gives its slot
            //back, and a dead entry must not keep the relay - and its marshaling cost - alive forever.
            for (int i = _targets.Count - 1; i >= 0; i--)
                if (_targets[i] == null)
                    _targets.RemoveAt(i);

            return _targets.Count > 0;
        }

        private void OnCollisionEnter(Collision collision)
        {
            //Unity DOES deliver this to disabled MonoBehaviours (measured on 6000.3.11f1, see #2236),
            //so both enabled checks are load bearing - they just cannot save the marshaling cost.
            if (this.enabled == false)
                return;

            var sqrImpulse = collision.impulse.sqrMagnitude;

            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];

                if (target != null && target.enabled)
                    target.HandleImpact(sqrImpulse);
            }
        }
    }
}
