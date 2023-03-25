using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IIgnorePickUpBehavior
    {
        bool ShouldBeIgnored { get; }
    }

    [AddComponentMenu("Fireworks Mania/Behaviors/Other/IgnorePickUpBehavior")]
    [DisallowMultipleComponent()]
    public class IgnorePickUpBehavior : MonoBehaviour, IIgnorePickUpBehavior
    {
        public bool ShouldBeIgnored
        {
            get
            {
                if (this.enabled == false)
                    return false;

                return true;
            }
        }
    }
}
