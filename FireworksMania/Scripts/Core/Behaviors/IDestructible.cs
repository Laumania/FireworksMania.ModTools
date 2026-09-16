using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    /// <summary>
    /// Describes the explosion that caused damage. Debris spawning is staggered over multiple frames,
    /// so the debris does not exist yet in the frame the explosion happens - this data allows the
    /// debris to receive the explosion force it would have gotten, once it spawns.
    /// </summary>
    public readonly struct ExplosionDamageSource
    {
        /// <summary>
        /// <see cref="CauserClientId"/> when nothing caused this explosion on behalf of a player -
        /// a scene-placed hazard, an editor test, or a firework whose igniter is unknown.
        /// Destruction with no causer is never credited to anybody.
        /// </summary>
        public const ulong NoCauser = ulong.MaxValue;

        public ExplosionDamageSource(Vector3 position, float explosionForce, float range, float upwardsModifier, ForceMode forceMode, bool applyForceRelativeToMass)
            : this(position, explosionForce, range, upwardsModifier, forceMode, applyForceRelativeToMass, NoCauser)
        {
        }

        public ExplosionDamageSource(Vector3 position, float explosionForce, float range, float upwardsModifier, ForceMode forceMode, bool applyForceRelativeToMass, ulong causerClientId)
        {
            Position                 = position;
            ExplosionForce           = explosionForce;
            Range                    = range;
            UpwardsModifier          = upwardsModifier;
            ForceMode                = forceMode;
            ApplyForceRelativeToMass = applyForceRelativeToMass;
            CauserClientId           = causerClientId;
        }

        public Vector3   Position                 { get; }
        public float     ExplosionForce           { get; }
        public float     Range                    { get; }
        public float     UpwardsModifier          { get; }
        public ForceMode ForceMode                { get; }
        public bool      ApplyForceRelativeToMass { get; }

        /// <summary>
        /// The client whose ignited firework caused this explosion, or <see cref="NoCauser"/>. Used to
        /// credit destruction to the player responsible instead of to whoever happens to be hosting.
        /// </summary>
        public ulong CauserClientId { get; }
    }

    public interface IDestructible
    {
        void ApplyDamage(float damage);

        //Default implementation so existing IDestructible implementations (e.g. in mods) keep working without changes
        void ApplyDamage(float damage, in ExplosionDamageSource explosionSource) => ApplyDamage(damage);

        bool IsDestroyed { get; }
    }
}
