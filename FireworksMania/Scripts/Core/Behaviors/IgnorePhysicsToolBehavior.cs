using Unity.Netcode;
using UnityEngine;

namespace FireworksMania.Core.Behaviors
{
    public interface IIgnorePhysicsToolBehavior
    {
        bool ShouldBeIgnored { get; }
    }

    /// <summary>
    /// Marks a prop as off limits to the Physics Tool, either for good or until physics has set it free.
    /// <para>
    /// This is a <see cref="NetworkBehaviour"/> because "has this prop come loose?" cannot be answered
    /// locally on every peer: <see cref="Common.ClientNetworkRigidbody"/> forces every non-authority copy
    /// kinematic for its whole life, so a client reading its own <see cref="Rigidbody.isKinematic"/> sees
    /// "still pinned scenery" forever. The authority publishes what it sees, everybody else reads that
    /// (#2592). A <see cref="NetworkObject"/> is therefore what makes the replicated half work - without
    /// one, nothing here is replicated and the component falls back to reading the local Rigidbody, which
    /// is exactly how it behaved before and is still correct in single player.
    /// </para>
    /// </summary>
    [AddComponentMenu("Fireworks Mania/Behaviors/Other/IgnorePhysicsToolBehavior")]
    [DisallowMultipleComponent()]
    public class IgnorePhysicsToolBehavior : NetworkBehaviour, IIgnorePhysicsToolBehavior
    {
        [Tooltip("Always = Always ignore Physics Tool, OnlyWhenKinematic = Only ignore when marked as Kinematic (Static), OnlyWhenKinematicOnce = Only ignore until it has been none Kinematic once")]
        [SerializeField]
        private PhysicsToolIgnoreTypes _ignoreType = PhysicsToolIgnoreTypes.OnlyWhenKinematicOnce;

        //The authority's view of the Rigidbody, so that a peer whose own copy is held kinematic by
        //ClientNetworkRigidbody still knows whether the prop is actually loose (#2592)
        private NetworkVariable<bool> _networkIsKinematic           = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<bool> _networkHaveBeenNoneKinematic = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool                  _havePreviouslyBeenNoneKinematic = false;
        private Rigidbody             _rigidbody;

        void Awake()
        {
            //Earlier than the Start below so the state is publishable from OnNetworkSpawn, which for a
            //dynamically spawned NetworkObject runs before Start does
            _rigidbody = this.GetComponent<Rigidbody>();
        }

        void Start()
        {
            if (_rigidbody == null)
                _rigidbody = this.GetComponent<Rigidbody>();

            if(_rigidbody == null)
            {
                Debug.LogWarning($"No Rigidbody found, disabling '{nameof(IgnorePhysicsToolBehavior)}'");
                this.enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            //So the very first frame after a spawn already carries the truth, rather than the serialized
            //defaults - a prop that starts out loose would otherwise read as pinned scenery for a frame
            PublishAuthorityPhysicsState();
        }

        void Update()
        {
            PublishAuthorityPhysicsState();
        }

        /// <summary>
        /// Copies the authority's Rigidbody state onto the wire. Polled rather than pushed because
        /// <see cref="Rigidbody.isKinematic"/> is a plain direct write made from all over the codebase -
        /// an explosion freeing a prop, a mount point seating a firework, the Physics Tool grabbing
        /// something - with no event to hang off of.
        /// </summary>
        private void PublishAuthorityPhysicsState()
        {
            if (IsSpawned == false || IsServer == false || _rigidbody == null)
                return;

            //Always ignores everything, so there is nothing about the Rigidbody worth replicating
            if (_ignoreType == PhysicsToolIgnoreTypes.Always)
                return;

            //OnlyWhenKinematicOnce is sticky, so once the prop has come loose the answer can never change
            //again and this stops costing anything for the rest of the prop's life
            if (_ignoreType == PhysicsToolIgnoreTypes.OnlyWhenKinematicOnce && _networkHaveBeenNoneKinematic.Value)
                return;

            var isKinematic = _rigidbody.isKinematic;

            if (isKinematic == false && _networkHaveBeenNoneKinematic.Value == false)
                _networkHaveBeenNoneKinematic.Value = true;

            if (_networkIsKinematic.Value != isKinematic)
                _networkIsKinematic.Value = isKinematic;
        }

        public bool ShouldBeIgnored
        {
            get
            {
                if (this.enabled == false || _rigidbody == null)
                    return false;

                switch (_ignoreType)
                {
                    case PhysicsToolIgnoreTypes.OnlyWhenKinematic:
                        return IsKinematic;
                    case PhysicsToolIgnoreTypes.OnlyWhenKinematicOnce:
                        return HaveBeenNoneKinematic == false;
                    case PhysicsToolIgnoreTypes.Always:
                    default:
                        return true;
                }
            }
        }

        //Both of these deliberately let EITHER view free the prop: the local Rigidbody can only ever
        //lie in the "still kinematic" direction - a non-authority copy is pinned kinematic, it is never
        //spuriously dynamic - so taking the more permissive of the two answers can only ever add the
        //cases that were wrongly refused, never take away a refusal that was right. When the object is
        //not spawned there is no replicated half at all and this is exactly the old local-only behavior.
        private bool IsKinematic => _rigidbody.isKinematic && (IsSpawned == false || _networkIsKinematic.Value);

        private bool HaveBeenNoneKinematic
        {
            get
            {
                if (_havePreviouslyBeenNoneKinematic == false)
                    _havePreviouslyBeenNoneKinematic = _rigidbody.isKinematic == false;

                return _havePreviouslyBeenNoneKinematic || (IsSpawned && _networkHaveBeenNoneKinematic.Value);
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
