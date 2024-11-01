using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IIgnorePhysicsToolBehavior
    {
        bool ShouldBeIgnored { get; }
    }

    [AddComponentMenu("Fireworks Mania/Behaviors/Other/IgnorePhysicsToolBehavior")]
    [DisallowMultipleComponent()]
    public class IgnorePhysicsToolBehavior : MonoBehaviour, IIgnorePhysicsToolBehavior
    {
        [Tooltip("Always = Always ignore Physics Tool, OnlyWhenKinematic = Only ignore when marked as Kinematic (Static), OnlyWhenKinematicOnce = Only ignore until it has been none Kinematic once")]
        [SerializeField]
        private PhysicsToolIgnoreTypes _ignoreType = PhysicsToolIgnoreTypes.OnlyWhenKinematicOnce;

        private bool _havePreviouslyBeenNoneKinematic = false;
        private Rigidbody _rigidbody;

        void Start()
        {
            _rigidbody = this.GetComponent<Rigidbody>();

            if(_rigidbody == null)
            {
                Debug.LogWarning($"No Rigidbody found, disabling '{nameof(IgnorePhysicsToolBehavior)}'");
                this.enabled = false;
            }
        }

        public bool ShouldBeIgnored
        {
            get
            {
                if (this.enabled == false || _rigidbody == null)
                    return false;

                if (_havePreviouslyBeenNoneKinematic == false)
                    _havePreviouslyBeenNoneKinematic = _rigidbody.isKinematic == false;

                switch (_ignoreType)
                {
                    case PhysicsToolIgnoreTypes.OnlyWhenKinematic:
                        return _rigidbody.isKinematic;
                    case PhysicsToolIgnoreTypes.OnlyWhenKinematicOnce:
                        return _havePreviouslyBeenNoneKinematic == false;
                    case PhysicsToolIgnoreTypes.Always:
                    default:
                        return true;
                }
            }
        }

        public enum PhysicsToolIgnoreTypes
        {
            Always = 0,
            OnlyWhenKinematic = 1,
            OnlyWhenKinematicOnce = 2
        }
    }
}
