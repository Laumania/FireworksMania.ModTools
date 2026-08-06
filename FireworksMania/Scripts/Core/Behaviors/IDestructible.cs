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
        public ExplosionDamageSource(Vector3 position, float explosionForce, float range, float upwardsModifier, ForceMode forceMode, bool applyForceRelativeToMass)
        {
            Position                 = position;
            ExplosionForce           = explosionForce;
            Range                    = range;
            UpwardsModifier          = upwardsModifier;
            ForceMode                = forceMode;
            ApplyForceRelativeToMass = applyForceRelativeToMass;
        }

        public Vector3   Position                 { get; }
        public float     ExplosionForce           { get; }
        public float     Range                    { get; }
        public float     UpwardsModifier          { get; }
        public ForceMode ForceMode                { get; }
        public bool      ApplyForceRelativeToMass { get; }
    }

    public interface IDestructible
    {
        void ApplyDamage(float damage);

        //Default implementation so existing IDestructible implementations (e.g. in mods) keep working without changes
        void ApplyDamage(float damage, in ExplosionDamageSource explosionSource) => ApplyDamage(damage);

        bool IsDestroyed { get; }
    }
}
