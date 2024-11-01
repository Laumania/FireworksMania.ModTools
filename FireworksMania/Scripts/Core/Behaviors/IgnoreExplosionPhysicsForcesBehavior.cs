using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IIgnoreExplosionPhysicsForcesBehavior
    {
    }

    [AddComponentMenu("Fireworks Mania/Behaviors/Other/IgnoreExplosionPhysicsForcesBehavior")]
    [DisallowMultipleComponent()]
    public class IgnoreExplosionPhysicsForcesBehavior : MonoBehaviour, IIgnoreExplosionPhysicsForcesBehavior
    {
    }
}
