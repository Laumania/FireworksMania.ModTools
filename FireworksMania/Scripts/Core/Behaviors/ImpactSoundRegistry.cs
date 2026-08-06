using System.Collections.Generic;

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
