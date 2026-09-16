using System.Collections.Generic;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Every live <see cref="PlaySoundOnImpactBehavior"/> in the scene, so a manager can decide which
    /// few of them actually carry an <see cref="ImpactCollisionRelay"/> at any moment
    /// (https://github.com/Laumania/FireworksMania/issues/2236).
    ///
    /// A plain static registry rather than Messenger events: the consumer needs the full live set on
    /// demand, not a stream of changes, and this keeps the whole thing unit testable.
    /// </summary>
    public static class ImpactSoundRegistry
    {
        private static readonly List<IImpactSoundCarrier> _registered = new List<IImpactSoundCarrier>();

        public static IReadOnlyList<IImpactSoundCarrier> Registered => _registered;

        //Carriers unregister themselves as they are destroyed, and ImpactSoundManager.OnDestroy clears
        //IsManaged - but with Domain Reload disabled anything those two miss is inherited by the next
        //play session, and an IsManaged that stayed true with no manager alive means silent impacts
        //because every carrier hands its relay away to nobody (#2612).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => Reset();

        /// <summary>
        /// True while an <c>ImpactSoundManager</c> is deciding who carries the collision message.
        /// Without one - a mod map, a test scene, an editor scene without MapEssentials - carriers
        /// fall back to carrying it themselves, so impact sounds never silently stop working.
        /// </summary>
        public static bool IsManaged { get; set; }

        public static void Register(IImpactSoundCarrier carrier)
        {
            if (carrier == null || _registered.Contains(carrier))
                return;

            _registered.Add(carrier);
        }

        public static void Unregister(IImpactSoundCarrier carrier)
        {
            if (carrier == null)
                return;

            _registered.Remove(carrier);
        }

        public static void Reset()
        {
            _registered.Clear();
            IsManaged = false;
        }
    }
}
